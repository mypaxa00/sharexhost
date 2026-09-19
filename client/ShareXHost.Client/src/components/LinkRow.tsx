import { useState } from "react"
import type {LinkResponse} from "../models/LinkResponse.ts";
import {formatDate} from "../utils/formatDate.ts";

interface LinkRowProps {
    link: LinkResponse
    onDelete: () => Promise<void>
}

function LinkRow({ link, onDelete }: LinkRowProps) {
    const [deleting, setDeleting] = useState(false)
    const [deleteError, setDeleteError] = useState<string | null>(null)

    async function handleDelete() {
        setDeleteError(null)
        setDeleting(true)
        try {
            await onDelete()
        } catch (error) {
            if (error instanceof Error) {
                setDeleteError(error.message)
            } else {
                setDeleteError("Unknown error! Try again later.")
            }
        } finally {
            setDeleting(false)
        }
    }

    return (
        <tr>
            <td>
                <a href={link.url} target="_blank" rel="noopener noreferrer">
                    {link.url}
                </a>
            </td>
            <td>{formatDate(link.createdAt)}</td>
            <td>
                <a className="button-accent" href={`/s/${link.shortId}`} target="_blank" rel="noopener noreferrer">
                    Open
                </a>
            </td>
            <td>
                <button className="button-danger" onClick={handleDelete} disabled={deleting}>Delete</button>
                {deleteError && <p className="error">{deleteError}</p>}
            </td>
        </tr>
    )
}

export default LinkRow
