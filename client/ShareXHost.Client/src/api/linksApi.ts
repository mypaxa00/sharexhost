import { getAuthHeaders } from "../authHeaders.ts"
import {ApiError} from "../dto/ApiError.ts";

export interface CreateLinkResponse {
    url: string;
    deletionUrl: string;
}

export async function createLink(url: string) : Promise<CreateLinkResponse> {
    const response = await fetch('/links', {
        method: 'POST',
        headers: {...getAuthHeaders(), 'Content-Type': 'application/json'},
        body: JSON.stringify({ url })
    })
    if (!response.ok) {
        throw new ApiError('Link creation failed', response.status)
    }

    return await response.json() as CreateLinkResponse;
}
    
export async function deleteLink(url: string) : Promise<void> {
    const response = await fetch(url, {
        method: 'DELETE',
        headers: getAuthHeaders(),
    })
    if (!response.ok) {
        throw new ApiError('Link deletion failed', response.status)
    }
}