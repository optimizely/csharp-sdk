using NUnit.Framework;
using OptimizelySDK.Bucketing;
using OptimizelySDK.Entity;
using OptimizelySDK.Event;
using OptimizelySDK.Event.Builder;
using OptimizelySDK.Logger;
using OptimizelySDK.Tests.EventTests;
using OptimizelySDK.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OptimizelySDK.Tests.UtilsTests
{
    [TestFixture]
    class EventTagUtilsTest
    {
        private string TestUserId = string.Empty;
        private ProjectConfig Config;
        private EventBuilder EventBuilder;

        [TestFixtureSetUp]
        public void Setup()
        {
            TestUserId = "testUserId";
            var logger = new NoOpLogger();
            Config = ProjectConfig.Create(TestData.Datafile, logger, new ErrorHandler.NoOpErrorHandler());
            EventBuilder = new EventBuilder(new Bucketer(logger));
        }

        [Test]
        public void TestGetRevenueValue()
        {
            var expectedValue = 42;
            var expectedValue2 = 100;
            var validTag = new Dictionary<string, object>() {
                { "revenue", 42 }
            };
            var validTag2 = new Dictionary<string, object>() {
                { "revenue", 100 }
            };

            var invalidTag = new Dictionary<string, object>() {
                { "abc", 42 }
            };
            var nullValue = new Dictionary<string, object>() {
                { "revenue", null }
            };
            var invalidValue = new Dictionary<string, object>() {
                { "revenue", 42.5 }
            };

            // Invalid data.
            Assert.Null(EventTagUtils.GetRevenueValue(null));
            Assert.Null(EventTagUtils.GetRevenueValue(invalidTag));
            Assert.Null(EventTagUtils.GetRevenueValue(nullValue));
            Assert.Null(EventTagUtils.GetRevenueValue(invalidValue));

            // Valid data.
            Assert.AreEqual(EventTagUtils.GetRevenueValue(validTag), expectedValue);
            Assert.AreEqual(EventTagUtils.GetRevenueValue(validTag2), expectedValue2);
        }

        [Test]
        public void TestGetEventValue()
        {
            int expectedValue = 42;
            float expectedValue2 = 42.5F;
            double expectedValue3 = 42.52;

            var validTag = new Dictionary<string, object>() {
                { "value", 42 }
            };

            var validTag2 = new Dictionary<string, object>() {
                { "value", 42.5 }
            };

            var validTag3 = new Dictionary<string, object>() {
                { "value", 42.52 }
            };

            var invalidTag = new Dictionary<string, object>() {
                { "abc", 42 }
            };
            var nullValue = new Dictionary<string, object>() {
                { "value", null }
            };
            var invalidValue = new Dictionary<string, object>() {
                { "value", "42" }
            };

            // Invalid data.
            Assert.Null(EventTagUtils.GetEventValue(null));
            Assert.Null(EventTagUtils.GetEventValue(invalidTag));
            Assert.Null(EventTagUtils.GetEventValue(nullValue));
            Assert.Null(EventTagUtils.GetEventValue(invalidValue));

            // Valid data.
            Assert.AreEqual(EventTagUtils.GetEventValue(validTag), expectedValue);
            Assert.AreEqual(EventTagUtils.GetEventValue(validTag2), expectedValue2);
            Assert.AreEqual(EventTagUtils.GetEventValue(validTag3), expectedValue3);
        }
    }
}
