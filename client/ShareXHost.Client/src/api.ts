export function getAuthHeaders(): HeadersInit {
    const jwt = localStorage.getItem('jwt')

    return jwt
        ? { Authorization: `Bearer ${jwt}` }
        : {}
}