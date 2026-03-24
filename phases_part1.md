# ZeroDawn Roadmap — Phases 0–4

## Phase 0 · Foundation & Core

**Goal**: Establish folder structure, `Result<T>`, `Guard`, constants, and base theme/CSS.
**Why**: Every subsequent phase depends on shared primitives and a consistent project layout.
**Prerequisites**: Clean solution compiles and runs.

### Task 0.1 — Create Shared Folder Structure

**Prompt for AI model:**
```
You are working on a .NET 10 Blazor Hybrid solution at q:\Work\ZeroDawn\ZeroDawn\.
The project ZeroDawn.Shared is a Razor Class Library at ZeroDawn.Shared/.

Create the following empty folder structure inside ZeroDawn.Shared/:
- Core/
- Core/Constants/
- Contracts/
- Contracts/Auth/
- Contracts/Users/
- Contracts/Common/
- Contracts/Validation/
- Services/
- Components/
- Components/Forms/
- Components/Feedback/
- Components/Layout/
- Components/Dialogs/
- Components/Common/
- Localization/

Each folder must contain an empty .gitkeep file so Git tracks them.
Do NOT modify any existing files. Do NOT move existing Pages/ or Layout/ folders.
Report: folders created, any issues.
```

**Expected output**: Empty folder skeleton with `.gitkeep` files.
**Risks**: None.
**Acceptance**: All folders exist. Solution still compiles.

---

### Task 0.2 — Implement Result\<T\> Wrapper

**Prompt for AI model:**
```
In ZeroDawn.Shared/Core/, create a file Result.cs with the following:

namespace ZeroDawn.Shared.Core;

public class Result
{
    public bool Succeeded { get; init; }
    public string? Error { get; init; }
    public string? ErrorCode { get; init; }
    public List<string> ValidationErrors { get; init; } = [];

    public static Result Success() => new() { Succeeded = true };
    public static Result Failure(string error, string? errorCode = null)
        => new() { Succeeded = false, Error = error, ErrorCode = errorCode };
    public static Result ValidationFailure(List<string> errors)
        => new() { Succeeded = false, ValidationErrors = errors, ErrorCode = "VALIDATION_ERROR" };
}

public class Result<T> : Result
{
    public T? Data { get; init; }

    public static Result<T> Success(T data) => new() { Succeeded = true, Data = data };
    public new static Result<T> Failure(string error, string? errorCode = null)
        => new() { Succeeded = false, Error = error, ErrorCode = errorCode };
    public new static Result<T> ValidationFailure(List<string> errors)
        => new() { Succeeded = false, ValidationErrors = errors, ErrorCode = "VALIDATION_ERROR" };
}

Rules:
- Use file-scoped namespace.
- Nullable enabled.
- Do NOT add extra methods, serialization attributes, or overengineering.
- Do NOT modify any other files.
- Report: file created, namespace used.
```

**Expected output**: `Result.cs` with non-generic and generic variants.
**Risks**: None.
**Acceptance**: `Result.Success()`, `Result<T>.Failure("x")`, `Result.ValidationFailure([...])` compile. No build errors.

---

### Task 0.3 — Implement Guard Helpers

**Prompt for AI model:**
```
In ZeroDawn.Shared/Core/, create Guard.cs:

namespace ZeroDawn.Shared.Core;

public static class Guard
{
    public static T AgainstNull<T>(T? value, string paramName) where T : class
        => value ?? throw new ArgumentNullException(paramName);

    public static string AgainstNullOrEmpty(string? value, string paramName)
        => string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Value cannot be null or empty.", paramName)
            : value;

    public static Guid AgainstEmptyGuid(Guid value, string paramName)
        => value == Guid.Empty
            ? throw new ArgumentException("GUID cannot be empty.", paramName)
            : value;
}

Rules:
- File-scoped namespace. Nullable enabled.
- Do NOT add more guards than shown. Keep it minimal.
- Do NOT modify any other files.
```

**Expected output**: `Guard.cs` with 3 guard methods.
**Risks**: None.
**Acceptance**: Compiles. `Guard.AgainstNull(null, "x")` throws `ArgumentNullException`.

---

### Task 0.4 — Shared Constants

