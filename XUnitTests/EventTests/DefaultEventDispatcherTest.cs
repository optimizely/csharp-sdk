/**
 *
 *    Copyright 2020, Optimizely and contributors
 *
 *    Licensed under the Apache License, Version 2.0 (the "License");
 *    you may not use this file except in compliance with the License.
 *    You may obtain a copy of the License at
 *
 *        http://www.apache.org/licenses/LICENSE-2.0
 *
 *    Unless required by applicable law or agreed to in writing, software
 *    distributed under the License is distributed on an "AS IS" BASIS,
 *    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *    See the License for the specific language governing permissions and
 *    limitations under the License.
 */

using OptimizelySDK.Logger;
using Moq;
using OptimizelySDK.Event;
using System.Collections.Generic;
using OptimizelySDK.Event.Dispatcher;
using Xunit;

namespace OptimizelySDK.XUnitTests.EventTests
{
    public class DefaultEventDispatcherTest
    {
        [Fact]
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
