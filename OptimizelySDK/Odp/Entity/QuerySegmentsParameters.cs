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
    /// <summary>
    /// Handles parameters used in querying ODP segments
    /// </summary>
    public class QuerySegmentsParameters
    {
        /// <summary>
        /// Optimizely Data Platform API key
        /// </summary>
        public string ApiKey { get; set; }
        
        /// <summary>
        /// Fully-qualified URL to ODP endpoint 
        /// </summary>
        public string ApiHost { get; set; }
        
        /// <summary>
        /// 'vuid' or 'fs_user_id' (client device id or fullstack id)
        /// </summary>
        public string UserKey { get; set; }
        
        /// <summary>
        /// Value for the user key
        /// </summary>
        public string UserValue { get; set; }
        
        /// <summary>
        /// Audience segments to check for inclusion in the experiment
        /// </summary>
        public List<string> SegmentToCheck { get; set; }
        
        /// <summary>
        /// Converts the QuerySegmentsParameters into JSON
        /// </summary>
        /// <returns>GraphQL JSON payload</returns>
        public string ToJson()
        {
            var segmentsArrayJson =
                JsonConvert.SerializeObject(SegmentToCheck).Replace("\"", "\\\"");
            var userValueWithEscapedQuotes = $"\\\"{UserValue}\\\"";

            var json = new StringBuilder();
            json.Append("{\"query\" : \"query {customer");
            json.Append($"({UserKey} : {userValueWithEscapedQuotes}) ");
            json.Append("{audiences");
            json.Append($"(subset: {segmentsArrayJson})");
            json.Append("{edges {node {name state}}}}}\"}");

            return json.ToString();
        }
    }
}
