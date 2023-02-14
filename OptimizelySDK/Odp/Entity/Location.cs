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
    /// Specifies the precise place in code or data where the error occurred
    /// </summary>
    public class Location
    {
        /// <summary>
        /// Code or data line number 
        /// </summary>
        public int Line { get; set; }

        /// <summary>
        /// Code or data column number
        /// </summary>
        public int Column { get; set; }
    }
}
