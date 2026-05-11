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
    /// Configuration manager for holdouts, providing holdout ID mapping.
    /// </summary>
    public class HoldoutConfig
    {
        private List<Holdout> _allHoldouts;
        private readonly Dictionary<string, Holdout> _holdoutIdMap;

        /// <summary>
        /// Initializes a new instance of the HoldoutConfig class.
        /// </summary>
        /// <param name="allHoldouts">Array of all holdouts from the datafile</param>
        public HoldoutConfig(Holdout[] allHoldouts = null)
        {
            _allHoldouts = allHoldouts?.ToList() ?? new List<Holdout>();
            _holdoutIdMap = new Dictionary<string, Holdout>();

            UpdateHoldoutMapping();
        }

        /// <summary>
        /// Gets a read-only dictionary mapping holdout IDs to holdout instances.
        /// </summary>
        public IDictionary<string, Holdout> HoldoutIdMap => _holdoutIdMap;

        /// <summary>
        /// Updates internal mappings of holdouts including the id map.
        /// </summary>
        private void UpdateHoldoutMapping()
        {
            // Clear existing mappings
            _holdoutIdMap.Clear();

            foreach (var holdout in _allHoldouts)
            {
                // Build ID mapping
                _holdoutIdMap[holdout.Id] = holdout;
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
