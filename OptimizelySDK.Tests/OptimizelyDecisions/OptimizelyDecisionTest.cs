using Moq;
using NUnit.Framework;
using OptimizelySDK.ErrorHandler;
using OptimizelySDK.Logger;
using OptimizelySDK.OptimizelyDecisions;
using System;
using System.Collections.Generic;

namespace OptimizelySDK.Tests.OptimizelyDecisions
{
    [TestFixture]
    public class OptimizelyDecisionTest
    {
        private Mock<ILogger> LoggerMock;
        private Mock<IErrorHandler> ErrorHandlerMock;

        [SetUp]
        public void Initialize()
        {
            ErrorHandlerMock = new Mock<IErrorHandler>();
            ErrorHandlerMock.Setup(e => e.HandleError(It.IsAny<Exception>()));

            LoggerMock = new Mock<ILogger>();
            LoggerMock.Setup(i => i.Log(It.IsAny<LogLevel>(), It.IsAny<string>()));
        }
        
        [Test]
        public void TestNewErrorDecision()
        {
            var optimizelyDecision = OptimizelyDecision.NewErrorDecision("var_key", null, "some error message", ErrorHandlerMock.Object, LoggerMock.Object);
            Assert.IsNull(optimizelyDecision.VariationKey);
            Assert.AreEqual(optimizelyDecision.FlagKey, "var_key");
            Assert.AreEqual(optimizelyDecision.Variables.ToDictionary(), new Dictionary<string, object>());
            Assert.AreEqual(optimizelyDecision.Reasons, new List<string>() { "some error message" });
            Assert.IsNull(optimizelyDecision.RuleKey);
            Assert.False(optimizelyDecision.Enabled);
        }

        [Test]
        public void TestNewDecision()
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
            string expectedStringObj = "{\"strField\":\"john doe\",\"intField\":12,\"objectField\":{\"inner_field_int\":3}}";

            var optimizelyDecision = new OptimizelyDecision("var_key",
                true,
                optimizelyJSONUsingMap,
                "experiment",
                "feature_key",
                null,
                new List<string>());
            Assert.AreEqual(optimizelyDecision.VariationKey, "var_key");
            Assert.AreEqual(optimizelyDecision.FlagKey, "feature_key");
            Assert.AreEqual(optimizelyDecision.Variables.ToString(), expectedStringObj);
            Assert.AreEqual(optimizelyDecision.Reasons, new List<string>());
            Assert.AreEqual(optimizelyDecision.RuleKey, "experiment");
            Assert.True(optimizelyDecision.Enabled);
        }

    }
}
