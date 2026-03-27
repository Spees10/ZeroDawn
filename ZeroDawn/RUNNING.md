# RUNNING

## Quick Start

Project folders live under `Q:\Work\ZeroDawn\ZeroDawn\`, but the solution file is one level higher at `Q:\Work\ZeroDawn\ZeroDawn.slnx`.

If you already have the repo locally and just want to run it:

```powershell
cd Q:\Work\ZeroDawn\ZeroDawn
dotnet restore ..\ZeroDawn.slnx
dotnet user-secrets --project .\ZeroDawn.Web\ZeroDawn.Web.csproj set "Jwt:Secret" "replace-with-a-long-random-secret-at-least-32-characters"
dotnet ef database update --project .\ZeroDawn.Web\ZeroDawn.Web.csproj
dotnet run --project .\ZeroDawn.Web
```

Default development login:

- Email: `loai.asp97@gmail.com`
- Password: `012Shbl10@ZD`

## Prerequisites

- .NET 10 SDK
- Visual Studio with:
  - `ASP.NET and web development`
  - `.NET Multi-platform App UI development`
- Android Emulator if you want MAUI Android
- SQL Server LocalDB for the default development DB
- EF Core CLI:

```powershell
dotnet tool install --global dotnet-ef
```

## Clone And Restore

```powershell
git clone <your-repo-url> Q:\Work\ZeroDawn
cd Q:\Work\ZeroDawn\ZeroDawn
dotnet restore ..\ZeroDawn.slnx
```

## User Secrets

`ZeroDawn.Web` already has a `UserSecretsId` in [ZeroDawn.Web.csproj](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Web\ZeroDawn.Web.csproj).

Set secrets:

```powershell
dotnet user-secrets --project .\ZeroDawn.Web\ZeroDawn.Web.csproj set "Jwt:Secret" "replace-with-a-long-random-secret-at-least-32-characters"
dotnet user-secrets --project .\ZeroDawn.Web\ZeroDawn.Web.csproj set "Smtp:Username" "your-smtp-username"
dotnet user-secrets --project .\ZeroDawn.Web\ZeroDawn.Web.csproj set "Smtp:Password" "your-smtp-password"
```

Verify:

```powershell
dotnet user-secrets --project .\ZeroDawn.Web\ZeroDawn.Web.csproj list
```

Important:

- Do not put `Jwt:Secret`, `Smtp:Username`, or `Smtp:Password` in checked-in JSON
- JWT token flows are disabled if `Jwt:Secret` is missing

## Database Setup

Default development connection string is in [appsettings.json](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Web\appsettings.json):

```json
"Database": {
  "ConnectionString": "Server=(localdb)\\mssqllocaldb;Database=ZeroDawn;Trusted_Connection=True;"
}
```

Apply migrations:

```powershell
dotnet ef database update --project .\ZeroDawn.Web\ZeroDawn.Web.csproj
```

Notes:

- Migrations live in [ZeroDawn.Web/Migrations](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Web\Migrations)
- [DatabaseSeeder.cs](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Web\Data\DatabaseSeeder.cs) also runs `MigrateAsync()` on startup and seeds roles plus the static super admin

## Run The Web App

```powershell
dotnet run --project .\ZeroDawn.Web
```

Main files:

- [Program.cs](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Web\Program.cs)
- [appsettings.json](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Web\appsettings.json)
- [appsettings.Development.json](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Web\appsettings.Development.json)

Expected development URL:

- `https://localhost:7001`

## Run MAUI Android

Use Visual Studio:

1. Open [ZeroDawn.csproj](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn\ZeroDawn.csproj)
2. Select an Android emulator
3. Press `F5`

Current Android API URL in [MauiProgram.cs](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn\MauiProgram.cs):

- `https://10.0.2.2:7001`

### MAUI Android HTTPS

- Android debug builds use [DevHttpsConnectionHelper.cs](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn\Platforms\Android\DevHttpsConnectionHelper.cs) to trust the local `CN=localhost` development certificate automatically.
- Production must use a real trusted certificate. Do not rely on this helper outside debug development.
- If HTTPS still fails on the emulator or device, try:

```powershell
dotnet dev-certs https --trust
```

## Run MAUI Windows

Use Visual Studio:

1. Open [ZeroDawn.csproj](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn\ZeroDawn.csproj)
2. Select `Windows Machine`
3. Press `F5`

Current Windows API URL in [MauiProgram.cs](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn\MauiProgram.cs):

- `https://localhost:7001`

## Known Platform API URL Rules

- Web host links use `App:BaseUrl` in [ZeroDawn.Web/appsettings.json](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Web\appsettings.json)
- Blazor WASM uses `ApiBaseUrl` in [ZeroDawn.Web.Client/wwwroot/appsettings.json](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Web.Client\wwwroot\appsettings.json)
- MAUI Android emulator uses `10.0.2.2`
- MAUI Windows uses `localhost`
- Physical devices must use `https://<your-machine-ip>:7001`

## Troubleshooting

### `Jwt:Secret is not configured`

Fix:

```powershell
dotnet user-secrets --project .\ZeroDawn.Web\ZeroDawn.Web.csproj set "Jwt:Secret" "replace-with-a-long-random-secret-at-least-32-characters"
```

### Android emulator cannot reach the API

Check:

1. The API is running
2. Android uses `https://10.0.2.2:7001`, not `localhost`
3. The firewall is not blocking the port
4. The dev HTTPS certificate is trusted

Trust the dev cert on the host:

```powershell
dotnet dev-certs https --trust
```

### HTTPS certificate issues on Android

Most likely cause:

- TLS trust problem between emulator/device and your host cert

Practical fix plan:

1. Trust the local dev certificate on Windows
2. Restart the emulator
3. Re-test with the correct host URL
4. For physical devices, move to a proper trusted certificate strategy instead of relying on localhost-style development certs

### LocalDB or SQL connection failure

Check:

- LocalDB is installed
- The connection string in [appsettings.json](Q:\Work\ZeroDawn\ZeroDawn\ZeroDawn.Web\appsettings.json) is valid for your machine
- `dotnet ef database update` succeeds

### `dotnet ef database update` fails

Check:

- `dotnet-ef` is installed
- `ZeroDawn.Web` is the startup project
- the SQL connection is valid

Safe retry path:

```powershell
dotnet restore .\ZeroDawn.Web\ZeroDawn.Web.csproj
dotnet ef database update --project .\ZeroDawn.Web\ZeroDawn.Web.csproj
```

## 15-Minute Setup Checklist

1. Restore packages
2. Set `Jwt:Secret`
3. Apply migrations
4. Run `ZeroDawn.Web`
5. Log in with `loai.asp97@gmail.com / 012Shbl10@ZD`
6. If needed, start a MAUI target from Visual Studio after the API is already running
