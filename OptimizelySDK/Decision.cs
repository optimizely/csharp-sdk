/* 
* Copyright 2017, Optimizely
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

namespace OptimizelySDK
{
    public class Decision
    {

        /// <summary>
        /// The ID of the Variation into which the user was bucketed.
        /// </summary>
        public string VariationId { get; set; }

        /// <summary>
        /// Initialize a Decision object.
        /// </summary>
        /// <param name="variationId">The ID of the variation into which the user was bucketed.</param>
        public Decision(string variationId)
        {
            VariationId = variationId;
        }

        public Dictionary<string, string> ToMap()
        {
            return new Dictionary<string, string>
            {
                { UserProfileService.VARIATION_ID_KEY, VariationId }
            };
        }
    }
}