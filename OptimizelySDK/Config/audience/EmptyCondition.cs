using OptimizelySDK.Entity;

namespace OptimizelySDK.Config.audience
{
    public class EmptyCondition<T> : Condition<T> {
        public bool? Evaluate(ProjectConfig config, UserAttributes attributes)
        {
            return true;
        }

        public string ToJson() { return null; }

        public string GetOperandOrId()
        {
            return null;
        }
    }

}
