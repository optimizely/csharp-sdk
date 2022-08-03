using Newtonsoft.Json;
using OptimizelySDK.Logger;
using OptimizelySDK.Odp.Entity;
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
        
        public string[] FetchSegments(string apiKey, string apiHost, string userKey, string userValue, List<string> segmentToCheck)
        {
            
            return new string[0];
        }
        
        public Response ParseJson(string jsonResponse)
        {
            var json = Regex.Replace(jsonResponse, @"\s+", string.Empty);
            
            return JsonConvert.DeserializeObject<Response>(json);
        }

    }
}
