# AI_HANDOFF

## Project Context

This is a .NET 10 solution with four active projects:

- [ZeroDawn.Shared](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Shared\ZeroDawn.Shared.csproj)
- [ZeroDawn.Web](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Web\ZeroDawn.Web.csproj)
- [ZeroDawn.Web.Client](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Web.Client\ZeroDawn.Web.Client.csproj)
- [ZeroDawn](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn\ZeroDawn.csproj)

Solution file:

- [ZeroDawn.slnx](Q:\Work\ZeroDawn\ZeroDawn.slnx)

Supporting repo docs:

- [DESCRIPTION.md](Q:\Work\ZeroDawn\DESCRIPTION.md)
- [ReadMeFirst.md](Q:\Work\ZeroDawn\ReadMeFirst.md)
- [ARCHITECTURE.md](Q:\Work\ZeroDawn\ZeroDawn\ARCHITECTURE.md)
- [ENVIRONMENT.md](Q:\Work\ZeroDawn\ZeroDawn\ENVIRONMENT.md)
- [PUBLISHING.md](Q:\Work\ZeroDawn\ZeroDawn\PUBLISHING.md)
- [RUNNING.md](Q:\Work\ZeroDawn\ZeroDawn\RUNNING.md)

## Technology Stack

- .NET 10
- Blazor Web App with interactive WebAssembly render mode
- .NET MAUI Blazor Hybrid
- ASP.NET Core controller APIs
- ASP.NET Core Identity
- EF Core SQL Server
- JWT auth with refresh tokens
- MailKit SMTP
- Serilog with SQL sink
- built-in localization with `.resx`
- rate limiting and health checks
- response compression and static asset mapping

## Key Files

Startup and host wiring:

- [ZeroDawn.Web/Program.cs](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Web\Program.cs)
- [ZeroDawn.Web.Client/Program.cs](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Web.Client\Program.cs)
- [ZeroDawn/MauiProgram.cs](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn\MauiProgram.cs)

Shell, layouts, and shared UI:

- [MainLayout.razor](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Shared\Layout\MainLayout.razor)
- [AuthLayout.razor](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Shared\Layout\AuthLayout.razor)
- [TopBar.razor](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Shared\Components\Layout\TopBar.razor)
- [ThemeToggle.razor](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Shared\Components\Layout\ThemeToggle.razor)
- [PermissionPrompt.razor](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Shared\Components\Feedback\PermissionPrompt.razor)
- [Routes.razor](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Shared\Routes.razor)
- [app.css](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Shared\wwwroot\app.css)

Server auth, data, and diagnostics:

- [AuthController.cs](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Web\Controllers\AuthController.cs)
- [UsersController.cs](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Web\Controllers\UsersController.cs)
- [AdminController.cs](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Web\Controllers\AdminController.cs)
- [ApplicationDbContext.cs](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Web\Data\ApplicationDbContext.cs)
- [ApplicationUser.cs](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Web\Data\ApplicationUser.cs)
- [DatabaseSeeder.cs](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Web\Data\DatabaseSeeder.cs)
- [HealthCheck.razor](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Shared\Pages\Admin\HealthCheck.razor)

Client-side auth plumbing:

- [AuthHttpHandler.cs](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Web.Client\Services\AuthHttpHandler.cs)
- [MauiAuthHttpHandler.cs](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn\Services\MauiAuthHttpHandler.cs)
- [PermissionPromptService.cs](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Shared\Services\PermissionPromptService.cs)

Localization:

- [SharedResources.cs](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Shared\Localization\SharedResources.cs)
- [SharedResources.resx](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Shared\Localization\Resources\SharedResources.resx)
- [SharedResources.ar.resx](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Shared\Localization\Resources\SharedResources.ar.resx)

## Current Behavior Notes

