/*
 * Copyright 2017-2022, Optimizely
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * https://www.apache.org/licenses/LICENSE-2.0
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
using OptimizelySDK.Event.Dispatcher;
using OptimizelySDK.ErrorHandler;
using OptimizelySDK.Exceptions;
using OptimizelySDK.Event;
using OptimizelySDK.Entity;
using OptimizelySDK.Tests.UtilsTests;
using OptimizelySDK.Bucketing;
using OptimizelySDK.Notifications;
using OptimizelySDK.Tests.NotificationTests;
using OptimizelySDK.Utils;
using OptimizelySDK.Config;
using OptimizelySDK.Event.Entity;
using OptimizelySDK.Tests.Utils;
using OptimizelySDK.OptimizelyDecisions;
using OptimizelySDK.OptlyConfig;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace OptimizelySDK.Tests
{
    [TestFixture]
    public class OptimizelyTest
    {
        private Mock<ILogger> LoggerMock;
        private ProjectConfigManager ConfigManager;
        private ProjectConfig Config;
        private Mock<IErrorHandler> ErrorHandlerMock;
        private Mock<IEventDispatcher> EventDispatcherMock;
        private Optimizely Optimizely;
        private const string TestUserId = "testUserId";
        private OptimizelyHelper Helper;
        private Mock<Optimizely> OptimizelyMock;
        private Mock<DecisionService> DecisionServiceMock;
        private Mock<EventProcessor> EventProcessorMock;
        private NotificationCenter NotificationCenter;
        private Mock<TestNotificationCallbacks> NotificationCallbackMock;
        private Variation VariationWithKeyControl;
        private Variation VariationWithKeyVariation;
        private Variation GroupVariation;
        private Optimizely OptimizelyWithTypedAudiences;
        private DecisionReasons DecisionReasons;

        private const string FEATUREVARIABLE_BOOLEANTYPE = "boolean";
        private const string FEATUREVARIABLE_INTEGERTYPE = "integer";
        private const string FEATUREVARIABLE_DOUBLETYPE = "double";
        private const string FEATUREVARIABLE_STRINGTYPE = "string";
        private const string FEATUREVARIABLE_JSONTYPE = "json";

        #region Test Life Cycle

        [SetUp]
        public void Initialize()
        {
            LoggerMock = new Mock<ILogger>();
            LoggerMock.Setup(i => i.Log(It.IsAny<LogLevel>(), It.IsAny<string>()));

            ErrorHandlerMock = new Mock<IErrorHandler>();
            ErrorHandlerMock.Setup(e => e.HandleError(It.IsAny<Exception>()));

            EventProcessorMock = new Mock<EventProcessor>();

            EventProcessorMock.Setup(b => b.Process(It.IsAny<UserEvent>()));
            DecisionReasons = new DecisionReasons();
            ProjectConfig config = DatafileProjectConfig.Create(
                TestData.Datafile,
                LoggerMock.Object,
                new NoOpErrorHandler());
            ConfigManager = new FallbackProjectConfigManager(config);
            Config = ConfigManager.GetConfig();
            EventDispatcherMock = new Mock<IEventDispatcher>();
            Optimizely = new Optimizely(TestData.Datafile, EventDispatcherMock.Object,
                LoggerMock.Object, ErrorHandlerMock.Object);
            OptimizelyWithTypedAudiences = new Optimizely(TestData.TypedAudienceDatafile,
                EventDispatcherMock.Object, LoggerMock.Object, ErrorHandlerMock.Object);

            Helper = new OptimizelyHelper
            {
                Datafile = TestData.Datafile,
                EventDispatcher = EventDispatcherMock.Object,
                Logger = LoggerMock.Object,
                ErrorHandler = ErrorHandlerMock.Object,
                SkipJsonValidation = false,
            };

            OptimizelyMock = new Mock<Optimizely>(TestData.Datafile, EventDispatcherMock.Object,
                LoggerMock.Object, ErrorHandlerMock.Object, null, false, null, null)
            {
                CallBase = true,
            };

            DecisionServiceMock = new Mock<DecisionService>(new Bucketer(LoggerMock.Object),
                ErrorHandlerMock.Object,
                null, LoggerMock.Object);

            NotificationCenter = new NotificationCenter(LoggerMock.Object);
            NotificationCallbackMock = new Mock<TestNotificationCallbacks>();

            VariationWithKeyControl = Config.GetVariationFromKey("test_experiment", "control");
            VariationWithKeyVariation = Config.GetVariationFromKey("test_experiment", "variation");
            GroupVariation = Config.GetVariationFromKey("group_experiment_1", "group_exp_1_var_2");
        }

        [TestFixtureTearDown]
        public void Cleanup()
        {
            LoggerMock = null;
            Config = null;
        }

        #endregion Test Life Cycle

        #region OptimizelyHelper

        private class OptimizelyHelper
        {
            private static Type[] ParameterTypes =
            {
                typeof(string), typeof(IEventDispatcher), typeof(ILogger), typeof(IErrorHandler),
                typeof(bool), typeof(EventProcessor),
            };

            public static Dictionary<string, object> SingleParameter =
                new Dictionary<string, object>
                {
                    {
                        "param1", "val1"
                    },
                };

            public static UserAttributes UserAttributes = new UserAttributes
            {
                {
                    "device_type", "iPhone"
                },
                {
                    "company", "Optimizely"
                },
                {
                    "location", "San Francisco"
                },
            };

            // NullUserAttributes extends copy of UserAttributes with key-value
            // pairs containing null values which should not be sent to OPTIMIZELY.COM .
            public static UserAttributes NullUserAttributes = new UserAttributes
            {
                {
                    "device_type", "iPhone"
                },
                {
                    "company", "Optimizely"
                },
                {
                    "location", "San Francisco"
                },
                {
                    "null_value", null
                },
                {
                    "wont_be_sent", null
                },
                {
                    "bad_food", null
                },
            };

            public string Datafile { get; set; }
            public IEventDispatcher EventDispatcher { get; set; }
            public ILogger Logger { get; set; }
            public IErrorHandler ErrorHandler { get; set; }
            public UserProfileService UserProfileService { get; set; }
            public bool SkipJsonValidation { get; set; }
            public EventProcessor EventProcessor { get; set; }

            public OptimizelyDecideOption[] DefaultDecideOptions { get; set; }

            public PrivateObject CreatePrivateOptimizely()
            {
                return new PrivateObject(typeof(Optimizely), ParameterTypes,
                    new object[]
                    {
                        Datafile, EventDispatcher, Logger, ErrorHandler, UserProfileService,
                        SkipJsonValidation, EventProcessor, DefaultDecideOptions,
                    });
            }
        }

        #endregion OptimizelyHelper

        #region Test UserContext

        [Test]
        public void TestCreateUserContext()
        {
            UserAttributes attribute = new UserAttributes
            {
                {
                    "device_type", "iPhone"
                },
                {
                    "location", "San Francisco"
                },
            };
            OptimizelyUserContext optlyUserContext =
                Optimizely.CreateUserContext(TestUserId, attribute);
            Assert.AreEqual(TestUserId, optlyUserContext.GetUserId());
            Assert.AreEqual(Optimizely, optlyUserContext.GetOptimizely());
            Assert.AreEqual(attribute, optlyUserContext.GetAttributes());
        }

        [Test]
        public void TestCreateUserContextWithoutAttributes()
        {
            OptimizelyUserContext optlyUserContext = Optimizely.CreateUserContext(TestUserId);
            Assert.AreEqual(TestUserId, optlyUserContext.GetUserId());
            Assert.AreEqual(Optimizely, optlyUserContext.GetOptimizely());
            Assert.IsTrue(optlyUserContext.GetAttributes().Count == 0);
        }

        [Test]
        public void TestCreateUserContextMultipleAttribute()
        {
            UserAttributes attribute1 = new UserAttributes
            {
                {
                    "device_type", "iPhone"
                },
                {
                    "location", "San Francisco"
                },
            };
            OptimizelyUserContext optlyUserContext1 =
                Optimizely.CreateUserContext("userId1", attribute1);

            UserAttributes attribute2 = new UserAttributes
            {
                {
                    "device_type2", "Samsung"
                },
                {
                    "location2", "California"
                },
            };
            OptimizelyUserContext optlyUserContext2 =
                Optimizely.CreateUserContext("userId2", attribute2);

            Assert.AreEqual("userId1", optlyUserContext1.GetUserId());
            Assert.AreEqual(Optimizely, optlyUserContext1.GetOptimizely());
            Assert.AreEqual(attribute1, optlyUserContext1.GetAttributes());

            Assert.AreEqual("userId2", optlyUserContext2.GetUserId());
            Assert.AreEqual(Optimizely, optlyUserContext2.GetOptimizely());
            Assert.AreEqual(attribute2, optlyUserContext2.GetAttributes());
        }

        [Test]
        public void TestDecisionNotificationSentWhenSendFlagDecisionsFalseAndFeature()
        {
            string featureKey = "boolean_feature";
            OptimizelyJSON variables = Optimizely.GetAllFeatureVariables(featureKey, TestUserId);
            UserAttributes userAttributes = new UserAttributes
            {
                {
                    "device_type", "iPhone"
                },
                {
                    "location", "San Francisco"
                },
            };
            Config.SendFlagDecisions = false;
            FallbackProjectConfigManager fallbackConfigManager =
                new FallbackProjectConfigManager(Config);
            Optimizely optimizely = new Optimizely(fallbackConfigManager,
                NotificationCenter,
                EventDispatcherMock.Object,
                LoggerMock.Object,
                ErrorHandlerMock.Object,
                null,
                new ForwardingEventProcessor(EventDispatcherMock.Object, NotificationCenter,
                    LoggerMock.Object, ErrorHandlerMock.Object),
                null);

            // Mocking objects.
            NotificationCallbackMock.Setup(nc => nc.TestDecisionCallback(It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<UserAttributes>(), It.IsAny<Dictionary<string, object>>()));
            optimizely.NotificationCenter.AddNotification(
                NotificationCenter.NotificationType.Decision,
                NotificationCallbackMock.Object.TestDecisionCallback);

            OptimizelyUserContext optimizelyUserContext =
                optimizely.CreateUserContext(TestUserId, userAttributes);
            optimizelyUserContext.Decide(featureKey);
            NotificationCallbackMock.Verify(nc => nc.TestDecisionCallback(
                DecisionNotificationTypes.FLAG, TestUserId, userAttributes,
                It.Is<Dictionary<string, object>>(info => TestData.CompareObjects(info,
                    new Dictionary<string, object>
                    {
                        {
                            "flagKey", featureKey
                        },
                        {
                            "enabled", false
                        },
                        {
                            "variables", variables.ToDictionary()
                        },
                        {
                            "variationKey", "group_exp_2_var_1"
                        },
                        {
                            "ruleKey", "group_experiment_2"
                        },
                        {
                            "reasons", new OptimizelyDecideOption[0]
                        },
                        {
                            "decisionEventDispatched", true
                        },
                    }))), Times.Once);
            EventDispatcherMock.Verify(dispatcher => dispatcher.DispatchEvent(It.IsAny<LogEvent>()),
                Times.Once);
        }

        [Test]
        public void TestDecisionNotificationSentWhenSendFlagDecisionsTrueAndFeature()
        {
            string featureKey = "boolean_feature";
            OptimizelyJSON variables = Optimizely.GetAllFeatureVariables(featureKey, TestUserId);
            UserAttributes userAttributes = new UserAttributes
            {
                {
                    "device_type", "iPhone"
                },
                {
                    "location", "San Francisco"
                },
            };
            FallbackProjectConfigManager fallbackConfigManager =
                new FallbackProjectConfigManager(Config);
            Optimizely optimizely = new Optimizely(fallbackConfigManager,
                NotificationCenter,
                EventDispatcherMock.Object,
                LoggerMock.Object,
                ErrorHandlerMock.Object,
                null,
                new ForwardingEventProcessor(EventDispatcherMock.Object, NotificationCenter,
                    LoggerMock.Object, ErrorHandlerMock.Object),
                null);

            // Mocking objects.
            NotificationCallbackMock.Setup(nc => nc.TestDecisionCallback(It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<UserAttributes>(), It.IsAny<Dictionary<string, object>>()));
            optimizely.NotificationCenter.AddNotification(
                NotificationCenter.NotificationType.Decision,
                NotificationCallbackMock.Object.TestDecisionCallback);

            OptimizelyUserContext optimizelyUserContext =
                optimizely.CreateUserContext(TestUserId, userAttributes);
            optimizelyUserContext.Decide(featureKey);
            NotificationCallbackMock.Verify(nc => nc.TestDecisionCallback(
                DecisionNotificationTypes.FLAG, TestUserId, userAttributes,
                It.Is<Dictionary<string, object>>(info => TestData.CompareObjects(info,
                    new Dictionary<string, object>
                    {
                        {
                            "flagKey", featureKey
                        },
                        {
                            "enabled", false
                        },
                        {
                            "variables", variables.ToDictionary()
                        },
                        {
                            "variationKey", "group_exp_2_var_1"
                        },
                        {
                            "ruleKey", "group_experiment_2"
                        },
                        {
                            "reasons", new OptimizelyDecideOption[0]
                        },
                        {
                            "decisionEventDispatched", true
                        },
                    }))), Times.Once);
            EventDispatcherMock.Verify(dispatcher => dispatcher.DispatchEvent(It.IsAny<LogEvent>()),
                Times.Once);
        }

        [Test]
        public void TestDecisionNotificationNotSentWhenSendFlagDecisionsFalseAndRollout()
        {
            string featureKey = "boolean_single_variable_feature";
            FeatureFlag featureFlag = Config.GetFeatureFlagFromKey(featureKey);
            OptimizelyJSON variables = Optimizely.GetAllFeatureVariables(featureKey, TestUserId);
            UserAttributes userAttributes = new UserAttributes
            {
                {
                    "device_type", "iPhone"
                },
                {
                    "location", "San Francisco"
                },
            };
            Experiment experiment = Config.GetRolloutFromId("166660").Experiments[1];
            string ruleKey = experiment.Key;
            Variation variation = Config.GetVariationFromKey(experiment.Key, "177773");
            Config.SendFlagDecisions = false;
            FallbackProjectConfigManager fallbackConfigManager =
                new FallbackProjectConfigManager(Config);
            Optimizely optimizely = new Optimizely(fallbackConfigManager,
                NotificationCenter,
                EventDispatcherMock.Object,
                LoggerMock.Object,
                ErrorHandlerMock.Object,
                null,
                new ForwardingEventProcessor(EventDispatcherMock.Object, NotificationCenter,
                    LoggerMock.Object, ErrorHandlerMock.Object),
                null);

            // Mocking objects.
            NotificationCallbackMock.Setup(nc => nc.TestDecisionCallback(It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<UserAttributes>(), It.IsAny<Dictionary<string, object>>()));
            optimizely.NotificationCenter.AddNotification(
                NotificationCenter.NotificationType.Decision,
                NotificationCallbackMock.Object.TestDecisionCallback);

            OptimizelyUserContext optimizelyUserContext =
                optimizely.CreateUserContext(TestUserId, userAttributes);
            optimizelyUserContext.Decide(featureKey);
            NotificationCallbackMock.Verify(nc => nc.TestDecisionCallback(
                DecisionNotificationTypes.FLAG, TestUserId, userAttributes,
                It.Is<Dictionary<string, object>>(info => TestData.CompareObjects(info,
                    new Dictionary<string, object>
                    {
                        {
                            "flagKey", featureKey
                        },
                        {
                            "enabled", true
                        },
                        {
                            "variables", variables.ToDictionary()
                        },
                        {
                            "variationKey", variation.Key
                        },
                        {
                            "ruleKey", ruleKey
                        },
                        {
                            "reasons", new OptimizelyDecideOption[0]
                        },
                        {
                            "decisionEventDispatched", false
                        },
                    }))), Times.Once);
            EventDispatcherMock.Verify(dispatcher => dispatcher.DispatchEvent(It.IsAny<LogEvent>()),
                Times.Never);
        }

        [Test]
        public void TestDecisionNotificationSentWhenSendFlagDecisionsTrueAndRollout()
        {
            string featureKey = "boolean_single_variable_feature";
            FeatureFlag featureFlag = Config.GetFeatureFlagFromKey(featureKey);
            OptimizelyJSON variables = Optimizely.GetAllFeatureVariables(featureKey, TestUserId);
            UserAttributes userAttributes = new UserAttributes
            {
                {
                    "device_type", "iPhone"
                },
                {
                    "location", "San Francisco"
                },
            };
            Experiment experiment = Config.GetRolloutFromId("166660").Experiments[1];
            string ruleKey = experiment.Key;
            Variation variation = Config.GetVariationFromKey(experiment.Key, "177773");
            Config.SendFlagDecisions = true;
            FallbackProjectConfigManager fallbackConfigManager =
                new FallbackProjectConfigManager(Config);
            Optimizely optimizely = new Optimizely(fallbackConfigManager,
                NotificationCenter,
                EventDispatcherMock.Object,
                LoggerMock.Object,
                ErrorHandlerMock.Object,
                null,
                new ForwardingEventProcessor(EventDispatcherMock.Object, NotificationCenter,
                    LoggerMock.Object, ErrorHandlerMock.Object),
                null);

            // Mocking objects.
            NotificationCallbackMock.Setup(nc => nc.TestDecisionCallback(It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<UserAttributes>(), It.IsAny<Dictionary<string, object>>()));
            optimizely.NotificationCenter.AddNotification(
                NotificationCenter.NotificationType.Decision,
                NotificationCallbackMock.Object.TestDecisionCallback);

            OptimizelyUserContext optimizelyUserContext =
                optimizely.CreateUserContext(TestUserId, userAttributes);
            optimizelyUserContext.Decide(featureKey);
            NotificationCallbackMock.Verify(nc => nc.TestDecisionCallback(
                DecisionNotificationTypes.FLAG, TestUserId, userAttributes,
                It.Is<Dictionary<string, object>>(info => TestData.CompareObjects(info,
                    new Dictionary<string, object>
                    {
                        {
                            "flagKey", featureKey
                        },
                        {
                            "enabled", true
                        },
                        {
                            "variables", variables.ToDictionary()
                        },
                        {
                            "variationKey", variation.Key
                        },
                        {
                            "ruleKey", ruleKey
                        },
                        {
                            "reasons", new OptimizelyDecideOption[0]
                        },
                        {
                            "decisionEventDispatched", true
                        },
                    }))), Times.Once);
            EventDispatcherMock.Verify(dispatcher => dispatcher.DispatchEvent(It.IsAny<LogEvent>()),
                Times.Once);
        }

        [Test]
        public void TestChangeAttributeDoesNotEffectValues()
        {
            string userId = "testUserId";
            UserAttributes attribute = new UserAttributes
            {
                {
                    "device_type", "iPhone"
                },
                {
                    "location", "San Francisco"
                },
            };
            OptimizelyUserContext optlyUserContext =
                Optimizely.CreateUserContext(userId, attribute);
            Assert.AreEqual(TestUserId, optlyUserContext.GetUserId());
            Assert.AreEqual(Optimizely, optlyUserContext.GetOptimizely());
            Assert.AreEqual(attribute, optlyUserContext.GetAttributes());

            attribute = new UserAttributes
            {
                {
                    "device_type", "iPhone"
                },
                {
                    "level", "low"
                },
                {
                    "location", "San Francisco"
                },
            };
            Assert.AreEqual("testUserId", optlyUserContext.GetUserId());
            Assert.AreEqual(Optimizely, optlyUserContext.GetOptimizely());
            Assert.AreNotEqual(attribute, optlyUserContext.GetAttributes());
        }

        #endregion Test UserContext

        #region Test Validate

        [Test]
        public void TestInvalidInstanceLogMessages()
        {
            string datafile = "{\"name\":\"optimizely\"}";
            Optimizely optimizely = new Optimizely(datafile, null, LoggerMock.Object);

            Assert.IsNull(optimizely.GetVariation(string.Empty, string.Empty));
            Assert.IsNull(optimizely.Activate(string.Empty, string.Empty));
            optimizely.Track(string.Empty, string.Empty);
            Assert.IsFalse(optimizely.IsFeatureEnabled(string.Empty, string.Empty));
            Assert.AreEqual(optimizely.GetEnabledFeatures(string.Empty).Count, 0);
            Assert.IsNull(
                optimizely.GetFeatureVariableBoolean(string.Empty, string.Empty, string.Empty));
            Assert.IsNull(
                optimizely.GetFeatureVariableString(string.Empty, string.Empty, string.Empty));
            Assert.IsNull(
                optimizely.GetFeatureVariableDouble(string.Empty, string.Empty, string.Empty));
            Assert.IsNull(
                optimizely.GetFeatureVariableInteger(string.Empty, string.Empty, string.Empty));
            Assert.IsNull(
                optimizely.GetFeatureVariableJSON(string.Empty, string.Empty, string.Empty));

            LoggerMock.Verify(
                log => log.Log(LogLevel.ERROR, "Provided 'datafile' has invalid schema."),
                Times.Once);
            LoggerMock.Verify(
                log => log.Log(LogLevel.ERROR,
                    "Datafile has invalid format. Failing 'GetVariation'."), Times.Once);
            LoggerMock.Verify(
                log => log.Log(LogLevel.ERROR, "Datafile has invalid format. Failing 'Activate'."),
                Times.Once);
            LoggerMock.Verify(
                log => log.Log(LogLevel.ERROR, "Datafile has invalid format. Failing 'Track'."),
                Times.Once);
            LoggerMock.Verify(
                log => log.Log(LogLevel.ERROR,
                    "Datafile has invalid format. Failing 'IsFeatureEnabled'."), Times.Once);
            LoggerMock.Verify(
                log => log.Log(LogLevel.ERROR,
                    "Datafile has invalid format. Failing 'GetEnabledFeatures'."), Times.Once);
            LoggerMock.Verify(
                log => log.Log(LogLevel.ERROR,
                    "Datafile has invalid format. Failing 'GetFeatureVariableBoolean'."),
                Times.Once);
            LoggerMock.Verify(
                log => log.Log(LogLevel.ERROR,
                    "Datafile has invalid format. Failing 'GetFeatureVariableString'."),
                Times.Once);
            LoggerMock.Verify(
                log => log.Log(LogLevel.ERROR,
                    "Datafile has invalid format. Failing 'GetFeatureVariableDouble'."),
                Times.Once);
            LoggerMock.Verify(
                log => log.Log(LogLevel.ERROR,
                    "Datafile has invalid format. Failing 'GetFeatureVariableInteger'."),
                Times.Once);
            LoggerMock.Verify(
                log => log.Log(LogLevel.ERROR,
                    "Datafile has invalid format. Failing 'GetFeatureVariableJSON'."), Times.Once);
        }

        [Test]
        public void TestValidateInputsInvalidFileJsonValidationNotSkipped()
        {
            string datafile = "{\"name\":\"optimizely\"}";
            Optimizely optimizely = new Optimizely(datafile);
            Assert.IsFalse(optimizely.IsValid);
        }

        [Test]
        public void TestValidateInputsInvalidFileJsonValidationSkipped()
        {
            string datafile = "{\"name\":\"optimizely\"}";
            Optimizely optimizely =
                new Optimizely(datafile, null, null, null, skipJsonValidation: true);
            Assert.IsFalse(optimizely.IsValid);
        }

        [Test]
        public void TestErrorHandlingWithNullDatafile()
        {
            Optimizely optimizelyNullDatafile = new Optimizely(null, null, LoggerMock.Object,
                ErrorHandlerMock.Object, null, true);
            LoggerMock.Verify(l => l.Log(LogLevel.ERROR, "Unable to parse null datafile."),
                Times.Once);
            ErrorHandlerMock.Verify(
                e => e.HandleError(It.Is<ConfigParseException>(ex =>
                    ex.Message == "Unable to parse null datafile.")), Times.Once);
        }

        [Test]
        public void TestErrorHandlingWithEmptyDatafile()
        {
            Optimizely optimizelyEmptyDatafile = new Optimizely("", null, LoggerMock.Object,
                ErrorHandlerMock.Object, null, true);
            LoggerMock.Verify(l => l.Log(LogLevel.ERROR, "Unable to parse empty datafile."),
                Times.Once);
            ErrorHandlerMock.Verify(
                e => e.HandleError(It.Is<ConfigParseException>(ex =>
                    ex.Message == "Unable to parse empty datafile.")), Times.Once);
        }

        [Test]
        public void TestErrorHandlingWithUnsupportedConfigVersion()
        {
            Optimizely optimizelyUnsupportedVersion = new Optimizely(
                TestData.UnsupportedVersionDatafile,
                null, LoggerMock.Object, ErrorHandlerMock.Object, null, true);
            LoggerMock.Verify(
                l => l.Log(LogLevel.ERROR,
                    $"This version of the C# SDK does not support the given datafile version: 5"),
                Times.Once);
            ErrorHandlerMock.Verify(
                e => e.HandleError(It.Is<ConfigParseException>(ex =>
                    ex.Message ==
                    $"This version of the C# SDK does not support the given datafile version: 5")),
                Times.Once);
        }

        [Test]
        public void TestValidatePreconditionsUserNotInForcedVariationInExperiment()
        {
            UserAttributes attributes = new UserAttributes
            {
                {
                    "device_type", "iPhone"
                },
                {
                    "location", "San Francisco"
                },
            };

            Variation variation =
                Optimizely.GetVariation("test_experiment", "test_user", attributes);

            LoggerMock.Verify(
                l => l.Log(LogLevel.DEBUG,
                    "User \"test_user\" is not in the forced variation map."), Times.Once);
            LoggerMock.Verify(
                l => l.Log(LogLevel.DEBUG,
                    "Assigned bucket [3037] to user [test_user] with bucketing ID [test_user]."),
                Times.Once);
            LoggerMock.Verify(
                l => l.Log(LogLevel.INFO,
                    "User [test_user] is in variation [control] of experiment [test_experiment]."),
                Times.Once);
            LoggerMock.Verify(
                l => l.Log(LogLevel.INFO,
                    "This decision will not be saved since the UserProfileService is null."),
                Times.Once);

            Assert.IsTrue(TestData.CompareObjects(VariationWithKeyControl, variation));
        }

        [Test]
        public void TestActivateInvalidOptimizelyObject()
        {
            Optimizely optly = new Optimizely("Random datafile", null, LoggerMock.Object);
            LoggerMock.Verify(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<string>()), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.ERROR, "Provided 'datafile' has invalid schema."),
                Times.Once);
        }

        //attributes can not be invalid beacuse now it is strongly typed.
        /* [TestMethod]
        public void TestActivateInvalidAttributes()
        {
            var attributes = new UserAttribute
            {
                {"abc", "43" }
            };

            var result = Optimizely.Activate("test_experiment", TestUserId, attributes);
            LoggerMock.Verify(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<string>()), Times.Once);
            //LoggerMock.Verify(l => l.Log(LogLevel.ERROR, "Provided attributes are in an invalid format."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, string.Format("Not activating user {0}.", TestUserId)), Times.Once);
            ErrorHandlerMock.Verify(e => e.HandleError(It.IsAny<InvalidAttributeException>()), Times.Once);

            Assert.IsNull(result);
        } */

        [Test]
        public void TestActivateUserInNoVariation()
        {
            PrivateObject optly = Helper.CreatePrivateOptimizely();

            object result = optly.Invoke("Activate", "test_experiment", "not_in_variation_user",
                OptimizelyHelper.UserAttributes);

            LoggerMock.Verify(
                l => l.Log(LogLevel.DEBUG,
                    "Assigned bucket [8495] to user [not_in_variation_user] with bucketing ID [not_in_variation_user]."),
                Times.Once);
            LoggerMock.Verify(
                l => l.Log(LogLevel.INFO, "User [not_in_variation_user] is in no variation."),
                Times.Once);
            LoggerMock.Verify(
                l => l.Log(LogLevel.INFO, "Not activating user not_in_variation_user."),
                Times.Once);

            Assert.IsNull(result);
        }

        [Test]
        public void TestActivateNoAudienceNoAttributes()
        {
            Dictionary<string, object> parameters = new Dictionary<string, object>
            {
                {
                    "param1", "val1"
                },
                {
                    "param2", "val2"
                },
            };

            PrivateObject optly = Helper.CreatePrivateOptimizely();
            optly.SetFieldOrProperty("EventProcessor", EventProcessorMock.Object);

            Variation variation =
                (Variation)optly.Invoke("Activate", "group_experiment_1", "user_1", null);

            EventProcessorMock.Verify(ep => ep.Process(It.IsAny<ImpressionEvent>()), Times.Once);

            LoggerMock.Verify(
                l => l.Log(LogLevel.DEBUG,
                    "Assigned bucket [1922] to user [user_1] with bucketing ID [user_1]."),
                Times.Once);
            LoggerMock.Verify(
                l => l.Log(LogLevel.INFO,
                    "User [user_1] is in experiment [group_experiment_1] of group [7722400015]."),
                Times.Once);
            LoggerMock.Verify(
                l => l.Log(LogLevel.DEBUG,
                    "Assigned bucket [9525] to user [user_1] with bucketing ID [user_1]."),
                Times.Once);
            LoggerMock.Verify(
                l => l.Log(LogLevel.INFO,
                    "User [user_1] is in variation [group_exp_1_var_2] of experiment [group_experiment_1]."),
                Times.Once);
            LoggerMock.Verify(
                l => l.Log(LogLevel.INFO,
                    "Activating user user_1 in experiment group_experiment_1."), Times.Once);

            Assert.IsTrue(TestData.CompareObjects(GroupVariation, variation));
        }

        #endregion Test Validate

        #region Test Activate

        [Test]
        public void TestActivateAudienceNoAttributes()
        {
            PrivateObject optly = Helper.CreatePrivateOptimizely();

            object variationkey = optly.Invoke("Activate", "test_experiment", "test_user", null);

            LoggerMock.Verify(
                l => l.Log(LogLevel.INFO,
                    "User \"test_user\" does not meet conditions to be in experiment \"test_experiment\"."),
                Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "Not activating user test_user."),
                Times.Once);

            Assert.IsNull(variationkey);
        }

        [Test]
        public void TestActivateWithAttributes()
        {
            PrivateObject optly = Helper.CreatePrivateOptimizely();
            optly.SetFieldOrProperty("EventProcessor", EventProcessorMock.Object);

            Variation variation = (Variation)optly.Invoke("Activate", "test_experiment",
                "test_user",
                OptimizelyHelper.UserAttributes);

            EventProcessorMock.Verify(ep => ep.Process(It.IsAny<ImpressionEvent>()), Times.Once);

            LoggerMock.Verify(
                l => l.Log(LogLevel.DEBUG,
                    "User \"test_user\" is not in the forced variation map."), Times.Once);
            LoggerMock.Verify(
                l => l.Log(LogLevel.DEBUG,
                    "Assigned bucket [3037] to user [test_user] with bucketing ID [test_user]."),
                Times.Once);
            LoggerMock.Verify(
                l => l.Log(LogLevel.INFO,
                    "User [test_user] is in variation [control] of experiment [test_experiment]."),
                Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO,
                "This decision will not be saved since the UserProfileService is null."));
            LoggerMock.Verify(
                l => l.Log(LogLevel.INFO,
                    "Activating user test_user in experiment test_experiment."), Times.Once);

            Assert.IsTrue(TestData.CompareObjects(VariationWithKeyControl, variation));
        }

        [Test]
        public void TestActivateWithNullAttributes()
        {
            PrivateObject optly = Helper.CreatePrivateOptimizely();
            optly.SetFieldOrProperty("EventProcessor", EventProcessorMock.Object);

            Variation variation = (Variation)optly.Invoke("Activate", "test_experiment",
                "test_user",
                OptimizelyHelper.NullUserAttributes);

            EventProcessorMock.Verify(ep => ep.Process(It.IsAny<ImpressionEvent>()), Times.Once);

            //"User "test_user" is not in the forced variation map."
            LoggerMock.Verify(
                l => l.Log(LogLevel.DEBUG,
                    "User \"test_user\" is not in the forced variation map."), Times.Once);
            LoggerMock.Verify(
                l => l.Log(LogLevel.DEBUG,
                    "Assigned bucket [3037] to user [test_user] with bucketing ID [test_user]."),
                Times.Once);
            LoggerMock.Verify(
                l => l.Log(LogLevel.INFO,
                    "User [test_user] is in variation [control] of experiment [test_experiment]."),
                Times.Once);
            LoggerMock.Verify(
                l => l.Log(LogLevel.INFO,
                    "Activating user test_user in experiment test_experiment."), Times.Once);

            Assert.IsTrue(TestData.CompareObjects(VariationWithKeyControl, variation));
        }

        [Test]
        public void TestActivateExperimentNotRunning()
        {
            PrivateObject optly = Helper.CreatePrivateOptimizely();

            object variationkey = optly.Invoke("Activate", "paused_experiment", "test_user", null);

            LoggerMock.Verify(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<string>()),
                Times.Exactly(2));
            LoggerMock.Verify(
                l => l.Log(LogLevel.INFO, "Experiment \"paused_experiment\" is not running."),
                Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "Not activating user test_user."),
                Times.Once);

            Assert.IsNull(variationkey);
        }

        [Test]
        public void TestActivateWithTypedAttributes()
        {
            UserAttributes userAttributes = new UserAttributes
            {
                {
                    "device_type", "iPhone"
                },
                {
                    "location", "San Francisco"
                },
                {
                    "boolean_key", true
                },
                {
                    "integer_key", 15
                },
                {
                    "double_key", 3.14
                },
            };

            PrivateObject optly = Helper.CreatePrivateOptimizely();
            optly.SetFieldOrProperty("EventProcessor", EventProcessorMock.Object);

            Variation variation = (Variation)optly.Invoke("Activate", "test_experiment",
                "test_user",
                userAttributes);

            EventProcessorMock.Verify(ep => ep.Process(It.IsAny<ImpressionEvent>()), Times.Once);

            LoggerMock.Verify(
                l => l.Log(LogLevel.DEBUG,
                    "Assigned bucket [3037] to user [test_user] with bucketing ID [test_user]."),
                Times.Once);
            LoggerMock.Verify(
                l => l.Log(LogLevel.INFO,
                    "User [test_user] is in variation [control] of experiment [test_experiment]."),
                Times.Once);
            LoggerMock.Verify(
                l => l.Log(LogLevel.INFO,
                    "Activating user test_user in experiment test_experiment."), Times.Once);

            Assert.IsTrue(TestData.CompareObjects(VariationWithKeyControl, variation));
        }

        #endregion Test Activate

        #region Test GetVariation

        [Test]
        public void TestGetVariationInvalidOptimizelyObject()
        {
            Optimizely optly = new Optimizely("Random datafile", null, LoggerMock.Object);
            Variation variationkey = optly.Activate("some_experiment", "some_user");
            LoggerMock.Verify(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<string>()),
                Times.Exactly(2));
            LoggerMock.Verify(
                l => l.Log(LogLevel.ERROR, "Datafile has invalid format. Failing 'Activate'."),
                Times.Once);
            //Assert.IsNull(variationkey);
        }

        //attributes can not be invalid beacuse now it is strongly typed.
        /* [TestMethod]
        public void TestGetVariationInvalidAttributes()
        {
            var attributes = new UserAttribute
            {
                {"abc", "43" }
            };
            var result = Optimizely.getVariation("test_experiment", TestUserId, attributes);

            //LoggerMock.Verify(l => l.Log(LogLevel.ERROR, "Provided attributes are in an invalid format."), Times.Once);
            ErrorHandlerMock.Verify(e => e.HandleError(It.IsAny<InvalidAttributeException>()), Times.Once);

            Assert.IsNull(result);
        } */

        [Test]
        public void TestGetVariationAudienceMatch()
        {
            UserAttributes attributes = new UserAttributes
            {
                {
                    "device_type", "iPhone"
                },
                {
                    "location", "San Francisco"
                },
            };

            Variation variation =
                Optimizely.GetVariation("test_experiment", "test_user", attributes);

            LoggerMock.Verify(
                l => l.Log(LogLevel.DEBUG,
                    "Assigned bucket [3037] to user [test_user] with bucketing ID [test_user]."),
                Times.Once);
            LoggerMock.Verify(
                l => l.Log(LogLevel.INFO,
                    "User [test_user] is in variation [control] of experiment [test_experiment]."),
                Times.Once);
            LoggerMock.Verify(
                l => l.Log(LogLevel.INFO,
                    "This decision will not be saved since the UserProfileService is null."),
                Times.Once);

            Assert.IsTrue(TestData.CompareObjects(VariationWithKeyControl, variation));
        }

        [Test]
        public void TestGetVariationAudienceNoMatch()
        {
            Variation variation = Optimizely.Activate("test_experiment", "test_user");
            LoggerMock.Verify(
                l => l.Log(LogLevel.INFO,
                    "User \"test_user\" does not meet conditions to be in experiment \"test_experiment\"."),
                Times.Once);
            Assert.IsNull(variation);
        }

        [Test]
        public void TestGetVariationExperimentNotRunning()
        {
            Variation variation = Optimizely.Activate("paused_experiment", "test_user");
            LoggerMock.Verify(
                l => l.Log(LogLevel.INFO, "Experiment \"paused_experiment\" is not running."),
                Times.Once);
            Assert.IsNull(variation);
        }

        [Test]
        public void TestTrackInvalidOptimizelyObject()
        {
            Optimizely optly = new Optimizely("Random datafile", null, LoggerMock.Object);
            optly.Track("some_event", "some_user");
            LoggerMock.Verify(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<string>()),
                Times.Exactly(2));
            LoggerMock.Verify(
                l => l.Log(LogLevel.ERROR, "Datafile has invalid format. Failing 'Track'."),
                Times.Once);
        }

        #endregion Test GetVariation

        #region Test Track

        [Test]
        public void TestTrackInvalidAttributes()
        {
            UserAttributes attributes = new UserAttributes
            {
                {
                    "abc", "43"
                },
            };

            Optimizely.Track("purchase", TestUserId, attributes);

            LoggerMock.Verify(
                l => l.Log(LogLevel.ERROR, @"Attribute key ""abc"" is not in datafile."),
                Times.Once);
        }

        [Test]
        public void TestTrackNoAttributesNoEventValue()
        {
            PrivateObject optly = Helper.CreatePrivateOptimizely();
            optly.SetFieldOrProperty("EventProcessor", EventProcessorMock.Object);

            optly.Invoke("Track", "purchase", "test_user", null, null);
            EventProcessorMock.Verify(processor => processor.Process(It.IsAny<ConversionEvent>()),
                Times.Once);

            LoggerMock.Verify(
                l => l.Log(LogLevel.INFO, "Tracking event purchase for user test_user."),
                Times.Once);
        }

        [Test]
        public void TestTrackWithAttributesNoEventValue()
        {
            PrivateObject optly = Helper.CreatePrivateOptimizely();
            optly.SetFieldOrProperty("EventProcessor", EventProcessorMock.Object);

            optly.Invoke("Track", "purchase", "test_user", OptimizelyHelper.UserAttributes, null);
            EventProcessorMock.Verify(ep => ep.Process(It.IsAny<ConversionEvent>()), Times.Once);

            LoggerMock.Verify(
                l => l.Log(LogLevel.INFO, "Tracking event purchase for user test_user."),
                Times.Once);
        }

        [Test]
        public void TestTrackUnknownEventKey()
        {
            Optimizely.Track("unknown_event", "test_user");

            LoggerMock.Verify(
                l => l.Log(LogLevel.ERROR, "Event key \"unknown_event\" is not in datafile."),
                Times.Once);
            LoggerMock.Verify(
                l => l.Log(LogLevel.INFO, "Not tracking user test_user for event unknown_event."),
                Times.Once);
        }

        [Test]
        public void TestTrackNoAttributesWithEventValue()
        {
            PrivateObject optly = Helper.CreatePrivateOptimizely();
            optly.SetFieldOrProperty("EventProcessor", EventProcessorMock.Object);

            optly.Invoke("Track", "purchase", "test_user", null, new EventTags
            {
                {
                    "revenue", 42
                },
            });
            EventProcessorMock.Verify(ep => ep.Process(It.IsAny<ConversionEvent>()), Times.Once);

            LoggerMock.Verify(
                l => l.Log(LogLevel.INFO, "Tracking event purchase for user test_user."),
                Times.Once);
        }

        [Test]
        public void TestTrackWithAttributesWithEventValue()
        {
            UserAttributes attributes = new UserAttributes
            {
                {
                    "device_type", "iPhone"
                },
                {
                    "company", "Optimizely"
                },
            };

            PrivateObject optly = Helper.CreatePrivateOptimizely();
            optly.SetFieldOrProperty("EventProcessor", EventProcessorMock.Object);

            optly.Invoke("Track", "purchase", "test_user", attributes, new EventTags
            {
                {
                    "revenue", 42
                },
            });

            EventProcessorMock.Verify(ep => ep.Process(It.IsAny<ConversionEvent>()), Times.Once);

            LoggerMock.Verify(
                l => l.Log(LogLevel.INFO, "Tracking event purchase for user test_user."),
                Times.Once);
        }

        [Test]
        public void TestTrackWithNullAttributesWithNullEventValue()
        {
            PrivateObject optly = Helper.CreatePrivateOptimizely();
            optly.SetFieldOrProperty("EventProcessor", EventProcessorMock.Object);

            optly.Invoke("Track", "purchase", "test_user", OptimizelyHelper.NullUserAttributes,
                new EventTags
                {
                    {
                        "revenue", 42
                    },
                    {
                        "wont_send_null", null
                    },
                });

            EventProcessorMock.Verify(ep => ep.Process(It.IsAny<ConversionEvent>()), Times.Once);

            LoggerMock.Verify(l => l.Log(LogLevel.ERROR,
                "[EventTags] Null value for key wont_send_null removed and will not be sent to results."));
            LoggerMock.Verify(l =>
                l.Log(LogLevel.INFO, "Tracking event purchase for user test_user."));
        }

        #endregion Test Track

        #region Test Invalid Dispatch

        [Test]
        public void TestInvalidDispatchImpressionEvent()
        {
            PrivateObject optly = Helper.CreatePrivateOptimizely();
            optly.SetFieldOrProperty("EventDispatcher", new InvalidEventDispatcher());

            Variation variation = (Variation)optly.Invoke("Activate", "test_experiment",
                "test_user",
                OptimizelyHelper.UserAttributes);

            EventDispatcherMock.Verify(dispatcher => dispatcher.DispatchEvent(It.IsAny<LogEvent>()),
                Times.Once);

            LoggerMock.Verify(
                l => l.Log(LogLevel.DEBUG,
                    "Assigned bucket [3037] to user [test_user] with bucketing ID [test_user]."),
                Times.Once);
            LoggerMock.Verify(
                l => l.Log(LogLevel.INFO,
                    "User [test_user] is in variation [control] of experiment [test_experiment]."),
                Times.Once);

            LoggerMock.Verify(
                l => l.Log(LogLevel.INFO,
                    "Activating user test_user in experiment test_experiment."), Times.Once);
            // Need to see how error handler can be verified.
            LoggerMock.Verify(l => l.Log(LogLevel.ERROR, It.IsAny<string>()), Times.Once);

            Assert.IsTrue(TestData.CompareObjects(VariationWithKeyControl, variation));
        }

        [Test]
        public void TestInvalidDispatchConversionEvent()
        {
            PrivateObject optly = Helper.CreatePrivateOptimizely();
            optly.SetFieldOrProperty("EventDispatcher", new InvalidEventDispatcher());

            optly.Invoke("Track", "purchase", "test_user", null, null);

            EventDispatcherMock.Verify(dispatcher => dispatcher.DispatchEvent(It.IsAny<LogEvent>()),
                Times.Once);

            LoggerMock.Verify(
                l => l.Log(LogLevel.INFO, "Tracking event purchase for user test_user."),
                Times.Once);
        }

        #endregion Test Invalid Dispatch

        #region Test Misc

        /* Start 1 */

        public void TestTrackNoAttributesWithInvalidEventValue()
        {
            PrivateObject optly = Helper.CreatePrivateOptimizely();
            optly.SetFieldOrProperty("EventDispatcher", new ValidEventDispatcher());

            optly.Invoke("Track", "purchase", "test_user", null, new Dictionary<string, object>
            {
                {
                    "revenue", 4200
                },
            });
        }

        public void TestTrackNoAttributesWithDeprecatedEventValue()
        {
            /* Note: This case is not applicable, C# only accepts what the datatype we provide.
             * In this case, int value can't be casted implicitly into Dictionary */

            PrivateObject optly = Helper.CreatePrivateOptimizely();
            optly.SetFieldOrProperty("EventDispatcher", new ValidEventDispatcher());
            optly.Invoke("Track", "purchase", "test_user", null, new Dictionary<string, object>
            {
                {
                    "revenue", 42
                },
            });
        }

        [Test]
        public void TestForcedVariationPreceedsWhitelistedVariation()
        {
            Optimizely optimizely = new Optimizely(TestData.Datafile, EventDispatcherMock.Object,
                LoggerMock.Object, ErrorHandlerMock.Object);
            ProjectConfig projectConfig = DatafileProjectConfig.Create(TestData.Datafile,
                LoggerMock.Object,
                ErrorHandlerMock.Object);
            Variation expectedVariation1 = projectConfig.GetVariationFromKey("etag3", "vtag5");
            Variation expectedVariation2 = projectConfig.GetVariationFromKey("etag3", "vtag6");

            //Check whitelisted experiment
            Variation variation = optimizely.GetVariation("etag3", "testUser1");
            Assert.IsTrue(TestData.CompareObjects(expectedVariation1, variation));

            //Set forced variation
            Assert.IsTrue(optimizely.SetForcedVariation("etag3", "testUser1", "vtag6"));
            variation = optimizely.GetVariation("etag3", "testUser1");

            // verify forced variation preceeds whitelisted variation
            Assert.IsTrue(TestData.CompareObjects(expectedVariation2, variation));

            // remove forced variation and verify whitelisted should be returned.
            Assert.IsTrue(optimizely.SetForcedVariation("etag3", "testUser1", null));
            variation = optimizely.GetVariation("etag3", "testUser1");

            Assert.IsTrue(TestData.CompareObjects(expectedVariation1, variation));

            LoggerMock.Verify(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<string>()),
                Times.Exactly(7));
            LoggerMock.Verify(
                l => l.Log(LogLevel.DEBUG,
                    "User \"testUser1\" is not in the forced variation map."), Times.Once);
            LoggerMock.Verify(
                l => l.Log(LogLevel.INFO, "User \"testUser1\" is forced in variation \"vtag5\"."),
                Times.Exactly(2));
            LoggerMock.Verify(
                l => l.Log(LogLevel.DEBUG,
                    "Set variation \"281\" for experiment \"224\" and user \"testUser1\" in the forced variation map."),
                Times.Once);
            LoggerMock.Verify(
                l => l.Log(LogLevel.DEBUG,
                    "Variation \"vtag6\" is mapped to experiment \"etag3\" and user \"testUser1\" in the forced variation map"),
                Times.Once);
            LoggerMock.Verify(
                l => l.Log(LogLevel.DEBUG,
                    "Variation mapped to experiment \"etag3\" has been removed for user \"testUser1\"."),
                Times.Once);
            LoggerMock.Verify(
                l => l.Log(LogLevel.DEBUG,
                    "No experiment \"etag3\" mapped to user \"testUser1\" in the forced variation map."),
                Times.Once);
        }

        [Test]
        public void TestForcedVariationPreceedsUserProfile()
        {
            Mock<UserProfileService> userProfileServiceMock = new Mock<UserProfileService>();
            string experimentKey = "etag1";
            string userId = "testUser3";
            string variationKey = "vtag2";
            string fbVariationKey = "vtag1";

            UserProfile userProfile = new UserProfile(userId,
                new Dictionary<string, Bucketing.Decision>
                {
                    {
                        experimentKey, new Bucketing.Decision(variationKey)
                    },
                });

            userProfileServiceMock.Setup(_ => _.Lookup(userId)).Returns(userProfile.ToMap());

            Optimizely optimizely = new Optimizely(TestData.Datafile, EventDispatcherMock.Object,
                LoggerMock.Object, ErrorHandlerMock.Object, userProfileServiceMock.Object);
            ProjectConfig projectConfig = DatafileProjectConfig.Create(TestData.Datafile,
                LoggerMock.Object,
                ErrorHandlerMock.Object);
            Variation expectedFbVariation =
                projectConfig.GetVariationFromKey(experimentKey, fbVariationKey);
            Variation expectedVariation =
                projectConfig.GetVariationFromKey(experimentKey, variationKey);

            Variation variationUserProfile = optimizely.GetVariation(experimentKey, userId);
            Assert.IsTrue(TestData.CompareObjects(expectedVariation, variationUserProfile));

            //assign same user with different variation, forced variation have higher priority
            Assert.IsTrue(optimizely.SetForcedVariation(experimentKey, userId, fbVariationKey));

            Variation variation2 = optimizely.GetVariation(experimentKey, userId);
            Assert.IsTrue(TestData.CompareObjects(expectedFbVariation, variation2));

            //remove forced variation and re-check userprofile
            Assert.IsTrue(optimizely.SetForcedVariation(experimentKey, userId, null));

            variationUserProfile = optimizely.GetVariation(experimentKey, userId);
            Assert.IsTrue(TestData.CompareObjects(expectedVariation, variationUserProfile));

            LoggerMock.Verify(
                l => l.Log(LogLevel.DEBUG,
                    "User \"testUser3\" is not in the forced variation map."), Times.Once);
            LoggerMock.Verify(
                l => l.Log(LogLevel.INFO,
                    "No previously activated variation of experiment \"etag1\" for user \"testUser3\" found in user profile."),
                Times.Exactly(2));
            LoggerMock.Verify(
                l => l.Log(LogLevel.DEBUG,
                    "Assigned bucket [4969] to user [testUser3] with bucketing ID [testUser3]."),
                Times.Exactly(2));
            LoggerMock.Verify(
                l => l.Log(LogLevel.INFO,
                    "User [testUser3] is in variation [vtag2] of experiment [etag1]."),
                Times.Exactly(2));
            LoggerMock.Verify(
                l => l.Log(LogLevel.INFO,
                    "Saved variation \"277\" of experiment \"223\" for user \"testUser3\"."),
                Times.Exactly(2));
            LoggerMock.Verify(
                l => l.Log(LogLevel.DEBUG,
                    "Set variation \"276\" for experiment \"223\" and user \"testUser3\" in the forced variation map."),
                Times.Once);
            LoggerMock.Verify(
                l => l.Log(LogLevel.DEBUG,
                    "Variation mapped to experiment \"etag1\" has been removed for user \"testUser3\"."),
                Times.Once);
            LoggerMock.Verify(
                l => l.Log(LogLevel.DEBUG,
                    "No experiment \"etag1\" mapped to user \"testUser3\" in the forced variation map."),
                Times.Once);
            LoggerMock.Verify(
                l => l.Log(LogLevel.DEBUG,
                    "User \"testUser3\" is not in the forced variation map."), Times.Once);
            LoggerMock.Verify(
                l => l.Log(LogLevel.DEBUG,
                    "User \"testUser3\" is not in the forced variation map."), Times.Once);
        }

        // check that a null variation key clears the forced variation
        [Test]
        public void TestSetForcedVariationNullVariation()
        {
            string expectedForcedVariationKey = "variation";
            string experimentKey = "test_experiment";

            UserAttributes userAttributes = new UserAttributes
            {
                {
                    "device_type", "iPhone"
                },
                {
                    "location", "San Francisco"
                },
            };

            Optimizely.Activate(experimentKey, TestUserId, userAttributes);

            // set variation
            Assert.IsTrue(
                Optimizely.SetForcedVariation(experimentKey, TestUserId,
                    expectedForcedVariationKey), "Set forced variation to variation failed.");

            Variation actualForcedVariation =
                Optimizely.GetVariation(experimentKey, TestUserId, userAttributes);
            Assert.IsTrue(TestData.CompareObjects(VariationWithKeyVariation, actualForcedVariation),
                string.Format(@"Forced variation key should be variation, but got ""{0}"".",
                    actualForcedVariation?.Key));

            // clear variation and check that the user gets bucketed normally
            Assert.IsTrue(Optimizely.SetForcedVariation(experimentKey, TestUserId, null),
                "Clear forced variation failed.");

            Variation actualVariation =
                Optimizely.GetVariation("test_experiment", "test_user", userAttributes);
            Assert.IsTrue(TestData.CompareObjects(VariationWithKeyControl, actualVariation),
                string.Format(@"Variation key should be control, but got ""{0}"".",
                    actualVariation?.Key));
        }

        // check that the forced variation is set correctly
        [Test]
        public void TestSetForcedVariation()
        {
            string experimentKey = "test_experiment";
            string expectedForcedVariationKey = "variation";

            UserAttributes userAttributes = new UserAttributes
            {
                {
                    "device_type", "iPhone"
                },
                {
                    "location", "San Francisco"
                },
            };

            Optimizely.Activate(experimentKey, TestUserId, userAttributes);

            // test invalid experiment -. normal bucketing should occur
            Assert.IsFalse(
                Optimizely.SetForcedVariation("bad_experiment", TestUserId, "bad_control"),
                "Set variation to 'variation' should have failed  because of invalid experiment.");

            Variation variation =
                Optimizely.GetVariation(experimentKey, TestUserId, userAttributes);
            Assert.IsTrue(TestData.CompareObjects(VariationWithKeyControl, variation));

            // test invalid variation -. normal bucketing should occur
            Assert.IsFalse(
                Optimizely.SetForcedVariation("test_experiment", TestUserId, "bad_variation"),
                "Set variation to 'bad_variation' should have failed.");

            variation = Optimizely.GetVariation("test_experiment", "test_user", userAttributes);
            Assert.IsTrue(TestData.CompareObjects(VariationWithKeyControl, variation));

            // test valid variation -. the user should be bucketed to the specified forced variation
            Assert.IsTrue(
                Optimizely.SetForcedVariation(experimentKey, TestUserId,
                    expectedForcedVariationKey), "Set variation to 'variation' failed.");

            Variation actualForcedVariation =
                Optimizely.GetVariation(experimentKey, TestUserId, userAttributes);
            Assert.IsTrue(TestData.CompareObjects(VariationWithKeyVariation,
                actualForcedVariation));

            // make sure another setForcedVariation call sets a new forced variation correctly
            Assert.IsTrue(
                Optimizely.SetForcedVariation(experimentKey, "test_user2",
                    expectedForcedVariationKey), "Set variation to 'variation' failed.");
            actualForcedVariation =
                Optimizely.GetVariation(experimentKey, "test_user2", userAttributes);

            Assert.IsTrue(TestData.CompareObjects(VariationWithKeyVariation,
                actualForcedVariation));
        }

        [Test]
        public void TestSetForcedVariationWithNullAndEmptyUserId()
        {
            Assert.False(Optimizely.SetForcedVariation("test_experiment", null, "variation"));
            Assert.True(Optimizely.SetForcedVariation("test_experiment", "", "variation"));
        }

        [Test]
        public void TestSetForcedVariationWithInvalidExperimentKey()
        {
            string userId = "test_user";
            string variation = "variation";

            Assert.False(Optimizely.SetForcedVariation("test_experiment_not_in_datafile", userId,
                variation));
            Assert.False(Optimizely.SetForcedVariation("", userId, variation));
            Assert.False(Optimizely.SetForcedVariation(null, userId, variation));
        }

        [Test]
        public void TestSetForcedVariationWithInvalidVariationKey()
        {
            string userId = "test_user";
            string experimentKey = "test_experiment";

            Assert.False(Optimizely.SetForcedVariation(experimentKey, userId,
                "variation_not_in_datafile"));
            Assert.False(Optimizely.SetForcedVariation(experimentKey, userId, ""));
        }

        // check that the get forced variation is correct.
        [Test]
        public void TestGetForcedVariation()
        {
            string experimentKey = "test_experiment";
            Variation expectedForcedVariation = new Variation
            {
                Key = "variation",
                Id = "7721010009",
            };
            Variation expectedForcedVariation2 = new Variation
            {
                Key = "variation",
                Id = "7721010509",
            };
            UserAttributes userAttributes = new UserAttributes
            {
                {
                    "device_type", "iPhone"
                },
                {
                    "location", "San Francisco"
                },
            };

            Optimizely.Activate(experimentKey, TestUserId, userAttributes);

            Assert.IsTrue(
                Optimizely.SetForcedVariation(experimentKey, TestUserId,
                    expectedForcedVariation.Key), "Set variation to 'variation' failed.");

            // call getForcedVariation with valid experiment key and valid user ID
            Variation actualForcedVariation =
                Optimizely.GetForcedVariation("test_experiment", TestUserId);
            Assert.IsTrue(TestData.CompareObjects(expectedForcedVariation, actualForcedVariation));

            // call getForcedVariation with invalid experiment and valid userID
            actualForcedVariation = Optimizely.GetForcedVariation("invalid_experiment", TestUserId);
            Assert.Null(actualForcedVariation);

            // call getForcedVariation with valid experiment and invalid userID
            actualForcedVariation =
                Optimizely.GetForcedVariation("test_experiment", "invalid_user");

            Assert.Null(actualForcedVariation);

            // call getForcedVariation with an experiment that"s not running
            Assert.IsTrue(
                Optimizely.SetForcedVariation("paused_experiment", "test_user2", "variation"),
                "Set variation to 'variation' failed.");

            actualForcedVariation =
                Optimizely.GetForcedVariation("paused_experiment", "test_user2");

            Assert.IsTrue(TestData.CompareObjects(expectedForcedVariation2, actualForcedVariation));

            // confirm that the second setForcedVariation call did not invalidate the first call to that method
            actualForcedVariation = Optimizely.GetForcedVariation("test_experiment", TestUserId);

            Assert.IsTrue(TestData.CompareObjects(expectedForcedVariation, actualForcedVariation));
        }

        [Test]
        public void TestGetForcedVariationWithInvalidUserID()
        {
            string experimentKey = "test_experiment";
            Optimizely.SetForcedVariation(experimentKey, "test_user", "test_variation");

            Assert.Null(Optimizely.GetForcedVariation(experimentKey, null));
            Assert.Null(Optimizely.GetForcedVariation(experimentKey, "invalid_user"));
        }

        [Test]
        public void TestGetForcedVariationWithInvalidExperimentKey()
        {
            string userId = "test_user";
            string experimentKey = "test_experiment";
            Optimizely.SetForcedVariation(experimentKey, userId, "test_variation");

            Assert.Null(Optimizely.GetForcedVariation("test_experiment", userId));
            Assert.Null(Optimizely.GetForcedVariation("", userId));
            Assert.Null(Optimizely.GetForcedVariation(null, userId));
        }

        [Test]
        public void TestGetVariationAudienceMatchAfterSetForcedVariation()
        {
            string userId = "test_user";
            string experimentKey = "test_experiment";
            string experimentId = "7716830082";
            string variationKey = "control";
            string variationId = "7722370027";

            UserAttributes attributes = new UserAttributes
            {
                {
                    "device_type", "iPhone"
                },
                {
                    "location", "San Francisco"
                },
            };

            Assert.True(Optimizely.SetForcedVariation(experimentKey, userId, variationKey),
                "Set variation for paused experiment should have failed.");
            Variation variation = Optimizely.GetVariation(experimentKey, userId, attributes);

            LoggerMock.Verify(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<string>()),
                Times.Exactly(2));
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG,
                string.Format(
                    @"Set variation ""{0}"" for experiment ""{1}"" and user ""{2}"" in the forced variation map.",
                    variationId, experimentId, userId)));
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG,
                string.Format(
                    @"Variation ""{0}"" is mapped to experiment ""{1}"" and user ""{2}"" in the forced variation map",
                    variationKey, experimentKey, userId)));

            Assert.IsTrue(TestData.CompareObjects(VariationWithKeyControl, variation));
        }

        [Test]
        public void TestGetVariationExperimentNotRunningAfterSetForceVariation()
        {
            string userId = "test_user";
            string experimentKey = "paused_experiment";
            string experimentId = "7716830585";
            string variationKey = "control";
            string variationId = "7722370427";

            UserAttributes attributes = new UserAttributes
            {
                {
                    "device_type", "iPhone"
                },
                {
                    "location", "San Francisco"
                },
            };

            Assert.True(Optimizely.SetForcedVariation(experimentKey, userId, variationKey),
                "Set variation for paused experiment should have failed.");
            Variation variation = Optimizely.GetVariation(experimentKey, userId, attributes);

            LoggerMock.Verify(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<string>()),
                Times.Exactly(2));
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG,
                string.Format(
                    @"Set variation ""{0}"" for experiment ""{1}"" and user ""{2}"" in the forced variation map.",
                    variationId, experimentId, userId)));
            LoggerMock.Verify(l => l.Log(LogLevel.INFO,
                string.Format("Experiment \"{0}\" is not running.", experimentKey)));

            Assert.Null(variation);
        }

        [Test]
        public void TestGetVariationWhitelistedUserAfterSetForcedVariation()
        {
            string userId = "user1";
            string experimentKey = "test_experiment";
            string experimentId = "7716830082";
            string variationKey = "variation";
            string variationId = "7721010009";

            Assert.True(Optimizely.SetForcedVariation(experimentKey, userId, variationKey),
                "Set variation for paused experiment should have passed.");
            Variation variation = Optimizely.GetVariation(experimentKey, userId);

            LoggerMock.Verify(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<string>()),
                Times.Exactly(2));
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG,
                string.Format(
                    @"Set variation ""{0}"" for experiment ""{1}"" and user ""{2}"" in the forced variation map.",
                    variationId, experimentId, userId)));
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG,
                string.Format(
                    @"Variation ""{0}"" is mapped to experiment ""{1}"" and user ""{2}"" in the forced variation map",
                    variationKey, experimentKey, userId)));

            Assert.IsTrue(TestData.CompareObjects(VariationWithKeyVariation, variation));
        }

        [Test]
        public void TestActivateNoAudienceNoAttributesAfterSetForcedVariation()
        {
            string userId = "test_user";
            string experimentKey = "test_experiment";
            string experimentId = "7716830082";
            string variationKey = "control";
            string variationId = "7722370027";
            Dictionary<string, object> parameters = new Dictionary<string, object>
            {
                {
                    "param1", "val1"
                },
                {
                    "param2", "val2"
                },
            };

            Experiment experiment = new Experiment();
            experiment.Key = "group_experiment_1";

            PrivateObject optly = Helper.CreatePrivateOptimizely();
            optly.SetFieldOrProperty("ProjectConfigManager", ConfigManager);

            // Set forced variation
            Assert.True(
                (bool)optly.Invoke("SetForcedVariation", experimentKey, userId, variationKey),
                "Set variation for paused experiment should have failed.");

            // Activate
            Variation variation =
                (Variation)optly.Invoke("Activate", "group_experiment_1", "user_1", null);

            EventDispatcherMock.Verify(dispatcher => dispatcher.DispatchEvent(It.IsAny<LogEvent>()),
                Times.Once);

            LoggerMock.Verify(
                l => l.Log(LogLevel.DEBUG,
                    string.Format(
                        @"Set variation ""{0}"" for experiment ""{1}"" and user ""{2}"" in the forced variation map.",
                        variationId, experimentId, userId)), Times.Once);
            LoggerMock.Verify(
                l => l.Log(LogLevel.DEBUG, "User \"user_1\" is not in the forced variation map."),
                Times.Once);
            LoggerMock.Verify(
                l => l.Log(LogLevel.DEBUG,
                    "Assigned bucket [1922] to user [user_1] with bucketing ID [user_1]."),
                Times.Once);
            LoggerMock.Verify(
                l => l.Log(LogLevel.INFO,
                    "User [user_1] is in experiment [group_experiment_1] of group [7722400015]."),
                Times.Once);
            LoggerMock.Verify(
                l => l.Log(LogLevel.DEBUG,
                    "Assigned bucket [9525] to user [user_1] with bucketing ID [user_1]."),
                Times.Once);
            LoggerMock.Verify(
                l => l.Log(LogLevel.INFO,
                    "User [user_1] is in variation [group_exp_1_var_2] of experiment [group_experiment_1]."),
                Times.Once);
            LoggerMock.Verify(
                l => l.Log(LogLevel.INFO,
                    "This decision will not be saved since the UserProfileService is null."),
                Times.Once);
            LoggerMock.Verify(
                l => l.Log(LogLevel.INFO,
                    "Activating user user_1 in experiment group_experiment_1."), Times.Once);

            Assert.IsTrue(TestData.CompareObjects(GroupVariation, variation));
        }

        [Test]
        public void TestTrackNoAttributesNoEventValueAfterSetForcedVariation()
        {
            string userId = "test_user";
            string experimentKey = "test_experiment";
            string experimentId = "7716830082";
            string variationKey = "control";
            string variationId = "7722370027";
            Dictionary<string, object> parameters = new Dictionary<string, object>
            {
                {
                    "param1", "val1"
                },
            };

            PrivateObject optly = Helper.CreatePrivateOptimizely();

            // Set forced variation
            Assert.True(
                (bool)optly.Invoke("SetForcedVariation", experimentKey, userId, variationKey),
                "Set variation for paused experiment should have failed.");

            // Track
            optly.Invoke("Track", "purchase", "test_user", null, null);
            EventDispatcherMock.Verify(dispatcher => dispatcher.DispatchEvent(It.IsAny<LogEvent>()),
                Times.Once);

            LoggerMock.Verify(
                l => l.Log(LogLevel.DEBUG,
                    string.Format(
                        @"Set variation ""{0}"" for experiment ""{1}"" and user ""{2}"" in the forced variation map.",
                        variationId, experimentId, userId)), Times.Once);
            LoggerMock.Verify(
                l => l.Log(LogLevel.INFO, "Tracking event purchase for user test_user."),
                Times.Once);
        }

        [Test]
        public void TestGetVariationBucketingIdAttribute()
        {
            string testBucketingIdControl =
                "testBucketingIdControl!"; // generates bucketing number 3741
            string testBucketingIdVariation = "123456789"; // generates bucketing number 4567
            string userId = "test_user";
            string experimentKey = "test_experiment";

            UserAttributes userAttributes = new UserAttributes
            {
                {
                    "device_type", "iPhone"
                },
                {
                    "company", "Optimizely"
                },
                {
                    "location", "San Francisco"
                },
            };

            UserAttributes userAttributesWithBucketingId = new UserAttributes
            {
                {
                    "device_type", "iPhone"
                },
                {
                    "company", "Optimizely"
                },
                {
                    "location", "San Francisco"
                },
                {
                    ControlAttributes.BUCKETING_ID_ATTRIBUTE, testBucketingIdVariation
                },
            };

            // confirm that a valid variation is bucketed without the bucketing ID
            Variation actualVariation =
                Optimizely.GetVariation(experimentKey, userId, userAttributes);
            Assert.IsTrue(TestData.CompareObjects(VariationWithKeyControl, actualVariation),
                string.Format("Invalid variation key \"{0}\" for getVariation.",
                    actualVariation?.Key));

            // confirm that invalid audience returns null
            actualVariation = Optimizely.GetVariation(experimentKey, userId);
            Assert.Null(actualVariation,
                string.Format(
                    "Invalid variation key \"{0}\" for getVariation with bucketing ID \"{1}\".",
                    actualVariation?.Key, testBucketingIdControl));

            // confirm that a valid variation is bucketed with the bucketing ID
            actualVariation =
                Optimizely.GetVariation(experimentKey, userId, userAttributesWithBucketingId);
            Assert.IsTrue(TestData.CompareObjects(VariationWithKeyVariation, actualVariation),
                string.Format(
                    "Invalid variation key \"{0}\" for getVariation with bucketing ID \"{1}\".",
                    actualVariation?.Key, testBucketingIdVariation));

            // confirm that invalid experiment with the bucketing ID returns null
            actualVariation = Optimizely.GetVariation("invalidExperimentKey", userId,
                userAttributesWithBucketingId);
            Assert.Null(actualVariation,
                string.Format(
                    "Invalid variation key \"{0}\" for getVariation with bucketing ID \"{1}\".",
                    actualVariation?.Key, testBucketingIdControl));
        }

        #endregion Test Misc

        #region Test GetFeatureVariable<Type> methods

        [Test]
        public void TestGetFeatureVariableBooleanReturnsCorrectValue()
        {
            string featureKey = "featureKey";
            string variableKeyTrue = "varTrue";
            string variableKeyFalse = "varFalse";
            string variableKeyNonBoolean = "varNonBoolean";
            string variableKeyNull = "varNull";
            string featureVariableType = "boolean";

            OptimizelyMock.Setup(om => om.GetFeatureVariableValueForType<bool?>(It.IsAny<string>(),
                    variableKeyTrue, It.IsAny<string>(),
                    It.IsAny<UserAttributes>(), featureVariableType))
                .Returns(true);
            Assert.AreEqual(true,
                OptimizelyMock.Object.GetFeatureVariableBoolean(featureKey, variableKeyTrue,
                    TestUserId, null));

            OptimizelyMock.Setup(om => om.GetFeatureVariableValueForType<bool?>(It.IsAny<string>(),
                    variableKeyFalse, It.IsAny<string>(),
                    It.IsAny<UserAttributes>(), featureVariableType))
                .Returns(false);
            Assert.AreEqual(false,
                OptimizelyMock.Object.GetFeatureVariableBoolean(featureKey, variableKeyFalse,
                    TestUserId, null));

            OptimizelyMock.Setup(om => om.GetFeatureVariableValueForType<string>(It.IsAny<string>(),
                    variableKeyNonBoolean, It.IsAny<string>(),
                    It.IsAny<UserAttributes>(), featureVariableType))
                .Returns("non_boolean_value");
            Assert.Null(OptimizelyMock.Object.GetFeatureVariableBoolean(featureKey,
                variableKeyNonBoolean, TestUserId, null));

            OptimizelyMock.Setup(om => om.GetFeatureVariableValueForType<bool?>(It.IsAny<string>(),
                    variableKeyNull, It.IsAny<string>(),
                    It.IsAny<UserAttributes>(), featureVariableType))
                .Returns<bool?>(null);
            Assert.Null(OptimizelyMock.Object.GetFeatureVariableBoolean(featureKey, variableKeyNull,
                TestUserId, null));
        }

        [Test]
        public void TestGetFeatureVariableDoubleFRCulture()
        {
            SetCulture("en-US");
            FallbackProjectConfigManager fallbackConfigManager =
                new FallbackProjectConfigManager(Config);

            Optimizely optimizely = new Optimizely(fallbackConfigManager);

            double? doubleValue = optimizely.GetFeatureVariableDouble(
                "double_single_variable_feature",
                "double_variable", "testUser1");

            Assert.AreEqual(doubleValue, 14.99);

            SetCulture("fr-FR");
            double? doubleValueFR =
                optimizely.GetFeatureVariableDouble("double_single_variable_feature",
                    "double_variable", "testUser1");
            Assert.AreEqual(doubleValueFR, 14.99);
        }

        [Test]
        public void TestGetFeatureVariableIntegerFRCulture()
        {
            SetCulture("en-US");
            FallbackProjectConfigManager fallbackConfigManager =
                new FallbackProjectConfigManager(Config);

            Optimizely optimizely = new Optimizely(fallbackConfigManager);

            int? integerValue =
                optimizely.GetFeatureVariableInteger("integer_single_variable_feature",
                    "integer_variable", "testUser1");

            Assert.AreEqual(integerValue, 13);

            SetCulture("fr-FR");
            int? integerValueFR =
                optimizely.GetFeatureVariableInteger("integer_single_variable_feature",
                    "integer_variable", "testUser1");
            Assert.AreEqual(integerValueFR, 13);
        }

        [Test]
        public void TestGetFeatureVariableBooleanFRCulture()
        {
            SetCulture("en-US");
            FallbackProjectConfigManager fallbackConfigManager =
                new FallbackProjectConfigManager(Config);

            Optimizely optimizely = new Optimizely(fallbackConfigManager);

            bool? booleanValue =
                optimizely.GetFeatureVariableBoolean("boolean_single_variable_feature",
                    "boolean_variable", "testUser1");

            Assert.AreEqual(booleanValue, false);

            SetCulture("fr-FR");
            bool? booleanValueFR =
                optimizely.GetFeatureVariableBoolean("boolean_single_variable_feature",
                    "boolean_variable", "testUser1");
            Assert.AreEqual(booleanValueFR, false);
        }

        [Test]
        public void TestGetFeatureVariableStringFRCulture()
        {
            SetCulture("en-US");
            FallbackProjectConfigManager fallbackConfigManager =
                new FallbackProjectConfigManager(Config);

            Optimizely optimizely = new Optimizely(fallbackConfigManager);

            string stringValue = optimizely.GetFeatureVariableString(
                "string_single_variable_feature",
                "string_variable", "testUser1");

            Assert.AreEqual(stringValue, "cta_1");

            SetCulture("fr-FR");
            string stringValueFR =
                optimizely.GetFeatureVariableString("string_single_variable_feature",
                    "string_variable", "testUser1");
            Assert.AreEqual(stringValueFR, "cta_1");
        }

        [Test]
        public void TestGetFeatureVariableJSONFRCulture()
        {
            FallbackProjectConfigManager fallbackConfigManager =
                new FallbackProjectConfigManager(Config);

            Dictionary<string, object> expectedDict = new Dictionary<string, object>()
            {
                {
                    "int_var", 1
                },
                {
                    "boolean_key", false
                },
            };

            SetCulture("en-US");
            Optimizely optimizely = new Optimizely(fallbackConfigManager);

            OptimizelyJSON optimizelyJsonValue =
                optimizely.GetFeatureVariableJSON("string_single_variable_feature", "json_var",
                    "testUser1");

            Assert.IsTrue(TestData.CompareObjects(optimizelyJsonValue.ToDictionary(),
                expectedDict));
            Assert.AreEqual(optimizelyJsonValue.GetValue<long>("int_var"), 1);
            Assert.AreEqual(optimizelyJsonValue.GetValue<bool>("boolean_key"), false);
            Assert.IsTrue(TestData.CompareObjects(optimizelyJsonValue.GetValue<object>(""),
                expectedDict));

            SetCulture("fr-FR");
            OptimizelyJSON optimizelyJsonValueFR =
                optimizely.GetFeatureVariableJSON("string_single_variable_feature", "json_var",
                    "testUser1");

            Assert.IsTrue(TestData.CompareObjects(optimizelyJsonValue.ToDictionary(),
                expectedDict));
            Assert.AreEqual(optimizelyJsonValueFR.GetValue<long>("int_var"), 1);
            Assert.AreEqual(optimizelyJsonValueFR.GetValue<bool>("boolean_key"), false);
            Assert.IsTrue(TestData.CompareObjects(optimizelyJsonValue.GetValue<object>(""),
                expectedDict));
        }

        [Test]
        public void TestGetFeatureVariableDoubleReturnsCorrectValue()
        {
            string featureKey = "featureKey";
            string variableKeyDouble = "varDouble";
            string variableKeyInt = "varInt";
            string variableKeyNonDouble = "varNonDouble";
            string variableKeyNull = "varNull";
            string featureVariableType = "double";

            OptimizelyMock.Setup(om => om.GetFeatureVariableValueForType<double?>(
                    It.IsAny<string>(), variableKeyDouble, It.IsAny<string>(),
                    It.IsAny<UserAttributes>(), featureVariableType))
                .Returns(100.54);
            Assert.AreEqual(100.54,
                OptimizelyMock.Object.GetFeatureVariableDouble(featureKey, variableKeyDouble,
                    TestUserId, null));

            OptimizelyMock.Setup(om => om.GetFeatureVariableValueForType<double?>(
                    It.IsAny<string>(), variableKeyInt, It.IsAny<string>(),
                    It.IsAny<UserAttributes>(), featureVariableType))
                .Returns(100);
            Assert.AreEqual(100,
                OptimizelyMock.Object.GetFeatureVariableDouble(featureKey, variableKeyInt,
                    TestUserId, null));

            OptimizelyMock.Setup(om => om.GetFeatureVariableValueForType<string>(It.IsAny<string>(),
                    variableKeyNonDouble, It.IsAny<string>(),
                    It.IsAny<UserAttributes>(), featureVariableType))
                .Returns("non_double_value");
            Assert.Null(OptimizelyMock.Object.GetFeatureVariableDouble(featureKey,
                variableKeyNonDouble, TestUserId, null));

            OptimizelyMock.Setup(om => om.GetFeatureVariableValueForType<double?>(
                    It.IsAny<string>(), variableKeyNull, It.IsAny<string>(),
                    It.IsAny<UserAttributes>(), featureVariableType))
                .Returns<double?>(null);
            Assert.Null(OptimizelyMock.Object.GetFeatureVariableDouble(featureKey, variableKeyNull,
                TestUserId, null));
        }

        [Test]
        public void TestGetFeatureVariableIntegerReturnsCorrectValue()
        {
            string featureKey = "featureKey";
            string variableKeyInt = "varInt";
            string variableNonInt = "varNonInt";
            string variableKeyNull = "varNull";
            string featureVariableType = "integer";

            OptimizelyMock.Setup(om => om.GetFeatureVariableValueForType<int?>(It.IsAny<string>(),
                    variableKeyInt, It.IsAny<string>(),
                    It.IsAny<UserAttributes>(), featureVariableType))
                .Returns(100);
            Assert.AreEqual(100,
                OptimizelyMock.Object.GetFeatureVariableInteger(featureKey, variableKeyInt,
                    TestUserId, null));

            OptimizelyMock.Setup(om => om.GetFeatureVariableValueForType<string>(It.IsAny<string>(),
                    variableNonInt, It.IsAny<string>(),
                    It.IsAny<UserAttributes>(), featureVariableType))
                .Returns("non_integer_value");
            Assert.Null(OptimizelyMock.Object.GetFeatureVariableInteger(featureKey, variableNonInt,
                TestUserId, null));

            OptimizelyMock.Setup(om => om.GetFeatureVariableValueForType<int?>(It.IsAny<string>(),
                    variableKeyNull, It.IsAny<string>(),
                    It.IsAny<UserAttributes>(), featureVariableType))
                .Returns<string>(null);
            Assert.Null(OptimizelyMock.Object.GetFeatureVariableInteger(featureKey, variableKeyNull,
                TestUserId, null));
        }

        [Test]
        public void TestGetFeatureVariableStringReturnsCorrectValue()
        {
            string featureKey = "featureKey";
            string variableKeyString = "varString1";
            string variableKeyIntString = "varString2";
            string variableKeyNull = "varNull";
            string featureVariableType = "string";

            OptimizelyMock.Setup(om => om.GetFeatureVariableValueForType<string>(It.IsAny<string>(),
                    variableKeyString, It.IsAny<string>(),
                    It.IsAny<UserAttributes>(), featureVariableType))
                .Returns("Test String");
            Assert.AreEqual("Test String",
                OptimizelyMock.Object.GetFeatureVariableString(featureKey, variableKeyString,
                    TestUserId, null));

            OptimizelyMock.Setup(om => om.GetFeatureVariableValueForType<string>(It.IsAny<string>(),
                    variableKeyIntString, It.IsAny<string>(),
                    It.IsAny<UserAttributes>(), featureVariableType))
                .Returns("123");
            Assert.AreEqual("123",
                OptimizelyMock.Object.GetFeatureVariableString(featureKey, variableKeyIntString,
                    TestUserId, null));

            OptimizelyMock.Setup(om => om.GetFeatureVariableValueForType<string>(It.IsAny<string>(),
                    variableKeyNull, It.IsAny<string>(),
                    It.IsAny<UserAttributes>(), featureVariableType))
                .Returns<string>(null);
            Assert.Null(OptimizelyMock.Object.GetFeatureVariableString(featureKey, variableKeyNull,
                TestUserId, null));
        }

        [Test]
        public void TestGetFeatureVariableJSONReturnsCorrectValue()
        {
            string featureKey = "featureKey";
            string variableKeyString = "varJSONString1";
            string variableKeyIntString = "varJSONString2";
            string variableKeyDouble = "varJSONDouble";
            string variableKeyNull = "varNull";
            string featureVariableType = "json";

            OptimizelyMock.Setup(om => om.GetFeatureVariableValueForType<OptimizelyJSON>(
                    It.IsAny<string>(), variableKeyString, It.IsAny<string>(),
                    It.IsAny<UserAttributes>(), featureVariableType))
                .Returns(new OptimizelyJSON("{\"string\": \"Test String\"}",
                    ErrorHandlerMock.Object,
                    LoggerMock.Object));
            Assert.AreEqual("Test String",
                OptimizelyMock.Object
                    .GetFeatureVariableJSON(featureKey, variableKeyString, TestUserId, null)
                    .GetValue<string>("string"));

            OptimizelyMock.Setup(om => om.GetFeatureVariableValueForType<OptimizelyJSON>(
                    It.IsAny<string>(), variableKeyIntString, It.IsAny<string>(),
                    It.IsAny<UserAttributes>(), featureVariableType))
                .Returns(new OptimizelyJSON("{ \"integer\": 123 }", ErrorHandlerMock.Object,
                    LoggerMock.Object));
            Assert.AreEqual(123,
                OptimizelyMock.Object
                    .GetFeatureVariableJSON(featureKey, variableKeyIntString, TestUserId, null)
                    .GetValue<long>("integer"));

            OptimizelyMock.Setup(om => om.GetFeatureVariableValueForType<OptimizelyJSON>(
                    It.IsAny<string>(), variableKeyDouble, It.IsAny<string>(),
                    It.IsAny<UserAttributes>(), featureVariableType))
                .Returns(new OptimizelyJSON("{ \"double\": 123.28 }", ErrorHandlerMock.Object,
                    LoggerMock.Object));
            Assert.AreEqual(123.28,
                OptimizelyMock.Object
                    .GetFeatureVariableJSON(featureKey, variableKeyDouble, TestUserId, null)
                    .GetValue<double>("double"));

            OptimizelyMock.Setup(om => om.GetFeatureVariableValueForType<OptimizelyJSON>(
                    It.IsAny<string>(), variableKeyNull, It.IsAny<string>(),
                    It.IsAny<UserAttributes>(), featureVariableType))
                .Returns<OptimizelyJSON>(null);
            Assert.Null(OptimizelyMock.Object.GetFeatureVariableJSON(featureKey, variableKeyNull,
                TestUserId, null));
        }

        [Test]
        public void TestGetFeatureVariableJSONReturnsCorrectValueWhenInitializedUsingDictionary()
        {
            string featureKey = "featureKey";
            string variableKeyString = "varJSONString1";
            string variableKeyIntString = "varJSONString2";
            string variableKeyDouble = "varJSONDouble";
            string variableKeyBoolean = "varJSONBoolean";
            string variableKeyNull = "varNull";
            string featureVariableType = "json";

            Dictionary<string, object> expectedStringDict = new Dictionary<string, object>()
            {
                {
                    "string", "Test String"
                },
            };
            Dictionary<string, object> expectedIntegerDict = new Dictionary<string, object>()
            {
                {
                    "integer", 123
                },
            };
            Dictionary<string, object> expectedDoubleDict = new Dictionary<string, object>()
            {
                {
                    "double", 123.28
                },
            };
            Dictionary<string, object> expectedBooleanDict = new Dictionary<string, object>()
            {
                {
                    "boolean", true
                },
            };

            OptimizelyMock.Setup(om => om.GetFeatureVariableValueForType<OptimizelyJSON>(
                    It.IsAny<string>(), variableKeyString, It.IsAny<string>(),
                    It.IsAny<UserAttributes>(), featureVariableType))
                .Returns(new OptimizelyJSON(expectedStringDict, ErrorHandlerMock.Object,
                    LoggerMock.Object));
            Assert.IsTrue(TestData.CompareObjects(expectedStringDict,
                OptimizelyMock.Object
                    .GetFeatureVariableJSON(featureKey, variableKeyString, TestUserId, null)
                    .ToDictionary()));
            Assert.AreEqual("Test String",
                OptimizelyMock.Object
                    .GetFeatureVariableJSON(featureKey, variableKeyString, TestUserId, null)
                    .GetValue<string>("string"));

            OptimizelyMock.Setup(om => om.GetFeatureVariableValueForType<OptimizelyJSON>(
                    It.IsAny<string>(), variableKeyIntString, It.IsAny<string>(),
                    It.IsAny<UserAttributes>(), featureVariableType))
                .Returns(new OptimizelyJSON(expectedIntegerDict, ErrorHandlerMock.Object,
                    LoggerMock.Object));
            Assert.IsTrue(TestData.CompareObjects(expectedIntegerDict,
                OptimizelyMock.Object
                    .GetFeatureVariableJSON(featureKey, variableKeyIntString, TestUserId, null)
                    .ToDictionary()));
            Assert.AreEqual(123,
                OptimizelyMock.Object
                    .GetFeatureVariableJSON(featureKey, variableKeyIntString, TestUserId, null)
                    .GetValue<long>("integer"));

            OptimizelyMock.Setup(om => om.GetFeatureVariableValueForType<OptimizelyJSON>(
                    It.IsAny<string>(), variableKeyDouble, It.IsAny<string>(),
                    It.IsAny<UserAttributes>(), featureVariableType))
                .Returns(new OptimizelyJSON(expectedDoubleDict, ErrorHandlerMock.Object,
                    LoggerMock.Object));
            Assert.IsTrue(TestData.CompareObjects(expectedDoubleDict,
                OptimizelyMock.Object
                    .GetFeatureVariableJSON(featureKey, variableKeyDouble, TestUserId, null)
                    .ToDictionary()));
            Assert.AreEqual(123.28,
                OptimizelyMock.Object
                    .GetFeatureVariableJSON(featureKey, variableKeyDouble, TestUserId, null)
                    .GetValue<double>("double"));

            OptimizelyMock.Setup(om => om.GetFeatureVariableValueForType<OptimizelyJSON>(
                    It.IsAny<string>(), variableKeyBoolean, It.IsAny<string>(),
                    It.IsAny<UserAttributes>(), featureVariableType))
                .Returns(new OptimizelyJSON(expectedBooleanDict, ErrorHandlerMock.Object,
                    LoggerMock.Object));
            Assert.IsTrue(TestData.CompareObjects(expectedBooleanDict,
                OptimizelyMock.Object
                    .GetFeatureVariableJSON(featureKey, variableKeyBoolean, TestUserId, null)
                    .ToDictionary()));
            Assert.AreEqual(true,
                OptimizelyMock.Object
                    .GetFeatureVariableJSON(featureKey, variableKeyBoolean, TestUserId, null)
                    .GetValue<bool>("boolean"));

            OptimizelyMock.Setup(om => om.GetFeatureVariableValueForType<OptimizelyJSON>(
                    It.IsAny<string>(), variableKeyNull, It.IsAny<string>(),
                    It.IsAny<UserAttributes>(), featureVariableType))
                .Returns<OptimizelyJSON>(null);
            Assert.Null(OptimizelyMock.Object.GetFeatureVariableJSON(featureKey, variableKeyNull,
                TestUserId, null));
        }

        #region Feature Toggle Tests

        [Test]
        public void
            TestGetFeatureVariableDoubleReturnsRightValueWhenUserBuckedIntoFeatureExperimentAndVariationIsToggleOn()
        {
            string featureKey = "double_single_variable_feature";
            FeatureFlag featureFlag = Config.GetFeatureFlagFromKey(featureKey);
            string variableKey = "double_variable";
            double expectedValue = 42.42;
            Experiment experiment = Config.GetExperimentFromKey("test_experiment_double_feature");
            Variation variation =
                Config.GetVariationFromKey("test_experiment_double_feature", "control");
            Result<FeatureDecision> decision = Result<FeatureDecision>.NewResult(
                new FeatureDecision(experiment, variation,
                    FeatureDecision.DECISION_SOURCE_FEATURE_TEST), DecisionReasons);

            DecisionServiceMock
                .Setup(ds =>
                    ds.GetVariationForFeature(featureFlag, It.IsAny<OptimizelyUserContext>(),
                        Config))
                .Returns(decision);

            PrivateObject optly = Helper.CreatePrivateOptimizely();
            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);
            optly.SetFieldOrProperty("ProjectConfigManager", ConfigManager);

            double variableValue = (double)optly.Invoke("GetFeatureVariableDouble", featureKey,
                variableKey, TestUserId, null);
            Assert.AreEqual(expectedValue, variableValue);

            LoggerMock.Verify(l => l.Log(LogLevel.INFO,
                $@"Got variable value ""{variableValue}"" for variable ""{variableKey
                }"" of feature flag ""{featureKey}""."));
        }

        [Test]
        public void
            TestGetFeatureVariableIntegerReturnsRightValueWhenUserBuckedIntoFeatureExperimentAndVariationIsToggleOn()
        {
            string featureKey = "integer_single_variable_feature";
            FeatureFlag featureFlag = Config.GetFeatureFlagFromKey(featureKey);
            string variableKey = "integer_variable";
            int expectedValue = 13;
            Experiment experiment = Config.GetExperimentFromKey("test_experiment_integer_feature");
            Variation variation =
                Config.GetVariationFromKey("test_experiment_integer_feature", "variation");
            Result<FeatureDecision> decision = Result<FeatureDecision>.NewResult(
                new FeatureDecision(experiment, variation,
                    FeatureDecision.DECISION_SOURCE_FEATURE_TEST), DecisionReasons);
            UserAttributes userAttributes = new UserAttributes
            {
                {
                    "device_type", "iPhone"
                },
                {
                    "company", "Optimizely"
                },
                {
                    "location", "San Francisco"
                },
            };

            DecisionServiceMock
                .Setup(ds =>
                    ds.GetVariationForFeature(featureFlag, It.IsAny<OptimizelyUserContext>(),
                        Config))
                .Returns(decision);

            PrivateObject optly = Helper.CreatePrivateOptimizely();
            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);
            optly.SetFieldOrProperty("ProjectConfigManager", ConfigManager);

            int variableValue = (int)optly.Invoke("GetFeatureVariableInteger", featureKey,
                variableKey, TestUserId, userAttributes);
            Assert.AreEqual(expectedValue, variableValue);

            LoggerMock.Verify(l => l.Log(LogLevel.INFO,
                $@"Got variable value ""{variableValue}"" for variable ""{variableKey
                }"" of feature flag ""{featureKey}""."));
        }

        [Test]
        public void
            TestGetFeatureVariableDoubleReturnsDefaultValueWhenUserBuckedIntoFeatureExperimentAndVariationIsToggleOff()
        {
            string featureKey = "double_single_variable_feature";
            FeatureFlag featureFlag = Config.GetFeatureFlagFromKey(featureKey);
            string variableKey = "double_variable";
            double expectedValue = 14.99;
            Experiment experiment = Config.GetExperimentFromKey("test_experiment_double_feature");
            Variation variation =
                Config.GetVariationFromKey("test_experiment_double_feature", "variation");
            Result<FeatureDecision> decision = Result<FeatureDecision>.NewResult(
                new FeatureDecision(experiment, variation,
                    FeatureDecision.DECISION_SOURCE_FEATURE_TEST), DecisionReasons);

            DecisionServiceMock
                .Setup(ds =>
                    ds.GetVariationForFeature(featureFlag, It.IsAny<OptimizelyUserContext>(),
                        Config))
                .Returns(decision);

            PrivateObject optly = Helper.CreatePrivateOptimizely();
            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);
            optly.SetFieldOrProperty("ProjectConfigManager", ConfigManager);

            double variableValue = (double)optly.Invoke("GetFeatureVariableDouble", featureKey,
                variableKey, TestUserId, null);
            Assert.AreEqual(expectedValue, variableValue);

            LoggerMock.Verify(l => l.Log(LogLevel.INFO,
                $@"Feature ""{featureKey}"" is not enabled for user {TestUserId
                }. Returning the default variable value ""{variableValue}""."));
        }

        [Test]
        public void
            TestGetFeatureVariableIntegerReturnsDefaultValueWhenUserBuckedIntoFeatureExperimentAndVariationIsToggleOff()
        {
            string featureKey = "integer_single_variable_feature";
            FeatureFlag featureFlag = Config.GetFeatureFlagFromKey(featureKey);
            string variableKey = "integer_variable";
            int expectedValue = 7;
            Experiment experiment = Config.GetExperimentFromKey("test_experiment_integer_feature");
            Variation variation =
                Config.GetVariationFromKey("test_experiment_integer_feature", "control");
            Result<FeatureDecision> decision = Result<FeatureDecision>.NewResult(
                new FeatureDecision(experiment, variation,
                    FeatureDecision.DECISION_SOURCE_FEATURE_TEST), DecisionReasons);
            UserAttributes userAttributes = new UserAttributes
            {
                {
                    "device_type", "iPhone"
                },
                {
                    "company", "Optimizely"
                },
                {
                    "location", "San Francisco"
                },
            };

            DecisionServiceMock
                .Setup(ds =>
                    ds.GetVariationForFeature(featureFlag, It.IsAny<OptimizelyUserContext>(),
                        Config))
                .Returns(decision);

            PrivateObject optly = Helper.CreatePrivateOptimizely();
            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);
            optly.SetFieldOrProperty("ProjectConfigManager", ConfigManager);

            int variableValue = (int)optly.Invoke("GetFeatureVariableInteger", featureKey,
                variableKey, TestUserId, userAttributes);
            Assert.AreEqual(expectedValue, variableValue);

            LoggerMock.Verify(l => l.Log(LogLevel.INFO,
                $@"Feature ""{featureKey}"" is not enabled for user {TestUserId
                }. Returning the default variable value ""{variableValue}""."));
        }

        [Test]
        public void
            TestGetFeatureVariableBooleanReturnsRightValueWhenUserBuckedIntoRolloutAndVariationIsToggleOn()
        {
            string featureKey = "boolean_single_variable_feature";
            FeatureFlag featureFlag = Config.GetFeatureFlagFromKey(featureKey);
            string variableKey = "boolean_variable";
            bool expectedValue = true;
            Experiment experiment = Config.GetRolloutFromId("166660").Experiments[0];
            Variation variation = Config.GetVariationFromKey(experiment.Key, "177771");
            Result<FeatureDecision> decision = Result<FeatureDecision>.NewResult(
                new FeatureDecision(experiment, variation, FeatureDecision.DECISION_SOURCE_ROLLOUT),
                DecisionReasons);

            DecisionServiceMock
                .Setup(ds =>
                    ds.GetVariationForFeature(featureFlag, It.IsAny<OptimizelyUserContext>(),
                        Config))
                .Returns(decision);

            PrivateObject optly = Helper.CreatePrivateOptimizely();
            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);
            optly.SetFieldOrProperty("ProjectConfigManager", ConfigManager);
            bool variableValue = (bool)optly.Invoke("GetFeatureVariableBoolean", featureKey,
                variableKey, TestUserId, null);
            Assert.AreEqual(expectedValue, variableValue);

            LoggerMock.Verify(l => l.Log(LogLevel.INFO,
                $@"Got variable value ""true"" for variable ""{variableKey}"" of feature flag ""{
                    featureKey}""."));
        }

        [Test]
        public void
            TestGetFeatureVariableJSONReturnsRightValueWhenUserBucketIntoRolloutAndVariationIsToggleOn()
        {
            string featureKey = "string_single_variable_feature";
            FeatureFlag featureFlag = Config.GetFeatureFlagFromKey(featureKey);
            string variableKey = "json_var";
            string expectedStringValue = "cta_4";
            int expectedIntValue = 4;
            Experiment experiment = Config.GetRolloutFromId("166661").Experiments[0];
            Variation variation = Config.GetVariationFromKey(experiment.Key, "177775");
            Result<FeatureDecision> decision = Result<FeatureDecision>.NewResult(
                new FeatureDecision(experiment, variation, FeatureDecision.DECISION_SOURCE_ROLLOUT),
                DecisionReasons);
            UserAttributes userAttributes = new UserAttributes
            {
                {
                    "device_type", "iPhone"
                },
                {
                    "company", "Optimizely"
                },
                {
                    "location", "San Francisco"
                },
            };

            DecisionServiceMock
                .Setup(ds =>
                    ds.GetVariationForFeature(featureFlag, It.IsAny<OptimizelyUserContext>(),
                        Config))
                .Returns(decision);

            PrivateObject optly = Helper.CreatePrivateOptimizely();

            optly.SetFieldOrProperty("ProjectConfigManager", ConfigManager);
            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);

            OptimizelyJSON variableValue = (OptimizelyJSON)optly.Invoke("GetFeatureVariableJSON",
                featureKey,
                variableKey, TestUserId, userAttributes);
            Assert.AreEqual(expectedIntValue, variableValue.GetValue<long>("int_var"));
            Assert.AreEqual(expectedStringValue, variableValue.GetValue<string>("string_var"));

            LoggerMock.Verify(l => l.Log(LogLevel.INFO,
                $@"Got variable value ""{variableValue}"" for variable ""{variableKey
                }"" of feature flag ""{featureKey}""."));
        }

        [Test]
        public void
            TestGetFeatureVariableJSONReturnsRightValueWhenUserBucketIntoRolloutAndVariationIsToggleOnTypeIsJson()
        {
            string featureKey = "string_single_variable_feature";
            FeatureFlag featureFlag = Config.GetFeatureFlagFromKey(featureKey);
            string variableKey = "true_json_var";
            string expectedStringValue = "cta_5";
            int expectedIntValue = 5;
            Experiment experiment = Config.GetRolloutFromId("166661").Experiments[0];
            Variation variation = Config.GetVariationFromKey(experiment.Key, "177775");
            Result<FeatureDecision> decision = Result<FeatureDecision>.NewResult(
                new FeatureDecision(experiment, variation, FeatureDecision.DECISION_SOURCE_ROLLOUT),
                DecisionReasons);
            UserAttributes userAttributes = new UserAttributes
            {
                {
                    "device_type", "iPhone"
                },
                {
                    "company", "Optimizely"
                },
                {
                    "location", "San Francisco"
                },
            };

            DecisionServiceMock
                .Setup(ds =>
                    ds.GetVariationForFeature(featureFlag, It.IsAny<OptimizelyUserContext>(),
                        Config))
                .Returns(decision);

            PrivateObject optly = Helper.CreatePrivateOptimizely();

            optly.SetFieldOrProperty("ProjectConfigManager", ConfigManager);
            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);

            OptimizelyJSON variableValue = (OptimizelyJSON)optly.Invoke("GetFeatureVariableJSON",
                featureKey,
                variableKey, TestUserId, userAttributes);
            Assert.AreEqual(expectedIntValue, variableValue.GetValue<long>("int_var"));
            Assert.AreEqual(expectedStringValue, variableValue.GetValue<string>("string_var"));

            LoggerMock.Verify(l => l.Log(LogLevel.INFO,
                $@"Got variable value ""{variableValue}"" for variable ""{variableKey
                }"" of feature flag ""{featureKey}""."));
        }

        [Test]
        public void
            TestGetFeatureVariableStringReturnsRightValueWhenUserBuckedIntoRolloutAndVariationIsToggleOn()
        {
            string featureKey = "string_single_variable_feature";
            FeatureFlag featureFlag = Config.GetFeatureFlagFromKey(featureKey);
            string variableKey = "string_variable";
            string expectedValue = "cta_4";
            Experiment experiment = Config.GetRolloutFromId("166661").Experiments[0];
            Variation variation = Config.GetVariationFromKey(experiment.Key, "177775");
            Result<FeatureDecision> decision = Result<FeatureDecision>.NewResult(
                new FeatureDecision(experiment, variation, FeatureDecision.DECISION_SOURCE_ROLLOUT),
                DecisionReasons);
            UserAttributes userAttributes = new UserAttributes
            {
                {
                    "device_type", "iPhone"
                },
                {
                    "company", "Optimizely"
                },
                {
                    "location", "San Francisco"
                },
            };

            DecisionServiceMock
                .Setup(ds =>
                    ds.GetVariationForFeature(featureFlag, It.IsAny<OptimizelyUserContext>(),
                        Config))
                .Returns(decision);

            PrivateObject optly = Helper.CreatePrivateOptimizely();

            optly.SetFieldOrProperty("ProjectConfigManager", ConfigManager);
            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);

            string variableValue = (string)optly.Invoke("GetFeatureVariableString", featureKey,
                variableKey, TestUserId, userAttributes);
            Assert.AreEqual(expectedValue, variableValue);

            LoggerMock.Verify(l => l.Log(LogLevel.INFO,
                $@"Got variable value ""{variableValue}"" for variable ""{variableKey
                }"" of feature flag ""{featureKey}""."));
        }

        [Test]
        public void
            TestGetFeatureVariableBooleanReturnsDefaultValueWhenUserBuckedIntoRolloutAndVariationIsToggleOff()
        {
            string featureKey = "boolean_single_variable_feature";
            FeatureFlag featureFlag = Config.GetFeatureFlagFromKey(featureKey);
            string variableKey = "boolean_variable";
            bool expectedValue = true;
            Experiment experiment = Config.GetRolloutFromId("166660").Experiments[3];
            Variation variation = Config.GetVariationFromKey(experiment.Key, "177782");
            Result<FeatureDecision> decision = Result<FeatureDecision>.NewResult(
                new FeatureDecision(experiment, variation, FeatureDecision.DECISION_SOURCE_ROLLOUT),
                DecisionReasons);

            DecisionServiceMock
                .Setup(ds =>
                    ds.GetVariationForFeature(featureFlag, It.IsAny<OptimizelyUserContext>(),
                        Config))
                .Returns(decision);

            PrivateObject optly = Helper.CreatePrivateOptimizely();
            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);
            optly.SetFieldOrProperty("ProjectConfigManager", ConfigManager);
            bool variableValue = (bool)optly.Invoke("GetFeatureVariableBoolean", featureKey,
                variableKey, TestUserId, null);
            Assert.AreEqual(expectedValue, variableValue);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO,
                $@"Feature ""{featureKey}"" is not enabled for user {TestUserId
                }. Returning the default variable value ""true""."));
        }

        [Test]
        public void
            TestGetFeatureVariableStringReturnsDefaultValueWhenUserBuckedIntoRolloutAndVariationIsToggleOff()
        {
            string featureKey = "string_single_variable_feature";
            FeatureFlag featureFlag = Config.GetFeatureFlagFromKey(featureKey);
            string variableKey = "string_variable";
            string expectedValue = "wingardium leviosa";
            Experiment experiment = Config.GetRolloutFromId("166661").Experiments[2];
            Variation variation = Config.GetVariationFromKey(experiment.Key, "177784");
            Result<FeatureDecision> decision = Result<FeatureDecision>.NewResult(
                new FeatureDecision(experiment, variation, FeatureDecision.DECISION_SOURCE_ROLLOUT),
                DecisionReasons);
            UserAttributes userAttributes = new UserAttributes
            {
                {
                    "device_type", "iPhone"
                },
                {
                    "company", "Optimizely"
                },
                {
                    "location", "San Francisco"
                },
            };

            DecisionServiceMock
                .Setup(ds =>
                    ds.GetVariationForFeature(featureFlag, It.IsAny<OptimizelyUserContext>(),
                        Config))
                .Returns(decision);

            PrivateObject optly = Helper.CreatePrivateOptimizely();
            optly.SetFieldOrProperty("ProjectConfigManager", ConfigManager);
            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);

            string variableValue = (string)optly.Invoke("GetFeatureVariableString", featureKey,
                variableKey, TestUserId, userAttributes);
            Assert.AreEqual(expectedValue, variableValue);

            LoggerMock.Verify(l => l.Log(LogLevel.INFO,
                $@"Feature ""{featureKey}"" is not enabled for user {TestUserId
                }. Returning the default variable value ""{variableValue}""."));
        }

        [Test]
        public void
            TestGetFeatureVariableDoubleReturnsDefaultValueWhenUserNotBuckedIntoBothFeatureExperimentAndRollout()
        {
            string featureKey = "double_single_variable_feature";
            FeatureFlag featureFlag =
                Config.GetFeatureFlagFromKey("double_single_variable_feature");
            string variableKey = "double_variable";
            double expectedValue = 14.99;
            Result<FeatureDecision> decision = Result<FeatureDecision>.NewResult(
                new FeatureDecision(null, null, FeatureDecision.DECISION_SOURCE_ROLLOUT),
                DecisionReasons);

            DecisionServiceMock
                .Setup(ds =>
                    ds.GetVariationForFeature(featureFlag, It.IsAny<OptimizelyUserContext>(),
                        Config))
                .Returns(decision);

            PrivateObject optly = Helper.CreatePrivateOptimizely();
            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);
            optly.SetFieldOrProperty("ProjectConfigManager", ConfigManager);

            double variableValue = (double)optly.Invoke("GetFeatureVariableDouble", featureKey,
                variableKey, TestUserId, null);
            Assert.AreEqual(expectedValue, variableValue);

            LoggerMock.Verify(l => l.Log(LogLevel.INFO,
                $@"User ""{TestUserId}"" is not in any variation for feature flag ""{featureKey
                }"", returning default value ""{variableValue}""."));
        }

        #endregion Feature Toggle Tests

        #endregion Test GetFeatureVariable<Type> methods

        #region Test GetFeatureVariableValueForType method

        // Should return null and log error message when arguments are null or empty.
        [Test]
        public void TestGetFeatureVariableValueForTypeGivenNullOrEmptyArguments()
        {
            string featureKey = "featureKey";
            string variableKey = "variableKey";
            string variableType = "boolean";

            // Passing null and empty feature key.
            Assert.IsNull(Optimizely.GetFeatureVariableValueForType<bool?>(null, variableKey,
                TestUserId, null, variableType));
            Assert.IsNull(Optimizely.GetFeatureVariableValueForType<bool?>("", variableKey,
                TestUserId, null, variableType));

            // Passing null and empty variable key.
            Assert.IsNull(Optimizely.GetFeatureVariableValueForType<bool?>(featureKey, null,
                TestUserId, null, variableType));
            Assert.IsNull(Optimizely.GetFeatureVariableValueForType<bool?>(featureKey, "",
                TestUserId, null, variableType));

            // Passing null and empty user Id.
            Assert.IsNull(Optimizely.GetFeatureVariableValueForType<bool?>(featureKey, variableKey,
                null, null, variableType));
            Assert.IsNull(Optimizely.GetFeatureVariableValueForType<bool?>(featureKey, variableKey,
                "", null, variableType));

            LoggerMock.Verify(
                l => l.Log(LogLevel.ERROR, "Provided Feature Key is in invalid format."),
                Times.Exactly(2));
            LoggerMock.Verify(
                l => l.Log(LogLevel.ERROR, "Provided Variable Key is in invalid format."),
                Times.Exactly(2));
            LoggerMock.Verify(l => l.Log(LogLevel.ERROR, "Provided User Id is in invalid format."),
                Times.Exactly(1));
        }

        // Should return null and log error message when feature key or variable key does not get found.
        [Test]
        public void TestGetFeatureVariableValueForTypeGivenFeatureKeyOrVariableKeyNotFound()
        {
            string featureKey =
                "this_feature_should_never_be_found_in_the_datafile_unless_the_datafile_creator_got_insane";
            string variableKey =
                "this_variable_should_never_be_found_in_the_datafile_unless_the_datafile_creator_got_insane";
            string variableType = "boolean";

            Assert.IsNull(Optimizely.GetFeatureVariableValueForType<bool?>(featureKey, variableKey,
                TestUserId, null, variableType));
            Assert.IsNull(Optimizely.GetFeatureVariableValueForType<bool?>(
                "double_single_variable_feature", variableKey, TestUserId, null, variableType));

            LoggerMock.Verify(l =>
                l.Log(LogLevel.ERROR, $@"Feature key ""{featureKey}"" is not in datafile."));
            LoggerMock.Verify(l => l.Log(LogLevel.ERROR,
                $@"No feature variable was found for key ""{variableKey
                }"" in feature flag ""double_single_variable_feature""."));
        }

        // Should return null and log error message when variable type is invalid.
        [Test]
        public void TestGetFeatureVariableValueForTypeGivenInvalidVariableType()
        {
            string variableTypeBool = "boolean";
            string variableTypeInt = "integer";
            string variableTypeDouble = "double";
            string variableTypeString = "string";

            Assert.IsNull(Optimizely.GetFeatureVariableValueForType<double?>(
                "double_single_variable_feature", "double_variable", TestUserId, null,
                variableTypeBool));
            Assert.IsNull(Optimizely.GetFeatureVariableValueForType<bool?>(
                "boolean_single_variable_feature", "boolean_variable", TestUserId, null,
                variableTypeDouble));
            Assert.IsNull(Optimizely.GetFeatureVariableValueForType<int?>(
                "integer_single_variable_feature", "integer_variable", TestUserId, null,
                variableTypeString));
            Assert.IsNull(Optimizely.GetFeatureVariableValueForType<string>(
                "string_single_variable_feature", "string_variable", TestUserId, null,
                variableTypeInt));
            Assert.IsNull(Optimizely.GetFeatureVariableValueForType<OptimizelyJSON>(
                "string_single_variable_feature", "json_var", TestUserId, null, variableTypeInt));

            LoggerMock.Verify(l => l.Log(LogLevel.ERROR,
                $@"Variable is of type ""double"", but you requested it as type ""{variableTypeBool
                }""."));
            LoggerMock.Verify(l => l.Log(LogLevel.ERROR,
                $@"Variable is of type ""boolean"", but you requested it as type ""{
                    variableTypeDouble}""."));
            LoggerMock.Verify(l => l.Log(LogLevel.ERROR,
                $@"Variable is of type ""integer"", but you requested it as type ""{
                    variableTypeString}""."));
            LoggerMock.Verify(l => l.Log(LogLevel.ERROR,
                $@"Variable is of type ""string"", but you requested it as type ""{variableTypeInt
                }""."));
        }

        [Test]
        public void TestUnsupportedVariableType()
        {
            string featureVariableStringRandomType =
                Optimizely.GetFeatureVariableString("", "any_key", TestUserId);
            Assert.IsNull(featureVariableStringRandomType);

            // This is to test that only json subtype is parsing and all other will subtype will be stringify
            string featureVariableStringRegexSubType =
                Optimizely.GetFeatureVariableString("unsupported_variabletype", "string_regex_key",
                    TestUserId);
            Assert.AreEqual(featureVariableStringRegexSubType, "^\\d+(\\.\\d+)?");
        }

        // Should return default value and log message when feature is not enabled for the user.
        [Test]
        public void TestGetFeatureVariableValueForTypeGivenFeatureFlagIsNotEnabledForUser()
        {
            string featureKey = "double_single_variable_feature";
            FeatureFlag featureFlag =
                Config.GetFeatureFlagFromKey("double_single_variable_feature");
            Experiment experiment = Config.GetExperimentFromKey("test_experiment_double_feature");
            Variation variation =
                Config.GetVariationFromKey("test_experiment_double_feature", "variation");
            string variableKey = "double_variable";
            string variableType = "double";
            double expectedValue = 14.99;

            Result<FeatureDecision> decision = Result<FeatureDecision>.NewResult(
                new FeatureDecision(experiment, variation,
                    FeatureDecision.DECISION_SOURCE_FEATURE_TEST), DecisionReasons);

            DecisionServiceMock
                .Setup(ds =>
                    ds.GetVariationForFeature(featureFlag, It.IsAny<OptimizelyUserContext>(),
                        Config))
                .Returns(decision);

            PrivateObject optly = Helper.CreatePrivateOptimizely();

            optly.SetFieldOrProperty("ProjectConfigManager", ConfigManager);
            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);

            double? variableValue = (double?)optly.InvokeGeneric("GetFeatureVariableValueForType",
                new Type[]
                {
                    typeof(double?),
                }, featureKey, variableKey, TestUserId, null, variableType);
            Assert.AreEqual(expectedValue, variableValue);

            LoggerMock.Verify(l => l.Log(LogLevel.INFO,
                $@"Feature ""{featureKey}"" is not enabled for user {TestUserId
                }. Returning the default variable value ""{variableValue}""."));
        }

        // Should return default value and log message when feature is enabled for the user
        // but variable usage does not get found for the variation.
        [Test]
        public void
            TestGetFeatureVariableValueForTypeGivenFeatureFlagIsEnabledForUserAndVaribaleNotInVariation()
        {
            string featureKey = "double_single_variable_feature";
            FeatureFlag featureFlag =
                Config.GetFeatureFlagFromKey("double_single_variable_feature");
            Experiment experiment = Config.GetExperimentFromKey("test_experiment_integer_feature");
            Variation differentVariation =
                Config.GetVariationFromKey("test_experiment_integer_feature", "control");
            Result<FeatureDecision> expectedDecision = Result<FeatureDecision>.NewResult(
                new FeatureDecision(experiment, differentVariation,
                    FeatureDecision.DECISION_SOURCE_FEATURE_TEST), DecisionReasons);
            string variableKey = "double_variable";
            string variableType = "double";
            double expectedValue = 14.99;

            // Mock GetVariationForFeature method to return variation of different feature.
            DecisionServiceMock
                .Setup(ds =>
                    ds.GetVariationForFeature(featureFlag, It.IsAny<OptimizelyUserContext>(),
                        Config))
                .Returns(expectedDecision);

            PrivateObject optly = Helper.CreatePrivateOptimizely();

            optly.SetFieldOrProperty("ProjectConfigManager", ConfigManager);
            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);

            double? variableValue = (double?)optly.InvokeGeneric("GetFeatureVariableValueForType",
                new Type[]
                {
                    typeof(double?),
                }, featureKey, variableKey, TestUserId, null, variableType);
            Assert.AreEqual(expectedValue, variableValue);

            LoggerMock.Verify(l => l.Log(LogLevel.INFO,
                $@"Variable ""{variableKey
                }"" is not used in variation ""control"", returning default value ""{expectedValue
                }""."));
        }

        // Should return variable value from variation and log message when feature is enabled for the user
        // and variable usage has been found for the variation.
        [Test]
        public void
            TestGetFeatureVariableValueForTypeGivenFeatureFlagIsEnabledForUserAndVaribaleIsInVariation()
        {
            string featureKey = "double_single_variable_feature";
            FeatureFlag featureFlag =
                Config.GetFeatureFlagFromKey("double_single_variable_feature");
            string variableKey = "double_variable";
            string variableType = "double";
            double expectedValue = 42.42;
            Experiment experiment = Config.GetExperimentFromKey("test_experiment_double_feature");
            Variation variation =
                Config.GetVariationFromKey("test_experiment_double_feature", "control");
            Result<FeatureDecision> decision = Result<FeatureDecision>.NewResult(
                new FeatureDecision(experiment, variation,
                    FeatureDecision.DECISION_SOURCE_FEATURE_TEST), DecisionReasons);

            DecisionServiceMock
                .Setup(ds =>
                    ds.GetVariationForFeature(featureFlag, It.IsAny<OptimizelyUserContext>(),
                        Config))
                .Returns(decision);

            PrivateObject optly = Helper.CreatePrivateOptimizely();

            optly.SetFieldOrProperty("ProjectConfigManager", ConfigManager);
            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);

            double? variableValue = (double?)optly.InvokeGeneric("GetFeatureVariableValueForType",
                new Type[]
                {
                    typeof(double?),
                }, featureKey, variableKey, TestUserId, null, variableType);
            Assert.AreEqual(expectedValue, variableValue);

            LoggerMock.Verify(l => l.Log(LogLevel.INFO,
                $@"Got variable value ""{variableValue}"" for variable ""{variableKey
                }"" of feature flag ""{featureKey}""."));
        }

        // Verify that GetFeatureVariableValueForType returns correct variable value for rollout rule.
        [Test]
        public void TestGetFeatureVariableValueForTypeWithRolloutRule()
        {
            string featureKey = "boolean_single_variable_feature";
            FeatureFlag featureFlag = Config.GetFeatureFlagFromKey(featureKey);
            string variableKey = "boolean_variable";
            //experimentid - 177772
            Experiment experiment = Config.Rollouts[0].Experiments[1];
            Variation variation = Config.GetVariationFromId(experiment.Key, "177773");
            Result<FeatureDecision> decision = Result<FeatureDecision>.NewResult(
                new FeatureDecision(experiment, variation, FeatureDecision.DECISION_SOURCE_ROLLOUT),
                DecisionReasons);
            bool expectedVariableValue = false;

            DecisionServiceMock
                .Setup(ds =>
                    ds.GetVariationForFeature(featureFlag, It.IsAny<OptimizelyUserContext>(),
                        Config))
                .Returns(decision);

            PrivateObject optly = Helper.CreatePrivateOptimizely();

            optly.SetFieldOrProperty("ProjectConfigManager", ConfigManager);
            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);

            // Calling GetFeatureVariableBoolean to get GetFeatureVariableValueForType returned value casted in bool.
            bool? actualVariableValue = (bool?)optly.Invoke("GetFeatureVariableBoolean", featureKey,
                variableKey, TestUserId, null);

            // Verify that variable value 'false' has been returned from GetFeatureVariableValueForType as it is the value
            // stored in rollout rule '177772'.
            Assert.AreEqual(expectedVariableValue, actualVariableValue);
        }

        #endregion Test GetFeatureVariableValueForType method

        #region Test IsFeatureEnabled method

        // Should return false and log error message when arguments are null or empty.
        [Test]
        public void TestIsFeatureEnabledGivenNullOrEmptyArguments()
        {
            string featureKey = "featureKey";

            Assert.IsFalse(Optimizely.IsFeatureEnabled(featureKey, null, null));
            Assert.IsFalse(Optimizely.IsFeatureEnabled(featureKey, "", null));

            Assert.IsFalse(Optimizely.IsFeatureEnabled(null, TestUserId, null));
            Assert.IsFalse(Optimizely.IsFeatureEnabled("", TestUserId, null));

            LoggerMock.Verify(
                l => l.Log(LogLevel.ERROR, "Provided Feature Key is in invalid format."),
                Times.Exactly(2));
            LoggerMock.Verify(l => l.Log(LogLevel.ERROR, "Provided User Id is in invalid format."),
                Times.Exactly(1));
        }

        // Should return false and log error message when feature flag key is not found in the datafile.
        [Test]
        public void TestIsFeatureEnabledGivenFeatureFlagNotFound()
        {
            string featureKey = "feature_not_found";
            Assert.IsFalse(Optimizely.IsFeatureEnabled(featureKey, TestUserId, null));

            LoggerMock.Verify(l =>
                l.Log(LogLevel.ERROR, $@"Feature key ""{featureKey}"" is not in datafile."));
        }

        // Should return false and log error message when arguments are null or empty.
        [Test]
        public void TestIsFeatureEnabledGivenFeatureFlagContainsInvalidExperiment()
        {
            ProjectConfig tempConfig = DatafileProjectConfig.Create(TestData.Datafile,
                LoggerMock.Object,
                new NoOpErrorHandler());
            FallbackProjectConfigManager tempConfigManager =
                new FallbackProjectConfigManager(tempConfig);
            FeatureFlag featureFlag = tempConfig.GetFeatureFlagFromKey("multi_variate_feature");

            PrivateObject optly = Helper.CreatePrivateOptimizely();
            optly.SetFieldOrProperty("ProjectConfigManager", tempConfigManager);

            // Set such an experiment to the list of experiment ids, that does not belong to the feature.
            featureFlag.ExperimentIds = new List<string>
            {
                "4209211",
            };

            // Should return false when the experiment in feature flag does not get found in the datafile.
            Assert.False((bool)optly.Invoke("IsFeatureEnabled", "multi_variate_feature", TestUserId,
                null));

            LoggerMock.Verify(l =>
                l.Log(LogLevel.ERROR, @"Experiment ID ""4209211"" is not in datafile."));
        }

        // Should return false and log message when feature is not enabled for the user.
        [Test]
        public void TestIsFeatureEnabledGivenFeatureFlagIsNotEnabledForUser()
        {
            string featureKey = "double_single_variable_feature";
            FeatureFlag featureFlag =
                Config.GetFeatureFlagFromKey("double_single_variable_feature");
            Experiment experiment = Config.GetExperimentFromKey("test_experiment_double_feature");
            Variation variation =
                Config.GetVariationFromKey("test_experiment_double_feature", "variation");
            Result<FeatureDecision> decision = Result<FeatureDecision>.NewResult(
                new FeatureDecision(experiment, variation,
                    FeatureDecision.DECISION_SOURCE_FEATURE_TEST), DecisionReasons);

            DecisionServiceMock
                .Setup(ds =>
                    ds.GetVariationForFeature(featureFlag, It.IsAny<OptimizelyUserContext>(),
                        Config))
                .Returns(decision);

            PrivateObject optly = Helper.CreatePrivateOptimizely();
            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);
            optly.SetFieldOrProperty("ProjectConfigManager", ConfigManager);

            bool result = (bool)optly.Invoke("IsFeatureEnabled", featureKey, TestUserId, null);
            Assert.False(result);

            LoggerMock.Verify(l => l.Log(LogLevel.INFO,
                $@"Feature flag ""{featureKey}"" is not enabled for user ""{TestUserId}""."));
        }

        // Should return true but does not send an impression event when feature is enabled for the user
        // but user does not get experimented.
        [Test]
        public void TestIsFeatureEnabledGivenFeatureFlagIsEnabledAndUserIsNotBeingExperimented()
        {
            string featureKey = "boolean_single_variable_feature";
            Rollout rollout = Config.GetRolloutFromId("166660");
            Experiment experiment = rollout.Experiments[0];
            Variation variation = experiment.Variations[0];
            FeatureFlag featureFlag = Config.GetFeatureFlagFromKey(featureKey);
            Result<FeatureDecision> decision = Result<FeatureDecision>.NewResult(
                new FeatureDecision(experiment, variation, FeatureDecision.DECISION_SOURCE_ROLLOUT),
                DecisionReasons);

            DecisionServiceMock
                .Setup(ds =>
                    ds.GetVariationForFeature(featureFlag, It.IsAny<OptimizelyUserContext>(),
                        Config))
                .Returns(decision);

            PrivateObject optly = Helper.CreatePrivateOptimizely();
            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);
            optly.SetFieldOrProperty("ProjectConfigManager", ConfigManager);

            bool result = (bool)optly.Invoke("IsFeatureEnabled", featureKey, TestUserId, null);
            Assert.True(result);

            // SendImpressionEvent() does not get called.
            LoggerMock.Verify(l => l.Log(LogLevel.INFO,
                $@"The user ""{TestUserId}"" is not being experimented on feature ""{featureKey
                }""."), Times.Once);

            LoggerMock.Verify(l => l.Log(LogLevel.INFO,
                $@"Feature flag ""{featureKey}"" is enabled for user ""{TestUserId}""."));
        }

        // Should return true and send an impression event when feature is enabled for the user
        // and user is being experimented.
        [Test]
        public void TestIsFeatureEnabledGivenFeatureFlagIsEnabledAndUserIsBeingExperimented()
        {
            string featureKey = "double_single_variable_feature";
            Experiment experiment = Config.GetExperimentFromKey("test_experiment_double_feature");
            Variation variation =
                Config.GetVariationFromKey("test_experiment_double_feature", "control");
            FeatureFlag featureFlag = Config.GetFeatureFlagFromKey(featureKey);
            Result<FeatureDecision> decision = Result<FeatureDecision>.NewResult(
                new FeatureDecision(experiment, variation,
                    FeatureDecision.DECISION_SOURCE_FEATURE_TEST), DecisionReasons);

            DecisionServiceMock
                .Setup(ds =>
                    ds.GetVariationForFeature(featureFlag, It.IsAny<OptimizelyUserContext>(),
                        Config))
                .Returns(decision);

            PrivateObject optly = Helper.CreatePrivateOptimizely();
            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);
            optly.SetFieldOrProperty("ProjectConfigManager", ConfigManager);

            bool result = (bool)optly.Invoke("IsFeatureEnabled", featureKey, TestUserId, null);
            Assert.True(result);

            // SendImpressionEvent() gets called.
            LoggerMock.Verify(l => l.Log(LogLevel.INFO,
                $@"The user ""{TestUserId}"" is not being experimented on feature ""{featureKey
                }""."), Times.Never);

            LoggerMock.Verify(l => l.Log(LogLevel.INFO,
                $@"Feature flag ""{featureKey}"" is enabled for user ""{TestUserId}""."));
        }

        // Should return false and send an impression event when feature is enabled for the user
        // and user is being experimented.
        [Test]
        public void TestIsFeatureEnabledGivenFeatureFlagIsNotEnabledAndUserIsBeingExperimented()
        {
            string featureKey = "double_single_variable_feature";
            Experiment experiment = Config.GetExperimentFromKey("test_experiment_double_feature");
            Variation variation =
                Config.GetVariationFromKey("test_experiment_double_feature", "variation");
            FeatureFlag featureFlag = Config.GetFeatureFlagFromKey(featureKey);
            Result<FeatureDecision> decision = Result<FeatureDecision>.NewResult(
                new FeatureDecision(experiment, variation,
                    FeatureDecision.DECISION_SOURCE_FEATURE_TEST), DecisionReasons);

            DecisionServiceMock
                .Setup(ds =>
                    ds.GetVariationForFeature(featureFlag, It.IsAny<OptimizelyUserContext>(),
                        Config))
                .Returns(decision);

            PrivateObject optly = Helper.CreatePrivateOptimizely();
            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);
            optly.SetFieldOrProperty("ProjectConfigManager", ConfigManager);

            bool result = (bool)optly.Invoke("IsFeatureEnabled", featureKey, TestUserId, null);
            Assert.False(result);

            // SendImpressionEvent() gets called.
            LoggerMock.Verify(l => l.Log(LogLevel.INFO,
                $@"The user ""{TestUserId}"" is not being experimented on feature ""{featureKey
                }""."), Times.Never);

            LoggerMock.Verify(l => l.Log(LogLevel.INFO,
                $@"Feature flag ""{featureKey}"" is not enabled for user ""{TestUserId}""."));
            EventDispatcherMock.Verify(dispatcher =>
                dispatcher.DispatchEvent(It.IsAny<LogEvent>()));
        }

        // Verify that IsFeatureEnabled returns true if a variation does not get found in the feature
        // flag experiment but found in the rollout rule.
        [Test]
        public void TestIsFeatureEnabledGivenVariationNotFoundInFeatureExperimentButInRolloutRule()
        {
            string featureKey = "boolean_single_variable_feature";
            UserAttributes userAttributes = new UserAttributes
            {
                {
                    "device_type", "iPhone"
                },
                {
                    "company", "Optimizely"
                },
            };

            Assert.True(Optimizely.IsFeatureEnabled(featureKey, TestUserId, userAttributes));

            LoggerMock.Verify(
                l => l.Log(LogLevel.INFO,
                    "The feature flag \"boolean_single_variable_feature\" is not used in any experiments."),
                Times.Once);
            LoggerMock.Verify(
                l => l.Log(LogLevel.DEBUG,
                    "User \"testUserId\" does not meet the conditions for targeting rule \"1\"."),
                Times.Once);
            LoggerMock.Verify(
                l => l.Log(LogLevel.DEBUG,
                    "User \"testUserId\" does not meet the conditions for targeting rule \"2\"."),
                Times.Once);
            LoggerMock.Verify(
                l => l.Log(LogLevel.DEBUG,
                    "Assigned bucket [8408] to user [testUserId] with bucketing ID [testUserId]."),
                Times.Once);
            LoggerMock.Verify(
                l => l.Log(LogLevel.INFO,
                    "The user \"testUserId\" is bucketed into a rollout for feature flag \"boolean_single_variable_feature\"."),
                Times.Once);
            LoggerMock.Verify(
                l => l.Log(LogLevel.INFO,
                    "The user \"testUserId\" is not being experimented on feature \"boolean_single_variable_feature\"."),
                Times.Once);
            LoggerMock.Verify(
                l => l.Log(LogLevel.INFO,
                    "Feature flag \"boolean_single_variable_feature\" is enabled for user \"testUserId\"."),
                Times.Once);
        }

        public void TestIsFeatureEnabledWithFeatureEnabledPropertyGivenFeatureExperiment()
        {
            string userId = "testUserId2";
            string featureKey = "double_single_variable_feature";
            Experiment experiment = Config.GetExperimentFromKey("test_experiment_double_feature");
            Variation featureEnabledTrue =
                Config.GetVariationFromKey("test_experiment_double_feature", "control");
            Variation featureEnabledFalse =
                Config.GetVariationFromKey("test_experiment_double_feature", "variation");
            FeatureFlag featureFlag = Config.GetFeatureFlagFromKey(featureKey);
            Result<FeatureDecision> decisionTrue = Result<FeatureDecision>.NewResult(
                new FeatureDecision(experiment, featureEnabledTrue,
                    FeatureDecision.DECISION_SOURCE_FEATURE_TEST), DecisionReasons);
            Result<FeatureDecision> decisionFalse = Result<FeatureDecision>.NewResult(
                new FeatureDecision(experiment, featureEnabledFalse,
                    FeatureDecision.DECISION_SOURCE_FEATURE_TEST), DecisionReasons);

            DecisionServiceMock
                .Setup(ds =>
                    ds.GetVariationForFeature(featureFlag, It.IsAny<OptimizelyUserContext>(),
                        Config))
                .Returns(decisionTrue);

            DecisionServiceMock
                .Setup(ds =>
                    ds.GetVariationForFeature(featureFlag, It.IsAny<OptimizelyUserContext>(),
                        Config))
                .Returns(decisionFalse);

            PrivateObject optly = Helper.CreatePrivateOptimizely();
            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);

            // Verify that IsFeatureEnabled returns true when feature experiment variation's 'featureEnabled' property is true.
            bool result = (bool)optly.Invoke("IsFeatureEnabled", featureKey, TestUserId, null);
            Assert.True(result);

            // Verify that IsFeatureEnabled returns false when feature experiment variation's 'featureEnabled' property is false.
            result = (bool)optly.Invoke("IsFeatureEnabled", featureKey, userId, null);
            Assert.False(result);
        }

        public void TestIsFeatureEnabledWithFeatureEnabledPropertyGivenRolloutRule()
        {
            string featureKey = "boolean_single_variable_feature";
            FeatureFlag featureFlag = Config.GetFeatureFlagFromKey(featureKey);

            // Verify that IsFeatureEnabled returns true when user is bucketed into the rollout rule's variation.
            Assert.True(Optimizely.IsFeatureEnabled("boolean_single_variable_feature", TestUserId));

            DecisionServiceMock
                .Setup(ds =>
                    ds.GetVariationForFeature(featureFlag, It.IsAny<OptimizelyUserContext>(),
                        Config))
                .Returns<FeatureDecision>(null);
            PrivateObject optly = Helper.CreatePrivateOptimizely();
            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);

            // Verify that IsFeatureEnabled returns false when user does not get bucketed into the rollout rule's variation.
            bool result = (bool)optly.Invoke("IsFeatureEnabled", featureKey, TestUserId, null);
            Assert.False(result);
        }

        #endregion Test IsFeatureEnabled method

        #region Test NotificationCenter

        [Test]
        public void TestActivateListenerWithoutAttributes()
        {
            TestActivateListener(null);
        }

        [Test]
        public void TestActivateListenerWithAttributes()
        {
            UserAttributes userAttributes = new UserAttributes
            {
                {
                    "device_type", "iPhone"
                },
                {
                    "company", "Optimizely"
                },
                {
                    "location", "San Francisco"
                },
            };

            TestActivateListener(userAttributes);
        }

        public void TestActivateListener(UserAttributes userAttributes)
        {
            string experimentKey = "group_experiment_1";
            string variationKey = "group_exp_1_var_1";
            string featureKey = "boolean_feature";
            Experiment experiment = Config.GetExperimentFromKey(experimentKey);
            Result<Variation> variation =
                Result<Variation>.NewResult(Config.GetVariationFromKey(experimentKey, variationKey),
                    DecisionReasons);
            FeatureFlag featureFlag = Config.GetFeatureFlagFromKey(featureKey);
            Result<FeatureDecision> decision = Result<FeatureDecision>.NewResult(
                new FeatureDecision(experiment, variation.ResultObject,
                    FeatureDecision.DECISION_SOURCE_FEATURE_TEST), DecisionReasons);

            // Mocking objects.
            NotificationCallbackMock.Setup(nc => nc.TestActivateCallback(It.IsAny<Experiment>(),
                It.IsAny<string>(),
                It.IsAny<UserAttributes>(), It.IsAny<Variation>(), It.IsAny<LogEvent>()));
            NotificationCallbackMock.Setup(nc => nc.TestAnotherActivateCallback(
                It.IsAny<Experiment>(), It.IsAny<string>(),
                It.IsAny<UserAttributes>(), It.IsAny<Variation>(), It.IsAny<LogEvent>()));
            NotificationCallbackMock.Setup(nc => nc.TestTrackCallback(It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<UserAttributes>(), It.IsAny<EventTags>(), It.IsAny<LogEvent>()));

            Mock<OptimizelyUserContext> mockUserContext =
                new Mock<OptimizelyUserContext>(OptimizelyMock.Object, TestUserId, userAttributes,
                    ErrorHandlerMock.Object, LoggerMock.Object);
            mockUserContext.Setup(ouc => ouc.GetUserId()).Returns(TestUserId);

            DecisionServiceMock.Setup(ds => ds.GetVariation(experiment,
                    It.IsAny<OptimizelyUserContext>(), It.IsAny<ProjectConfig>()))
                .Returns(variation);
            DecisionServiceMock.Setup(ds => ds.GetVariationForFeature(featureFlag,
                    It.IsAny<OptimizelyUserContext>(), It.IsAny<ProjectConfig>()))
                .Returns(decision);

            PrivateObject optly = Helper.CreatePrivateOptimizely();
            Optimizely optStronglyTyped = optly.GetObject() as Optimizely;

            // Adding notification listeners.
            NotificationCenter.NotificationType notificationType =
                NotificationCenter.NotificationType.Activate;
            optStronglyTyped.NotificationCenter.AddNotification(notificationType,
                NotificationCallbackMock.Object.TestActivateCallback);
            optStronglyTyped.NotificationCenter.AddNotification(notificationType,
                NotificationCallbackMock.Object.TestAnotherActivateCallback);

            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);

            // Calling Activate and IsFeatureEnabled.
            optStronglyTyped.Activate(experimentKey, TestUserId, userAttributes);
            optStronglyTyped.IsFeatureEnabled(featureKey, TestUserId, userAttributes);

            // Verify that all the registered callbacks are called once for both Activate and IsFeatureEnabled.
            EventDispatcherMock.Verify(dispatcher => dispatcher.DispatchEvent(It.IsAny<LogEvent>()),
                Times.Exactly(2));

            NotificationCallbackMock.Verify(
                nc => nc.TestActivateCallback(experiment, TestUserId, userAttributes,
                    variation.ResultObject, It.IsAny<LogEvent>()), Times.Exactly(2));
            NotificationCallbackMock.Verify(
                nc => nc.TestAnotherActivateCallback(experiment, TestUserId, userAttributes,
                    variation.ResultObject, It.IsAny<LogEvent>()), Times.Exactly(2));
        }

        [Test]
        public void TestTrackListenerWithoutAttributesAndEventTags()
        {
            TestTrackListener(null, null);
        }

        [Test]
        public void TestTrackListenerWithAttributesWithoutEventTags()
        {
            UserAttributes userAttributes = new UserAttributes
            {
                {
                    "device_type", "iPhone"
                },
                {
                    "company", "Optimizely"
                },
                {
                    "location", "San Francisco"
                },
            };

            TestTrackListener(userAttributes, null);
        }

        [Test]
        public void TestTrackListenerWithAttributesAndEventTags()
        {
            UserAttributes userAttributes = new UserAttributes
            {
                {
                    "device_type", "iPhone"
                },
                {
                    "company", "Optimizely"
                },
                {
                    "location", "San Francisco"
                },
            };

            EventTags eventTags = new EventTags
            {
                {
                    "revenue", 42
                },
            };

            TestTrackListener(userAttributes, eventTags);
        }

        public void TestTrackListener(UserAttributes userAttributes, EventTags eventTags)
        {
            string experimentKey = "test_experiment";
            string variationKey = "control";
            string eventKey = "purchase";
            Experiment experiment = Config.GetExperimentFromKey(experimentKey);
            Result<Variation> variation =
                Result<Variation>.NewResult(Config.GetVariationFromKey(experimentKey, variationKey),
                    DecisionReasons);
            LogEvent logEvent = new LogEvent("https://logx.optimizely.com/v1/events",
                OptimizelyHelper.SingleParameter,
                "POST", new Dictionary<string, string>
                    { });

            PrivateObject optly = Helper.CreatePrivateOptimizely();
            Optimizely optStronglyTyped = optly.GetObject() as Optimizely;

            // Mocking objects.
            NotificationCallbackMock.Setup(nc => nc.TestTrackCallback(It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<UserAttributes>(), It.IsAny<EventTags>(), It.IsAny<LogEvent>()));
            NotificationCallbackMock.Setup(nc => nc.TestAnotherTrackCallback(It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<UserAttributes>(), It.IsAny<EventTags>(), It.IsAny<LogEvent>()));

            Mock<OptimizelyUserContext> mockUserContext =
                new Mock<OptimizelyUserContext>(Optimizely, TestUserId, new UserAttributes(),
                    ErrorHandlerMock.Object, LoggerMock.Object);
            mockUserContext.Setup(ouc => ouc.GetUserId()).Returns(TestUserId);

            DecisionServiceMock
                .Setup(ds => ds.GetVariation(experiment, It.IsAny<OptimizelyUserContext>(), Config))
                .Returns(variation);

            // Adding notification listeners.
            NotificationCenter.NotificationType notificationType =
                NotificationCenter.NotificationType.Track;
            optStronglyTyped.NotificationCenter.AddNotification(notificationType,
                NotificationCallbackMock.Object.TestTrackCallback);
            optStronglyTyped.NotificationCenter.AddNotification(notificationType,
                NotificationCallbackMock.Object.TestAnotherTrackCallback);

            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);

            // Calling Track.
            optly.Invoke("Track", eventKey, TestUserId, userAttributes, eventTags);

            // Verify that all the registered callbacks for Track are called.
            EventDispatcherMock.Verify(dispatcher => dispatcher.DispatchEvent(It.IsAny<LogEvent>()),
                Times.Once);
            NotificationCallbackMock.Verify(
                nc => nc.TestTrackCallback(eventKey, TestUserId, userAttributes, eventTags,
                    It.IsAny<LogEvent>()), Times.Exactly(1));
            NotificationCallbackMock.Verify(
                nc => nc.TestAnotherTrackCallback(eventKey, TestUserId, userAttributes, eventTags,
                    It.IsAny<LogEvent>()), Times.Exactly(1));
        }

        #region Decision Listener

        [Test]
        public void TestActivateSendsDecisionNotificationWithActualVariationKey()
        {
            string experimentKey = "test_experiment";
            string variationKey = "variation";
            Experiment experiment = Config.GetExperimentFromKey(experimentKey);
            Result<Variation> variation =
                Result<Variation>.NewResult(Config.GetVariationFromKey(experimentKey, variationKey),
                    DecisionReasons);
            UserAttributes userAttributes = new UserAttributes
            {
                {
                    "device_type", "iPhone"
                },
                {
                    "company", "Optimizely"
                },
                {
                    "location", "San Francisco"
                },
            };

            // Mocking objects.
            NotificationCallbackMock.Setup(nc => nc.TestDecisionCallback(It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<UserAttributes>(), It.IsAny<Dictionary<string, object>>()));

            DecisionServiceMock
                .Setup(ds => ds.GetVariation(experiment, It.IsAny<OptimizelyUserContext>(), Config))
                .Returns(variation);

            PrivateObject optly = Helper.CreatePrivateOptimizely();
            optly.SetFieldOrProperty("ProjectConfigManager", ConfigManager);
            Optimizely optStronglyTyped = optly.GetObject() as Optimizely;

            optStronglyTyped.NotificationCenter.AddNotification(
                NotificationCenter.NotificationType.Decision,
                NotificationCallbackMock.Object.TestDecisionCallback);
            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);

            optly.Invoke("Activate", experimentKey, TestUserId, userAttributes);
            Dictionary<string, object> decisionInfo = new Dictionary<string, object>
            {
                {
                    "experimentKey", experimentKey
                },
                {
                    "variationKey", variationKey
                },
            };

            NotificationCallbackMock.Verify(
                nc => nc.TestDecisionCallback(DecisionNotificationTypes.AB_TEST, TestUserId,
                    userAttributes, decisionInfo), Times.Once);
        }

        [Test]
        public void
            TestActivateSendsDecisionNotificationWithVariationKeyAndTypeFeatureTestForFeatureExperiment()
        {
            string experimentKey = "group_experiment_1";
            string variationKey = "group_exp_1_var_1";
            Experiment experiment = Config.GetExperimentFromKey(experimentKey);
            Result<Variation> variation =
                Result<Variation>.NewResult(Config.GetVariationFromKey(experimentKey, variationKey),
                    DecisionReasons);
            UserAttributes userAttributes = new UserAttributes
            {
                {
                    "device_type", "iPhone"
                },
                {
                    "company", "Optimizely"
                },
                {
                    "location", "San Francisco"
                },
            };

            // Mocking objects.
            NotificationCallbackMock.Setup(nc => nc.TestDecisionCallback(It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<UserAttributes>(), It.IsAny<Dictionary<string, object>>()));

            DecisionServiceMock
                .Setup(ds => ds.GetVariation(experiment, It.IsAny<OptimizelyUserContext>(), Config))
                .Returns(variation);

            PrivateObject optly = Helper.CreatePrivateOptimizely();
            optly.SetFieldOrProperty("ProjectConfigManager", ConfigManager);
            Optimizely optStronglyTyped = optly.GetObject() as Optimizely;

            optStronglyTyped.NotificationCenter.AddNotification(
                NotificationCenter.NotificationType.Decision,
                NotificationCallbackMock.Object.TestDecisionCallback);
            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);

            optly.Invoke("Activate", experimentKey, TestUserId, userAttributes);
            Dictionary<string, object> decisionInfo = new Dictionary<string, object>
            {
                {
                    "experimentKey", experimentKey
                },
                {
                    "variationKey", variationKey
                },
            };

            NotificationCallbackMock.Verify(
                nc => nc.TestDecisionCallback(DecisionNotificationTypes.FEATURE_TEST, TestUserId,
                    userAttributes, decisionInfo), Times.Once);
        }

        [Test]
        public void TestActivateSendsDecisionNotificationWithNullVariationKey()
        {
            string experimentKey = "test_experiment";
            Experiment experiment = Config.GetExperimentFromKey(experimentKey);

            PrivateObject optly = Helper.CreatePrivateOptimizely();
            Optimizely optStronglyTyped = optly.GetObject() as Optimizely;

            NotificationCallbackMock.Setup(nc => nc.TestDecisionCallback(It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<UserAttributes>(), It.IsAny<Dictionary<string, object>>()));

            DecisionServiceMock
                .Setup(ds => ds.GetVariation(experiment, It.IsAny<OptimizelyUserContext>(),
                    It.IsAny<ProjectConfig>(), null))
                .Returns(Result<Variation>.NullResult(null));

            optStronglyTyped.NotificationCenter.AddNotification(
                NotificationCenter.NotificationType.Decision,
                NotificationCallbackMock.Object.TestDecisionCallback);
            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);

            optly.Invoke("Activate", experimentKey, TestUserId, null);
            Dictionary<string, object> decisionInfo = new Dictionary<string, object>
            {
                {
                    "experimentKey", experimentKey
                },
                {
                    "variationKey", null
                },
            };

            NotificationCallbackMock.Verify(
                nc => nc.TestDecisionCallback(DecisionNotificationTypes.AB_TEST, TestUserId,
                    new UserAttributes(), decisionInfo), Times.Once);
        }

        [Test]
        public void TestGetVariationSendsDecisionNotificationWithActualVariationKey()
        {
            string experimentKey = "test_experiment";
            string variationKey = "variation";
            Experiment experiment = Config.GetExperimentFromKey(experimentKey);
            Result<Variation> variation =
                Result<Variation>.NewResult(Config.GetVariationFromKey(experimentKey, variationKey),
                    DecisionReasons);
            UserAttributes userAttributes = new UserAttributes
            {
                {
                    "device_type", "iPhone"
                },
                {
                    "company", "Optimizely"
                },
                {
                    "location", "San Francisco"
                },
            };

            PrivateObject optly = Helper.CreatePrivateOptimizely();

            optly.SetFieldOrProperty("ProjectConfigManager", ConfigManager);
            Optimizely optStronglyTyped = optly.GetObject() as Optimizely;

            // Mocking objects.
            NotificationCallbackMock.Setup(nc => nc.TestDecisionCallback(It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<UserAttributes>(), It.IsAny<Dictionary<string, object>>()));

            Mock<OptimizelyUserContext> mockUserContext =
                new Mock<OptimizelyUserContext>(optStronglyTyped, TestUserId, new UserAttributes(),
                    ErrorHandlerMock.Object, LoggerMock.Object);
            mockUserContext.Setup(ouc => ouc.GetUserId()).Returns(TestUserId);

            DecisionServiceMock
                .Setup(ds => ds.GetVariation(experiment, It.IsAny<OptimizelyUserContext>(), Config))
                .Returns(variation);

            optStronglyTyped.NotificationCenter.AddNotification(
                NotificationCenter.NotificationType.Decision,
                NotificationCallbackMock.Object.TestDecisionCallback);
            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);

            optly.Invoke("GetVariation", experimentKey, TestUserId, userAttributes);
            Dictionary<string, object> decisionInfo = new Dictionary<string, object>
            {
                {
                    "experimentKey", experimentKey
                },
                {
                    "variationKey", variationKey
                },
            };

            NotificationCallbackMock.Verify(
                nc => nc.TestDecisionCallback(DecisionNotificationTypes.AB_TEST, TestUserId,
                    userAttributes, decisionInfo), Times.Once);
        }

        [Test]
        public void
            TestGetVariationSendsDecisionNotificationWithVariationKeyAndTypeFeatureTestForFeatureExperiment()
        {
            string experimentKey = "group_experiment_1";
            string variationKey = "group_exp_1_var_1";
            Experiment experiment = Config.GetExperimentFromKey(experimentKey);
            Result<Variation> variation =
                Result<Variation>.NewResult(Config.GetVariationFromKey(experimentKey, variationKey),
                    DecisionReasons);
            UserAttributes userAttributes = new UserAttributes
            {
                {
                    "device_type", "iPhone"
                },
                {
                    "company", "Optimizely"
                },
                {
                    "location", "San Francisco"
                },
            };

            PrivateObject optly = Helper.CreatePrivateOptimizely();
            Optimizely optStronglyTyped = optly.GetObject() as Optimizely;

            // Mocking objects.
            NotificationCallbackMock.Setup(nc => nc.TestDecisionCallback(It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<UserAttributes>(), It.IsAny<Dictionary<string, object>>()));

            Mock<OptimizelyUserContext> mockUserContext =
                new Mock<OptimizelyUserContext>(optStronglyTyped, TestUserId, new UserAttributes(),
                    ErrorHandlerMock.Object, LoggerMock.Object);
            mockUserContext.Setup(ouc => ouc.GetUserId()).Returns(TestUserId);

            DecisionServiceMock
                .Setup(ds => ds.GetVariation(experiment, It.IsAny<OptimizelyUserContext>(), Config))
                .Returns(variation);

            optStronglyTyped.NotificationCenter.AddNotification(
                NotificationCenter.NotificationType.Decision,
                NotificationCallbackMock.Object.TestDecisionCallback);
            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);
            optly.SetFieldOrProperty("ProjectConfigManager", ConfigManager);

            optly.Invoke("GetVariation", experimentKey, TestUserId, userAttributes);
            Dictionary<string, object> decisionInfo = new Dictionary<string, object>
            {
                {
                    "experimentKey", experimentKey
                },
                {
                    "variationKey", variationKey
                },
            };

            NotificationCallbackMock.Verify(
                nc => nc.TestDecisionCallback(DecisionNotificationTypes.FEATURE_TEST, TestUserId,
                    userAttributes, decisionInfo), Times.Once);
        }

        [Test]
        public void TestGetVariationSendsDecisionNotificationWithNullVariationKey()
        {
            string experimentKey = "test_experiment";
            Experiment experiment = Config.GetExperimentFromKey(experimentKey);

            PrivateObject optly = Helper.CreatePrivateOptimizely();
            Optimizely optStronglyTyped = optly.GetObject() as Optimizely;

            NotificationCallbackMock.Setup(nc => nc.TestDecisionCallback(It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<UserAttributes>(), It.IsAny<Dictionary<string, object>>()));

            DecisionServiceMock
                .Setup(ds => ds.GetVariation(It.IsAny<Experiment>(),
                    It.IsAny<OptimizelyUserContext>(), It.IsAny<ProjectConfig>()))
                .Returns(Result<Variation>.NullResult(null));
            //DecisionServiceMock.Setup(ds => ds.GetVariation(experiment, TestUserId, Config, null)).Returns(Result<Variation>.NullResult(null));

            optStronglyTyped.NotificationCenter.AddNotification(
                NotificationCenter.NotificationType.Decision,
                NotificationCallbackMock.Object.TestDecisionCallback);
            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);

            optly.Invoke("GetVariation", experimentKey, TestUserId, null);
            Dictionary<string, object> decisionInfo = new Dictionary<string, object>
            {
                {
                    "experimentKey", experimentKey
                },
                {
                    "variationKey", null
                },
            };

            NotificationCallbackMock.Verify(
                nc => nc.TestDecisionCallback(DecisionNotificationTypes.AB_TEST, TestUserId,
                    new UserAttributes(), decisionInfo), Times.Once);
        }

        public void
            TestIsFeatureEnabledSendsDecisionNotificationWithFeatureEnabledTrueForFeatureExperiment()
        {
            string featureKey = "double_single_variable_feature";
            Experiment experiment = Config.GetExperimentFromKey("test_experiment_double_feature");
            Result<Variation> variation = Result<Variation>.NewResult(
                Config.GetVariationFromKey("test_experiment_double_feature", "control"),
                DecisionReasons);
            FeatureFlag featureFlag = Config.GetFeatureFlagFromKey(featureKey);
            Result<FeatureDecision> decision = Result<FeatureDecision>.NewResult(
                new FeatureDecision(experiment, variation.ResultObject,
                    FeatureDecision.DECISION_SOURCE_FEATURE_TEST), DecisionReasons);

            DecisionServiceMock
                .Setup(ds =>
                    ds.GetVariationForFeature(featureFlag, It.IsAny<OptimizelyUserContext>(),
                        Config))
                .Returns(decision);
            NotificationCallbackMock.Setup(nc => nc.TestDecisionCallback(It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<UserAttributes>(), It.IsAny<Dictionary<string, object>>()));

            DecisionServiceMock.Setup(ds => ds.GetVariation(experiment,
                    It.IsAny<OptimizelyUserContext>(), ConfigManager.GetConfig(), null))
                .Returns(variation);

            PrivateObject optly = Helper.CreatePrivateOptimizely();
            Optimizely optStronglyTyped = optly.GetObject() as Optimizely;

            optStronglyTyped.NotificationCenter.AddNotification(
                NotificationCenter.NotificationType.Decision,
                NotificationCallbackMock.Object.TestDecisionCallback);
            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);

            bool result = (bool)optly.Invoke("IsFeatureEnabled", featureKey, TestUserId, null);
            Assert.True(result);

            Dictionary<string, object> decisionInfo = new Dictionary<string, object>
            {
                {
                    "featureKey", featureKey
                },
                {
                    "featureEnabled", true
                },
                {
                    "source", FeatureDecision.DECISION_SOURCE_FEATURE_TEST
                },
                {
                    "sourceExperimentKey", "test_experiment_double_feature"
                },
                {
                    "sourceVariationKey", "control"
                },
            };

            NotificationCallbackMock.Verify(
                nc => nc.TestDecisionCallback(DecisionNotificationTypes.FEATURE, TestUserId,
                    new UserAttributes(), decisionInfo), Times.Once);
        }

        [Test]
        public void
            TestIsFeatureEnabledSendsDecisionNotificationWithFeatureEnabledFalseForFeatureExperiment()
        {
            string featureKey = "double_single_variable_feature";
            Experiment experiment = Config.GetExperimentFromKey("test_experiment_double_feature");
            Result<Variation> variation = Result<Variation>.NewResult(
                Config.GetVariationFromKey("test_experiment_double_feature", "variation"),
                DecisionReasons);
            FeatureFlag featureFlag = Config.GetFeatureFlagFromKey(featureKey);
            Result<FeatureDecision> decision = Result<FeatureDecision>.NewResult(
                new FeatureDecision(experiment, variation.ResultObject,
                    FeatureDecision.DECISION_SOURCE_FEATURE_TEST), DecisionReasons);

            DecisionServiceMock
                .Setup(ds =>
                    ds.GetVariationForFeature(featureFlag, It.IsAny<OptimizelyUserContext>(),
                        Config))
                .Returns(decision);
            NotificationCallbackMock.Setup(nc => nc.TestDecisionCallback(It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<UserAttributes>(), It.IsAny<Dictionary<string, object>>()));

            DecisionServiceMock
                .Setup(ds =>
                    ds.GetVariation(experiment, It.IsAny<OptimizelyUserContext>(), Config, null))
                .Returns(variation);

            PrivateObject optly = Helper.CreatePrivateOptimizely();
            Optimizely optStronglyTyped = optly.GetObject() as Optimizely;

            optStronglyTyped.NotificationCenter.AddNotification(
                NotificationCenter.NotificationType.Decision,
                NotificationCallbackMock.Object.TestDecisionCallback);
            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);
            optly.SetFieldOrProperty("ProjectConfigManager", ConfigManager);

            bool result = (bool)optly.Invoke("IsFeatureEnabled", featureKey, TestUserId, null);
            Assert.False(result);

            Dictionary<string, object> decisionInfo = new Dictionary<string, object>
            {
                {
                    "featureKey", featureKey
                },
                {
                    "featureEnabled", false
                },
                {
                    "source", FeatureDecision.DECISION_SOURCE_FEATURE_TEST
                },
                {
                    "sourceInfo", new Dictionary<string, string>
                    {
                        {
                            "experimentKey", "test_experiment_double_feature"
                        },
                        {
                            "variationKey", "variation"
                        },
                    }
                },
            };

            NotificationCallbackMock.Verify(
                nc => nc.TestDecisionCallback(DecisionNotificationTypes.FEATURE, TestUserId,
                    new UserAttributes(),
                    It.Is<Dictionary<string, object>>(info =>
                        TestData.CompareObjects(info, decisionInfo))), Times.Once);
        }

        [Test]
        public void
            TestIsFeatureEnabledSendsDecisionNotificationWithFeatureEnabledTrueForFeatureRollout()
        {
            string featureKey = "boolean_single_variable_feature";
            UserAttributes userAttributes = new UserAttributes
            {
                {
                    "device_type", "iPhone"
                },
                {
                    "company", "Optimizely"
                },
            };
            Experiment experiment = Config.GetRolloutFromId("166660").Experiments[0];
            Variation variation = Config.GetVariationFromKey("177770", "177771");
            FeatureFlag featureFlag = Config.GetFeatureFlagFromKey(featureKey);
            Result<FeatureDecision> decision = Result<FeatureDecision>.NewResult(
                new FeatureDecision(experiment, variation, FeatureDecision.DECISION_SOURCE_ROLLOUT),
                DecisionReasons);

            DecisionServiceMock
                .Setup(ds =>
                    ds.GetVariationForFeature(featureFlag, It.IsAny<OptimizelyUserContext>(),
                        Config))
                .Returns(decision);
            NotificationCallbackMock.Setup(nc => nc.TestDecisionCallback(It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<UserAttributes>(), It.IsAny<Dictionary<string, object>>()));

            PrivateObject optly = Helper.CreatePrivateOptimizely();
            optly.SetFieldOrProperty("ProjectConfigManager", ConfigManager);
            Optimizely optStronglyTyped = optly.GetObject() as Optimizely;

            optStronglyTyped.NotificationCenter.AddNotification(
                NotificationCenter.NotificationType.Decision,
                NotificationCallbackMock.Object.TestDecisionCallback);
            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);

            bool result = (bool)optly.Invoke("IsFeatureEnabled", featureKey, TestUserId,
                userAttributes);
            Assert.True(result);

            Dictionary<string, object> decisionInfo = new Dictionary<string, object>
            {
                {
                    "featureKey", featureKey
                },
                {
                    "featureEnabled", true
                },
                {
                    "source", FeatureDecision.DECISION_SOURCE_ROLLOUT
                },
                {
                    "sourceInfo", new Dictionary<string, string>()
                },
            };

            NotificationCallbackMock.Verify(
                nc => nc.TestDecisionCallback(DecisionNotificationTypes.FEATURE, TestUserId,
                    userAttributes,
                    It.Is<Dictionary<string, object>>(info =>
                        TestData.CompareObjects(info, decisionInfo))), Times.Once);
        }

        [Test]
        public void
            TestIsFeatureEnabledSendsDecisionNotificationWithFeatureEnabledFalseForFeatureRollout()
        {
            string featureKey = "boolean_single_variable_feature";
            UserAttributes userAttributes = new UserAttributes
            {
                {
                    "device_type", "iPhone"
                },
                {
                    "company", "Optimizely"
                },
            };
            Experiment experiment = Config.GetRolloutFromId("166660").Experiments[3];
            Variation variation = Config.GetVariationFromKey("188880", "188881");
            FeatureFlag featureFlag = Config.GetFeatureFlagFromKey(featureKey);
            Result<FeatureDecision> decision = Result<FeatureDecision>.NewResult(
                new FeatureDecision(experiment, variation, FeatureDecision.DECISION_SOURCE_ROLLOUT),
                DecisionReasons);

            DecisionServiceMock
                .Setup(ds =>
                    ds.GetVariationForFeature(featureFlag, It.IsAny<OptimizelyUserContext>(),
                        Config))
                .Returns(decision);
            NotificationCallbackMock.Setup(nc => nc.TestDecisionCallback(It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<UserAttributes>(), It.IsAny<Dictionary<string, object>>()));

            PrivateObject optly = Helper.CreatePrivateOptimizely();
            Optimizely optStronglyTyped = optly.GetObject() as Optimizely;

            optStronglyTyped.NotificationCenter.AddNotification(
                NotificationCenter.NotificationType.Decision,
                NotificationCallbackMock.Object.TestDecisionCallback);
            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);
            optly.SetFieldOrProperty("ProjectConfigManager", ConfigManager);

            bool result = (bool)optly.Invoke("IsFeatureEnabled", featureKey, TestUserId,
                userAttributes);
            Assert.False(result);

            Dictionary<string, object> decisionInfo = new Dictionary<string, object>
            {
                {
                    "featureKey", featureKey
                },
                {
                    "featureEnabled", false
                },
                {
                    "source", FeatureDecision.DECISION_SOURCE_ROLLOUT
                },
                {
                    "sourceInfo", new Dictionary<string, string>()
                },
            };

            NotificationCallbackMock.Verify(
                nc => nc.TestDecisionCallback(DecisionNotificationTypes.FEATURE, TestUserId,
                    userAttributes,
                    It.Is<Dictionary<string, object>>(info =>
                        TestData.CompareObjects(info, decisionInfo))), Times.Once);
        }

        [Test]
        public void
            TestIsFeatureEnabledSendsDecisionNotificationWithFeatureEnabledFalseWhenUserIsNotBucketed()
        {
            string featureKey = "boolean_single_variable_feature";
            FeatureFlag featureFlag = Config.GetFeatureFlagFromKey(featureKey);
            Result<FeatureDecision> decision = Result<FeatureDecision>.NewResult(
                new FeatureDecision(null, null, FeatureDecision.DECISION_SOURCE_ROLLOUT),
                DecisionReasons);

            DecisionServiceMock
                .Setup(ds =>
                    ds.GetVariationForFeature(featureFlag, It.IsAny<OptimizelyUserContext>(),
                        Config))
                .Returns(decision);
            NotificationCallbackMock.Setup(nc => nc.TestDecisionCallback(It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<UserAttributes>(), It.IsAny<Dictionary<string, object>>()));

            PrivateObject optly = Helper.CreatePrivateOptimizely();
            Optimizely optStronglyTyped = optly.GetObject() as Optimizely;

            optStronglyTyped.NotificationCenter.AddNotification(
                NotificationCenter.NotificationType.Decision,
                NotificationCallbackMock.Object.TestDecisionCallback);
            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);
            optly.SetFieldOrProperty("ProjectConfigManager", ConfigManager);

            bool result = (bool)optly.Invoke("IsFeatureEnabled", featureKey, TestUserId, null);
            Assert.False(result);

            Dictionary<string, object> decisionInfo = new Dictionary<string, object>
            {
                {
                    "featureKey", featureKey
                },
                {
                    "featureEnabled", false
                },
                {
                    "source", FeatureDecision.DECISION_SOURCE_ROLLOUT
                },
                {
                    "sourceInfo", new Dictionary<string, string>()
                },
            };

            NotificationCallbackMock.Verify(
                nc => nc.TestDecisionCallback(DecisionNotificationTypes.FEATURE, TestUserId,
                    new UserAttributes(),
                    It.Is<Dictionary<string, object>>(info =>
                        TestData.CompareObjects(info, decisionInfo))), Times.Once);
        }

        [Test]
        public void
            TestGetEnabledFeaturesSendDecisionNotificationForBothEnabledAndDisabledFeatures()
        {
            string[] enabledFeatures =
            {
                "double_single_variable_feature", "boolean_single_variable_feature",
                "string_single_variable_feature",
            };

            UserAttributes userAttributes = new UserAttributes
            {
                {
                    "device_type", "iPhone"
                },
                {
                    "location", "San Francisco"
                },
            };

            NotificationCallbackMock.Setup(nc => nc.TestDecisionCallback(It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<UserAttributes>(), It.IsAny<Dictionary<string, object>>()));
            OptimizelyMock.Object.NotificationCenter.AddNotification(
                NotificationCenter.NotificationType.Decision,
                NotificationCallbackMock.Object.TestDecisionCallback);

            List<string> actualFeaturesList =
                OptimizelyMock.Object.GetEnabledFeatures(TestUserId, userAttributes);
            CollectionAssert.AreEquivalent(enabledFeatures, actualFeaturesList);

            NotificationCallbackMock.Verify(nc => nc.TestDecisionCallback(
                DecisionNotificationTypes.FEATURE, TestUserId, userAttributes,
                It.Is<Dictionary<string, object>>(info => TestData.CompareObjects(info,
                    new Dictionary<string, object>
                    {
                        {
                            "featureKey", "boolean_feature"
                        },
                        {
                            "featureEnabled", false
                        },
                        {
                            "source", FeatureDecision.DECISION_SOURCE_FEATURE_TEST
                        },
                        {
                            "sourceInfo", new Dictionary<string, string>
                            {
                                {
                                    "experimentKey", "group_experiment_2"
                                },
                                {
                                    "variationKey", "group_exp_2_var_1"
                                },
                            }
                        },
                    }))), Times.Once);
            NotificationCallbackMock.Verify(nc => nc.TestDecisionCallback(
                DecisionNotificationTypes.FEATURE, TestUserId, userAttributes,
                It.Is<Dictionary<string, object>>(info => TestData.CompareObjects(info,
                    new Dictionary<string, object>
                    {
                        {
                            "featureKey", "double_single_variable_feature"
                        },
                        {
                            "featureEnabled", true
                        },
                        {
                            "source", FeatureDecision.DECISION_SOURCE_FEATURE_TEST
                        },
                        {
                            "sourceInfo", new Dictionary<string, string>
                            {
                                {
                                    "experimentKey", "test_experiment_double_feature"
                                },
                                {
                                    "variationKey", "control"
                                },
                            }
                        },
                    }))), Times.Once);
            NotificationCallbackMock.Verify(nc => nc.TestDecisionCallback(
                DecisionNotificationTypes.FEATURE, TestUserId, userAttributes,
                It.Is<Dictionary<string, object>>(info => TestData.CompareObjects(info,
                    new Dictionary<string, object>
                    {
                        {
                            "featureKey", "integer_single_variable_feature"
                        },
                        {
                            "featureEnabled", false
                        },
                        {
                            "source", FeatureDecision.DECISION_SOURCE_FEATURE_TEST
                        },
                        {
                            "sourceInfo", new Dictionary<string, string>
                            {
                                {
                                    "experimentKey", "test_experiment_integer_feature"
                                },
                                {
                                    "variationKey", "control"
                                },
                            }
                        },
                    }))), Times.Once);
            NotificationCallbackMock.Verify(nc => nc.TestDecisionCallback(
                DecisionNotificationTypes.FEATURE, TestUserId, userAttributes,
                It.Is<Dictionary<string, object>>(info => TestData.CompareObjects(info,
                    new Dictionary<string, object>
                    {
                        {
                            "featureKey", "boolean_single_variable_feature"
                        },
                        {
                            "featureEnabled", true
                        },
                        {
                            "source", FeatureDecision.DECISION_SOURCE_ROLLOUT
                        },
                        {
                            "sourceInfo", new Dictionary<string, string>()
                        },
                    }))), Times.Once);
            NotificationCallbackMock.Verify(nc => nc.TestDecisionCallback(
                DecisionNotificationTypes.FEATURE, TestUserId, userAttributes,
                It.Is<Dictionary<string, object>>(info => TestData.CompareObjects(info,
                    new Dictionary<string, object>
                    {
                        {
                            "featureKey", "string_single_variable_feature"
                        },
                        {
                            "featureEnabled", true
                        },
                        {
                            "source", FeatureDecision.DECISION_SOURCE_FEATURE_TEST
                        },
                        {
                            "sourceInfo", new Dictionary<string, string>
                            {
                                {
                                    "experimentKey", "test_experiment_with_feature_rollout"
                                },
                                {
                                    "variationKey", "variation"
                                },
                            }
                        },
                    }))), Times.Once);
            NotificationCallbackMock.Verify(nc => nc.TestDecisionCallback(
                DecisionNotificationTypes.FEATURE, TestUserId, userAttributes,
                It.Is<Dictionary<string, object>>(info => TestData.CompareObjects(info,
                    new Dictionary<string, object>
                    {
                        {
                            "featureKey", "multi_variate_feature"
                        },
                        {
                            "featureEnabled", false
                        },
                        {
                            "source", FeatureDecision.DECISION_SOURCE_ROLLOUT
                        },
                        {
                            "sourceInfo", new Dictionary<string, string>()
                        },
                    }))), Times.Once);
            NotificationCallbackMock.Verify(nc => nc.TestDecisionCallback(
                DecisionNotificationTypes.FEATURE, TestUserId, userAttributes,
                It.Is<Dictionary<string, object>>(info => TestData.CompareObjects(info,
                    new Dictionary<string, object>
                    {
                        {
                            "featureKey", "mutex_group_feature"
                        },
                        {
                            "featureEnabled", false
                        },
                        {
                            "source", FeatureDecision.DECISION_SOURCE_FEATURE_TEST
                        },
                        {
                            "sourceInfo", new Dictionary<string, string>
                            {
                                {
                                    "experimentKey", "group_experiment_2"
                                },
                                {
                                    "variationKey", "group_exp_2_var_1"
                                },
                            }
                        },
                    }))), Times.Once);
            NotificationCallbackMock.Verify(nc => nc.TestDecisionCallback(
                DecisionNotificationTypes.FEATURE, TestUserId, userAttributes,
                It.Is<Dictionary<string, object>>(info => TestData.CompareObjects(info,
                    new Dictionary<string, object>
                    {
                        {
                            "featureKey", "empty_feature"
                        },
                        {
                            "featureEnabled", false
                        },
                        {
                            "source", FeatureDecision.DECISION_SOURCE_ROLLOUT
                        },
                        {
                            "sourceInfo", new Dictionary<string, string>()
                        },
                    }))), Times.Once);
            NotificationCallbackMock.Verify(nc => nc.TestDecisionCallback(
                DecisionNotificationTypes.FEATURE, TestUserId, userAttributes,
                It.Is<Dictionary<string, object>>(info => TestData.CompareObjects(info,
                    new Dictionary<string, object>
                    {
                        {
                            "featureKey", "no_rollout_experiment_feature"
                        },
                        {
                            "featureEnabled", false
                        },
                        {
                            "source", FeatureDecision.DECISION_SOURCE_ROLLOUT
                        },
                        {
                            "sourceInfo", new Dictionary<string, string>()
                        },
                    }))), Times.Once);
        }

        [Test]
        public void
            TestGetFeatureVariableDoubleSendsNotificationWhenUserBuckedIntoFeatureExperimentAndVariationIsToggleOn()
        {
            string featureKey = "double_single_variable_feature";
            FeatureFlag featureFlag = Config.GetFeatureFlagFromKey(featureKey);
            string variableKey = "double_variable";
            double expectedValue = 42.42;
            Experiment experiment = Config.GetExperimentFromKey("test_experiment_double_feature");
            Variation variation =
                Config.GetVariationFromKey("test_experiment_double_feature", "control");
            Result<FeatureDecision> decision = Result<FeatureDecision>.NewResult(
                new FeatureDecision(experiment, variation,
                    FeatureDecision.DECISION_SOURCE_FEATURE_TEST), DecisionReasons);

            DecisionServiceMock
                .Setup(ds =>
                    ds.GetVariationForFeature(featureFlag, It.IsAny<OptimizelyUserContext>(),
                        Config))
                .Returns(decision);
            NotificationCallbackMock.Setup(nc => nc.TestDecisionCallback(It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<UserAttributes>(),
                It.IsAny<Dictionary<string, object>>()));

            PrivateObject optly = Helper.CreatePrivateOptimizely();
            Optimizely optStronglyTyped = optly.GetObject() as Optimizely;

            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);
            optly.SetFieldOrProperty("ProjectConfigManager", ConfigManager);

            optStronglyTyped.NotificationCenter.AddNotification(
                NotificationCenter.NotificationType.Decision,
                NotificationCallbackMock.Object.TestDecisionCallback);

            double variableValue = (double)optly.Invoke("GetFeatureVariableDouble", featureKey,
                variableKey, TestUserId, null);
            Assert.AreEqual(expectedValue, variableValue);
            Dictionary<string, object> decisionInfo = new Dictionary<string, object>
            {
                {
                    "featureKey", featureKey
                },
                {
                    "featureEnabled", true
                },
                {
                    "variableKey", variableKey
                },
                {
                    "variableValue", expectedValue
                },
                {
                    "variableType", FEATUREVARIABLE_DOUBLETYPE
                },
                {
                    "source", FeatureDecision.DECISION_SOURCE_FEATURE_TEST
                },
                {
                    "sourceInfo", new Dictionary<string, string>
                    {
                        {
                            "experimentKey", "test_experiment_double_feature"
                        },
                        {
                            "variationKey", "control"
                        },
                    }
                },
            };

            NotificationCallbackMock.Verify(
                nc => nc.TestDecisionCallback(DecisionNotificationTypes.FEATURE_VARIABLE,
                    TestUserId, new UserAttributes(),
                    It.Is<Dictionary<string, object>>(info =>
                        TestData.CompareObjects(info, decisionInfo))), Times.Once);
        }

        [Test]
        public void
            TestGetFeatureVariableJsonSendsNotificationWhenUserBuckedIntoFeatureExperimentAndVariationIsToggleOn()
        {
            string featureKey = "string_single_variable_feature";
            FeatureFlag featureFlag = Config.GetFeatureFlagFromKey(featureKey);
            string variableKey = "json_var";
            Dictionary<string, object> expectedDict = new Dictionary<string, object>()
            {
                {
                    "int_var", 4
                },
                {
                    "string_var", "cta_4"
                },
            };
            Experiment experiment = Config.GetRolloutFromId("166661").Experiments[0];
            Variation variation = Config.GetVariationFromKey(experiment.Key, "177775");
            Result<FeatureDecision> decision = Result<FeatureDecision>.NewResult(
                new FeatureDecision(experiment, variation, FeatureDecision.DECISION_SOURCE_ROLLOUT),
                DecisionReasons);
            UserAttributes userAttributes = new UserAttributes
            {
                {
                    "device_type", "iPhone"
                },
                {
                    "company", "Optimizely"
                },
                {
                    "location", "San Francisco"
                },
            };

            DecisionServiceMock
                .Setup(ds =>
                    ds.GetVariationForFeature(featureFlag, It.IsAny<OptimizelyUserContext>(),
                        Config))
                .Returns(decision);
            NotificationCallbackMock.Setup(nc => nc.TestDecisionCallback(It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<UserAttributes>(),
                It.IsAny<Dictionary<string, object>>()));

            PrivateObject optly = Helper.CreatePrivateOptimizely();

            optly.SetFieldOrProperty("ProjectConfigManager", ConfigManager);
            Optimizely optStronglyTyped = optly.GetObject() as Optimizely;

            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);
            optStronglyTyped.NotificationCenter.AddNotification(
                NotificationCenter.NotificationType.Decision,
                NotificationCallbackMock.Object.TestDecisionCallback);

            OptimizelyJSON variableValue = (OptimizelyJSON)optly.Invoke("GetFeatureVariableJSON",
                featureKey,
                variableKey, TestUserId, userAttributes);
            Assert.IsTrue(TestData.CompareObjects(expectedDict, variableValue.ToDictionary()));
            Dictionary<string, object> decisionInfo = new Dictionary<string, object>
            {
                {
                    "featureKey", featureKey
                },
                {
                    "featureEnabled", true
                },
                {
                    "variableKey", variableKey
                },
                {
                    "variableValue", expectedDict
                },
                {
                    "variableType", FEATUREVARIABLE_JSONTYPE
                },
                {
                    "source", FeatureDecision.DECISION_SOURCE_ROLLOUT
                },
                {
                    "sourceInfo", new Dictionary<string, string>()
                },
            };

            NotificationCallbackMock.Verify(
                nc => nc.TestDecisionCallback(DecisionNotificationTypes.FEATURE_VARIABLE,
                    TestUserId, userAttributes,
                    It.Is<Dictionary<string, object>>(info =>
                        TestData.CompareObjects(info, decisionInfo))), Times.Once);
        }

        [Test]
        public void
            TestGetFeatureVariableIntegerSendsNotificationWhenUserBuckedIntoFeatureExperimentAndVariationIsToggleOn()
        {
            string featureKey = "integer_single_variable_feature";
            FeatureFlag featureFlag = Config.GetFeatureFlagFromKey(featureKey);
            string variableKey = "integer_variable";
            int expectedValue = 13;
            Experiment experiment = Config.GetExperimentFromKey("test_experiment_integer_feature");
            Variation variation =
                Config.GetVariationFromKey("test_experiment_integer_feature", "variation");
            Result<FeatureDecision> decision = Result<FeatureDecision>.NewResult(
                new FeatureDecision(experiment, variation,
                    FeatureDecision.DECISION_SOURCE_FEATURE_TEST), DecisionReasons);
            UserAttributes userAttributes = new UserAttributes
            {
                {
                    "device_type", "iPhone"
                },
                {
                    "company", "Optimizely"
                },
                {
                    "location", "San Francisco"
                },
            };

            DecisionServiceMock
                .Setup(ds =>
                    ds.GetVariationForFeature(featureFlag, It.IsAny<OptimizelyUserContext>(),
                        Config))
                .Returns(decision);
            NotificationCallbackMock.Setup(nc => nc.TestDecisionCallback(It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<UserAttributes>(),
                It.IsAny<Dictionary<string, object>>()));

            PrivateObject optly = Helper.CreatePrivateOptimizely();
            optly.SetFieldOrProperty("ProjectConfigManager", ConfigManager);

            Optimizely optStronglyTyped = optly.GetObject() as Optimizely;

            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);
            optStronglyTyped.NotificationCenter.AddNotification(
                NotificationCenter.NotificationType.Decision,
                NotificationCallbackMock.Object.TestDecisionCallback);

            int variableValue = (int)optly.Invoke("GetFeatureVariableInteger", featureKey,
                variableKey, TestUserId, userAttributes);
            Assert.AreEqual(expectedValue, variableValue);
            Dictionary<string, object> decisionInfo = new Dictionary<string, object>
            {
                {
                    "featureKey", featureKey
                },
                {
                    "featureEnabled", true
                },
                {
                    "variableKey", variableKey
                },
                {
                    "variableValue", expectedValue
                },
                {
                    "variableType", FEATUREVARIABLE_INTEGERTYPE
                },
                {
                    "source", FeatureDecision.DECISION_SOURCE_FEATURE_TEST
                },
                {
                    "sourceInfo", new Dictionary<string, string>
                    {
                        {
                            "experimentKey", "test_experiment_integer_feature"
                        },
                        {
                            "variationKey", "variation"
                        },
                    }
                },
            };

            NotificationCallbackMock.Verify(
                nc => nc.TestDecisionCallback(DecisionNotificationTypes.FEATURE_VARIABLE,
                    TestUserId, userAttributes,
                    It.Is<Dictionary<string, object>>(info =>
                        TestData.CompareObjects(info, decisionInfo))), Times.Once);
        }

        [Test]
        public void
            TestGetFeatureVariableDoubleSendsNotificationWhenUserBuckedIntoFeatureExperimentAndVariationIsToggleOff()
        {
            string featureKey = "double_single_variable_feature";
            FeatureFlag featureFlag = Config.GetFeatureFlagFromKey(featureKey);
            string variableKey = "double_variable";
            double expectedValue = 14.99;
            Experiment experiment = Config.GetExperimentFromKey("test_experiment_double_feature");
            Variation variation =
                Config.GetVariationFromKey("test_experiment_double_feature", "variation");
            Result<FeatureDecision> decision = Result<FeatureDecision>.NewResult(
                new FeatureDecision(experiment, variation,
                    FeatureDecision.DECISION_SOURCE_FEATURE_TEST), DecisionReasons);

            DecisionServiceMock
                .Setup(ds =>
                    ds.GetVariationForFeature(featureFlag, It.IsAny<OptimizelyUserContext>(),
                        Config))
                .Returns(decision);
            NotificationCallbackMock.Setup(nc => nc.TestDecisionCallback(It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<UserAttributes>(),
                It.IsAny<Dictionary<string, object>>()));

            PrivateObject optly = Helper.CreatePrivateOptimizely();
            Optimizely optStronglyTyped = optly.GetObject() as Optimizely;

            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);
            optly.SetFieldOrProperty("ProjectConfigManager", ConfigManager);
            optStronglyTyped.NotificationCenter.AddNotification(
                NotificationCenter.NotificationType.Decision,
                NotificationCallbackMock.Object.TestDecisionCallback);

            double variableValue = (double)optly.Invoke("GetFeatureVariableDouble", featureKey,
                variableKey, TestUserId, null);
            Assert.AreEqual(expectedValue, variableValue);
            Dictionary<string, object> decisionInfo = new Dictionary<string, object>
            {
                {
                    "featureKey", featureKey
                },
                {
                    "featureEnabled", false
                },
                {
                    "variableKey", variableKey
                },
                {
                    "variableValue", expectedValue
                },
                {
                    "variableType", FEATUREVARIABLE_DOUBLETYPE
                },
                {
                    "source", FeatureDecision.DECISION_SOURCE_FEATURE_TEST
                },
                {
                    "sourceInfo", new Dictionary<string, string>
                    {
                        {
                            "experimentKey", "test_experiment_double_feature"
                        },
                        {
                            "variationKey", "variation"
                        },
                    }
                },
            };

            NotificationCallbackMock.Verify(
                nc => nc.TestDecisionCallback(DecisionNotificationTypes.FEATURE_VARIABLE,
                    TestUserId, new UserAttributes(),
                    It.Is<Dictionary<string, object>>(info =>
                        TestData.CompareObjects(info, decisionInfo))), Times.Once);
        }

        [Test]
        public void
            TestGetFeatureVariableIntegerSendsNotificationWhenUserBuckedIntoFeatureExperimentAndVariationIsToggleOff()
        {
            string featureKey = "integer_single_variable_feature";
            FeatureFlag featureFlag = Config.GetFeatureFlagFromKey(featureKey);
            string variableKey = "integer_variable";
            int expectedValue = 7;
            Experiment experiment = Config.GetExperimentFromKey("test_experiment_integer_feature");
            Variation variation =
                Config.GetVariationFromKey("test_experiment_integer_feature", "control");
            Result<FeatureDecision> decision = Result<FeatureDecision>.NewResult(
                new FeatureDecision(experiment, variation,
                    FeatureDecision.DECISION_SOURCE_FEATURE_TEST), DecisionReasons);
            UserAttributes userAttributes = new UserAttributes
            {
                {
                    "device_type", "iPhone"
                },
                {
                    "company", "Optimizely"
                },
                {
                    "location", "San Francisco"
                },
            };

            DecisionServiceMock
                .Setup(ds =>
                    ds.GetVariationForFeature(featureFlag, It.IsAny<OptimizelyUserContext>(),
                        Config))
                .Returns(decision);
            NotificationCallbackMock.Setup(nc => nc.TestDecisionCallback(It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<UserAttributes>(),
                It.IsAny<Dictionary<string, object>>()));

            PrivateObject optly = Helper.CreatePrivateOptimizely();
            optly.SetFieldOrProperty("ProjectConfigManager", ConfigManager);
            Optimizely optStronglyTyped = optly.GetObject() as Optimizely;

            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);
            optStronglyTyped.NotificationCenter.AddNotification(
                NotificationCenter.NotificationType.Decision,
                NotificationCallbackMock.Object.TestDecisionCallback);

            int variableValue = (int)optly.Invoke("GetFeatureVariableInteger", featureKey,
                variableKey, TestUserId, userAttributes);
            Assert.AreEqual(expectedValue, variableValue);
            Dictionary<string, object> decisionInfo = new Dictionary<string, object>
            {
                {
                    "featureKey", featureKey
                },
                {
                    "featureEnabled", false
                },
                {
                    "variableKey", variableKey
                },
                {
                    "variableValue", expectedValue
                },
                {
                    "variableType", FEATUREVARIABLE_INTEGERTYPE
                },
                {
                    "source", FeatureDecision.DECISION_SOURCE_FEATURE_TEST
                },
                {
                    "sourceInfo", new Dictionary<string, string>
                    {
                        {
                            "experimentKey", "test_experiment_integer_feature"
                        },
                        {
                            "variationKey", "control"
                        },
                    }
                },
            };

            NotificationCallbackMock.Verify(
                nc => nc.TestDecisionCallback(DecisionNotificationTypes.FEATURE_VARIABLE,
                    TestUserId, userAttributes,
                    It.Is<Dictionary<string, object>>(info =>
                        TestData.CompareObjects(info, decisionInfo))), Times.Once);
        }

        [Test]
        public void
            TestGetFeatureVariableBooleanSendsNotificationWhenUserBuckedIntoRolloutAndVariationIsToggleOn()
        {
            string featureKey = "boolean_single_variable_feature";
            FeatureFlag featureFlag = Config.GetFeatureFlagFromKey(featureKey);
            string variableKey = "boolean_variable";
            bool expectedValue = true;
            Experiment experiment = Config.GetRolloutFromId("166660").Experiments[0];
            Variation variation = Config.GetVariationFromKey(experiment.Key, "177771");
            Result<FeatureDecision> decision = Result<FeatureDecision>.NewResult(
                new FeatureDecision(experiment, variation, FeatureDecision.DECISION_SOURCE_ROLLOUT),
                DecisionReasons);

            DecisionServiceMock
                .Setup(ds =>
                    ds.GetVariationForFeature(featureFlag, It.IsAny<OptimizelyUserContext>(),
                        Config))
                .Returns(decision);
            NotificationCallbackMock.Setup(nc => nc.TestDecisionCallback(It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<UserAttributes>(),
                It.IsAny<Dictionary<string, object>>()));

            PrivateObject optly = Helper.CreatePrivateOptimizely();
            optly.SetFieldOrProperty("ProjectConfigManager", ConfigManager);
            Optimizely optStronglyTyped = optly.GetObject() as Optimizely;

            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);
            optStronglyTyped.NotificationCenter.AddNotification(
                NotificationCenter.NotificationType.Decision,
                NotificationCallbackMock.Object.TestDecisionCallback);

            bool variableValue = (bool)optly.Invoke("GetFeatureVariableBoolean", featureKey,
                variableKey, TestUserId, null);
            Assert.AreEqual(expectedValue, variableValue);
            Dictionary<string, object> decisionInfo = new Dictionary<string, object>
            {
                {
                    "featureKey", featureKey
                },
                {
                    "featureEnabled", true
                },
                {
                    "variableKey", variableKey
                },
                {
                    "variableValue", expectedValue
                },
                {
                    "variableType", FEATUREVARIABLE_BOOLEANTYPE
                },
                {
                    "source", FeatureDecision.DECISION_SOURCE_ROLLOUT
                },
                {
                    "sourceInfo", new Dictionary<string, string>()
                },
            };

            NotificationCallbackMock.Verify(
                nc => nc.TestDecisionCallback(DecisionNotificationTypes.FEATURE_VARIABLE,
                    TestUserId, new UserAttributes(),
                    It.Is<Dictionary<string, object>>(info =>
                        TestData.CompareObjects(info, decisionInfo))), Times.Once);
        }

        [Test]
        public void
            TestGetFeatureVariableStringSendsNotificationWhenUserBuckedIntoRolloutAndVariationIsToggleOn()
        {
            string featureKey = "string_single_variable_feature";
            FeatureFlag featureFlag = Config.GetFeatureFlagFromKey(featureKey);
            string variableKey = "string_variable";
            string expectedValue = "cta_4";
            Experiment experiment = Config.GetRolloutFromId("166661").Experiments[0];
            Variation variation = Config.GetVariationFromKey(experiment.Key, "177775");
            Result<FeatureDecision> decision = Result<FeatureDecision>.NewResult(
                new FeatureDecision(experiment, variation, FeatureDecision.DECISION_SOURCE_ROLLOUT),
                DecisionReasons);
            UserAttributes userAttributes = new UserAttributes
            {
                {
                    "device_type", "iPhone"
                },
                {
                    "company", "Optimizely"
                },
                {
                    "location", "San Francisco"
                },
            };

            DecisionServiceMock
                .Setup(ds =>
                    ds.GetVariationForFeature(featureFlag, It.IsAny<OptimizelyUserContext>(),
                        Config))
                .Returns(decision);
            NotificationCallbackMock.Setup(nc => nc.TestDecisionCallback(It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<UserAttributes>(),
                It.IsAny<Dictionary<string, object>>()));

            PrivateObject optly = Helper.CreatePrivateOptimizely();

            optly.SetFieldOrProperty("ProjectConfigManager", ConfigManager);
            Optimizely optStronglyTyped = optly.GetObject() as Optimizely;

            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);
            optStronglyTyped.NotificationCenter.AddNotification(
                NotificationCenter.NotificationType.Decision,
                NotificationCallbackMock.Object.TestDecisionCallback);

            string variableValue = (string)optly.Invoke("GetFeatureVariableString", featureKey,
                variableKey, TestUserId, userAttributes);
            Assert.AreEqual(expectedValue, variableValue);
            Dictionary<string, object> decisionInfo = new Dictionary<string, object>
            {
                {
                    "featureKey", featureKey
                },
                {
                    "featureEnabled", true
                },
                {
                    "variableKey", variableKey
                },
                {
                    "variableValue", expectedValue
                },
                {
                    "variableType", FEATUREVARIABLE_STRINGTYPE
                },
                {
                    "source", FeatureDecision.DECISION_SOURCE_ROLLOUT
                },
                {
                    "sourceInfo", new Dictionary<string, string>()
                },
            };

            NotificationCallbackMock.Verify(
                nc => nc.TestDecisionCallback(DecisionNotificationTypes.FEATURE_VARIABLE,
                    TestUserId, userAttributes,
                    It.Is<Dictionary<string, object>>(info =>
                        TestData.CompareObjects(info, decisionInfo))), Times.Once);
        }

        [Test]
        public void
            TestGetFeatureVariableBooleanSendsNotificationWhenUserBuckedIntoRolloutAndVariationIsToggleOff()
        {
            string featureKey = "boolean_single_variable_feature";
            FeatureFlag featureFlag = Config.GetFeatureFlagFromKey(featureKey);
            string variableKey = "boolean_variable";
            bool expectedValue = true;
            Experiment experiment = Config.GetRolloutFromId("166660").Experiments[3];
            Variation variation = Config.GetVariationFromKey(experiment.Key, "177782");
            Result<FeatureDecision> decision = Result<FeatureDecision>.NewResult(
                new FeatureDecision(experiment, variation, FeatureDecision.DECISION_SOURCE_ROLLOUT),
                DecisionReasons);

            DecisionServiceMock
                .Setup(ds =>
                    ds.GetVariationForFeature(featureFlag, It.IsAny<OptimizelyUserContext>(),
                        Config))
                .Returns(decision);
            NotificationCallbackMock.Setup(nc => nc.TestDecisionCallback(It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<UserAttributes>(),
                It.IsAny<Dictionary<string, object>>()));

            PrivateObject optly = Helper.CreatePrivateOptimizely();
            Optimizely optStronglyTyped = optly.GetObject() as Optimizely;

            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);
            optly.SetFieldOrProperty("ProjectConfigManager", ConfigManager);
            optStronglyTyped.NotificationCenter.AddNotification(
                NotificationCenter.NotificationType.Decision,
                NotificationCallbackMock.Object.TestDecisionCallback);

            bool variableValue = (bool)optly.Invoke("GetFeatureVariableBoolean", featureKey,
                variableKey, TestUserId, null);
            Assert.AreEqual(expectedValue, variableValue);
            Dictionary<string, object> decisionInfo = new Dictionary<string, object>
            {
                {
                    "featureKey", featureKey
                },
                {
                    "featureEnabled", false
                },
                {
                    "variableKey", variableKey
                },
                {
                    "variableValue", expectedValue
                },
                {
                    "variableType", FEATUREVARIABLE_BOOLEANTYPE
                },
                {
                    "source", FeatureDecision.DECISION_SOURCE_ROLLOUT
                },
                {
                    "sourceInfo", new Dictionary<string, string>()
                },
            };

            NotificationCallbackMock.Verify(
                nc => nc.TestDecisionCallback(DecisionNotificationTypes.FEATURE_VARIABLE,
                    TestUserId, new UserAttributes(),
                    It.Is<Dictionary<string, object>>(info =>
                        TestData.CompareObjects(info, decisionInfo))), Times.Once);
        }

        [Test]
        public void
            TestGetFeatureVariableStringSendsNotificationWhenUserBuckedIntoRolloutAndVariationIsToggleOff()
        {
            string featureKey = "string_single_variable_feature";
            FeatureFlag featureFlag = Config.GetFeatureFlagFromKey(featureKey);
            string variableKey = "string_variable";
            string expectedValue = "wingardium leviosa";
            Experiment experiment = Config.GetRolloutFromId("166661").Experiments[2];
            Variation variation = Config.GetVariationFromKey(experiment.Key, "177784");
            Result<FeatureDecision> decision = Result<FeatureDecision>.NewResult(
                new FeatureDecision(experiment, variation, FeatureDecision.DECISION_SOURCE_ROLLOUT),
                DecisionReasons);
            UserAttributes userAttributes = new UserAttributes
            {
                {
                    "device_type", "iPhone"
                },
                {
                    "company", "Optimizely"
                },
                {
                    "location", "San Francisco"
                },
            };

            DecisionServiceMock
                .Setup(ds =>
                    ds.GetVariationForFeature(featureFlag, It.IsAny<OptimizelyUserContext>(),
                        Config))
                .Returns(decision);
            NotificationCallbackMock.Setup(nc => nc.TestDecisionCallback(It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<UserAttributes>(),
                It.IsAny<Dictionary<string, object>>()));

            PrivateObject optly = Helper.CreatePrivateOptimizely();
            optly.SetFieldOrProperty("ProjectConfigManager", ConfigManager);
            Optimizely optStronglyTyped = optly.GetObject() as Optimizely;

            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);
            optStronglyTyped.NotificationCenter.AddNotification(
                NotificationCenter.NotificationType.Decision,
                NotificationCallbackMock.Object.TestDecisionCallback);

            string variableValue = (string)optly.Invoke("GetFeatureVariableString", featureKey,
                variableKey, TestUserId, userAttributes);
            Assert.AreEqual(expectedValue, variableValue);
            Dictionary<string, object> decisionInfo = new Dictionary<string, object>
            {
                {
                    "featureKey", featureKey
                },
                {
                    "featureEnabled", false
                },
                {
                    "variableKey", variableKey
                },
                {
                    "variableValue", expectedValue
                },
                {
                    "variableType", FEATUREVARIABLE_STRINGTYPE
                },
                {
                    "source", FeatureDecision.DECISION_SOURCE_ROLLOUT
                },
                {
                    "sourceInfo", new Dictionary<string, string>()
                },
            };

            NotificationCallbackMock.Verify(
                nc => nc.TestDecisionCallback(DecisionNotificationTypes.FEATURE_VARIABLE,
                    TestUserId, userAttributes,
                    It.Is<Dictionary<string, object>>(info =>
                        TestData.CompareObjects(info, decisionInfo))), Times.Once);
        }

        [Test]
        public void
            TestGetFeatureVariableDoubleSendsNotificationWhenUserNotBuckedIntoBothFeatureExperimentAndRollout()
        {
            string featureKey = "double_single_variable_feature";
            FeatureFlag featureFlag =
                Config.GetFeatureFlagFromKey("double_single_variable_feature");
            string variableKey = "double_variable";
            double expectedValue = 14.99;
            Result<FeatureDecision> decision = Result<FeatureDecision>.NewResult(
                new FeatureDecision(null, null, FeatureDecision.DECISION_SOURCE_ROLLOUT),
                DecisionReasons);

            DecisionServiceMock
                .Setup(ds =>
                    ds.GetVariationForFeature(featureFlag, It.IsAny<OptimizelyUserContext>(),
                        Config))
                .Returns(decision);
            NotificationCallbackMock.Setup(nc => nc.TestDecisionCallback(It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<UserAttributes>(),
                It.IsAny<Dictionary<string, object>>()));

            PrivateObject optly = Helper.CreatePrivateOptimizely();
            Optimizely optStronglyTyped = optly.GetObject() as Optimizely;

            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);
            optly.SetFieldOrProperty("ProjectConfigManager", ConfigManager);
            optStronglyTyped.NotificationCenter.AddNotification(
                NotificationCenter.NotificationType.Decision,
                NotificationCallbackMock.Object.TestDecisionCallback);

            double variableValue = (double)optly.Invoke("GetFeatureVariableDouble", featureKey,
                variableKey, TestUserId, null);
            Assert.AreEqual(expectedValue, variableValue);
            Dictionary<string, object> decisionInfo = new Dictionary<string, object>
            {
                {
                    "featureKey", featureKey
                },
                {
                    "featureEnabled", false
                },
                {
                    "variableKey", variableKey
                },
                {
                    "variableValue", expectedValue
                },
                {
                    "variableType", FEATUREVARIABLE_DOUBLETYPE
                },
                {
                    "source", FeatureDecision.DECISION_SOURCE_ROLLOUT
                },
                {
                    "sourceInfo", new Dictionary<string, string>()
                },
            };

            NotificationCallbackMock.Verify(
                nc => nc.TestDecisionCallback(DecisionNotificationTypes.FEATURE_VARIABLE,
                    TestUserId, new UserAttributes(),
                    It.Is<Dictionary<string, object>>(info =>
                        TestData.CompareObjects(info, decisionInfo))), Times.Once);
        }

        [Test]
        public void
            TestGetAllFeatureVariablesSendsNotificationWhenUserBucketIntoRolloutAndVariationIsToggleOn()
        {
            string featureKey = "string_single_variable_feature";
            FeatureFlag featureFlag = Config.GetFeatureFlagFromKey(featureKey);
            Dictionary<string, object> expectedValue = new Dictionary<string, object>()
            {
                {
                    "string_variable", "cta_4"
                },
                {
                    "json_var", new Dictionary<string, object>()
                    {
                        {
                            "int_var", 4
                        },
                        {
                            "string_var", "cta_4"
                        },
                    }
                },
                {
                    "true_json_var", new Dictionary<string, object>()
                    {
                        {
                            "int_var", 5
                        },
                        {
                            "string_var", "cta_5"
                        },
                    }
                },
            };
            Experiment experiment = Config.GetRolloutFromId("166661").Experiments[0];
            Variation variation = Config.GetVariationFromKey(experiment.Key, "177775");
            Result<FeatureDecision> decision = Result<FeatureDecision>.NewResult(
                new FeatureDecision(experiment, variation, FeatureDecision.DECISION_SOURCE_ROLLOUT),
                DecisionReasons);
            UserAttributes userAttributes = new UserAttributes
            {
                {
                    "device_type", "iPhone"
                },
                {
                    "company", "Optimizely"
                },
                {
                    "location", "San Francisco"
                },
            };

            DecisionServiceMock
                .Setup(ds =>
                    ds.GetVariationForFeature(featureFlag, It.IsAny<OptimizelyUserContext>(),
                        Config))
                .Returns(decision);
            NotificationCallbackMock.Setup(nc => nc.TestDecisionCallback(It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<UserAttributes>(),
                It.IsAny<Dictionary<string, object>>()));

            PrivateObject optly = Helper.CreatePrivateOptimizely();

            optly.SetFieldOrProperty("ProjectConfigManager", ConfigManager);
            Optimizely optStronglyTyped = optly.GetObject() as Optimizely;

            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);
            optStronglyTyped.NotificationCenter.AddNotification(
                NotificationCenter.NotificationType.Decision,
                NotificationCallbackMock.Object.TestDecisionCallback);

            OptimizelyJSON variableValues = (OptimizelyJSON)optly.Invoke("GetAllFeatureVariables",
                featureKey,
                TestUserId, userAttributes);
            Assert.IsTrue(TestData.CompareObjects(variableValues.ToDictionary(), expectedValue));
            Dictionary<string, object> decisionInfo = new Dictionary<string, object>
            {
                {
                    "featureKey", featureKey
                },
                {
                    "featureEnabled", true
                },
                {
                    "variableValues", expectedValue
                },
                {
                    "source", FeatureDecision.DECISION_SOURCE_ROLLOUT
                },
                {
                    "sourceInfo", new Dictionary<string, string>()
                },
            };

            NotificationCallbackMock.Verify(
                nc => nc.TestDecisionCallback(DecisionNotificationTypes.ALL_FEATURE_VARIABLE,
                    TestUserId, userAttributes,
                    It.Is<Dictionary<string, object>>(info =>
                        TestData.CompareObjects(info, decisionInfo))), Times.Once);
        }

        #endregion Decision Listener

        #region Test GetAllFeatureVariables

        [Test]
        public void TestGetAllFeatureVariablesReturnsNullScenarios()
        {
            UserAttributes userAttributes = new UserAttributes
            {
                {
                    "device_type", "iPhone"
                },
                {
                    "company", "Optimizely"
                },
                {
                    "location", "San Francisco"
                },
            };
            Optimizely optimizely = new Optimizely(TestData.Datafile, EventDispatcherMock.Object,
                LoggerMock.Object, ErrorHandlerMock.Object);

            // Null Feature flag key
            OptimizelyJSON result =
                optimizely.GetAllFeatureVariables(null, TestUserId, userAttributes);
            Assert.Null(result);

            LoggerMock.Verify(
                log => log.Log(LogLevel.WARN, "The featureKey parameter must be nonnull."),
                Times.Once);

            // Null User ID
            OptimizelyJSON result2 = optimizely.GetAllFeatureVariables(
                "string_single_variable_feature", null,
                userAttributes);
            Assert.Null(result2);

            LoggerMock.Verify(
                log => log.Log(LogLevel.WARN, "The userId parameter must be nonnull."), Times.Once);

            // Invalid featureKey
            string featureKey = "InvalidFeatureKey";

            OptimizelyJSON result3 =
                optimizely.GetAllFeatureVariables(featureKey, TestUserId, userAttributes);
            Assert.Null(result3);

            LoggerMock.Verify(
                log => log.Log(LogLevel.INFO,
                    "No feature flag was found for key \"" + featureKey + "\"."), Times.Once);

            // Null Optimizely config
            Optimizely invalidOptly = new Optimizely("Random datafile", null, LoggerMock.Object);

            OptimizelyJSON result4 =
                invalidOptly.GetAllFeatureVariables("validFeatureKey", TestUserId, userAttributes);

            Assert.Null(result4);

            LoggerMock.Verify(
                log => log.Log(LogLevel.ERROR,
                    "Optimizely instance is not valid, failing getAllFeatureVariableValues call. type"),
                Times.Once);
        }

        [Test]
        public void TestGetAllFeatureVariablesRollout()
        {
            string featureKey = "string_single_variable_feature";
            Experiment experiment = Config.GetRolloutFromId("166661").Experiments[0];

            UserAttributes userAttributes = new UserAttributes
            {
                {
                    "device_type", "iPhone"
                },
                {
                    "company", "Optimizely"
                },
                {
                    "location", "San Francisco"
                },
            };

            FeatureFlag featureFlag = Config.GetFeatureFlagFromKey(featureKey);

            Result<FeatureDecision> decision = Result<FeatureDecision>.NewResult(
                new FeatureDecision(experiment, null, FeatureDecision.DECISION_SOURCE_ROLLOUT),
                DecisionReasons);

            DecisionServiceMock
                .Setup(ds =>
                    ds.GetVariationForFeature(featureFlag, It.IsAny<OptimizelyUserContext>(),
                        Config))
                .Returns(decision);

            PrivateObject optly = Helper.CreatePrivateOptimizely();
            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);
            optly.SetFieldOrProperty("ProjectConfigManager", ConfigManager);

            OptimizelyJSON result = (OptimizelyJSON)optly.Invoke("GetAllFeatureVariables",
                featureKey,
                TestUserId, userAttributes);
            Assert.NotNull(result);
            LoggerMock.Verify(
                log => log.Log(LogLevel.INFO,
                    "Feature \"" + featureKey + "\" is not enabled for user \"" + TestUserId +
                    "\""), Times.Once);

            LoggerMock.Verify(log => log.Log(LogLevel.INFO,
                "User \"" + TestUserId +
                "\" was not bucketed into any variation for feature flag \"" + featureKey + "\". " +
                "The default values are being returned."), Times.Once);
        }

        [Test]
        public void TestGetAllFeatureVariablesSourceFeatureTest()
        {
            string featureKey = "double_single_variable_feature";
            Dictionary<string, object> expectedValue = new Dictionary<string, object>()
            {
                {
                    "double_variable", 42.42
                },
            };

            Optimizely optimizely = new Optimizely(TestData.Datafile, EventDispatcherMock.Object,
                LoggerMock.Object, ErrorHandlerMock.Object);

            OptimizelyJSON variableValues =
                optimizely.GetAllFeatureVariables(featureKey, TestUserId, null);
            Assert.IsTrue(TestData.CompareObjects(variableValues.ToDictionary(), expectedValue));
            LoggerMock.Verify(
                log => log.Log(LogLevel.INFO,
                    "Feature \"" + featureKey + "\" is enabled for user \"" + TestUserId + "\""),
                Times.Once);

            LoggerMock.Verify(log => log.Log(LogLevel.INFO,
                "User \"" + TestUserId +
                "\" was not bucketed into any variation for feature flag \"" + featureKey + "\". " +
                "The default values are being returned."), Times.Never);
        }

        #endregion Test GetAllFeatureVariables

        #region DFM Notification

        [Test]
        public void TestDFMNotificationWhenProjectConfigIsUpdated()
        {
            CountdownEvent cde = new CountdownEvent(1);
            Mock<HttpProjectConfigManager.HttpClient> httpClientMock =
                new Mock<HttpProjectConfigManager.HttpClient>();
            Task t = TestHttpProjectConfigManagerUtil.MockSendAsync(httpClientMock,
                TestData.Datafile, TimeSpan.FromMilliseconds(300));
            TestHttpProjectConfigManagerUtil.SetClientFieldValue(httpClientMock.Object);

            NotificationCenter notificationCenter = new NotificationCenter();
            NotificationCallbackMock.Setup(notification => notification.TestConfigUpdateCallback())
                .Callback(() => cde.Signal());

            HttpProjectConfigManager httpManager = new HttpProjectConfigManager.Builder()
                .WithSdkKey("QBw9gFM8oTn7ogY9ANCC1z")
                .WithLogger(LoggerMock.Object)
                .WithPollingInterval(TimeSpan.FromMilliseconds(1000))
                .WithStartByDefault(false)
                .WithBlockingTimeoutPeriod(TimeSpan.FromMilliseconds(500))
                .WithNotificationCenter(notificationCenter)
                .Build(true);

            Optimizely optimizely = new Optimizely(httpManager, notificationCenter);
            optimizely.NotificationCenter.AddNotification(
                NotificationCenter.NotificationType.OptimizelyConfigUpdate,
                NotificationCallbackMock.Object.TestConfigUpdateCallback);
            httpManager.Start();

            cde.Wait(10000);

            t.Wait();
            NotificationCallbackMock.Verify(nc => nc.TestConfigUpdateCallback(), Times.Once);
            httpManager.Dispose();
        }

        [Test]
        public void TestDFMWhenDatafileProvidedDoesNotNotifyWithoutStart()
        {
            Mock<HttpProjectConfigManager.HttpClient> httpClientMock =
                new Mock<HttpProjectConfigManager.HttpClient>();
            TestHttpProjectConfigManagerUtil.SetClientFieldValue(httpClientMock.Object);

            HttpProjectConfigManager httpManager = new HttpProjectConfigManager.Builder()
                .WithSdkKey("QBw9gFM8oTn7ogY9ANCC1z")
                .WithDatafile(TestData.Datafile)
                .WithLogger(LoggerMock.Object)
                .WithPollingInterval(TimeSpan.FromMilliseconds(1000))
                .WithBlockingTimeoutPeriod(TimeSpan.FromMilliseconds(500))
                .Build();

            Optimizely optimizely = new Optimizely(httpManager);
            optimizely.NotificationCenter.AddNotification(
                NotificationCenter.NotificationType.OptimizelyConfigUpdate,
                NotificationCallbackMock.Object.TestConfigUpdateCallback);

            // added 10 secs max wait to avoid stale state.
            httpManager.OnReady().Wait(10000);

            NotificationCallbackMock.Verify(nc => nc.TestConfigUpdateCallback(), Times.Never);
            httpManager.Dispose();
        }

        #endregion DFM Notification

        #endregion Test NotificationCenter

        #region Test GetEnabledFeatures

        [Test]
        public void TestGetEnabledFeaturesWithInvalidDatafile()
        {
            Optimizely optly = new Optimizely("Random datafile", null, LoggerMock.Object);

            Assert.IsEmpty(optly.GetEnabledFeatures("some_user", null));

            LoggerMock.Verify(
                l => l.Log(LogLevel.ERROR,
                    "Datafile has invalid format. Failing 'GetEnabledFeatures'."), Times.Once);
        }

        [Test]
        public void TestGetEnabledFeaturesWithNoFeatureEnabledForUser()
        {
            UserAttributes userAttributes = new UserAttributes
            {
                {
                    "device_type", "iPhone"
                },
                {
                    "location", "San Francisco"
                },
            };

            OptimizelyMock.Setup(om =>
                    om.IsFeatureEnabled(It.IsAny<string>(), TestUserId, It.IsAny<UserAttributes>()))
                .Returns(false);
            Assert.IsEmpty(OptimizelyMock.Object.GetEnabledFeatures(TestUserId, userAttributes));
        }

        [Test]
        public void TestGetEnabledFeaturesWithSomeFeaturesEnabledForUser()
        {
            string[] enabledFeatures =
            {
                "boolean_feature", "double_single_variable_feature",
                "string_single_variable_feature", "multi_variate_feature", "empty_feature",
            };
            string[] notEnabledFeatures =
            {
                "integer_single_variable_feature", "boolean_single_variable_feature",
                "mutex_group_feature", "no_rollout_experiment_feature",
            };
            UserAttributes userAttributes = new UserAttributes
            {
                {
                    "device_type", "iPhone"
                },
                {
                    "location", "San Francisco"
                },
            };

            OptimizelyMock.Setup(om => om.IsFeatureEnabled(It.IsIn<string>(enabledFeatures),
                    TestUserId,
                    It.IsAny<UserAttributes>()))
                .Returns(true);
            OptimizelyMock.Setup(om => om.IsFeatureEnabled(It.IsIn<string>(notEnabledFeatures),
                    TestUserId,
                    It.IsAny<UserAttributes>()))
                .Returns(false);

            List<string> actualFeaturesList =
                OptimizelyMock.Object.GetEnabledFeatures(TestUserId, userAttributes);

            // Verify that the returned feature list contains only enabledFeatures.
            CollectionAssert.AreEquivalent(enabledFeatures, actualFeaturesList);
            Array.ForEach(notEnabledFeatures,
                nef => CollectionAssert.DoesNotContain(actualFeaturesList, nef));
        }

        #endregion Test GetEnabledFeatures

        #region Test ValidateStringInputs

        [Test]
        public void TestValidateStringInputsWithValidValues()
        {
            PrivateObject optly = Helper.CreatePrivateOptimizely();

            bool result = (bool)optly.Invoke("ValidateStringInputs", new Dictionary<string, string>
            {
                {
                    Optimizely.EXPERIMENT_KEY, "test_experiment"
                },
            });
            Assert.True(result);

            result = (bool)optly.Invoke("ValidateStringInputs", new Dictionary<string, string>
            {
                {
                    Optimizely.EVENT_KEY, "buy_now_event"
                },
            });
            Assert.True(result);
        }

        [Test]
        public void TestValidateStringInputsWithInvalidValues()
        {
            PrivateObject optly = Helper.CreatePrivateOptimizely();

            bool result = (bool)optly.Invoke("ValidateStringInputs", new Dictionary<string, string>
            {
                {
                    Optimizely.EXPERIMENT_KEY, ""
                },
            });
            Assert.False(result);

            result = (bool)optly.Invoke("ValidateStringInputs", new Dictionary<string, string>
            {
                {
                    Optimizely.EVENT_KEY, null
                },
            });
            Assert.False(result);
        }

        [Test]
        public void TestValidateStringInputsWithUserId()
        {
            PrivateObject optly = Helper.CreatePrivateOptimizely();

            bool result = (bool)optly.Invoke("ValidateStringInputs", new Dictionary<string, string>
            {
                {
                    Optimizely.USER_ID, "testUser"
                },
            });
            Assert.True(result);

            result = (bool)optly.Invoke("ValidateStringInputs", new Dictionary<string, string>
            {
                {
                    Optimizely.USER_ID, ""
                },
            });
            Assert.True(result);

            result = (bool)optly.Invoke("ValidateStringInputs", new Dictionary<string, string>
            {
                {
                    Optimizely.USER_ID, null
                },
            });
            Assert.False(result);
        }

        [Test]
        public void TestActivateValidateInputValues()
        {
            // Verify that ValidateStringInputs does not log error for valid values.
            Variation variation = Optimizely.Activate("test_experiment", "test_user");
            LoggerMock.Verify(l => l.Log(LogLevel.ERROR, "Provided User Id is in invalid format."),
                Times.Never);
            LoggerMock.Verify(
                l => l.Log(LogLevel.ERROR, "Provided Experiment Key is in invalid format."),
                Times.Never);

            // Verify that ValidateStringInputs logs error for invalid values.
            variation = Optimizely.Activate("", null);
            LoggerMock.Verify(l => l.Log(LogLevel.ERROR, "Provided User Id is in invalid format."),
                Times.Once);
            LoggerMock.Verify(
                l => l.Log(LogLevel.ERROR, "Provided Experiment Key is in invalid format."),
                Times.Once);
        }

        [Test]
        public void TestGetVariationValidateInputValues()
        {
            // Verify that ValidateStringInputs does not log error for valid values.
            Variation variation = Optimizely.GetVariation("test_experiment", "test_user");
            LoggerMock.Verify(l => l.Log(LogLevel.ERROR, "Provided User Id is in invalid format."),
                Times.Never);
            LoggerMock.Verify(
                l => l.Log(LogLevel.ERROR, "Provided Experiment Key is in invalid format."),
                Times.Never);

            // Verify that ValidateStringInputs logs error for invalid values.
            variation = Optimizely.GetVariation("", null);
            LoggerMock.Verify(l => l.Log(LogLevel.ERROR, "Provided User Id is in invalid format."),
                Times.Once);
            LoggerMock.Verify(
                l => l.Log(LogLevel.ERROR, "Provided Experiment Key is in invalid format."),
                Times.Once);
        }

        [Test]
        public void TestTrackValidateInputValues()
        {
            // Verify that ValidateStringInputs does not log error for valid values.
            Optimizely.Track("purchase", "test_user");
            LoggerMock.Verify(l => l.Log(LogLevel.ERROR, "Provided User Id is in invalid format."),
                Times.Never);
            LoggerMock.Verify(
                l => l.Log(LogLevel.ERROR, "Provided Event Key is in invalid format."),
                Times.Never);

            // Verify that ValidateStringInputs logs error for invalid values.
            Optimizely.Track("", null);
            LoggerMock.Verify(l => l.Log(LogLevel.ERROR, "Provided User Id is in invalid format."),
                Times.Once);
            LoggerMock.Verify(
                l => l.Log(LogLevel.ERROR, "Provided Event Key is in invalid format."), Times.Once);
        }

        #endregion Test ValidateStringInputs

        #region Test Audience Match Types

        [Test]
        public void TestActivateWithTypedAudiences()
        {
            Variation variation = OptimizelyWithTypedAudiences.Activate("typed_audience_experiment",
                "user1", new UserAttributes
                {
                    {
                        "house", "Gryffindor"
                    },
                });

            Assert.AreEqual("A", variation.Key);
            EventDispatcherMock.Verify(dispatcher => dispatcher.DispatchEvent(It.IsAny<LogEvent>()),
                Times.Once);

            variation = OptimizelyWithTypedAudiences.Activate("typed_audience_experiment", "user1",
                new UserAttributes
                {
                    {
                        "lasers", 45.5
                    },
                });

            Assert.AreEqual("A", variation.Key);
            EventDispatcherMock.Verify(dispatcher => dispatcher.DispatchEvent(It.IsAny<LogEvent>()),
                Times.Exactly(2));
        }

        [Test]
        public void TestActivateExcludeUserFromExperimentWithTypedAudiences()
        {
            Variation variation = OptimizelyWithTypedAudiences.Activate("typed_audience_experiment",
                "user1", new UserAttributes
                {
                    {
                        "house", "Hufflepuff"
                    },
                });

            Assert.Null(variation);
            EventDispatcherMock.Verify(dispatcher => dispatcher.DispatchEvent(It.IsAny<LogEvent>()),
                Times.Never);
        }

        [Test]
        public void TestTrackWithTypedAudiences()
        {
            OptimizelyWithTypedAudiences.Track("item_bought", "user1", new UserAttributes
            {
                {
                    "house", "Welcome to Slytherin!"
                },
            });

            EventDispatcherMock.Verify(dispatcher => dispatcher.DispatchEvent(It.IsAny<LogEvent>()),
                Times.Once);
        }

        [Test]
        public void
            TestTrackDoesNotExcludeUserFromExperimentWhenAttributesMismatchWithTypedAudiences()
        {
            OptimizelyWithTypedAudiences.Track("item_bought", "user1", new UserAttributes
            {
                {
                    "house", "Hufflepuff"
                },
            });

            EventDispatcherMock.Verify(dispatcher => dispatcher.DispatchEvent(It.IsAny<LogEvent>()),
                Times.Once);
        }

        [Test]
        public void TestIsFeatureEnabledWithTypedAudiences()
        {
            bool featureEnabled = OptimizelyWithTypedAudiences.IsFeatureEnabled("feat_no_vars",
                "user1", new UserAttributes
                {
                    {
                        "favorite_ice_cream", "chocolate"
                    },
                });

            Assert.True(featureEnabled);

            featureEnabled = OptimizelyWithTypedAudiences.IsFeatureEnabled("feat_no_vars", "user1",
                new UserAttributes
                {
                    {
                        "lasers", 45.5
                    },
                });

            Assert.True(featureEnabled);
        }

        [Test]
        public void TestIsFeatureEnabledExcludeUserFromExperimentWithTypedAudiences()
        {
            bool featureEnabled = OptimizelyWithTypedAudiences.IsFeatureEnabled("feat", "user1",
                new UserAttributes
                    { });
            Assert.False(featureEnabled);
        }

        [Test]
        public void TestGetFeatureVariableStringReturnVariableValueWithTypedAudiences()
        {
            string variableValue = OptimizelyWithTypedAudiences.GetFeatureVariableString(
                "feat_with_var", "x", "user1", new UserAttributes
                {
                    {
                        "lasers", 71
                    },
                });

            Assert.AreEqual(variableValue, "xyz");

            variableValue = OptimizelyWithTypedAudiences.GetFeatureVariableString("feat_with_var",
                "x", "user1", new UserAttributes
                {
                    {
                        "should_do_it", true
                    },
                });

            Assert.AreEqual(variableValue, "xyz");
        }

        [Test]
        public void TestGetFeatureVariableStringReturnDefaultVariableValueWithTypedAudiences()
        {
            string variableValue = OptimizelyWithTypedAudiences.GetFeatureVariableString(
                "feat_with_var", "x", "user1", new UserAttributes
                {
                    {
                        "lasers", 50
                    },
                });

            Assert.AreEqual(variableValue, "x");
        }

        #endregion Test Audience Match Types

        #region Test Audience Combinations

        [Test]
        public void TestActivateIncludesUserInExperimentWithComplexAudienceConditions()
        {
            UserAttributes userAttributes = new UserAttributes
            {
                {
                    "house", "Welcome to Slytherin!"
                },
                {
                    "lasers", 45.5
                },
            };

            // Should be included via substring match string audience with id '3988293898' and exact match number audience with id '3468206646'
            Variation variation =
                OptimizelyWithTypedAudiences.Activate("audience_combinations_experiment", "user1",
                    userAttributes);
            Assert.AreEqual("A", variation.Key);

            EventDispatcherMock.Verify(dispatcher => dispatcher.DispatchEvent(It.IsAny<LogEvent>()),
                Times.Once);
        }

        [Test]
        public void TestActivateExcludesUserFromExperimentWithComplexAudienceConditions()
        {
            UserAttributes userAttributes = new UserAttributes
            {
                {
                    "house", "Hufflepuff"
                },
                {
                    "lasers", 45.5
                },
            };

            // Should be excluded as substring audience with id '3988293898' does not match, so the overall conditions fail.
            Variation variation =
                OptimizelyWithTypedAudiences.Activate("audience_combinations_experiment", "user1",
                    userAttributes);
            Assert.Null(variation);

            EventDispatcherMock.Verify(dispatcher => dispatcher.DispatchEvent(It.IsAny<LogEvent>()),
                Times.Never);
        }

        [Test]
        public void TestTrackIncludesUserInExperimentWithComplexAudienceConditions()
        {
            UserAttributes userAttributes = new UserAttributes
            {
                {
                    "house", "Gryffindor"
                },
                {
                    "should_do_it", true
                },
            };

            // Should be included via exact match string audience with id '3468206642' and exact match boolean audience with id '3468206646'
            OptimizelyWithTypedAudiences.Track("user_signed_up", "user1", userAttributes);

            EventDispatcherMock.Verify(dispatcher => dispatcher.DispatchEvent(It.IsAny<LogEvent>()),
                Times.Once);
        }

        [Test]
        public void
            TestTrackDoesNotExcludesUserFromExperimentWhenAttributesMismatchWithAudienceConditions()
        {
            UserAttributes userAttributes = new UserAttributes
            {
                {
                    "house", "Gryffindor"
                },
                {
                    "should_do_it", false
                },
            };

            // Should be excluded as exact match boolean audience with id '3468206643' does not match so the overall conditions fail.
            OptimizelyWithTypedAudiences.Track("user_signed_up", "user1", userAttributes);

            EventDispatcherMock.Verify(dispatcher => dispatcher.DispatchEvent(It.IsAny<LogEvent>()),
                Times.Once);
        }

        [Test]
        public void TestIsFeatureEnabledIncludesUserInRolloutWithComplexAudienceConditions()
        {
            UserAttributes userAttributes = new UserAttributes
            {
                {
                    "house", "Welcome to Slytherin!"
                },
                {
                    "favorite_ice_cream", "walls"
                },
            };

            // Should be included via substring match string audience with id '3988293898' and exists audience with id '3988293899'
            bool result =
                OptimizelyWithTypedAudiences.IsFeatureEnabled("feat2", "user1", userAttributes);
            Assert.True(result);
        }

        [Test]
        public void TestIsFeatureEnabledExcludesUserFromRolloutWithComplexAudienceConditions()
        {
            UserAttributes userAttributes = new UserAttributes
            {
                {
                    "house", "Ravenclaw"
                },
                {
                    "lasers", 45.5
                },
            };

            // Should be excluded - substring match string audience with id '3988293898' does not match,
            // and no audience in the other branch of the 'and' matches either
            bool result =
                OptimizelyWithTypedAudiences.IsFeatureEnabled("audience_combinations_experiment",
                    "user1", userAttributes);
            Assert.False(result);
        }

        [Test]
        public void TestGetFeatureVariableIntegerReturnsVariableValueWithComplexAudienceConditions()
        {
            UserAttributes userAttributes = new UserAttributes
            {
                {
                    "house", "Gryffindor"
                },
                {
                    "lasers", 700
                },
            };

            // Should be included via substring match string audience with id '3988293898' and exists audience with id '3988293899'
            int? value =
                OptimizelyWithTypedAudiences.GetFeatureVariableInteger("feat2_with_var", "z",
                    "user1", userAttributes);
            Assert.AreEqual(150, value);
        }

        [Test]
        public void TestGetFeatureVariableIntegerReturnsDefaultValueWithComplexAudienceConditions()
        {
            UserAttributes userAttributes = new UserAttributes
                { };

            // Should be excluded - no audiences match with no attributes.
            int? value =
                OptimizelyWithTypedAudiences.GetFeatureVariableInteger("feat2_with_var", "z",
                    "user1", userAttributes);
            Assert.AreEqual(10, value);
        }

        #endregion Test Audience Combinations

        #region Disposable Optimizely

        [Test]
        public void TestOptimizelyDisposeAlsoDisposedConfigManager()
        {
            HttpProjectConfigManager httpManager = new HttpProjectConfigManager.Builder()
                .WithSdkKey("QBw9gFM8oTn7ogY9ANCC1z")
                .WithLogger(LoggerMock.Object)
                .WithPollingInterval(TimeSpan.FromMilliseconds(5000))
                .WithBlockingTimeoutPeriod(TimeSpan.FromMilliseconds(500))
                .Build();
            Optimizely optimizely = new Optimizely(httpManager);
            optimizely.Dispose();

            Assert.True(optimizely.Disposed);
            Assert.True(httpManager.Disposed);
        }

        [Test]
        public void TestDisposeInvalidateObject()
        {
            HttpProjectConfigManager httpManager = new HttpProjectConfigManager.Builder()
                .WithSdkKey("QBw9gFM8oTn7ogY9ANCC1z")
                .WithLogger(LoggerMock.Object)
                .WithPollingInterval(TimeSpan.FromMilliseconds(5000))
                .WithBlockingTimeoutPeriod(TimeSpan.FromMilliseconds(500))
                .Build();
            Optimizely optimizely = new Optimizely(httpManager);
            optimizely.Dispose();

            Assert.False(optimizely.IsValid);
        }

        [Test]
        public void TestAfterDisposeAPIsNoLongerValid()
        {
            HttpProjectConfigManager httpManager = new HttpProjectConfigManager.Builder()
                .WithSdkKey("QBw9gFM8oTn7ogY9ANCC1z")
                .WithDatafile(TestData.Datafile)
                .WithLogger(LoggerMock.Object)
                .WithPollingInterval(TimeSpan.FromMilliseconds(50000))
                .WithBlockingTimeoutPeriod(TimeSpan.FromMilliseconds(500))
                .Build(true);
            Optimizely optimizely = new Optimizely(httpManager);
            httpManager.Start();
            Variation activate = optimizely.Activate("test_experiment", TestUserId,
                new UserAttributes()
                {
                    {
                        "device_type", "iPhone"
                    },
                    {
                        "location", "San Francisco"
                    },
                });
            Assert.NotNull(activate);
            optimizely.Dispose();
            Variation activateAfterDispose = optimizely.Activate("test_experiment", TestUserId,
                new UserAttributes()
                {
                    {
                        "device_type", "iPhone"
                    },
                    {
                        "location", "San Francisco"
                    },
                });
            Assert.Null(activateAfterDispose);
            httpManager.Dispose();
        }

        [Test]
        public void TestNonDisposableConfigManagerDontCrash()
        {
            FallbackProjectConfigManager fallbackConfigManager =
                new FallbackProjectConfigManager(Config);

            Optimizely optimizely = new Optimizely(fallbackConfigManager);
            optimizely.Dispose();
            Assert.True(optimizely.Disposed);
        }

        [Test]
        public void TestAfterDisposeAPIsShouldNotCrash()
        {
            FallbackProjectConfigManager fallbackConfigManager =
                new FallbackProjectConfigManager(Config);

            Optimizely optimizely = new Optimizely(fallbackConfigManager);
            optimizely.Dispose();
            Assert.True(optimizely.Disposed);

            Assert.IsNull(optimizely.GetVariation(string.Empty, string.Empty));
            Assert.IsNull(optimizely.Activate(string.Empty, string.Empty));
            optimizely.Track(string.Empty, string.Empty);
            Assert.IsFalse(optimizely.IsFeatureEnabled(string.Empty, string.Empty));
            Assert.AreEqual(optimizely.GetEnabledFeatures(string.Empty).Count, 0);
            Assert.IsNull(
                optimizely.GetFeatureVariableBoolean(string.Empty, string.Empty, string.Empty));
            Assert.IsNull(
                optimizely.GetFeatureVariableString(string.Empty, string.Empty, string.Empty));
            Assert.IsNull(
                optimizely.GetFeatureVariableDouble(string.Empty, string.Empty, string.Empty));
            Assert.IsNull(
                optimizely.GetFeatureVariableInteger(string.Empty, string.Empty, string.Empty));
        }

        #endregion Disposable Optimizely

        #region Test GetOptimizelyConfig

        [Test]
        public void TestGetOptimizelyConfigNullConfig()
        {
            Optimizely optly = new Optimizely(new FallbackProjectConfigManager(null));
            OptimizelyConfig optimizelyConfig = optly.GetOptimizelyConfig();

            Assert.IsNull(optimizelyConfig);
        }

        // Test that OptimizelyConfig.Datafile returns the expected datafile, which was used to generate project config
        [Test]
        public void TestGetOptimizelyConfigDatafile()
        {
            OptimizelyConfig optimizelyConfig = Optimizely.GetOptimizelyConfig();
            Assert.AreEqual(optimizelyConfig.GetDatafile(), TestData.Datafile);
        }

        #endregion Test GetOptimizelyConfig

        #region Test Culture

        public static void SetCulture(string culture)
        {
            CultureInfo ci1 = new CultureInfo(culture);
            Thread.CurrentThread.CurrentCulture = ci1;
            Thread.CurrentThread.CurrentUICulture = ci1;
        }

        #endregion Test Culture
    }
}
