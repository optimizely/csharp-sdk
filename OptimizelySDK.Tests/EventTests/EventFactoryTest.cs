/**
 *
 *    Copyright 2019-2020, Optimizely and contributors
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

using System;
using System.Collections.Generic;
using Moq;
using NUnit.Framework;
using OptimizelySDK.Config;
using OptimizelySDK.Entity;
using OptimizelySDK.ErrorHandler;
using OptimizelySDK.Event;
using OptimizelySDK.Logger;
using OptimizelySDK.Utils;

namespace OptimizelySDK.Tests.EventTests
{
    [TestFixture]
    public class EventFactoryTest
    {

        private string TestUserId = string.Empty;
        private ProjectConfig Config;
        private ILogger Logger;

        [TestFixtureSetUp]
        public void Setup()
        {
            TestUserId = "testUserId";
            var logger = new NoOpLogger();
            Config = DatafileProjectConfig.Create(TestData.Datafile, logger, new ErrorHandler.NoOpErrorHandler());
        }

        [Test]
        public void TestCreateImpressionEventReturnsNullWhenSendFlagDecisionsIsFalseAndIsRollout()
        {
            Config.SendFlagDecisions = false;
            var impressionEvent = UserEventFactory.CreateImpressionEvent(
                Config, Config.GetExperimentFromKey("test_experiment"), "7722370027", TestUserId, null, "test_feature", "rollout");
            Assert.IsNull(impressionEvent);
        }

        [Test]
        public void TestCreateImpressionEventReturnsNullWhenSendFlagDecisionsIsFalseAndVariationIsNull()
        {
            Config.SendFlagDecisions = false;
            Variation variation = null;
            var impressionEvent = UserEventFactory.CreateImpressionEvent(
                Config, Config.GetExperimentFromKey("test_experiment"), variation, TestUserId, null, "test_experiment", "experiment");
            Assert.IsNull(impressionEvent);
        }

        [Test]
        public void TestCreateImpressionEventNoAttributes()
        {
            var guid = Guid.NewGuid();
            var timeStamp = TestData.SecondsSince1970();

            var payloadParams = new Dictionary<string, object> {
                {
                        "visitors", new object[] {
                            new Dictionary<string, object>() {
                                {
                                    "snapshots", new object[] {
                                        new Dictionary<string, object> {
                                            {
                                                "decisions", new object[] {
                                                    new Dictionary<string, object> {
                                                        { "campaign_id", "7719770039" },
                                                        { "experiment_id", "7716830082" },
                                                        { "variation_id", "7722370027" },
                                                        { "metadata",
                                                            new Dictionary<string, object> {
                                                            { "rule_type", "experiment" },
                                                            { "rule_key", "test_experiment" },
                                                            { "flag_key", "test_experiment" },
                                                            { "variation_key", "control" },
                                                            { "enabled", false }
                                                        } }
                                                    }
                                                }
                                            },
                                            {
                                                "events", new object[] {
                                                    new Dictionary<string, object> {
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
                                {
                                    "attributes", new object[] {
                                        new Dictionary<string, object> {
                                            {"entity_id", ControlAttributes.BOT_FILTERING_ATTRIBUTE},
                                            {"key", ControlAttributes.BOT_FILTERING_ATTRIBUTE},
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
                    {"enrich_decisions", true} ,
                    {"client_name", "csharp-sdk" },
                    {"client_version", Optimizely.SDK_VERSION },
                    {"revision", "15" },
                    {"anonymize_ip", false}
                };

            var expectedLogEvent = new LogEvent("https://logx.optimizely.com/v1/events",
                payloadParams,
                "POST",
                new Dictionary<string, string>
                {
                    { "Content-Type", "application/json" }
                });
            var impressionEvent = UserEventFactory.CreateImpressionEvent(
                Config, Config.GetExperimentFromKey("test_experiment"), "7722370027", TestUserId, null, "test_experiment", "experiment");

            var logEvent = EventFactory.CreateLogEvent(impressionEvent, Logger);

            TestData.ChangeGUIDAndTimeStamp(expectedLogEvent.Params, impressionEvent.Timestamp, Guid.Parse(impressionEvent.UUID));

            Assert.IsTrue(TestData.CompareObjects(expectedLogEvent, logEvent));
        }

        [Test]
        public void TestCreateImpressionEventWithAttributes()
        {
            var guid = Guid.NewGuid();
            var timeStamp = TestData.SecondsSince1970();
            var variationId = "7722370027";
            var payloadParams = new Dictionary<string, object> {
                {
                    "visitors", new object[] {
                        new Dictionary<string, object>() {
                            {
                                "snapshots", new object[] {
                                    new Dictionary<string, object> {
                                        {
                                            "decisions", new object[] {
                                                new Dictionary<string, object> {
                                                    {"campaign_id", "7719770039" },
                                                    {"experiment_id", "7716830082" },
                                                    {"variation_id", "7722370027" },
                                                    { "metadata", new Dictionary<string, object> {
                                                            { "rule_type", "experiment" },
                                                            { "rule_key", "test_experiment" },
                                                            { "flag_key", "test_experiment" },
                                                            { "variation_key", "control" },
                                                            {"enabled", false }

                                                        }
                                                    }
                                                }
                                            }
                                        },
                                        {
                                            "events", new object[] {
                                                new Dictionary<string, object> {
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
                            {
                                "attributes", new object[] {
                                    new Dictionary<string, object>
                                    {
                                        {"entity_id", "7723280020" },
                                        {"key", "device_type" },
                                        {"type", "custom" },
                                        {"value", "iPhone"}
                                    },
                                    new Dictionary<string, object>
                                    {
                                        {"entity_id", ControlAttributes.BOT_FILTERING_ATTRIBUTE},
                                        {"key", ControlAttributes.BOT_FILTERING_ATTRIBUTE},
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
                {"enrich_decisions", true} ,
                {"client_name", "csharp-sdk" },
                {"client_version", Optimizely.SDK_VERSION },
                {"revision", "15" },
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
            //
            var impressionEvent = UserEventFactory.CreateImpressionEvent(Config, Config.GetExperimentFromKey("test_experiment"), variationId, TestUserId, userAttributes, "test_experiment", "experiment");
            var logEvent = EventFactory.CreateLogEvent(impressionEvent, Logger);

            TestData.ChangeGUIDAndTimeStamp(expectedLogEvent.Params, impressionEvent.Timestamp, Guid.Parse(impressionEvent.UUID));

            Assert.IsTrue(TestData.CompareObjects(expectedLogEvent, logEvent));
        }

        [Test]
        public void TestCreateImpressionEventWithTypedAttributes()
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
                                                    {"variation_id", "7722370027" },
                                                    { "metadata", new Dictionary<string, object> {
                                                            { "rule_type", "experiment" },
                                                            { "rule_key", "test_experiment" },
                                                            { "flag_key", "test_experiment" },
                                                            { "variation_key", "control" },
                                                            {"enabled", false }
                                                        }
                                                    }
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
                                        {"entity_id", "323434545" },
                                        {"key", "boolean_key" },
                                        {"type", "custom" },
                                        {"value", true}
                                    },
                                    new Dictionary<string, object>
                                    {
                                        {"entity_id", "616727838" },
                                        {"key", "integer_key" },
                                        {"type", "custom" },
                                        {"value", 15}
                                    },
                                    new Dictionary<string, object>
                                    {
                                        {"entity_id", "808797686" },
                                        {"key", "double_key" },
                                        {"type", "custom" },
                                        {"value", 3.14}
                                    },
                                    new Dictionary<string, object>
                                    {
                                        {"entity_id", ControlAttributes.BOT_FILTERING_ATTRIBUTE},
                                        {"key", ControlAttributes.BOT_FILTERING_ATTRIBUTE},
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
                {"enrich_decisions", true} ,
                {"client_name", "csharp-sdk" },
                {"client_version", Optimizely.SDK_VERSION },
                {"revision", "15" },
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
                {"device_type", "iPhone" },
                {"boolean_key", true },
                {"integer_key", 15 },
                {"double_key", 3.14 }
            };
            var impressionEvent = UserEventFactory.CreateImpressionEvent(Config, Config.GetExperimentFromKey("test_experiment"), "7722370027", TestUserId, userAttributes, "test_experiment", "experiment");
            var logEvent = EventFactory.CreateLogEvent(impressionEvent, Logger);

            TestData.ChangeGUIDAndTimeStamp(expectedLogEvent.Params, impressionEvent.Timestamp, Guid.Parse(impressionEvent.UUID));

            Assert.IsTrue(TestData.CompareObjects(expectedLogEvent, logEvent));
        }

        [Test]
        public void TestCreateImpressionEventRemovesInvalidAttributesFromPayload()
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
                                                    {"variation_id", "7722370027" },
                                                    { "metadata", new Dictionary<string, object> {
                                                           { "rule_type", "experiment" },
                                                           { "rule_key", "test_experiment" },
                                                           { "flag_key", "test_experiment" },
                                                           { "variation_key", "control" },
                                                           {"enabled", false }
                                                        }
                                                    }
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
                                        {"entity_id", "323434545" },
                                        {"key", "boolean_key" },
                                        {"type", "custom" },
                                        {"value", true}
                                    },
                                    new Dictionary<string, object>
                                    {
                                        {"entity_id", "808797686" },
                                        {"key", "double_key" },
                                        {"type", "custom" },
                                        {"value", 3.14}
                                    },
                                    new Dictionary<string, object>
                                    {
                                        {"entity_id", ControlAttributes.BOT_FILTERING_ATTRIBUTE},
                                        {"key", ControlAttributes.BOT_FILTERING_ATTRIBUTE},
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
                {"enrich_decisions", true} ,
                {"client_name", "csharp-sdk" },
                {"client_version", Optimizely.SDK_VERSION },
                {"revision", "15" },
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
                { "boolean_key", true },
                { "double_key", 3.14 },
                { "", "Android" },
                { "null", null },
                { "objects", new object() },
                { "arrays", new string[] { "a", "b", "c" } },
                { "negative_infinity", double.NegativeInfinity },
                { "positive_infinity", double.PositiveInfinity },
                { "nan", double.NaN },
                { "invalid_num_value", Math.Pow(2, 53) + 2 },
            };
            var impressionEvent = UserEventFactory.CreateImpressionEvent(Config, Config.GetExperimentFromKey("test_experiment"), "7722370027", TestUserId, userAttributes, "test_experiment", "experiment");
            var logEvent = EventFactory.CreateLogEvent(impressionEvent, Logger);

            TestData.ChangeGUIDAndTimeStamp(expectedLogEvent.Params, impressionEvent.Timestamp, Guid.Parse(impressionEvent.UUID));

            Assert.IsTrue(TestData.CompareObjects(expectedLogEvent, logEvent));
        }

        [Test]
        public void TestCreateImpressionEventRemovesInvalidAttributesFromPayloadRollout()
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
                                                    {"campaign_id", null },
                                                    {"experiment_id", null },
                                                    {"variation_id", null },
                                                    { "metadata", new Dictionary<string, object> {
                                                            { "rule_type", "rollout" },
                                                            { "rule_key", "" },
                                                            { "flag_key", "test_feature" },
                                                            { "variation_key", "" },
                                                            { "enabled", false }
                                                        }
                                                    }
                                                }
                                            }
                                        },
                                        { "events", new object[]
                                            {
                                                new Dictionary<string, object>
                                                {
                                                    {"entity_id", null },
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
                                        {"entity_id", "323434545" },
                                        {"key", "boolean_key" },
                                        {"type", "custom" },
                                        {"value", true}
                                    },
                                    new Dictionary<string, object>
                                    {
                                        {"entity_id", "808797686" },
                                        {"key", "double_key" },
                                        {"type", "custom" },
                                        {"value", 3.14}
                                    },
                                    new Dictionary<string, object>
                                    {
                                        {"entity_id", ControlAttributes.BOT_FILTERING_ATTRIBUTE},
                                        {"key", ControlAttributes.BOT_FILTERING_ATTRIBUTE},
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
                {"enrich_decisions", true} ,
                {"client_name", "csharp-sdk" },
                {"client_version", Optimizely.SDK_VERSION },
                {"revision", "15" },
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
                { "boolean_key", true },
                { "double_key", 3.14 },
                { "", "Android" },
                { "null", null },
                { "objects", new object() },
                { "arrays", new string[] { "a", "b", "c" } },
                { "negative_infinity", double.NegativeInfinity },
                { "positive_infinity", double.PositiveInfinity },
                { "nan", double.NaN },
                { "invalid_num_value", Math.Pow(2, 53) + 2 },
            };
            Variation variation = null;

            var impressionEvent = UserEventFactory.CreateImpressionEvent(Config, null, variation, TestUserId, userAttributes, "test_feature", "rollout");
            var logEvent = EventFactory.CreateLogEvent(impressionEvent, Logger);

            TestData.ChangeGUIDAndTimeStamp(expectedLogEvent.Params, impressionEvent.Timestamp, Guid.Parse(impressionEvent.UUID));

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
                                        {"entity_id", ControlAttributes.BOT_FILTERING_ATTRIBUTE},
                                        {"key", ControlAttributes.BOT_FILTERING_ATTRIBUTE},
                                        {"type", "custom" },
                                        {"value", true }
                                    }
                                }
                            }
                        }
                    }
                },
                {"project_id", "7720880029"},
                {"enrich_decisions", true} ,
                {"account_id", "1592310167"},
                {"client_name", "csharp-sdk"},
                {"client_version", Optimizely.SDK_VERSION },
                {"revision", "15" },
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

            var conversionEvent = UserEventFactory.CreateConversionEvent(Config, "purchase", TestUserId, null, null);
            var logEvent = EventFactory.CreateLogEvent(conversionEvent, Logger);

            TestData.ChangeGUIDAndTimeStamp(expectedEvent.Params, conversionEvent.Timestamp, Guid.Parse(conversionEvent.UUID));

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
                                        {"entity_id", ControlAttributes.BOT_FILTERING_ATTRIBUTE},
                                        {"key", ControlAttributes.BOT_FILTERING_ATTRIBUTE},
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
                {"enrich_decisions", true},
                {"client_name", "csharp-sdk"},
                {"client_version", Optimizely.SDK_VERSION },
                {"revision", "15" },
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
            var conversionEvent = UserEventFactory.CreateConversionEvent(Config, "purchase", TestUserId, userAttributes, null);
            var logEvent = EventFactory.CreateLogEvent(conversionEvent, Logger);

            TestData.ChangeGUIDAndTimeStamp(expectedEvent.Params, conversionEvent.Timestamp, Guid.Parse(conversionEvent.UUID)); ;

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
                                        {"entity_id", ControlAttributes.BOT_FILTERING_ATTRIBUTE},
                                        {"key", ControlAttributes.BOT_FILTERING_ATTRIBUTE},
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
                {"enrich_decisions", true},
                {"client_name", "csharp-sdk" },
                {"client_version", Optimizely.SDK_VERSION },
                {"revision", "15" },
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

            var conversionEvent = UserEventFactory.CreateConversionEvent(Config, "purchase", TestUserId, null,
                new EventTags
            {
                    {"revenue", 42 }
            });
            var logEvent = EventFactory.CreateLogEvent(conversionEvent, Logger);

            TestData.ChangeGUIDAndTimeStamp(expectedEvent.Params, conversionEvent.Timestamp, Guid.Parse(conversionEvent.UUID));

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
                                        {"entity_id", ControlAttributes.BOT_FILTERING_ATTRIBUTE},
                                        {"key", ControlAttributes.BOT_FILTERING_ATTRIBUTE},
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
                {"enrich_decisions", true},
                {"account_id", "1592310167" },
                {"client_name", "csharp-sdk" },
                {"client_version", Optimizely.SDK_VERSION },
                {"revision", "15" },
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

            var conversionEvent = UserEventFactory.CreateConversionEvent(Config, "purchase", TestUserId, userAttributes,
                new EventTags
                {
                    {"revenue", 42 },
                    {"non-revenue", "definitely" }
                });
            var logEvent = EventFactory.CreateLogEvent(conversionEvent, Logger);

            TestData.ChangeGUIDAndTimeStamp(expectedEvent.Params, conversionEvent.Timestamp, Guid.Parse(conversionEvent.UUID));

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
                                        {"entity_id", ControlAttributes.BOT_FILTERING_ATTRIBUTE},
                                        {"key", ControlAttributes.BOT_FILTERING_ATTRIBUTE},
                                        {"type", "custom" },
                                        {"value", true }
                                    }
                                }
                            }
                        }
                    }
                },
                {"project_id", "7720880029" },
                {"enrich_decisions", true},
                {"account_id", "1592310167" },
                {"client_name", "csharp-sdk" },
                {"client_version", Optimizely.SDK_VERSION },
                {"revision", "15" },
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

            var conversionEvent = UserEventFactory.CreateConversionEvent(Config, "purchase", TestUserId, null,
                new EventTags
                {
                    {"revenue", "42" },
                    {"non-revenue", "definitely" }
                });
            var logEvent = EventFactory.CreateLogEvent(conversionEvent, Logger);
            TestData.ChangeGUIDAndTimeStamp(expectedEvent.Params, conversionEvent.Timestamp, Guid.Parse(conversionEvent.UUID));

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
                                        {"entity_id", ControlAttributes.BOT_FILTERING_ATTRIBUTE},
                                        {"key", ControlAttributes.BOT_FILTERING_ATTRIBUTE},
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
                {"enrich_decisions", true},
                {"account_id", "1592310167" },
                {"client_name", "csharp-sdk" },
                {"client_version", Optimizely.SDK_VERSION },
                {"revision", "15" },
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

            var conversionEvent = UserEventFactory.CreateConversionEvent(Config, "purchase", TestUserId, null,
                new EventTags
            {
                    {"revenue", 42 },
                    {"value", 400 }
            });


            var logEvent = EventFactory.CreateLogEvent(conversionEvent, Logger);

            TestData.ChangeGUIDAndTimeStamp(expectedEvent.Params, conversionEvent.Timestamp, Guid.Parse(conversionEvent.UUID));

            Assert.IsTrue(TestData.CompareObjects(expectedEvent, logEvent));
        }

        [Test]
        public void TestConversionEventWithFalsyNumericAndRevenueValues()
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
                                        { "events", new object[]
                                            {
                                                new Dictionary<string, object>
                                                {
                                                    {"entity_id", "7718020063" },
                                                    {"timestamp", timeStamp },
                                                    {"uuid", guid },
                                                    {"key", "purchase" },
                                                    {"revenue", 0 },
                                                    {"value", 0.0 },
                                                    {"tags",
                                                        new Dictionary<string, object>
                                                        {
                                                            {"revenue", 0 },
                                                            {"value", 0.0 }
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
                                        {"entity_id", ControlAttributes.BOT_FILTERING_ATTRIBUTE},
                                        {"key", ControlAttributes.BOT_FILTERING_ATTRIBUTE},
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
                {"enrich_decisions", true},
                {"account_id", "1592310167" },
                {"client_name", "csharp-sdk" },
                {"client_version", Optimizely.SDK_VERSION },
                {"revision", "15" },
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

            var conversionEvent = UserEventFactory.CreateConversionEvent(Config, "purchase", TestUserId, null,
                new EventTags
            {
                    {"revenue", 0 },
                    {"value", 0.0 }
            });
            var logEvent = EventFactory.CreateLogEvent(conversionEvent, Logger);

            TestData.ChangeGUIDAndTimeStamp(expectedEvent.Params, conversionEvent.Timestamp, Guid.Parse(conversionEvent.UUID));

            Assert.IsTrue(TestData.CompareObjects(expectedEvent, logEvent));
        }

        [Test]
        public void TestConversionEventWithNumericValue1()
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
                                        { "events", new object[]
                                            {
                                                new Dictionary<string, object>
                                                {
                                                    {"entity_id", "7718020063" },
                                                    {"timestamp", timeStamp },
                                                    {"uuid", guid },
                                                    {"key", "purchase" },
                                                    {"revenue", 10 },
                                                    {"value", 1.0 },
                                                    {"tags",
                                                        new Dictionary<string, object>
                                                        {
                                                            {"revenue", 10 },
                                                            {"value", 1.0 }
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
                                        {"entity_id", ControlAttributes.BOT_FILTERING_ATTRIBUTE},
                                        {"key", ControlAttributes.BOT_FILTERING_ATTRIBUTE},
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
                {"enrich_decisions", true},
                {"account_id", "1592310167" },
                {"client_name", "csharp-sdk" },
                {"client_version", Optimizely.SDK_VERSION },
                {"revision", "15" },
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
            var conversionEvent = UserEventFactory.CreateConversionEvent(Config, "purchase", TestUserId, null,
                new EventTags
            {
                    {"revenue", 10 },
                    {"value", 1.0 }
            });
            var logEvent = EventFactory.CreateLogEvent(conversionEvent, Logger);

            TestData.ChangeGUIDAndTimeStamp(expectedEvent.Params, conversionEvent.Timestamp, Guid.Parse(conversionEvent.UUID));

            Assert.IsTrue(TestData.CompareObjects(expectedEvent, logEvent));
        }

        [Test]
        public void TestConversionEventWithRevenueValue1()
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
                                        { "events", new object[]
                                            {
                                                new Dictionary<string, object>
                                                {
                                                    {"entity_id", "7718020063" },
                                                    {"timestamp", timeStamp },
                                                    {"uuid", guid },
                                                    {"key", "purchase" },
                                                    {"revenue", 1 },
                                                    {"value", 10.0 },
                                                    {"tags",
                                                        new Dictionary<string, object>
                                                        {
                                                            {"revenue", 1 },
                                                            {"value", 10.0 }
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
                                        {"entity_id", ControlAttributes.BOT_FILTERING_ATTRIBUTE},
                                        {"key", ControlAttributes.BOT_FILTERING_ATTRIBUTE},
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
                {"enrich_decisions", true},
                {"account_id", "1592310167" },
                {"client_name", "csharp-sdk" },
                {"client_version", Optimizely.SDK_VERSION },
                {"revision", "15" },
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

            var conversionEvent = UserEventFactory.CreateConversionEvent(Config, "purchase", TestUserId, null,
                new EventTags
            {
                    {"revenue", 1 },
                    {"value", 10.0 }
            });
            var logEvent = EventFactory.CreateLogEvent(conversionEvent, Logger);

            TestData.ChangeGUIDAndTimeStamp(expectedEvent.Params, conversionEvent.Timestamp, Guid.Parse(conversionEvent.UUID));

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
                                        {"entity_id", ControlAttributes.BUCKETING_ID_ATTRIBUTE },
                                        {"key", ControlAttributes.BUCKETING_ID_ATTRIBUTE },
                                        {"type", "custom" },
                                        {"value", "variation"}
                                    },
                                    new Dictionary<string, object>
                                    {
                                        {"entity_id", ControlAttributes.BOT_FILTERING_ATTRIBUTE},
                                        {"key", ControlAttributes.BOT_FILTERING_ATTRIBUTE},
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
                {"enrich_decisions", true} ,
                {"client_name", "csharp-sdk" },
                {"client_version", Optimizely.SDK_VERSION },
                {"revision", "15" },
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
                {ControlAttributes.BUCKETING_ID_ATTRIBUTE, "variation" }
            };

            var conversionEvent = UserEventFactory.CreateConversionEvent(Config, "purchase", TestUserId, userAttributes, null);
            var logEvent = EventFactory.CreateLogEvent(conversionEvent, Logger);

            TestData.ChangeGUIDAndTimeStamp(expectedEvent.Params, conversionEvent.Timestamp, Guid.Parse(conversionEvent.UUID));

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
                                                    {"variation_id", "7722370027" },
                                                    { "metadata", new Dictionary<string, object> {
                                                            { "rule_type", "experiment" },
                                                            { "rule_key", "test_experiment" },
                                                            { "flag_key", "test_experiment" },
                                                            { "variation_key", "control" },
                                                            {"enabled", false }
                                                        }
                                                    }
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
                                        {"entity_id", ControlAttributes.BUCKETING_ID_ATTRIBUTE },
                                        {"key", ControlAttributes.BUCKETING_ID_ATTRIBUTE },
                                        {"type", "custom" },
                                        {"value", "variation"}
                                    },
                                    new Dictionary<string, object>
                                    {
                                        {"entity_id", ControlAttributes.BOT_FILTERING_ATTRIBUTE},
                                        {"key", ControlAttributes.BOT_FILTERING_ATTRIBUTE},
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
                {"enrich_decisions", true},
                {"account_id", "1592310167" },
                {"client_name", "csharp-sdk" },
                {"client_version", Optimizely.SDK_VERSION },
                {"revision", "15" },
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
                {ControlAttributes.BUCKETING_ID_ATTRIBUTE, "variation" }
            };
            var impressionEvent = UserEventFactory.CreateImpressionEvent(Config, Config.GetExperimentFromKey("test_experiment"), "7722370027", TestUserId, userAttributes, "test_experiment", "experiment");
            var logEvent = EventFactory.CreateLogEvent(impressionEvent, Logger);

            TestData.ChangeGUIDAndTimeStamp(expectedLogEvent.Params, impressionEvent.Timestamp, Guid.Parse(impressionEvent.UUID));

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
                                                    {"variation_id", "7722370027" },
                                                    { "metadata", new Dictionary<string, object> {
                                                            { "rule_type", "experiment" },
                                                            { "rule_key", "test_experiment" },
                                                            { "flag_key", "test_experiment" },
                                                            { "variation_key", "control" },
                                                            {"enabled", false }
                                                        }
                                                    }
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
                                        {"entity_id", ControlAttributes.USER_AGENT_ATTRIBUTE },
                                        {"key", ControlAttributes.USER_AGENT_ATTRIBUTE },
                                        {"type", "custom" },
                                        {"value", "chrome"}
                                    },
                                    new Dictionary<string, object>
                                    {
                                        {"entity_id", ControlAttributes.BOT_FILTERING_ATTRIBUTE},
                                        {"key", ControlAttributes.BOT_FILTERING_ATTRIBUTE},
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
                {"enrich_decisions", true} ,
                {"client_name", "csharp-sdk" },
                {"client_version", Optimizely.SDK_VERSION },
                {"revision", "15" },
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
                {ControlAttributes.USER_AGENT_ATTRIBUTE, "chrome" }
            };

            var botFilteringEnabledConfig = Config;
            botFilteringEnabledConfig.BotFiltering = true;
            var experiment = botFilteringEnabledConfig.GetExperimentFromKey("test_experiment");

            var impressionEvent = UserEventFactory.CreateImpressionEvent(botFilteringEnabledConfig, experiment, "7722370027", TestUserId, userAttributes, "test_experiment", "experiment");
            var logEvent = EventFactory.CreateLogEvent(impressionEvent, Logger);

            TestData.ChangeGUIDAndTimeStamp(expectedLogEvent.Params, impressionEvent.Timestamp, Guid.Parse(impressionEvent.UUID));

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
                                                    {"variation_id", "7722370027" },
                                                    { "metadata", new Dictionary<string, object> {
                                                            { "rule_type", "experiment" },
                                                            { "rule_key", "test_experiment" },
                                                            { "flag_key", "test_experiment" },
                                                            { "variation_key", "control" },
                                                            {"enabled", false }
                                                        }
                                                    }
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
                                        {"entity_id", ControlAttributes.USER_AGENT_ATTRIBUTE },
                                        {"key", ControlAttributes.USER_AGENT_ATTRIBUTE },
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
                {"enrich_decisions", true} ,
                {"client_name", "csharp-sdk" },
                {"client_version", Optimizely.SDK_VERSION },
                {"revision", "15" },
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
                {ControlAttributes.USER_AGENT_ATTRIBUTE, "chrome" }
            };

            var botFilteringDisabledConfig = Config;
            botFilteringDisabledConfig.BotFiltering = null;
            var experiment = botFilteringDisabledConfig.GetExperimentFromKey("test_experiment");

            var impressionEvent = UserEventFactory.CreateImpressionEvent(botFilteringDisabledConfig, experiment, "7722370027", TestUserId, userAttributes, "test_experiment", "experiment");
            var logEvent = EventFactory.CreateLogEvent(impressionEvent, Logger);

            TestData.ChangeGUIDAndTimeStamp(expectedLogEvent.Params, impressionEvent.Timestamp, Guid.Parse(impressionEvent.UUID));

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
                                        {"entity_id", ControlAttributes.USER_AGENT_ATTRIBUTE },
                                        {"key", ControlAttributes.USER_AGENT_ATTRIBUTE },
                                        {"type", "custom" },
                                        {"value", "safari"}
                                    },
                                    new Dictionary<string, object>
                                    {
                                        {"entity_id", ControlAttributes.BOT_FILTERING_ATTRIBUTE},
                                        {"key", ControlAttributes.BOT_FILTERING_ATTRIBUTE},
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
                {"enrich_decisions", true} ,
                {"client_name", "csharp-sdk"},
                {"client_version", Optimizely.SDK_VERSION },
                {"revision", "15" },
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
                {ControlAttributes.USER_AGENT_ATTRIBUTE, "safari" }
            };
            var experimentToVariationMap = new Dictionary<string, Variation>
            {
                {"7716830082", new Variation{Id="7722370027", Key="control"} }
            };

            var botFilteringEnabledConfig = Config;
            botFilteringEnabledConfig.BotFiltering = true;

            var conversionEvent = UserEventFactory.CreateConversionEvent(botFilteringEnabledConfig, "purchase", TestUserId, userAttributes, null);
            var logEvent = EventFactory.CreateLogEvent(conversionEvent, Logger);

            TestData.ChangeGUIDAndTimeStamp(expectedEvent.Params, conversionEvent.Timestamp, Guid.Parse(conversionEvent.UUID));

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
                                        {"entity_id", ControlAttributes.USER_AGENT_ATTRIBUTE },
                                        {"key", ControlAttributes.USER_AGENT_ATTRIBUTE },
                                        {"type", "custom" },
                                        {"value", "safari"}
                                    }
                                }
                            }
                        }
                    }
                },
                {"project_id", "7720880029"},
                {"enrich_decisions", true},
                {"account_id", "1592310167"},
                {"client_name", "csharp-sdk"},
                {"client_version", Optimizely.SDK_VERSION },
                {"revision", "15" },
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
                {ControlAttributes.USER_AGENT_ATTRIBUTE, "safari" }
            };
            var experimentToVariationMap = new Dictionary<string, Variation>
            {
                {"7716830082", new Variation{Id="7722370027", Key="control"} }
            };

            var botFilteringDisabledConfig = Config;
            botFilteringDisabledConfig.BotFiltering = null;

            var conversionEvent = UserEventFactory.CreateConversionEvent(botFilteringDisabledConfig, "purchase", TestUserId, userAttributes, null);
            var logEvent = EventFactory.CreateLogEvent(conversionEvent, Logger);

            TestData.ChangeGUIDAndTimeStamp(expectedEvent.Params, conversionEvent.Timestamp, Guid.Parse(conversionEvent.UUID));

            Assert.IsTrue(TestData.CompareObjects(expectedEvent, logEvent));
        }

        [Test]
        public void TestCreateConversionEventWhenEventUsedInMultipleExp()
        {
            var guid = Guid.NewGuid();
            var timeStamp = TestData.SecondsSince1970();

            var eventInMultiExperimentConfig = DatafileProjectConfig.Create(TestData.SimpleABExperimentsDatafile, new NoOpLogger(), new ErrorHandler.NoOpErrorHandler());

            var experimentIdVariationMap = new Dictionary<string, Variation>
            {
                {
                    "111127", new Variation{Id="111129", Key="variation"}
                },
                {
                    "111130", new Variation{Id="111131", Key="variation"}
                }
            };

            var payloadParams = new Dictionary<string, object>
                {
                {"client_version", Optimizely.SDK_VERSION},
                {"project_id", "111001"},
                {"enrich_decisions", true},
                {"account_id", "12001"},
                {"client_name", "csharp-sdk"},
                {"anonymize_ip", false},
                {"revision", eventInMultiExperimentConfig.Revision},
                {"visitors", new object[]
                    {
                        //visitors[0]
                        new Dictionary<string, object>
                        {
                            //visitors[0].attributes
                            {
                                "attributes", new object[]
                                {
                                    new Dictionary<string, string>
                                    {
                                        {"entity_id", "111094"},
                                        {"type", "custom"},
                                        {"value", "test_value"},
                                        {"key", "test_attribute"}
                                    }
                                }
                            },
                            //visitors[0].visitor_id
                            {"visitor_id", "test_user"},
                            //visitors[0].snapshots
                            {"snapshots", new object[]
                                {
                                    //snapshots[0]
                                    new Dictionary<string, object>
                                    {
                                        //snapshots[0].events
                                        {
                                            "events", new object[]
                                            {
                                                new Dictionary<string, object>
                                                {
                                                    {"uuid", guid},
                                                    {"timestamp", timeStamp},
                                                    {"revenue", 4200},
                                                    {"value", 1.234},
                                                    {"key", "event_with_multiple_running_experiments"},
                                                    {"entity_id", "111095"},
                                                    {
                                                        "tags", new Dictionary<string, object>
                                                        {
                                                            {"non-revenue", "abc"},
                                                            {"revenue", 4200},
                                                            {"value", 1.234},
                                                        }
                                                    }

                                                }
                                            }
                                        }

                                    }

                                }
                            }

                        }
                    }

                }
            };


            var expectedLogEvent = new LogEvent(
                "https://logx.optimizely.com/v1/events",
                payloadParams,
                "POST",
                new Dictionary<string, string>
                {
                    { "Content-Type", "application/json"}
                });
            var conversionEvent = UserEventFactory.CreateConversionEvent(eventInMultiExperimentConfig, "event_with_multiple_running_experiments", "test_user",
                                                              new UserAttributes {
                                                                {"test_attribute", "test_value"}
                                                              },
                                                              new EventTags {
                                                                {"revenue", 4200},
                                                                {"value", 1.234},
                                                                {"non-revenue", "abc"}
                                                             });
            var logEvent = EventFactory.CreateLogEvent(conversionEvent, Logger);

            TestData.ChangeGUIDAndTimeStamp(expectedLogEvent.Params, conversionEvent.Timestamp, Guid.Parse(conversionEvent.UUID));

            Assert.IsTrue(TestData.CompareObjects(expectedLogEvent, logEvent));
        }

        [Test]
        public void TestCreateConversionEventRemovesInvalidAttributesFromPayload()
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
                                        {"entity_id", "323434545" },
                                        {"key", "boolean_key" },
                                        {"type", "custom" },
                                        {"value", true}
                                    },
                                    new Dictionary<string, object>
                                    {
                                        {"entity_id", "808797686" },
                                        {"key", "double_key" },
                                        {"type", "custom" },
                                        {"value", 3.14}
                                    },
                                    new Dictionary<string, object>
                                    {
                                        {"entity_id", ControlAttributes.BOT_FILTERING_ATTRIBUTE},
                                        {"key", ControlAttributes.BOT_FILTERING_ATTRIBUTE},
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
                {"enrich_decisions", true},
                {"client_name", "csharp-sdk"},
                {"client_version", Optimizely.SDK_VERSION },
                {"revision", "15" },
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
                { "boolean_key", true },
                { "double_key", 3.14 },
                { "", "Android" },
                { "null", null },
                { "objects", new object() },
                { "arrays", new string[] { "a", "b", "c" } },
                { "negative_infinity", double.NegativeInfinity },
                { "positive_infinity", double.PositiveInfinity },
                { "nan", double.NaN },
                { "invalid_num_value", Math.Pow(2, 53) + 2 },
            };

            var experimentToVariationMap = new Dictionary<string, Variation>
            {
                {"7716830082", new Variation{Id="7722370027", Key="control"} }
            };
            var conversionEvent = UserEventFactory.CreateConversionEvent(Config, "purchase", TestUserId, userAttributes, null);
            var logEvent = EventFactory.CreateLogEvent(conversionEvent, Logger);

            TestData.ChangeGUIDAndTimeStamp(expectedEvent.Params, conversionEvent.Timestamp, Guid.Parse(conversionEvent.UUID));
            Assert.IsTrue(TestData.CompareObjects(expectedEvent, logEvent));
        }
    }
}

