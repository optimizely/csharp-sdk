using System.Collections.Generic;
using OptimizelySDK.Logger;

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

        public static object GetNumericValue(Dictionary<string, object> eventTags, ILogger logger  = null)
        {
            string debugMessage = string.Empty;
            bool isCasted = false;

            float refVar = 0;

            if (eventTags == null)
                debugMessage = "Event tags is undefined.";

            else if (!eventTags.ContainsKey(VALUE_EVENT_METRIC_NAME))
                debugMessage = "The numeric metric key is not in event tags.";

            else if (eventTags[VALUE_EVENT_METRIC_NAME] == null)
                debugMessage = "The numeric metric key value is not defined in event tags.";

            else if (eventTags[VALUE_EVENT_METRIC_NAME] is bool)
                debugMessage = "Provided numeric value is boolean which is an invalid format.";

            else if (!(eventTags[VALUE_EVENT_METRIC_NAME] is int) && !(eventTags[VALUE_EVENT_METRIC_NAME] is string) && !(eventTags[VALUE_EVENT_METRIC_NAME] is float)
                && !(eventTags[VALUE_EVENT_METRIC_NAME] is decimal)
                && !(eventTags[VALUE_EVENT_METRIC_NAME] is double)
                && !(eventTags[VALUE_EVENT_METRIC_NAME] is float))
                debugMessage = "Numeric metric value is not in integer, float, or string form.";

            else
            {
                if (!float.TryParse(eventTags[VALUE_EVENT_METRIC_NAME].ToString(), out refVar))
                    debugMessage = string.Format("Provided numeric value {0} is in an invalid format.", eventTags[VALUE_EVENT_METRIC_NAME]);
                else
                {
                    if (float.IsInfinity(refVar))
                        debugMessage = string.Format("Provided numeric value {0} is in an invalid format.", eventTags[VALUE_EVENT_METRIC_NAME]);
                    else
                        isCasted = true;
                }
            }
                

            if (isCasted)
                logger.Log(LogLevel.INFO, string.Format("The numeric metric value {0} will be sent to results.", refVar));
            else
                if (string.IsNullOrEmpty(debugMessage))
                    logger.Log(LogLevel.ERROR, string.Format("The provided numeric metric value {0} is in an invalid format and will not be sent to results.", eventTags[VALUE_EVENT_METRIC_NAME]));
                else
                    logger.Log(LogLevel.ERROR, debugMessage);

            object o = refVar;

            if(isCasted && eventTags[VALUE_EVENT_METRIC_NAME] is float)
            {
                // Special case, maximum value when passed and gone through tryparse, it loses precision.
                o = eventTags[VALUE_EVENT_METRIC_NAME];
            }

            return isCasted ? o : null;

        }
    }
}
 