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

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OptimizelySDK.Cmab
{
    /// <summary>
    /// Interface for CMAB client that fetches decisions from the prediction service.
    /// </summary>
    public interface ICmabClient
    {
        /// <summary>
        /// Fetch a decision (variation id) from CMAB prediction service.
        /// Throws on failure (network/non-2xx/invalid response/exhausted retries).
        /// </summary>
        /// <returns>Variation ID as string.</returns>
        string FetchDecision(
            string ruleId,
            string userId,
            IDictionary<string, object> attributes,
            string cmabUuid,
            TimeSpan? timeout = null);
    }
}
