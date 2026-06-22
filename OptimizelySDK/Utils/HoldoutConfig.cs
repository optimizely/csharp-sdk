/*
 * Copyright 2025-2026, Optimizely
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
using System.Linq;
using OptimizelySDK.Entity;

namespace OptimizelySDK.Utils
{
    /// <summary>
    /// Configuration manager for holdouts, providing holdout ID mapping,
    /// global holdout access, and rule-level local holdout lookups.
    /// Scope (global vs local) is determined by the IsGlobal property set at parse time,
    /// which reflects section membership in the datafile.
    /// </summary>
    public class HoldoutConfig
    {
        private List<Holdout> _allHoldouts;
        private readonly Dictionary<string, Holdout> _holdoutIdMap;

        /// <summary>
        /// Global holdouts — holdouts from the "holdouts" section (IsGlobal == true).
        /// These are evaluated at flag level, before any per-rule logic.
        /// </summary>
        private readonly List<Holdout> _globalHoldouts;

        /// <summary>
        /// Maps rule IDs to the local holdouts that target those rules.
        /// Local holdouts are from the "localHoldouts" section (IsGlobal == false)
        /// and use their IncludedRules array for rule targeting.
        /// </summary>
        private readonly Dictionary<string, List<Holdout>> _ruleHoldoutsMap;

        /// <summary>
        /// Initializes a new instance of the HoldoutConfig class.
        /// </summary>
        /// <param name="allHoldouts">Array of all holdouts from the datafile</param>
        public HoldoutConfig(Holdout[] allHoldouts = null)
        {
            _allHoldouts = allHoldouts?.ToList() ?? new List<Holdout>();
            _holdoutIdMap = new Dictionary<string, Holdout>();
            _globalHoldouts = new List<Holdout>();
            _ruleHoldoutsMap = new Dictionary<string, List<Holdout>>();

            UpdateHoldoutMapping();
        }

        /// <summary>
        /// Gets a read-only dictionary mapping holdout IDs to holdout instances.
        /// </summary>
        public IDictionary<string, Holdout> HoldoutIdMap => _holdoutIdMap;

        /// <summary>
        /// Updates internal mappings of holdouts: ID map, global list, and rule-to-holdout map.
        /// </summary>
        private void UpdateHoldoutMapping()
        {
            // Clear existing mappings
            _holdoutIdMap.Clear();
            _globalHoldouts.Clear();
            _ruleHoldoutsMap.Clear();

            foreach (var holdout in _allHoldouts)
            {
                // Build ID mapping
                _holdoutIdMap[holdout.Id] = holdout;

                // Classify based on IsGlobal (set by section membership at parse time)
                if (holdout.IsGlobal)
                {
                    _globalHoldouts.Add(holdout);
                }
                else if (holdout.IncludedRules != null)
                {
                    // Local holdout — map each targeted rule ID
                    foreach (var ruleId in holdout.IncludedRules)
                    {
                        if (!_ruleHoldoutsMap.ContainsKey(ruleId))
                        {
                            _ruleHoldoutsMap[ruleId] = new List<Holdout>();
                        }

                        _ruleHoldoutsMap[ruleId].Add(holdout);
                    }
                }
            }
        }

        /// <summary>
        /// Get a Holdout object for an ID.
        /// </summary>
        /// <param name="holdoutId">The holdout identifier</param>
        /// <returns>The Holdout object if found, null otherwise</returns>
        public Holdout GetHoldout(string holdoutId)
        {
            if (string.IsNullOrEmpty(holdoutId))
            {
                return null;
            }

            _holdoutIdMap.TryGetValue(holdoutId, out var holdout);

            return holdout;
        }

        /// <summary>
        /// Returns all global holdouts (from the "holdouts" datafile section).
        /// These apply to all rules across all flags and are evaluated at flag level.
        /// </summary>
        /// <returns>List of global holdouts</returns>
        public List<Holdout> GetGlobalHoldouts()
        {
            return _globalHoldouts;
        }

        /// <summary>
        /// Returns local holdouts (from the "localHoldouts" section) that target a specific rule ID.
        /// These are evaluated per-rule, after forced decisions but before regular rule evaluation.
        /// </summary>
        /// <param name="ruleId">The rule ID to look up holdouts for</param>
        /// <returns>List of holdouts targeting the specified rule, or an empty list if none</returns>
        public List<Holdout> GetHoldoutsForRule(string ruleId)
        {
            if (string.IsNullOrEmpty(ruleId))
            {
                return new List<Holdout>();
            }

            if (_ruleHoldoutsMap.TryGetValue(ruleId, out var holdouts))
            {
                return holdouts;
            }

            return new List<Holdout>();
        }

        /// <summary>
        /// Gets the total number of holdouts.
        /// </summary>
        public int HoldoutCount => _allHoldouts.Count;

        /// <summary>
        /// Updates the holdout configuration with a new set of holdouts.
        /// This method is useful for testing or when the holdout configuration needs to be updated at runtime.
        /// </summary>
        /// <param name="newHoldouts">The new array of holdouts to use</param>
        public void UpdateHoldoutMapping(Holdout[] newHoldouts)
        {
            _allHoldouts = newHoldouts?.ToList() ?? new List<Holdout>();
            UpdateHoldoutMapping();
        }
    }
}
