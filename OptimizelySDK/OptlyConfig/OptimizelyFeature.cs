using System.Collections.Generic;

namespace OptimizelySDK.OptlyConfig
{
    public class OptimizelyFeature
    {
        public string ID { get; set; }
        public string Key { get; set; }
        public Dictionary<string, OptimizelyExperiment> ExperimentsMap = new Dictionary<string, OptimizelyExperiment>();
        public Dictionary<string, OptimizelyVariable> VariablesMap = new Dictionary<string, OptimizelyVariable>();
    }
}
