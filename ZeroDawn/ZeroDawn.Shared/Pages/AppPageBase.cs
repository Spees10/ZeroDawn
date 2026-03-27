#nullable enable

using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using ZeroDawn.Shared.Contracts.Common;
using ZeroDawn.Shared.Services;

namespace ZeroDawn.Shared.Pages;

public abstract class AppPageBase : ComponentBase
{
    [Inject] protected IServiceProvider Services { get; set; } = default!;
    [Inject] protected IToastService ToastService { get; set; } = default!;
    [Inject] protected IStringLocalizer<SharedResources> L { get; set; } = default!;
    [Inject] protected NavigationManager NavigationManager { get; set; } = default!;
    [Inject] protected AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

    protected string? ErrorMessage { get; private set; }
    protected string? ErrorReferenceNumber { get; private set; }
    protected string? ErrorTechnicalDetails { get; private set; }

    protected bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    protected IUserApiClient? UserApiClient => Services.GetService<IUserApiClient>();
    protected IAdminApiClient? AdminApiClient => Services.GetService<IAdminApiClient>();

    protected void ClearError()
    {
        ErrorMessage = null;
        ErrorReferenceNumber = null;
        ErrorTechnicalDetails = null;
    }

    protected void SetError(Exception exception)
    {
        ErrorMessage = L["ErrorOccurred"];
        ErrorReferenceNumber = BuildClientReferenceNumber();
        ErrorTechnicalDetails = exception.ToString();
    }

    protected void SetApiError(ApiResponse response)
    {
        ErrorMessage = string.IsNullOrWhiteSpace(response.Error) ? L["ErrorOccurred"] : response.Error;
        ErrorReferenceNumber = response.ReferenceNumber;
    }

    protected void ShowApiErrors(ApiResponse response)
    {
        if (response.ValidationErrors.Count > 0)
        {
            foreach (var error in response.ValidationErrors)
            {
                ToastService.ShowError(error);
            }

            return;
        }

        var message = string.IsNullOrWhiteSpace(response.Error) ? L["ErrorOccurred"].Value : response.Error!;
        if (!string.IsNullOrWhiteSpace(response.ReferenceNumber))
        {
            message = $"{message} ({response.ReferenceNumber})";
        }

        ToastService.ShowError(message);
    }

    protected void ShowSuccess(string message)
    {
        ToastService.ShowSuccess(message);
    }

    protected async Task<ClaimsPrincipal> GetCurrentUserAsync()
    {
        var state = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        return state.User;
    }

    protected static string GetDisplayName(ClaimsPrincipal user)
        => user.FindFirst("fullName")?.Value
           ?? user.Identity?.Name
           ?? user.FindFirst(ClaimTypes.Name)?.Value
           ?? "مستخدم";

    protected static string? GetRole(ClaimsPrincipal user)
    {
        if (user.IsInRole("SuperAdmin"))
        {
            return "SuperAdmin";
        }

        if (user.IsInRole("Admin"))
        {
            return "Admin";
        }

        if (user.IsInRole("User"))
        {
            return "User";
        }

        return null;
    }

    protected static string GetInitials(string text)
    {
        var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            return "م";
        }

        return string.Concat(parts.Take(2).Select(part => char.ToUpperInvariant(part[0])));
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
