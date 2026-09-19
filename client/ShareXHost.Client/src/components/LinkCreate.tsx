import { useState } from "react"
import CreatedUrl from "./CreatedUrl.tsx"
import {createLink, deleteLink, type CreateLinkResponse} from "../api/linksApi.ts";
import {ApiError} from "../dto/ApiError.ts";

function LinkCreate() {
    const [url, setUrl] = useState('')
    const [linkResult, setLinkResult] = useState<CreateLinkResponse | null>(null)
    const [createError, setCreateError] = useState<string | null>(null)
    const [creating, setCreating] = useState(false)
    const [deleteError, setDeleteError] = useState<string | null>(null)

    // url is null or empty
    const createDisabled = !url || url.trim() === '';
    
    async function onCreateLink() {
        if (!url || url.trim() === '') return;
        setCreateError(null)

        setCreating(true)
        try {
            const result = await createLink(url)
            setUrl('')
            setLinkResult(result)
        } catch (error) {
            if (error instanceof ApiError) {
                if (error.status === 400) {
                    setCreateError('Invalid URL.')
                }
                else if (error.status === 429) {
                    setCreateError('Too many requests. Please try again later.')
                } else {
                    setCreateError(`Link creation failed. Please try again later.`)
                }
            }
            else {
                setCreateError(`Link creation failed. Please try again later.`)
            }
        }
        finally {
            setCreating(false)
        }
    }
    async function onDeleteLink() {
        try {
            await deleteLink(linkResult!.deletionUrl)
            setLinkResult(null)
        } catch (error) {
            if (error instanceof ApiError && error.status === 403) {
                setDeleteError("You don't have permission to delete this link.")
            } else {
                setDeleteError("Unknown error! Try again later.")
            }
        }
    }
    
    return (
        <section className="tool">
            <h2>Create Link</h2>
            <input className="string-input" type="text" placeholder="Enter URL" value={url} onChange={(e) => {
                setUrl(e.target.value)
                setLinkResult(null)
                setCreateError(null)
            }} />
            <button className="button-accent" onClick={onCreateLink} disabled={creating || createDisabled}>Create Link</button>
            {creating && <p>Creating link...</p>}
            {createError && <p className="error">{createError}</p>}
            {deleteError && <p className="error">{deleteError}</p>}
            {!creating && linkResult && <CreatedUrl name="Short URL" url={linkResult.url} onDelete={onDeleteLink}/>}
        </section>
    )
}

export default LinkCreate;