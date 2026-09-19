import { useState } from "react"
import { type UserData } from "../hooks/useAuth.ts";

interface WelcomeProps {
    user: UserData | null
    onLogin: (login: string, password: string) => Promise<void>
    onLogout: () => void
}

function Welcome({user, onLogin, onLogout}: WelcomeProps) {
    const [login, setLogin] = useState('')
    const [password, setPassword] = useState('')
    const [loginError, setLoginError] = useState<string | null>(null)
    const [loggingIn, setLoggingIn] = useState(false)

    const loginButtonDisabled = loggingIn || !login.trim() || !password

    async function handleLogin() {
        setLoginError(null)
        setLoggingIn(true)
        try {
            await onLogin(login.trim(), password)
        } catch {
            setLoginError("Failed to log in. Try again later.")
        } finally {
            setLoggingIn(false)
        }
    }

    return user != null
        ? (<>
            <div className="welcome-user">
                <p>Welcome, {user.name}!</p>
                <sub>Role: {user.role}</sub>
            </div>
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

export default Welcome