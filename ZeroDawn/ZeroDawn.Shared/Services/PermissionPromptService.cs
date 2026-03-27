namespace ZeroDawn.Shared.Services;

public interface IPermissionPromptService
{
    event Action<PermissionPromptRequest>? OnRequest;
    Task<bool> RequestAsync(string title, string message, string? confirmText = null, string? cancelText = null);
}

public sealed class PermissionPromptService : IPermissionPromptService
{
    public event Action<PermissionPromptRequest>? OnRequest;

    public Task<bool> RequestAsync(string title, string message, string? confirmText = null, string? cancelText = null)
    {
        var request = new PermissionPromptRequest(title, message, confirmText, cancelText);
        OnRequest?.Invoke(request);
        return request.Completion.Task;
    }
}

public sealed class PermissionPromptRequest
{
    public PermissionPromptRequest(string title, string message, string? confirmText, string? cancelText)
    {
        Title = title;
        Message = message;
        ConfirmText = confirmText;
        CancelText = cancelText;
    }

    public string Title { get; }
    public string Message { get; }
    public string? ConfirmText { get; }
    public string? CancelText { get; }
    public TaskCompletionSource<bool> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
}
