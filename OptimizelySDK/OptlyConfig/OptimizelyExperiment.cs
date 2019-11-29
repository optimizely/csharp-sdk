using System.Collections.Generic;

namespace OptimizelySDK.OptlyConfig
{
    public class OptimizelyExperiment
    {
        public string ID { get; set; }
        public string Key { get; set; }
        public Dictionary<string, OptimizelyVariation> VariationsMap = new Dictionary<string, OptimizelyVariation>();
    }
}
