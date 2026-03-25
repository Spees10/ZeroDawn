using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using System.Threading.RateLimiting;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using ZeroDawn.Shared.Services;
using ZeroDawn.Shared.Contracts.Common;
using ZeroDawn.Web.Configuration;
using ZeroDawn.Web.Components;
using ZeroDawn.Web.Data;
using ZeroDawn.Web.Middleware;
using ZeroDawn.Web.Services;

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

    var jwtOptions = builder.Configuration.GetSection(JwtOptions.Section).Get<JwtOptions>()
        ?? throw new InvalidOperationException("Jwt configuration section is not configured.");
    var isJwtConfigured = !string.IsNullOrWhiteSpace(jwtOptions.Secret);

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

    builder.Services.AddScoped<IEmailService, MailKitEmailService>();
    builder.Services.AddScoped<ITokenService, TokenService>();

    if (isJwtConfigured)
    {
        // Re-enable this block when Jwt:Secret is configured.
        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
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
    }

    // Add services to the container.
    builder.Services.AddRazorComponents()
        .AddInteractiveWebAssemblyComponents();

    builder.Services.AddControllers();

    // Add device-specific services used by the ZeroDawn.Shared project
    builder.Services.AddSingleton<IFormFactor, FormFactor>();
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.OnRejected = async (context, cancellationToken) =>
        {
            var httpContext = context.HttpContext;
            if (httpContext.Response.HasStarted)
            {
                return;
            }

            httpContext.Features.Get<Microsoft.AspNetCore.Diagnostics.IStatusCodePagesFeature>()?.Enabled = false;
            httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            httpContext.Response.ContentType = "application/json";

            var response = new ApiResponse
            {
                Succeeded = false,
                Error = "Too many requests. Please try again later.",
                ErrorCode = "RATE_LIMIT_EXCEEDED"
            };

            await httpContext.Response.WriteAsync(
                JsonSerializer.Serialize(
                    response,
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
                cancellationToken);
        };

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

    var app = builder.Build();
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseMiddleware<GlobalExceptionMiddleware>();
    app.UseSerilogRequestLogging(options =>
    {
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("CorrelationId",
                httpContext.Items["CorrelationId"]?.ToString() ?? "N/A");
        };
    });

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseWebAssemblyDebugging();
    }
    else
    {
        app.UseExceptionHandler("/Error", createScopeForErrors: true);
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }
    app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
    app.UseHttpsRedirection();

    app.UseRouting();
    app.UseAntiforgery();
    app.UseRateLimiter();

    if (isJwtConfigured)
    {
        app.UseAuthentication();
        app.UseAuthorization();
    }

    app.MapControllers();

    app.MapStaticAssets();

    app.MapRazorComponents<App>()
        .AddInteractiveWebAssemblyRenderMode()
        .AddAdditionalAssemblies(
            typeof(ZeroDawn.Shared._Imports).Assembly,
            typeof(ZeroDawn.Web.Client._Imports).Assembly);

    await DatabaseSeeder.SeedAsync(app.Services, app.Logger);

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
