import { getAuthHeaders } from "../api"

export interface LinkResponse {
    url: string;
    deletionUrl: string;
}

export function useLinks(){
    async function createLink(url: string) : Promise<LinkResponse> {
        const response = await fetch('/links', {
            method: 'POST',
            headers: {...getAuthHeaders(), 'Content-Type': 'application/json'},
            body: JSON.stringify({ url })
        })
        if (!response.ok) {
            const errorData = await response.json()
            console.log(errorData)
            throw new LinkError('Link upload failed', response.status)
        }

        const result = await response.json() as LinkResponse;
        console.log('Link created successfully:', result)

        return result
    }
    
    async function deleteLink(url: string) : Promise<void> {
        const response = await fetch(url, {
            method: 'DELETE',
            headers: getAuthHeaders(),
        })
        if (!response.ok) {
            throw new LinkError('Link deletion failed', response.status)
        }

        console.log('Link deleted successfully.')
    }

    return { createLink, deleteLink }
}

export class LinkError extends Error {
    status: number;

    constructor(message: string, status: number) {
        super(message);
        this.status = status;
    }
}