/*
 * Copyright 2025, Optimizely
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
    /// Configuration manager for holdouts, providing flag-to-holdout relationship mapping and optimization logic.
    /// </summary>
    public class HoldoutConfig
    {
        private List<Holdout> _allHoldouts;
        private readonly List<Holdout> _globalHoldouts;
        private readonly Dictionary<string, Holdout> _holdoutIdMap;
        private readonly Dictionary<string, List<Holdout>> _includedHoldouts;
        private readonly Dictionary<string, List<Holdout>> _excludedHoldouts;
        private readonly Dictionary<string, List<Holdout>> _flagHoldoutCache;

        /// <summary>
        /// Initializes a new instance of the HoldoutConfig class.
        /// </summary>
        /// <param name="allHoldouts">Array of all holdouts from the datafile</param>
        public HoldoutConfig(Holdout[] allHoldouts = null)
        {
            _allHoldouts = allHoldouts?.ToList() ?? new List<Holdout>();
            _globalHoldouts = new List<Holdout>();
            _holdoutIdMap = new Dictionary<string, Holdout>();
            _includedHoldouts = new Dictionary<string, List<Holdout>>();
            _excludedHoldouts = new Dictionary<string, List<Holdout>>();
            _flagHoldoutCache = new Dictionary<string, List<Holdout>>();

            UpdateHoldoutMapping();
        }

        /// <summary>
        /// Gets a read-only dictionary mapping holdout IDs to holdout instances.
        /// </summary>
        public IDictionary<string, Holdout> HoldoutIdMap => _holdoutIdMap;

        /// <summary>
        /// Updates internal mappings of holdouts including the id map, global list, and per-flag inclusion/exclusion maps.
        /// </summary>
        private void UpdateHoldoutMapping()
        {
            // Clear existing mappings
            _holdoutIdMap.Clear();
            _globalHoldouts.Clear();
            _includedHoldouts.Clear();
            _excludedHoldouts.Clear();
            _flagHoldoutCache.Clear();

            foreach (var holdout in _allHoldouts)
            {
                // Build ID mapping
                _holdoutIdMap[holdout.Id] = holdout;

                var hasIncludedFlags = holdout.IncludedFlags != null && holdout.IncludedFlags.Length > 0;
                var hasExcludedFlags = holdout.ExcludedFlags != null && holdout.ExcludedFlags.Length > 0;

                if (hasIncludedFlags)
                {
                    // Local/targeted holdout - only applies to specific included flags
                    foreach (var flagId in holdout.IncludedFlags)
                    {
                        if (!_includedHoldouts.ContainsKey(flagId))
                            _includedHoldouts[flagId] = new List<Holdout>();

                        _includedHoldouts[flagId].Add(holdout);
                    }
                }
                else
                {
                    // Global holdout (applies to all flags)
                    _globalHoldouts.Add(holdout);

                    // If it has excluded flags, track which flags to exclude it from
                    if (hasExcludedFlags)
                    {
                        foreach (var flagId in holdout.ExcludedFlags)
                        {
                            if (!_excludedHoldouts.ContainsKey(flagId))
                                _excludedHoldouts[flagId] = new List<Holdout>();

                            _excludedHoldouts[flagId].Add(holdout);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns the applicable holdouts for the given flag ID by combining global holdouts (excluding any specified) and included holdouts, in that order.
        /// Caches the result for future calls.
        /// </summary>
        /// <param name="flagId">The flag identifier</param>
        /// <returns>A list of Holdout objects relevant to the given flag</returns>
        public List<Holdout> GetHoldoutsForFlag(string flagId)
        {
            if (string.IsNullOrEmpty(flagId) || _allHoldouts.Count == 0)
                return new List<Holdout>();

            // Check cache first
            if (_flagHoldoutCache.ContainsKey(flagId))
                return _flagHoldoutCache[flagId];

            var activeHoldouts = new List<Holdout>();

            // Start with global holdouts, excluding any that are specifically excluded for this flag
            var excludedForFlag = _excludedHoldouts.ContainsKey(flagId) ? _excludedHoldouts[flagId] : new List<Holdout>();

            foreach (var globalHoldout in _globalHoldouts)
            {
                if (!excludedForFlag.Contains(globalHoldout))
                {
                    activeHoldouts.Add(globalHoldout);
                }
            }

            // Add included holdouts for this flag
            if (_includedHoldouts.ContainsKey(flagId))
            {
                activeHoldouts.AddRange(_includedHoldouts[flagId]);
            }

            // Cache the result
            _flagHoldoutCache[flagId] = activeHoldouts;

            return activeHoldouts;
        }

        /// <summary>
        /// Get a Holdout object for an ID.
        /// </summary>
        /// <param name="holdoutId">The holdout identifier</param>
        /// <returns>The Holdout object if found, null otherwise</returns>
        public Holdout GetHoldout(string holdoutId)
        {
            return _holdoutIdMap.ContainsKey(holdoutId) ? _holdoutIdMap[holdoutId] : null;
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
