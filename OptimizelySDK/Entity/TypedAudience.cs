using OptimizelySDK.Config.audience;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OptimizelySDK.Entity
{
    public class TypedAudience : Audience
    {
        // TODO need to ask if Object will work
        public new Condition Conditions { get; set; }

    }
}
