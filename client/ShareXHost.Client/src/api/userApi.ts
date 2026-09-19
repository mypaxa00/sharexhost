import { getAuthHeaders } from "../authHeaders.ts"
import {ApiError} from "../dto/ApiError.ts";

export const UserRole = {
    User: "User",
    Admin: "Admin"
} as const

export type UserRole = typeof UserRole[keyof typeof UserRole]

export async function createUser(username: string, password: string, displayName: string, role: UserRole) : Promise<void> {
    const response = await fetch('/admin/users', {

        method: 'POST',
        headers: {
            ...getAuthHeaders(),
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({
            username, password, name: displayName, role
        })
    })
    if (!response.ok) {
        throw new ApiError('User creation failed', response.status)
    }
}