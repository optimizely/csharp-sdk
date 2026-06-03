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

        /// <summary>
        /// Optional array of rule IDs that this holdout targets (local holdout).
        /// When null, the holdout applies to all rules across all flags (global holdout).
        /// When set to an array (even empty), the holdout only applies to the specified rules.
        /// Rule IDs in this array are experiment/delivery rule IDs from the datafile, NOT flag IDs.
        /// </summary>
        public string[] IncludedRules { get; set; }

        /// <summary>
        /// Returns true if this is a global holdout (IncludedRules is null),
        /// false if this is a local holdout (IncludedRules is a non-null array).
        /// </summary>
        public bool IsGlobal => IncludedRules == null;
    }
}
