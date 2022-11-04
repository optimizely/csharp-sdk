using System.Collections.Generic;

namespace OptimizelySDK.Odp
{
    public class IOdpSegmentManager
    {
        public List<string> GetQualifiedSegments(string fsUserId,
            List<OdpSegmentOption> options = null
        );
    }
}
