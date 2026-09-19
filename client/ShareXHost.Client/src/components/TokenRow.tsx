import { useState } from "react"
import type {ApiTokenResponse} from "../api/tokensApi.ts";
import {formatDate} from "../utils/formatDate.ts";

interface TokenRowProps {
    token: ApiTokenResponse
    onDelete: () => Promise<void>
}

function TokenRow({ token, onDelete }: TokenRowProps) {
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
            <td>{token.name}</td>
            <td>{formatDate(token.createdAt)}</td>
            <td>
                <button className="button-danger" onClick={handleDelete} disabled={deleting}>Delete</button>
                {deleteError && <p className="error">{deleteError}</p>}
            </td>
        </tr>
    )
}

export default TokenRow
