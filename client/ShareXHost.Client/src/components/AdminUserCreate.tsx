import { UserRole } from "../hooks/useAuth.ts";
import { useState } from "react";
import {UserError, useUser} from "../hooks/useUser.ts";

function AdminUserCreate() {
    const createUser = useUser()
    const [userName, setUserName] = useState('')
    const [password, setPassword] = useState('')
    const [confirmPassword, setConfirmPassword] = useState('')
    const [displayName, setDisplayName] = useState('')
    const [role, setRole] = useState<UserRole>(UserRole.User)
    const [creating, setCreating] = useState(false)
    const [createError, setCreateError] = useState<string | null>(null)
    const [result, setResult] = useState(false)
    
    const createDisabled = !userName || !password || !confirmPassword || !displayName || password !== confirmPassword
    
    async function onCreate() {
        setCreating(true)
        setCreateError(null)
        setResult(false)
        try {
            await createUser(userName, password, displayName, role)
            setResult(true)
        } catch (error) {
            if (error instanceof UserError) {
                setCreateError(`Error creating user: ${error.message} (status: ${error.status})`)
            } else {
                setCreateError(`Error creating user: ${error}`)
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
            <input className="string-input" type="Text" placeholder="Display Name" value={displayName} onChange={(e) => {
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