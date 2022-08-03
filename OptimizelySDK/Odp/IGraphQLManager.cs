using OptimizelySDK.Odp.Entity;
using System.Collections.Generic;

namespace OptimizelySDK.Odp
{
    public interface IGraphQLManager
    {
        string[] FetchSegments(string apiKey, string apiHost, string userKey, string userValue,
            List<string> segmentToCheck
        );
        
        Response ParseJson(string jsonResponse);
    }
}
