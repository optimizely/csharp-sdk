using OptimizelySDK.Entity;
using System.Collections.Generic;

namespace OptimizelySDK.Tests.EventTests
{
    public class CanonicalEvent
    {
        private string ExperimentId;
        private string VariationId;
        private string EventName;
        private string VisitorId;
        private Dictionary<string, object> Attributes;
        private EventTags Tags;

        public CanonicalEvent(string experimentId, string variationId, string eventName, string visitorId, Dictionary<string, object> attributes, EventTags tags)
        {
            ExperimentId = experimentId;
            VariationId = variationId;
            EventName = eventName;
            VisitorId = visitorId;
            Attributes = attributes ?? new Dictionary<string, object>();
            Tags = tags ?? new EventTags();
        }

        // if u really need to use Equals, use hashcode as well.
        // i don't thiknk so it's need, u may use overloaded operator to check if it's equal or not.
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            var canonicalEvent = (CanonicalEvent)obj;

            return ExperimentId == canonicalEvent.ExperimentId &&
                VariationId == canonicalEvent.VariationId &&
                EventName == canonicalEvent.EventName &&
                VisitorId == canonicalEvent.VisitorId &&
                Attributes == canonicalEvent.Attributes &&
                Tags == canonicalEvent.Tags;
        }
    }
}
