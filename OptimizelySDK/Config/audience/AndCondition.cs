using OptimizelySDK.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OptimizelySDK.Config.audience
{
    class AndCondition<T> : Condition<T>
    {
        private readonly Condition<T>[] Conditions;
        private static readonly string OPERAND = "AND";

        public AndCondition(Condition<T>[] conditions)
        {
            Conditions = conditions;
        }

        public bool? Evaluate(ProjectConfig config, UserAttributes attributes)
        {
            if (Conditions == null) return null;
            bool foundNull = false;
            // According to the matrix where:
            // false and true is false
            // false and null is false
            // true and null is null.
            // true and false is false
            // true and true is true
            // null and null is null
            foreach (Condition<T> condition in Conditions)
            {
                bool? conditionEval = condition.Evaluate(config, attributes);
                if (conditionEval == null)
                {
                    foundNull = true;
                }
                else if (conditionEval == false)
                { // false with nulls or trues is false.
                    return false;
                }
                // true and nulls with no false will be null.
            }

            if (foundNull)
            { // true and null or all null returns null
                return null;
            }

            return true; // otherwise, return true
        }

        public string GetOperandOrId()
        {
            return OPERAND;
        }

        public string ToJson()
        {
            throw new NotImplementedException();
        }
    }
}
