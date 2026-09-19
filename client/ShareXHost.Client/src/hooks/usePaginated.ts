import {useCallback, useState} from "react";
import {type PaginatedDataSource} from "../api/createPaginatedApi.ts";

export interface UsePaginatedResult<TResponse>
{
    items: TResponse[],
    page: number,
    totalPages: number,
    loading: boolean,
    error: Error | null,
    loadPage: (newPage: number) => Promise<void>,
    handleDelete: (itemId: string) => Promise<void>
}

function usePaginated<TResponse>(dataSource: PaginatedDataSource<TResponse>, deleteItem: (id: string) => Promise<void>, pageSize: number = 5)
    : UsePaginatedResult<TResponse>
{
    const [items, setItems] = useState<TResponse[]>([])
    const [page, setPage] = useState(1)
    const [totalCount, setTotalCount] = useState(0)
    const [loading, setLoading] = useState(false)
    const [error, setError] = useState<Error | null>(null)

    const loadPage = useCallback(async (newPage: number) => {
        setLoading(true)
        setError(null)
        try {
            const result = await dataSource.getItems(newPage, pageSize)
            setItems(result.items)
            setPage(newPage)
            setTotalCount(result.totalCount)
        } catch (err) {
            setError(err instanceof Error ? err : new Error("Unknown error"))
        } finally {
            setLoading(false)
        }
    }, [dataSource, pageSize])

    const handleDelete = useCallback(async (itemId: string) => {
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
    }, [deleteItem, loadPage, page, pageSize, totalCount])
    
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