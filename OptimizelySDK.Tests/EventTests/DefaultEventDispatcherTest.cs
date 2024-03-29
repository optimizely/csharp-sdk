﻿using System;
using System.Collections.Generic;
using Moq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using OptimizelySDK.Entity;
using OptimizelySDK.Event;
using OptimizelySDK.Event.Builder;
using OptimizelySDK.Event.Dispatcher;
using OptimizelySDK.Logger;

namespace OptimizelySDK.Tests.EventTests
{
    [TestFixture]
    public class DefaultEventDispatcherTest
    {
        [Test]
        public void TestDispatchEvent()
        {
            var logEvent = new LogEvent("",
                new Dictionary<string, object>
                {
                    { "accountId", "1234" },
                    { "projectId", "9876" },
                    { "visitorId", "testUser" },
                },
                "POST",
                new Dictionary<string, string>
                {
                    { "Content-Type", "application/json" },
                });

            var expectionedOptions = new Dictionary<string, object>
            {
                { "headers", logEvent.Headers },
                { "json", logEvent.Params },
                { "timeout", 10 },
                { "connect_timeout", 10 },
            };

            //TODO: Have to mock http calls. Will discuss with Randall.
            var eventDispatcher = new DefaultEventDispatcher(new Mock<ILogger>().Object);
        }
    }
}
