import { useState } from "react"
import './App.css'
import useAuth from "./useAuth.ts";
import useUpload from "./useUpload.ts";

function App() {
    const {user, loading, login, logout} = useAuth();
    return (
        <>
            <h1>Hello ShareXHost!</h1>
            {loading && <p>Loading...</p>}
            {!loading && <Welcome user={user} onLogin={login} onLogout={logout}/>}
        </>
    )
}

function Welcome({user, onLogin, onLogout}: {user: string | null, onLogin: (login: string) => void, onLogout: () => void}) {
    const [login, setLogin] = useState('')
    return (
        <>
            {user != null ? <p>Welcome, {user}!</p> : <p>Please log in</p>}
            {!user && <input
                type="text"
                placeholder="Enter your login"
                value={login}
                onChange={(e) => setLogin(e.target.value)}
            />}
            {!user && <button onClick={() => onLogin(login)}>Click me</button>}
            {user && <button onClick={onLogout}>Logout</button>}
            {user && <FileUpload />}
        </>
    )
}

function FileUpload() {
    const uploadFile = useUpload()
    const [file, setFile] = useState<File | null>(null)
    const fileSelected = file !== null

    const [uploading, setUploading] = useState(false)
    const [uploadResult, setUploadResult] = useState<string | null>(null)
    
    async function onUpload() {
        if (!file) return;
        
        const formData = new FormData()
        formData.append('file', file)
        
        setUploading(true)
        try {
            const result = await uploadFile(formData)
            if (result.success) {
                setFile(null)
                setUploadResult(`File uploaded successfully: ${result.url}, Deletion URL: ${result.deletionUrl}`)
            } else {
                setUploadResult(`File upload failed: ${result.error}`)
            }
        } catch (error) {
            console.error('Error uploading file:', error)
            setUploadResult('File upload failed due to an error')
        } finally {
            setUploading(false)
        }
    }

    return (
        <>
            <h2>File Upload</h2>
            <input type="file" onChange={(e) => {
                setFile(e.target.files ? e.target.files[0] : null)
                setUploadResult(null)
            }} />
            {fileSelected && <p>Selected file: {file.name} Size: ({file.size} bytes Type: {file.type})</p>}
            {fileSelected && <button onClick={onUpload} disabled={uploading}>Upload</button>}
            {uploading && <p>Uploading...</p>}
            {!uploading && uploadResult && <p>{uploadResult}</p>}
        </>
    )
}

export default App
