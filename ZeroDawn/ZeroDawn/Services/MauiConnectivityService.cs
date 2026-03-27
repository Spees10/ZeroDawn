using Microsoft.Maui.Networking;
using ZeroDawn.Shared.Services;

namespace ZeroDawn.Services;

public class MauiConnectivityService : IConnectivityService, IDisposable
{
    public bool IsConnected => Connectivity.Current.NetworkAccess == NetworkAccess.Internet;

    public event EventHandler<bool>? ConnectivityChanged;

    public MauiConnectivityService()
    {
        Connectivity.Current.ConnectivityChanged += OnConnectivityChanged;
    }

    private void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
    {
        ConnectivityChanged?.Invoke(this, e.NetworkAccess == NetworkAccess.Internet);
    }

    public void Dispose()
    {
        Connectivity.Current.ConnectivityChanged -= OnConnectivityChanged;
    }
}
