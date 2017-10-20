
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using OptimizelySDK.Event;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OptimizelySDK.Tests.EventTests
{
    [TestFixture]
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

        [OneTimeSetUp]
        public void Setup()
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

        [Test]
        public void TestGetUrl()
        {
            Assert.AreEqual("https://logx.optimizely.com", LogEvent.Url);
        }

        [Test]
        public void TestGetParams()
        {
            var testParams = new Dictionary<string, object>
            {
                { "accountId", "1234" },
                { "projectId", "9876" },
                { "visitorId", "testUser" }
            };
            Assert.IsTrue(CompareObjects(testParams, LogEvent.Params));
        }

        [Test]
        public void TestGetHttpVerb()
        {
            Assert.AreEqual("POST", LogEvent.HttpVerb);
        }

        [Test]
        public void TestGetHeaders()
        {
            var headers = new Dictionary<string, string>
            {
                { "Content-type", "application/json" }
            };
            Assert.IsTrue(CompareObjects(headers, LogEvent.Headers));
        }
    }
}
