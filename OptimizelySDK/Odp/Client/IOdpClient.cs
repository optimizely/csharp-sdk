using OptimizelySDK.Logger;
using OptimizelySDK.Odp.Entity;

namespace OptimizelySDK.Odp.Client
{
    public interface IOdpClient
    {
        string QuerySegments(QuerySegmentsParameters parameters);
    }
}