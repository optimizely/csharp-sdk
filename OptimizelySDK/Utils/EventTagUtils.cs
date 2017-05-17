using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OptimizelySDK.Utils
{
    public class EventTagUtils
    {
        public const string REVENUE_EVENT_METRIC_NAME = "revenue";

        public static object GetRevenueValue(Dictionary<string, object> eventTags)
        {
            if(eventTags == null)
            {
                return null;
            }
            
            if(!eventTags.ContainsKey(REVENUE_EVENT_METRIC_NAME) || eventTags[REVENUE_EVENT_METRIC_NAME] == null)
            {
                return null;
            }

            if (!(eventTags[REVENUE_EVENT_METRIC_NAME] is int))
            {
                return null;
            }

            return eventTags[REVENUE_EVENT_METRIC_NAME];

        }

    }
}
