import './App.css'
import {useAuth } from "./hooks/useAuth.ts";
import FileUpload from "./components/FileUpload.tsx";
import LinkCreate from "./components/LinkCreate.tsx";
import AdminUserCreate from "./components/AdminUserCreate.tsx";
import MyFiles from "./components/MyFiles.tsx";
import {UserRole} from "./api/userApi.ts";
import MyLinks from "./components/MyLinks.tsx";
import APITokens from "./components/APITokens.tsx";
import Welcome from "./components/Welcome.tsx";

function App() {
    const {user, initialized, login, logout} = useAuth();
    return (
        <div className="app">
            <h1 className="app-title">ShareXHost!</h1>
            {!initialized && <p>Loading...</p>}
            {initialized && <Welcome user={user} onLogin={login} onLogout={logout}/>}
            <div className="tools">
                {user?.role === UserRole.Admin && <AdminUserCreate />}
                {user && <APITokens />}
                <FileUpload />
                <LinkCreate />
                {user && <MyFiles />}
                {user && <MyLinks />}
            </div>
        </div>
    )
}

export default App
