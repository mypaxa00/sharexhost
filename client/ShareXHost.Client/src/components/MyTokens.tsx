import { useEffect } from "react"
import {createPaginatedApi, type PaginatedDataSource} from "../api/createPaginatedApi.ts";
import usePaginated, {type UsePaginatedResult} from "../hooks/usePaginated.ts";
import {type ApiTokenResponse, deleteApiToken} from "../api/tokensApi.ts";
import TokenRow from "./TokenRow.tsx";
import {ApiError} from "../dto/ApiError.ts";

const pageSize = 5;
const api: PaginatedDataSource<ApiTokenResponse> = createPaginatedApi("/auth/tokens");

function deleteTokenById(id: string): Promise<void> {
    return deleteApiToken(id)
}

function MyTokens() {
    const pagination: UsePaginatedResult<ApiTokenResponse> = usePaginated(api, deleteTokenById, pageSize)

    useEffect(() => {
        pagination.loadPage(1)
    }, [pagination.loadPage])

    return (
        <section className="tool">
            <h2>My Tokens</h2>
            {pagination.loading && <p>Loading...</p>}
            {!pagination.loading && pagination.error && <p className="error">{
                pagination.error instanceof ApiError && pagination.error.status === 401
                    ? "Your session has expired. Please log in again."
                    : "Failed to load tokens. Try again later."
            }</p>}
            {!pagination.loading && !pagination.error && (
                <>
                    <table className="file-table">
                        <thead>
                        <tr>
                            <th>Name</th>
                            <th>Created</th>
                            <th>Actions</th>
                        </tr>
                        </thead>
                        <tbody>
                        {pagination.items.map(token => (
                            <TokenRow key={token.id} token={token} onDelete={() => pagination.handleDelete(token.id)} />
                        ))}
                        {pagination.items.length === 0 && (
                            <tr>
                                <td colSpan={3}>No tokens yet.</td>
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

export default MyTokens
