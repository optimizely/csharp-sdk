/* 
 * Copyright 2020, Optimizely
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
using OptimizelySDK.Logger;
using OptimizelySDK.Utils;
using System.Collections.Generic;
using Xunit;

namespace OptimizelySDK.XUnitTests.UtilsTests
{
    public class EventTagUtilsTest : UtilsData
    {
        private Mock<ILogger> LoggerMock;
        private ILogger Logger;


        public EventTagUtilsTest()
        {
            LoggerMock = new Mock<ILogger>();
            LoggerMock.Setup(i => i.Log(It.IsAny<LogLevel>(), It.IsAny<string>()));

            Logger = LoggerMock.Object;
        }

        [Theory]
        [MemberData(nameof(InvalidTagsKeyNotDefined))]
        public void TestGetRevenueKeyNotDefined(Dictionary<string, object> tag)
        {
            Assert.Null(EventTagUtils.GetRevenueValue(tag, Logger));

            //LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "Event tags is undefined."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "The revenue key is not defined in the event tags."), Times.Exactly(1));
            // LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "The revenue key value is not defined in event tags."), Times.Once);
            //LoggerMock.Verify(l => l.Log(LogLevel.ERROR, "Revenue value is not an integer or couldn't be parsed as an integer."), Times.Once);
        }

        [Theory]
        [MemberData(nameof(InvalidTagsUnDefined))]
        public void TestGetRevenueKeyUndefined(Dictionary<string, object> tag)
        {
            // Invalid data.
            Assert.Null(EventTagUtils.GetRevenueValue(tag, Logger));

            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "Event tags is undefined."), Times.Once);
            //LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "The revenue key is not defined in the event tags."), Times.Exactly(1));
            // LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "The revenue key value is not defined in event tags."), Times.Once);
            //LoggerMock.Verify(l => l.Log(LogLevel.ERROR, "Revenue value is not an integer or couldn't be parsed as an integer."), Times.Once);
        }

        [Theory]
        [MemberData(nameof(InvalidRevenueNotInt))]
        public void TestGetRevenueValueNotInteger(Dictionary<string, object> tag)
        {
            // Invalid data.
            Assert.Null(EventTagUtils.GetRevenueValue(tag, Logger));

            LoggerMock.Verify(l => l.Log(LogLevel.ERROR, "Revenue value is not an integer or couldn't be parsed as an integer."), Times.Once);
        }

        [Theory]
        [MemberData(nameof(InvalidTagsNotDefinedInEventTags))]
        public void TestGetRevenueKeyValueIsNotDefinedInEventTags(Dictionary<string, object> tag)
        {
            Assert.Null(EventTagUtils.GetRevenueValue(tag, Logger));

            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "The revenue key value is not defined in event tags."), Times.Once);
        }

        [Theory]
        [MemberData(nameof(ValidRevenueTagsData))]
        public void TestGetRevenueValueValid(Dictionary<string, object> tag, int expectedValue)
        {
            // Valid data.
            Assert.Equal(EventTagUtils.GetRevenueValue(tag, Logger), expectedValue);

            LoggerMock.Verify(l => l.Log(LogLevel.INFO, $"The revenue value {expectedValue} will be sent to results."), Times.Once);
        }


        [Fact]
        public void TestGetEventValueInvalid()
        {
            var invalidTag = new Dictionary<string, object>() {
                { "abc", 42 }
            };
            var nullValue = new Dictionary<string, object>() {
                { "value", null }
            };

            // Invalid data.
            Assert.Null(EventTagUtils.GetNumericValue(null, Logger));
            Assert.Null(EventTagUtils.GetNumericValue(invalidTag, Logger));
            Assert.Null(EventTagUtils.GetNumericValue(nullValue, Logger));

            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "Event tags is undefined."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "The numeric metric key is not in event tags."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "The numeric metric key value is not defined in event tags."), Times.Once);

        }


        [Theory]
        [MemberData(nameof(ValidEventValueData))]
        public void TestGetEventValue(Dictionary<string, object> tag, object expectedValue)
        {
            // Valid data.
            Assert.Equal(expectedValue, EventTagUtils.GetNumericValue(tag, Logger));
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, $"The numeric metric value {expectedValue} will be sent to results."), Times.Once);
        }

        [Fact]
        public void TestGetNumericMetricInvalidArgs()
        {
            Assert.Null(EventTagUtils.GetNumericValue(null, Logger));
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "Event tags is undefined."), Times.Once);

            //Errors for all, because it accepts only dictionary// 
            // Not valid test cases in C# 
            //Assert.Null(EventTagUtils.GetEventValue(0.5));
            //Assert.Null(EventTagUtils.GetEventValue(65536));
            //Assert.Null(EventTagUtils.GetEventValue(9223372036854775807));
            //Assert.Null(EventTagUtils.GetEventValue('9223372036854775807'));
            //Assert.Null(EventTagUtils.GetEventValue(True));
            //Assert.Null(EventTagUtils.GetEventValue(False));
        }

        [Theory]
        [MemberData(nameof(NumericMetricInvalidValueTagData))]
        public void TestGetNumericMetricInvalidValueTag(Dictionary<string, object> tag)
        {

            // Test that numeric value is not returned when revenue event tag has invalid data type.
            Assert.Null(EventTagUtils.GetNumericValue(tag, Logger));

            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "The numeric metric key is not in event tags."), Times.Exactly(1));
        }

        [Fact]

        public void TestGetNumericMetricValueTag()
        {

            // An integer should be cast to a float
            Assert.Equal(12345.0f, EventTagUtils.GetNumericValue(new Dictionary<string, object> { { "value", 12345 } }, Logger));

            // A string should be cast to a float
            Assert.Equal(12345.0f, EventTagUtils.GetNumericValue(new Dictionary<string, object> { { "value", "12345" } }, Logger));

            // Valid float values
            float someFloat = 1.2345F;

            Assert.Equal(someFloat, EventTagUtils.GetNumericValue(new Dictionary<string, object> { { "value", someFloat } }, Logger));

            float maxFloat = float.MaxValue;
            Assert.Equal(maxFloat, EventTagUtils.GetNumericValue(new Dictionary<string, object> { { "value", maxFloat } }, Logger));


            float minFloat = float.MinValue;
            Assert.Equal(minFloat, EventTagUtils.GetNumericValue(new Dictionary<string, object> { { "value", minFloat } }, Logger));

            // Invalid values
            Assert.Null(EventTagUtils.GetNumericValue(new Dictionary<string, object> { { "value", false } }, Logger));
            Assert.Null(EventTagUtils.GetNumericValue(new Dictionary<string, object> { { "value", null } }, Logger));

            Assert.Null(EventTagUtils.GetNumericValue(new Dictionary<string, object> { { "value", true } }, Logger));
            Assert.Null(EventTagUtils.GetNumericValue(new Dictionary<string, object> { { "value", new int[] { } } }, Logger));

            var numericValueArray = EventTagUtils.GetNumericValue(new Dictionary<string, object> { { "value", new object[] { } } }, Logger);
            Assert.Null(numericValueArray);


            var numericValueNone = EventTagUtils.GetNumericValue(new Dictionary<string, object> { { "value", null } }, Logger);
            Assert.Null(numericValueNone);


            var numericValueOverflow = EventTagUtils.GetNumericValue(new Dictionary<string, object> { { "value", float.MaxValue * 10 } }, Logger);
            Assert.Null(numericValueOverflow);

            Assert.Equal(0.0f, EventTagUtils.GetNumericValue(new Dictionary<string, object> { { "value", 0.0 } }, Logger));

            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "The numeric metric value 12345 will be sent to results."), Times.Exactly(2));
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, $"The numeric metric value {maxFloat} will be sent to results."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, $"The numeric metric value {minFloat} will be sent to results."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.ERROR, $"Provided numeric value {float.PositiveInfinity} is in an invalid format."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "The numeric metric value 0 will be sent to results."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.ERROR, "Provided numeric value is boolean which is an invalid format."), Times.Exactly(2));
            LoggerMock.Verify(l => l.Log(LogLevel.ERROR, "Numeric metric value is not in integer, float, or string form."), Times.Exactly(2));

            /* Value is converted into 1234F */
            //var numericValueInvalidLiteral = EventTagUtils.GetEventValue(new Dictionary<string, object> { { "value", "1,234" } });
            //Assert.Null(numericValueInvalidLiteral, string.Format("Invalid string literal value is {0}", numericValueInvalidLiteral));

            /* Not valid use cases in C# */
            // float - inf is not possible.
            // float -inf is not possible.
        }

    }
}
