# ARCHITECTURE

## Solution Diagram

```text
Q:\Work\ZeroDawn\ZeroDawn.slnx
|
+-- Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Shared
|   Shared Razor Class Library
|   DTOs, layouts, components, pages, localization, service contracts
|
+-- Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Web
|   ASP.NET Core host
|   Controllers, Identity, EF Core, middleware, email, JWT, Serilog
|
+-- Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Web.Client
|   Blazor WebAssembly client services
|   Browser token storage, auth handler, typed auth API client
|
\-- Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn
    .NET MAUI Blazor Hybrid host
    Secure storage, auth handler, preferences, connectivity
```

## Project Responsibilities

### `ZeroDawn.Shared`

Owns:

- shared contracts
- validation rules
- shared layouts and components
- localization resources
- shared pages
- service interfaces

Key folders:

- [Contracts](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Shared\Contracts)
- [Core](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Shared\Core)
- [Components](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Shared\Components)
- [Layout](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Shared\Layout)
- [Pages](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Shared\Pages)
- [Localization](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Shared\Localization)
- [Services](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Shared\Services)

### `ZeroDawn.Web`

Owns:

- ASP.NET Core startup and DI
- Identity and EF Core
- JWT auth
- email sending
- logging and middleware
- migrations and DB seeding
- API controllers

Key folders:

- [Configuration](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Web\Configuration)
- [Controllers](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Web\Controllers)
- [Data](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Web\Data)
- [Middleware](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Web\Middleware)
- [Services](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Web\Services)
- [Migrations](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Web\Migrations)

### `ZeroDawn.Web.Client`

Owns:

- browser-only auth infra
- localStorage-backed token storage
- browser preferences
- typed auth API client

Key folders:

- [Services](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Web.Client\Services)
- [wwwroot](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Web.Client\wwwroot)

### `ZeroDawn`

Owns:

- MAUI host startup
- SecureStorage-backed token storage
- MAUI auth handler/state provider
- MAUI preferences/secure storage/connectivity

Key folders:

- [Services](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn\Services)
- [Platforms](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn\Platforms)
- [Resources](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn\Resources)

## Folder Structure

### Shared

```text
ZeroDawn.Shared/
  Components/
    Common/
    Feedback/
    Forms/
    Layout/
  Contracts/
    Auth/
    Common/
    Users/
    Validation/
  Core/
    Constants/
  Layout/
  Localization/
    Resources/
  Pages/
    Admin/
    Auth/
    Dashboards/
    Profile/
    Static/
  Services/
  wwwroot/
```

### Web

```text
ZeroDawn.Web/
  Configuration/
  Controllers/
  Data/
  Middleware/
  Migrations/
  Services/
  Components/
  Program.cs
```

### Web.Client

```text
ZeroDawn.Web.Client/
  Services/
  wwwroot/
  Program.cs
```

### MAUI

```text
ZeroDawn/
  Platforms/
  Resources/
  Services/
  MauiProgram.cs
```

## Data Flow

Ideal app flow:

```text
Page / component
  -> typed HttpClient
  -> auth handler
  -> API controller
  -> Identity / EF Core / server service logic
  -> SQL Server
```

Current concrete examples:

- browser auth calls use [AuthApiClient.cs](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Web.Client\Services\AuthApiClient.cs)
- server auth API lives in [AuthController.cs](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Web\Controllers\AuthController.cs)
- DB access is through [ApplicationDbContext.cs](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Web\Data\ApplicationDbContext.cs)

## Auth Flow

```text
Login page
  -> POST /api/auth/login
  -> access token + refresh token
  -> store tokens
     - WASM: localStorage
     - MAUI: SecureStorage
  -> auth state provider parses JWT
  -> role-based UI and route access
```

Refresh flow:

```text
401 from API
  -> AuthHttpHandler / MauiAuthHttpHandler
  -> one refresh attempt using no-auth HttpClient
  -> save new tokens
  -> retry original request once
```

Important:

- Auth POST operations are intentionally not behind automatic resilience retry

## Error Flow

```text
Unhandled exception
  -> CorrelationIdMiddleware
  -> GlobalExceptionMiddleware
  -> Serilog
  -> ErrorLogs table attempt
  -> safe ApiResponse with ReferenceNumber
```

Key files:

- [CorrelationIdMiddleware.cs](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Web\Middleware\CorrelationIdMiddleware.cs)
- [GlobalExceptionMiddleware.cs](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Web\Middleware\GlobalExceptionMiddleware.cs)
- [ErrorLog.cs](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Web\Data\ErrorLog.cs)
- [ErrorDisplay.razor](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Shared\Components\Feedback\ErrorDisplay.razor)

## Shared vs Server-Only

Shared:

- DTOs
- validation
- pages/layouts/components
- localization
- service interfaces

Server-only:

- controllers
- EF Core
- Identity
- JWT signing
- SMTP sending
- Serilog SQL sink

Host-specific:

- browser storage/preferences/connectivity
- MAUI storage/preferences/connectivity
- platform-specific API base URLs

## Risks And Boundaries

- `IUserApiClient` and `IAdminApiClient` exist as shared contracts, but concrete host implementations still need to be completed for full live data flow on all pages
- Secrets must never move into `ZeroDawn.Shared` or client config
- Non-idempotent auth endpoints must not get retry policies automatically
