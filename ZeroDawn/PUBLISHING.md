# PUBLISHING

## Pre-Publish Rules

Before any publish:

- make sure `Jwt:Secret` comes from environment variables or a secret manager
- make sure SMTP credentials are not in checked-in JSON
- point `Database:ConnectionString` to the real target DB
- set `App:BaseUrl` to the real public URL
- build the full solution successfully

Build check:

```powershell
dotnet build Q:\Work\ZeroDawn\ZeroDawn.slnx
```

## Web Publish

Project:

- [ZeroDawn.Web.csproj](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Web\ZeroDawn.Web.csproj)

Publish command:

```powershell
dotnet publish Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Web\ZeroDawn.Web.csproj -c Release -o Q:\Work\ZeroDawn\publish\web
```

### IIS Basics

1. Publish `ZeroDawn.Web`
2. Install ASP.NET Core Hosting Bundle on the server
3. Create the IIS site/app pool
4. Point IIS to the publish folder
5. Set environment variables for secrets and connection string
6. Run DB migrations

### Azure App Service Basics

1. Publish `ZeroDawn.Web`
2. Deploy to App Service
3. Set App Settings for:
   - `Jwt__Secret`
   - `Smtp__Username`
   - `Smtp__Password`
   - `Database__ConnectionString`
   - `App__BaseUrl`
4. Run DB migrations

## Production Environment Variables

Examples:

```powershell
Jwt__Secret=replace-with-a-real-secret
Smtp__Username=real-smtp-user
Smtp__Password=real-smtp-password
Database__ConnectionString=Server=...;Database=...;User Id=...;Password=...;TrustServerCertificate=True;
App__BaseUrl=https://your-real-domain.example
```

## Database Migration In Production

Command:

```powershell
dotnet ef database update --project Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Web\ZeroDawn.Web.csproj
```

Important:

- [DatabaseSeeder.cs](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Web\Data\DatabaseSeeder.cs) also runs migrations on startup
- for controlled production deployments, explicit migration during release is still safer than relying only on startup migration

## MAUI Android Publish

Project:

- [ZeroDawn.csproj](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn\ZeroDawn.csproj)

Basic Android publish:

```powershell
dotnet publish Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn\ZeroDawn.csproj -f net10.0-android -c Release
```

Practical notes:

- production Android builds need signing
- do not ship with emulator URL `10.0.2.2`
- update [MauiProgram.cs](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn\MauiProgram.cs) to the real API URL for the target environment

## MAUI Windows Publish

Current state:

- [ZeroDawn.csproj](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn\ZeroDawn.csproj) contains `<WindowsPackageType>None</WindowsPackageType>`

That means the current setup is unpackaged Windows output, not a ready MSIX pipeline.

Unpackaged Windows publish:

```powershell
dotnet publish Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn\ZeroDawn.csproj -f net10.0-windows10.0.19041.0 -c Release
```

If you need MSIX:

- switch to packaged Windows configuration first
- define certificate/signing strategy
- verify package identity/versioning

Do not assume MSIX is ready just because the Windows target exists.

## Checklist Before Publish

### Web

- `dotnet build` succeeds
- production secrets supplied externally
- production DB reachable
- `App:BaseUrl` correct
- SMTP tested
- default development admin account/password reviewed

### MAUI

- API base URL correct for the environment
- no emulator-only addresses left in production build
- signing configured
- offline behavior tested
- token storage tested on the target platform

### Functional Smoke Tests

- login
- refresh token flow
- forgot password
- email confirmation
- admin user list
- error handling with reference numbers

## Deployment Risks

- missing `Jwt:Secret` breaks token-issuing flows
- Android emulator URL is wrong for real devices
- Windows MSIX needs extra packaging work
- auto-migrate-on-start is convenient but risky if DB changes are tightly controlled
