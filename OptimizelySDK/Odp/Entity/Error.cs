/* 
 * Copyright 2022 Optimizely
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

namespace OptimizelySDK.Odp.Entity
{
    /// <summary>
    /// GraphQL response from an errant query
    /// </summary>
    public class Error
    {
        /// <summary>
        /// Human-readable message from the error
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Points of failure producing the error
        /// </summary>
        public Location[] Locations { get; set; }

        /// <summary>
        /// Files or urls producing the error
        /// </summary>
        public string[] Path { get; set; }

        /// <summary>
        /// Additional technical error information 
        /// </summary>
        public Extension Extensions { get; set; }

        public override string ToString()
        {
            return $"{Message}";
        }
    }
}
