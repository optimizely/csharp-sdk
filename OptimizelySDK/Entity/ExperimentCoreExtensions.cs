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

using System.Linq;

namespace OptimizelySDK.Entity
{
    /// <summary>
    /// Extension methods providing common functionality for IExperimentCore implementations
    /// </summary>
    public static class ExperimentCoreExtensions
    {
        /// <summary>
        /// Get variation by ID
        /// </summary>
        /// <param name="experimentCore">The experiment or holdout instance</param>
        /// <param name="id">Variation ID to search for</param>
        /// <returns>Variation with the specified ID, or null if not found</returns>
        public static Variation GetVariation(this IExperimentCore experimentCore, string id)
        {
            if (experimentCore?.Variations == null || string.IsNullOrEmpty(id))
            {
                return null;
            }

            return experimentCore.Variations.FirstOrDefault(v => v.Id == id);
        }

        /// <summary>
        /// Get variation by key
        /// </summary>
        /// <param name="experimentCore">The experiment or holdout instance</param>
        /// <param name="key">Variation key to search for</param>
        /// <returns>Variation with the specified key, or null if not found</returns>
        public static Variation GetVariationByKey(this IExperimentCore experimentCore, string key)
        {
            if (experimentCore?.Variations == null || string.IsNullOrEmpty(key))
            {
                return null;
            }

            return experimentCore.Variations.FirstOrDefault(v => v.Key == key);
        }
    }
}