**Prompt for AI model:**
```
In ZeroDawn.Shared/Core/Constants/, create the following files:

1. Roles.cs:
namespace ZeroDawn.Shared.Core.Constants;
public static class Roles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Admin = "Admin";
    public const string User = "User";
}

2. Permissions.cs:
namespace ZeroDawn.Shared.Core.Constants;
public static class Permissions
{
    public const string ManageUsers = "Permissions.ManageUsers";
    public const string ManageAdmins = "Permissions.ManageAdmins";
    public const string ViewErrorLogs = "Permissions.ViewErrorLogs";
    public const string ManageSettings = "Permissions.ManageSettings";
}

3. PolicyNames.cs:
namespace ZeroDawn.Shared.Core.Constants;
public static class PolicyNames
{
    public const string RequireSuperAdmin = "RequireSuperAdmin";
    public const string RequireAdmin = "RequireAdmin";
    public const string RequireUser = "RequireUser";
}

Rules:
- File-scoped namespaces. No extra constants beyond what is shown.
- Do NOT modify any other files.
```

**Expected output**: 3 constant files.
**Acceptance**: All compile. Constants are accessible from any project referencing `ZeroDawn.Shared`.

---

### Task 0.5 — API Response Contract

**Prompt for AI model:**
```
In ZeroDawn.Shared/Contracts/Common/, create:

1. ApiResponse.cs:
namespace ZeroDawn.Shared.Contracts.Common;

public class ApiResponse<T>
{
    public bool Succeeded { get; init; }
    public T? Data { get; init; }
    public string? Error { get; init; }
    public string? ErrorCode { get; init; }
    public List<string> ValidationErrors { get; init; } = [];
    public string? ReferenceNumber { get; init; }
}

2. PagedRequest.cs:
namespace ZeroDawn.Shared.Contracts.Common;

public class PagedRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? SearchTerm { get; init; }
    public string? SortBy { get; init; }
    public bool SortDescending { get; init; }
}

3. PagedResponse.cs:
namespace ZeroDawn.Shared.Contracts.Common;

public class PagedResponse<T>
{
    public List<T> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

Rules:
- File-scoped namespaces. Nullable enabled.
- Do NOT modify any other files.
```

**Expected output**: 3 contract files.
**Acceptance**: Compiles. `ApiResponse<string>`, `PagedRequest`, `PagedResponse<UserDto>` are usable.

---

### Task 0.6 — Auth Contracts (DTOs)

**Prompt for AI model:**
```
In ZeroDawn.Shared/Contracts/Auth/, create these files:

1. LoginRequest.cs:
namespace ZeroDawn.Shared.Contracts.Auth;
using System.ComponentModel.DataAnnotations;

public class LoginRequest
{
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required, MinLength(6)] public string Password { get; set; } = "";
}

2. RegisterRequest.cs:
namespace ZeroDawn.Shared.Contracts.Auth;
using System.ComponentModel.DataAnnotations;

public class RegisterRequest
{
    [Required, MaxLength(50)] public string FullName { get; set; } = "";
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required, MinLength(6), MaxLength(100)] public string Password { get; set; } = "";
    [Required, Compare(nameof(Password))] public string ConfirmPassword { get; set; } = "";
}

3. AuthResponse.cs:
namespace ZeroDawn.Shared.Contracts.Auth;

public class AuthResponse
{
    public string AccessToken { get; init; } = "";
    public string RefreshToken { get; init; } = "";
    public DateTime ExpiresAt { get; init; }
    public string Email { get; init; } = "";
    public string FullName { get; init; } = "";
    public List<string> Roles { get; init; } = [];
}

4. ForgotPasswordRequest.cs:
namespace ZeroDawn.Shared.Contracts.Auth;
using System.ComponentModel.DataAnnotations;

public class ForgotPasswordRequest
{
    [Required, EmailAddress] public string Email { get; set; } = "";
}

5. ResetPasswordRequest.cs:
namespace ZeroDawn.Shared.Contracts.Auth;
using System.ComponentModel.DataAnnotations;

public class ResetPasswordRequest
{
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required] public string Token { get; set; } = "";
    [Required, MinLength(6)] public string NewPassword { get; set; } = "";
    [Required, Compare(nameof(NewPassword))] public string ConfirmPassword { get; set; } = "";
}

6. ChangePasswordRequest.cs:
namespace ZeroDawn.Shared.Contracts.Auth;
using System.ComponentModel.DataAnnotations;

public class ChangePasswordRequest
{
    [Required] public string CurrentPassword { get; set; } = "";
    [Required, MinLength(6)] public string NewPassword { get; set; } = "";
    [Required, Compare(nameof(NewPassword))] public string ConfirmPassword { get; set; } = "";
}

7. RefreshTokenRequest.cs:
namespace ZeroDawn.Shared.Contracts.Auth;

public class RefreshTokenRequest
{
    public string AccessToken { get; set; } = "";
    public string RefreshToken { get; set; } = "";
}

8. ConfirmEmailRequest.cs:
namespace ZeroDawn.Shared.Contracts.Auth;

public class ConfirmEmailRequest
{
    public string UserId { get; set; } = "";
    public string Token { get; set; } = "";
}

9. ResendConfirmationRequest.cs:
namespace ZeroDawn.Shared.Contracts.Auth;
using System.ComponentModel.DataAnnotations;

public class ResendConfirmationRequest
{
    [Required, EmailAddress] public string Email { get; set; } = "";
}

Rules:
- File-scoped namespaces. DataAnnotations for validation.
- Do NOT add controllers or services. DTOs only.
- Do NOT modify any other files.
```

