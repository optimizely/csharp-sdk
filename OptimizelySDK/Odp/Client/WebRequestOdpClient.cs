using OptimizelySDK.Logger;
using System.Collections.Generic;

namespace OptimizelySDK.Odp.Client
{
    public class WebRequestOdpClient : IOdpClient
    {
        public ILogger Logger { get; set; }

        public string QuerySegments(string apiKey, string apiHost, string userKey, string userValue,
            List<string> segmentToCheck
        )
        {
            throw new System.NotImplementedException();
        }
    }
}