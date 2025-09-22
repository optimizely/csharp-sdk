/* 
 * Copyright 2017-2019, Optimizely
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
    public class Experiment : ExperimentCore
    {
        private const string MUTEX_GROUP_POLICY = "random";

        /// <summary>
        /// Group ID for the experiment
        /// </summary>
        public string GroupId { get; set; }

        /// <summary>
        /// ForcedVariations for the experiment
        /// </summary>
        public Dictionary<string, string> ForcedVariations { get; set; }

        /// <summary>
        /// ForcedVariations for the experiment
        /// </summary>
        public Dictionary<string, string> UserIdToKeyVariations => ForcedVariations;

        /// <summary>
        /// Policy of the experiment group
        /// </summary>
        public string GroupPolicy { get; set; }

        /// <summary>
        /// Determine if experiment is in a mutually exclusive group
        /// </summary>
        public bool IsInMutexGroup =>
            !string.IsNullOrEmpty(GroupPolicy) && GroupPolicy == MUTEX_GROUP_POLICY;

        /// <summary>
        /// CMAB (Contextual Multi-Armed Bandit) configuration for the experiment.
        /// </summary>
        [JsonProperty("cmab")]
        public Cmab Cmab { get; set; }

        /// <summary>
        /// Determin if user is forced variation of experiment
        /// </summary>
        /// <param name="userId">User ID of the user</param>
        /// <returns>True iff user is in forced variation of experiment</returns>
        public bool IsUserInForcedVariation(string userId)
        {
            return ForcedVariations != null && ForcedVariations.ContainsKey(userId);
        }
    }
}
