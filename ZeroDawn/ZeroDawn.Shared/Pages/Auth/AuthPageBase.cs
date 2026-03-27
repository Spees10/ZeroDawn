#nullable enable

using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using ZeroDawn.Shared.Contracts.Common;
using ZeroDawn.Shared.Services;

namespace ZeroDawn.Shared.Pages.Auth;

public abstract class AuthPageBase : ComponentBase
{
    [Inject] protected IAuthService AuthService { get; set; } = default!;
    [Inject] protected IToastService ToastService { get; set; } = default!;
    [Inject] protected NavigationManager NavigationManager { get; set; } = default!;
    [Inject] protected AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
    [Inject] protected IStringLocalizer<SharedResources> L { get; set; } = default!;
    [Inject] protected IConfiguration? Configuration { get; set; }

    protected string? ErrorMessage { get; private set; }
    protected string? ErrorReferenceNumber { get; private set; }
    protected string? ErrorTechnicalDetails { get; private set; }

    protected bool AllowSelfRegistration
        => !bool.TryParse(Configuration?["App:AllowSelfRegistration"], out var allowed) || allowed;

    protected bool HasUnhandledError => !string.IsNullOrWhiteSpace(ErrorMessage);

    protected void ClearUnhandledError()
    {
        ErrorMessage = null;
        ErrorReferenceNumber = null;
        ErrorTechnicalDetails = null;
    }

    protected void SetUnhandledError(Exception exception)
    {
        ErrorMessage = L["ErrorOccurred"];
        ErrorReferenceNumber = BuildClientReferenceNumber();
        ErrorTechnicalDetails = exception.ToString();

        var toastMessage = $"{ErrorMessage} ({ErrorReferenceNumber})";
        ToastService.ShowError(toastMessage);
    }

    protected void ShowApiErrors(ApiResponse response)
    {
        if (response.ValidationErrors.Count > 0)
        {
            foreach (var validationError in response.ValidationErrors)
            {
                ToastService.ShowError(validationError);
            }

            return;
        }

        if (!string.IsNullOrWhiteSpace(response.Message))
        {
            var message = response.Message;
            if (!string.IsNullOrWhiteSpace(response.ReferenceNumber))
            {
                message = $"{message} ({response.ReferenceNumber})";
            }

            ToastService.ShowError(message!);
            return;
        }

        if (!string.IsNullOrWhiteSpace(response.Error))
        {
            var message = response.Error;
            if (!string.IsNullOrWhiteSpace(response.ReferenceNumber))
            {
                message = $"{message} ({response.ReferenceNumber})";
            }

            ToastService.ShowError(message);
            return;
        }

        var fallbackMessage = L["ErrorOccurred"].Value;
        if (!string.IsNullOrWhiteSpace(response.ReferenceNumber))
        {
            fallbackMessage = $"{fallbackMessage} ({response.ReferenceNumber})";
        }

        ToastService.ShowError(fallbackMessage);
    }

    protected void ShowSuccess(string fallbackMessage, string? message = null)
    {
        ToastService.ShowSuccess(string.IsNullOrWhiteSpace(message) ? fallbackMessage : message!);
    }

    protected Task NotifyAuthStateChangedAsync()
    {
        var method = AuthenticationStateProvider.GetType().GetMethod(
            "NotifyAuthStateChanged",
            BindingFlags.Instance | BindingFlags.Public,
            binder: null,
            types: Type.EmptyTypes,
            modifiers: null);

        method?.Invoke(AuthenticationStateProvider, null);
        return Task.CompletedTask;
    }

    private static string BuildClientReferenceNumber()
        => $"UI-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..20];
}
