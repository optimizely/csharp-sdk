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

        [Test]
        public void TestCreateImpressionEventNoAttributes()
        {
            var guid = Guid.NewGuid();
            var timeStamp = TestData.SecondsSince1970();

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
                {"client_version", "1.2.0" },
                {"revision", 15 },
                {"anonymize_ip", false}
            };

            var expectedLogEvent = new LogEvent("https://logx.optimizely.com/v1/events",
                payloadParams, 
                "POST",
                new Dictionary<string, string>
                {
                    { "Content-Type", "application/json" }
                });

            var logEvent = EventBuilder.CreateImpressionEvent(Config, Config.GetExperimentFromKey("test_experiment"), "77210100090", TestUserId, null);

            TestData.ChangeGUIDAndTimeStamp(logEvent.Params, timeStamp, guid);

            Assert.IsTrue(TestData.CompareObjects(expectedLogEvent, logEvent));

        }

        [Test]
        public void TestCreateImpressionEventWithAttributes()
        {
            var guid = Guid.NewGuid();
            var timeStamp = TestData.SecondsSince1970();

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
                {"client_version", "1.2.0" },
                {"revision", 15 },
                {"anonymize_ip", false}
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

            TestData.ChangeGUIDAndTimeStamp(logEvent.Params, timeStamp, guid);

            Assert.IsTrue(TestData.CompareObjects(expectedLogEvent, logEvent));
        }

        [Test]
        public void TestCreateConversionEventNoAttributesNoValue()
        {
            var guid = Guid.NewGuid();
            var timeStamp = TestData.SecondsSince1970();

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
                {"client_version", "1.2.0" },
                {"revision", 15 },
                {"anonymize_ip", false}
            };
            
            var expectedEvent = new LogEvent(
                "https://logx.optimizely.com/v1/events",
                payloadParams,
                "POST",
                new Dictionary<string, string>
                {
                    { "Content-Type", "application/json"}
                });
            var experimentToVariationMap = new Dictionary<string, Variation>
            {
                {"7716830082", new Variation{Id="7722370027", Key="control"} }
            };

            var logEvent = EventBuilder.CreateConversionEvent(Config, "purchase", experimentToVariationMap, TestUserId, null, null);

            TestData.ChangeGUIDAndTimeStamp(logEvent.Params, timeStamp, guid);

            Assert.IsTrue(TestData.CompareObjects(expectedEvent, logEvent));
        }

        [Test]
        public void TestCreateConversionEventWithAttributesNoValue()
        {
            var guid = Guid.NewGuid();
            var timeStamp = TestData.SecondsSince1970();
            
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
                {"client_version", "1.2.0" },
                {"revision", 15 },
                {"anonymize_ip", false}
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
            var experimentToVariationMap = new Dictionary<string, Variation>
            {
                {"7716830082", new Variation{Id="7722370027", Key="control"} }
            };
            var logEvent = EventBuilder.CreateConversionEvent(Config, "purchase", experimentToVariationMap, TestUserId, userAttributes, null);

            TestData.ChangeGUIDAndTimeStamp(logEvent.Params, timeStamp, guid);

            Assert.IsTrue(TestData.CompareObjects(expectedEvent, logEvent));
        }

        [Test]
        public void TestCreateConversionEventNoAttributesWithValue()
        {
            var guid = Guid.NewGuid();
            var timeStamp = TestData.SecondsSince1970();

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
                {"client_version", "1.2.0" },
                {"revision", 15 },
                {"anonymize_ip", false}
            };

            var expectedEvent = new LogEvent(
                "https://logx.optimizely.com/v1/events",
                payloadParams,
                "POST",
                new Dictionary<string, string>
                {
                    { "Content-Type", "application/json"}
                });

            var experimentToVariationMap = new Dictionary<string, Variation>
            {
                {"7716830082", new Variation{Id="7722370027", Key="control"} }
            };


            var logEvent = EventBuilder.CreateConversionEvent(Config, "purchase", experimentToVariationMap, TestUserId, null,
                new EventTags
            {
                    {"revenue", 42 }
            });

            TestData.ChangeGUIDAndTimeStamp(logEvent.Params, timeStamp, guid);

            Assert.IsTrue(TestData.CompareObjects(expectedEvent, logEvent));
        }

        [Test]
        public void TestCreateConversionEventWithAttributesWithValue()
        {
            var guid = Guid.NewGuid();
            var timeStamp = TestData.SecondsSince1970();

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
                {"client_version", "1.2.0" },
                {"revision", 15 },
                {"anonymize_ip", false}
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

            var experimentToVariationMap = new Dictionary<string, Variation>
            {
                {"7716830082", new Variation{Id="7722370027", Key="control"} }
            };

            var logEvent = EventBuilder.CreateConversionEvent(Config, "purchase", experimentToVariationMap, TestUserId, userAttributes,
                new EventTags
                {
                    {"revenue", 42 },
                    {"non-revenue", "definitely" }
                });

            TestData.ChangeGUIDAndTimeStamp(logEvent.Params, timeStamp, guid);

            Assert.IsTrue(TestData.CompareObjects(expectedEvent, logEvent));
        }
        
        [Test]
        public void TestCreateConversionEventNoAttributesWithInvalidValue()
        {
            var guid = Guid.NewGuid();
            var timeStamp = TestData.SecondsSince1970();

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
                {"client_version", "1.2.0" },
                {"revision", 15 },
                {"anonymize_ip", false}
            };
            
            var expectedEvent = new LogEvent(
                "https://logx.optimizely.com/v1/events",
                payloadParams,
                "POST",
                new Dictionary<string, string>
                {
                    { "Content-Type", "application/json"}
                });

            var experimentToVariationMap = new Dictionary<string, Variation>
            {
                {"7716830082", new Variation{Id="7722370027", Key="control"} }
            };

            var logEvent = EventBuilder.CreateConversionEvent(Config, "purchase", experimentToVariationMap, TestUserId, null,
                new EventTags
                {
                    {"revenue", "42" },
                    {"non-revenue", "definitely" }
                });

            TestData.ChangeGUIDAndTimeStamp(logEvent.Params, timeStamp, guid);

            Assert.IsTrue(TestData.CompareObjects(expectedEvent, logEvent));
        }

        [Test]
        public void TestConversionEventWithNumericTag()
        {
            var guid = Guid.NewGuid();
            var timeStamp = TestData.SecondsSince1970();

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
                                                    {"value", 400.0 },
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
                {"client_version", "1.2.0" },
                {"revision", 15 },
                {"anonymize_ip", false}
            };

            var expectedEvent = new LogEvent(
                "https://logx.optimizely.com/v1/events",
                payloadParams,
                "POST",
                new Dictionary<string, string>
                {
                    { "Content-Type", "application/json"}
                });

            var experimentToVariationMap = new Dictionary<string, Variation>
            {
                {"7716830082", new Variation{Id="7722370027", Key="control"} }
            };

            var logEvent = EventBuilder.CreateConversionEvent(Config, "purchase", experimentToVariationMap, TestUserId, null,
                new EventTags
            {
                    {"revenue", 42 },
                    {"value", 400 }
            });

            TestData.ChangeGUIDAndTimeStamp(logEvent.Params, timeStamp, guid);

            Assert.IsTrue(TestData.CompareObjects(expectedEvent, logEvent));
        }

        [Test]
        public void TestCreateConversionEventWithBucketingIDAttribute()
        {
            var guid = Guid.NewGuid();
            var timeStamp = TestData.SecondsSince1970();

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
                                    },
                                    new Dictionary<string, object>
                                    {
                                        {"entity_id", "$opt_bucketing_id" },
                                        {"key", "optimizely_bucketing_id" },
                                        {"type", "custom" },
                                        {"value", "variation"}
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
                {"client_version", "1.2.0" },
                {"revision", 15 },
                {"anonymize_ip", false}
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
                {"company", "Optimizely" },
                {DecisionService.RESERVED_ATTRIBUTE_KEY_BUCKETING_ID, "variation" }
            };

            var experimentToVariationMap = new Dictionary<string, Variation>
            {
                {"7716830082", new Variation{Id = "7722370027", Key = "control"} }
            };

            var logEvent = EventBuilder.CreateConversionEvent(Config, "purchase", experimentToVariationMap, TestUserId, userAttributes, null);

            TestData.ChangeGUIDAndTimeStamp(logEvent.Params, timeStamp, guid);

            Assert.IsTrue(TestData.CompareObjects(expectedEvent, logEvent));
        }

        [Test]
        public void TestCreateImpressionEventWithBucketingIDAttribute()
        {
            var guid = Guid.NewGuid();
            var timeStamp = TestData.SecondsSince1970();

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
                                    },
                                    new Dictionary<string, object>
                                    {
                                        {"entity_id", "$opt_bucketing_id" },
                                        {"key", "optimizely_bucketing_id" },
                                        {"type", "custom" },
                                        {"value", "variation"}
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
                {"client_version", "1.2.0" },
                {"revision", 15 },
                {"anonymize_ip", false}
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
                { "company", "Optimizely" },
                {DecisionService.RESERVED_ATTRIBUTE_KEY_BUCKETING_ID, "variation" }
            };
            
            var logEvent = EventBuilder.CreateImpressionEvent(Config, Config.GetExperimentFromKey("test_experiment"), "7722370027", TestUserId, userAttributes);

            TestData.ChangeGUIDAndTimeStamp(logEvent.Params, timeStamp, guid);

            Assert.IsTrue(TestData.CompareObjects(expectedLogEvent, logEvent));
        }
    }
}
