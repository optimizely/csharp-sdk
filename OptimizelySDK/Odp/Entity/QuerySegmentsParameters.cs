using System.Collections.Generic;

namespace OptimizelySDK.Odp.Entity
{
    public class QuerySegmentsParameters
    {
        public string ApiKey { get; set; }
        public string ApiHost { get; set; }
        public string UserKey { get; set; }
        public string UserValue { get; set; }
        public List<string> SegmentToCheck { get; set; }
    }
}