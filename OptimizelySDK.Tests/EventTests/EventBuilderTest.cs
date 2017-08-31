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

        private void ChangeGUIDAndTimeStamp(Dictionary<string, object> paramsObj, long timeStamp, Guid guid)
        {
            // Path from where to find
            // visitors.[0].snapshots.[0].events.[0].uuid or timestamp

            var visitor = (paramsObj["visitors"] as object[])[0] as Dictionary<string, object>;

            var snapshot = (visitor["snapshots"] as object[])[0] as Dictionary<string, object>;

            var @event = (snapshot["events"] as object[])[0] as Dictionary<string, object>;

            @event["uuid"] = guid;
            @event["timestamp"] = timeStamp;
        }

        [Test]
        public void TestCreateImpressionEventNoAttributes()
        {
            var guid = Guid.NewGuid();
            var timeStamp = SecondsSince1970();

            var payloadParams = new Dictionary<string, object>
            {
                { "visitors", new object[]
                    {
                        new Dictionary<string, object>()
                        {
                            { "snapshots", new object[]
                                {
                                    new Dictionary<string, object>
                                    {
                                        { "decisions", new object[]
                                            {
                                                new Dictionary<string, object>
                                                {
                                                    {"campaign_id", "7719770039" },
                                                    {"experiment_id", "7716830082" },
                                                    {"variation_id", "77210100090" }
                                                }
                                            }
                                        },
                                        { "events", new object[]
                                            {
                                                new Dictionary<string, object>
                                                {
                                                    {"entity_id", "7719770039" },
                                                    {"timestamp", timeStamp },
                                                    {"uuid", guid },
                                                    {"key", "campaign_activated" }
                                                }
                                            }
                                        }
                                    }
                                }
                            },
                            {"attributes", new object[] { }},
                            {"visitor_id", TestUserId}
                        }
                    }
                },
                {"project_id", "7720880029" },
                {"account_id", "1592310167" },
                {"client_name", "csharp-sdk" },
                {"client_version", "1.1.0" },
                {"revision", 15 }
            };

            var expectedLogEvent = new LogEvent("https://logx.optimizely.com/v1/events",
                payloadParams, 
                "POST",
                new Dictionary<string, string>
                {
                    { "Content-Type", "application/json" }
                });

            var logEvent = EventBuilder.CreateImpressionEvent(Config, Config.GetExperimentFromKey("test_experiment"), "77210100090", TestUserId, null);

            ChangeGUIDAndTimeStamp(logEvent.Params, timeStamp, guid);

            Assert.IsTrue(TestData.CompareObjects(expectedLogEvent, logEvent));

        }

        [Test]
        public void TestCreateImpressionEventWithAttributes()
        {
            var guid = Guid.NewGuid();
            var timeStamp = SecondsSince1970();

            var payloadParams = new Dictionary<string, object>
            {
                { "visitors", new object[]
                    {
                        new Dictionary<string, object>()
                        {
                            { "snapshots", new object[]
                                {
                                    new Dictionary<string, object>
                                    {
                                        { "decisions", new object[]
                                            {
                                                new Dictionary<string, object>
                                                {
                                                    {"campaign_id", "7719770039" },
                                                    {"experiment_id", "7716830082" },
                                                    {"variation_id", "77210100090" }
                                                }
                                            }
                                        },
                                        { "events", new object[]
                                            {
                                                new Dictionary<string, object>
                                                {
                                                    {"entity_id", "7719770039" },
                                                    {"timestamp", timeStamp },
                                                    {"uuid", guid },
                                                    {"key", "campaign_activated" }
                                                }
                                            }
                                        }
                                    }
                                }
                            },
                            {"attributes", new object[]
                                {
                                    new Dictionary<string, object>
                                    {
                                        {"entity_id", "7723280020" },
                                        {"key", "device_type" },
                                        {"type", "custom" },
                                        {"value", "iPhone"}
                                    }
                                }
                            },
                            { "visitor_id", TestUserId }
                        }
                    }
                },
                {"project_id", "7720880029" },
                {"account_id", "1592310167" },
                {"client_name", "csharp-sdk" },
                {"client_version", "1.1.0" },
                {"revision", 15 }
            };
            
            var expectedLogEvent = new LogEvent("https://logx.optimizely.com/v1/events",
                payloadParams,
                "POST", 
                new Dictionary<string, string>
                {
                    { "Content-Type", "application/json" }

                });

            var userAttributes = new UserAttributes
            {
                { "device_type", "iPhone" },
                { "company", "Optimizely" }
            };

            var logEvent = EventBuilder.CreateImpressionEvent(Config, Config.GetExperimentFromKey("test_experiment"), "77210100090", TestUserId, userAttributes);

            ChangeGUIDAndTimeStamp(logEvent.Params, timeStamp, guid);

            Assert.IsTrue(TestData.CompareObjects(expectedLogEvent, logEvent));
        }

        [Test]
        public void TestCreateConversionEventNoAttributesNoValue()
        {
            var guid = Guid.NewGuid();
            var timeStamp = SecondsSince1970();

            var payloadParams = new Dictionary<string, object>
            {
                {"visitors", new object[]
                    {
                        new Dictionary<string, object>
                        {
                            {"snapshots", new object[]
                                {
                                    new Dictionary<string, object>
                                    {
                                        { "decisions", new object[]
                                            {
                                                new Dictionary<string, object>
                                                {
                                                    {"campaign_id", "7719770039"},
                                                    {"experiment_id", "7716830082"},
                                                    {"variation_id", "7722370027"}
                                                }
                                            }
                                        },
                                        {"events", new object[]
                                            {
                                                new Dictionary<string, object>
                                                {
                                                    {"entity_id", "7718020063"},
                                                    {"timestamp", timeStamp},
                                                    {"uuid", guid},
                                                    {"key", "purchase"},
                                                }
                                            }
                                        }
                                    }
                                }
                            },
                            {"visitor_id", TestUserId },
                            {"attributes", new object[] {}}
                        }
                    }
                },
                {"project_id", "7720880029"},
                {"account_id", "1592310167"},
                {"client_name", "csharp-sdk"},
                {"client_version", "1.1.0"},
                {"revision", 15}
            };
            
            var expectedEvent = new LogEvent(
                "https://logx.optimizely.com/v1/events",
                payloadParams,
                "POST",
                new Dictionary<string, string>
                {
                    { "Content-Type", "application/json"}
                });

            var logEvent = EventBuilder.CreateConversionEvent(Config, "purchase", new Experiment[] { Config.GetExperimentFromKey("test_experiment") }, TestUserId, null, null);

            ChangeGUIDAndTimeStamp(logEvent.Params, timeStamp, guid);

            Assert.IsTrue(TestData.CompareObjects(expectedEvent, logEvent));
        }

        [Test]
        public void TestCreateConversionEventWithAttributesNoValue()
        {
            var guid = Guid.NewGuid();
            var timeStamp = SecondsSince1970();
            
            var payloadParams = new Dictionary<string, object>
            {
                {"visitors", new object[]
                    {
                        new Dictionary<string, object>
                        {
                            {"snapshots", new object[]
                                {
                                    new Dictionary<string, object>
                                    {
                                        { "decisions", new object[]
                                            {
                                                new Dictionary<string, object>
                                                {
                                                    {"campaign_id", "7719770039"},
                                                    {"experiment_id", "7716830082"},
                                                    {"variation_id", "7722370027"}
                                                }
                                            }
                                        },
                                        {"events", new object[]
                                            {
                                                new Dictionary<string, object>
                                                {
                                                    {"entity_id", "7718020063"},
                                                    {"timestamp", timeStamp},
                                                    {"uuid", guid},
                                                    {"key", "purchase"},
                                                }
                                            }
                                        }
                                    }
                                }
                            },
                            {"visitor_id", TestUserId },
                            {"attributes", new object[]
                                {
                                    new Dictionary<string, object>
                                    {
                                        {"entity_id", "7723280020" },
                                        {"key", "device_type" },
                                        {"type", "custom" },
                                        {"value", "iPhone"}
                                    }
                                }
                            }
                        }
                    }
                },
                {"project_id", "7720880029"},
                {"account_id", "1592310167"},
                {"client_name", "csharp-sdk"},
                {"client_version", "1.1.0"},
                {"revision", 15}
            };

            var expectedEvent = new LogEvent(
                "https://logx.optimizely.com/v1/events",
                payloadParams,
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

            ChangeGUIDAndTimeStamp(logEvent.Params, timeStamp, guid);

            Assert.IsTrue(TestData.CompareObjects(expectedEvent, logEvent));
        }

        [Test]
        public void TestCreateConversionEventNoAttributesWithValue()
        {
            var guid = Guid.NewGuid();
            var timeStamp = SecondsSince1970();

            var payloadParams = new Dictionary<string, object>
            {
                { "visitors", new object[]
                    {
                        new Dictionary<string, object>()
                        {
                            { "snapshots", new object[]
                                {
                                    new Dictionary<string, object>
                                    {
                                        { "decisions", new object[]
                                            {
                                                new Dictionary<string, object>
                                                {
                                                    {"campaign_id", "7719770039" },
                                                    {"experiment_id", "7716830082" },
                                                    {"variation_id", "7722370027" }
                                                }
                                            }
                                        },
                                        { "events", new object[]
                                            {
                                                new Dictionary<string, object>
                                                {
                                                    {"entity_id", "7718020063" },
                                                    {"timestamp", timeStamp },
                                                    {"uuid", guid },
                                                    {"key", "purchase" },
                                                    {"revenue", 42 },
                                                    {"tags",
                                                        new Dictionary<string, object>
                                                        {
                                                            {"revenue", 42 }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            },
                            { "attributes", new object[]{ } },
                            { "visitor_id", TestUserId }
                        }
                    }
                },
                {"project_id", "7720880029" },
                {"account_id", "1592310167" },
                {"client_name", "csharp-sdk" },
                {"client_version", "1.1.0" },
                {"revision", 15 }
            };

            var expectedEvent = new LogEvent(
                "https://logx.optimizely.com/v1/events",
                payloadParams,
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

            ChangeGUIDAndTimeStamp(logEvent.Params, timeStamp, guid);

            Assert.IsTrue(TestData.CompareObjects(expectedEvent, logEvent));
        }

        [Test]
        public void TestCreateConversionEventWithAttributesWithValue()
        {
            var guid = Guid.NewGuid();
            var timeStamp = SecondsSince1970();

            var payloadParams = new Dictionary<string, object>
            {
                { "visitors", new object[]
                    {
                        new Dictionary<string, object>()
                        {
                            { "snapshots", new object[]
                                {
                                    new Dictionary<string, object>
                                    {
                                        { "decisions", new object[]
                                            {
                                                new Dictionary<string, object>
                                                {
                                                    {"campaign_id", "7719770039" },
                                                    {"experiment_id", "7716830082" },
                                                    {"variation_id", "7722370027" }
                                                }
                                            }
                                        },
                                        { "events", new object[]
                                            {
                                                new Dictionary<string, object>
                                                {
                                                    {"entity_id", "7718020063" },
                                                    {"timestamp", timeStamp },
                                                    {"uuid", guid },
                                                    {"key", "purchase" },
                                                    {"revenue", 42 },
                                                    {"tags",
                                                        new Dictionary<string, object>
                                                        {
                                                            {"revenue", 42},
                                                            {"non-revenue", "definitely"}
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            },
                            {"attributes", new object[]
                                {
                                    new Dictionary<string, object>
                                    {
                                        {"entity_id", "7723280020" },
                                        {"key", "device_type" },
                                        {"type", "custom" },
                                        {"value", "iPhone"}
                                    }
                                }
                            },
                            { "visitor_id", TestUserId }
                        }
                    }
                },
                {"project_id", "7720880029" },
                {"account_id", "1592310167" },
                {"client_name", "csharp-sdk" },
                {"client_version", "1.1.0" },
                {"revision", 15 }
            };

            var expectedEvent = new LogEvent(
                "https://logx.optimizely.com/v1/events",
                payloadParams,
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

            ChangeGUIDAndTimeStamp(logEvent.Params, timeStamp, guid);

            Assert.IsTrue(TestData.CompareObjects(expectedEvent, logEvent));
        }


        /* Start */
        [Test]
        public void TestCreateConversionEventNoAttributesWithInvalidValue()
        {
            var guid = Guid.NewGuid();
            var timeStamp = SecondsSince1970();

            var payloadParams = new Dictionary<string, object>
            {
                { "visitors", new object[]
                    {
                        new Dictionary<string, object>()
                        {
                            { "snapshots", new object[]
                                {
                                    new Dictionary<string, object>
                                    {
                                        { "decisions", new object[]
                                            {
                                                new Dictionary<string, object>
                                                {
                                                    {"campaign_id", "7719770039" },
                                                    {"experiment_id", "7716830082" },
                                                    {"variation_id", "7722370027" }
                                                }
                                            }
                                        },
                                        { "events", new object[]
                                            {
                                                new Dictionary<string, object>
                                                {
                                                    {"entity_id", "7718020063" },
                                                    {"timestamp", timeStamp },
                                                    {"uuid", guid },
                                                    {"key", "purchase" },
                                                    {"tags",
                                                        new Dictionary<string, object>
                                                        {
                                                            {"revenue", "42" },
                                                            {"non-revenue", "definitely"}
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            },
                            { "visitor_id", TestUserId },
                            { "attributes", new object[]{ } }
                        }
                    }
                },
                {"project_id", "7720880029" },
                {"account_id", "1592310167" },
                {"client_name", "csharp-sdk" },
                {"client_version", "1.1.0" },
                {"revision", 15 }
            };
            
            var expectedEvent = new LogEvent(
                "https://logx.optimizely.com/v1/events",
                payloadParams,
                "POST",
                new Dictionary<string, string>
                {
                    { "Content-Type", "application/json"}
                });

            var logEvent = EventBuilder.CreateConversionEvent(Config, "purchase", new Experiment[] { Config.GetExperimentFromKey("test_experiment") }, TestUserId, null,
                new EventTags
                {
                    {"revenue", "42" },
                    {"non-revenue", "definitely" }
                });

            ChangeGUIDAndTimeStamp(logEvent.Params, timeStamp, guid);

            Assert.IsTrue(TestData.CompareObjects(expectedEvent, logEvent));
        }

        [Test]
        public void TestConversionEventWithNumericTag()
        {
            var guid = Guid.NewGuid();
            var timeStamp = SecondsSince1970();

            var payloadParams = new Dictionary<string, object>
            {
                { "visitors", new object[]
                    {
                        new Dictionary<string, object>()
                        {
                            { "snapshots", new object[]
                                {
                                    new Dictionary<string, object>
                                    {
                                        { "decisions", new object[]
                                            {
                                                new Dictionary<string, object>
                                                {
                                                    {"campaign_id", "7719770039" },
                                                    {"experiment_id", "7716830082" },
                                                    {"variation_id", "7722370027" }
                                                }
                                            }
                                        },
                                        { "events", new object[]
                                            {
                                                new Dictionary<string, object>
                                                {
                                                    {"entity_id", "7718020063" },
                                                    {"timestamp", timeStamp },
                                                    {"uuid", guid },
                                                    {"key", "purchase" },
                                                    {"revenue", 42 },
                                                    {"value", 400 },
                                                    {"tags",
                                                        new Dictionary<string, object>
                                                        {
                                                            {"revenue", 42 },
                                                            {"value", 400 }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            },
                            { "attributes", new object[]{ } },
                            { "visitor_id", TestUserId }
                        }
                    }
                },
                {"project_id", "7720880029" },
                {"account_id", "1592310167" },
                {"client_name", "csharp-sdk" },
                {"client_version", "1.1.0" },
                {"revision", 15 }
            };

            var expectedEvent = new LogEvent(
                "https://logx.optimizely.com/v1/events",
                payloadParams,
                "POST",
                new Dictionary<string, string>
                {
                    { "Content-Type", "application/json"}
                });


            var logEvent = EventBuilder.CreateConversionEvent(Config, "purchase", new Experiment[] { Config.GetExperimentFromKey("test_experiment") }, TestUserId, null,
                new EventTags
            {
                    {"revenue", 42 },
                    {"value", 400 }
            });

            ChangeGUIDAndTimeStamp(logEvent.Params, timeStamp, guid);

            Assert.IsTrue(TestData.CompareObjects(expectedEvent, logEvent));
        }

        /* End */


    }
}
