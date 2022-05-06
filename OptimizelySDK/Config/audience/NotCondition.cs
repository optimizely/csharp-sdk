
using OptimizelySDK.Entity;

namespace OptimizelySDK.Config.audience
{
    public class NotCondition<T> : Condition<T>
    {
        private Condition<T> _Condition { get; }
        private readonly static string OPERAND = "NOT";

        public NotCondition(Condition<T> condition)
        {
            _Condition = condition;
        }

        public bool? Evaluate(ProjectConfig config, UserAttributes attributes)
        {
            bool? conditionEval = _Condition == null ? null : _Condition.Evaluate(config, attributes);
            return (conditionEval == null ? null : !conditionEval);
        }

        public string GetOperandOrId()
        {
            return OPERAND;
        }

        public string ToJson()
        {
            throw new System.NotImplementedException();
        }
    }
}
