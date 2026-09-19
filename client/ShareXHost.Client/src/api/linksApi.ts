import { getAuthHeaders } from "../api"

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
        const errorData = await response.json()
        console.log(errorData)
        throw new LinkError('Link upload failed', response.status)
    }

    const result = await response.json() as CreateLinkResponse;
    console.log('Link created successfully:', result)

    return result
}
    
export async function deleteLink(url: string) : Promise<void> {
    const response = await fetch(url, {
        method: 'DELETE',
        headers: getAuthHeaders(),
    })
    if (!response.ok) {
        throw new LinkError('Link deletion failed', response.status)
    }

    console.log('Link deleted successfully.')
}

export class LinkError extends Error {
    status: number;

    constructor(message: string, status: number) {
        super(message);
        this.status = status;
    }
}