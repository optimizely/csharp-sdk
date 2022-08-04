using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace OptimizelySDK.Odp.Entity
{
    public class QuerySegmentsParameters
    {
        public string ApiKey { get; set; }
        public string ApiHost { get; set; }
        public string UserKey { get; set; }
        public string UserValue { get; set; }
        public List<string> SegmentToCheck { get; set; }
        
        public string ToJson()
        {
            var segmentsArryJson =
                JsonConvert.SerializeObject(SegmentToCheck).Replace("\"", "\\\"");
            var userValueWithEscapedQuotes = $"\\\"{UserValue}\\\"";

            var json = new StringBuilder();
            json.Append("{\"query\" : \"query {customer");
            json.Append($"({UserKey} : {userValueWithEscapedQuotes}) ");
            json.Append("{audiences");
            json.Append($"(subset: {segmentsArryJson})");
            json.Append("{edges {node {name state}}}}}\"}");

            return json.ToString();
        }
    }
}