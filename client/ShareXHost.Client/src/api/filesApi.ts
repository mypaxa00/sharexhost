import { getAuthHeaders } from "../api"

export interface UploadFileResponse {
    url: string;
    deletionUrl: string;
}

export async function uploadFile(formData: FormData) : Promise<UploadFileResponse> {
    const response = await fetch('/files', {
        method: 'POST',
        headers: getAuthHeaders(),
        body: formData
    })
    if (!response.ok) {
        throw new UploadError('File upload failed', response.status)
    }

    const result = await response.json() as UploadFileResponse;
    console.log('File uploaded successfully:', result)

    return result
}
    
export async function deleteFile(url: string) : Promise<void> {
    const response = await fetch(url, {
        method: 'DELETE',
        headers: getAuthHeaders(),
    })
    if (!response.ok) {
        throw new UploadError('File deletion failed', response.status)
    }

    console.log('File deleted successfully.')
}

export class UploadError extends Error {
    status: number;
    
    constructor(message: string, status: number) {
        super(message);
        this.status = status;
    }
}