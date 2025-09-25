/* 
* Copyright 2025, Optimizely
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System.Collections.Generic;
using Newtonsoft.Json;

namespace OptimizelySDK.Cmab
{
    internal class CmabAttribute
    {
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("value")] public object Value { get; set; }
        [JsonProperty("type")] public string Type { get; set; } = "custom_attribute";
    }

    internal class CmabInstance
    {
        [JsonProperty("visitorId")] public string VisitorId { get; set; }
        [JsonProperty("experimentId")] public string ExperimentId { get; set; }
        [JsonProperty("attributes")] public List<CmabAttribute> Attributes { get; set; }
        [JsonProperty("cmabUUID")] public string CmabUUID { get; set; }
    }

    internal class CmabRequest
    {
        [JsonProperty("instances")] public List<CmabInstance> Instances { get; set; }
    }
}