- `ZeroDawn.Web` is the published web host. It serves the server APIs, Razor components, and the WebAssembly client assets.
- `ZeroDawn.Web.Client` reads `ApiBaseUrl` from static client config and registers named clients `ApiAuthenticated` and `ApiNoAuth`.
- `ZeroDawn` currently uses platform-specific API base URLs hardcoded in [MauiProgram.cs](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn\MauiProgram.cs).
- `App:BaseUrl` is used by the server host for generated links and for the server-side `IAuthApiClient`.
- `ApiBaseUrl` is a separate browser setting and must be updated before web publishing if the deployment URL changes.
- Server request localization honors `App:DefaultLanguage`, but the WASM client and MAUI host currently default to Arabic directly in code.
- The shared shell now includes `TopBar`, `ThemeToggle`, `ToastContainer`, `OfflineBanner`, and `PermissionPrompt`.
- The admin health page calls `/api/health` through an authenticated named `HttpClient`, and the endpoint is restricted to `SuperAdmin`.

## Coding Conventions

- file-scoped namespaces
- `#nullable enable`
- DataAnnotations for validation
- shared contracts go in `ZeroDawn.Shared.Contracts`
- reusable UI goes in `ZeroDawn.Shared.Components`
- pages go in `ZeroDawn.Shared.Pages`
- host-specific storage/connectivity stays in host projects
- use `ApiResponse` / `ApiResponse<T>` consistently
- avoid N+1 queries in EF Core
- keep secrets out of shared and browser-delivered config

## Common Tasks

### Add a new page

1. Create it under [ZeroDawn.Shared/Pages](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Shared\Pages)
2. Add `@page`
3. Use `@layout AuthLayout` for auth pages or `@layout MainLayout` for app pages
4. Reuse shared components from [Components](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Shared\Components)
5. Inject `IStringLocalizer<SharedResources>` for visible text
6. Use `ErrorDisplay`, `EmptyState`, `ToastService`, and `PermissionPromptService` when the UX needs them
7. Build the solution

### Add a new API endpoint

1. Put the DTO in the right folder under [ZeroDawn.Shared/Contracts](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Shared\Contracts)
2. Add or update endpoint constants in [ApiEndpoints.cs](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Shared\Core\ApiEndpoints.cs) if needed
3. Implement the endpoint in a controller under [ZeroDawn.Web/Controllers](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Web\Controllers)
4. Return `ApiResponse` or `ApiResponse<T>`
5. Add authorization explicitly
6. Use transactions for multi-entity writes when required
7. Build and test

### Add a new shared component

1. Create the `.razor` in [ZeroDawn.Shared/Components](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Shared\Components)
2. Create a matching `.razor.css`
3. Use design-system variables from [app.css](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Shared\wwwroot\app.css)
4. Keep RTL behavior in mind for mixed Arabic and English UI
5. Prefer parameters and `EventCallback`

### Add or change runtime configuration

1. Server-only settings belong in [ZeroDawn.Web/appsettings.json](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Web\appsettings.json) plus secrets or environment overrides
2. Browser runtime settings belong in [ZeroDawn.Web.Client/wwwroot/appsettings.json](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Web.Client\wwwroot\appsettings.json)
3. MAUI API endpoints currently require code changes in [MauiProgram.cs](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn\MauiProgram.cs)
4. Update [ENVIRONMENT.md](Q:\Work\ZeroDawn\ZeroDawn\ENVIRONMENT.md) and [PUBLISHING.md](Q:\Work\ZeroDawn\ZeroDawn\PUBLISHING.md) when behavior changes

## What Not To Do

- Do not put secrets in browser config files
- Do not put controllers in `ZeroDawn.Shared`
- Do not create `new HttpClient()` manually when a registered or named client should be used
- Do not add retry to non-idempotent auth POST calls
- Do not log JWT secrets, SMTP passwords, or raw tokens
- Do not assume `App:BaseUrl` and `ApiBaseUrl` are the same setting
- Do not move server-only logic into the shared RCL

## Build Verification

From `Q:\Work\ZeroDawn`:

```powershell
dotnet build .\ZeroDawn.slnx
```

## Current Gaps To Remember

- `IUserApiClient` and `IAdminApiClient` are still shared interfaces without concrete host registrations, so some live-data surfaces remain stub-oriented
- MAUI physical-device certificate and network setup is still an environment concern, not solved purely by code
- Browser publishing must account for both `App:BaseUrl` and `ApiBaseUrl`
