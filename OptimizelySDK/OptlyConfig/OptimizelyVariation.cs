using System.Collections.Generic;

namespace OptimizelySDK.OptlyConfig
{
    public class OptimizelyVariation
    {
        public string ID { get; set; }
        public string Key { get; set; }
        public bool? FeatureEnabled { get; set; }
        public Dictionary<string, OptimizelyVariable> VariablesMap = new Dictionary<string, OptimizelyVariable>();
    }
}
