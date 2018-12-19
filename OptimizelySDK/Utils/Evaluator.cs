/* 
 * Copyright 2018, Optimizely
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

using System;

namespace OptimizelySDK.Utils
{
    public static class Evaluator
    {
        public static class AttributeMatchTypes
        {
            public const string EXACT = "exact";
            public const string EXIST = "exists";
            public const string GREATER_THAN = "gt";
            public const string LESS_THAN = "lt";
            public const string SUBSTRING = "substring";
        }

        public static Func<object, object, bool?> GetEvaluator(string matchType)
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

        public static bool? ExactEvaluator(object conditionValue, object attributeValue)
        {
            if (conditionValue == null && attributeValue == null)
                return true;

            if (conditionValue is bool && attributeValue is bool)
                return (bool)conditionValue == (bool)attributeValue;

            if (Validator.IsValidNumericValue(conditionValue) && Validator.IsValidNumericValue(attributeValue))
                return Convert.ToDouble(conditionValue) == Convert.ToDouble(attributeValue);

            if (conditionValue is string && attributeValue is string)
                return (string)conditionValue == (string)attributeValue;

            return null;
        }

        public static bool? ExistEvaluator(object conditionValue, object attributeValue)
        {
            return attributeValue != null;
        }

        public static bool? GreaterThanEvaluator(object conditionValue, object attributeValue)
        {
            if (Validator.IsValidNumericValue(conditionValue) && Validator.IsValidNumericValue(attributeValue))
                return Convert.ToDouble(attributeValue) > Convert.ToDouble(conditionValue);

            return null;
        }

        public static bool? LessThanEvaluator(object conditionValue, object attributeValue)
        {
            if (Validator.IsValidNumericValue(conditionValue) && Validator.IsValidNumericValue(attributeValue))
                return Convert.ToDouble(attributeValue) < Convert.ToDouble(conditionValue);

            return null;
        }

        public static bool? SubstringEvaluator(object conditionValue, object attributeValue)
        {
            if (conditionValue is string && attributeValue is string)
            {
                var value = (string)attributeValue;
                return value != null && value.Contains((string)conditionValue);
            }

            return null;
        }
    }
}
