
namespace OptimizelySDK.Config.audience.match
{
    public class LEMatch : Match
    {
        public bool? Eval(object conditionValue, object attributeValue)
        {
            return NumberComparator.Compare(attributeValue, conditionValue) <= 0;
        }
    }
}
