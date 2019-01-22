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
using OptimizelySDK.Utils;
using System;

namespace OptimizelySDK.AudienceConditions
{
    public class BaseCondition : ICondition
    {
        /// <summary>
        /// String constant representing custome attribute condition type.
        /// </summary>
        public const string CUSTOM_ATTRIBUTE_CONDITION_TYPE = "custom_attribute";

        public string Type { get; set; }

        public string Match { get; set; }

        public string Name { get; set; }

        public object Value { get; set; }

        public bool? Evaluate(ProjectConfig config, UserAttributes userAttributes)
        {
            if (Type == null || Type != CUSTOM_ATTRIBUTE_CONDITION_TYPE)
                return null;

            if (!IsValidMatchType())
                return null;

            userAttributes.TryGetValue(Name, out object attributeValue);
            if (attributeValue == null && Match != AttributeMatchTypes.EXIST)
                return null;

            var evaluator = GetEvaluator();
            return evaluator != null ? evaluator(attributeValue) : null;
        }

        public bool IsValidMatchType()
        {
            switch (Match)
            {
                case AttributeMatchTypes.EXACT:
                case AttributeMatchTypes.EXIST:
                case AttributeMatchTypes.GREATER_THAN:
                case AttributeMatchTypes.LESS_THAN:
                case AttributeMatchTypes.SUBSTRING:
                case null:
                    return true;
                default:
                    return false;
            }
        }

        public Func<object, bool?> GetEvaluator()
        {
            switch (Match)
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

        public bool? ExactEvaluator(object attributeValue)
        {
            if (!IsValueValidForExactConditions(Value) ||
                !IsValueValidForExactConditions(attributeValue) ||
                !AreValuesSameType(Value, attributeValue))
                return null;

            if (Validator.IsNumericType(Value) && Validator.IsNumericType(attributeValue))
                return Convert.ToDouble(Value).Equals(Convert.ToDouble(attributeValue));

            return Value.Equals(attributeValue);
        }

        public bool? ExistEvaluator(object attributeValue)
        {
            return attributeValue != null;
        }

        public bool? GreaterThanEvaluator(object attributeValue)
        {
            if (Validator.IsValidNumericValue(Value) && Validator.IsValidNumericValue(attributeValue))
                return Convert.ToDouble(attributeValue) > Convert.ToDouble(Value);

            return null;
        }

        public bool? LessThanEvaluator(object attributeValue)
        {
            if (Validator.IsValidNumericValue(Value) && Validator.IsValidNumericValue(attributeValue))
                return Convert.ToDouble(attributeValue) < Convert.ToDouble(Value);

            return null;
        }

        public bool? SubstringEvaluator(object attributeValue)
        {
            if (Value is string && attributeValue is string)
            {
                var attrValue = (string)attributeValue;
                return attrValue != null && attrValue.Contains((string)Value);
            }

            return null;
        }

        /// <summary>
        /// Validates the value for exact conditions.
        /// </summary>
        /// <param name="value">Value to validate</param>
        /// <returns>true if the type of value is valid for exact conditions, false otherwise.</returns>
        public bool IsValueValidForExactConditions(object value)
        {
            return value is string || value is bool || Validator.IsValidNumericValue(value);
        }

        /// <summary>
        /// Validates that the types of first and second value are same.
        /// </summary>
        /// <param name="firstValue"></param>
        /// <param name="secondValue"></param>
        /// <returns>true if the type of both values are same, false otherwise.</returns>
        public bool AreValuesSameType(object firstValue, object secondValue)
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
