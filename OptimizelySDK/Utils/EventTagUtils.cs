using System.Collections.Generic;

namespace OptimizelySDK.Utils
{
    public class EventTagUtils
    {
        public const string REVENUE_EVENT_METRIC_NAME = "revenue";
        public const string VALUE_EVENT_METRIC_NAME = "value";

        public static object GetRevenueValue(Dictionary<string, object> eventTags)
        {
            if (eventTags == null 
                || !eventTags.ContainsKey(REVENUE_EVENT_METRIC_NAME) 
                || eventTags[REVENUE_EVENT_METRIC_NAME] == null 
                || !(eventTags[REVENUE_EVENT_METRIC_NAME] is int))
                return null;

           return eventTags[REVENUE_EVENT_METRIC_NAME];
        }

        public static object GetEventValue(Dictionary<string, object> eventTags)
        {
            decimal refVar = 0;

            if ( eventTags == null
                || !eventTags.ContainsKey(VALUE_EVENT_METRIC_NAME)
                || eventTags[VALUE_EVENT_METRIC_NAME] == null
                || ((!(eventTags[VALUE_EVENT_METRIC_NAME] is int)) 
                    && (!(eventTags[VALUE_EVENT_METRIC_NAME] is float)) 
                    && (!(eventTags[VALUE_EVENT_METRIC_NAME] is double))
                    && !decimal.TryParse(eventTags[VALUE_EVENT_METRIC_NAME].ToString(), out refVar))
                )

                return null;

            return eventTags[VALUE_EVENT_METRIC_NAME];
        }
    }
}