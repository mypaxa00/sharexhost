import { getAuthHeaders } from "../api"
import type {PaginatedResponse} from "../models/PaginatedResponse.ts";

export interface PaginatedApi<TResponse> {
    getItems(page: number, pageSize: number): Promise<PaginatedResponse<TResponse>>;
}

export function createPaginatedApi<TResponse>(baseUrl: string): PaginatedApi<TResponse> {
    async function getItems(page: number, pageSize: number): Promise<PaginatedResponse<TResponse>> {
        const response = await fetch(`${baseUrl}?page=${page}&pageSize=${pageSize}`, {
            method: 'GET',
            headers: getAuthHeaders(),
        })
        if (!response.ok) {
            throw new PaginationError(`Failed to load items from ${baseUrl}`, response.status)
        }

        const result = await response.json() as PaginatedResponse<TResponse>;
        console.log('Loaded successfully:', result)

        return result
    }

    return { getItems }
}

export class PaginationError extends Error {
    status: number;

    constructor(message: string, status: number)
    {
        super(message);
        this.status = status;
    }
}