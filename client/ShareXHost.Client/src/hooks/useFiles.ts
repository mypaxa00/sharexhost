import { getAuthHeaders } from "../api"

export interface FileResponse {
    id: string;
    originalFileName: string;
    sizeBytes: number;
    createdAt: string;
}

export interface PaginatedResponse<T> {
    page: number;
    pageSize: number;
    totalCount: number;
    items: T[];
}

export function useFiles() {
    async function getMyFiles(page: number, pageSize: number): Promise<PaginatedResponse<FileResponse>> {
        const response = await fetch(`/files/mine?page=${page}&pageSize=${pageSize}`, {
            method: 'GET',
            headers: getAuthHeaders(),
        })
        if (!response.ok) {
            throw new FileError('Failed to load files', response.status)
        }

        const result = await response.json() as PaginatedResponse<FileResponse>;
        console.log('Files loaded successfully:', result)

        return result
    }

    return { getMyFiles }
}

export class FileError extends Error {
    status: number;

    constructor(message: string, status: number) {
        super(message);
        this.status = status;
    }
}
