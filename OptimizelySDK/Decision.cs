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
        /// The ID of the { @link com.optimizely.ab.config.Variation }
        //the user was bucketed into.
        /// </summary>
        public string VariationId;

        /// <summary>
        /// Initialize a Decision object.
        /// </summary>
        /// <param name = "variationId" > The ID of the variation the user was bucketed into.</param>
        public Decision(string variationId)
        {
            this.VariationId = variationId;
        }

        public Dictionary<string, string> ToMap()
        {
            Dictionary<string, string> decisionMap = new Dictionary<string, string>();

            decisionMap[UserProfileService.VARIATION_ID_KEY] = VariationId;

            return decisionMap;
        }
    }
}
