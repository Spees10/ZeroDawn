# ZeroDawn Roadmap — Phases 5–10

## Phase 5 · Logging & Error Handling

**Goal**: Serilog structured logging, correlation IDs, global exception middleware, DB error logging, UI error components with reference numbers.
**Why**: Every error must be traceable, user-facing errors must be safe, and super admins need a debug toggle.
**Prerequisites**: Phase 1 (config), Phase 2 (ErrorLog entity in DB).

### Task 5.1 — Serilog Pipeline + Correlation ID Middleware

**Prompt for AI model:**
```
In ZeroDawn.Web, set up Serilog and correlation ID middleware.

1. Update Program.cs — replace default logging with Serilog.
   At the very top, before CreateBuilder:

   using Serilog;

   Log.Logger = new LoggerConfiguration()
       .WriteTo.Console()
       .CreateBootstrapLogger();

   try
   {
       var builder = WebApplication.CreateBuilder(args);
       builder.Host.UseSerilog((context, services, config) => config
           .ReadFrom.Configuration(context.Configuration)
           .ReadFrom.Services(services)
           .Enrich.FromLogContext()
           .Enrich.WithMachineName()
           .WriteTo.Console(outputTemplate:
               "[{Timestamp:HH:mm:ss} {Level:u3}] {CorrelationId} {Message:lj}{NewLine}{Exception}")
           .WriteTo.MSSqlServer(
               connectionString: context.Configuration["Database:ConnectionString"],
               sinkOptions: new Serilog.Sinks.MSSqlServer.MSSqlServerSinkOptions
               {
                   TableName = "SerilogEntries",
                   AutoCreateSqlTable = true
               },
               restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Warning));

       // ... rest of builder setup ...

       var app = builder.Build();

       app.UseSerilogRequestLogging(options =>
       {
           options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
           {
               diagnosticContext.Set("CorrelationId",
                   httpContext.Items["CorrelationId"]?.ToString() ?? "N/A");
           };
       });

       // ... rest of pipeline ...
       app.Run();
   }
   catch (Exception ex)
   {
       Log.Fatal(ex, "Application terminated unexpectedly");
   }
   finally
   {
       Log.CloseAndFlush();
   }

2. Create Middleware/CorrelationIdMiddleware.cs:
   namespace ZeroDawn.Web.Middleware;

   public class CorrelationIdMiddleware(RequestDelegate next)
   {
       public async Task InvokeAsync(HttpContext context)
       {
           var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault()
               ?? Guid.NewGuid().ToString("N")[..12];
           context.Items["CorrelationId"] = correlationId;
           context.Response.Headers["X-Correlation-Id"] = correlationId;
           using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId))
           {
               await next(context);
           }
       }
   }

   Register BEFORE UseSerilogRequestLogging:
   app.UseMiddleware<CorrelationIdMiddleware>();

Rules:
- Wrap entire Program.cs in try/catch/finally for startup failures.
- Never log secrets (JWT secret, SMTP password, connection string password).
- Preserve ALL existing Program.cs registrations and middleware order.
- Report: middleware registered, Serilog sinks, pipeline order.
```

**Expected output**: Serilog pipeline with console + SQL Server sinks, correlation ID middleware.
**Acceptance**: Logs appear in console with correlation IDs. Serilog auto-creates its table in DB.

---

### Task 5.2 — Global Exception Middleware + ProblemDetails

