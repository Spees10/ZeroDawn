#nullable enable

namespace ZeroDawn.Shared.Contracts.Common;

public class ApiResponse
{
    public bool Succeeded { get; init; }
    public string? Message { get; init; }
    public string? Error { get; init; }
    public string? ErrorCode { get; init; }
    public List<string> ValidationErrors { get; init; } = [];
    public string? ReferenceNumber { get; init; }
}

public class ApiResponse<T> : ApiResponse
{
    public T? Data { get; init; }
}
