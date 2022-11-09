/* 
 * Copyright 2022, Optimizely
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

using System.Collections.Generic;

namespace OptimizelySDK.Odp
{
    public class OdpConfig
    {
        /// <summary>
        /// Public API key for the ODP account from which the audience segments will be fetched (optional).
        /// </summary>
        public string ApiKey { get; private set; }

        /// <summary>
        /// Host of ODP audience segments API.
        /// </summary>
        public string ApiHost { get; private set; }

        /// <summary>
        /// All ODP segments used in the current datafile (associated with apiHost/apiKey).
        /// </summary>
        public List<string> SegmentsToCheck { get; private set; }

        public OdpConfig(string apiKey, string apiHost, List<string> segmentsToCheck)
        {
            ApiKey = apiKey;
            ApiHost = apiHost;
            SegmentsToCheck = segmentsToCheck ?? new List<string>(0);
        }

        /// <summary>
        /// Update the ODP configuration details
        /// </summary>
        /// <param name="apiKey">Public API key for the ODP account</param>
        /// <param name="apiHost">Host of ODP audience segments API</param>
        /// <param name="segmentsToCheck">Audience segments</param>
        /// <returns>true if configuration was updated successfully otherwise false</returns>
        public bool Update(string apiKey, string apiHost, List<string> segmentsToCheck)
        {
            if (ApiKey == apiKey && ApiHost == apiHost && SegmentsToCheck == segmentsToCheck)
            {
                return false;
            }

            ApiKey = apiKey;
            ApiHost = apiHost;
            SegmentsToCheck = segmentsToCheck;

            return true;
        }

        /// <summary>
        /// Determines if ODP configuration has the minimum amount of information
        /// </summary>
        /// <returns>true if ODP configuration can be used otherwise false</returns>
        public bool IsReady()
        {
            return !string.IsNullOrWhiteSpace(ApiKey) && !string.IsNullOrWhiteSpace(ApiHost);
        }
    }
}