**Expected output**: 9 DTO files.
**Acceptance**: All compile. DataAnnotations validate correctly.

---

## Phase 1 · Configuration & Secrets

**Goal**: Centralized configuration with strongly-typed options, multi-runtime API URL resolution.
**Why**: Every client and server component needs to know where the API lives, and secrets must be isolated.
**Prerequisites**: Phase 0 complete.

### Task 1.1 — Server Configuration Options

**Prompt for AI model:**
```
In ZeroDawn.Web, create a folder Configuration/ with these files:

1. JwtOptions.cs:
namespace ZeroDawn.Web.Configuration;

public class JwtOptions
{
    public const string Section = "Jwt";
    public string Secret { get; set; } = "";
    public string Issuer { get; set; } = "";
    public string Audience { get; set; } = "";
    public int AccessTokenExpirationMinutes { get; set; } = 30;
    public int RefreshTokenExpirationDays { get; set; } = 7;
}

2. SmtpOptions.cs:
namespace ZeroDawn.Web.Configuration;

public class SmtpOptions
{
    public const string Section = "Smtp";
    public string Host { get; set; } = "";
    public int Port { get; set; } = 587;
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string FromEmail { get; set; } = "";
    public string FromName { get; set; } = "";
    public bool UseSsl { get; set; } = true;
}

3. AppOptions.cs:
namespace ZeroDawn.Web.Configuration;

public class AppOptions
{
    public const string Section = "App";
    public string AppName { get; set; } = "ZeroDawn";
    public string BaseUrl { get; set; } = "";
    public bool AllowSelfRegistration { get; set; } = true;
    public bool RequireEmailConfirmation { get; set; } = true;
    public string DefaultLanguage { get; set; } = "en";
}

4. DatabaseOptions.cs:
namespace ZeroDawn.Web.Configuration;

public class DatabaseOptions
{
    public const string Section = "Database";
    public string ConnectionString { get; set; } = "";
}

Then update appsettings.json to include placeholder sections:
{
  "Jwt": {
    "Issuer": "ZeroDawn",
    "Audience": "ZeroDawn.Clients",
    "AccessTokenExpirationMinutes": 30,
    "RefreshTokenExpirationDays": 7
  },
  "Smtp": {
    "Host": "smtp.example.com",
    "Port": 587,
    "FromEmail": "noreply@example.com",
    "FromName": "ZeroDawn",
    "UseSsl": true
  },
  "App": {
    "AppName": "ZeroDawn",
    "BaseUrl": "https://localhost:7001",
    "AllowSelfRegistration": true,
    "RequireEmailConfirmation": true,
    "DefaultLanguage": "en"
  },
  "Database": {
    "ConnectionString": "Server=(localdb)\\mssqllocaldb;Database=ZeroDawn;Trusted_Connection=True;"
  },
  "Logging": {
    "LogLevel": { "Default": "Information" }
  }
}

IMPORTANT: Do NOT put Jwt:Secret, Smtp:Username, or Smtp:Password in appsettings.json. These must come from User Secrets in development and environment variables in production.

Update appsettings.Development.json:
{
  "Logging": {
    "LogLevel": { "Default": "Debug" }
  }
}

Rules:
- Secrets (Jwt:Secret, Smtp:Username, Smtp:Password) go in User Secrets only.
- Do NOT create any services or DI registration yet.
- Do NOT modify Program.cs yet.
- Do NOT modify any other existing files beyond appsettings*.json.
```

