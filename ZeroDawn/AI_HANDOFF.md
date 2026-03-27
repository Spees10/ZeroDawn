# AI_HANDOFF

## Project Context

This is a .NET 10 Blazor solution with four projects:

- [ZeroDawn.Shared](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Shared\ZeroDawn.Shared.csproj)
- [ZeroDawn.Web](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Web\ZeroDawn.Web.csproj)
- [ZeroDawn.Web.Client](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Web.Client\ZeroDawn.Web.Client.csproj)
- [ZeroDawn](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn\ZeroDawn.csproj)

Solution file:

- [ZeroDawn.slnx](Q:\Work\ZeroDawn\ZeroDawn.slnx)

## Technology Stack

- .NET 10
- Blazor Web App + Blazor WebAssembly
- .NET MAUI Blazor Hybrid
- ASP.NET Core controller APIs
- ASP.NET Core Identity
- EF Core SQL Server
- JWT auth with refresh tokens
- MailKit SMTP
- Serilog
- built-in localization with `.resx`
- built-in rate limiting

## Key Files

Startup:

- [ZeroDawn.Web/Program.cs](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Web\Program.cs)
- [ZeroDawn.Web.Client/Program.cs](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Web.Client\Program.cs)
- [ZeroDawn/MauiProgram.cs](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn\MauiProgram.cs)

Server auth and data:

- [AuthController.cs](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Web\Controllers\AuthController.cs)
- [UsersController.cs](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Web\Controllers\UsersController.cs)
- [AdminController.cs](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Web\Controllers\AdminController.cs)
- [ApplicationDbContext.cs](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Web\Data\ApplicationDbContext.cs)
- [ApplicationUser.cs](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Web\Data\ApplicationUser.cs)

Shared contracts and UI:

- [ApiResponse.cs](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Shared\Contracts\Common\ApiResponse.cs)
- [ApiEndpoints.cs](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Shared\Core\ApiEndpoints.cs)
- [Roles.cs](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Shared\Core\Constants\Roles.cs)
- [MainLayout.razor](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Shared\Layout\MainLayout.razor)
- [AuthLayout.razor](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Shared\Layout\AuthLayout.razor)
- [Routes.razor](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Shared\Routes.razor)

Localization:

- [SharedResources.cs](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Shared\Localization\SharedResources.cs)
- [SharedResources.resx](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Shared\Localization\Resources\SharedResources.resx)
- [SharedResources.ar.resx](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Shared\Localization\Resources\SharedResources.ar.resx)

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

## Common Tasks

### Add a new page

1. Create it under [ZeroDawn.Shared/Pages](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Shared\Pages)
2. Add `@page`
3. Use `@layout AuthLayout` for auth pages or `@layout MainLayout` for app pages
4. Reuse shared components from [Components](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Shared\Components)
5. Inject `IStringLocalizer<SharedResources>` for visible text
6. Use `ErrorDisplay` and `IToastService` for failures/feedback
7. Build the solution

### Add a new API endpoint

1. Put the DTO in the right folder under [ZeroDawn.Shared/Contracts](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Shared\Contracts)
2. Add/update endpoint constants in [ApiEndpoints.cs](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Shared\Core\ApiEndpoints.cs) if needed
3. Implement the endpoint in a controller under [ZeroDawn.Web/Controllers](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Web\Controllers)
4. Return `ApiResponse` or `ApiResponse<T>`
5. Add authorization explicitly
6. Use transactions for multi-entity writes
7. Build and test

### Add a new DTO

1. Create it in [ZeroDawn.Shared/Contracts](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Shared\Contracts)
2. Use file-scoped namespace
3. Add DataAnnotations when it is an input DTO
4. Reuse [ValidationMessages.cs](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Shared\Contracts\Validation\ValidationMessages.cs) where possible

### Add a new shared component

1. Create the `.razor` in [ZeroDawn.Shared/Components](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Shared\Components)
2. Create a matching `.razor.css`
3. Use design-system variables from [app.css](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Shared\wwwroot\app.css)
4. Avoid third-party UI packages
5. Prefer parameters and `EventCallback`

### Add a new localization key

1. Add the key to [SharedResources.resx](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Shared\Localization\Resources\SharedResources.resx)
2. Add the translation to [SharedResources.ar.resx](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Shared\Localization\Resources\SharedResources.ar.resx)
3. Use `IStringLocalizer<SharedResources>`

## What Not To Do

- Do not put secrets in browser config files
- Do not put controllers in `ZeroDawn.Shared`
- Do not create `new HttpClient()` manually
- Do not add retry to non-idempotent auth POST calls
- Do not log JWT secrets, SMTP passwords, or raw tokens
- Do not move server-only logic into the shared RCL

## Build Verification

From `Q:\Work\ZeroDawn`:

```powershell
dotnet build .\ZeroDawn.slnx
```

## Current Gaps To Remember

- `IUserApiClient` and `IAdminApiClient` are shared contracts/stubs and still need concrete host implementations for full real-data coverage
- Production MAUI device certificate/network setup is still an environment task, not “done by code”
