import {getAuthHeaders} from "../authHeaders.ts";
import {ApiError} from "../dto/ApiError.ts";

export interface TokenResponse {
    token: string;
}

export interface ApiTokenResponse {
    id: string;
    name: string;
    createdAt: string;
}

export async function createApiToken(name: string): Promise<TokenResponse>
{
    const response = await fetch('/auth/tokens', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            ...getAuthHeaders()
        },
        body: JSON.stringify({ name })
    });
    if (!response.ok) {
        throw new ApiError(`Failed to create API token.`, response.status);
    }
    return await response.json() as TokenResponse;
}

export async function deleteApiToken(id: string): Promise<void> {
    const response = await fetch(`/auth/tokens/${id}`, {
        method: 'DELETE',
        headers: getAuthHeaders()
    });
    if (!response.ok) {
        throw new ApiError(`Failed to delete API token.`, response.status);
    }
}