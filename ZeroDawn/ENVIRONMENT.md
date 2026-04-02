# ENVIRONMENT

## Configuration Precedence

For `ZeroDawn.Web`, config resolves in this order:

1. [appsettings.json](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Web\appsettings.json)
2. [appsettings.Development.json](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Web\appsettings.Development.json) or the active environment file
3. User Secrets in development
4. Environment variables

Main binding happens in [Program.cs](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Web\Program.cs).

For `ZeroDawn.Web.Client`, runtime config comes from static browser files:

- [wwwroot/appsettings.json](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Web.Client\wwwroot\appsettings.json)
- [wwwroot/appsettings.Development.json](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Web.Client\wwwroot\appsettings.Development.json)

These browser files must never contain secrets.

For `ZeroDawn`, the MAUI API base URL is currently configured in code:

- [MauiProgram.cs](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn\MauiProgram.cs)

## Config Keys

| Key | Purpose | Where set now | Secret? |
|---|---|---|---|
| `Jwt:Secret` | JWT signing key | User Secrets / env vars | Yes |
| `Jwt:Issuer` | token issuer | `ZeroDawn.Web/appsettings.json` | No |
| `Jwt:Audience` | token audience | `ZeroDawn.Web/appsettings.json` | No |
| `Jwt:AccessTokenExpirationMinutes` | access token lifetime | `ZeroDawn.Web/appsettings.json` | No |
| `Jwt:RefreshTokenExpirationDays` | refresh token lifetime | `ZeroDawn.Web/appsettings.json` | No |
| `Smtp:Host` | SMTP host | `ZeroDawn.Web/appsettings.json` | No |
| `Smtp:Port` | SMTP port | `ZeroDawn.Web/appsettings.json` | No |
| `Smtp:Username` | SMTP username | User Secrets / env vars | Yes |
| `Smtp:Password` | SMTP password | User Secrets / env vars | Yes |
| `Smtp:FromEmail` | sender email | `ZeroDawn.Web/appsettings.json` | No |
| `Smtp:FromName` | sender name | `ZeroDawn.Web/appsettings.json` | No |
| `Smtp:UseSsl` | SMTP SSL flag | `ZeroDawn.Web/appsettings.json` | No |
| `App:AppName` | app display name | `ZeroDawn.Web/appsettings.json` | No |
| `App:BaseUrl` | public server URL used in generated links and server-side auth client base address | `ZeroDawn.Web/appsettings.json` | No |
| `App:AllowSelfRegistration` | self-registration toggle | `ZeroDawn.Web/appsettings.json` | No |
| `App:RequireEmailConfirmation` | email confirmation behavior toggle consumed by app logic | `ZeroDawn.Web/appsettings.json` | No |
| `App:DefaultLanguage` | server request localization default | `ZeroDawn.Web/appsettings.json` | No |
| `Database:ConnectionString` | SQL connection string | `ZeroDawn.Web/appsettings.json` or env vars | Usually yes in production |
| `Logging:LogLevel:Default` | app log level | appsettings files | No |
| `ApiBaseUrl` | browser client API URL | `ZeroDawn.Web.Client/wwwroot/appsettings*.json` | No |

## Environment Variable Examples

Use double underscores:

```powershell
$env:Jwt__Secret = "replace-with-a-long-random-secret"
$env:Smtp__Username = "smtp-user"
$env:Smtp__Password = "smtp-password"
$env:Database__ConnectionString = "Server=...;Database=...;User Id=...;Password=...;TrustServerCertificate=True;"
$env:App__BaseUrl = "https://your-domain.example"
```

## API URL Strategy

### Web Host

Used for generated links and the server-side auth API client:

- key: `App:BaseUrl`
- file: [ZeroDawn.Web/appsettings.json](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Web\appsettings.json)

### Blazor WASM

Used for browser API calls:

- key: `ApiBaseUrl`
- file: [ZeroDawn.Web.Client/wwwroot/appsettings.json](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Web.Client\wwwroot\appsettings.json)
- current checked-in dev value: `https://localhost:7001`

### MAUI

Configured in code, not JSON:

- file: [MauiProgram.cs](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn\MauiProgram.cs)
- Android emulator: `https://10.0.2.2:7001`
- Windows: `https://localhost:7001`
- physical device: `https://<your-machine-ip>:7001`

## Localization Notes

- The server uses `App:DefaultLanguage` when configuring `RequestLocalizationOptions`
- The WASM client currently defaults to Arabic directly in [ZeroDawn.Web.Client/Program.cs](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Web.Client\Program.cs)
- The MAUI host currently defaults to Arabic directly in [MauiProgram.cs](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn\MauiProgram.cs)

If you change language strategy, update both config and code paths together.

## Add A New Config Key

### Server key

1. Add the property to the correct options class in [ZeroDawn.Web/Configuration](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Web\Configuration)
2. Add a non-secret default to [appsettings.json](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Web\appsettings.json)
3. If it is secret, keep it out of checked-in JSON
4. Bind or read it in [Program.cs](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Web\Program.cs)
5. Document it here

### Browser client key

1. Add it to `ZeroDawn.Web.Client/wwwroot/appsettings*.json`
2. Read it from [ZeroDawn.Web.Client/Program.cs](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Web.Client\Program.cs)
3. Do not store secrets there
4. Re-check [PUBLISHING.md](Q:\Work\ZeroDawn\ZeroDawn\PUBLISHING.md) if it affects deployment

### MAUI key or host constant

1. Update [MauiProgram.cs](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn\MauiProgram.cs)
2. Keep emulator, desktop, and physical-device behavior in mind
3. Document the change here and in [RUNNING.md](Q:\Work\ZeroDawn\ZeroDawn\RUNNING.md)

## Practical Notes

- JWT middleware in [ZeroDawn.Web/Program.cs](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Web\Program.cs) only turns on when `Jwt:Secret` is configured
- missing required secrets keep token flows from working, and non-development startup should fail fast
- SMTP failures are logged without crashing forgot-password or resend-confirmation style flows
- the checked-in DB connection string is development-only; production should override it externally
- `ApiBaseUrl` is a deployment-sensitive browser setting and is separate from `App:BaseUrl`
