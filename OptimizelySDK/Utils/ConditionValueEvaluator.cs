/* 
 * Copyright 2019, Optimizely
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use file except in compliance with the License.
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
using OptimizelySDK.Logger;
using System;

namespace OptimizelySDK.Utils
{
    public static class ConditionValueEvaluator
    {
        public static class AttributeMatchTypes
        {
            public const string EXACT = "exact";
            public const string EXIST = "exists";
            public const string GREATER_THAN = "gt";
            public const string LESS_THAN = "lt";
            public const string SUBSTRING = "substring";
        }

        public static Func<JToken, object, ILogger, bool?> GetEvaluator(string matchType)
        {
            switch (matchType)
            {
                case AttributeMatchTypes.EXACT:
                    return ExactEvaluator;
                case AttributeMatchTypes.EXIST:
                    return ExistEvaluator;
                case AttributeMatchTypes.GREATER_THAN:
                    return GreaterThanEvaluator;
                case AttributeMatchTypes.LESS_THAN:
                    return LessThanEvaluator;
                case AttributeMatchTypes.SUBSTRING:
                    return SubstringEvaluator;
                case null:
                    return ExactEvaluator;
            }

            return null;
        }

        public static bool? ExactEvaluator(JToken condition, object attributeValue, ILogger logger)
        {
            var conditionName = condition["name"].ToString();
            var conditionValue = condition["value"]?.ToObject<object>();

            if (!IsValueValidForExactConditions(conditionValue))
                return null;

            if (!IsValueValidForExactConditions(attributeValue))
            {
                logger.Log(LogLevel.WARN, $@"Audience condition ""{condition}"" evaluated as UNKNOWN because the value for user attribute ""{attributeValue}"" is inapplicable: {conditionName}");
                return null;
            }

            if (!AreValuesSameType(conditionValue, attributeValue))
                return null;

            if (Validator.IsNumericType(conditionValue) && Validator.IsNumericType(attributeValue))
                return Convert.ToDouble(conditionValue).Equals(Convert.ToDouble(attributeValue));

            return conditionValue.Equals(attributeValue);
        }

        public static bool? ExistEvaluator(JToken condition, object attributeValue, ILogger logger)
        {
            return attributeValue != null;
        }

        public static bool? GreaterThanEvaluator(JToken condition, object attributeValue, ILogger logger)
        {
            var conditionName = condition["name"].ToString();
            var conditionValue = condition["value"]?.ToObject<object>();

            if (!Validator.IsValidNumericValue(conditionValue))
                return null;

            if (!Validator.IsValidNumericValue(attributeValue))
            {
                logger.Log(LogLevel.WARN, $@"Audience condition ""{condition}"" evaluated as UNKNOWN because the value for user attribute ""{attributeValue}"" is inapplicable: {conditionName}");
                return null;
            }
            
            return Convert.ToDouble(attributeValue) > Convert.ToDouble(conditionValue);
        }

        public static bool? LessThanEvaluator(JToken condition, object attributeValue, ILogger logger)
        {
            var conditionName = condition["name"].ToString();
            var conditionValue = condition["value"]?.ToObject<object>();

            if (!Validator.IsValidNumericValue(conditionValue))
                return null;

            if (!Validator.IsValidNumericValue(attributeValue))
            {
                logger.Log(LogLevel.WARN, $@"Audience condition ""{condition}"" evaluated as UNKNOWN because the value for user attribute ""{attributeValue}"" is inapplicable: {conditionName}");
                return null;
            }

            return Convert.ToDouble(attributeValue) < Convert.ToDouble(conditionValue);
        }

        public static bool? SubstringEvaluator(JToken condition, object attributeValue, ILogger logger)
        {
            var conditionName = condition["name"].ToString();
            var conditionValue = condition["value"]?.ToObject<object>();

            if (!(conditionValue is string))
                return null;

            if (!(attributeValue is string))
            {
                logger.Log(LogLevel.WARN, $@"Audience condition ""{condition}"" evaluated as UNKNOWN because the value for user attribute ""{attributeValue}"" is inapplicable: {conditionName}");
                return null;
            }
            
            var value = (string)attributeValue;
            return value != null && value.Contains((string)conditionValue);
        }

        /// <summary>
        /// Validates the value for exact conditions.
        /// </summary>
        /// <param name="value">Value to validate</param>
        /// <returns>true if the type of value is valid for exact conditions, false otherwise.</returns>
        public static bool IsValueValidForExactConditions(object value)
        {
            return value is string || value is bool || Validator.IsValidNumericValue(value);
        }

        /// <summary>
        /// Validates that the types of first and second value are same.
        /// </summary>
        /// <param name="firstValue"></param>
        /// <param name="secondValue"></param>
        /// <returns>true if the type of both values are same, false otherwise.</returns>
        public static bool AreValuesSameType(object firstValue, object secondValue)
        {
            if (firstValue is string && secondValue is string)
                return true;

            if (firstValue is bool && secondValue is bool)
                return true;

            if (Validator.IsNumericType(firstValue) && Validator.IsNumericType(secondValue))
                return true;

            return false;
        }
    }
}
