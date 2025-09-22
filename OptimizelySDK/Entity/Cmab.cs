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

namespace OptimizelySDK.Entity
{
    /// <summary>
    /// Class representing CMAB (Contextual Multi-Armed Bandit) configuration for experiments.
    /// </summary>
    public class Cmab
    {
        /// <summary>
        /// List of attribute IDs that are relevant for CMAB decision making.
        /// These attributes will be used to filter user attributes when making CMAB requests.
        /// </summary>
        [JsonProperty("attributeIds")]
        public List<string> AttributeIds { get; set; }

        /// <summary>
        /// Traffic allocation value for CMAB experiments.
        /// Determines what portion of traffic should be allocated to CMAB decision making.
        /// </summary>
        [JsonProperty("trafficAllocation")]
        public int? TrafficAllocation { get; set; }

        /// <summary>
        /// Initializes a new instance of the Cmab class with specified values.
        /// </summary>
        /// <param name="attributeIds">List of attribute IDs for CMAB</param>
        /// <param name="trafficAllocation">Traffic allocation value</param>
        public Cmab(List<string> attributeIds, int? trafficAllocation = null)
        {
            AttributeIds = attributeIds ?? new List<string>();
            TrafficAllocation = trafficAllocation;
        }

        /// <summary>
        /// Returns a string representation of the CMAB configuration.
        /// </summary>
        /// <returns>String representation</returns>
        public override string ToString()
        {
            var attributeList = AttributeIds ?? new List<string>();
            return string.Format("Cmab{{AttributeIds=[{0}], TrafficAllocation={1}}}",
                string.Join(", ", attributeList.ToArray()), TrafficAllocation);
        }
    }
}
