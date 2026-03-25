using ZeroDawn.Shared.Components.Feedback;

namespace ZeroDawn.Shared.Services;

public interface IToastService
{
    event Action<string, ToastType>? OnShow;
    void ShowInfo(string message);
    void ShowSuccess(string message);
    void ShowWarning(string message);
    void ShowError(string message);
}

public class ToastService : IToastService
{
    public event Action<string, ToastType>? OnShow;

    public void ShowInfo(string message) => OnShow?.Invoke(message, ToastType.Info);
    public void ShowSuccess(string message) => OnShow?.Invoke(message, ToastType.Success);
    public void ShowWarning(string message) => OnShow?.Invoke(message, ToastType.Warning);
    public void ShowError(string message) => OnShow?.Invoke(message, ToastType.Error);
}
