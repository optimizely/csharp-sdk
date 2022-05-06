
namespace OptimizelySDK.Config.audience.match
{
    public interface Match
    {
        bool? Eval(object conditionValue, object attributeValue);
    }
}
