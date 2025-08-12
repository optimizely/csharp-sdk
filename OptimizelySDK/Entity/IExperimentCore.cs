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
using OptimizelySDK.AudienceConditions;

namespace OptimizelySDK.Entity
{
    /// <summary>
    /// Interface defining common properties and behaviors shared between Experiment and Holdout
    /// </summary>
    public interface IExperimentCore
    {
        /// <summary>
        /// Entity ID
        /// </summary>
        string Id { get; set; }

        /// <summary>
        /// Entity Key
        /// </summary>
        string Key { get; set; }

        /// <summary>
        /// Status of the experiment/holdout
        /// </summary>
        string Status { get; set; }

        /// <summary>
        /// Layer ID for the experiment/holdout
        /// </summary>
        string LayerId { get; set; }

        /// <summary>
        /// Variations for the experiment/holdout
        /// </summary>
        Variation[] Variations { get; set; }

        /// <summary>
        /// Traffic allocation of variations in the experiment/holdout
        /// </summary>
        TrafficAllocation[] TrafficAllocation { get; set; }

        /// <summary>
        /// ID(s) of audience(s) the experiment/holdout is targeted to
        /// </summary>
        string[] AudienceIds { get; set; }

        /// <summary>
        /// Audience Conditions
        /// </summary>
        object AudienceConditions { get; set; }

        /// <summary>
        /// De-serialized audience conditions
        /// </summary>
        ICondition AudienceConditionsList { get; }

        /// <summary>
        /// Stringified audience conditions
        /// </summary>
        string AudienceConditionsString { get; }

        /// <summary>
        /// De-serialized audience conditions from audience IDs
        /// </summary>
        ICondition AudienceIdsList { get; }

        /// <summary>
        /// Stringified audience IDs
        /// </summary>
        string AudienceIdsString { get; }

        /// <summary>
        /// Variation key to variation mapping
        /// </summary>
        Dictionary<string, Variation> VariationKeyToVariationMap { get; }

        /// <summary>
        /// Variation ID to variation mapping
        /// </summary>
        Dictionary<string, Variation> VariationIdToVariationMap { get; }

        /// <summary>
        /// Determine if experiment/holdout is currently activated/running
        /// </summary>
        bool IsActivated { get; }

        /// <summary>
        /// Generate variation key maps for performance optimization
        /// </summary>
        void GenerateVariationKeyMap();
    }
}
