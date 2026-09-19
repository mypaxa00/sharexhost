import {useEffect, useState } from "react"
import type {UserRole} from "../api/userApi.ts";

export interface UserData {
    name: string
    role: UserRole
}

export function useAuth() {
    const [initialized, setInitialized] = useState(false)
    const [user, setUser] = useState<UserData | null>(null)

    async function loadUser() {
        const jwt = localStorage.getItem('jwt')
        if (!jwt) return
        
        const response = await fetch('/me', {
            headers: {
                Authorization: `Bearer ${jwt}`
            }
        })

        if (response.status === 401) {
            localStorage.removeItem('jwt')
            setUser(null)
            return
        }

        if (!response.ok) {
            return
        }

        const userData = await response.json() as UserData
        setUser(userData)
    }


    async function onLogin(login: string, password: string) {
        setUser(null)
        const response = await fetch(`/auth/login`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ name: login, password: password })
        })
        if (!response.ok) {
            throw new Error('Failed to get JWT token.')
        }

        const tokenData = await response.json()
        localStorage.setItem('jwt', tokenData.token)
        
        await loadUser()
    }

    function onLogout() {
        localStorage.removeItem('jwt')
        setUser(null)
    }

    useEffect(() => {
        loadUser().finally(() => setInitialized(true))
    }, [])
    
    return {user, initialized, login: onLogin, logout: onLogout}
}