**Prompt for AI model:**
```
In ZeroDawn.Web/Middleware/, create GlobalExceptionMiddleware.cs:

namespace ZeroDawn.Web.Middleware;

using System.Net;
using System.Text.Json;
using ZeroDawn.Shared.Contracts.Common;
using ZeroDawn.Web.Data;

public class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, ApplicationDbContext dbContext)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            var correlationId = context.Items["CorrelationId"]?.ToString() ?? "unknown";
            var referenceNumber = $"ERR-{DateTime.UtcNow:yyyyMMdd}-{correlationId[..8]}";

            logger.LogError(ex,
                "Unhandled exception. Reference: {ReferenceNumber}, Path: {Path}, CorrelationId: {CorrelationId}",
                referenceNumber, context.Request.Path, correlationId);

            // Try to log to DB, but don't throw if DB logging fails
            try
            {
                dbContext.ErrorLogs.Add(new ErrorLog
                {
                    ReferenceNumber = referenceNumber,
                    Message = ex.Message,
                    StackTrace = ex.StackTrace,
                    Source = ex.Source,
                    InnerException = ex.InnerException?.Message,
                    UserId = context.User?.FindFirst("sub")?.Value,
                    RequestPath = context.Request.Path,
                    CorrelationId = correlationId
                });
                await dbContext.SaveChangesAsync();
            }
            catch (Exception dbEx)
            {
                logger.LogError(dbEx, "Failed to log error to database. Original ref: {ReferenceNumber}", referenceNumber);
            }

            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";

            var isDevOrAdmin = context.RequestServices
                .GetRequiredService<IWebHostEnvironment>().IsDevelopment()
                || context.User?.IsInRole("SuperAdmin") == true;

            var response = new ApiResponse
            {
                Succeeded = false,
                Error = isDevOrAdmin ? ex.Message : "An unexpected error occurred.",
                ErrorCode = "SERVER_ERROR",
                ReferenceNumber = referenceNumber
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
        }
    }
}

Register in Program.cs AFTER UseCorrelationId, BEFORE UseAuthentication:
app.UseMiddleware<GlobalExceptionMiddleware>();

Rules:
- DB logging failure must NOT cause a second unhandled exception.
- Production users see only "An unexpected error occurred." + reference number.
- Dev/SuperAdmin see the actual exception message.
- Stack trace is NEVER in the API response — only in DB logs.
- Preserve all existing middleware order.
- Report: middleware behavior, fallback strategy, files changed.
```

**Expected output**: Global exception middleware with DB fallback.
**Acceptance**: Throwing in any controller returns `ApiResponse` with reference number. Error is logged to DB. If DB fails, error is still logged to console/Serilog.

---

### Task 5.3 — Shared Error Component for UI

**Prompt for AI model:**
```
In ZeroDawn.Shared/Components/Feedback/, create:

1. ErrorDisplay.razor:
A Blazor component that displays errors with two modes:
- Normal user mode: shows safe message + reference number + "contact support" text.
- Debug/Admin mode: shows toggle for technical details (exception message, source, inner exception, stack trace if available, copy-to-clipboard button).

Parameters:
  [Parameter] public string? Message { get; set; }
  [Parameter] public string? ReferenceNumber { get; set; }
  [Parameter] public string? TechnicalDetails { get; set; }
  [Parameter] public bool ShowTechnicalDetails { get; set; }
  [Parameter] public EventCallback OnRetry { get; set; }

Styling: use a CSS isolation file ErrorDisplay.razor.css with:
- Red/orange accent border
- Icon area (use ⚠️ emoji for now, replace with SVG later)
- Rounded corners, subtle shadow
- "Copy Details" button styled as secondary
- Responsive layout

2. Toast.razor:
A simple toast notification component.

Parameters:
  [Parameter] public string Message { get; set; } = "";
  [Parameter] public ToastType Type { get; set; } = ToastType.Info;
  [Parameter] public int DurationMs { get; set; } = 4000;
  [Parameter] public EventCallback OnClose { get; set; }

Create enum in Components/Feedback/ToastType.cs:
  public enum ToastType { Info, Success, Warning, Error }

The toast should auto-dismiss after DurationMs using a Timer.
Style with CSS isolation: slide in from top-right, color coded by type.

3. ToastService.cs in ZeroDawn.Shared/Services/:
  public interface IToastService
  {
      event Action<string, ToastType>? OnShow;
      void ShowInfo(string message);
      void ShowSuccess(string message);
      void ShowWarning(string message);
      void ShowError(string message);
  }

  public class ToastService : IToastService { ... }

4. ToastContainer.razor:
Renders a list of active toasts. Place in MainLayout.
Listens for IToastService.OnShow events.
Auto-removes after duration.

Rules:
- Do NOT use any JavaScript. Blazor-only with CSS animations.
- Do NOT use third-party component libraries.
- Do NOT modify any existing files except to add @using statements to _Imports.razor if needed.
- Keep CSS simple, modern, and dark-mode compatible (use CSS variables).
- Report: components created, how to use them, files changed.
```

