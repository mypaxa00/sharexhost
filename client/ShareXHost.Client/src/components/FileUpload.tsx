import { useState } from "react"
import { useUpload, UploadError, type UploadResponse } from "../hooks/useUpload.ts";
import CreatedUrl from "./CreatedUrl.tsx"

function FileUpload() {
    const {uploadFile, deleteFile} = useUpload()
    const [file, setFile] = useState<File | null>(null)
    const fileSelected = file !== null

    const [uploading, setUploading] = useState(false)
    const [uploadResult, setUploadResult] = useState<UploadResponse | null>(null)
    const [uploadError, setUploadError] = useState<string | null>(null)

    async function onUpload() {
        if (!file) return;

        const formData = new FormData()
        formData.append('file', file)

        setUploadError(null)
        setUploading(true)
        try {
            const result = await uploadFile(formData)
            setFile(null)
            setUploadResult(result)
        } catch (error) {
            if (error instanceof UploadError) {
                if (error.status === 413) {
                    setUploadError('File upload failed: File is too large.')
                } else if (error.status === 429) {
                    setUploadError('File upload failed: Too many requests. Please try again later.')
                } else {
                    setUploadError(`File upload failed with status ${error.status}`)
                }
            } else {
                setUploadError(`File upload failed due to an unknown error: ${error}`)
            }
        } finally {
            setUploading(false)
        }
    }

    async function onDelete() {
        try {
            await deleteFile(uploadResult!.deletionUrl)
            setUploadResult(null)
        } catch (error) {
            if (error instanceof UploadError && error.status === 403) {
                throw new Error("You don't have permission to delete this file.")
            } else {
                throw new Error("Unknown error! Try again later.")
            }
        }
    }

    return (
        <section className="tool">
            <h2>File Upload</h2>
            <input className="file-input" type="file" onChange={(e) => {
                setFile(e.target.files ? e.target.files[0] : null)
                setUploadResult(null)
                setUploadError(null)
            }} />
            {fileSelected && <p>Selected file: {file.name} Size: ({file.size} bytes Type: {file.type})</p>}
            {fileSelected && <button className="button-accent" onClick={onUpload} disabled={uploading}>Upload</button>}
            {uploading && <p>Uploading...</p>}
            {uploadError && <p className="error">{uploadError}</p>}
            {!uploading && uploadResult && <CreatedUrl name="File URL:" url={uploadResult.url} onDelete={onDelete}/>}
        </section>
    )
}

export default FileUpload;