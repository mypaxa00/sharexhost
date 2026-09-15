import { useState } from "react"
import './App.css'
import useAuth from "./hooks/useAuth.ts";
import FileUpload from "./components/FileUpload.tsx";
import CreateLink from "./components/CreateLink.tsx";

function App() {
    const {user, initialized, login, logout} = useAuth();
    return (
        <div className="app">
            <h1 className="app-title">Hello ShareXHost!</h1>
            {!initialized && <p>Loading...</p>}
            {initialized && <Welcome user={user} onLogin={login} onLogout={logout}/>}
            <div className="tools">
                <FileUpload />
                <CreateLink />
            </div>
        </div>
    )
}

function Welcome({user, onLogin, onLogout}: {user: string | null, onLogin: (login: string) => Promise<void>, onLogout: () => void}) {
    const [login, setLogin] = useState('')
    const [loginError, setLoginError] = useState<string | null>(null)
    const [loggingIn, setLoggingIn] = useState(false)

    async function handleLogin() {
        setLoginError(null)
        setLoggingIn(true)
        try {
            await onLogin(login)
        } catch (error) {
            console.error("LOGIN ERROR:", error)
            setLoginError("Failed to log in. Try again later.")
        } finally {
            setLoggingIn(false)
        }
    }
    
    return user != null
        ? (<>
            <p>Welcome, {user}!</p>
            <button onClick={onLogout}>Logout</button>
        </>)
        : (<>
            <p>You are not logged in. Uploads will be anonymous.</p>
            <input
                type="text"
                placeholder="Enter your login"
                value={login}
                onChange={(e) => setLogin(e.target.value)}
            />
            <button onClick={handleLogin} disabled={loggingIn}>{loggingIn ? "Logging in..." : "Login"}</button>
            {loginError && <p className="error">{loginError}</p>}
        </>)
}

export default App
