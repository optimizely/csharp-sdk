using OptimizelySDK.Logger;
using OptimizelySDK.Odp.Entity;

namespace OptimizelySDK.Odp.Client
{
    public interface IOdpClient
    {
        ILogger Logger { get; set; }
        string QuerySegments(QuerySegmentsParameters);
    }
}