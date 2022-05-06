using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OptimizelySDK.Config.audience.match
{
    class DefaultMatchForLegacyAttributes : Match
    {
        public bool? Eval(object conditionValue, object attributeValue)
        {
            if (!(conditionValue is string)) {
                throw new Exception("Unknown type");
            }
            if (attributeValue == null)
            {
                return false;
            }
            return conditionValue.ToString().Equals(attributeValue.ToString());
        }
    }
}