**Expected output**: 4 option classes + updated appsettings files.
**Risks**: Forgetting to exclude secrets from appsettings.json.
**Acceptance**: Solution compiles. No secrets in source-controlled config files.

---

### Task 1.2 — Client Runtime Configuration

**Prompt for AI model:**
```
In ZeroDawn.Shared/Core/, create ApiEndpoints.cs:

namespace ZeroDawn.Shared.Core;

public class ApiEndpoints
{
    public string BaseUrl { get; set; } = "";

    public static class Auth
    {
        public const string Login = "api/auth/login";
        public const string Register = "api/auth/register";
        public const string RefreshToken = "api/auth/refresh";
        public const string ForgotPassword = "api/auth/forgot-password";
        public const string ResetPassword = "api/auth/reset-password";
        public const string ChangePassword = "api/auth/change-password";
        public const string ConfirmEmail = "api/auth/confirm-email";
        public const string ResendConfirmation = "api/auth/resend-confirmation";
        public const string Logout = "api/auth/logout";
    }

    public static class Users
    {
        public const string GetAll = "api/users";
        public const string GetById = "api/users/{0}";
        public const string Profile = "api/users/profile";
        public const string UpdateProfile = "api/users/profile";
    }

    public static class Admin
    {
        public const string ErrorLogs = "api/admin/error-logs";
        public const string ManageUsers = "api/admin/users";
        public const string ManageAdmins = "api/admin/admins";
    }
}

Then in ZeroDawn.Web.Client/wwwroot/appsettings.json:
{
  "ApiBaseUrl": "https://localhost:7001"
}

And ZeroDawn.Web.Client/wwwroot/appsettings.Development.json:
{
  "ApiBaseUrl": "https://localhost:7001"
}

IMPORTANT: These client-side files must NEVER contain secrets. Only non-secret runtime config.

For MAUI, the API base URL will be set differently per platform in MauiProgram.cs (later task). For now, just note the pattern:
- Android emulator: https://10.0.2.2:7001
- Windows: https://localhost:7001
- Physical device: https://<your-machine-ip>:7001

Rules:
- Do NOT modify Program.cs files yet.
- Do NOT add HttpClient registrations yet.
- Do NOT modify any existing files other than the client appsettings files.
```

**Expected output**: `ApiEndpoints.cs` + client appsettings.
**Acceptance**: Compiles. No secrets in client-accessible configs.

---

## Phase 2 · Database & Identity

**Goal**: EF Core DbContext, Identity setup, role seeding, migrations.
**Why**: Auth, user management, error logging all depend on the database.
**Prerequisites**: Phase 1 complete.

### Task 2.1 — Install NuGet Packages (Server)

**Prompt for AI model:**
```
Add the following NuGet packages to ZeroDawn.Web/ZeroDawn.Web.csproj:

<PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="10.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="10.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="10.0.0">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
</PackageReference>
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="10.0.0" />
<PackageReference Include="Serilog.AspNetCore" Version="9.0.0" />
<PackageReference Include="Serilog.Sinks.MSSqlServer" Version="8.1.0" />
<PackageReference Include="MailKit" Version="4.12.1" />

Do NOT modify any CS files. Only modify the .csproj file.
Verify versions exist on NuGet (user should check actual latest stable versions).
Restore packages: dotnet restore.
Report: packages added, restore result.
```

**Expected output**: Updated csproj with packages. `dotnet restore` succeeds.
**Risks**: Version mismatch with .NET 10 preview. User must verify latest stable versions.
**Acceptance**: `dotnet restore` and `dotnet build` succeed.

---

### Task 2.2 — Application User & DbContext

