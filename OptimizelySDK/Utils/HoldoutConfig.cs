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
    /// Configuration manager for holdouts, providing rule-to-holdout relationship mapping and optimization logic.
    /// </summary>
    public class HoldoutConfig
    {
        private List<Holdout> _allHoldouts;
        private readonly List<Holdout> _globalHoldouts;
        private readonly Dictionary<string, Holdout> _holdoutIdMap;
        private readonly Dictionary<string, List<Holdout>> _ruleHoldoutsMap;

        /// <summary>
        /// Initializes a new instance of the HoldoutConfig class.
        /// </summary>
        /// <param name="allHoldouts">Array of all holdouts from the datafile</param>
        public HoldoutConfig(Holdout[] allHoldouts = null)
        {
            _allHoldouts = allHoldouts?.ToList() ?? new List<Holdout>();
            _globalHoldouts = new List<Holdout>();
            _holdoutIdMap = new Dictionary<string, Holdout>();
            _ruleHoldoutsMap = new Dictionary<string, List<Holdout>>();

            UpdateHoldoutMapping();
        }

        /// <summary>
        /// Gets a read-only dictionary mapping holdout IDs to holdout instances.
        /// </summary>
        public IDictionary<string, Holdout> HoldoutIdMap => _holdoutIdMap;

        /// <summary>
        /// Updates internal mappings of holdouts including the id map, global list, and per-rule mappings.
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

                if (holdout.IsGlobal())
                {
                    // Global holdout - applies to all rules
                    _globalHoldouts.Add(holdout);
                }
                else if (holdout.IncludedRules != null && holdout.IncludedRules.Length > 0)
                {
                    // Local holdout - applies only to specific rules
                    foreach (var ruleId in holdout.IncludedRules)
                    {
                        if (!_ruleHoldoutsMap.ContainsKey(ruleId))
                            _ruleHoldoutsMap[ruleId] = new List<Holdout>();

                        _ruleHoldoutsMap[ruleId].Add(holdout);
                    }
                }
                // Note: If IncludedRules is an empty array, it's a local holdout with no rules (edge case)
                // It won't be added to either global or rule maps
            }
        }

        /// <summary>
        /// Returns all global holdouts that apply to all rules.
        /// </summary>
        /// <returns>A list of global Holdout objects</returns>
        public List<Holdout> GetGlobalHoldouts()
        {
            return new List<Holdout>(_globalHoldouts);
        }

        /// <summary>
        /// Returns local holdouts that apply to a specific rule.
        /// </summary>
        /// <param name="ruleId">The rule identifier</param>
        /// <returns>A list of Holdout objects that target this specific rule</returns>
        public List<Holdout> GetHoldoutsForRule(string ruleId)
        {
            if (string.IsNullOrEmpty(ruleId))
                return new List<Holdout>();

            if (_ruleHoldoutsMap.ContainsKey(ruleId))
                return new List<Holdout>(_ruleHoldoutsMap[ruleId]);

            return new List<Holdout>();
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
        /// Gets the total number of holdouts.
        /// </summary>
        public int HoldoutCount => _allHoldouts.Count;

        /// <summary>
        /// Gets the number of global holdouts.
        /// </summary>
        public int GlobalHoldoutCount => _globalHoldouts.Count;

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
