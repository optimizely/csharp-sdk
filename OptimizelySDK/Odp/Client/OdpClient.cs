using OptimizelySDK.Logger;

namespace OptimizelySDK.Odp.Client
{
    public class OdpClient :
#if NET35 || NET40
        WebRequestOdpClient
#else
        HttpClientOdpClient
#endif 
    {
        public OdpClient(ILogger logger)
        {
            Logger = logger;
        }
    }
}