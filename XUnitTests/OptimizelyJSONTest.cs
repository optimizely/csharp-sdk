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

using Moq;
using Xunit;
using OptimizelySDK.ErrorHandler;
using OptimizelySDK.Exceptions;
using OptimizelySDK.Logger;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OptimizelySDK.XUnitTests
{
    class ParentJson
    {
        public string strField { get; set; }
        public int intField { get; set; }
        public double doubleField { get; set; }
        public bool boolField { get; set; }
        public ObjectJson objectField { get; set; }
        
    }
    class ObjectJson
    {
        public int inner_field_int { get; set; }
        public double inner_field_double { get; set; }
        public string inner_field_string {get;set;}
        public bool inner_field_boolean { get; set; }
    }

    class Field4
    {
        public long inner_field1 { get; set; }
        public InnerField2 inner_field2 { get; set; }
    }
    class InnerField2 : List<object> { }


    public class OptimizelyJSONTest
    {
        private string Payload;
        private Dictionary<string, object> Map;
        private Mock<ILogger> LoggerMock;
        private Mock<IErrorHandler> ErrorHandlerMock;

        public OptimizelyJSONTest()
        {
            ErrorHandlerMock = new Mock<IErrorHandler>();
            ErrorHandlerMock.Setup(e => e.HandleError(It.IsAny<Exception>()));

            LoggerMock = new Mock<ILogger>();
            LoggerMock.Setup(i => i.Log(It.IsAny<LogLevel>(), It.IsAny<string>()));

            Payload = "{ \"field1\": 1, \"field2\": 2.5, \"field3\": \"three\", \"field4\": {\"inner_field1\":3,\"inner_field2\":[\"1\",\"2\", 3, 4.23, true]}, \"field5\": true, }";
            Map = new Dictionary<string, object>() {
                { "strField", "john doe" },
                { "intField", 12 },
                { "doubleField", 2.23 },
                { "boolField", true},
                { "objectField", new Dictionary<string, object> () {
                        { "inner_field_int", 3 },
                        { "inner_field_double", 13.21 },
                        { "inner_field_string", "john" },
                        { "inner_field_boolean", true }
                    }
                }
            };
        }

        [Fact]
        public void TestOptimizelyJsonObjectIsValid()
        {
            var optimizelyJSONUsingMap = new OptimizelyJSON(Map, ErrorHandlerMock.Object, LoggerMock.Object);
            var optimizelyJSONUsingString = new OptimizelyJSON(Payload, ErrorHandlerMock.Object, LoggerMock.Object);

            Assert.NotNull(optimizelyJSONUsingMap);
            Assert.NotNull(optimizelyJSONUsingString);
        }
        [Fact]
        public void TestToStringReturnValidString()
        {
            var map = new Dictionary<string, object>() {
                { "strField", "john doe" },
                { "intField", 12 },
                { "objectField", new Dictionary<string, object> () {
                        { "inner_field_int", 3 }
                    }
                }
            };
            var optimizelyJSONUsingMap = new OptimizelyJSON(map, ErrorHandlerMock.Object, LoggerMock.Object);
            string str = optimizelyJSONUsingMap.ToString();
            string expectedStringObj = "{\"strField\":\"john doe\",\"intField\":12,\"objectField\":{\"inner_field_int\":3}}";
            Assert.Equal(expectedStringObj, str);
        }

        [Fact]
        public void TestGettingErrorUponInvalidJsonString()
        {
            var optimizelyJSONUsingString = new OptimizelyJSON("{\"invalid\":}", ErrorHandlerMock.Object, LoggerMock.Object);
            LoggerMock.Verify(log => log.Log(LogLevel.ERROR, "Provided string could not be converted to map."), Times.Once);
            ErrorHandlerMock.Verify(er => er.HandleError(It.IsAny<InvalidJsonException>()), Times.Once);
        }

        [Fact]
        public void TestOptimizelyJsonGetVariablesWhenSetUsingMap()
        {
            var optimizelyJSONUsingMap = new OptimizelyJSON(Map, ErrorHandlerMock.Object, LoggerMock.Object);

            Assert.Equal("john doe", optimizelyJSONUsingMap.GetValue<string>("strField"));
            Assert.Equal(12, optimizelyJSONUsingMap.GetValue<int>("intField"));
            Assert.Equal(2.23, optimizelyJSONUsingMap.GetValue<double>("doubleField"));
            Assert.True(optimizelyJSONUsingMap.GetValue<bool>("boolField"));
            Assert.Equal(3, optimizelyJSONUsingMap.GetValue<int>("objectField.inner_field_int"));
            Assert.Equal(13.21, optimizelyJSONUsingMap.GetValue<double>("objectField.inner_field_double"));
            Assert.Equal("john", optimizelyJSONUsingMap.GetValue<string>("objectField.inner_field_string"));
            Assert.True(optimizelyJSONUsingMap.GetValue<bool>("objectField.inner_field_boolean"));
            Assert.True(optimizelyJSONUsingMap.GetValue<Dictionary<string, object>>("objectField") is Dictionary<string, object>);
        }

        [Fact]
        public void TestOptimizelyJsonGetVariablesWhenSetUsingString()
        {
            var optimizelyJSONUsingString = new OptimizelyJSON(Payload, ErrorHandlerMock.Object, LoggerMock.Object);

            Assert.Equal(1, optimizelyJSONUsingString.GetValue<long>("field1"));
            Assert.Equal(2.5, optimizelyJSONUsingString.GetValue<double>("field2"));
            Assert.Equal("three", optimizelyJSONUsingString.GetValue<string>("field3"));
            Assert.Equal(3, optimizelyJSONUsingString.GetValue<long>("field4.inner_field1"));
            Assert.True(TestData.CompareObjects(optimizelyJSONUsingString.GetValue<List<object>>("field4.inner_field2"), new List<object>() { "1", "2", 3, 4.23, true }));
        }

        [Fact]
        public void TestGetValueReturnsEntireDictWhenJsonPathIsEmptyAndTypeIsValid()
        {
            var optimizelyJSONUsingString = new OptimizelyJSON(Payload, ErrorHandlerMock.Object, LoggerMock.Object);
            var actualDict = optimizelyJSONUsingString.ToDictionary();
            var expectedValue = optimizelyJSONUsingString.GetValue<Dictionary<string, object>>("");
            Assert.NotNull(expectedValue);
            Assert.True(TestData.CompareObjects(expectedValue, actualDict));
        }

        [Fact]
        public void TestGetValueReturnsDefaultValueWhenJsonIsInvalid()
        {
            var payload = "{ \"field1\" : {1:\"Csharp\", 2:\"Java\"} }";
            var optimizelyJSONUsingString = new OptimizelyJSON(payload, ErrorHandlerMock.Object, LoggerMock.Object);
            var expectedValue = optimizelyJSONUsingString.GetValue<Dictionary<float, string>>("field1");
            // Even though above given JSON is not valid, newtonsoft is parsing it so
            Assert.NotNull(expectedValue);
        }

        [Fact]
        public void TestGetValueReturnsDefaultValueWhenTypeIsInvalid()
        {
            var payload = "{ \"field1\" : {\"1\":\"Csharp\",\"2\":\"Java\"} }";
            var optimizelyJSONUsingString = new OptimizelyJSON(payload, ErrorHandlerMock.Object, LoggerMock.Object);
            var expectedValue = optimizelyJSONUsingString.GetValue<Dictionary<float, string>>("field1");
            Assert.NotNull(expectedValue);
        }

        [Fact]
        public void TestGetValueReturnsNullWhenJsonPathIsEmptyAndTypeIsOfObject()
        {
            var optimizelyJSONUsingString = new OptimizelyJSON(Payload, ErrorHandlerMock.Object, LoggerMock.Object);
            var expectedValue = optimizelyJSONUsingString.GetValue<object>("");
            Assert.NotNull(expectedValue);
        }

        [Fact]
        public void TestGetValueReturnsDefaultValueWhenJsonPathIsEmptyAndTypeIsNotValid()
        {
            var optimizelyJSONUsingString = new OptimizelyJSON(Payload, ErrorHandlerMock.Object, LoggerMock.Object);
            var expectedValue = optimizelyJSONUsingString.GetValue<string>("");
            Assert.Null(expectedValue);
            LoggerMock.Verify(log => log.Log(LogLevel.ERROR, "Value for path could not be assigned to provided type."), Times.Once);
            ErrorHandlerMock.Verify(er => er.HandleError(It.IsAny<OptimizelyRuntimeException>()), Times.Once);
        }

        [Fact]
        public void TestGetValueReturnsDefaultValueWhenJsonPathIsInvalid()
        {
            var optimizelyJSONUsingString = new OptimizelyJSON(Payload, ErrorHandlerMock.Object, LoggerMock.Object);
            var expectedValue = optimizelyJSONUsingString.GetValue<string>("field11");
            Assert.Null(expectedValue);
            LoggerMock.Verify(log => log.Log(LogLevel.ERROR, "Value for JSON key not found."), Times.Once);
            ErrorHandlerMock.Verify(er => er.HandleError(It.IsAny<OptimizelyRuntimeException>()), Times.Once);
        }

        [Fact]
        public void TestGetValueReturnsDefaultValueWhenJsonPath1IsInvalid()
        {
            var optimizelyJSONUsingString = new OptimizelyJSON(Payload, ErrorHandlerMock.Object, LoggerMock.Object);
            var expectedValue = optimizelyJSONUsingString.GetValue<string>("field4.");
            Assert.Null(expectedValue);
            LoggerMock.Verify(log => log.Log(LogLevel.ERROR, "Value for JSON key not found."), Times.Once);
            ErrorHandlerMock.Verify(er => er.HandleError(It.IsAny<OptimizelyRuntimeException>()), Times.Once);
        }

        [Fact]
        public void TestGetValueReturnsDefaultValueWhenJsonPath2IsInvalid()
        {
            var optimizelyJSONUsingString = new OptimizelyJSON(Payload, ErrorHandlerMock.Object, LoggerMock.Object);
            var expectedValue = optimizelyJSONUsingString.GetValue<string>("field4..inner_field1");
            Assert.Null(expectedValue);
            LoggerMock.Verify(log => log.Log(LogLevel.ERROR, "Value for JSON key not found."), Times.Once);
            ErrorHandlerMock.Verify(er => er.HandleError(It.IsAny<OptimizelyRuntimeException>()), Times.Once);
        } 
        
        [Fact]
        public void TestGetValueObjectNotModifiedIfCalledTwice()
        {
            var optimizelyJSONUsingString = new OptimizelyJSON(Payload, ErrorHandlerMock.Object, LoggerMock.Object);
            var expectedValue = optimizelyJSONUsingString.GetValue<string>("field4.inner_field1");
            var expectedValue2 = optimizelyJSONUsingString.GetValue<string>("field4.inner_field1");

            Assert.Equal(expectedValue, expectedValue2);
        }

        [Fact]
        public void TestGetValueReturnsUsingGivenClassType()
        {
            var optimizelyJSONUsingString = new OptimizelyJSON(Payload, ErrorHandlerMock.Object, LoggerMock.Object);
            var expectedValue = optimizelyJSONUsingString.GetValue<Field4>("field4");

            Assert.Equal(3, expectedValue.inner_field1);
            Assert.Equal(new InnerField2() { "1", "2", 3L, 4.23, true }, expectedValue.inner_field2);
        }

        [Fact]
        public void TestGetValueReturnsCastedObject()
        {
            var optimizelyJson = new OptimizelyJSON(Map, ErrorHandlerMock.Object, LoggerMock.Object);
            var expectedValue = optimizelyJson.ToDictionary();
            var actualValue = optimizelyJson.GetValue<ParentJson>(null);
            
            Assert.True(TestData.CompareObjects(actualValue, expectedValue));
        }
    }
}
