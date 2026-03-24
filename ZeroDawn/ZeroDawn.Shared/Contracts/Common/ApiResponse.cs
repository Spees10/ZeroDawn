#nullable enable

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
