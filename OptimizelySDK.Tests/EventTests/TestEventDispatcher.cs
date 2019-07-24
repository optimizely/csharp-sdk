using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OptimizelySDK.Config;
using OptimizelySDK.Entity;
using OptimizelySDK.Event;
using OptimizelySDK.Event.Dispatcher;
using OptimizelySDK.Event.Entity;
using OptimizelySDK.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace OptimizelySDK.Tests.EventTests
{
    public class TestEventDispatcher : IEventDispatcher
    {
        public ILogger Logger { get; set; }
        private readonly CountdownEvent CountdownEvent;

        private const string IMPRESSION_EVENT_NAME = "campaign_activated";
        private List<CanonicalEvent> ExpectedEvents = new List<CanonicalEvent>();
        private List<CanonicalEvent> ActualEvents = new List<CanonicalEvent>();

        public TestEventDispatcher(CountdownEvent countdownEvent = null)
        {
            CountdownEvent = countdownEvent;
        }

        public bool CompareEvents()
        {
            if (ExpectedEvents.Count != ActualEvents.Count)
                return false;


            for (int count = 0; count < ExpectedEvents.Count; ++count)
            {
                var expectedEvent = ExpectedEvents[count];
                var actualEvent = ActualEvents[count];

                if (!expectedEvent.Equals(actualEvent))
                    return false;
            }

            return true;
        }

        public void DispatchEvent(LogEvent actualLogEvent)
        {
            Visitor[] visitors = null;
            if (actualLogEvent.Params.ContainsKey("visitors"))
            {
                JArray jArray = (JArray)actualLogEvent.Params["visitors"];
                visitors = jArray.ToObject<Visitor[]>();
            }

            if (visitors == null)
                return;

            foreach (var visitor in visitors)
            {
                foreach (var snapshot in visitor.Snapshots)
                {
                    var decisions = snapshot.Decisions ?? new Decision[1] { new Decision() };
                    foreach (var decision in decisions)
                    {
                        foreach (var eevent in snapshot.Events)
                        {
                            var attributes = visitor.Attributes
                            .Where(attr => !attr.Key.StartsWith(DatafileProjectConfig.RESERVED_ATTRIBUTE_PREFIX))
                            .ToDictionary(attr => attr.Key, attr => attr.Value);

                            ActualEvents.Add(new CanonicalEvent(decision.ExperimentId, decision.VariationId, eevent.Key,
                                visitor.VisitorId, attributes, eevent.EventTags));
                        }
                    }
                }
            }

            CountdownEvent?.Signal();
        }

        public void ExpectImpression(string experimentId, string variationId, string userId, UserAttributes attributes = null)
        {
            Expect(experimentId, variationId, IMPRESSION_EVENT_NAME, userId, attributes, null);
        }

        public void ExpectConversion(string eventName, string userId, UserAttributes attributes = null, EventTags tags = null)
        {
            Expect(null, null, eventName, userId, attributes, tags);
        }

        private void Expect(string experimentId, string variationId, string eventName, string visitorId, UserAttributes attributes, EventTags tags)
        {
            var expectedEvent = new CanonicalEvent(experimentId, variationId, eventName, visitorId, attributes, tags);
            ExpectedEvents.Add(expectedEvent);
        }
    }
}
