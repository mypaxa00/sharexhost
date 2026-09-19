# ShareXHost

A self-hosted file hosting and URL shortening service built with **ASP.NET Core, PostgreSQL, React, and TypeScript**.

ShareXHost provides a simple web interface for uploading files and creating short links, while also exposing an API designed to work with [ShareX](https://getsharex.com/).

The project started as a way to learn full-stack development with .NET and React, and has grown into a small production-style application with authentication, API tokens, persistent storage, rate limiting, Docker deployment, and integration tests.

## Features

* 📁 File uploads and downloads
* 🔗 URL shortening
* 👤 User accounts and authentication
* 🔐 JWT authentication
* 🔑 Personal API tokens for ShareX and other clients
* 🗑️ File and link deletion
* 📄 Paginated file, link, and API-token management
* 👑 Admin user management
* 🚦 Rate limiting
* 🐳 Docker-based deployment
* 🐘 PostgreSQL persistence
* ☁️ Reverse-proxy support through Cloudflare Tunnel
* 🧪 Integration tests covering important API and authorization scenarios

## Tech Stack

### Backend

* **C# / .NET 10**
* **ASP.NET Core**
* **Entity Framework Core**
* **PostgreSQL**
* **JWT Bearer Authentication**
* **ASP.NET Core Rate Limiting**
* **Docker**

### Frontend

* **React**
* **TypeScript**
* **Vite**
* **CSS**

### Testing

* **xUnit**
* **ASP.NET Core integration testing**
* **WebApplicationFactory**

## Architecture

The project follows a relatively simple service-oriented structure.

```text
ShareXHost
├── client/
│   └── ShareXHost.Client/
│       └── React + TypeScript frontend
│
├── src/
│   └── ShareXHost/
│       ├── Authentication/
│       ├── Authorization/
│       ├── Data/
│       │   ├── Dto/
│       │   └── Models/
│       ├── Services/
│       │   ├── Bootstrap/
│       │   ├── FileService/
│       │   ├── LinkService/
│       │   ├── ShortId/
│       │   ├── User/
│       │   └── Token/
│       ├── Storage/
│       └── Program.cs
│
└── tests/
    └── ShareXHost.Tests/
```

The backend keeps HTTP endpoint definitions in Program.cs while moving application logic into services such as FileService, LinkService, and ApiTokenService.

File contents are stored separately from PostgreSQL. The database stores file metadata and the path to the stored file.

## Authentication

ShareXHost supports two authentication mechanisms:

### JWT authentication

The web application uses JWT bearer authentication for normal user sessions.

JWTs contain the user's identity and role and are validated using an application signing key.

### API tokens

API tokens are intended for programmatic access, particularly from ShareX.

Tokens:

* are generated using cryptographically secure random bytes
* are prefixed with `shx_`
* are stored in the database as SHA-256 hashes
* are only shown to the user when they are created
* can be revoked independently

The API distinguishes between JWT authentication and API-token authentication, allowing endpoints to require a normal user session where appropriate.

## File Storage

Uploaded files are stored on the filesystem rather than directly in PostgreSQL.

The application stores metadata such as:

* original filename
* content type
* file size
* storage path
* owner
* creation time

This keeps the database focused on metadata while allowing the actual files to live on persistent storage.

The storage layer is abstracted behind an interface so the application does not need to depend directly on the local filesystem implementation.

## URL Shortening

Short links use six-character alphanumeric IDs.

IDs are generated using a cryptographically secure random number generator rather than a predictable sequence.

The database's unique constraint remains the final authority for uniqueness. If a collision occurs, the application retries the insert with a new ID.

## Security Considerations

Security has been considered as part of the application's design rather than added only after the main functionality was complete.

Some examples include:

* Passwords are stored using ASP.NET Core's password hashing infrastructure.
* API tokens are stored as hashes rather than plaintext.
* JWT issuer, audience, and signing keys are validated.
* Authorization policies distinguish normal users, administrators, and API-token authentication.
* Anonymous file uploads use deletion tokens.
* API endpoints use rate limiting.
* Antiforgery protection is enabled for the application where browser-based CSRF protection is relevant.
* Secrets used in deployment are supplied through configuration or Docker secrets rather than committed credentials.
* The application container runs as a non-root user.

### ⚠️ Warning ⚠️
Bootstrap admin credentials:

Default implementation rotates credentials every startup and logs them to the console. This is intended for development and testing only. In production, a secure bootstrap mechanism should be used.

## API

The API provides endpoints for authentication, files, links, API tokens, and administration.

Some examples:

```text
POST   /auth/login
GET    /me

POST   /files
GET    /files/{id}
DELETE /files/{id}

POST   /links
GET    /s/{shortId}
DELETE /links/{shortId}

POST   /auth/tokens
GET    /auth/tokens
DELETE /auth/tokens/{id}

POST   /admin/users
```

File and link uploads return URLs compatible with the ShareX upload workflow, including deletion URLs where applicable.

## Running Locally

### Prerequisites

* .NET 10 SDK
* Node.js
* PostgreSQL
* Git

Clone the repository:

```bash
git clone https://github.com/mypaxa00/sharexhost.git
cd ShareXHost
```

Restore and build the backend:

```bash
dotnet restore
dotnet build
```

Install frontend dependencies:

```bash
cd client/ShareXHost.Client
npm install
```

Run the frontend:

```bash
npm run dev
```

The backend can be started from the repository root with:

```bash
dotnet run --project src/ShareXHost
```

### Configuration

Development configuration uses ASP.NET Core configuration and User Secrets for values such as the database connection string and JWT signing key.

Production secrets should **not** be committed to the repository.

Example configuration files are provided where appropriate.

## Docker

The application can be built as a single Docker image containing both the ASP.NET Core backend and the compiled React frontend.

Build the image:

```bash
docker build -t sharexhost:1.0 -f src/ShareXHost/Dockerfile .
```

The production deployment uses Docker Compose with:

* ShareXHost
* PostgreSQL
* persistent file storage
* persistent database storage
* Docker secrets

The application is designed to sit behind a reverse proxy or Cloudflare Tunnel in production.

## Testing

Integration tests exercise the application through its HTTP API rather than testing only individual methods.

The test suite covers scenarios including:

* authentication
* invalid and revoked API tokens
* file uploads
* file deletion
* link creation and deletion
* ownership and authorization
* admin-only endpoints
* API-token lifecycle
* pagination
* anonymous uploads
* authenticated uploads

Run the tests with:

```bash
dotnet test
```

## ShareX Configuration:

ShareXHost can be used as a custom upload destination in ShareX.

### Url Shortener
Use the following custom URL shortener configuration:
```
{
    "Version": "21.0.0",
    "Name": "ShareXHostLinks",
    "DestinationType": "URLShortener",
    "RequestMethod": "POST",
    "RequestURL": "{your-host}/links",
    "Headers": {
    "Authorization": "Bearer shx_...acquired-token..."
    },
    "Body": "JSON",
    "Data": "{\"url\":\"{input}\"}",
    "URL": "{json:url}",
    "DeletionURL": "{json:deletionUrl}",
    "ErrorMessage": "{json:error}"
}
```
### File Uploader
Use the following custom file uploader configuration:
```
{
  "Version": "21.0.0",
  "Name": "ShareXHostFiles",
  "DestinationType": "ImageUploader, FileUploader",
  "RequestMethod": "POST",
  "RequestURL": "{your-host}/files",
  "Headers": {
    "Authorization": "Bearer shx_...acquired-token..."
  },
  "Body": "MultipartFormData",
  "FileFormName": "file",
  "URL": "{json:url}",
  "DeletionURL": "{json:deletionUrl}",
  "ErrorMessage": "{json:error}"
}
```

### Getting an API Token
Create a user account through the admin's web interface.
User can then generate an API token from the API Tokens section.

The bootstrap-admin user can be used to create initial admin accounts.

## What I Learned

This project started primarily as a learning exercise, so the development process has been as important as the finished application.

Some of the main things I worked with:

* Designing REST APIs with ASP.NET Core
* Dependency injection and service abstractions
* Entity Framework Core and database migrations
* PostgreSQL
* JWT authentication and authorization policies
* API-token authentication
* Password hashing
* Secure random token generation
* Filesystem-based storage
* React state and hooks
* TypeScript API models
* Generic pagination components
* Docker multi-stage builds
* Docker Compose
* Production configuration and secrets
* Reverse proxies and forwarded headers
* Rate limiting
* Integration testing

One of the main lessons from the project was that getting something working is only the first part. Making the boundaries between the frontend, API, database, authentication, storage, and deployment understandable is what makes the project maintainable.

## Project Status

ShareXHost is a personal project developed as a full-stack learning project.

The core functionality is implemented and deployed, with ongoing improvements focused on code quality, testing, documentation, and production readiness.

## License

[GNU General Public License (GPLv3)](https://www.gnu.org/licenses/gpl-3.0.html)