**Expected output**: ErrorDisplay, Toast, ToastContainer, ToastService.
**Acceptance**: Toast shows and auto-dismisses. ErrorDisplay shows safe message normally, reveals details when toggled.

---

## Phase 6 · SMTP & Email

**Goal**: MailKit email abstraction with templates for confirmation, password reset, and OTP resend.
**Why**: Auth flows need email sending. Abstracted so the implementation can be swapped.
**Prerequisites**: Phase 2 (Identity generates tokens).

### Task 6.1 — Email Abstraction + MailKit Implementation

**Prompt for AI model:**
```
In ZeroDawn.Web/Services/, create:

1. IEmailService.cs:
namespace ZeroDawn.Web.Services;

public interface IEmailService
{
    Task SendEmailAsync(string toEmail, string subject, string htmlBody);
    Task SendConfirmationEmailAsync(string toEmail, string userName, string confirmationLink);
    Task SendPasswordResetEmailAsync(string toEmail, string userName, string resetLink);
}

2. MailKitEmailService.cs implementing IEmailService:
- Inject IOptions<SmtpOptions> and ILogger<MailKitEmailService>.
- SendEmailAsync: use MailKit's SmtpClient to connect, authenticate, and send.
  Wrap in try/catch. Log success/failure (never log email body or recipient email at Information level — use Debug).
- SendConfirmationEmailAsync: build HTML email body with inline styles (no external CSS).
  Include: app name, user name, confirmation link as a button, expiry note.
  Call SendEmailAsync.
- SendPasswordResetEmailAsync: similar template with reset link.

Register in Program.cs:
builder.Services.AddScoped<IEmailService, MailKitEmailService>();

3. Update AuthController to call IEmailService:
- In register (when RequireEmailConfirmation is true):
  Generate confirmation token, URL-encode it, build link as "{AppOptions.BaseUrl}/confirm-email?userId={user.Id}&token={encodedToken}".
  Call SendConfirmationEmailAsync.
- In forgot-password:
  Generate reset token, build link as "{AppOptions.BaseUrl}/reset-password?email={encodedEmail}&token={encodedToken}".
  Call SendPasswordResetEmailAsync.
- In resend-confirmation:
  Same as register flow.

Rules:
- MailKit SmtpClient must be disposed properly (using statement).
- Never log SMTP password.
- Never log email body content at Info level or above.
- If SMTP fails, log the error but still return success to the user (to prevent email enumeration on forgot-password).
- Email templates use inline CSS only (for email client compatibility).
- Do NOT install additional packages — MailKit is already in csproj.
- Report: files created/modified, template structure, error handling.
```

**Expected output**: Email service + updated auth controller.
**Acceptance**: Registration with email confirmation sends an email. Forgot password sends reset email. SMTP failure is logged but doesn't crash the endpoint.

---

## Phase 7 · Rate Limiting

**Goal**: Apply ASP.NET Core built-in rate limiting to abuse-prone auth endpoints only.
**Why**: Login, register, forgot-password, and resend-confirmation are brute-force targets.
**Prerequisites**: Phase 3 (auth endpoints exist).

### Task 7.1 — Rate Limiting Policies

