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
            var validTagStr = new Dictionary<string, object>() {
                { "value", "42" }
            };
            var validTagStr1 = new Dictionary<string, object>() {
                { "value", "42.3" }
            };

            // Invalid data.
            Assert.Null(EventTagUtils.GetNumericValue(null));
            Assert.Null(EventTagUtils.GetNumericValue(invalidTag));
            Assert.Null(EventTagUtils.GetNumericValue(nullValue));


            // Valid data.
            Assert.AreEqual(42, EventTagUtils.GetNumericValue(validTagStr));
            Assert.AreEqual("42.3", EventTagUtils.GetNumericValue(validTagStr1).ToString());
            Assert.AreEqual(EventTagUtils.GetNumericValue(validTag), expectedValue);
            Assert.AreEqual(EventTagUtils.GetNumericValue(validTag2), expectedValue2);
            Assert.AreEqual(EventTagUtils.GetNumericValue(validTag3).ToString(), expectedValue3.ToString());
        }

        [Test]
        public void TestGetNumericMetricInvalidArgs()
        {
            Assert.IsNull(EventTagUtils.GetNumericValue(null));

            //Errors for all, because it accepts only dictionary// 
            // Not valid test cases in C# 
            //Assert.IsNull(EventTagUtils.GetEventValue(0.5));
            //Assert.IsNull(EventTagUtils.GetEventValue(65536));
            //Assert.IsNull(EventTagUtils.GetEventValue(9223372036854775807));
            //Assert.IsNull(EventTagUtils.GetEventValue('9223372036854775807'));
            //Assert.IsNull(EventTagUtils.GetEventValue(True));
            //Assert.IsNull(EventTagUtils.GetEventValue(False));
        }

        
        [Test]
        public void TestGetNumericMetricNoValueTag()
        {
            // Test that numeric value is not returned when there's no numeric event tag.
            Assert.IsNull(EventTagUtils.GetNumericValue(new Dictionary<string, object> { }));
            Assert.IsNull(EventTagUtils.GetNumericValue(new Dictionary<string, object> {
                { "non-value", 42 }
            }));

            //Errors for all, because it accepts only dictionary// 
            //Assert.IsNull(EventTagUtils.GetEventValue(new object[] { }));
        }

        [Test]
        public void TestGetNumericMetricInvalidValueTag()
        {
            
            // Test that numeric value is not returned when revenue event tag has invalid data type.

            Assert.IsNull(EventTagUtils.GetNumericValue(new Dictionary<string, object> { { "non-value", null } }));
            Assert.IsNull(EventTagUtils.GetNumericValue(new Dictionary<string, object> { { "non-value",  0.5} }));
            Assert.IsNull(EventTagUtils.GetNumericValue(new Dictionary<string, object> { { "non-value",  12345} }));
            Assert.IsNull(EventTagUtils.GetNumericValue(new Dictionary<string, object> { { "non-value", "65536" } }));
            Assert.IsNull(EventTagUtils.GetNumericValue(new Dictionary<string, object> { { "non-value", true } }));
            Assert.IsNull(EventTagUtils.GetNumericValue(new Dictionary<string, object> { { "non-value", false } }));
            Assert.IsNull(EventTagUtils.GetNumericValue(new Dictionary<string, object> { { "non-value", new object[] { 1, 2, 3 } } }));
            Assert.IsNull(EventTagUtils.GetNumericValue(new Dictionary<string, object> { { "non-value", new object[] { 'a', 'b', 'c' } } }));
        }

        [Test]

        public void TestGetNumericMetricValueTag()
        {

            // An integer should be cast to a float
            Assert.AreEqual(12345.0, EventTagUtils.GetNumericValue(new Dictionary<string, object> { { "value", 12345 } }, new Logger.DefaultLogger()));

            // A string should be cast to a float
            Assert.AreEqual(12345.0, EventTagUtils.GetNumericValue(new Dictionary<string, object> { { "value", "12345" } }, new Logger.DefaultLogger()));
            
            // Valid float values
            float someFloat = 1.2345F;

            Assert.AreEqual(someFloat, EventTagUtils.GetNumericValue(new Dictionary<string, object> { { "value", someFloat } }, new Logger.DefaultLogger()));

            float maxFloat = float.MaxValue;
            Assert.AreEqual(maxFloat, EventTagUtils.GetNumericValue(new Dictionary<string, object> { { "value", maxFloat } }, new Logger.DefaultLogger()));


            float minFloat = float.MinValue;
            Assert.AreEqual(minFloat, EventTagUtils.GetNumericValue(new Dictionary<string, object> { { "value", minFloat } }, new Logger.DefaultLogger()));

            // Invalid values
            Assert.IsNull(EventTagUtils.GetNumericValue(new Dictionary<string, object> { { "value", false } }, new Logger.DefaultLogger()));
            Assert.IsNull(EventTagUtils.GetNumericValue(new Dictionary<string, object> { { "value", null } }, new Logger.DefaultLogger()));

            var numericValueArray = EventTagUtils.GetNumericValue(new Dictionary<string, object> { { "value", new object[] { }} });
            Assert.IsNull(numericValueArray, string.Format("Array numeric value is {0}", numericValueArray));


            var numericValueNone = EventTagUtils.GetNumericValue(new Dictionary<string, object> { { "value", null } });
            Assert.IsNull(numericValueNone, string.Format("None numeric value is {0}", numericValueNone));


            var numericValueOverflow = EventTagUtils.GetNumericValue(new Dictionary<string, object> { { "value", float.MaxValue * 10 } });
            Assert.IsNull(numericValueOverflow, string.Format("Max numeric value is {0}", float.MaxValue * 10 ));

            Assert.AreEqual(0.0, EventTagUtils.GetNumericValue(new Dictionary<string, object> { { "value", 0.0 } }));


            /* Value is converted into 1234F */
            //var numericValueInvalidLiteral = EventTagUtils.GetEventValue(new Dictionary<string, object> { { "value", "1,234" } });
            //Assert.IsNull(numericValueInvalidLiteral, string.Format("Invalid string literal value is {0}", numericValueInvalidLiteral));

            /* Not valid use cases in C# */
            // float - inf is not possible.
            // float -inf is not possible.
        }

    }
}
