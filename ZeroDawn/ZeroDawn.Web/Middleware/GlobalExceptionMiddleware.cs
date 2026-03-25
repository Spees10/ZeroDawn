#nullable enable

using System.Net;
using System.Security.Claims;
using System.Text.Json;
using ZeroDawn.Shared.Contracts.Common;
using ZeroDawn.Web.Data;

namespace ZeroDawn.Web.Middleware;

public class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, ApplicationDbContext dbContext)
    {
        if (!IsApiRequest(context))
        {
            await next(context);
            return;
        }

        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            var correlationId = context.Items["CorrelationId"]?.ToString() ?? "unknown";
            var correlationSegment = correlationId[..Math.Min(7, correlationId.Length)];
            var referenceNumber = $"ERR-{DateTime.UtcNow:yyyyMMdd}-{correlationSegment}";

            logger.LogError(
                ex,
                "Unhandled exception. Reference: {ReferenceNumber}, Path: {Path}, CorrelationId: {CorrelationId}",
                referenceNumber,
                context.Request.Path,
                correlationId);

            try
            {
                dbContext.ErrorLogs.Add(new ErrorLog
                {
                    ReferenceNumber = referenceNumber,
                    Message = ex.Message,
                    StackTrace = ex.StackTrace,
                    Source = ex.Source,
                    InnerException = ex.InnerException?.Message,
                    UserId = GetUserId(context.User),
                    RequestPath = context.Request.Path,
                    CorrelationId = correlationId
                });

                await dbContext.SaveChangesAsync();
            }
            catch (Exception dbEx)
            {
                logger.LogError(dbEx, "Failed to log error to database. Original ref: {ReferenceNumber}", referenceNumber);
            }

            if (context.Response.HasStarted)
            {
                logger.LogWarning(
                    "The response has already started, the global exception middleware will not write a response. Reference: {ReferenceNumber}",
                    referenceNumber);
                return;
            }

            context.Response.Clear();
            context.Response.Headers["X-Correlation-Id"] = correlationId;
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";

            var isDevOrAdmin = context.RequestServices
                .GetRequiredService<IWebHostEnvironment>()
                .IsDevelopment()
                || context.User?.IsInRole("SuperAdmin") == true;

            var response = new ApiResponse
            {
                Succeeded = false,
                Error = isDevOrAdmin ? ex.Message : "An unexpected error occurred.",
                ErrorCode = "SERVER_ERROR",
                ReferenceNumber = referenceNumber
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(
                response,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
        }
    }

    private static bool IsApiRequest(HttpContext context)
        => context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase);

    private static string? GetUserId(ClaimsPrincipal? user)
        => user?.FindFirst("sub")?.Value
           ?? user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
}
