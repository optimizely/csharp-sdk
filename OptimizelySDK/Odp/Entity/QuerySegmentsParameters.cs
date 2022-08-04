/* 
 * Copyright 2022 Optimizely
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * https://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text;

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