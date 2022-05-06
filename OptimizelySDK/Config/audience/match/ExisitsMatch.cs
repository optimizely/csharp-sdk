using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OptimizelySDK.Config.audience.match
{
    class ExisitsMatch : Match
    {
        public bool? Eval(object conditionValue, object attributeValue)
        {
            return attributeValue != null;
        }
    }
}
