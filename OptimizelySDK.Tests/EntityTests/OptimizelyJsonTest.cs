using Moq;
using NUnit.Framework;
using OptimizelySDK.Entity;
using OptimizelySDK.Logger;
using System.Collections.Generic;

namespace OptimizelySDK.Tests.EntityTests
{
    [TestFixture]
    public class OptimizelyJsonTest
    {
        private string Payload;
        private Dictionary<string, object> Map;
        private Mock<ILogger> LoggerMock;

        [SetUp]
        public void Initialize()
        {
            LoggerMock = new Mock<ILogger>();
            LoggerMock.Setup(i => i.Log(It.IsAny<LogLevel>(), It.IsAny<string>()));

            Payload = "{ \"field1\": 1, \"field2\": 2.5, \"field3\": \"three\", \"field4\": {\"inner_field1\":3,\"inner_field2\":[\"1\",\"2\", 3.01, 4.23, true]}, \"field5\": true, }";
            
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

        [Test]
        public void TestOptimizelyJsonObjectIsValid()
        {
            OptimizelyJson OptimizelyJSONUsingMap = new OptimizelyJson(Map, LoggerMock.Object);
            OptimizelyJson OptimizelyJSONUsingString = new OptimizelyJson(Payload, LoggerMock.Object);

            Assert.IsNotNull(OptimizelyJSONUsingMap); 
            Assert.IsNotNull(OptimizelyJSONUsingString); 
        }
        [Test]
        public void TestToStringReturnValidString()
        {
            Dictionary<string, object> map = new Dictionary<string, object>() {
                { "strField", "john doe" },
                { "intField", 12 },
                { "objectField", new Dictionary<string, object> () {
                        { "inner_field_int", 3 }
                    }
                } 
            };
            OptimizelyJson OptimizelyJSONUsingMap = new OptimizelyJson(map, LoggerMock.Object);
            string str = OptimizelyJSONUsingMap.ToString();
            string expectedStringObj = "{\"strField\":\"john doe\",\"intField\":12,\"objectField\":{\"inner_field_int\":3}}";
            Assert.AreEqual(expectedStringObj, str);
        }

        [Test]
        public void TestGettingErrorUponInvalidJsonString()
        {
            OptimizelyJson OptimizelyJSONUsingString = new OptimizelyJson("{\"invalid\":}", LoggerMock.Object);
            LoggerMock.Verify(log => log.Log(LogLevel.ERROR, "Provided string could not be converted to map."), Times.Once);
        }

        [Test]
        public void TestGettingErrorUponNotFindingValuePath()
        {
            OptimizelyJson OptimizelyJSONUsingString = new OptimizelyJson("{\"invalid\":}", LoggerMock.Object);
            LoggerMock.Verify(log => log.Log(LogLevel.ERROR, "Provided string could not be converted to map."), Times.Once);
        }

        [Test]
        public void TestOptimizelyJsonGetVariablesWhenSetUsingMap()
        {
            OptimizelyJson OptimizelyJSONUsingMap = new OptimizelyJson(Map, LoggerMock.Object);

            Assert.AreEqual(OptimizelyJSONUsingMap.GetValue<string>("strField"), "john doe");
            Assert.AreEqual(OptimizelyJSONUsingMap.GetValue<int>("intField"), 12);
            Assert.AreEqual(OptimizelyJSONUsingMap.GetValue<double>("doubleField"), 2.23);
            Assert.AreEqual(OptimizelyJSONUsingMap.GetValue<bool>("boolField"), true);
            Assert.AreEqual(OptimizelyJSONUsingMap.GetValue<int>("objectField.inner_field_int"), 3);
            Assert.AreEqual(OptimizelyJSONUsingMap.GetValue<double>("objectField.inner_field_double"), 13.21);
            Assert.AreEqual(OptimizelyJSONUsingMap.GetValue<string>("objectField.inner_field_string"), "john");
            Assert.AreEqual(OptimizelyJSONUsingMap.GetValue<bool>("objectField.inner_field_boolean"), true);
            Assert.IsTrue(OptimizelyJSONUsingMap.GetValue<Dictionary<string, object>>("objectField") is Dictionary<string, object>);
        }

        [Test]
        public void TestOptimizelyJsonGetVariablesWhenSetUsingString()
        {
            OptimizelyJson OptimizelyJSONUsingString = new OptimizelyJson(Payload, LoggerMock.Object);

            Assert.AreEqual(OptimizelyJSONUsingString.GetValue<long>("field1"), 1);
            Assert.AreEqual(OptimizelyJSONUsingString.GetValue<double>("field2"), 2.5);
            Assert.AreEqual(OptimizelyJSONUsingString.GetValue<string>("field3"), "three");
            Assert.AreEqual(OptimizelyJSONUsingString.GetValue<long>("field4.inner_field1"), 3);
            Assert.AreEqual(OptimizelyJSONUsingString.GetValue<object[]>("field4.inner_field2"), new object[] { "1", "2", 3.01, 4.23, true });
        }
    }
}