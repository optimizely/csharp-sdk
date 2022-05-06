using OptimizelySDK.Utils;
using System;

namespace OptimizelySDK.Config.audience.match
{
    class ExactMatch: Match
    {
        public bool? Eval(object conditionValue, object attributeValue)
        {
            if (attributeValue == null) return null;

            if (Validator.IsValidNumericValue(attributeValue)) {
                if (Validator.IsValidNumericValue(conditionValue))
                {
                    double userValue = Convert.ToDouble(attributeValue);
                    double conditionalValue = Convert.ToDouble(conditionValue);

                    return userValue.CompareTo(conditionalValue) == 0;
                }
                return null;
            }

            if (!(conditionValue is string || conditionValue is bool)) {
                throw new Exception("Unexpected type");
            }

            if (attributeValue.GetType() != conditionValue.GetType()) {
                return null;
            }

            return conditionValue.Equals(attributeValue);
        }
    }

}
