using System.Collections.Generic;

namespace OptimizelySDK.Utils
{
    public class EventTagUtils
    {
        public const string REVENUE_EVENT_METRIC_NAME = "revenue";

        public static object GetRevenueValue(Dictionary<string, object> eventTags)
        {
            if (eventTags == null 
                || !eventTags.ContainsKey(REVENUE_EVENT_METRIC_NAME) 
                || eventTags[REVENUE_EVENT_METRIC_NAME] == null 
                || !(eventTags[REVENUE_EVENT_METRIC_NAME] is int))
                return null;

           return eventTags[REVENUE_EVENT_METRIC_NAME];
        }
    }
}