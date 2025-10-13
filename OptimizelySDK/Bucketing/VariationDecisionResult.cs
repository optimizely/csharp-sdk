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

using OptimizelySDK.Entity;

namespace OptimizelySDK.Bucketing
{
    /// <summary>
    /// Represents the result of a variation decision, including CMAB-specific fields.
    /// </summary>
    public class VariationDecisionResult
    {
        /// <summary>
        /// The variation selected for the user. Null if no variation was selected.
        /// </summary>
        public Variation Variation { get; set; }

        /// <summary>
        /// The CMAB UUID associated with this decision. Null for non-CMAB experiments.
        /// </summary>
        public string CmabUuid { get; set; }

        /// <summary>
        /// Indicates whether an error occurred during the CMAB decision process.
        /// False for non-CMAB experiments or successful CMAB decisions.
        /// </summary>
        public bool CmabError { get; set; }

        public VariationDecisionResult(Variation variation, string cmabUuid = null, bool cmabError = false)
        {
            Variation = variation;
            CmabUuid = cmabUuid;
            CmabError = cmabError;
        }
    }
}
