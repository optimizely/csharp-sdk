using OptimizelySDK.Odp.Entities;
using System.Collections.Generic;

namespace OptimizelySDK.Odp
{
    public interface IGraphQLManager
    {
        Response ParseResponse(string jsonResponse);
        string FetchSegments(string apiKey, string apiHost, string userKey, string userValue,
            List<string> segmentToCheck
        );
    }
}
