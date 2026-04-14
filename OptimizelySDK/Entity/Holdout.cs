/*
 * Copyright 2025-2026, Optimizely
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

namespace OptimizelySDK.Entity
{
    /// <summary>
    /// Represents a holdout in an Optimizely project
    /// </summary>
    public class Holdout : ExperimentCore
    {
        /// <summary>
        /// Holdout status enumeration
        /// </summary>
        public enum HoldoutStatus
        {
            Draft,
            Running,
            Concluded,
            Archived
        }

        /// <summary>
        /// Rule IDs included in this holdout. If null, this is a global holdout that applies to all rules.
        /// If empty array, this is a local holdout with no rules (edge case).
        /// If populated, this is a local holdout that applies only to the specified rules.
        /// </summary>
        public string[] IncludedRules { get; set; }

        /// <summary>
        /// Returns true if this is a global holdout (applies to all rules), false if it's a local holdout (specific rules only).
        /// </summary>
        public bool IsGlobal()
        {
            return IncludedRules == null;
        }

        /// <summary>
        /// Layer ID is always empty for holdouts as they don't belong to any layer
        /// </summary>
        public override string LayerId
        {
            get => string.Empty;
            set
            {
                /* Holdouts don't have layer IDs, ignore any assignment */
            }
        }
    }
}
