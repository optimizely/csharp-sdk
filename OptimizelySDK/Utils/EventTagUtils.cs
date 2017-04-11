using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        /*const REVENUE_EVENT_METRIC_NAME = 'revenue';
 
 
 Grab the revenue value from the event tags. "revenue" is a reserved keyword.
 
 @param $eventTags array Representing metadata associated with the event.
 @return integer Revenue value as an integer number or null if revenue can't be retrieved from the event tags
 
 +    public static function getRevenueValue($eventTags)
        {
            +        if (!$eventTags) {
                +            return null;
                +        }
            +        if (!is_array($eventTags))
            {
                +            return null;
                +        }
            +
            +        if (!isset($eventTags[self::REVENUE_EVENT_METRIC_NAME]) or !$eventTags[self::REVENUE_EVENT_METRIC_NAME]) {
                +            return null;
                +        }
            +
            +        $raw_value = $eventTags[self::REVENUE_EVENT_METRIC_NAME];
            +        if (!is_int($raw_value))
            {
                +            return null;
                +        }
            +
            +        return $raw_value;
            +    }*/

    }
}
