using System.Collections.Generic;

namespace OptimizelySDK.Odp
{
    public interface IOdpSegmentManager
    {
        List<string> GetQualifiedSegments(string fsUserId, List<OdpSegmentOption> options = null);
    }
}
