import {useEffect, useState } from "react"

function useAuth() {
    const [loading, setLoading] = useState(true)
    const [user, setUser] = useState<string | null>(null)

    async function loadUser() {
        const jwt = localStorage.getItem('jwt')
        if (!jwt) {
            setLoading(false)
            return
        }
        const response = await fetch('/me', {
            headers: {
                Authorization: `Bearer ${jwt}`
            }
        })

        if (!response.ok) {
            console.error('Failed to load user data, status:', response.status)
            localStorage.removeItem('jwt')
            setLoading(false)
            return
        }

        const userData = await response.json()
        setUser(userData.name)
        setLoading(false)
    }


    async function onLogin(login: string) {
        setLoading(true)
        const response = await fetch(`/dev-token/${login}`)

        if (!response.ok) {
            console.error('Failed to get JWT token, for login:', login, ' status:', response.status)
            setLoading(false)
            return
        }

        const tokenData = await response.json()
        localStorage.setItem('jwt', tokenData.token)
        await loadUser()
    }

    function onLogout() {
        localStorage.removeItem('jwt')
        setUser(null)
    }

    useEffect(() => { loadUser() }, [])
    
    return {user, loading, login: onLogin, logout: onLogout}
}

export default useAuth