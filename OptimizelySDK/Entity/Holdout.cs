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
        /// Per-rule targeting for local holdouts. Scope comes from the datafile
        /// section, not this field; DatafileProjectConfig strips it on entries
        /// from the 'holdouts' section so they remain unambiguously global.
        /// Required (non-null) on entries from the 'localHoldouts' section.
        /// </summary>
        public string[] IncludedRules { get; set; }

        /// <summary>
        /// True if this is a global holdout (IncludedRules is null).
        /// Scope is set by the datafile section ('holdouts' vs 'localHoldouts');
        /// DatafileProjectConfig strips 'includedRules' on 'holdouts' entries, so
        /// this property stays consistent with section membership.
        /// </summary>
        public bool IsGlobal => IncludedRules == null;
    }
}