**Prompt for AI model:**
```
In ZeroDawn.Web/Program.cs, add rate limiting.

Add using:
using System.Threading.RateLimiting;

After builder.Services configuration, add:
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Strict policy for auth endpoints: 5 requests per 60 seconds per IP
    options.AddPolicy("auth-strict", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromSeconds(60),
                QueueLimit = 0
            }));

    // Moderate policy for resend/confirm: 3 requests per 120 seconds per IP
    options.AddPolicy("auth-resend", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 3,
                Window = TimeSpan.FromSeconds(120),
                QueueLimit = 0
            }));
});

In the pipeline, add AFTER UseRouting, BEFORE UseAuthentication:
app.UseRateLimiter();

Then in AuthController, apply attributes:
- [EnableRateLimiting("auth-strict")] on: Login, Register, ForgotPassword, ResetPassword
- [EnableRateLimiting("auth-resend")] on: ResendConfirmation, ConfirmEmail

Do NOT apply rate limiting to: RefreshToken, ChangePassword, Logout (these require valid auth).

Add usings to AuthController:
using Microsoft.AspNetCore.RateLimiting;

Policy rationale table (for documentation):
| Policy | Limit | Window | Applied To | Reason |
|--------|-------|--------|------------|--------|
| auth-strict | 5/min | 60s | login, register, forgot-password, reset-password | Brute force prevention |
| auth-resend | 3/2min | 120s | resend-confirmation, confirm-email | Email abuse prevention |

Rules:
- Do NOT apply rate limiting globally to all endpoints.
- Do NOT rate limit authenticated-only endpoints.
- Preserve all existing middleware order.
- Report: policies created, endpoints decorated, pipeline order.
```

**Expected output**: Two rate limiting policies applied selectively.
**Acceptance**: 6th login attempt within 60s returns 429. Authenticated endpoints are not rate limited.

---

## Phase 8 · API Resilience

**Goal**: Microsoft.Extensions.Http.Resilience pipeline on typed HttpClients — retry only transient/idempotent failures.
**Why**: Network calls from WASM/MAUI to the server can fail transiently.
**Prerequisites**: Phase 4 (typed HttpClient registrations exist).

### Task 8.1 — Install Resilience Package + Typed Client Setup

**Prompt for AI model:**
```
Add to ZeroDawn.Web.Client/ZeroDawn.Web.Client.csproj:
<PackageReference Include="Microsoft.Extensions.Http.Resilience" Version="10.0.0" />

Add to ZeroDawn/ZeroDawn.csproj:
<PackageReference Include="Microsoft.Extensions.Http.Resilience" Version="10.0.0" />

In ZeroDawn.Shared/Services/, create IAuthApiClient.cs:
namespace ZeroDawn.Shared.Services;

using ZeroDawn.Shared.Contracts.Auth;
using ZeroDawn.Shared.Contracts.Common;

public interface IAuthApiClient
{
    Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request);
    Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request);
    Task<ApiResponse> ForgotPasswordAsync(ForgotPasswordRequest request);
    Task<ApiResponse> ResetPasswordAsync(ResetPasswordRequest request);
    Task<ApiResponse> ChangePasswordAsync(ChangePasswordRequest request);
    Task<ApiResponse> ConfirmEmailAsync(ConfirmEmailRequest request);
    Task<ApiResponse> ResendConfirmationAsync(ResendConfirmationRequest request);
    Task<ApiResponse> LogoutAsync();
}

In ZeroDawn.Web.Client/Services/, create AuthApiClient.cs implementing IAuthApiClient:
- Inject HttpClient (via constructor, DI-provided).
- Each method: POST to the correct ApiEndpoints path, serialize request, deserialize response.
- Handle HttpRequestException (network failure): return ApiResponse with error "Network error. Please check your connection."
- Handle non-success status codes: deserialize error response from body.
- Do NOT create new HttpClient() — use the injected one.

Register typed HttpClient in ZeroDawn.Web.Client/Program.cs:

builder.Services.AddHttpClient<IAuthApiClient, AuthApiClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"]
        ?? throw new InvalidOperationException("ApiBaseUrl not configured"));
})
.AddHttpMessageHandler<AuthHttpHandler>()
.AddStandardResilienceHandler(options =>
{
    // Retry: 3 attempts, only for transient failures (5xx, 408, network errors)
    options.Retry.MaxRetryAttempts = 3;
    options.Retry.BackoffType = DelayBackoffType.Exponential;
    options.Retry.UseJitter = true;
    options.Retry.ShouldHandle = args => ValueTask.FromResult(
        args.Outcome.Result?.StatusCode is >= System.Net.HttpStatusCode.InternalServerError
        || args.Outcome.Result?.StatusCode is System.Net.HttpStatusCode.RequestTimeout
        || args.Outcome.Exception is HttpRequestException);

    // Total request timeout: 30 seconds
    options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30);

    // Circuit breaker: open after 5 failures in 30 seconds
    options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
    options.CircuitBreaker.FailureRatio = 0.5;
    options.CircuitBreaker.MinimumThroughput = 5;
});

CRITICAL RULES — what the resilience pipeline must NOT retry:
- 400 Bad Request (validation errors) — NOT transient
- 401 Unauthorized — handled by AuthHttpHandler, NOT the resilience pipeline
- 403 Forbidden — business rule, NOT transient
- 404 Not Found — NOT transient
- 409 Conflict — business rule, NOT transient
- 422 Unprocessable Entity — NOT transient
- Non-idempotent POST (login, register) — DO NOT RETRY.
  To handle this, the AuthApiClient POST methods should set a custom header
  "X-Idempotent: false" on non-idempotent requests, and the ShouldHandle
  predicate should check for this header and skip retry.

ACTUALLY, simpler approach: Do NOT use AddStandardResilienceHandler on the auth client.
Instead, only add resilience to read-only API clients (users list, error logs, profile GET).
Auth operations are NOT safe to retry automatically.

So the corrected registration is:
builder.Services.AddHttpClient<IAuthApiClient, AuthApiClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"]!);
})
.AddHttpMessageHandler<AuthHttpHandler>();
// NO resilience handler on auth client — auth operations are not idempotent

For a future IUserApiClient (GET operations), resilience IS appropriate.

Rules:
- Do NOT add resilience to POST-heavy auth clients.
- DO add resilience to GET-heavy read clients (implemented in later phases).
- Do NOT create new HttpClient() anywhere.
- Report: packages added, clients registered, resilience rationale, files changed.
```

