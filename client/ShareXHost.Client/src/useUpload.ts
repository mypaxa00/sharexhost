export class UploadResponse {
    success: boolean;
    url: string | null;
    deletionUrl: string | null;
    error: string | null;
}

function useUpload(){
    async function uploadFile(formData: FormData) : Promise<UploadResponse> {
        const jwt = localStorage.getItem('jwt')
        const tokenResponse = await fetch('/antiforgery/token', {
            method: 'GET',
            headers: {
                'Authorization': `Bearer ${jwt}`
            }
        });
        if (!tokenResponse.ok) {
            console.error('Failed to get anti-forgery token status:', tokenResponse.status)
            return {
                success: false,
                url: null,
                deletionUrl: null,
                error: 'Failed to get anti-forgery token'
            }
        }
        const token = await tokenResponse.json()

        const response = await fetch('/files', {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${jwt}`,
                'X-XSRF-TOKEN': token.requestToken
            },
            body: formData
        })
        if (!response.ok) {
            console.error('File upload failed status:', response.status)
            return {
                success: false,
                url: null,
                deletionUrl: null,
                error: 'File upload failed: ' + (response.status)
            }
        }

        const result = await response.json() as UploadResponse;
        result.success = true
        console.log('File uploaded successfully:', result)

        return result
    }
    
    return uploadFile
}

export default useUpload