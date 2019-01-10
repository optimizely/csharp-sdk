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

using Newtonsoft.Json.Linq;
using OptimizelySDK.Entity;

namespace OptimizelySDK.Utils
{
    public static class CustomAttributeConditionEvaluator
    {
        /// <summary>
        /// String constant representing custome attribute condition type.
        /// </summary>
        public const string CUSTOM_ATTRIBUTE_CONDITION_TYPE = "custom_attribute";

        public static bool? Evaluate(JToken condition, UserAttributes userAttributes)
        {
            string conditionType = condition["type"]?.ToString();
            if (conditionType == null || conditionType != CUSTOM_ATTRIBUTE_CONDITION_TYPE)
                return null;

            string matchType = condition["match"]?.ToString();
            var conditionValue = condition["value"]?.ToObject<object>();

            object attributeValue = null;
            if (userAttributes != null && userAttributes.ContainsKey(condition["name"].ToString()))
                attributeValue = userAttributes[condition["name"].ToString()];

            var evaluator = ConditionValueEvaluator.GetEvaluator(matchType);
            return evaluator != null ? evaluator(conditionValue, attributeValue) : null;
        }
    }
}