**Expected output**: IAuthApiClient interface + implementation + typed HttpClient registration without resilience. Resilience pattern documented for future read-only clients.
**Risks**: Accidentally retrying non-idempotent POSTs.
**Acceptance**: Auth API calls work via typed client. No retry on auth POST operations.

---

## Phase 9 · Validation

**Goal**: DataAnnotations on DTOs (already done in Phase 0.6), server-side ModelState validation via `[ApiController]`, UI validation via Blazor `EditForm`.
**Why**: Validation must happen at both boundaries — UI for UX, server for security.
**Prerequisites**: Phase 0 (DTOs with DataAnnotations exist).

### Task 9.1 — Validation Messages Constants + Custom Validators

**Prompt for AI model:**
```
In ZeroDawn.Shared/Contracts/Validation/, create:

1. ValidationMessages.cs:
namespace ZeroDawn.Shared.Contracts.Validation;

public static class ValidationMessages
{
    public const string Required = "{0} is required.";
    public const string EmailInvalid = "Invalid email address.";
    public const string PasswordTooShort = "Password must be at least {1} characters.";
    public const string PasswordsDoNotMatch = "Passwords do not match.";
    public const string NameTooLong = "Name cannot exceed {1} characters.";
}

2. PasswordStrengthAttribute.cs (optional custom validator):
using System.ComponentModel.DataAnnotations;

namespace ZeroDawn.Shared.Contracts.Validation;

[AttributeUsage(AttributeTargets.Property)]
public class PasswordStrengthAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext context)
    {
        if (value is not string password) return ValidationResult.Success;

        if (password.Length < 6)
            return new ValidationResult("Password must be at least 6 characters.");
        if (!password.Any(char.IsDigit))
            return new ValidationResult("Password must contain at least one digit.");
        if (!password.Any(char.IsUpper))
            return new ValidationResult("Password must contain at least one uppercase letter.");
        if (!password.Any(char.IsLower))
            return new ValidationResult("Password must contain at least one lowercase letter.");

        return ValidationResult.Success;
    }
}

Then update the auth DTOs (LoginRequest, RegisterRequest, ResetPasswordRequest, ChangePasswordRequest)
to use [PasswordStrength] on password fields instead of just [MinLength(6)].
Use ValidationMessages constants for error messages in [Required] and [MaxLength].

Server-side: [ApiController] on AuthController already returns 400 automatically for invalid ModelState.
UI-side: Blazor <EditForm> with <DataAnnotationsValidator /> will use these same attributes.

Rules:
- Do NOT use FluentValidation. DataAnnotations is the chosen strategy.
- Do NOT modify controllers (validation is automatic via [ApiController]).
- Do NOT modify any files other than the ones specified.
- Report: validation messages, custom attribute, updated DTOs.
```

