using OptimizelySDK.Entity;
using OptimizelySDK.Logger;
using Moq;
using OptimizelySDK.Event.Builder;
using OptimizelySDK.Event;
using System.Collections.Generic;
using System;
using NUnit.Framework;
using OptimizelySDK.Bucketing;

namespace OptimizelySDK.Tests.EventTests
{
    [TestFixture]
    public class EventBuilderTest
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
        public long SecondsSince1970()
        {
            return (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
        }

        [Test]
        public void TestCreateImpressionEventNoAttributes()
        {
            var expectedLogEvent = new LogEvent("https://logx.optimizely.com/log/decision",
                new Dictionary<string, object>
                {
                    {"projectId", "7720880029" },
                    {"accountId", "1592310167" },
                    {"layerId", "7719770039" },
                    {"visitorId", "testUserId" },
                    {"clientEngine", "csharp-sdk" },
                    {"clientVersion", "1.1.1" },
                    {"timestamp", SecondsSince1970() * 1000L },
                    {"isGlobalHoldback", false },
                    {"userFeatures", new string[0] },
                    {
                        "decision",new Dictionary<string, object>
                        {
                            { "experimentId", "7716830082" },
                            { "variationId", "77210100090" },
                            { "isLayerHoldback", false }
                        }
                    }
                }, "POST", new Dictionary<string, string>
                {
                    { "Content-Type", "application/json" }
                });

            var logEvent = EventBuilder.CreateImpressionEvent(Config, Config.GetExperimentFromKey("test_experiment"), "77210100090", TestUserId, null);

            Assert.IsTrue(TestData.CompareObjects(expectedLogEvent, logEvent));

        }

        [Test]
        public void TestCreateImpressionEventWithAttributes()
        {
            var expectedLogEvent = new LogEvent("https://logx.optimizely.com/log/decision",
                new Dictionary<string, object>
                {
                    {"projectId", "7720880029" },
                    {"accountId", "1592310167" },
                    {"layerId", "7719770039" },
                    {"visitorId", "testUserId" },
                    {"clientEngine", "csharp-sdk" },
                    {"clientVersion", "1.1.1" },
                    {"timestamp", SecondsSince1970() * 1000L},
                    {"isGlobalHoldback", false },
                    {"userFeatures",
                        new object[]
                        {
                            new Dictionary<string,object>
                            {
                                {"id", "7723280020" },
                                {"name", "device_type" },
                                {"type", "custom" },
                                {"value", "iPhone" },
                                {"shouldIndex", true }
                            }
                        }
                    },
                    {
                        "decision",new Dictionary<string, object>
                        {
                            { "experimentId", "7716830082" },
                            { "variationId", "77210100090" },
                            { "isLayerHoldback", false }
                        }
                    }
                }, "POST", new Dictionary<string, string>
                {
                    { "Content-Type", "application/json" }

                });
            var userAttributes = new UserAttributes
            {
                { "device_type", "iPhone" },
                { "company", "Optimizely" }
            };

            var logEvent = EventBuilder.CreateImpressionEvent(Config, Config.GetExperimentFromKey("test_experiment"), "77210100090", TestUserId, userAttributes);

            Assert.IsTrue(TestData.CompareObjects(expectedLogEvent, logEvent));
        }

        [Test]
        public void TestCreateConversionEventNoAttributesNovalue()
        {
            var expectedEvent = new LogEvent(
                "https://logx.optimizely.com/log/event",
                new Dictionary<string, object>
                {
                    {"projectId", "7720880029"},
                    {"accountId", "1592310167"},
                    {"visitorId", TestUserId},
                    {"clientEngine", "csharp-sdk"},
                    {"clientVersion", "1.1.1"},
                    {"userFeatures", new string[0]},
                    {"isGlobalHoldback", false},
                    {"timestamp", SecondsSince1970() * 1000L} ,
                    {"eventFeatures", new string[0]},
                    {"eventMetrics",  new string[0]},
                    {"eventEntityId", "7718020063"},
                    {"eventName", "purchase"},
                    {"layerStates", new object[]{
                            new Dictionary<string, object>
                            {
                                {"layerId", "7719770039"},
                                {"actionTriggered", true},
                                {"decision",
                                    new Dictionary<string, object>{
                                        {"experimentId", "7716830082"},
                                        {"variationId", "7722370027"},
                                        {"isLayerHoldback", false}

                                    }
                                }
                            }
                        }
                    }
                },
                "POST",
                new Dictionary<string, string>
                {
                    { "Content-Type", "application/json"}
                });
            var logEvent = EventBuilder.CreateConversionEvent(Config, "purchase", new Experiment[] { Config.GetExperimentFromKey("test_experiment") }, TestUserId, null, null);

            Assert.IsTrue(TestData.CompareObjects(expectedEvent, logEvent));
        }

        [Test]
        public void TestCreateConversionEventWithAttributesNoValue()
        {
            var expectedEvent = new LogEvent(
                "https://logx.optimizely.com/log/event",
                new Dictionary<string, object>
                {
                    {"projectId", "7720880029"},
                    {"accountId", "1592310167"},
                    {"visitorId", TestUserId},
                    {"clientEngine", "csharp-sdk"},
                    {"clientVersion", "1.1.1"},

                    {"isGlobalHoldback", false},
                    {"timestamp", SecondsSince1970() * 1000L} ,
                    {"eventFeatures", new string[0]},
                    {"eventMetrics",  new string[0]},
                    {"eventEntityId", "7718020063"},
                    {"eventName", "purchase"},
                    {
                        "userFeatures", new object[]
                        {
                            new Dictionary<string, object>
                            {
                                {"id", "7723280020" },
                                {"name", "device_type" },
                                {"type", "custom" },
                                {"value", "iPhone"},
                                {"shouldIndex", true }
                            }
                        }
                    },
                    {
                        "layerStates", new object[]{
                            new Dictionary<string, object>
                            {
                                {"layerId", "7719770039"},
                                {"actionTriggered", true},
                                {"decision",
                                    new Dictionary<string, object>{
                                        {"experimentId", "7716830082"},
                                        {"variationId", "7722370027"},
                                        {"isLayerHoldback", false}

                                    }
                                }
                            }
                        }
                    }
                },
                "POST",
                new Dictionary<string, string>
                {
                    { "Content-Type", "application/json"}
                });

            var userAttributes = new UserAttributes
            {
                { "device_type", "iPhone" },
                {"company", "Optimizely" }
            };

            var logEvent = EventBuilder.CreateConversionEvent(Config, "purchase", new Experiment[] { Config.GetExperimentFromKey("test_experiment") }, TestUserId, userAttributes, null);

            Assert.IsTrue(TestData.CompareObjects(expectedEvent, logEvent));
        }

        [Test]
        public void TestCreateConversionEventNoAttributesWithValue()
        {
            var expectedEvent = new LogEvent(
                "https://logx.optimizely.com/log/event",
                new Dictionary<string, object>
                {
                    {"projectId", "7720880029"},
                    {"accountId", "1592310167"},
                    {"visitorId", TestUserId},
                    {"clientEngine", "csharp-sdk"},
                    {"clientVersion", "1.1.1"},

                    {"isGlobalHoldback", false},
                    {"timestamp", SecondsSince1970() * 1000L} ,
                    {"eventFeatures", new object[]
                        {
                            new Dictionary<string, object> {
                                {"name", "revenue" },
                                {"type", "custom" },
                                {"value", 42 },
                                {"shouldIndex", false }
                            }
                        }
                    },
                    {"eventMetrics",
                        new object[]
                        {
                            new Dictionary<string, object>
                            {
                                {"name", "revenue" },
                                {"value", 42 }
                            }
                        }
                    },
                    {"eventEntityId", "7718020063"},
                    {"eventName", "purchase"},
                    {"userFeatures", new object[0]},
                    {"layerStates",
                        new object[]{
                            new Dictionary<string, object>
                            {
                                {"layerId", "7719770039"},
                                {"actionTriggered", true},
                                {"decision",
                                    new Dictionary<string, object>{
                                        {"experimentId", "7716830082"},
                                        {"variationId", "7722370027"},
                                        {"isLayerHoldback", false}

                                    }
                                }
                            }
                        }
                    }
                },
                "POST",
                new Dictionary<string, string>
                {
                    { "Content-Type", "application/json"}
                });


            var logEvent = EventBuilder.CreateConversionEvent(Config, "purchase", new Experiment[] { Config.GetExperimentFromKey("test_experiment") }, TestUserId, null,
                new EventTags
            {
                    {"revenue", 42 }
            });

            Assert.IsTrue(TestData.CompareObjects(expectedEvent, logEvent));
        }

        [Test]
        public void TestCreateConversionEventWithAttributesWithValue()
        {
            var expectedEvent = new LogEvent(
                "https://logx.optimizely.com/log/event",
                new Dictionary<string, object>
                {
                    {"projectId", "7720880029"},
                    {"accountId", "1592310167"},
                    {"visitorId", TestUserId},
                    {"clientEngine", "csharp-sdk"},
                    {"clientVersion", "1.1.1"},
                    {"isGlobalHoldback", false},
                    {"timestamp", SecondsSince1970() * 1000L} ,
                    {"eventFeatures",
                        new object[]
                        {
                            new Dictionary<string, object> {
                                {"name", "revenue" },
                                {"type", "custom" },
                                {"value", 42 },
                                {"shouldIndex", false }
                            },
                            new Dictionary<string, object>
                            {
                                { "name", "non-revenue"},
                                {"type", "custom" },
                                {"value", "definitely" },
                                {"shouldIndex", false }
                            }
                        }
                    },
                    {"eventMetrics",
                        new object[]
                        {
                            new Dictionary<string, object>
                            {
                                {"name", "revenue" },
                                {"value", 42 }
                            }
                        }
                    },
                    {"eventEntityId", "7718020063"},
                    {"eventName", "purchase"},
                    {
                        "userFeatures", new object[]
                        {
                            new Dictionary<string, object>
                            {
                                {"id", "7723280020" },
                                {"name", "device_type" },
                                {"type", "custom" },
                                {"value", "iPhone"},
                                {"shouldIndex", true }
                            }
                        }
                    },
                    {"layerStates",
                        new object[]{
                            new Dictionary<string, object>
                            {
                                {"layerId", "7719770039"},
                                {"actionTriggered", true},
                                {"decision",
                                    new Dictionary<string, object>{
                                        {"experimentId", "7716830082"},
                                        {"variationId", "7722370027"},
                                        {"isLayerHoldback", false}

                                    }
                                }
                            }
                        }
                    }
                },
                "POST",
                new Dictionary<string, string>
                {
                    { "Content-Type", "application/json"}
                });

            var userAttributes = new UserAttributes
            {
                { "device_type", "iPhone"},
                {"company", "Optimizely" }
            };

            var logEvent = EventBuilder.CreateConversionEvent(Config, "purchase", new Experiment[] { Config.GetExperimentFromKey("test_experiment") }, TestUserId, userAttributes,
                new EventTags
                {
                    {"revenue", 42 },
                    {"non-revenue", "definitely" }
                });

            Assert.IsTrue(TestData.CompareObjects(expectedEvent, logEvent));
        }


        /* Start */
        [Test]
        public void TestCreateConversionEventNoAttributesWithInvalidValue()
        {

            var parameters = new Dictionary<string, object>
            {
                {"projectId", "7720880029"},
                {"accountId", "1592310167"},
                {"visitorId", "testUserId"},
                // {"revision", "15"}, TODO: It should be a part of project config file, have to check it.
                {"clientEngine", "csharp-sdk"},
                {"clientVersion", "1.1.1"},
                {"userFeatures" , new object[0]},
                {"isGlobalHoldback", false},
                {"timestamp", SecondsSince1970() * 1000L },
                { "eventFeatures",
                    new object[]
                    {
                        new Dictionary<string, object>
                        {
                            {"name", "revenue" },
                            {"type", "custom" },
                            {"value", "42" },
                            {"shouldIndex", false }
                        },
                        new Dictionary<string, object>
                        {
                            {"name", "non-revenue" },
                            {"type", "custom" },
                            {"value", "definitely" },
                            {"shouldIndex", false }
                        }
                    }
                },
                {"eventMetrics", new string[0] },
                {"eventEntityId", "7718020063" },
                {"eventName", "purchase" },
                {"layerStates",
                    new object[]
                    {
                        new Dictionary<string, object>
                        {
                            {"layerId", "7719770039" },
                            {"actionTriggered", true },
                            //{"revision", "15" }, // TODO: Have to check revision.
                            {"decision", new Dictionary<string, object>
                            {
                                {"experimentId", "7716830082" },
                                {"variationId", "7722370027" },
                                {"isLayerHoldback", false }
                            } }
                        }
                    }
                }
            };
            var expectedLogEvent = new LogEvent("https://logx.optimizely.com/log/event", parameters, "POST", new Dictionary<string, string>
            {
                { "Content-Type", "application/json" }
            });

            var logEvent = EventBuilder.CreateConversionEvent(Config, "purchase", new Experiment[] { Config.GetExperimentFromKey("test_experiment") }, TestUserId, null,
                new EventTags
                {
                    {"revenue", "42" },
                    {"non-revenue", "definitely" }
                });

            Assert.IsTrue(TestData.CompareObjects(expectedLogEvent, logEvent));
        }

        /* End */


    }
}
