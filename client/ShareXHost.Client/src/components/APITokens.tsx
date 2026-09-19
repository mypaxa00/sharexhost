import { useState } from "react";
import { createApiToken } from "../api/tokensApi.ts";
import MyTokens from "./MyTokens.tsx";

function APITokens() {
    const [name, setName] = useState('');
    const [token, setToken] = useState('');
    const [error, setError] = useState<string | null>(null);
    const [creating, setCreating] = useState(false);
    
    const canCreate = name.trim() !== '' && !creating;

    async function handleCreateToken() {
        const tokenName = name.trim();

        if (!tokenName) {
            setError("Token name is required");
            return;
        }

        setToken('');
        setError(null);
        setCreating(true);
        try {
            const response = await createApiToken(tokenName)
            setToken(response.token);
        } catch (error) {
            setError("Failed to create API token");
        } finally {
            setCreating(false);
        }
    }

    async function handleCopyToken() {
        try {
            await navigator.clipboard.writeText(token)
        } catch {
            setError("Failed to copy API token.")
        }
    }
    
    return (
        <section className="tool">
            <h2>API Tokens</h2>
            <p>Give your token a descriptive name.</p>
            <input className="string-input"
                type="text"
                value={name}
                onChange={(e) => {
                    setName(e.target.value)
                    setError(null)
                }}
                placeholder="Token Name"
            />
            <button className="button-accent" onClick={handleCreateToken} disabled={!canCreate}>{creating ? "Creating..." : "Create Token"}</button>
            {token && (
                <div>
                    <p>Token: {token} <button onClick={handleCopyToken}>Copy</button></p>
                    <p className="warning">This token should be copied and stored securely, as it will not be shown again.</p>
                </div>
            )}
            {error && <p className="error">{error}</p>}
            <MyTokens />
        </section>
    );
}

export default APITokens