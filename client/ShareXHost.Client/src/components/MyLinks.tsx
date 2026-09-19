import { useEffect } from "react"
import {createPaginatedApi, type PaginatedDataSource} from "../api/createPaginatedApi.ts";
import usePaginated, {type UsePaginatedResult} from "../hooks/usePaginated.ts";
import {deleteLink} from "../api/linksApi.ts";
import type {LinkResponse} from "../models/LinkResponse.ts";
import LinkRow from "./LinkRow.tsx";
import {ApiError} from "../dto/ApiError.ts";

const pageSize = 5;
const api: PaginatedDataSource<LinkResponse> = createPaginatedApi("/links/mine");

function deleteLinkById(fileId: string): Promise<void> {
    return deleteLink(`/links/${fileId}`)
}

function MyLinks() {
    const pagination: UsePaginatedResult<LinkResponse> = usePaginated(api, deleteLinkById, pageSize)

    useEffect(() => {
        pagination.loadPage(1)
    }, [pagination.loadPage])

    return (
        <section className="tool">
            <h2>My Links</h2>
            {pagination.loading && <p>Loading...</p>}
            {!pagination.loading && pagination.error && <p className="error">{
                pagination.error instanceof ApiError && pagination.error.status === 401
                    ? "Your session has expired. Please log in again."
                    : "Failed to load links. Try again later."
            }</p>}
            {!pagination.loading && !pagination.error && (
                <>
                    <table className="file-table">
                        <thead>
                        <tr>
                            <th>Destination</th>
                            <th>Created</th>
                            <th colSpan={2}>Actions</th>
                        </tr>
                        </thead>
                        <tbody>
                        {pagination.items.map(link => (
                            <LinkRow key={link.shortId} link={link} onDelete={() => pagination.handleDelete(link.shortId)} />
                        ))}
                        {pagination.items.length === 0 && (
                            <tr>
                                <td colSpan={4}>No links yet.</td>
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

export default MyLinks