**Expected output**: Validation constants, custom attribute, updated DTOs.
**Acceptance**: Same validation rules work in Blazor EditForm AND server-side ModelState.

---

## Phase 10 · Localization

**Goal**: `IStringLocalizer<T>` with `.resx` resource files only. No third-party packages.
**Why**: Multi-language support must be built-in from the start. Using built-in .NET localization avoids dependency on unmaintained packages.
**Prerequisites**: Phase 0.

### Task 10.1 — Localization Setup

**Prompt for AI model:**
```
Set up localization using built-in .NET IStringLocalizer with .resx files.

1. In ZeroDawn.Shared/Localization/, create:
   - Resources/ folder
   - Resources/SharedResources.resx (default/English)
   - Resources/SharedResources.ar.resx (Arabic as example second language)

   SharedResources.resx entries (key → value):
   AppName → ZeroDawn
   Login → Login
   Register → Register
   Email → Email
   Password → Password
   ConfirmPassword → Confirm Password
   ForgotPassword → Forgot Password?
   ResetPassword → Reset Password
   ChangePassword → Change Password
   FullName → Full Name
   Submit → Submit
   Cancel → Cancel
   Save → Save
   Delete → Delete
   Edit → Edit
   Search → Search
   Loading → Loading...
   NoResults → No results found.
   ErrorOccurred → An error occurred. Please try again.
   ContactSupport → If the problem persists, contact support with reference:
   Home → Home
   Dashboard → Dashboard
   Profile → Profile
   Users → Users
   Settings → Settings
   Logout → Logout
   About → About
   ContactUs → Contact Us
   NotFound → Page not found
   Welcome → Welcome, {0}!
   ConfirmEmail → Confirm Email
   ResendEmail → Resend Confirmation
   Back → Back

   SharedResources.ar.resx: Arabic translations for each key.

2. Create ZeroDawn.Shared/Localization/SharedResources.cs:
   namespace ZeroDawn.Shared.Localization;
   // Marker class for IStringLocalizer<SharedResources>
   public class SharedResources { }

3. In ZeroDawn.Web/Program.cs, add localization services:
   builder.Services.AddLocalization(options =>
       options.ResourcesPath = "");

   // Supported cultures
   var supportedCultures = new[] { "en", "ar" };
   builder.Services.Configure<RequestLocalizationOptions>(options =>
   {
       options.SetDefaultCulture("en");
       options.AddSupportedCultures(supportedCultures);
       options.AddSupportedUICultures(supportedCultures);
   });

   In pipeline, after UseRouting:
   app.UseRequestLocalization();

4. In ZeroDawn.Shared/_Imports.razor, add:
   @using Microsoft.Extensions.Localization
   @using ZeroDawn.Shared.Localization

Usage in any Razor component:
   @inject IStringLocalizer<SharedResources> L
   <h1>@L["Dashboard"]</h1>
   <p>@L["Welcome", userName]</p>

Rules:
- Use .resx files (built-in .NET), NOT aksoftware.multilanguage.blazor.
- .resx files live in ZeroDawn.Shared so both WASM and MAUI can access them.
- Language switching UI will come in Phase 11 (nav menu component).
- Do NOT modify any other files.
- Report: resx files, marker class, DI setup, usage example.
```

**Expected output**: Localization scaffold with English + Arabic resx files.
**Acceptance**: `@L["Login"]` renders "Login" in English, Arabic equivalent when culture is "ar".

---

*Continued in [phases_part3.md](file:///C:/Users/Spees/.gemini/antigravity/brain/719bee17-bd6b-4428-a5f1-1a4b127d9705/phases_part3.md)*
