#nullable enable

using ZeroDawn.Shared.Services;

namespace ZeroDawn.Web.Client.Services;

public class WebConnectivityService : IConnectivityService
{
    public bool IsConnected => true;

    public event EventHandler<bool>? ConnectivityChanged
    {
        add { }
        remove { }
    }
}
