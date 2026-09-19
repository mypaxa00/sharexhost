import { getAuthHeaders } from "../api"

export const UserRole = {
    User: "User",
    Admin: "Admin"
} as const

export type UserRole = typeof UserRole[keyof typeof UserRole]

export async function createUser(username: string, password: string, displayName: string, role: UserRole) : Promise<void> {
    const response = await fetch('/admin/users', {

        method: 'POST',
        headers: {...getAuthHeaders(), 'Content-Type': 'application/json'},
        body: JSON.stringify({
            username, password, name: displayName, role
        })
    })
    if (!response.ok) {
        const errorData = await response.json()
        console.log(errorData)
        throw new UserError('User creation failed', response.status)
    }

    console.log('User created successfully');
}

export class UserError extends Error {
    status: number;

    constructor(message: string, status: number) {
        super(message);
        this.status = status;
    }
}