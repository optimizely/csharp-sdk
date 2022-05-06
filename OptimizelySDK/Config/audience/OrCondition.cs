using OptimizelySDK.Entity;
using System;

namespace OptimizelySDK.Config.audience
{
    class OrCondition<T> : Condition
    {
        private readonly Condition[] Conditions;
        private static readonly string OPERAND = "OR";

        public OrCondition(Condition[] conditions)
        {
            Conditions = conditions;
        }

        public Condition[] getConditions()
        {
            return Conditions;
        }

        public bool? Evaluate(ProjectConfig config, UserAttributes attributes)
        {
            if (Conditions == null) return null;
            bool foundNull = false;
            foreach (Condition condition in Conditions)
            {
                bool? conditionEval = condition.Evaluate(config, attributes);
                if (conditionEval == null)
                { // true with falses and nulls is still true
                    foundNull = true;
                }
                else if (conditionEval ==  true)
                {
                    return true;
                }
            }

            // if found null and false return null.  all false return false
            if (foundNull)
            {
                return null;
            }

            return false;
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
