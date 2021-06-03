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

using Newtonsoft.Json.Linq;
using OptimizelySDK.Event;
using System.Collections.Generic;
using Xunit;

namespace OptimizelySDK.XUnitTests.EventTests
{
    public class LogEventTest
    {
        private LogEvent LogEvent;

        public static bool CompareObjects(object o1, object o2)
        {

            var str1 = Newtonsoft.Json.JsonConvert.SerializeObject(o1);
            var str2 = Newtonsoft.Json.JsonConvert.SerializeObject(o2);
            var jtoken1 = JToken.Parse(Newtonsoft.Json.JsonConvert.SerializeObject(o1));
            var jtoken2 = JToken.Parse(Newtonsoft.Json.JsonConvert.SerializeObject(o2));

            return JToken.DeepEquals(jtoken1, jtoken2);
        }

        public LogEventTest()
        {
            LogEvent = new LogEvent(
                url: "https://logx.optimizely.com", 
                parameters: new Dictionary<string, object>
                {
                    { "accountId", "1234" },
                    { "projectId", "9876" },
                    { "visitorId", "testUser" }
                },
                httpVerb: "POST", 
                headers: new Dictionary<string, string>
                {
                    { "Content-type", "application/json" }
                });
        }

        [Fact]
        public void TestGetUrl()
        {
            Assert.Equal("https://logx.optimizely.com", LogEvent.Url);
        }

        [Fact]
        public void TestGetParams()
        {
            var testParams = new Dictionary<string, object>
            {
                { "accountId", "1234" },
                { "projectId", "9876" },
                { "visitorId", "testUser" }
            };
            Assert.True(CompareObjects(testParams, LogEvent.Params));
        }

        [Fact]
        public void TestGetHttpVerb()
        {
            Assert.Equal("POST", LogEvent.HttpVerb);
        }

        [Fact]
        public void TestGetHeaders()
        {
            var headers = new Dictionary<string, string>
            {
                { "Content-type", "application/json" }
            };
            Assert.True(CompareObjects(headers, LogEvent.Headers));
        }
    }
}
