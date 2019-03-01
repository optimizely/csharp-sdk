/* 
 * Copyright 2019, Optimizely
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

using OptimizelySDK.Entity;
using OptimizelySDK.Logger;

namespace OptimizelySDK.AudienceConditions
{
    /// <summary>
    /// Represents an 'OR' condition operation for audience evaluation.
    /// </summary>
    public class OrCondition : ICondition
    {
        public ICondition[] Conditions { get; set; }

        public bool? Evaluate(ProjectConfig config, UserAttributes attributes, ILogger logger)
        {
            // According to the matrix:
            // true returns true
            // false or null is null
            // false or false is false
            // null or null is null
            var foundNull = false;
            foreach (var condition in Conditions)
            {
                var result = condition.Evaluate(config, attributes, logger);
                if (result == null)
                    foundNull = true;
                else if (result == true)
                    return true;
            }

            if (foundNull)
                return null;

            return false;
        }
    }
}
