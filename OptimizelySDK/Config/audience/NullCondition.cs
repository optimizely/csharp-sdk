using OptimizelySDK.Entity;

namespace OptimizelySDK.Config.audience
{
    public class NullCondition<T> : Condition
    {
        public bool? Evaluate(ProjectConfig config, UserAttributes attributes)
        {
            return null;
        }

        public string ToJson() { return null; }

        public string GetOperandOrId()
        {
            return null;
        }
    }
}
