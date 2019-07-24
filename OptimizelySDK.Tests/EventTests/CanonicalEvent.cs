using OptimizelySDK.Entity;
using System.Collections.Generic;
using System.Linq;

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

            CanonicalEvent canonicalEvent = obj as CanonicalEvent;
            if (canonicalEvent == null)
                return false;

            if (ExperimentId != canonicalEvent.ExperimentId ||
                VariationId != canonicalEvent.VariationId ||
                EventName != canonicalEvent.EventName ||
                VisitorId != canonicalEvent.VisitorId)
                return false;

            if (!Attributes.OrderBy(pair => pair.Key)
                .SequenceEqual(canonicalEvent.Attributes.OrderBy(pair => pair.Key)))
                return false;

            if (!Tags.OrderBy(pair => pair.Key)
                .SequenceEqual(canonicalEvent.Tags.OrderBy(pair => pair.Key)))
                return false;

            return true;
        }

        public override int GetHashCode()
        {
            var hashCode = -907746114;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(ExperimentId);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(VariationId);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(EventName);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(VisitorId);
            hashCode = hashCode * -1521134295 + EqualityComparer<Dictionary<string, object>>.Default.GetHashCode(Attributes);
            hashCode = hashCode * -1521134295 + EqualityComparer<EventTags>.Default.GetHashCode(Tags);
            return hashCode;
        }
    }
}
