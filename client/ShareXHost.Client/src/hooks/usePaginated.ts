import {useState} from "react";
import {type PaginatedApi, PaginationError} from "../api/createPaginatedApi.ts";

export interface UsePaginatedResult<TResponse>
{
    items: TResponse[],
    page: number,
    totalPages: number,
    loading: boolean,
    error: PaginationError | null,
    loadPage: (newPage: number) => Promise<void>,
    handleDelete: (fileId: string) => Promise<void>
}

function usePaginated<TResponse>(api: PaginatedApi<TResponse>, deleteItem: (id: string) => Promise<void>, pageSize: number = 5)
    : UsePaginatedResult<TResponse>
{
    const [items, setItems] = useState<TResponse[]>([])
    const [page, setPage] = useState(1)
    const [totalCount, setTotalCount] = useState(0)
    const [loading, setLoading] = useState(false)
    const [error, setError] = useState<PaginationError | null>(null)

    async function loadPage(newPage: number) {
        setLoading(true)
        setError(null)
        try {
            const result = await api.getItems(newPage, pageSize)
            setItems(result.items)
            setPage(newPage)
            setTotalCount(result.totalCount)
        } catch (err) {
            if (err instanceof PaginationError) {
                setError(err)
            } else {
                setError(new PaginationError("Unknown error!", 0))
            }
        } finally {
            setLoading(false)
        }
    }

    async function handleDelete(itemId: string) {
        await deleteItem(itemId)

        const newTotalCount = totalCount - 1
        const newTotalPages = Math.ceil(newTotalCount / pageSize)

        setTotalCount(newTotalCount)

        if (page > newTotalPages) {
            if (newTotalPages > 0) await loadPage(newTotalPages)
            else setItems([])
        } else {
            await loadPage(page)
        }
    }
    
    return {
        items,
        page,
        totalPages: Math.ceil(totalCount / pageSize),
        loading,
        error,
        loadPage,
        handleDelete
    }
}

export default usePaginated