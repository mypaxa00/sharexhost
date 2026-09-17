import { useState } from "react"
import type { FileResponse } from "../hooks/useFiles.ts"

function FileRow({ file, onDelete }: { file: FileResponse, onDelete: () => Promise<void> }) {
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
            <td>{file.originalFileName}</td>
            <td>{formatSize(file.sizeBytes)}</td>
            <td>{new Date(file.createdAt).toLocaleString()}</td>
            <td>
                <a className="button-accent" href={`/files/${file.id}`} download>Download</a>
                <button className="button-danger" onClick={handleDelete} disabled={deleting}>Delete</button>
                {deleteError && <p className="error">{deleteError}</p>}
            </td>
        </tr>
    )
}

function formatSize(bytes: number): string {
    if (bytes < 1024) return `${bytes} B`
    const kb = bytes / 1024
    if (kb < 1024) return `${kb.toFixed(1)} KB`
    const mb = kb / 1024
    if (mb < 1024) return `${mb.toFixed(1)} MB`
    return `${(mb / 1024).toFixed(1)} GB`
}

export default FileRow
