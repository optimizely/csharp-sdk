﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OptimizelySDK.Config;
using OptimizelySDK.Entity;
using OptimizelySDK.Event;
using OptimizelySDK.Event.Dispatcher;
using OptimizelySDK.Event.Entity;
using OptimizelySDK.Logger;

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
            {
                return false;
            }


            for (var count = 0; count < ExpectedEvents.Count; ++count)
            {
                var expectedEvent = ExpectedEvents[count];
                var actualEvent = ActualEvents[count];

                if (expectedEvent != actualEvent)
                {
                    return false;
                }
            }

            return true;
        }

        public void DispatchEvent(LogEvent actualLogEvent)
        {
            Visitor[] visitors = null;
            if (actualLogEvent.Params.ContainsKey("visitors"))
            {
                var jArray = (JArray)actualLogEvent.Params["visitors"];
                visitors = jArray.ToObject<Visitor[]>();
            }

            if (visitors == null)
            {
                return;
            }

            foreach (var visitor in visitors)
            {
                foreach (var snapshot in visitor.Snapshots)
                {
                    var decisions = snapshot.Decisions ?? new Decision[1] { new Decision() };
                    foreach (var decision in decisions)
                    {
                        foreach (var @event in snapshot.Events)
                        {
                            var userAttributes = new UserAttributes();
                            foreach (var attribute in visitor.Attributes.Where(attr =>
                                         !attr.Key.StartsWith(DatafileProjectConfig.
                                             RESERVED_ATTRIBUTE_PREFIX)))
                            {
                                userAttributes.Add(attribute.Key, attribute.Value);
                            }

                            ActualEvents.Add(new CanonicalEvent(decision.ExperimentId,
                                decision.VariationId, @event.Key,
                                visitor.VisitorId, userAttributes, @event.EventTags));
                        }
                    }
                }
            }

            try
            {
                CountdownEvent?.Signal();
            }
            catch (ObjectDisposedException)
            {
                Logger.Log(LogLevel.ERROR,
                    "The CountdownEvent instance has already been disposed.");
            }
            catch (InvalidOperationException)
            {
                Logger.Log(LogLevel.ERROR, "The CountdownEvent instance has already been set.");
            }
        }

        public void ExpectImpression(string experimentId, string variationId, string userId,
            UserAttributes attributes = null
        )
        {
            Expect(experimentId, variationId, IMPRESSION_EVENT_NAME, userId, attributes, null);
        }

        public void ExpectConversion(string eventName, string userId,
            UserAttributes attributes = null, EventTags tags = null
        )
        {
            Expect(null, null, eventName, userId, attributes, tags);
        }

        private void Expect(string experimentId, string variationId, string eventName,
            string visitorId, UserAttributes attributes, EventTags tags
        )
        {
            var expectedEvent = new CanonicalEvent(experimentId, variationId, eventName, visitorId,
                attributes, tags);
            ExpectedEvents.Add(expectedEvent);
        }
    }
}
