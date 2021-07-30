using OptimizelySDK.Entity;
using OptimizelySDK.Logger;
using Moq;
using OptimizelySDK.Event.Builder;
using OptimizelySDK.Event;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System;
using OptimizelySDK.Event.Dispatcher;
using NUnit.Framework;
using System.Diagnostics.CodeAnalysis;

namespace OptimizelySDK.Tests.EventTests
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class DefaultEventDispatcherTest
    {
        [Test]
        public void TestDispatchEvent()
        {
            var logEvent = new LogEvent("",
                new Dictionary<string, object>
                {
                    {"accountId", "1234" },
                    {"projectId", "9876" },
                    {"visitorId", "testUser" }
                },
                "POST",
                new Dictionary<string, string>
                {
                    {"Content-Type", "application/json" }
                });

            var expectionedOptions = new Dictionary<string, object>
            {
                {"headers", logEvent.Headers },
                {"json", logEvent.Params },
                {"timeout", 10 },
                {"connect_timeout", 10 }
            };

            //TODO: Have to mock http calls. Will discuss with Randall.
            var eventDispatcher = new DefaultEventDispatcher(new Mock<ILogger>().Object);
        }
    }
}