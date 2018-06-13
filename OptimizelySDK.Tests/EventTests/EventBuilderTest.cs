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
                            {"attributes", new object[] 
                                {
                                    new Dictionary<string, object>
                                    {
                                        {"entity_id", EventBuilder.BOT_FILTERING_ATTRIBUTE},
                                        {"key", EventBuilder.BOT_FILTERING_ATTRIBUTE},
                                        {"type", "custom" },
                                        {"value", true }
                                    }
                                }
                            },
                            {"visitor_id", TestUserId}
                        }
                    }
                },
                {"project_id", "7720880029" },
                {"account_id", "1592310167" },
                {"client_name", "csharp-sdk" },
                {"client_version", Optimizely.SDK_VERSION },
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
                                    },
                                    new Dictionary<string, object>
                                    {
                                        {"entity_id", EventBuilder.BOT_FILTERING_ATTRIBUTE},
                                        {"key", EventBuilder.BOT_FILTERING_ATTRIBUTE},
                                        {"type", "custom" },
                                        {"value", true }
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
                {"client_version", Optimizely.SDK_VERSION },
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
                            {"attributes", new object[] 
                                {
                                    new Dictionary<string, object>
                                    {
                                        {"entity_id", EventBuilder.BOT_FILTERING_ATTRIBUTE},
                                        {"key", EventBuilder.BOT_FILTERING_ATTRIBUTE},
                                        {"type", "custom" },
                                        {"value", true }
                                    }
                                }
                            }
                        }
                    }
                },
                {"project_id", "7720880029"},
                {"account_id", "1592310167"},
                {"client_name", "csharp-sdk"},
                {"client_version", Optimizely.SDK_VERSION },
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
                                    },
                                    new Dictionary<string, object>
                                    {
                                        {"entity_id", EventBuilder.BOT_FILTERING_ATTRIBUTE},
                                        {"key", EventBuilder.BOT_FILTERING_ATTRIBUTE},
                                        {"type", "custom" },
                                        {"value", true }
                                    }
                                }
                            }
                        }
                    }
                },
                {"project_id", "7720880029"},
                {"account_id", "1592310167"},
                {"client_name", "csharp-sdk"},
                {"client_version", Optimizely.SDK_VERSION },
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
                { "company", "Optimizely" }
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
                            { "attributes", new object[]
                                {
                                    new Dictionary<string, object>
                                    {
                                        {"entity_id", EventBuilder.BOT_FILTERING_ATTRIBUTE},
                                        {"key", EventBuilder.BOT_FILTERING_ATTRIBUTE},
                                        {"type", "custom" },
                                        {"value", true }
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
                {"client_version", Optimizely.SDK_VERSION },
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
                                    },
                                    new Dictionary<string, object>
                                    {
                                        {"entity_id", EventBuilder.BOT_FILTERING_ATTRIBUTE},
                                        {"key", EventBuilder.BOT_FILTERING_ATTRIBUTE},
                                        {"type", "custom" },
                                        {"value", true }
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
                {"client_version", Optimizely.SDK_VERSION },
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
                                                    {"revenue", 42 },
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
                            { "attributes", new object[]
                                {
                                    new Dictionary<string, object>
                                    {
                                        {"entity_id", EventBuilder.BOT_FILTERING_ATTRIBUTE},
                                        {"key", EventBuilder.BOT_FILTERING_ATTRIBUTE},
                                        {"type", "custom" },
                                        {"value", true }
                                    }
                                }
                            }
                        }
                    }
                },
                {"project_id", "7720880029" },
                {"account_id", "1592310167" },
                {"client_name", "csharp-sdk" },
                {"client_version", Optimizely.SDK_VERSION },
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
                            { "attributes", new object[]
                                {
                                    new Dictionary<string, object>
                                    {
                                        {"entity_id", EventBuilder.BOT_FILTERING_ATTRIBUTE},
                                        {"key", EventBuilder.BOT_FILTERING_ATTRIBUTE},
                                        {"type", "custom" },
                                        {"value", true }
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
                {"client_version", Optimizely.SDK_VERSION },
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
                                        {"entity_id", DecisionService.BUCKETING_ID_ATTRIBUTE },
                                        {"key", DecisionService.BUCKETING_ID_ATTRIBUTE },
                                        {"type", "custom" },
                                        {"value", "variation"}
                                    },
                                    new Dictionary<string, object>
                                    {
                                        {"entity_id", EventBuilder.BOT_FILTERING_ATTRIBUTE},
                                        {"key", EventBuilder.BOT_FILTERING_ATTRIBUTE},
                                        {"type", "custom" },
                                        {"value", true }
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
                {"client_version", Optimizely.SDK_VERSION },
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
                {DecisionService.BUCKETING_ID_ATTRIBUTE, "variation" }
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
                                        {"entity_id", DecisionService.BUCKETING_ID_ATTRIBUTE },
                                        {"key", DecisionService.BUCKETING_ID_ATTRIBUTE },
                                        {"type", "custom" },
                                        {"value", "variation"}
                                    },
                                    new Dictionary<string, object>
                                    {
                                        {"entity_id", EventBuilder.BOT_FILTERING_ATTRIBUTE},
                                        {"key", EventBuilder.BOT_FILTERING_ATTRIBUTE},
                                        {"type", "custom" },
                                        {"value", true }
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
                {"client_version", Optimizely.SDK_VERSION },
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
                {DecisionService.BUCKETING_ID_ATTRIBUTE, "variation" }
            };
            
            var logEvent = EventBuilder.CreateImpressionEvent(Config, Config.GetExperimentFromKey("test_experiment"), "7722370027", TestUserId, userAttributes);

            TestData.ChangeGUIDAndTimeStamp(logEvent.Params, timeStamp, guid);

            Assert.IsTrue(TestData.CompareObjects(expectedLogEvent, logEvent));
        }

        [Test]
        public void TestCreateImpressionEventWhenBotFilteringIsProvidedInDatafile()
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
                                        {"entity_id", EventBuilder.USER_AGENT_ATTRIBUTE },
                                        {"key", EventBuilder.USER_AGENT_ATTRIBUTE },
                                        {"type", "custom" },
                                        {"value", "chrome"}
                                    },
                                    new Dictionary<string, object>
                                    {
                                        {"entity_id", EventBuilder.BOT_FILTERING_ATTRIBUTE},
                                        {"key", EventBuilder.BOT_FILTERING_ATTRIBUTE},
                                        {"type", "custom" },
                                        {"value", true }
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
                {"client_version", Optimizely.SDK_VERSION },
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
                {EventBuilder.USER_AGENT_ATTRIBUTE, "chrome" }
            };

            var botFilteringEnabledConfig = Config;
            botFilteringEnabledConfig.BotFiltering = true;
            var experiment = botFilteringEnabledConfig.GetExperimentFromKey("test_experiment");

            var logEvent = EventBuilder.CreateImpressionEvent(botFilteringEnabledConfig, experiment, "7722370027", TestUserId, userAttributes);
            TestData.ChangeGUIDAndTimeStamp(logEvent.Params, timeStamp, guid);

            Assert.IsTrue(TestData.CompareObjects(expectedLogEvent, logEvent));
        }

        [Test]
        public void TestCreateImpressionEventWhenBotFilteringIsNotProvidedInDatafile()
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
                                        {"entity_id", EventBuilder.USER_AGENT_ATTRIBUTE },
                                        {"key", EventBuilder.USER_AGENT_ATTRIBUTE },
                                        {"type", "custom" },
                                        {"value", "chrome"}
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
                {"client_version", Optimizely.SDK_VERSION },
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
                {EventBuilder.USER_AGENT_ATTRIBUTE, "chrome" }
            };

            var botFilteringDisabledConfig = Config;
            botFilteringDisabledConfig.BotFiltering = null;
            var experiment = botFilteringDisabledConfig.GetExperimentFromKey("test_experiment");

            var logEvent = EventBuilder.CreateImpressionEvent(botFilteringDisabledConfig, experiment, "7722370027", TestUserId, userAttributes);
            TestData.ChangeGUIDAndTimeStamp(logEvent.Params, timeStamp, guid);

            Assert.IsTrue(TestData.CompareObjects(expectedLogEvent, logEvent));
        }

        [Test]
        public void TestCreateConversionEventWhenBotFilteringIsProvidedInDatafile()
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
                                        {"entity_id", EventBuilder.USER_AGENT_ATTRIBUTE },
                                        {"key", EventBuilder.USER_AGENT_ATTRIBUTE },
                                        {"type", "custom" },
                                        {"value", "safari"}
                                    },
                                    new Dictionary<string, object>
                                    {
                                        {"entity_id", EventBuilder.BOT_FILTERING_ATTRIBUTE},
                                        {"key", EventBuilder.BOT_FILTERING_ATTRIBUTE},
                                        {"type", "custom" },
                                        {"value", true }
                                    }
                                }
                            }
                        }
                    }
                },
                {"project_id", "7720880029"},
                {"account_id", "1592310167"},
                {"client_name", "csharp-sdk"},
                {"client_version", Optimizely.SDK_VERSION },
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
                {EventBuilder.USER_AGENT_ATTRIBUTE, "safari" }
            };
            var experimentToVariationMap = new Dictionary<string, Variation>
            {
                {"7716830082", new Variation{Id="7722370027", Key="control"} }
            };

            var botFilteringEnabledConfig = Config;
            botFilteringEnabledConfig.BotFiltering = true;
            var logEvent = EventBuilder.CreateConversionEvent(botFilteringEnabledConfig, "purchase", experimentToVariationMap, TestUserId, userAttributes, null);

            TestData.ChangeGUIDAndTimeStamp(logEvent.Params, timeStamp, guid);

            Assert.IsTrue(TestData.CompareObjects(expectedEvent, logEvent));
        }

        [Test]
        public void TestCreateConversionEventWhenBotFilteringIsNotProvidedInDatafile()
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
                                        {"entity_id", EventBuilder.USER_AGENT_ATTRIBUTE },
                                        {"key", EventBuilder.USER_AGENT_ATTRIBUTE },
                                        {"type", "custom" },
                                        {"value", "safari"}
                                    }
                                }
                            }
                        }
                    }
                },
                {"project_id", "7720880029"},
                {"account_id", "1592310167"},
                {"client_name", "csharp-sdk"},
                {"client_version", Optimizely.SDK_VERSION },
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
                {EventBuilder.USER_AGENT_ATTRIBUTE, "safari" }
            };
            var experimentToVariationMap = new Dictionary<string, Variation>
            {
                {"7716830082", new Variation{Id="7722370027", Key="control"} }
            };

            var botFilteringDisabledConfig = Config;
            botFilteringDisabledConfig.BotFiltering = null;
            var logEvent = EventBuilder.CreateConversionEvent(botFilteringDisabledConfig, "purchase", experimentToVariationMap, TestUserId, userAttributes, null);

            TestData.ChangeGUIDAndTimeStamp(logEvent.Params, timeStamp, guid);

            Assert.IsTrue(TestData.CompareObjects(expectedEvent, logEvent));
        }
    }
}
