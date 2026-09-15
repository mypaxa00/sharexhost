import { useState } from "react"
import { useLinks, LinkError, type LinkResponse } from "../hooks/useLinks.ts";
import CreatedUrl from "./CreatedUrl.tsx"

function CreateLink() {
    const { createLink, deleteLink } = useLinks()
    const [url, setUrl] = useState('')
    const [linkResult, setLinkResult] = useState<LinkResponse | null>(null)
    const [createError, setCreateError] = useState<string | null>(null)
    const [creating, setCreating] = useState(false)

    // url is null or empty
    const createDisabled = url === '';
    
    async function onCreateLink() {
        if (!url) return;
        setCreateError(null)

        setCreating(true)
        try {
            const result = await createLink(url)
            setUrl('')
            setLinkResult(result)
        } catch (error) {
            if (error instanceof LinkError) {
                if (error.status === 400) {
                    setCreateError('Link creation failed: Invalid URL.')
                }
                else if (error.status === 429) {
                    setCreateError('Link creation failed: Too many requests. Please try again later.')
                } else {
                    setCreateError(`Link creation failed with status ${error.status}`)
                }
            }
            else {
                setCreateError(`Link creation failed due to an unknown error: ${error}`)
            }
        }
        finally {
            setCreating(false)
        }
    }
    async function onDeleteLink() {
        try {
            await deleteLink(linkResult.deletionUrl)
            setLinkResult(null)
        } catch (error) {
            if (error instanceof LinkError && error.status === 403) {
                throw new Error("You don't have permission to delete this link.")
            } else {
                throw new Error("Unknown error! Try again later.")
            }
        }
    }
    
    return (
        <section className="tool">
            <h2>Create Link</h2>
            <input className="url-input" type="text" placeholder="Enter URL" value={url} onChange={(e) => {
                setUrl(e.target.value)
                setLinkResult(null)
                setCreateError(null)
            }} />
            <button className="button-accent" onClick={onCreateLink} disabled={creating || createDisabled}>Create Link</button>
            {creating && <p>Creating link...</p>}
            {createError && <p className="error">{createError}</p>}
            {!creating && linkResult && <CreatedUrl name="Short URL" url={linkResult.url} onDelete={onDeleteLink}/>}
        </section>
    )
}

export default CreateLink;