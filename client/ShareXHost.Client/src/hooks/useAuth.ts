import {useEffect, useState } from "react"
import type {UserRole} from "../api/userApi.ts";

export interface IUserData {
    name: string
    role: UserRole
}

export function useAuth() {
    const [initialized, setInitialized] = useState(false)
    const [user, setUser] = useState<IUserData | null>(null)

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
            console.error('Failed to load user data, status:', response.status)
            return
        }

        const userData = await response.json() as IUserData
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
            console.error('Failed to get JWT token, for login:', login, ' status:', response.status)
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

    async function initialLoadAttempt() {
        setInitialized(false)
        try {
            await loadUser()
        } finally {
            setInitialized(true)
        }
    }
    useEffect(() => { initialLoadAttempt() }, [])
    
    return {user, initialized, login: onLogin, logout: onLogout}
}