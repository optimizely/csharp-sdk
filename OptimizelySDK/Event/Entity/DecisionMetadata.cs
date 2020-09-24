
namespace OptimizelySDK.Event.Entity
{
    /// <summary>
    /// DecisionMetadata captures additional information regarding the decision
    /// </summary>
    public class DecisionMetadata
    {
        public string FlagType { get; private set; }
        public string FlagKey { get; private set; }
        public string VariationKey { get; private set; }

        public DecisionMetadata(string flagKey, string flagType, string variationKey = null) 
        {
            FlagType = flagType;
            FlagKey = flagKey;
            VariationKey = variationKey;
        }
    }
}
