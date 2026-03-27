using System.Net.Security;

namespace ZeroDawn.Platforms.Android;

public static class DevHttpsConnectionHelper
{
    public static HttpMessageHandler GetPlatformMessageHandler()
    {
        var handler = new HttpClientHandler();
#if DEBUG
        handler.ServerCertificateCustomValidationCallback = (_, cert, _, errors) =>
        {
            if (cert?.Issuer == "CN=localhost")
            {
                return true;
            }

            return errors == SslPolicyErrors.None;
        };
#endif
        return handler;
    }
}
