import { useState } from "react"

interface CreatedUrlProps {
    name: string
    url: string
    onDelete: () => Promise<void>
}

function CreatedUrl({ name, url, onDelete }: CreatedUrlProps) {
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
        <>
            <p>{name}:
                <a href={url} target="_blank" rel="noopener noreferrer">{url}</a>
                <button className="button-danger" onClick={handleDelete} disabled={deleting}>Delete</button>
            </p>
            {deleteError && <p className="error">{deleteError}</p>}
        </>
    )
}

export default CreatedUrl
