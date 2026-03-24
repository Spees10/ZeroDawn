#nullable enable

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
