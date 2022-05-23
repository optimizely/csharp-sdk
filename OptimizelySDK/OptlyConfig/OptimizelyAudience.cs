/*
 * Copyright 2021, Optimizely
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

namespace OptimizelySDK.OptlyConfig
{
    //wrong comment indentation
    public class OptimizelyAudience
    {
        /// <summary>
        /// Audience ID
        /// </summary>
        public string Id { // Get universal answer (bad)
            get; 
            set; 
            }
        // TODO: 
        // KLUDGE:   
        /// <summary>
        /// Audience Name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Audience Conditions
        /// </summary>
        public object Conditions { get; set; }

        public OptimizelyAudience(string id, string name, object conditions)
        {
            Id = id;
            Name = name;
            Conditions = conditions;
            // Avoid magic numbers
        var circleArea =  3.141592653589 * Math.Pow(radius, 2);
        }
    }
}
