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

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OptimizelySDK.Entity;
using OptimizelySDK.Logger;

namespace OptimizelySDK.Utils
{
    public static class CustomAttributeConditionEvaluator
    {
        /// <summary>
        /// String constant representing custome attribute condition type.
        /// </summary>
        public const string CUSTOM_ATTRIBUTE_CONDITION_TYPE = "custom_attribute";

        public static bool? Evaluate(JToken condition, UserAttributes userAttributes, ILogger logger)
        {
            string conditionType = condition["type"]?.ToString();
            if (conditionType == null || conditionType != CUSTOM_ATTRIBUTE_CONDITION_TYPE)
            {
                logger.Log(LogLevel.WARN, $@"Audience condition ""{condition.ToString(Formatting.None)}"" uses an unknown condition type.");
                return null;
            }

            string matchType = condition["match"]?.ToString();
            var conditionName = condition["name"].ToString();

            object attributeValue = null;
            if (userAttributes != null && userAttributes.ContainsKey(conditionName))
                attributeValue = userAttributes[conditionName];

            if (attributeValue == null && matchType != "exists")
            {
                logger.Log(LogLevel.WARN, $@"Audience condition ""{condition.ToString(Formatting.None)}"" evaluated as UNKNOWN because no value was passed for user attribute ""{conditionName}"".");
                return null;
            }

            var evaluator = ConditionValueEvaluator.GetEvaluator(matchType);
            if (evaluator == null)
            {
                logger.Log(LogLevel.WARN, $@"Audience condition ""{condition.ToString(Formatting.None)}"" uses an unknown match type.");
                return null;
            }

            return evaluator != null ? evaluator(condition, attributeValue, logger) : null;
        }
    }
}
