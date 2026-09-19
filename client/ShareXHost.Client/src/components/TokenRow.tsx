import { useState } from "react"
import type {ApiTokenResponse} from "../api/tokensApi.ts";

function TokenRow({ token, onDelete }: { token: ApiTokenResponse, onDelete: () => Promise<void> }) {
    const [deleting, setDeleting] = useState(false)
    const [deleteError, setDeleteError] = useState<string | null>(null)

    async function handleDelete() {
        setDeleteError(null)
        setDeleting(true)
        try {
            await onDelete()
        } catch (error) {
            if (error instanceof Error) {
                setDeleteError(error.message)
            } else {
                setDeleteError("Unknown error! Try again later.")
            }
        } finally {
            setDeleting(false)
        }
    }

    return (
        <tr>
            <td>
                <p>{token.name}</p>
            </td>
            <td>{new Date(token.createdAt).toLocaleString()}</td>
            <td>
                <button className="button-danger" onClick={handleDelete} disabled={deleting}>Delete</button>
                {deleteError && <p className="error">{deleteError}</p>}
            </td>
        </tr>
    )
}

export default TokenRow
