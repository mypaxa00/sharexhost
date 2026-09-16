import { useState } from "react"
import './App.css'
import {useAuth, type IUserData, UserRole } from "./hooks/useAuth.ts";
import FileUpload from "./components/FileUpload.tsx";
import CreateLink from "./components/CreateLink.tsx";
import AdminUserCreate from "./components/AdminUserCreate.tsx";

function App() {
    const {user, initialized, login, logout} = useAuth();
    return (
        <div className="app">
            <h1 className="app-title">Hello ShareXHost!</h1>
            {!initialized && <p>Loading...</p>}
            {initialized && <Welcome user={user} onLogin={login} onLogout={logout}/>}
            <div className="tools">
                {user?.role === UserRole.Admin && <AdminUserCreate />}
                <FileUpload />
                <CreateLink />
            </div>
        </div>
    )
}

function Welcome({user, onLogin, onLogout}: {user: IUserData | null, onLogin: (login: string, password: string) => Promise<void>, onLogout: () => void}) {
    const [login, setLogin] = useState('')
    const [password, setPassword] = useState('')
    const [loginError, setLoginError] = useState<string | null>(null)
    const [loggingIn, setLoggingIn] = useState(false)
    
    const loginButtonDisabled = loggingIn || !login || !password

    async function handleLogin() {
        setLoginError(null)
        setLoggingIn(true)
        try {
            await onLogin(login, password)
        } catch {
            setLoginError("Failed to log in. Try again later.")
        } finally {
            setLoggingIn(false)
        }
    }
    
    return user != null
        ? (<>
            <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center'}}>
                <p>Welcome, {user.name}!</p>
                <sub>Role: {user.role}</sub>
            </div>
            <button className="button-accent" onClick={() => {
                const jwt = localStorage.getItem('jwt')
                if (jwt) navigator.clipboard.writeText(jwt)
            }}>Copy ShareX Token</button>
            <button className="button-danger" onClick={onLogout}>Logout</button>
        </>)
        : (<>
            <p>You are not logged in. Uploads will be anonymous.</p>
            <input
                type="text"
                placeholder="Enter your login"
                value={login}
                onChange={(e) => setLogin(e.target.value)}
            />
            <input
                type="password"
                placeholder="Enter your password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
            />
            <button onClick={handleLogin} disabled={loginButtonDisabled}>{loggingIn ? "Logging in..." : "Login"}</button>
            {loginError && <p className="error">{loginError}</p>}
        </>)
}

export default App
