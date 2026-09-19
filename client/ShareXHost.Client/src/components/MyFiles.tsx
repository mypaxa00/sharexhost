import { useEffect } from "react"
import FileRow from "./FileRow.tsx"
import {createPaginatedApi, type PaginatedDataSource} from "../api/createPaginatedApi.ts";
import type {FileResponse} from "../models/FileResponse.ts";
import {deleteFile} from "../api/filesApi.ts";
import usePaginated from "../hooks/usePaginated.ts";
import {ApiError} from "../dto/ApiError.ts";

const pageSize = 5;
const api: PaginatedDataSource<FileResponse> = createPaginatedApi("/files/mine");

function deleteFileById(fileId: string): Promise<void> {
    return deleteFile(`/files/${fileId}`)
}

function MyFiles() {
    const pagination = usePaginated(api, deleteFileById, pageSize)

    useEffect(() => {
        pagination.loadPage(1)
    }, [pagination.loadPage])

    return (
        <section className="tool">
            <h2>My Files</h2>
            {pagination.loading && <p>Loading...</p>}
            {!pagination.loading && pagination.error && <p className="error">{
                pagination.error instanceof ApiError && pagination.error.status === 401
                ? "Your session has expired. Please log in again."
                : "Failed to load files. Try again later."
            }</p>}
            {!pagination.loading && !pagination.error && (
                <>
                    <table className="file-table">
                        <thead>
                            <tr>
                                <th>Name</th>
                                <th>Size</th>
                                <th>Created</th>
                                <th colSpan={2}>Actions</th>
                            </tr>
                        </thead>
                        <tbody>
                            {pagination.items.map(file => (
                                <FileRow key={file.id} file={file} onDelete={() => pagination.handleDelete(file.id)} />
                            ))}
                            {pagination.items.length === 0 && (
                                <tr>
                                    <td colSpan={5}>No files yet.</td>
                                </tr>
                            )}
                        </tbody>
                    </table>
                    {pagination.totalPages > 1 && (
                        <div className="pagination">
                            <button onClick={() => pagination.loadPage(pagination.page - 1)} disabled={pagination.page <= 1}>Previous</button>
                            <span>Page {pagination.page} of {pagination.totalPages}</span>
                            <button onClick={() => pagination.loadPage(pagination.page + 1)} disabled={pagination.page >= pagination.totalPages}>Next</button>
                        </div>
                    )}
                </>
            )}
        </section>
    )
}

export default MyFiles
