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
    public interface IOdpConfig
    {
        /// <summary>
        /// Public API key for the ODP account from which the audience segments will be fetched (optional).
        /// </summary>
        string ApiKey { get; }

        /// <summary>
        /// Host of ODP audience segments API.
        /// </summary>
        string ApiHost { get; }

        /// <summary>
        /// All ODP segments used in the current datafile (associated with apiHost/apiKey).
        /// </summary>
        List<string> SegmentsToCheck { get; }

        /// <summary>
        /// Update the ODP configuration details
        /// </summary>
        /// <param name="apiKey">Public API key for the ODP account</param>
        /// <param name="apiHost">Host of ODP audience segments API</param>
        /// <param name="segmentsToCheck">Audience segments</param>
        /// <returns>true if configuration was updated successfully otherwise false</returns>
        bool Update(string apiKey, string apiHost, List<string> segmentsToCheck);

        /// <summary>
        /// Determines if ODP configuration has the minimum amount of information
        /// </summary>
        /// <returns>true if ODP configuration can be used otherwise false</returns>
        bool IsReady();
    }
}
