using OptimizelySDK.Odp.Entity;
using System.Collections.Generic;

namespace OptimizelySDK.Odp
{
    public interface IGraphQLManager
    {
        Response ParseSegmentsResponseJson(string jsonResponse);
    }
}
