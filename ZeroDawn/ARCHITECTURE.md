# ARCHITECTURE

## Solution Diagram

```text
Q:\Work\ZeroDawn\ZeroDawn.slnx
|
+-- Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Shared
|   Shared Razor Class Library
|   DTOs, layouts, pages, components, localization, service contracts
|
+-- Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Web
|   ASP.NET Core host
|   Controllers, Identity, EF Core, middleware, diagnostics, publishing host
|
+-- Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Web.Client
|   Blazor WebAssembly client services
|   Browser token storage, auth handler, named HttpClients, runtime config
|
\-- Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn
    .NET MAUI Blazor Hybrid host
    Secure storage, auth handler, connectivity, platform-specific API URLs
```

## Project Responsibilities

### `ZeroDawn.Shared`

Owns:

- shared contracts
- validation rules
- shared layouts and components
- shared pages
- localization resources
- service interfaces and shared UX services
- design-system CSS and static shared assets

Key folders:

- [Contracts](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Shared\Contracts)
- [Core](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Shared\Core)
- [Components](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Shared\Components)
- [Layout](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Shared\Layout)
- [Pages](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Shared\Pages)
- [Localization](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Shared\Localization)
- [Services](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Shared\Services)
- [wwwroot](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Shared\wwwroot)

### `ZeroDawn.Web`

Owns:

- ASP.NET Core startup and DI
- Identity and EF Core
- JWT auth and authorization
- email sending
- logging, middleware, rate limiting, health checks
- migrations and DB seeding
- API controllers
- published web host for the interactive app

Key folders:

- [Configuration](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Web\Configuration)
- [Controllers](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Web\Controllers)
- [Data](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Web\Data)
- [Middleware](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Web\Middleware)
- [Services](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Web\Services)
- [Migrations](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Web\Migrations)
- [Components](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Web\Components)

### `ZeroDawn.Web.Client`

Owns:

- browser-only auth infrastructure
- localStorage-backed token storage
- browser preferences and connectivity helpers
- `AuthHttpHandler`
- named clients `ApiAuthenticated` and `ApiNoAuth`
- browser runtime config through `wwwroot/appsettings*.json`

Key folders:

- [Services](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Web.Client\Services)
- [wwwroot](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Web.Client\wwwroot)

### `ZeroDawn`

Owns:

- MAUI host startup
- SecureStorage-backed token storage
- `MauiAuthStateProvider`
- `MauiAuthHttpHandler`
- MAUI preferences, connectivity, and secure storage
- platform-specific API base URLs
- Android dev HTTPS helper

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
  Components/
  Configuration/
  Controllers/
  Data/
  Middleware/
  Migrations/
  Services/
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

## UI Shell Flow

Main authenticated shell:

```text
MainLayout
  -> Sidebar
  -> TopBar
     -> ThemeToggle
     -> culture switcher
     -> logout action
  -> OfflineBanner
  -> ToastContainer
  -> PermissionPrompt
```

Auth shell:

```text
AuthLayout
  -> OfflineBanner
  -> auth page body
  -> ToastContainer
  -> PermissionPrompt
```

## Data Flow

Typical authenticated browser flow:

```text
Page / component
  -> named HttpClient "ApiAuthenticated"
  -> AuthHttpHandler
  -> API controller
  -> Identity / EF Core / server logic
  -> SQL Server
```

Refresh flow:

```text
401 from API
  -> AuthHttpHandler / MauiAuthHttpHandler
  -> refresh request via named client "ApiNoAuth"
  -> save new tokens
  -> retry original request once
```

Current concrete examples:

- browser auth uses [AuthApiClient.cs](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Web.Client\Services\AuthApiClient.cs)
- MAUI auth uses [MauiAuthHttpHandler.cs](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn\Services\MauiAuthHttpHandler.cs)
- server auth API lives in [AuthController.cs](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Web\Controllers\AuthController.cs)
- DB access is through [ApplicationDbContext.cs](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Web\Data\ApplicationDbContext.cs)
- the admin health page creates an authenticated client and calls `/api/health`

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

Important:

- auth POST operations are intentionally not behind automatic resilience retry
- `Jwt:Secret` controls whether JWT middleware is wired on the server
- `/api/health` requires `SuperAdmin`

## Error And Ops Flow

```text
Unhandled exception
  -> CorrelationIdMiddleware
  -> GlobalExceptionMiddleware
  -> Serilog
  -> ErrorLogs table attempt
  -> safe ApiResponse with ReferenceNumber
```

```text
Health page
  -> authenticated named HttpClient
  -> /api/health
  -> JSON report
  -> diagnostic UI cards and status rows
```

Key files:

- [CorrelationIdMiddleware.cs](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Web\Middleware\CorrelationIdMiddleware.cs)
- [GlobalExceptionMiddleware.cs](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Web\Middleware\GlobalExceptionMiddleware.cs)
- [ErrorLog.cs](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Web\Data\ErrorLog.cs)
- [ErrorDisplay.razor](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Shared\Components\Feedback\ErrorDisplay.razor)
- [HealthCheck.razor](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Shared\Pages\Admin\HealthCheck.razor)

## Shared vs Server-Only

Shared:

- DTOs
- validation
- pages, layouts, and reusable UI
- localization
- service interfaces
- shared UX services such as toast and permission prompt orchestration

Server-only:

- controllers
- EF Core
- Identity
- JWT signing
- SMTP sending
- Serilog SQL sink
- rate limiting and health check endpoint mapping

Host-specific:

- browser storage, preferences, and connectivity
- MAUI storage, preferences, secure storage, and connectivity
- platform-specific API base URLs
- Android dev HTTPS certificate handling

## Risks And Boundaries

- `IUserApiClient` and `IAdminApiClient` exist as shared contracts, but concrete host implementations still need to be completed for full live data flow on all pages
- secrets must never move into `ZeroDawn.Shared` or browser-delivered config
- non-idempotent auth endpoints must not get retry policies automatically
- `App:DefaultLanguage` currently affects server request localization, while WASM and MAUI still default to Arabic in code
- browser deployments must update `ApiBaseUrl`; MAUI deployments must update `MauiProgram.cs`
