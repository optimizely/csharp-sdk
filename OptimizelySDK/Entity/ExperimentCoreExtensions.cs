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

        /// <summary>
        /// Replace audience IDs with audience names in a condition string
        /// </summary>
        /// <param name="experimentCore">The experiment or holdout instance</param>
        /// <param name="conditionString">String containing audience conditions</param>
        /// <param name="audiencesMap">Map of audience ID to audience name</param>
        /// <returns>String with audience IDs replaced by names</returns>
        public static string ReplaceAudienceIdsWithNames(this IExperimentCore experimentCore, 
            string conditionString, System.Collections.Generic.Dictionary<string, string> audiencesMap)
        {
            if (string.IsNullOrEmpty(conditionString) || audiencesMap == null)
            {
                return conditionString ?? string.Empty;
            }

            const string beginWord = "AUDIENCE(";
            const string endWord = ")";
            var keyIdx = 0;
            var audienceId = string.Empty;
            var collect = false;
            var replaced = string.Empty;

            foreach (var ch in conditionString)
            {
                // Extract audience id in parenthesis (example: AUDIENCE("35") => "35")
                if (collect)
                {
                    if (ch.ToString() == endWord)
                    {
                        // Output the extracted audienceId
                        var audienceName = audiencesMap.ContainsKey(audienceId) ? audiencesMap[audienceId] : audienceId;
                        replaced += $"\"{audienceName}\"";
                        collect = false;
                        audienceId = string.Empty;
                    }
                    else
                    {
                        audienceId += ch;
                    }
                    continue;
                }

                // Walk-through until finding a matching keyword "AUDIENCE("
                if (ch == beginWord[keyIdx])
                {
                    keyIdx++;
                    if (keyIdx == beginWord.Length)
                    {
                        keyIdx = 0;
                        collect = true;
                    }
                    continue;
                }
                else
                {
                    if (keyIdx > 0)
                    {
                        replaced += beginWord.Substring(0, keyIdx);
                    }
                    keyIdx = 0;
                }

                // Pass through other characters
                replaced += ch;
            }

            return replaced;
        }
    }
}
