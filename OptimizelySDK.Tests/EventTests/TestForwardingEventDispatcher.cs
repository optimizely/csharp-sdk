using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using OptimizelySDK.Event;
using OptimizelySDK.Event.Dispatcher;
using OptimizelySDK.Logger;

namespace OptimizelySDK.Tests.EventTests
{
    public class TestForwardingEventDispatcher : IEventDispatcher
    {
        public ILogger Logger { get; set; }
        public bool IsUpdated { get; set; } = false;

        public void DispatchEvent(LogEvent logEvent)
        {
            Assert.AreEqual(logEvent.HttpVerb, "POST");
            Assert.AreEqual(logEvent.Url, EventFactory.EVENT_ENDPOINT);
            IsUpdated = true;
        }
    }
}
