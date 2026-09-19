import {getAuthHeaders} from "../authHeaders.ts"
import {ApiError} from "../dto/ApiError.ts";

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
        throw new ApiError('File upload failed', response.status)
    }
    return await response.json() as UploadFileResponse
}
    
export async function deleteFile(url: string) : Promise<void> {
    const response = await fetch(url, {
        method: 'DELETE',
        headers: getAuthHeaders(),
    })
    if (!response.ok) {
        throw new ApiError('File deletion failed', response.status)
    }
}