/* 
 * Copyright 2017-2018, Optimizely
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Moq;
using NUnit.Framework;
using OptimizelySDK.Logger;
using OptimizelySDK.Utils;
using System.Collections.Generic;

namespace OptimizelySDK.Tests.UtilsTests
{
    [TestFixture]
    internal class EventTagUtilsTest
    {
        private Mock<ILogger> LoggerMock;
        private ILogger Logger;

        [SetUp]
        public void Setup()
        {
            LoggerMock = new Mock<ILogger>();
            LoggerMock.Setup(i => i.Log(It.IsAny<LogLevel>(), It.IsAny<string>()));

            Logger = LoggerMock.Object;
        }

        [Test]
        public void TestGetRevenueValue()
        {
            var expectedValue = 42;
            var expectedValue2 = 100;
            var expectedValueString = 123;
            var validTag = new Dictionary<string, object>()
            {
                { "revenue", 42 },
            };
            var validTag2 = new Dictionary<string, object>()
            {
                { "revenue", 100 },
            };
            var validTagStringValue = new Dictionary<string, object>()
            {
                { "revenue", "123" },
            };

            var invalidTag = new Dictionary<string, object>()
            {
                { "abc", 42 },
            };
            var nullValue = new Dictionary<string, object>()
            {
                { "revenue", null },
            };
            var invalidValue = new Dictionary<string, object>()
            {
                { "revenue", 42.5 },
            };
            var invalidTagNonRevenue = new Dictionary<string, object>()
            {
                { "non-revenue", 123 },
            };

            // Invalid data.
            Assert.Null(EventTagUtils.GetRevenueValue(null, Logger));
            Assert.Null(EventTagUtils.GetRevenueValue(invalidTag, Logger));
            Assert.Null(EventTagUtils.GetRevenueValue(nullValue, Logger));
            Assert.Null(EventTagUtils.GetRevenueValue(invalidValue, Logger));
            Assert.Null(EventTagUtils.GetRevenueValue(invalidTagNonRevenue, Logger));

            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "Event tags is undefined."), Times.Once);
            LoggerMock.Verify(
                l => l.Log(LogLevel.DEBUG, "The revenue key is not defined in the event tags."),
                Times.Exactly(2));
            LoggerMock.Verify(
                l => l.Log(LogLevel.DEBUG, "The revenue key value is not defined in event tags."),
                Times.Once);
            LoggerMock.Verify(
                l => l.Log(LogLevel.ERROR,
                    "Revenue value is not an integer or couldn't be parsed as an integer."),
                Times.Once);

            // Valid data.
            Assert.AreEqual(EventTagUtils.GetRevenueValue(validTag, Logger), expectedValue);
            Assert.AreEqual(EventTagUtils.GetRevenueValue(validTag2, Logger), expectedValue2);
            Assert.AreEqual(EventTagUtils.GetRevenueValue(validTagStringValue, Logger),
                expectedValueString);

            LoggerMock.Verify(
                l => l.Log(LogLevel.INFO,
                    $"The revenue value {expectedValue} will be sent to results."), Times.Once);
            LoggerMock.Verify(
                l => l.Log(LogLevel.INFO,
                    $"The revenue value {expectedValue2} will be sent to results."), Times.Once);
            LoggerMock.Verify(
                l => l.Log(LogLevel.INFO,
                    $"The revenue value {expectedValueString} will be sent to results."),
                Times.Once);
        }

        [Test]
        public void TestGetEventValue()
        {
            var expectedValue = 42;
            var expectedValue2 = 42.5F;
            var expectedValue3 = 42.52;

            var validTag = new Dictionary<string, object>()
            {
                { "value", 42 },
            };

            var validTag2 = new Dictionary<string, object>()
            {
                { "value", 42.5 },
            };

            var validTag3 = new Dictionary<string, object>()
            {
                { "value", 42.52 },
            };

            var invalidTag = new Dictionary<string, object>()
            {
                { "abc", 42 },
            };
            var nullValue = new Dictionary<string, object>()
            {
                { "value", null },
            };
            var validTagStr = new Dictionary<string, object>()
            {
                { "value", "42" },
            };
            var validTagStr1 = new Dictionary<string, object>()
            {
                { "value", "42.3" },
            };

            // Invalid data.
            Assert.Null(EventTagUtils.GetNumericValue(null, Logger));
            Assert.Null(EventTagUtils.GetNumericValue(invalidTag, Logger));
            Assert.Null(EventTagUtils.GetNumericValue(nullValue, Logger));

            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "Event tags is undefined."), Times.Once);
            LoggerMock.Verify(
                l => l.Log(LogLevel.DEBUG, "The numeric metric key is not in event tags."),
                Times.Once);
            LoggerMock.Verify(
                l => l.Log(LogLevel.DEBUG,
                    "The numeric metric key value is not defined in event tags."), Times.Once);

            // Valid data.
            Assert.AreEqual(42, EventTagUtils.GetNumericValue(validTagStr, Logger));
            Assert.AreEqual("42.3", EventTagUtils.GetNumericValue(validTagStr1, Logger).ToString());
            Assert.AreEqual(EventTagUtils.GetNumericValue(validTag, Logger), expectedValue);
            Assert.AreEqual(EventTagUtils.GetNumericValue(validTag2, Logger), expectedValue2);
            Assert.AreEqual(EventTagUtils.GetNumericValue(validTag3, Logger).ToString(),
                expectedValue3.ToString());

            LoggerMock.Verify(
                l => l.Log(LogLevel.INFO, "The numeric metric value 42.3 will be sent to results."),
                Times.Once);
            LoggerMock.Verify(
                l => l.Log(LogLevel.INFO,
                    $"The numeric metric value {expectedValue} will be sent to results."),
                Times.Exactly(2));
            LoggerMock.Verify(
                l => l.Log(LogLevel.INFO,
                    $"The numeric metric value {expectedValue2} will be sent to results."),
                Times.Once);
            LoggerMock.Verify(
                l => l.Log(LogLevel.INFO,
                    $"The numeric metric value {expectedValue3} will be sent to results."),
                Times.Once);
        }

        [Test]
        public void TestGetNumericMetricInvalidArgs()
        {
            Assert.IsNull(EventTagUtils.GetNumericValue(null, Logger));
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "Event tags is undefined."), Times.Once);

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
            Assert.IsNull(
                EventTagUtils.GetNumericValue(new Dictionary<string, object> { }, Logger));
            Assert.IsNull(
                EventTagUtils.GetNumericValue(
                    new Dictionary<string, object> { { "non-value", 42 } }, Logger));

            LoggerMock.Verify(
                l => l.Log(LogLevel.DEBUG, "The numeric metric key is not in event tags."),
                Times.Exactly(2));

            //Errors for all, because it accepts only dictionary// 
            //Assert.IsNull(EventTagUtils.GetEventValue(new object[] { }));
        }

        [Test]
        public void TestGetNumericMetricInvalidValueTag()
        {
            // Test that numeric value is not returned when revenue event tag has invalid data type.

            Assert.IsNull(
                EventTagUtils.GetNumericValue(
                    new Dictionary<string, object> { { "non-value", null } }, Logger));
            Assert.IsNull(
                EventTagUtils.GetNumericValue(
                    new Dictionary<string, object> { { "non-value", 0.5 } }, Logger));
            Assert.IsNull(EventTagUtils.GetNumericValue(
                new Dictionary<string, object> { { "non-value", 12345 } }, Logger));
            Assert.IsNull(EventTagUtils.GetNumericValue(
                new Dictionary<string, object> { { "non-value", "65536" } }, Logger));
            Assert.IsNull(
                EventTagUtils.GetNumericValue(
                    new Dictionary<string, object> { { "non-value", true } }, Logger));
            Assert.IsNull(EventTagUtils.GetNumericValue(
                new Dictionary<string, object> { { "non-value", false } }, Logger));
            Assert.IsNull(EventTagUtils.GetNumericValue(
                new Dictionary<string, object> { { "non-value", new object[] { 1, 2, 3 } } },
                Logger));
            Assert.IsNull(EventTagUtils.GetNumericValue(
                new Dictionary<string, object> { { "non-value", new object[] { 'a', 'b', 'c' } } },
                Logger));

            LoggerMock.Verify(
                l => l.Log(LogLevel.DEBUG, "The numeric metric key is not in event tags."),
                Times.Exactly(8));
        }

        [Test]
        public void TestGetNumericMetricValueTag()
        {
            // An integer should be cast to a float
            Assert.AreEqual(12345.0,
                EventTagUtils.GetNumericValue(new Dictionary<string, object> { { "value", 12345 } },
                    Logger));

            // A string should be cast to a float
            Assert.AreEqual(12345.0,
                EventTagUtils.GetNumericValue(
                    new Dictionary<string, object> { { "value", "12345" } }, Logger));

            // Valid float values
            var someFloat = 1.2345F;

            Assert.AreEqual(someFloat,
                EventTagUtils.GetNumericValue(
                    new Dictionary<string, object> { { "value", someFloat } }, Logger));

            var maxFloat = float.MaxValue;
            Assert.AreEqual(maxFloat,
                EventTagUtils.GetNumericValue(
                    new Dictionary<string, object> { { "value", maxFloat } }, Logger));


            var minFloat = float.MinValue;
            Assert.AreEqual(minFloat,
                EventTagUtils.GetNumericValue(
                    new Dictionary<string, object> { { "value", minFloat } }, Logger));

            // Invalid values
            Assert.IsNull(
                EventTagUtils.GetNumericValue(new Dictionary<string, object> { { "value", false } },
                    Logger));
            Assert.IsNull(
                EventTagUtils.GetNumericValue(new Dictionary<string, object> { { "value", null } },
                    Logger));

            Assert.IsNull(
                EventTagUtils.GetNumericValue(new Dictionary<string, object> { { "value", true } },
                    Logger));
            Assert.IsNull(EventTagUtils.GetNumericValue(
                new Dictionary<string, object> { { "value", new int[] { } } }, Logger));

            var numericValueArray = EventTagUtils.GetNumericValue(
                new Dictionary<string, object> { { "value", new object[] { } } }, Logger);
            Assert.IsNull(numericValueArray,
                string.Format("Array numeric value is {0}", numericValueArray));


            var numericValueNone =
                EventTagUtils.GetNumericValue(new Dictionary<string, object> { { "value", null } },
                    Logger);
            Assert.IsNull(numericValueNone,
                string.Format("None numeric value is {0}", numericValueNone));


            var numericValueOverflow = EventTagUtils.GetNumericValue(
                new Dictionary<string, object> { { "value", float.MaxValue * 10 } }, Logger);
            Assert.IsNull(numericValueOverflow,
                string.Format("Max numeric value is {0}", float.MaxValue * 10));

            Assert.AreEqual(0.0,
                EventTagUtils.GetNumericValue(new Dictionary<string, object> { { "value", 0.0 } },
                    Logger));

            LoggerMock.Verify(
                l => l.Log(LogLevel.INFO,
                    "The numeric metric value 12345 will be sent to results."), Times.Exactly(2));
            LoggerMock.Verify(
                l => l.Log(LogLevel.INFO,
                    $"The numeric metric value {maxFloat} will be sent to results."), Times.Once);
            LoggerMock.Verify(
                l => l.Log(LogLevel.INFO,
                    $"The numeric metric value {minFloat} will be sent to results."), Times.Once);
            LoggerMock.Verify(
                l => l.Log(LogLevel.ERROR,
                    $"Provided numeric value {float.PositiveInfinity} is in an invalid format."),
                Times.Once);
            LoggerMock.Verify(
                l => l.Log(LogLevel.INFO, "The numeric metric value 0 will be sent to results."),
                Times.Once);
            LoggerMock.Verify(
                l => l.Log(LogLevel.ERROR,
                    "Provided numeric value is boolean which is an invalid format."),
                Times.Exactly(2));
            LoggerMock.Verify(
                l => l.Log(LogLevel.ERROR,
                    "Numeric metric value is not in integer, float, or string form."),
                Times.Exactly(2));

            /* Value is converted into 1234F */
            //var numericValueInvalidLiteral = EventTagUtils.GetEventValue(new Dictionary<string, object> { { "value", "1,234" } });
            //Assert.IsNull(numericValueInvalidLiteral, string.Format("Invalid string literal value is {0}", numericValueInvalidLiteral));

            /* Not valid use cases in C# */
            // float - inf is not possible.
            // float -inf is not possible.
        }
    }
}