**Prompt for AI model:**
```
In ZeroDawn.Web, create folder Data/ with:

1. Data/ApplicationUser.cs:
using Microsoft.AspNetCore.Identity;

namespace ZeroDawn.Web.Data;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; } = true;
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }
}

2. Data/ApplicationDbContext.cs:
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ZeroDawn.Web.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<ErrorLog> ErrorLogs => Set<ErrorLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.RefreshToken).HasMaxLength(500);
        });

        builder.Entity<ErrorLog>(entity =>
        {
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.ReferenceNumber);
            entity.Property(e => e.Message).HasMaxLength(2000);
            entity.Property(e => e.ReferenceNumber).HasMaxLength(20);
        });
    }
}

3. Data/ErrorLog.cs:
namespace ZeroDawn.Web.Data;

public class ErrorLog
{
    public int Id { get; set; }
    public string ReferenceNumber { get; set; } = "";
    public string Message { get; set; } = "";
    public string? StackTrace { get; set; }
    public string? Source { get; set; }
    public string? InnerException { get; set; }
    public string? UserId { get; set; }
    public string? RequestPath { get; set; }
    public string? CorrelationId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

Rules:
- File-scoped namespaces. Nullable enabled.
- Do NOT register in DI yet. Do NOT create migrations yet.
- Do NOT modify any existing files.
```

**Expected output**: 3 data files.
**Acceptance**: Compiles with EF Core packages.

---

### Task 2.3 — Register Identity + EF Core in Program.cs

**Prompt for AI model:**
```
Modify ZeroDawn.Web/Program.cs to register Identity and EF Core.

Add these using statements at the top:
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ZeroDawn.Web.Configuration;
using ZeroDawn.Web.Data;

After `var builder = WebApplication.CreateBuilder(args);`, add:

// Configuration
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.Section));
builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection(SmtpOptions.Section));
builder.Services.Configure<AppOptions>(builder.Configuration.GetSection(AppOptions.Section));

// Database
var connectionString = builder.Configuration.GetSection("Database:ConnectionString").Value
    ?? throw new InvalidOperationException("Database connection string is not configured.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = false; // controlled by AppOptions at runtime
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

After `builder.Services.AddRazorComponents()...`, add:
builder.Services.AddControllers();

After `app.UseAntiforgery();`, add:
app.MapControllers();

Rules:
- Preserve ALL existing code in Program.cs.
- Only ADD new lines where specified.
- Do NOT remove existing service registrations (FormFactor, RazorComponents, etc.).
- Do NOT create controllers yet.
- Report: lines added, final Program.cs behavior.
```

