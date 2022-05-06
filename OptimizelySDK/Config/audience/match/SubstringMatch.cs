
using System;

namespace OptimizelySDK.Config.audience.match
{
    public class SubstringMatch : Match
    {
        public bool? Eval(object conditionValue, object attributeValue)
        {
            if (!(conditionValue is string)) {
                throw new Exception("unknown type");
            }

            if (!(attributeValue is string)) {
                return null;
            }

            try
            {
                return attributeValue.ToString().Contains(conditionValue.ToString());
            }
            catch (Exception e)
            {
                return null;
            }
        }
    }
}
