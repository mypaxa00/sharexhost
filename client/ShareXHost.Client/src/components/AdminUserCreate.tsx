import {useState} from "react";
import {UserRole, createUser} from "../api/userApi.ts";
import {ApiError} from "../dto/ApiError.ts";

function AdminUserCreate() {
    const [userName, setUserName] = useState('')
    const [password, setPassword] = useState('')
    const [confirmPassword, setConfirmPassword] = useState('')
    const [displayName, setDisplayName] = useState('')
    const [role, setRole] = useState<UserRole>(UserRole.User)
    const [creating, setCreating] = useState(false)
    const [createError, setCreateError] = useState<string | null>(null)
    const [result, setResult] = useState(false)

    const createDisabled =
        !userName.trim() ||
        !password ||
        !confirmPassword ||
        !displayName.trim() ||
        password !== confirmPassword
    
    async function onCreate() {
        setCreating(true)
        setCreateError(null)
        setResult(false)
        try {
            await createUser(userName.trim(), password, displayName.trim(), role)
            setResult(true)
        } catch (error) {
            if (error instanceof ApiError) {
                if (error.status === 400) {
                    setCreateError("Invalid user details.")
                } else if (error.status === 409) {
                    setCreateError("A user with this username already exists.")
                } else if (error.status === 429) {
                    setCreateError("Too many requests. Please try again later.")
                } else {
                    setCreateError("Failed to create user. Please try again later.")
                }
            } else {
                setCreateError("Failed to create user. Please try again later.")
            }
        } finally {
            setCreating(false)
        }
    }

    return (
        <section className="tool">
            <h2>Create User</h2>
            <input className="string-input" type="text" placeholder="Username" value={userName} onChange={(e) => {
                setUserName(e.target.value)
            }} />
            <input className="string-input" type="password" placeholder="Password" value={password} onChange={(e) => {
                setPassword(e.target.value)
            }} />
            <input className="string-input" type="password" placeholder="Confirm Password" value={confirmPassword} onChange={(e) => {
                setConfirmPassword(e.target.value)
            }} />
            <input className="string-input" type="text" placeholder="Display Name" value={displayName} onChange={(e) => {
                setDisplayName(e.target.value)
            }} />
            <select className="string-input" value={role}
                onChange={(e) => setRole(e.target.value as UserRole)}
            >
                <option value={UserRole.User}>User</option>
                <option value={UserRole.Admin}>Admin</option>
            </select>
            <button className="button-accent" onClick={onCreate} disabled={creating || createDisabled}>Create User</button>
            {creating && <p>Creating user...</p>}
            {createError && <p className="error">{createError}</p>}
            {!creating && result && <p>User created successfully</p>}
        </section>
    )
}

export default AdminUserCreate;