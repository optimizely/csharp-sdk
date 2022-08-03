using Newtonsoft.Json;
using OptimizelySDK.Logger;
using OptimizelySDK.Odp.Entities;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace OptimizelySDK.Odp
{
    public class GraphQLManager : IGraphQLManager
    {
        private readonly ILogger Logger;

        public GraphQLManager(ILogger logger)
        {
            Logger = logger;
        }
        
        public Response ParseResponse(string jsonResponse)
        {
            var json = Regex.Replace(jsonResponse, @"\s+", string.Empty);
            
            return JsonConvert.DeserializeObject<Response>(json);
        }

        public string FetchSegments(string apiKey, string apiHost, string userKey, string userValue, List<string> segmentToCheck)
        {
            
        }
    }
}
