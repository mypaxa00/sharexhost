import type {PaginatedResponse} from "../models/PaginatedResponse.ts";
import {getAuthHeaders} from "../authHeaders.ts";
import {ApiError} from "../dto/ApiError.ts";

export interface PaginatedDataSource<TResponse> {
    getItems(page: number, pageSize: number): Promise<PaginatedResponse<TResponse>>;
}

export function createPaginatedApi<TResponse>(baseUrl: string): PaginatedDataSource<TResponse> {
    async function getItems(page: number, pageSize: number): Promise<PaginatedResponse<TResponse>> {
        const params = new URLSearchParams({
            page: page.toString(),
            pageSize: pageSize.toString(),
        });
        const response = await fetch(`${baseUrl}?${params}`, {
            method: 'GET',
            headers: getAuthHeaders(),
        })
        if (!response.ok) {
            throw new ApiError(`Failed to load items from ${baseUrl}`, response.status)
        }

        return await response.json() as PaginatedResponse<TResponse>
    }

    return { getItems }
}