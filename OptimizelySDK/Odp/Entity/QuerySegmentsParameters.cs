using Newtonsoft.Json;
using System.Collections.Generic;
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
            var json = @$"
{{ 
    ""query"" : ""
        query {{ 
            customer({UserKey}: ""{UserValue}"") {{
                audiences(subset: {JsonConvert.SerializeObject(SegmentToCheck)}) {{
                    edges {{
                        node {{
                            name 
                            state
                        }}
                    }}
                }}
            }}
        }}""
}}";
            return Regex.Replace(json, @"\s", string.Empty);
        }
    }
}