**Expected output**: Updated Program.cs with Identity + EF Core + Controllers wired.
**Risks**: Must preserve existing Blazor Hybrid wiring.
**Acceptance**: `dotnet build` succeeds. App starts (won't have DB yet, but no compile errors).

---

### Task 2.4 — Initial Migration & Seed Data

**Prompt for AI model:**
```
In ZeroDawn.Web/Data/, create DatabaseSeeder.cs:

using Microsoft.AspNetCore.Identity;
using ZeroDawn.Shared.Core.Constants;

namespace ZeroDawn.Web.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider services, ILogger logger)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        await context.Database.MigrateAsync();

        // Seed roles
        string[] roles = [Roles.SuperAdmin, Roles.Admin, Roles.User];
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
                logger.LogInformation("Created role: {Role}", role);
            }
        }

        // Seed super admin
        const string adminEmail = "admin@zerodawn.local";
        if (await userManager.FindByEmailAsync(adminEmail) is null)
        {
            var admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "Super Admin",
                EmailConfirmed = true,
                IsActive = true
            };
            var result = await userManager.CreateAsync(admin, "Admin@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, Roles.SuperAdmin);
                logger.LogInformation("Seeded super admin: {Email}", adminEmail);
            }
            else
            {
                logger.LogError("Failed to seed super admin: {Errors}",
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }
}

Then in Program.cs, BEFORE `app.Run();`, add:
await DatabaseSeeder.SeedAsync(app.Services, app.Logger);

Then run from ZeroDawn.Web directory:
dotnet ef migrations add InitialCreate

Rules:
- Do NOT log passwords or secrets.
- Do NOT modify any other files besides DatabaseSeeder.cs and Program.cs.
- The super admin password is a DEVELOPMENT placeholder only.
- Report: migration created, seed logic, any issues.
```

**Expected output**: Seeder + migration. DB created on first run.
**Acceptance**: `dotnet ef database update` succeeds. SuperAdmin user exists in DB. 3 roles created.

---

## Phase 3 · Auth API & JWT

**Goal**: REST API for login, register, refresh, forgot/reset password, confirm email, change password, logout.
**Why**: Both WASM and MAUI clients authenticate via API. No cookie auth.
**Prerequisites**: Phase 2 complete (Identity + DB running).

### Task 3.1 — JWT Token Service

**Prompt for AI model:**
```
In ZeroDawn.Web/Services/, create:

1. ITokenService.cs:
using ZeroDawn.Web.Data;

namespace ZeroDawn.Web.Services;

public interface ITokenService
{
    string GenerateAccessToken(ApplicationUser user, IList<string> roles);
    string GenerateRefreshToken();
    System.Security.Claims.ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}

2. TokenService.cs implementing ITokenService:
- Inject IOptions<JwtOptions>.
- GenerateAccessToken: create JWT with claims (sub=userId, email, fullName, roles), sign with HS256 using JwtOptions.Secret, set expiry from JwtOptions.AccessTokenExpirationMinutes, set issuer and audience.
- GenerateRefreshToken: generate 64-byte random base64 string using RandomNumberGenerator.
- GetPrincipalFromExpiredToken: validate token WITHOUT checking expiry, return ClaimsPrincipal or null.

Register in Program.cs DI:
builder.Services.AddScoped<ITokenService, TokenService>();

Use these packages (already installed):
- System.IdentityModel.Tokens.Jwt
- Microsoft.IdentityModel.Tokens
- Microsoft.AspNetCore.Authentication.JwtBearer

Also add JWT authentication to Program.cs:
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var jwtOptions = builder.Configuration.GetSection(JwtOptions.Section).Get<JwtOptions>()!;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtOptions.Issuer,
        ValidAudience = jwtOptions.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(
            System.Text.Encoding.UTF8.GetBytes(jwtOptions.Secret)),
        ClockSkew = TimeSpan.Zero
    };
});

Add BEFORE app.MapControllers():
app.UseAuthentication();
app.UseAuthorization();

Rules:
- Do NOT log the JWT secret.
- Do NOT store tokens in logs.
- Use ClockSkew = TimeSpan.Zero (no grace period).
- Do NOT modify unrelated files.
- Report: files created, DI registrations, auth pipeline order.
```

**Expected output**: Token service + JWT auth middleware configured.
**Acceptance**: Compiles. Auth pipeline is in correct order (UseAuthentication before UseAuthorization before MapControllers).

---

### Task 3.2 — Auth Controller

**Prompt for AI model:**
```
In ZeroDawn.Web, create Controllers/AuthController.cs:

An [ApiController] with [Route("api/auth")] containing these endpoints:

[POST] login - accepts LoginRequest, returns ApiResponse<AuthResponse>
  - Find user by email. If not found, return generic "Invalid credentials" (do NOT reveal if email exists).
  - Check password. If wrong, return "Invalid credentials."
  - If user.IsActive == false, return "Account is disabled."
  - If AppOptions.RequireEmailConfirmation && !user.EmailConfirmed, return "Email not confirmed."
  - On success: generate access token + refresh token, store refresh token hash on user, update LastLoginAt, return AuthResponse.
  - Log: LogInformation("User logged in: {UserId}", user.Id) — never log email or password.

[POST] register - accepts RegisterRequest, returns ApiResponse<AuthResponse>
  - If AppOptions.AllowSelfRegistration == false, return 403.
  - Create user with UserManager. On failure return validation errors.
  - Add User role.
  - If email confirmation on: generate confirmation token, return success with message "Please confirm your email."
  - If email confirmation off: auto-confirm, generate tokens, return AuthResponse.

[POST] refresh - accepts RefreshTokenRequest, returns ApiResponse<AuthResponse>
  - Extract principal from expired access token.
  - Find user. Validate stored refresh token matches and is not expired.
  - Generate new access + refresh tokens. Store new refresh token on user.
  - Return new AuthResponse.

[POST] forgot-password - accepts ForgotPasswordRequest, returns ApiResponse (non-generic)
  - Always return success message "If the email exists, a reset link has been sent."
  - If user exists, generate password reset token — email sending is a later task, for now just log the token in dev.
  - Do NOT reveal whether the email exists.

[POST] reset-password - accepts ResetPasswordRequest, returns ApiResponse
  - Find user by email. Validate token. Reset password.
  - Return success or validation errors.

[POST] change-password - [Authorize], accepts ChangePasswordRequest, returns ApiResponse
  - Get current user from claims. Change password via UserManager.

[POST] confirm-email - accepts ConfirmEmailRequest, returns ApiResponse
  - Find user. Confirm email via UserManager.

[POST] resend-confirmation - accepts ResendConfirmationRequest, returns ApiResponse
  - Always return success. If user exists and unconfirmed, generate new token.
  - Email sending is a later task.

[POST] logout - [Authorize], returns ApiResponse
  - Clear refresh token on user. Return success.

Error handling:
- Catch all exceptions, log them, return 500 with generic message + reference number.
- Use try/catch in each action for now. Global middleware comes in Phase 5.
- Return ProblemDetails for 500 errors.
- Never reveal stack traces in production responses.

Validation:
- ModelState is automatically validated via [ApiController].
- Return 400 with validation errors if ModelState is invalid.

Rules:
- Do NOT create custom middleware yet.
- Do NOT implement email sending yet — just log tokens in dev mode.
- Do NOT modify any files other than AuthController.cs and Program.cs (for DI if needed).
- All DB operations that modify multiple entities should use a transaction.
- Log user IDs, never emails/passwords/tokens.
- Report: endpoints created, error handling approach, files changed.
```

**Expected output**: Full auth controller with 9 endpoints.
**Risks**: Token validation logic must be rigorous. Refresh token must be hashed before storage.
**Acceptance**: All endpoints return proper `ApiResponse<T>`. Login → token → refresh flow works via Postman/curl. Invalid credentials return generic message.

---

## Phase 4 · Client Auth Infrastructure

**Goal**: Token storage, auth state provider, auth HTTP handler, typed HttpClient registrations.
**Why**: Both WASM and MAUI clients need to store tokens, attach them to requests, and detect auth state.
**Prerequisites**: Phase 3 complete (API returns JWT).

### Task 4.1 — Shared Auth Interfaces

**Prompt for AI model:**
```
In ZeroDawn.Shared/Services/, create:

1. ITokenStorageService.cs:
namespace ZeroDawn.Shared.Services;

public interface ITokenStorageService
{
    Task<string?> GetAccessTokenAsync();
    Task<string?> GetRefreshTokenAsync();
    Task SetTokensAsync(string accessToken, string refreshToken);
    Task ClearTokensAsync();
}

2. IAuthService.cs:
using ZeroDawn.Shared.Contracts.Auth;
using ZeroDawn.Shared.Contracts.Common;

namespace ZeroDawn.Shared.Services;

public interface IAuthService
{
    Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request);
    Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request);
    Task<ApiResponse<AuthResponse>> RefreshTokenAsync();
    Task<ApiResponse> ForgotPasswordAsync(ForgotPasswordRequest request);
    Task<ApiResponse> ResetPasswordAsync(ResetPasswordRequest request);
    Task<ApiResponse> ChangePasswordAsync(ChangePasswordRequest request);
    Task<ApiResponse> ConfirmEmailAsync(ConfirmEmailRequest request);
    Task<ApiResponse> ResendConfirmationAsync(ResendConfirmationRequest request);
    Task LogoutAsync();
}

Note: ApiResponse (non-generic) needs to be added to Contracts/Common/ApiResponse.cs:
public class ApiResponse
{
    public bool Succeeded { get; init; }
    public string? Error { get; init; }
    public string? ErrorCode { get; init; }
    public List<string> ValidationErrors { get; init; } = [];
    public string? ReferenceNumber { get; init; }
}

And make ApiResponse<T> inherit from ApiResponse.

Rules:
- Interfaces only. No implementations yet.
- Do NOT modify any other files except ApiResponse.cs to add the non-generic base class.
```

**Expected output**: 2 interface files + updated ApiResponse.
**Acceptance**: Compiles from all projects.

---

### Task 4.2 — WASM Token Store & Auth State Provider

**Prompt for AI model:**
```
In ZeroDawn.Web.Client/Services/, create:

1. BrowserTokenStorage.cs implementing ITokenStorageService:
- Use IJSRuntime to read/write from browser localStorage.
- Keys: "access_token", "refresh_token".
- Protected with try/catch (JS interop can fail during prerendering).

2. WebAuthStateProvider.cs extending AuthenticationStateProvider:
- Inject ITokenStorageService.
- GetAuthenticationStateAsync: read access token, if null return anonymous.
  If token exists, parse claims from JWT (System.Security.Claims), check expiry.
  If expired, return anonymous (refresh will be handled by the HTTP handler).
  If valid, return authenticated ClaimsPrincipal.
- Add public method NotifyAuthStateChanged() to call NotifyAuthenticationStateChanged().

3. AuthHttpHandler.cs (DelegatingHandler):
- Inject ITokenStorageService.
- On every request: attach "Authorization: Bearer {accessToken}" header if token exists.
- If response is 401: attempt token refresh via a direct HttpClient call to the refresh endpoint.
  If refresh succeeds: store new tokens, retry original request once.
  If refresh fails: clear tokens, do not retry.
- Do NOT retry non-auth failures.

Register in ZeroDawn.Web.Client/Program.cs:
builder.Services.AddScoped<ITokenStorageService, BrowserTokenStorage>();
builder.Services.AddScoped<AuthenticationStateProvider, WebAuthStateProvider>();
builder.Services.AddScoped<WebAuthStateProvider>();
builder.Services.AddScoped<AuthHttpHandler>();

Add package reference to ZeroDawn.Web.Client.csproj:
<PackageReference Include="Microsoft.AspNetCore.Components.Authorization" Version="10.0.0" />

Rules:
- The AuthHttpHandler must NOT create new HttpClient() — it receives one via DI.
- Handle circular dependency: the refresh call should use a separate named HttpClient without the auth handler.
- Do NOT modify ZeroDawn.Shared files.
- Do NOT modify ZeroDawn.Web files.
- Report: files created, DI registrations, refresh flow explained.
```

**Expected output**: 3 service files + updated Program.cs + updated csproj.
**Risks**: Circular dependency in refresh flow. Prerendering JS interop failures.
**Acceptance**: Compiles. Token stored in localStorage. 401 triggers refresh once.

---

### Task 4.3 — MAUI Token Store & Connectivity

**Prompt for AI model:**
```
In ZeroDawn/Services/, create:

1. SecureStorageTokenService.cs implementing ITokenStorageService:
- Use MAUI SecureStorage for tokens.
- Keys: "access_token", "refresh_token".
- Wrap in try/catch — SecureStorage can throw on some platforms.

2. MauiAuthStateProvider.cs extending AuthenticationStateProvider:
- Same logic as WebAuthStateProvider but using ITokenStorageService from SecureStorage.

3. MauiConnectivityService.cs:
using Microsoft.Maui.Networking;

namespace ZeroDawn.Services;

public interface IConnectivityService
{
    bool IsConnected { get; }
    event EventHandler<bool> ConnectivityChanged;
}

public class MauiConnectivityService : IConnectivityService, IDisposable
{
    public bool IsConnected => Connectivity.Current.NetworkAccess == NetworkAccess.Internet;

    public event EventHandler<bool>? ConnectivityChanged;

    public MauiConnectivityService()
    {
        Connectivity.Current.ConnectivityChanged += OnConnectivityChanged;
    }

    private void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
    {
        ConnectivityChanged?.Invoke(this, e.NetworkAccess == NetworkAccess.Internet);
    }

    public void Dispose()
    {
        Connectivity.Current.ConnectivityChanged -= OnConnectivityChanged;
    }
}

4. MauiAuthHttpHandler.cs — same as WASM AuthHttpHandler but using ITokenStorageService from SecureStorage.

Register in MauiProgram.cs with platform-aware API base URL:
#if ANDROID
    const string apiBaseUrl = "https://10.0.2.2:7001";
#elif WINDOWS
    const string apiBaseUrl = "https://localhost:7001";
#else
    const string apiBaseUrl = "https://localhost:7001";
#endif

builder.Services.AddSingleton<IConnectivityService, MauiConnectivityService>();
builder.Services.AddSingleton<ITokenStorageService, SecureStorageTokenService>();
builder.Services.AddScoped<AuthenticationStateProvider, MauiAuthStateProvider>();
builder.Services.AddTransient<MauiAuthHttpHandler>();

Rules:
- Do NOT install any additional NuGet packages — MAUI includes SecureStorage and Connectivity.
- Do NOT modify ZeroDawn.Shared files.
- Report: files created, platform-specific URL strategy, DI registrations.
```

**Expected output**: 4 MAUI service files + updated MauiProgram.cs.
**Acceptance**: Android emulator resolves API via 10.0.2.2. Windows uses localhost.

---

*Continued in [phases_part2.md](file:///C:/Users/Spees/.gemini/antigravity/brain/719bee17-bd6b-4428-a5f1-1a4b127d9705/phases_part2.md)*
