using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static OptimizelySDK.Notifications.DecisionNotification;

namespace OptimizelySDK.Notifications
{
    public class FeatureTestSourceInfo: SourceInfo
    {
        private string ExperimentKey;
        private string VariationKey;

        public FeatureTestSourceInfo(string experimentKey, string variationKey)
        {
            ExperimentKey = experimentKey;
            VariationKey = variationKey;
        }

        public IDictionary<string, string> Get()
        {
            var sourceInfo = new Dictionary<string, string>();
            sourceInfo.Add(ExperimentDecisionNotificationBuilder.EXPERIMENT_KEY, ExperimentKey);
            sourceInfo.Add(ExperimentDecisionNotificationBuilder.VARIATION_KEY, VariationKey);

            return sourceInfo;
        }
    }
}
