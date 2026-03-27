#nullable enable

namespace ZeroDawn.Shared.Contracts.Common;

public class ErrorLogDto
{
    public int Id { get; init; }
    public string ReferenceNumber { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string? StackTrace { get; init; }
    public string? Source { get; init; }
    public string? InnerException { get; init; }
    public string? UserId { get; init; }
    public string? RequestPath { get; init; }
    public string? CorrelationId { get; init; }
    public DateTime CreatedAt { get; init; }
}
