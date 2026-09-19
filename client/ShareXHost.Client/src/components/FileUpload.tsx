import { useState } from "react"
import CreatedUrl from "./CreatedUrl.tsx"
import {deleteFile, uploadFile, type UploadFileResponse} from "../api/filesApi.ts";
import {ApiError} from "../dto/ApiError.ts";

function FileUpload() {
    const [file, setFile] = useState<File | null>(null)
    const fileSelected = file !== null

    const [uploading, setUploading] = useState(false)
    const [uploadResult, setUploadResult] = useState<UploadFileResponse | null>(null)
    const [uploadError, setUploadError] = useState<string | null>(null)
    const [deleteError, setDeleteError] = useState<string | null>(null)

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
            if (error instanceof ApiError) {
                if (error.status === 413) {
                    setUploadError('File is too large.')
                } else if (error.status === 429) {
                    setUploadError('Too many requests. Please try again later.')
                } else {
                    setUploadError(`File upload failed. Please try again later.`)
                }
            } else {
                setUploadError(`File upload failed. Please try again later.`)
            }
        } finally {
            setUploading(false)
        }
    }

    async function onDelete() {
        if (!uploadResult) return;
        try {
            await deleteFile(uploadResult.deletionUrl)
            setUploadResult(null)
        } catch (error) {
            if (error instanceof ApiError && error.status === 403) {
                setDeleteError("You don't have permission to delete this file.")
            } else {
                setDeleteError("Unknown error! Try again later.")
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
            {deleteError && <p className="error">{deleteError}</p>}
            {!uploading && uploadResult && <CreatedUrl name="File URL:" url={uploadResult.url} onDelete={onDelete}/>}
        </section>
    )
}

export default FileUpload;