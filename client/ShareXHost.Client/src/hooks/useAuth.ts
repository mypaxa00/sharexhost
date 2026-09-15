import {useEffect, useState } from "react"

function useAuth() {
    const [initialized, setInitialized] = useState(false)
    const [user, setUser] = useState<string | null>(null)

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

        const userData = await response.json()
        setUser(userData.name)
    }


    async function onLogin(login: string) {
        setUser(null)
        const response = await fetch(`/dev-token/${login}`)
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

export default useAuth