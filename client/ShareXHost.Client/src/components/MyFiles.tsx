import { useEffect, useState } from "react"
import { useFiles, FileError, type FileResponse } from "../hooks/useFiles.ts"
import { useUpload } from "../hooks/useUpload.ts"
import FileRow from "./FileRow.tsx"

function MyFiles() {
    const { getMyFiles } = useFiles()
    const { deleteFile } = useUpload()

    const [files, setFiles] = useState<FileResponse[]>([])
    const [page, setPage] = useState(1)
    const [pageSize] = useState(5)
    const [totalCount, setTotalCount] = useState(0)
    const [loading, setLoading] = useState(false)
    const [error, setError] = useState<string | null>(null)

    async function loadPage(newPage: number) {
        setLoading(true)
        setError(null)
        try {
            const result = await getMyFiles(newPage, pageSize)
            setFiles(result.items)
            setPage(newPage)
            setTotalCount(result.totalCount)
        } catch (err) {
            if (err instanceof FileError && err.status === 401) {
                setError("Your session has expired. Please log in again.")
            } else {
                setError("Failed to load files. Try again later.")
            }
        } finally {
            setLoading(false)
        }
    }

    useEffect(() => {
        loadPage(1)
    }, [])

    async function handleDelete(fileId: string) {
        await deleteFile(`/files/${fileId}`)

        const newTotalCount = totalCount - 1
        const newTotalPages = Math.ceil(newTotalCount / pageSize)

        setTotalCount(newTotalCount)

        if (page > newTotalPages) {
            if (newTotalPages > 0) await loadPage(newTotalPages)
            else setFiles([])
        } else {
            await loadPage(page)
        }
    }

    const totalPages = Math.ceil(totalCount / pageSize)

    return (
        <section className="tool">
            <h2>My Files</h2>
            {loading && <p>Loading...</p>}
            {!loading && error && <p className="error">{error}</p>}
            {!loading && !error && (
                <>
                    <table className="file-table">
                        <thead>
                            <tr>
                                <th>Name</th>
                                <th>Size</th>
                                <th>Created</th>
                                <th>Actions</th>
                            </tr>
                        </thead>
                        <tbody>
                            {files.map(file => (
                                <FileRow key={file.id} file={file} onDelete={() => handleDelete(file.id)} />
                            ))}
                            {files.length === 0 && (
                                <tr>
                                    <td colSpan={4}>No files yet.</td>
                                </tr>
                            )}
                        </tbody>
                    </table>
                    {totalPages > 1 && (
                        <div className="pagination">
                            <button onClick={() => loadPage(page - 1)} disabled={page <= 1}>Previous</button>
                            <span>Page {page} of {totalPages}</span>
                            <button onClick={() => loadPage(page + 1)} disabled={page >= totalPages}>Next</button>
                        </div>
                    )}
                </>
            )}
        </section>
    )
}

export default MyFiles
