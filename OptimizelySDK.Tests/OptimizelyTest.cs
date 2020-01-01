﻿/* 
 * Copyright 2017-2019, Optimizely
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
using System;
using System.Collections.Generic;
using Moq;
using OptimizelySDK.Logger;
using OptimizelySDK.Event.Builder;
using OptimizelySDK.Event.Dispatcher;
using OptimizelySDK.ErrorHandler;
using OptimizelySDK.Exceptions;
using OptimizelySDK.Event;
using OptimizelySDK.Entity;
using NUnit.Framework;
using OptimizelySDK.Tests.UtilsTests;
using OptimizelySDK.Bucketing;
using OptimizelySDK.Notifications;
using OptimizelySDK.Tests.NotificationTests;
using OptimizelySDK.Utils;
using OptimizelySDK.Config;
using OptimizelySDK.Event.Entity;
using OptimizelySDK.OptlyConfig;

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

        const string FEATUREVARIABLE_BOOLEANTYPE = "boolean";
        const string FEATUREVARIABLE_INTEGERTYPE = "integer";
        const string FEATUREVARIABLE_DOUBLETYPE = "double";
        const string FEATUREVARIABLE_STRINGTYPE = "string";

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
            
            var config = DatafileProjectConfig.Create(
                content: TestData.Datafile,
                logger: LoggerMock.Object,
                errorHandler: new NoOpErrorHandler());
            ConfigManager = new FallbackProjectConfigManager(config);
            Config = ConfigManager.GetConfig();
            EventDispatcherMock = new Mock<IEventDispatcher>();
            Optimizely = new Optimizely(TestData.Datafile, EventDispatcherMock.Object, LoggerMock.Object, ErrorHandlerMock.Object);
            OptimizelyWithTypedAudiences = new Optimizely(TestData.TypedAudienceDatafile, EventDispatcherMock.Object, LoggerMock.Object, ErrorHandlerMock.Object);

            Helper = new OptimizelyHelper
            {
                Datafile = TestData.Datafile,
                EventDispatcher = EventDispatcherMock.Object,
                Logger = LoggerMock.Object,
                ErrorHandler = ErrorHandlerMock.Object,
                SkipJsonValidation = false,
            };

            OptimizelyMock = new Mock<Optimizely>(TestData.Datafile, EventDispatcherMock.Object, LoggerMock.Object, ErrorHandlerMock.Object, null, false, null)
            {
                CallBase = true
            };

            DecisionServiceMock = new Mock<DecisionService>(new Bucketer(LoggerMock.Object), ErrorHandlerMock.Object,
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
        #endregion
        
        #region OptimizelyHelper
        private class OptimizelyHelper
        {
            static Type[] ParameterTypes = {
                typeof(string),
                typeof(IEventDispatcher),
                typeof(ILogger),
                typeof(IErrorHandler),
                typeof(bool),
                typeof(EventProcessor)
            };

            public static Dictionary<string, object> SingleParameter = new Dictionary<string, object>
            {
                { "param1", "val1" }
            };

            public static UserAttributes UserAttributes = new UserAttributes
            {
                { "device_type", "iPhone" },
                { "company", "Optimizely" },
                { "location", "San Francisco" }
            };

            // NullUserAttributes extends copy of UserAttributes with key-value
            // pairs containing null values which should not be sent to OPTIMIZELY.COM .
            public static UserAttributes NullUserAttributes = new UserAttributes
            {
                { "device_type", "iPhone" },
                { "company", "Optimizely" },
                { "location", "San Francisco" },
                { "null_value", null},
                { "wont_be_sent", null},
                { "bad_food", null}
            };

            public string Datafile { get; set; }
            public IEventDispatcher EventDispatcher { get; set; }
            public ILogger Logger { get; set; }
            public IErrorHandler ErrorHandler { get; set; }
            public UserProfileService UserProfileService { get; set; }
            public bool SkipJsonValidation { get; set; }
            public EventProcessor EventProcessor { get; set; }

            public PrivateObject CreatePrivateOptimizely()
            {
                return new PrivateObject(typeof(Optimizely), ParameterTypes,
                    new object[]
                    {
                        Datafile,
                        EventDispatcher,
                        Logger,
                        ErrorHandler,
                        UserProfileService,
                        SkipJsonValidation,
                        EventProcessor
                    });
            }
        }
        #endregion

        #region Test Validate
        [Test]
        public void TestInvalidInstanceLogMessages()
        {
            string datafile = "{\"name\":\"optimizely\"}";
            var optimizely = new Optimizely(datafile, null, LoggerMock.Object);

            Assert.IsNull(optimizely.GetVariation(string.Empty, string.Empty));
            Assert.IsNull(optimizely.Activate(string.Empty, string.Empty));
            optimizely.Track(string.Empty, string.Empty);
            Assert.IsFalse(optimizely.IsFeatureEnabled(string.Empty, string.Empty));
            Assert.AreEqual(optimizely.GetEnabledFeatures(string.Empty).Count, 0);
            Assert.IsNull(optimizely.GetFeatureVariableBoolean(string.Empty, string.Empty, string.Empty));
            Assert.IsNull(optimizely.GetFeatureVariableString(string.Empty, string.Empty, string.Empty));
            Assert.IsNull(optimizely.GetFeatureVariableDouble(string.Empty, string.Empty, string.Empty));
            Assert.IsNull(optimizely.GetFeatureVariableInteger(string.Empty, string.Empty, string.Empty));

            LoggerMock.Verify(log => log.Log(LogLevel.ERROR, "Provided 'datafile' has invalid schema."), Times.Once);
            LoggerMock.Verify(log => log.Log(LogLevel.ERROR, "Datafile has invalid format. Failing 'GetVariation'."), Times.Once);
            LoggerMock.Verify(log => log.Log(LogLevel.ERROR, "Datafile has invalid format. Failing 'Activate'."), Times.Once);
            LoggerMock.Verify(log => log.Log(LogLevel.ERROR, "Datafile has invalid format. Failing 'Track'."), Times.Once);
            LoggerMock.Verify(log => log.Log(LogLevel.ERROR, "Datafile has invalid format. Failing 'IsFeatureEnabled'."), Times.Once);
            LoggerMock.Verify(log => log.Log(LogLevel.ERROR, "Datafile has invalid format. Failing 'GetEnabledFeatures'."), Times.Once);
            LoggerMock.Verify(log => log.Log(LogLevel.ERROR, "Datafile has invalid format. Failing 'GetFeatureVariableBoolean'."), Times.Once);
            LoggerMock.Verify(log => log.Log(LogLevel.ERROR, "Datafile has invalid format. Failing 'GetFeatureVariableString'."), Times.Once);
            LoggerMock.Verify(log => log.Log(LogLevel.ERROR, "Datafile has invalid format. Failing 'GetFeatureVariableDouble'."), Times.Once);
            LoggerMock.Verify(log => log.Log(LogLevel.ERROR, "Datafile has invalid format. Failing 'GetFeatureVariableInteger'."), Times.Once);

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
            Optimizely optimizely = new Optimizely(datafile, null, null, null, skipJsonValidation: true);
            Assert.IsFalse(optimizely.IsValid);
        }

        [Test]
        public void TestErrorHandlingWithNullDatafile()
        {
            var optimizelyNullDatafile = new Optimizely(null, null, LoggerMock.Object, ErrorHandlerMock.Object, null, true);
            LoggerMock.Verify(l => l.Log(LogLevel.ERROR, "Unable to parse null datafile."), Times.Once);
            ErrorHandlerMock.Verify(e => e.HandleError(It.Is<ConfigParseException>(ex => ex.Message == "Unable to parse null datafile.")), Times.Once);
        }

        [Test]
        public void TestErrorHandlingWithEmptyDatafile()
        {
            var optimizelyEmptyDatafile = new Optimizely("", null, LoggerMock.Object, ErrorHandlerMock.Object, null, true);
            LoggerMock.Verify(l => l.Log(LogLevel.ERROR, "Unable to parse empty datafile."), Times.Once);
            ErrorHandlerMock.Verify(e => e.HandleError(It.Is<ConfigParseException>(ex => ex.Message == "Unable to parse empty datafile.")), Times.Once);
        }

        [Test]
        public void TestErrorHandlingWithUnsupportedConfigVersion()
        {
            var optimizelyUnsupportedVersion = new Optimizely(TestData.UnsupportedVersionDatafile, null, LoggerMock.Object, ErrorHandlerMock.Object, null, true);
            LoggerMock.Verify(l => l.Log(LogLevel.ERROR, $"This version of the C# SDK does not support the given datafile version: 5"), Times.Once);
            ErrorHandlerMock.Verify(e => e.HandleError(It.Is<ConfigParseException>(ex => ex.Message == $"This version of the C# SDK does not support the given datafile version: 5")), Times.Once);
        }

        [Test]
        public void TestValidatePreconditionsExperimentNotRunning()
        {
            var optly = Helper.CreatePrivateOptimizely();

            optly.SetFieldOrProperty("ProjectConfigManager", ConfigManager);

            bool result = (bool)optly.Invoke("ValidatePreconditions",
                Config.GetExperimentFromKey("paused_experiment"),
                TestUserId, ConfigManager.GetConfig(), new UserAttributes { });
            Assert.IsFalse(result);
        }

        [Test]
        public void TestValidatePreconditionsExperimentRunning()
        {
            var optly = Helper.CreatePrivateOptimizely();
            bool result = (bool)optly.Invoke("ValidatePreconditions",
                Config.GetExperimentFromKey("test_experiment"),
                TestUserId,
                ConfigManager.GetConfig(),
                new UserAttributes
                {
                    { "device_type", "iPhone" },
                    { "location", "San Francisco" }
                }
                );
            Assert.IsTrue(result);
        }

        [Test]
        public void TestValidatePreconditionsUserInForcedVariationNotInExperiment()
        {
            var optly = Helper.CreatePrivateOptimizely();
            bool result = (bool)optly.Invoke("ValidatePreconditions",
                Config.GetExperimentFromKey("test_experiment"),
                "user1", ConfigManager.GetConfig(), new UserAttributes { });
            Assert.IsTrue(result);
        }

        [Test]
        public void TestValidatePreconditionsUserInForcedVariationInExperiment()
        {
            var optly = Helper.CreatePrivateOptimizely();
            bool result = (bool)optly.Invoke("ValidatePreconditions",
                Config.GetExperimentFromKey("test_experiment"),
                "user1", ConfigManager.GetConfig(), new UserAttributes { });
            Assert.IsTrue(result);
        }

        [Test]
        public void TestValidatePreconditionsUserNotInForcedVariationNotInExperiment()
        {
            var optly = Helper.CreatePrivateOptimizely();
            bool result = (bool)optly.Invoke("ValidatePreconditions",
                Config.GetExperimentFromKey("test_experiment"),
                TestUserId, ConfigManager.GetConfig(), new UserAttributes { });
            Assert.IsFalse(result);
        }

        [Test]
        public void TestValidatePreconditionsUserNotInForcedVariationInExperiment()
        {
            var attributes = new UserAttributes
            {
                { "device_type", "iPhone" },
                { "location", "San Francisco" }

            };

            var variation = Optimizely.GetVariation("test_experiment", "test_user", attributes);
            
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "User \"test_user\" is not in the forced variation map."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "Assigned bucket [3037] to user [test_user] with bucketing ID [test_user]."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "User [test_user] is in variation [control] of experiment [test_experiment]."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "This decision will not be saved since the UserProfileService is null."), Times.Once);

            Assert.IsTrue(TestData.CompareObjects(VariationWithKeyControl, variation));
        }

        [Test]
        public void TestActivateInvalidOptimizelyObject()
        {
            var optly = new Optimizely("Random datafile", null, LoggerMock.Object);
            LoggerMock.Verify(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<string>()), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.ERROR, "Provided 'datafile' has invalid schema."), Times.Once);
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
            var optly = Helper.CreatePrivateOptimizely();            

            var result = optly.Invoke("Activate", "test_experiment", "not_in_variation_user", OptimizelyHelper.UserAttributes);            
            
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "Assigned bucket [8495] to user [not_in_variation_user] with bucketing ID [not_in_variation_user]."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "User [not_in_variation_user] is in no variation."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "Not activating user not_in_variation_user."), Times.Once);

            Assert.IsNull(result);
        }

        [Test]
        public void TestActivateNoAudienceNoAttributes()
        {
            var parameters = new Dictionary<string, object>
            {
                { "param1", "val1" },
                { "param2", "val2" }
            };

            
            var optly = Helper.CreatePrivateOptimizely();            
            optly.SetFieldOrProperty("EventProcessor", EventProcessorMock.Object);

            var variation = (Variation)optly.Invoke("Activate", "group_experiment_1", "user_1", null);
            
            EventProcessorMock.Verify(ep => ep.Process(It.IsAny<ImpressionEvent>()), Times.Once);            

            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "Assigned bucket [1922] to user [user_1] with bucketing ID [user_1]."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "User [user_1] is in experiment [group_experiment_1] of group [7722400015]."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "Assigned bucket [9525] to user [user_1] with bucketing ID [user_1]."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "User [user_1] is in variation [group_exp_1_var_2] of experiment [group_experiment_1]."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "Activating user user_1 in experiment group_experiment_1."), Times.Once);            

            Assert.IsTrue(TestData.CompareObjects(GroupVariation, variation));
        }
        #endregion

        #region Test Activate
        [Test]
        public void TestActivateAudienceNoAttributes()
        {
            var optly = Helper.CreatePrivateOptimizely();            

            var variationkey = optly.Invoke("Activate", "test_experiment", "test_user", null);
                        
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "User \"test_user\" does not meet conditions to be in experiment \"test_experiment\"."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "Not activating user test_user."), Times.Once);

            Assert.IsNull(variationkey);
        }

        [Test]
        public void TestActivateWithAttributes()
        {
           
            var optly = Helper.CreatePrivateOptimizely();            
            optly.SetFieldOrProperty("EventProcessor", EventProcessorMock.Object);

            var variation = (Variation)optly.Invoke("Activate", "test_experiment", "test_user", OptimizelyHelper.UserAttributes);
            
            EventProcessorMock.Verify(ep => ep.Process(It.IsAny<ImpressionEvent>()), Times.Once);

            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "User \"test_user\" is not in the forced variation map."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "Assigned bucket [3037] to user [test_user] with bucketing ID [test_user]."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "User [test_user] is in variation [control] of experiment [test_experiment]."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "This decision will not be saved since the UserProfileService is null."));
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "Activating user test_user in experiment test_experiment."), Times.Once);                        

            Assert.IsTrue(TestData.CompareObjects(VariationWithKeyControl, variation));
        }

        [Test]
        public void TestActivateWithNullAttributes()
        {
           
            var optly = Helper.CreatePrivateOptimizely();            
            optly.SetFieldOrProperty("EventProcessor", EventProcessorMock.Object);
            
            var variation = (Variation)optly.Invoke("Activate", "test_experiment", "test_user", OptimizelyHelper.NullUserAttributes);

            EventProcessorMock.Verify(ep => ep.Process(It.IsAny<ImpressionEvent>()), Times.Once);

            //"User "test_user" is not in the forced variation map."
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "User \"test_user\" is not in the forced variation map."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "Assigned bucket [3037] to user [test_user] with bucketing ID [test_user]."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "User [test_user] is in variation [control] of experiment [test_experiment]."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "Activating user test_user in experiment test_experiment."), Times.Once);

            Assert.IsTrue(TestData.CompareObjects(VariationWithKeyControl, variation));
        }

        [Test]
        public void TestActivateExperimentNotRunning()
        {
            var optly = Helper.CreatePrivateOptimizely();

            var variationkey = optly.Invoke("Activate", "paused_experiment", "test_user", null);            

            LoggerMock.Verify(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<string>()), Times.Exactly(2));
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "Experiment \"paused_experiment\" is not running."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "Not activating user test_user."), Times.Once);

            Assert.IsNull(variationkey);
        }

        [Test]
        public void TestActivateWithTypedAttributes()
        {
            var userAttributes = new UserAttributes
            {
                {"device_type", "iPhone" },
                {"location", "San Francisco" },
                {"boolean_key", true },
                {"integer_key", 15 },
                {"double_key", 3.14 }
            };

            var optly = Helper.CreatePrivateOptimizely();
            optly.SetFieldOrProperty("EventProcessor", EventProcessorMock.Object);

            var variation = (Variation)optly.Invoke("Activate", "test_experiment", "test_user", userAttributes);

            EventProcessorMock.Verify(ep => ep.Process(It.IsAny<ImpressionEvent>()), Times.Once);

            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "Assigned bucket [3037] to user [test_user] with bucketing ID [test_user]."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "User [test_user] is in variation [control] of experiment [test_experiment]."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "Activating user test_user in experiment test_experiment."), Times.Once);            

            Assert.IsTrue(TestData.CompareObjects(VariationWithKeyControl, variation));
        }
        #endregion

        #region Test GetVariation
        [Test]
        public void TestGetVariationInvalidOptimizelyObject()
        {
            var optly = new Optimizely("Random datafile", null, LoggerMock.Object);
            var variationkey = optly.Activate("some_experiment", "some_user");
            LoggerMock.Verify(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<string>()), Times.Exactly(2));
            LoggerMock.Verify(l => l.Log(LogLevel.ERROR, "Datafile has invalid format. Failing 'Activate'."), Times.Once);
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
            var attributes = new UserAttributes
            {
               { "device_type", "iPhone" },
               { "location", "San Francisco" }
            };

            var variation = Optimizely.GetVariation("test_experiment", "test_user", attributes);
            
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "Assigned bucket [3037] to user [test_user] with bucketing ID [test_user]."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "User [test_user] is in variation [control] of experiment [test_experiment]."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "This decision will not be saved since the UserProfileService is null."), Times.Once);

            Assert.IsTrue(TestData.CompareObjects(VariationWithKeyControl, variation));
        }

        [Test]
        public void TestGetVariationAudienceNoMatch()
        {
            var variation = Optimizely.Activate("test_experiment", "test_user");
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "User \"test_user\" does not meet conditions to be in experiment \"test_experiment\"."), Times.Once);
            Assert.IsNull(variation);
        }

        [Test]
        public void TestGetVariationExperimentNotRunning()
        {
            var variation = Optimizely.Activate("paused_experiment", "test_user");
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "Experiment \"paused_experiment\" is not running."), Times.Once);
            Assert.IsNull(variation);
        }

        [Test]
        public void TestTrackInvalidOptimizelyObject()
        {
            var optly = new Optimizely("Random datafile", null, LoggerMock.Object);
            optly.Track("some_event", "some_user");
            LoggerMock.Verify(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<string>()), Times.Exactly(2));
            LoggerMock.Verify(l => l.Log(LogLevel.ERROR, "Datafile has invalid format. Failing 'Track'."), Times.Once);
        }
        #endregion

        #region Test Track
        [Test]
        public void TestTrackInvalidAttributes()
        {
            var attributes = new UserAttributes
            {
                { "abc", "43" }
            };

            Optimizely.Track("purchase", TestUserId, attributes);

            LoggerMock.Verify(l => l.Log(LogLevel.ERROR, @"Attribute key ""abc"" is not in datafile."), Times.Once);
        }

        [Test]
        public void TestTrackNoAttributesNoEventValue()
        {
            var optly = Helper.CreatePrivateOptimizely();
            optly.SetFieldOrProperty("EventProcessor", EventProcessorMock.Object);

            optly.Invoke("Track", "purchase", "test_user", null, null);            
            EventProcessorMock.Verify(processor => processor.Process(It.IsAny<ConversionEvent>()), Times.Once);            

            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "Tracking event purchase for user test_user."), Times.Once);            
        }

        [Test]
        public void TestTrackWithAttributesNoEventValue()
        {
            var optly = Helper.CreatePrivateOptimizely();            
            optly.SetFieldOrProperty("EventProcessor", EventProcessorMock.Object);

            optly.Invoke("Track", "purchase", "test_user", OptimizelyHelper.UserAttributes, null);
            EventProcessorMock.Verify(ep => ep.Process(It.IsAny<ConversionEvent>()), Times.Once);

            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "Tracking event purchase for user test_user."), Times.Once);            
        }

        [Test]
        public void TestTrackUnknownEventKey()
        {
            Optimizely.Track("unknown_event", "test_user");

            LoggerMock.Verify(l => l.Log(LogLevel.ERROR, "Event key \"unknown_event\" is not in datafile."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "Not tracking user test_user for event unknown_event."), Times.Once);
        }

        [Test]
        public void TestTrackNoAttributesWithEventValue()
        {
            var optly = Helper.CreatePrivateOptimizely();            
            optly.SetFieldOrProperty("EventProcessor", EventProcessorMock.Object);
            
            optly.Invoke("Track", "purchase", "test_user", null, new EventTags
            {
                { "revenue", 42 }
            });
            EventProcessorMock.Verify(ep => ep.Process(It.IsAny<ConversionEvent>()), Times.Once);

            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "Tracking event purchase for user test_user."), Times.Once);            
        }

        [Test]
        public void TestTrackWithAttributesWithEventValue()
        {
            var attributes = new UserAttributes
            {
                { "device_type", "iPhone" },
                { "company", "Optimizely" },
            };

            var optly = Helper.CreatePrivateOptimizely();
            optly.SetFieldOrProperty("EventProcessor", EventProcessorMock.Object);
                        
            optly.Invoke("Track", "purchase", "test_user", attributes, new EventTags
            {
                { "revenue", 42 }
            });

            EventProcessorMock.Verify(ep => ep.Process(It.IsAny<ConversionEvent>()), Times.Once);

            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "Tracking event purchase for user test_user."), Times.Once);            
        }

        [Test]
        public void TestTrackWithNullAttributesWithNullEventValue()
        {
            var optly = Helper.CreatePrivateOptimizely();
            optly.SetFieldOrProperty("EventProcessor", EventProcessorMock.Object);
            

            optly.Invoke("Track", "purchase", "test_user", OptimizelyHelper.NullUserAttributes, new EventTags
            {
                { "revenue", 42 },
                { "wont_send_null", null}
            });

            EventProcessorMock.Verify(ep => ep.Process(It.IsAny<ConversionEvent>()), Times.Once);            

            LoggerMock.Verify(l => l.Log(LogLevel.ERROR, "[EventTags] Null value for key wont_send_null removed and will not be sent to results."));
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "Tracking event purchase for user test_user."));            
        }
        #endregion

        #region Test Invalid Dispatch
        [Test]
        public void TestInvalidDispatchImpressionEvent()
        {
            var optly = Helper.CreatePrivateOptimizely();            
            optly.SetFieldOrProperty("EventDispatcher", new InvalidEventDispatcher());

            var variation = (Variation)optly.Invoke("Activate", "test_experiment", "test_user", OptimizelyHelper.UserAttributes);

            EventDispatcherMock.Verify(dispatcher => dispatcher.DispatchEvent(It.IsAny<LogEvent>()), Times.Once);

            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "Assigned bucket [3037] to user [test_user] with bucketing ID [test_user]."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "User [test_user] is in variation [control] of experiment [test_experiment]."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "Activating user test_user in experiment test_experiment."), Times.Once);
            
            // Need to see how error handler can be verified.
            LoggerMock.Verify(l => l.Log(LogLevel.ERROR, It.IsAny<string>()), Times.Once);

            Assert.IsTrue(TestData.CompareObjects(VariationWithKeyControl, variation));
        }

        [Test]
        public void TestInvalidDispatchConversionEvent()
        {
            var optly = Helper.CreatePrivateOptimizely();            
            optly.SetFieldOrProperty("EventDispatcher", new InvalidEventDispatcher());

            optly.Invoke("Track", "purchase", "test_user", null, null);

            EventDispatcherMock.Verify(dispatcher => dispatcher.DispatchEvent(It.IsAny<LogEvent>()), Times.Once);

            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "Tracking event purchase for user test_user."), Times.Once);            
        }
        #endregion

        #region Test Misc
        /* Start 1 */
        public void TestTrackNoAttributesWithInvalidEventValue()
        {            
            var optly = Helper.CreatePrivateOptimizely();            
            optly.SetFieldOrProperty("EventDispatcher", new ValidEventDispatcher());

            optly.Invoke("Track", "purchase", "test_user", null, new Dictionary<string, object>
            {
                {"revenue", 4200 }
            });
        }

        public void TestTrackNoAttributesWithDeprecatedEventValue()
        {
            /* Note: This case is not applicable, C# only accepts what the datatype we provide. 
             * In this case, int value can't be casted implicitly into Dictionary */
            
            var optly = Helper.CreatePrivateOptimizely();            
            optly.SetFieldOrProperty("EventDispatcher", new ValidEventDispatcher());
            optly.Invoke("Track", "purchase", "test_user", null, new Dictionary<string, object>
            {
                {"revenue", 42 }
            });
        }
        
        [Test]
        public void TestForcedVariationPreceedsWhitelistedVariation()
        {
            var optimizely = new Optimizely(TestData.Datafile, EventDispatcherMock.Object, LoggerMock.Object, ErrorHandlerMock.Object);
            var projectConfig = DatafileProjectConfig.Create(TestData.Datafile, LoggerMock.Object, ErrorHandlerMock.Object);
            Variation expectedVariation1 = projectConfig.GetVariationFromKey("etag3", "vtag5");
            Variation expectedVariation2 = projectConfig.GetVariationFromKey("etag3", "vtag6");

            //Check whitelisted experiment
            var variation = optimizely.GetVariation("etag3", "testUser1");
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

            LoggerMock.Verify(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<string>()), Times.Exactly(7));
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "User \"testUser1\" is not in the forced variation map."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "User \"testUser1\" is forced in variation \"vtag5\"."), Times.Exactly(2));
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "Set variation \"281\" for experiment \"224\" and user \"testUser1\" in the forced variation map."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "Variation \"vtag6\" is mapped to experiment \"etag3\" and user \"testUser1\" in the forced variation map"), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "Variation mapped to experiment \"etag3\" has been removed for user \"testUser1\"."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "No experiment \"etag3\" mapped to user \"testUser1\" in the forced variation map."), Times.Once);
        }

        [Test]
        public void TestForcedVariationPreceedsUserProfile()
        {
            var userProfileServiceMock = new Mock<UserProfileService>();
            var experimentKey = "etag1";
            var userId = "testUser3";
            var variationKey = "vtag2";
            var fbVariationKey = "vtag1";
            

            UserProfile userProfile = new UserProfile(userId, new Dictionary<string, Bucketing.Decision>
            {
                { experimentKey, new Bucketing.Decision(variationKey)}
            });

            userProfileServiceMock.Setup(_ => _.Lookup(userId)).Returns(userProfile.ToMap());

            var optimizely = new Optimizely(TestData.Datafile, EventDispatcherMock.Object, LoggerMock.Object, ErrorHandlerMock.Object, userProfileServiceMock.Object);
            var projectConfig = DatafileProjectConfig.Create(TestData.Datafile, LoggerMock.Object, ErrorHandlerMock.Object);
            Variation expectedFbVariation = projectConfig.GetVariationFromKey(experimentKey, fbVariationKey);
            Variation expectedVariation = projectConfig.GetVariationFromKey(experimentKey, variationKey);

            var variationUserProfile = optimizely.GetVariation(experimentKey, userId);
            Assert.IsTrue(TestData.CompareObjects(expectedVariation, variationUserProfile));

            //assign same user with different variation, forced variation have higher priority
            Assert.IsTrue(optimizely.SetForcedVariation(experimentKey, userId, fbVariationKey));

            var variation2 = optimizely.GetVariation(experimentKey, userId);
            Assert.IsTrue(TestData.CompareObjects(expectedFbVariation, variation2));
            
            //remove forced variation and re-check userprofile
            Assert.IsTrue(optimizely.SetForcedVariation(experimentKey, userId, null));

            variationUserProfile = optimizely.GetVariation(experimentKey, userId);
            Assert.IsTrue(TestData.CompareObjects(expectedVariation, variationUserProfile));
            
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "User \"testUser3\" is not in the forced variation map."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "No previously activated variation of experiment \"etag1\" for user \"testUser3\" found in user profile."), Times.Exactly(2));
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "Assigned bucket [4969] to user [testUser3] with bucketing ID [testUser3]."), Times.Exactly(2));
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "User [testUser3] is in variation [vtag2] of experiment [etag1]."), Times.Exactly(2));
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "Saved variation \"277\" of experiment \"223\" for user \"testUser3\"."), Times.Exactly(2));
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "Set variation \"276\" for experiment \"223\" and user \"testUser3\" in the forced variation map."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "Variation mapped to experiment \"etag1\" has been removed for user \"testUser3\"."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "No experiment \"etag1\" mapped to user \"testUser3\" in the forced variation map."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "User \"testUser3\" is not in the forced variation map."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "User \"testUser3\" is not in the forced variation map."), Times.Once);
        }

        // check that a null variation key clears the forced variation
        [Test]
        public void TestSetForcedVariationNullVariation()
        {
            var expectedForcedVariationKey = "variation";
            var experimentKey = "test_experiment";

            var userAttributes = new UserAttributes
            {
                {"device_type", "iPhone" },
                {"location", "San Francisco" }
            };

            Optimizely.Activate(experimentKey, TestUserId, userAttributes);

            // set variation
            Assert.IsTrue(Optimizely.SetForcedVariation(experimentKey, TestUserId, expectedForcedVariationKey), "Set forced variation to variation failed.");

            var actualForcedVariation = Optimizely.GetVariation(experimentKey, TestUserId, userAttributes);
            Assert.IsTrue(TestData.CompareObjects(VariationWithKeyVariation, actualForcedVariation), string.Format(@"Forced variation key should be variation, but got ""{0}"".", actualForcedVariation?.Key));

            // clear variation and check that the user gets bucketed normally
            Assert.IsTrue(Optimizely.SetForcedVariation(experimentKey, TestUserId, null), "Clear forced variation failed.");

            var actualVariation = Optimizely.GetVariation("test_experiment", "test_user", userAttributes);
            Assert.IsTrue(TestData.CompareObjects(VariationWithKeyControl, actualVariation), string.Format(@"Variation key should be control, but got ""{0}"".", actualVariation?.Key));
        }

        // check that the forced variation is set correctly
        [Test]
        public void TestSetForcedVariation()
        {
            var experimentKey = "test_experiment";
            var expectedForcedVariationKey = "variation";

            var userAttributes = new UserAttributes
            {
                {"device_type", "iPhone" },
                {"location", "San Francisco" }
            };

            Optimizely.Activate(experimentKey, TestUserId, userAttributes);

            // test invalid experiment -. normal bucketing should occur
            Assert.IsFalse(Optimizely.SetForcedVariation("bad_experiment", TestUserId, "bad_control"), "Set variation to 'variation' should have failed  because of invalid experiment.");

            var variation = Optimizely.GetVariation(experimentKey, TestUserId, userAttributes);
            Assert.IsTrue(TestData.CompareObjects(VariationWithKeyControl, variation));

            // test invalid variation -. normal bucketing should occur
            Assert.IsFalse(Optimizely.SetForcedVariation("test_experiment", TestUserId, "bad_variation"), "Set variation to 'bad_variation' should have failed.");

            variation = Optimizely.GetVariation("test_experiment", "test_user", userAttributes);
            Assert.IsTrue(TestData.CompareObjects(VariationWithKeyControl, variation));

            // test valid variation -. the user should be bucketed to the specified forced variation
            Assert.IsTrue(Optimizely.SetForcedVariation(experimentKey, TestUserId, expectedForcedVariationKey), "Set variation to 'variation' failed.");

            var actualForcedVariation = Optimizely.GetVariation(experimentKey, TestUserId, userAttributes);
            Assert.IsTrue(TestData.CompareObjects(VariationWithKeyVariation, actualForcedVariation));

            // make sure another setForcedVariation call sets a new forced variation correctly
            Assert.IsTrue(Optimizely.SetForcedVariation(experimentKey, "test_user2", expectedForcedVariationKey), "Set variation to 'variation' failed.");
            actualForcedVariation = Optimizely.GetVariation(experimentKey, "test_user2", userAttributes);

            Assert.IsTrue(TestData.CompareObjects(VariationWithKeyVariation, actualForcedVariation));
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
            var userId = "test_user";
            var variation = "variation";

            Assert.False(Optimizely.SetForcedVariation("test_experiment_not_in_datafile", userId, variation));
            Assert.False(Optimizely.SetForcedVariation("", userId, variation));
            Assert.False(Optimizely.SetForcedVariation(null, userId, variation));
        }

        [Test]
        public void TestSetForcedVariationWithInvalidVariationKey()
        {
            var userId = "test_user";
            var experimentKey = "test_experiment";

            Assert.False(Optimizely.SetForcedVariation(experimentKey, userId, "variation_not_in_datafile"));
            Assert.False(Optimizely.SetForcedVariation(experimentKey, userId, ""));
        }

        // check that the get forced variation is correct.
        [Test]
        public void TestGetForcedVariation()
        {
            var experimentKey = "test_experiment";
            var expectedForcedVariation = new Variation { Key = "variation", Id = "7721010009" };
            var expectedForcedVariation2 = new Variation { Key = "variation", Id = "7721010509" };
            var userAttributes = new UserAttributes
            {
                {"device_type", "iPhone" },
                {"location", "San Francisco" }
            };

            Optimizely.Activate(experimentKey, TestUserId, userAttributes);

            Assert.IsTrue(Optimizely.SetForcedVariation(experimentKey, TestUserId, expectedForcedVariation.Key), "Set variation to 'variation' failed.");

            // call getForcedVariation with valid experiment key and valid user ID
            var actualForcedVariation = Optimizely.GetForcedVariation("test_experiment", TestUserId);
            Assert.IsTrue(TestData.CompareObjects(expectedForcedVariation, actualForcedVariation));
            
            // call getForcedVariation with invalid experiment and valid userID
            actualForcedVariation = Optimizely.GetForcedVariation("invalid_experiment", TestUserId);
            Assert.Null(actualForcedVariation);

            // call getForcedVariation with valid experiment and invalid userID
            actualForcedVariation = Optimizely.GetForcedVariation("test_experiment", "invalid_user");

            Assert.Null(actualForcedVariation);

            // call getForcedVariation with an experiment that"s not running
            Assert.IsTrue(Optimizely.SetForcedVariation("paused_experiment", "test_user2", "variation"), "Set variation to 'variation' failed.");

            actualForcedVariation = Optimizely.GetForcedVariation("paused_experiment", "test_user2");

            Assert.IsTrue(TestData.CompareObjects(expectedForcedVariation2, actualForcedVariation));
            
            // confirm that the second setForcedVariation call did not invalidate the first call to that method
            actualForcedVariation = Optimizely.GetForcedVariation("test_experiment", TestUserId);

            Assert.IsTrue(TestData.CompareObjects(expectedForcedVariation, actualForcedVariation));
        }

        [Test]
        public void TestGetForcedVariationWithInvalidUserID()
        {
            var experimentKey = "test_experiment";
            Optimizely.SetForcedVariation(experimentKey, "test_user", "test_variation");

            Assert.Null(Optimizely.GetForcedVariation(experimentKey, null));
            Assert.Null(Optimizely.GetForcedVariation(experimentKey, "invalid_user"));
        }

        [Test]
        public void TestGetForcedVariationWithInvalidExperimentKey()
        {
            var userId = "test_user";
            var experimentKey = "test_experiment";
            Optimizely.SetForcedVariation(experimentKey, userId, "test_variation");
            
            Assert.Null(Optimizely.GetForcedVariation("test_experiment", userId));
            Assert.Null(Optimizely.GetForcedVariation("", userId));
            Assert.Null(Optimizely.GetForcedVariation(null, userId));
        }

        [Test]
        public void TestGetVariationAudienceMatchAfterSetForcedVariation()
        {
            var userId = "test_user";
            var experimentKey = "test_experiment";
            var experimentId = "7716830082";
            var variationKey = "control";
            var variationId = "7722370027";

            var attributes = new UserAttributes
            {
               { "device_type", "iPhone" },
               { "location", "San Francisco" }
            };

            Assert.True(Optimizely.SetForcedVariation(experimentKey, userId, variationKey), "Set variation for paused experiment should have failed.");
            var variation = Optimizely.GetVariation(experimentKey, userId, attributes);

            LoggerMock.Verify(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<string>()), Times.Exactly(2));
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, string.Format(@"Set variation ""{0}"" for experiment ""{1}"" and user ""{2}"" in the forced variation map.", variationId, experimentId, userId)));
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, string.Format(@"Variation ""{0}"" is mapped to experiment ""{1}"" and user ""{2}"" in the forced variation map", variationKey, experimentKey, userId)));

            Assert.IsTrue(TestData.CompareObjects(VariationWithKeyControl, variation));
        }

        [Test]
        public void TestGetVariationExperimentNotRunningAfterSetForceVariation()
        {
            var userId = "test_user";
            var experimentKey = "paused_experiment";
            var experimentId = "7716830585";
            var variationKey = "control";
            var variationId = "7722370427";

            var attributes = new UserAttributes
            {
               { "device_type", "iPhone" },
               { "location", "San Francisco" }
            };

            Assert.True(Optimizely.SetForcedVariation(experimentKey, userId, variationKey), "Set variation for paused experiment should have failed.");
            var variation = Optimizely.GetVariation(experimentKey, userId, attributes);

            LoggerMock.Verify(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<string>()), Times.Exactly(2));
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, string.Format(@"Set variation ""{0}"" for experiment ""{1}"" and user ""{2}"" in the forced variation map.", variationId, experimentId, userId)));
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, string.Format("Experiment \"{0}\" is not running.", experimentKey)));

            Assert.Null(variation);
        }

        [Test]
        public void TestGetVariationWhitelistedUserAfterSetForcedVariation()
        {
            var userId = "user1";
            var experimentKey = "test_experiment";
            var experimentId = "7716830082";
            var variationKey = "variation";
            var variationId = "7721010009";
            
            Assert.True(Optimizely.SetForcedVariation(experimentKey, userId, variationKey), "Set variation for paused experiment should have passed.");
            var variation = Optimizely.GetVariation(experimentKey, userId);

            LoggerMock.Verify(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<string>()), Times.Exactly(2));
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, string.Format(@"Set variation ""{0}"" for experiment ""{1}"" and user ""{2}"" in the forced variation map.", variationId, experimentId, userId)));
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, string.Format(@"Variation ""{0}"" is mapped to experiment ""{1}"" and user ""{2}"" in the forced variation map", variationKey, experimentKey, userId)));

            Assert.IsTrue(TestData.CompareObjects(VariationWithKeyVariation, variation));
        }

        [Test]
        public void TestActivateNoAudienceNoAttributesAfterSetForcedVariation()
        {
            var userId = "test_user";
            var experimentKey = "test_experiment";
            var experimentId = "7716830082";
            var variationKey = "control";
            var variationId = "7722370027";
            var parameters = new Dictionary<string, object>
            {
                { "param1", "val1" },
                { "param2", "val2" }
            };

            Experiment experiment = new Experiment();
            experiment.Key = "group_experiment_1";

            var optly = Helper.CreatePrivateOptimizely();            
            optly.SetFieldOrProperty("ProjectConfigManager", ConfigManager);

            // Set forced variation
            Assert.True((bool)optly.Invoke("SetForcedVariation", experimentKey, userId, variationKey), "Set variation for paused experiment should have failed.");
            
            // Activate
            var variation = (Variation)optly.Invoke("Activate", "group_experiment_1", "user_1", null);

            EventDispatcherMock.Verify(dispatcher => dispatcher.DispatchEvent(It.IsAny<LogEvent>()), Times.Once);

            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, string.Format(@"Set variation ""{0}"" for experiment ""{1}"" and user ""{2}"" in the forced variation map.", variationId, experimentId, userId)), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "User \"user_1\" is not in the forced variation map."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "Assigned bucket [1922] to user [user_1] with bucketing ID [user_1]."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "User [user_1] is in experiment [group_experiment_1] of group [7722400015]."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "Assigned bucket [9525] to user [user_1] with bucketing ID [user_1]."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "User [user_1] is in variation [group_exp_1_var_2] of experiment [group_experiment_1]."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "This decision will not be saved since the UserProfileService is null."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "Activating user user_1 in experiment group_experiment_1."), Times.Once);            

            Assert.IsTrue(TestData.CompareObjects(GroupVariation, variation));
        }

        [Test]
        public void TestTrackNoAttributesNoEventValueAfterSetForcedVariation()
        {
            var userId = "test_user";
            var experimentKey = "test_experiment";
            var experimentId = "7716830082";
            var variationKey = "control";
            var variationId = "7722370027";
            var parameters = new Dictionary<string, object>
            {
                { "param1", "val1" }
            };

            var optly = Helper.CreatePrivateOptimizely();            

            // Set forced variation
            Assert.True((bool)optly.Invoke("SetForcedVariation", experimentKey, userId, variationKey), "Set variation for paused experiment should have failed.");

            // Track
            optly.Invoke("Track", "purchase", "test_user", null, null);
            EventDispatcherMock.Verify(dispatcher => dispatcher.DispatchEvent(It.IsAny<LogEvent>()), Times.Once);
            
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, string.Format(@"Set variation ""{0}"" for experiment ""{1}"" and user ""{2}"" in the forced variation map.", variationId, experimentId, userId)), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "Tracking event purchase for user test_user."), Times.Once);            
        }

        [Test]
        public void TestGetVariationBucketingIdAttribute()
        {
            var testBucketingIdControl = "testBucketingIdControl!"; // generates bucketing number 3741
            var testBucketingIdVariation = "123456789"; // generates bucketing number 4567
            var userId = "test_user";
            var experimentKey = "test_experiment";

            var userAttributes = new UserAttributes
            {
               { "device_type", "iPhone" },
               { "company", "Optimizely" },
               { "location", "San Francisco" }
            };

            var userAttributesWithBucketingId = new UserAttributes
            {
               { "device_type", "iPhone" },
               { "company", "Optimizely" },
               { "location", "San Francisco" },
               { ControlAttributes.BUCKETING_ID_ATTRIBUTE, testBucketingIdVariation }
            };

            // confirm that a valid variation is bucketed without the bucketing ID
            var actualVariation = Optimizely.GetVariation(experimentKey, userId, userAttributes);
            Assert.IsTrue(TestData.CompareObjects(VariationWithKeyControl, actualVariation), string.Format("Invalid variation key \"{0}\" for getVariation.", actualVariation?.Key));

            // confirm that invalid audience returns null
            actualVariation = Optimizely.GetVariation(experimentKey, userId);
            Assert.Null(actualVariation, string.Format("Invalid variation key \"{0}\" for getVariation with bucketing ID \"{1}\".", actualVariation?.Key, testBucketingIdControl));

            // confirm that a valid variation is bucketed with the bucketing ID
            actualVariation = Optimizely.GetVariation(experimentKey, userId, userAttributesWithBucketingId);
            Assert.IsTrue(TestData.CompareObjects(VariationWithKeyVariation, actualVariation), string.Format("Invalid variation key \"{0}\" for getVariation with bucketing ID \"{1}\".", actualVariation?.Key, testBucketingIdVariation));

            // confirm that invalid experiment with the bucketing ID returns null
            actualVariation = Optimizely.GetVariation("invalidExperimentKey", userId, userAttributesWithBucketingId);
            Assert.Null(actualVariation, string.Format("Invalid variation key \"{0}\" for getVariation with bucketing ID \"{1}\".", actualVariation?.Key, testBucketingIdControl));
        }

        #endregion

        #region Test GetFeatureVariable<Type> methods

        [Test]
        public void TestGetFeatureVariableBooleanReturnsCorrectValue()
        {
            var featureKey = "featureKey";
            var variableKeyTrue = "varTrue";
            var variableKeyFalse = "varFalse";
            var variableKeyNonBoolean = "varNonBoolean";
            var variableKeyNull = "varNull";
            var featureVariableType = FeatureVariable.VariableType.BOOLEAN;

            OptimizelyMock.Setup(om => om.GetFeatureVariableValueForType<bool?>(It.IsAny<string>(), variableKeyTrue, It.IsAny<string>(),
                It.IsAny<UserAttributes>(), featureVariableType)).Returns(true);
            Assert.AreEqual(true, OptimizelyMock.Object.GetFeatureVariableBoolean(featureKey, variableKeyTrue, TestUserId, null));

            OptimizelyMock.Setup(om => om.GetFeatureVariableValueForType<bool?>(It.IsAny<string>(), variableKeyFalse, It.IsAny<string>(),
                It.IsAny<UserAttributes>(), featureVariableType)).Returns(false);
            Assert.AreEqual(false, OptimizelyMock.Object.GetFeatureVariableBoolean(featureKey, variableKeyFalse, TestUserId, null));

            OptimizelyMock.Setup(om => om.GetFeatureVariableValueForType<string>(It.IsAny<string>(), variableKeyNonBoolean, It.IsAny<string>(),
                It.IsAny<UserAttributes>(), featureVariableType)).Returns("non_boolean_value");
            Assert.Null(OptimizelyMock.Object.GetFeatureVariableBoolean(featureKey, variableKeyNonBoolean, TestUserId, null));
            
            OptimizelyMock.Setup(om => om.GetFeatureVariableValueForType<bool?>(It.IsAny<string>(), variableKeyNull, It.IsAny<string>(),
                It.IsAny<UserAttributes>(), featureVariableType)).Returns<bool?>(null);
            Assert.Null(OptimizelyMock.Object.GetFeatureVariableBoolean(featureKey, variableKeyNull, TestUserId, null));
        }

        [Test]
        public void TestGetFeatureVariableDoubleReturnsCorrectValue()
        {
            var featureKey = "featureKey";
            var variableKeyDouble = "varDouble";
            var variableKeyInt = "varInt";
            var variableKeyNonDouble = "varNonDouble";
            var variableKeyNull = "varNull";
            var featureVariableType = FeatureVariable.VariableType.DOUBLE;

            OptimizelyMock.Setup(om => om.GetFeatureVariableValueForType<double?>(It.IsAny<string>(), variableKeyDouble, It.IsAny<string>(),
                It.IsAny<UserAttributes>(), featureVariableType)).Returns(100.54);
            Assert.AreEqual(100.54, OptimizelyMock.Object.GetFeatureVariableDouble(featureKey, variableKeyDouble, TestUserId, null));

            OptimizelyMock.Setup(om => om.GetFeatureVariableValueForType<double?>(It.IsAny<string>(), variableKeyInt, It.IsAny<string>(),
                It.IsAny<UserAttributes>(), featureVariableType)).Returns(100);
            Assert.AreEqual(100, OptimizelyMock.Object.GetFeatureVariableDouble(featureKey, variableKeyInt, TestUserId, null));

            OptimizelyMock.Setup(om => om.GetFeatureVariableValueForType<string>(It.IsAny<string>(), variableKeyNonDouble, It.IsAny<string>(),
                It.IsAny<UserAttributes>(), featureVariableType)).Returns("non_double_value");
            Assert.Null(OptimizelyMock.Object.GetFeatureVariableDouble(featureKey, variableKeyNonDouble, TestUserId, null));
            
            OptimizelyMock.Setup(om => om.GetFeatureVariableValueForType<double?>(It.IsAny<string>(), variableKeyNull, It.IsAny<string>(),
                It.IsAny<UserAttributes>(), featureVariableType)).Returns<double?>(null);
            Assert.Null(OptimizelyMock.Object.GetFeatureVariableDouble(featureKey, variableKeyNull, TestUserId, null));
        }

        [Test]
        public void TestGetFeatureVariableIntegerReturnsCorrectValue()
        {
            var featureKey = "featureKey";
            var variableKeyInt = "varInt";
            var variableNonInt = "varNonInt";
            var variableKeyNull = "varNull";
            var featureVariableType = FeatureVariable.VariableType.INTEGER;

            OptimizelyMock.Setup(om => om.GetFeatureVariableValueForType<int?>(It.IsAny<string>(), variableKeyInt, It.IsAny<string>(),
                It.IsAny<UserAttributes>(), featureVariableType)).Returns(100);
            Assert.AreEqual(100, OptimizelyMock.Object.GetFeatureVariableInteger(featureKey, variableKeyInt, TestUserId, null));
            
            OptimizelyMock.Setup(om => om.GetFeatureVariableValueForType<string>(It.IsAny<string>(), variableNonInt, It.IsAny<string>(),
                It.IsAny<UserAttributes>(), featureVariableType)).Returns("non_integer_value");
            Assert.Null(OptimizelyMock.Object.GetFeatureVariableInteger(featureKey, variableNonInt, TestUserId, null));
            
            OptimizelyMock.Setup(om => om.GetFeatureVariableValueForType<int?>(It.IsAny<string>(), variableKeyNull, It.IsAny<string>(),
                                                                               It.IsAny<UserAttributes>(), featureVariableType)).Returns<string>(null);
            Assert.Null(OptimizelyMock.Object.GetFeatureVariableInteger(featureKey, variableKeyNull, TestUserId, null));
        }

        [Test]
        public void TestGetFeatureVariableStringReturnsCorrectValue()
        {
            var featureKey = "featureKey";
            var variableKeyString = "varString1";
            var variableKeyIntString = "varString2";
            var variableKeyNull = "varNull";
            var featureVariableType = FeatureVariable.VariableType.STRING;

            OptimizelyMock.Setup(om => om.GetFeatureVariableValueForType<string>(It.IsAny<string>(), variableKeyString, It.IsAny<string>(),
                It.IsAny<UserAttributes>(), featureVariableType)).Returns("Test String");
            Assert.AreEqual("Test String", OptimizelyMock.Object.GetFeatureVariableString(featureKey, variableKeyString, TestUserId, null));

            OptimizelyMock.Setup(om => om.GetFeatureVariableValueForType<string>(It.IsAny<string>(), variableKeyIntString, It.IsAny<string>(),
                It.IsAny<UserAttributes>(), featureVariableType)).Returns("123");
            Assert.AreEqual("123", OptimizelyMock.Object.GetFeatureVariableString(featureKey, variableKeyIntString, TestUserId, null));

            OptimizelyMock.Setup(om => om.GetFeatureVariableValueForType<string>(It.IsAny<string>(), variableKeyNull, It.IsAny<string>(),
                It.IsAny<UserAttributes>(), featureVariableType)).Returns<string>(null);
            Assert.Null(OptimizelyMock.Object.GetFeatureVariableString(featureKey, variableKeyNull, TestUserId, null));
        }

        #region Feature Toggle Tests

        [Test]
        public void TestGetFeatureVariableDoubleReturnsRightValueWhenUserBuckedIntoFeatureExperimentAndVariationIsToggleOn()
        {
            var featureKey = "double_single_variable_feature";
            var featureFlag = Config.GetFeatureFlagFromKey(featureKey);
            var variableKey = "double_variable";
            var expectedValue = 42.42;
            var experiment = Config.GetExperimentFromKey("test_experiment_double_feature");
            var variation = Config.GetVariationFromKey("test_experiment_double_feature", "control");
            var decision = new FeatureDecision(experiment, variation, FeatureDecision.DECISION_SOURCE_FEATURE_TEST);

            DecisionServiceMock.Setup(ds => ds.GetVariationForFeature(featureFlag, TestUserId, Config, null)).Returns(decision);
            
            var optly = Helper.CreatePrivateOptimizely();
            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);
            optly.SetFieldOrProperty("ProjectConfigManager", ConfigManager);

            var variableValue = (double)optly.Invoke("GetFeatureVariableDouble", featureKey, variableKey, TestUserId, null);
            Assert.AreEqual(expectedValue, variableValue);

            LoggerMock.Verify(l => l.Log(LogLevel.INFO, $@"Returning variable value ""{variableValue}"" for variation ""{variation.Key}"" of feature flag ""{featureKey}""."));
        }

        [Test]
        public void TestGetFeatureVariableIntegerReturnsRightValueWhenUserBuckedIntoFeatureExperimentAndVariationIsToggleOn()
        {
            var featureKey = "integer_single_variable_feature";
            var featureFlag = Config.GetFeatureFlagFromKey(featureKey);
            var variableKey = "integer_variable";
            var expectedValue = 13;
            var experiment = Config.GetExperimentFromKey("test_experiment_integer_feature");
            var variation = Config.GetVariationFromKey("test_experiment_integer_feature", "variation");
            var decision = new FeatureDecision(experiment, variation, FeatureDecision.DECISION_SOURCE_FEATURE_TEST);
            var userAttributes = new UserAttributes
            {
               { "device_type", "iPhone" },
               { "company", "Optimizely" },
               { "location", "San Francisco" }
            };

            DecisionServiceMock.Setup(ds => ds.GetVariationForFeature(featureFlag, TestUserId, Config, userAttributes)).Returns(decision);
            
            var optly = Helper.CreatePrivateOptimizely();
            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);
            optly.SetFieldOrProperty("ProjectConfigManager", ConfigManager);

            var variableValue = (int)optly.Invoke("GetFeatureVariableInteger", featureKey, variableKey, TestUserId, userAttributes);
            Assert.AreEqual(expectedValue, variableValue);

            LoggerMock.Verify(l => l.Log(LogLevel.INFO, $@"Returning variable value ""{variableValue}"" for variation ""{variation.Key}"" of feature flag ""{featureKey}""."));
        }

        [Test]
        public void TestGetFeatureVariableDoubleReturnsDefaultValueWhenUserBuckedIntoFeatureExperimentAndVariationIsToggleOff()
        {
            var featureKey = "double_single_variable_feature";
            var featureFlag = Config.GetFeatureFlagFromKey(featureKey);
            var variableKey = "double_variable";
            var expectedValue = 14.99;
            var experiment = Config.GetExperimentFromKey("test_experiment_double_feature");
            var variation = Config.GetVariationFromKey("test_experiment_double_feature", "variation");
            var decision = new FeatureDecision(experiment, variation, FeatureDecision.DECISION_SOURCE_FEATURE_TEST);

            DecisionServiceMock.Setup(ds => ds.GetVariationForFeature(featureFlag, TestUserId, Config, null)).Returns(decision);
            
            var optly = Helper.CreatePrivateOptimizely();
            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);
            optly.SetFieldOrProperty("ProjectConfigManager", ConfigManager);

            var variableValue = (double)optly.Invoke("GetFeatureVariableDouble", featureKey, variableKey, TestUserId, null);
            Assert.AreEqual(expectedValue, variableValue);

            LoggerMock.Verify(l => l.Log(LogLevel.INFO, $@"Feature ""{featureKey}"" is not enabled for user {TestUserId}. Returning default value for variable ""{variableKey}""."));
        }

        [Test]
        public void TestGetFeatureVariableIntegerReturnsDefaultValueWhenUserBuckedIntoFeatureExperimentAndVariationIsToggleOff()
        {
            var featureKey = "integer_single_variable_feature";
            var featureFlag = Config.GetFeatureFlagFromKey(featureKey);
            var variableKey = "integer_variable";
            var expectedValue = 7;
            var experiment = Config.GetExperimentFromKey("test_experiment_integer_feature");
            var variation = Config.GetVariationFromKey("test_experiment_integer_feature", "control");
            var decision = new FeatureDecision(experiment, variation, FeatureDecision.DECISION_SOURCE_FEATURE_TEST);
            var userAttributes = new UserAttributes
            {
               { "device_type", "iPhone" },
               { "company", "Optimizely" },
               { "location", "San Francisco" }
            };

            DecisionServiceMock.Setup(ds => ds.GetVariationForFeature(featureFlag, TestUserId, Config, userAttributes)).Returns(decision);
            
            var optly = Helper.CreatePrivateOptimizely();
            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);
            optly.SetFieldOrProperty("ProjectConfigManager", ConfigManager);

            var variableValue = (int)optly.Invoke("GetFeatureVariableInteger", featureKey, variableKey, TestUserId, userAttributes);
            Assert.AreEqual(expectedValue, variableValue);

            LoggerMock.Verify(l => l.Log(LogLevel.INFO, $@"Feature ""{featureKey}"" is not enabled for user {TestUserId}. Returning default value for variable ""{variableKey}""."));
        }

        [Test]
        public void TestGetFeatureVariableBooleanReturnsRightValueWhenUserBuckedIntoRolloutAndVariationIsToggleOn()
        {
            var featureKey = "boolean_single_variable_feature";
            var featureFlag = Config.GetFeatureFlagFromKey(featureKey);
            var variableKey = "boolean_variable";
            var expectedValue = true;
            var experiment = Config.GetRolloutFromId("166660").Experiments[0];
            var variation = Config.GetVariationFromKey(experiment.Key, "177771");
            var decision = new FeatureDecision(experiment, variation, FeatureDecision.DECISION_SOURCE_ROLLOUT);

            DecisionServiceMock.Setup(ds => ds.GetVariationForFeature(featureFlag, TestUserId, Config, null)).Returns(decision);
            
            var optly = Helper.CreatePrivateOptimizely();
            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);
            optly.SetFieldOrProperty("ProjectConfigManager", ConfigManager);
            var variableValue = (bool)optly.Invoke("GetFeatureVariableBoolean", featureKey, variableKey, TestUserId, null);
            Assert.AreEqual(expectedValue, variableValue);

            LoggerMock.Verify(l => l.Log(LogLevel.INFO, $@"Returning variable value ""true"" for variation ""{variation.Key}"" of feature flag ""{featureKey}""."));
        }

        [Test]
        public void TestGetFeatureVariableStringReturnsRightValueWhenUserBuckedIntoRolloutAndVariationIsToggleOn()
        {
            var featureKey = "string_single_variable_feature";
            var featureFlag = Config.GetFeatureFlagFromKey(featureKey);
            var variableKey = "string_variable";
            var expectedValue = "cta_4";
            var experiment = Config.GetRolloutFromId("166661").Experiments[0];
            var variation = Config.GetVariationFromKey(experiment.Key, "177775");
            var decision = new FeatureDecision(experiment, variation, FeatureDecision.DECISION_SOURCE_ROLLOUT);
            var userAttributes = new UserAttributes
            {
               { "device_type", "iPhone" },
               { "company", "Optimizely" },
               { "location", "San Francisco" }
            };

            DecisionServiceMock.Setup(ds => ds.GetVariationForFeature(featureFlag, TestUserId, Config, userAttributes)).Returns(decision);

            var optly = Helper.CreatePrivateOptimizely();

            optly.SetFieldOrProperty("ProjectConfigManager", ConfigManager);
            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);
            
            var variableValue = (string)optly.Invoke("GetFeatureVariableString", featureKey, variableKey, TestUserId, userAttributes);
            Assert.AreEqual(expectedValue, variableValue);

            LoggerMock.Verify(l => l.Log(LogLevel.INFO, $@"Returning variable value ""{variableValue}"" for variation ""{variation.Key}"" of feature flag ""{featureKey}""."));
        }

        [Test]
        public void TestGetFeatureVariableBooleanReturnsDefaultValueWhenUserBuckedIntoRolloutAndVariationIsToggleOff()
        {
            var featureKey = "boolean_single_variable_feature";
            var featureFlag = Config.GetFeatureFlagFromKey(featureKey);
            var variableKey = "boolean_variable";
            var expectedValue = true;
            var experiment = Config.GetRolloutFromId("166660").Experiments[3];
            var variation = Config.GetVariationFromKey(experiment.Key, "177782");
            var decision = new FeatureDecision(experiment, variation, FeatureDecision.DECISION_SOURCE_ROLLOUT);

            DecisionServiceMock.Setup(ds => ds.GetVariationForFeature(featureFlag, TestUserId, Config, null)).Returns(decision);
            
            var optly = Helper.CreatePrivateOptimizely();
            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);
            optly.SetFieldOrProperty("ProjectConfigManager", ConfigManager);
            var variableValue = (bool)optly.Invoke("GetFeatureVariableBoolean", featureKey, variableKey, TestUserId, null);
            Assert.AreEqual(expectedValue, variableValue);

            LoggerMock.Verify(l => l.Log(LogLevel.INFO, $@"Feature ""{featureKey}"" is not enabled for user {TestUserId}. Returning default value for variable ""{variableKey}""."));
        }

        [Test]
        public void TestGetFeatureVariableStringReturnsDefaultValueWhenUserBuckedIntoRolloutAndVariationIsToggleOff()
        {
            var featureKey = "string_single_variable_feature";
            var featureFlag = Config.GetFeatureFlagFromKey(featureKey);
            var variableKey = "string_variable";
            var expectedValue = "wingardium leviosa";
            var experiment = Config.GetRolloutFromId("166661").Experiments[2];
            var variation = Config.GetVariationFromKey(experiment.Key, "177784");
            var decision = new FeatureDecision(experiment, variation, FeatureDecision.DECISION_SOURCE_ROLLOUT);
            var userAttributes = new UserAttributes
            {
               { "device_type", "iPhone" },
               { "company", "Optimizely" },
               { "location", "San Francisco" }
            };

            DecisionServiceMock.Setup(ds => ds.GetVariationForFeature(featureFlag, TestUserId, Config, userAttributes)).Returns(decision);
            
            var optly = Helper.CreatePrivateOptimizely();
            optly.SetFieldOrProperty("ProjectConfigManager", ConfigManager);
            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);

            var variableValue = (string)optly.Invoke("GetFeatureVariableString", featureKey, variableKey, TestUserId, userAttributes);
            Assert.AreEqual(expectedValue, variableValue);

            LoggerMock.Verify(l => l.Log(LogLevel.INFO, $@"Feature ""{featureKey}"" is not enabled for user {TestUserId}. Returning default value for variable ""{variableKey}""."));
        }

        [Test]
        public void TestGetFeatureVariableDoubleReturnsDefaultValueWhenUserNotBuckedIntoBothFeatureExperimentAndRollout()
        {
            var featureKey = "double_single_variable_feature";
            var featureFlag = Config.GetFeatureFlagFromKey("double_single_variable_feature");
            var variableKey = "double_variable";
            var expectedValue = 14.99;
            var decision = new FeatureDecision(null, null, FeatureDecision.DECISION_SOURCE_ROLLOUT);

            DecisionServiceMock.Setup(ds => ds.GetVariationForFeature(featureFlag, TestUserId, Config, null)).Returns(decision);

            var optly = Helper.CreatePrivateOptimizely();
            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);
            optly.SetFieldOrProperty("ProjectConfigManager", ConfigManager);

            var variableValue = (double)optly.Invoke("GetFeatureVariableDouble", featureKey, variableKey, TestUserId, null);
            Assert.AreEqual(expectedValue, variableValue);

            LoggerMock.Verify(l => l.Log(LogLevel.INFO, $@"User ""{TestUserId}"" is not in any variation for feature flag ""{featureKey}"", returning default value ""{variableValue}""."));
        }

        #endregion Feature Toggle Tests

        #endregion // Test GetFeatureVariable<Type> TypeCasting

        #region Test GetFeatureVariableValueForType method

        // Should return null and log error message when arguments are null or empty.
        [Test]
        public void TestGetFeatureVariableValueForTypeGivenNullOrEmptyArguments()
        {
            var featureKey = "featureKey";
            var variableKey = "variableKey";
            var variableType = FeatureVariable.VariableType.BOOLEAN;

            // Passing null and empty feature key.
            Assert.IsNull(Optimizely.GetFeatureVariableValueForType<bool?>(null, variableKey, TestUserId, null, variableType));
            Assert.IsNull(Optimizely.GetFeatureVariableValueForType<bool?>("", variableKey, TestUserId, null, variableType));

            // Passing null and empty variable key.
            Assert.IsNull(Optimizely.GetFeatureVariableValueForType<bool?>(featureKey, null, TestUserId, null, variableType));
            Assert.IsNull(Optimizely.GetFeatureVariableValueForType<bool?>(featureKey, "", TestUserId, null, variableType));

            // Passing null and empty user Id.
            Assert.IsNull(Optimizely.GetFeatureVariableValueForType<bool?>(featureKey, variableKey, null, null, variableType));
            Assert.IsNull(Optimizely.GetFeatureVariableValueForType<bool?>(featureKey, variableKey, "", null, variableType));

            LoggerMock.Verify(l => l.Log(LogLevel.ERROR, "Provided Feature Key is in invalid format."), Times.Exactly(2));
            LoggerMock.Verify(l => l.Log(LogLevel.ERROR, "Provided Variable Key is in invalid format."), Times.Exactly(2));
            LoggerMock.Verify(l => l.Log(LogLevel.ERROR, "Provided User Id is in invalid format."), Times.Exactly(1));
        }

        // Should return null and log error message when feature key or variable key does not get found.
        [Test]
        public void TestGetFeatureVariableValueForTypeGivenFeatureKeyOrVariableKeyNotFound()
        {
            var featureKey = "this_feature_should_never_be_found_in_the_datafile_unless_the_datafile_creator_got_insane";
            var variableKey = "this_variable_should_never_be_found_in_the_datafile_unless_the_datafile_creator_got_insane";
            var variableType = FeatureVariable.VariableType.BOOLEAN;

            Assert.IsNull(Optimizely.GetFeatureVariableValueForType<bool?>(featureKey, variableKey, TestUserId, null, variableType));
            Assert.IsNull(Optimizely.GetFeatureVariableValueForType<bool?>("double_single_variable_feature", variableKey, TestUserId, null, variableType));

            LoggerMock.Verify(l => l.Log(LogLevel.ERROR, $@"Feature key ""{featureKey}"" is not in datafile."));
            LoggerMock.Verify(l => l.Log(LogLevel.ERROR, 
                $@"No feature variable was found for key ""{variableKey}"" in feature flag ""double_single_variable_feature""."));
        }

        // Should return null and log error message when variable type is invalid.
        [Test]
        public void TestGetFeatureVariableValueForTypeGivenInvalidVariableType()
        {
            var variableTypeBool = FeatureVariable.VariableType.BOOLEAN;
            var variableTypeInt = FeatureVariable.VariableType.INTEGER;
            var variableTypeDouble = FeatureVariable.VariableType.DOUBLE;
            var variableTypeString = FeatureVariable.VariableType.STRING;

            Assert.IsNull(Optimizely.GetFeatureVariableValueForType<double?>("double_single_variable_feature", "double_variable", TestUserId, null, variableTypeBool));
            Assert.IsNull(Optimizely.GetFeatureVariableValueForType<bool?>("boolean_single_variable_feature", "boolean_variable", TestUserId, null, variableTypeDouble));
            Assert.IsNull(Optimizely.GetFeatureVariableValueForType<int?>("integer_single_variable_feature", "integer_variable", TestUserId, null, variableTypeString));
            Assert.IsNull(Optimizely.GetFeatureVariableValueForType<string>("string_single_variable_feature", "string_variable", TestUserId, null, variableTypeInt));

            LoggerMock.Verify(l => l.Log(LogLevel.ERROR,
                $@"Variable is of type ""DOUBLE"", but you requested it as type ""{variableTypeBool}""."));
            LoggerMock.Verify(l => l.Log(LogLevel.ERROR,
                $@"Variable is of type ""BOOLEAN"", but you requested it as type ""{variableTypeDouble}""."));
            LoggerMock.Verify(l => l.Log(LogLevel.ERROR,
                $@"Variable is of type ""INTEGER"", but you requested it as type ""{variableTypeString}""."));
            LoggerMock.Verify(l => l.Log(LogLevel.ERROR,
                $@"Variable is of type ""STRING"", but you requested it as type ""{variableTypeInt}""."));
        }

        // Should return default value and log message when feature is not enabled for the user.
        [Test]
        public void TestGetFeatureVariableValueForTypeGivenFeatureFlagIsNotEnabledForUser()
        {
            var featureKey = "double_single_variable_feature";
            var featureFlag = Config.GetFeatureFlagFromKey("double_single_variable_feature");
            var experiment = Config.GetExperimentFromKey("test_experiment_double_feature");
            var variation = Config.GetVariationFromKey("test_experiment_double_feature", "variation");
            var variableKey = "double_variable";
            var variableType = FeatureVariable.VariableType.DOUBLE;
            var expectedValue = 14.99;

            var decision = new FeatureDecision(experiment, variation, FeatureDecision.DECISION_SOURCE_FEATURE_TEST);

            DecisionServiceMock.Setup(ds => ds.GetVariationForFeature(featureFlag, TestUserId, Config, null)).Returns(decision);

            var optly = Helper.CreatePrivateOptimizely();

            optly.SetFieldOrProperty("ProjectConfigManager", ConfigManager);
            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);

            var variableValue = (double?)optly.InvokeGeneric("GetFeatureVariableValueForType", new Type[] { typeof(double?) }, featureKey, variableKey, TestUserId, null, variableType);
            Assert.AreEqual(expectedValue, variableValue);

            LoggerMock.Verify(l => l.Log(LogLevel.INFO,
                $@"Feature ""{featureKey}"" is not enabled for user {TestUserId}. Returning default value for variable ""{variableKey}""."));
        }

        // Should return default value and log message when feature is enabled for the user 
        // but variable usage does not get found for the variation.
        [Test]
        public void TestGetFeatureVariableValueForTypeGivenFeatureFlagIsEnabledForUserAndVaribaleNotInVariation()
        {
            var featureKey = "double_single_variable_feature";
            var featureFlag = Config.GetFeatureFlagFromKey("double_single_variable_feature");
            var experiment = Config.GetExperimentFromKey("test_experiment_integer_feature");
            var differentVariation = Config.GetVariationFromKey("test_experiment_integer_feature", "control");
            var expectedDecision = new FeatureDecision(experiment, differentVariation, FeatureDecision.DECISION_SOURCE_FEATURE_TEST);
            var variableKey = "double_variable";
            var variableType = FeatureVariable.VariableType.DOUBLE;
            var expectedValue = 14.99;

            // Mock GetVariationForFeature method to return variation of different feature.
            DecisionServiceMock.Setup(ds => ds.GetVariationForFeature(featureFlag, TestUserId, Config, null)).Returns(expectedDecision);

            var optly = Helper.CreatePrivateOptimizely();

            optly.SetFieldOrProperty("ProjectConfigManager", ConfigManager);
            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);

            var variableValue = (double?)optly.InvokeGeneric("GetFeatureVariableValueForType", new Type[] { typeof(double?) }, featureKey, variableKey, TestUserId, null, variableType);
            Assert.AreEqual(expectedValue, variableValue);

            LoggerMock.Verify(l => l.Log(LogLevel.INFO,
                $@"Variable ""{variableKey}"" is not used in variation ""control"", returning default value ""{expectedValue}""."));
        }

        // Should return variable value from variation and log message when feature is enabled for the user
        // and variable usage has been found for the variation.
        [Test]
        public void TestGetFeatureVariableValueForTypeGivenFeatureFlagIsEnabledForUserAndVaribaleIsInVariation()
        {
            var featureKey = "double_single_variable_feature";
            var featureFlag = Config.GetFeatureFlagFromKey("double_single_variable_feature");
            var variableKey = "double_variable";
            var variableType = FeatureVariable.VariableType.DOUBLE;
            var expectedValue = 42.42;
            var experiment = Config.GetExperimentFromKey("test_experiment_double_feature");
            var variation = Config.GetVariationFromKey("test_experiment_double_feature", "control");
            var decision = new FeatureDecision(experiment, variation, FeatureDecision.DECISION_SOURCE_FEATURE_TEST);

            DecisionServiceMock.Setup(ds => ds.GetVariationForFeature(featureFlag, TestUserId, Config, null)).Returns(decision);

            var optly = Helper.CreatePrivateOptimizely();

            optly.SetFieldOrProperty("ProjectConfigManager", ConfigManager);
            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);

            var variableValue = (double?)optly.InvokeGeneric("GetFeatureVariableValueForType", new Type[] { typeof(double?) }, featureKey, variableKey, TestUserId, null, variableType);
            Assert.AreEqual(expectedValue, variableValue);

            LoggerMock.Verify(l => l.Log(LogLevel.INFO,
                $@"Returning variable value ""{variableValue}"" for variation ""{variation.Key}"" of feature flag ""{featureKey}""."));
        }
        
        // Verify that GetFeatureVariableValueForType returns correct variable value for rollout rule.
        [Test]
        public void TestGetFeatureVariableValueForTypeWithRolloutRule()
        {
            var featureKey = "boolean_single_variable_feature";
            var featureFlag = Config.GetFeatureFlagFromKey(featureKey);
            var variableKey = "boolean_variable";
            //experimentid - 177772
            var experiment = Config.Rollouts[0].Experiments[1]; 
            var variation = Config.GetVariationFromId(experiment.Key, "177773");
            var decision = new FeatureDecision(experiment, variation, FeatureDecision.DECISION_SOURCE_ROLLOUT);
            var expectedVariableValue = false;

            DecisionServiceMock.Setup(ds => ds.GetVariationForFeature(featureFlag, TestUserId, Config, null)).Returns(decision);

            var optly = Helper.CreatePrivateOptimizely();

            optly.SetFieldOrProperty("ProjectConfigManager", ConfigManager);
            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);

            // Calling GetFeatureVariableBoolean to get GetFeatureVariableValueForType returned value casted in bool.
            var actualVariableValue = (bool?)optly.Invoke("GetFeatureVariableBoolean", featureKey, variableKey, TestUserId, null);

            // Verify that variable value 'false' has been returned from GetFeatureVariableValueForType as it is the value
            // stored in rollout rule '177772'.
            Assert.AreEqual(expectedVariableValue, actualVariableValue);
        }

        #endregion // Test GetFeatureVariableValueForType method

        #region Test IsFeatureEnabled method

        // Should return false and log error message when arguments are null or empty.
        [Test]
        public void TestIsFeatureEnabledGivenNullOrEmptyArguments()
        {
            var featureKey = "featureKey";

            Assert.IsFalse(Optimizely.IsFeatureEnabled(featureKey, null, null));
            Assert.IsFalse(Optimizely.IsFeatureEnabled(featureKey, "", null));
                   
            Assert.IsFalse(Optimizely.IsFeatureEnabled(null, TestUserId, null));
            Assert.IsFalse(Optimizely.IsFeatureEnabled("", TestUserId, null));

            LoggerMock.Verify(l => l.Log(LogLevel.ERROR, "Provided Feature Key is in invalid format."), Times.Exactly(2));
            LoggerMock.Verify(l => l.Log(LogLevel.ERROR, "Provided User Id is in invalid format."), Times.Exactly(1));
        }

        // Should return false and log error message when feature flag key is not found in the datafile.
        [Test]
        public void TestIsFeatureEnabledGivenFeatureFlagNotFound()
        {
            var featureKey = "feature_not_found";
            Assert.IsFalse(Optimizely.IsFeatureEnabled(featureKey, TestUserId, null));
            
            LoggerMock.Verify(l => l.Log(LogLevel.ERROR, $@"Feature key ""{featureKey}"" is not in datafile."));
        }

        // Should return false and log error message when arguments are null or empty.
        [Test]
        public void TestIsFeatureEnabledGivenFeatureFlagContainsInvalidExperiment()
        {
            var tempConfig = DatafileProjectConfig.Create(TestData.Datafile, LoggerMock.Object, new NoOpErrorHandler());
            var tempConfigManager = new FallbackProjectConfigManager(tempConfig);
            var featureFlag = tempConfig.GetFeatureFlagFromKey("multi_variate_feature");

            var optly = Helper.CreatePrivateOptimizely();
            optly.SetFieldOrProperty("ProjectConfigManager", tempConfigManager);

            // Set such an experiment to the list of experiment ids, that does not belong to the feature.
            featureFlag.ExperimentIds = new List<string> { "4209211" };

            // Should return false when the experiment in feature flag does not get found in the datafile.
            Assert.False((bool)optly.Invoke("IsFeatureEnabled", "multi_variate_feature", TestUserId, null));

            LoggerMock.Verify(l => l.Log(LogLevel.ERROR, @"Experiment ID ""4209211"" is not in datafile."));
        }

        // Should return false and log message when feature is not enabled for the user.
        [Test]
        public void TestIsFeatureEnabledGivenFeatureFlagIsNotEnabledForUser()
        {
            var featureKey = "double_single_variable_feature";
            var featureFlag = Config.GetFeatureFlagFromKey("double_single_variable_feature");
            var experiment = Config.GetExperimentFromKey("test_experiment_double_feature");
            var variation = Config.GetVariationFromKey("test_experiment_double_feature", "variation");
            var decision = new FeatureDecision(experiment, variation, FeatureDecision.DECISION_SOURCE_FEATURE_TEST);

            DecisionServiceMock.Setup(ds => ds.GetVariationForFeature(featureFlag, TestUserId, Config, null)).Returns(decision);

            var optly = Helper.CreatePrivateOptimizely();
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
            var featureKey = "boolean_single_variable_feature";
            var rollout = Config.GetRolloutFromId("166660");
            var experiment = rollout.Experiments[0];
            var variation = experiment.Variations[0];
            var featureFlag = Config.GetFeatureFlagFromKey(featureKey);
            var decision = new FeatureDecision(experiment, variation, FeatureDecision.DECISION_SOURCE_ROLLOUT);

            DecisionServiceMock.Setup(ds => ds.GetVariationForFeature(featureFlag, TestUserId, Config, null)).Returns(decision);

            var optly = Helper.CreatePrivateOptimizely();
            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);
            optly.SetFieldOrProperty("ProjectConfigManager", ConfigManager);

            bool result = (bool)optly.Invoke("IsFeatureEnabled", featureKey, TestUserId, null);
            Assert.True(result);

            // SendImpressionEvent() does not get called.
            LoggerMock.Verify(l => l.Log(LogLevel.INFO,
                $@"The user ""{TestUserId}"" is not being experimented on feature ""{featureKey}""."), Times.Once);

            LoggerMock.Verify(l => l.Log(LogLevel.INFO,
                $@"Feature flag ""{featureKey}"" is enabled for user ""{TestUserId}""."));
        }

        // Should return true and send an impression event when feature is enabled for the user 
        // and user is being experimented.
        [Test]
        public void TestIsFeatureEnabledGivenFeatureFlagIsEnabledAndUserIsBeingExperimented()
        {
            var featureKey = "double_single_variable_feature";
            var experiment = Config.GetExperimentFromKey("test_experiment_double_feature");
            var variation = Config.GetVariationFromKey("test_experiment_double_feature", "control");
            var featureFlag = Config.GetFeatureFlagFromKey(featureKey);
            var decision = new FeatureDecision(experiment, variation, FeatureDecision.DECISION_SOURCE_FEATURE_TEST);

            DecisionServiceMock.Setup(ds => ds.GetVariationForFeature(featureFlag, TestUserId, Config, null)).Returns(decision);

            var optly = Helper.CreatePrivateOptimizely();
            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);
            optly.SetFieldOrProperty("ProjectConfigManager", ConfigManager);

            bool result = (bool)optly.Invoke("IsFeatureEnabled", featureKey, TestUserId, null);
            Assert.True(result);

            // SendImpressionEvent() gets called.
            LoggerMock.Verify(l => l.Log(LogLevel.INFO,
                $@"The user ""{TestUserId}"" is not being experimented on feature ""{featureKey}""."), Times.Never);

            LoggerMock.Verify(l => l.Log(LogLevel.INFO,
                $@"Feature flag ""{featureKey}"" is enabled for user ""{TestUserId}""."));
        }

        // Should return false and send an impression event when feature is enabled for the user 
        // and user is being experimented.
        [Test]
        public void TestIsFeatureEnabledGivenFeatureFlagIsNotEnabledAndUserIsBeingExperimented()
        {
            var featureKey = "double_single_variable_feature";
            var experiment = Config.GetExperimentFromKey("test_experiment_double_feature");
            var variation = Config.GetVariationFromKey("test_experiment_double_feature", "variation");
            var featureFlag = Config.GetFeatureFlagFromKey(featureKey);
            var decision = new FeatureDecision(experiment, variation, FeatureDecision.DECISION_SOURCE_FEATURE_TEST);

            DecisionServiceMock.Setup(ds => ds.GetVariationForFeature(featureFlag, TestUserId, Config, null)).Returns(decision);

            var optly = Helper.CreatePrivateOptimizely();
            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);
            optly.SetFieldOrProperty("ProjectConfigManager", ConfigManager);

            bool result = (bool)optly.Invoke("IsFeatureEnabled", featureKey, TestUserId, null);
            Assert.False(result);

            // SendImpressionEvent() gets called.
            LoggerMock.Verify(l => l.Log(LogLevel.INFO,
                $@"The user ""{TestUserId}"" is not being experimented on feature ""{featureKey}""."), Times.Never);

            LoggerMock.Verify(l => l.Log(LogLevel.INFO,
                $@"Feature flag ""{featureKey}"" is not enabled for user ""{TestUserId}""."));
            EventDispatcherMock.Verify(dispatcher => dispatcher.DispatchEvent(It.IsAny<LogEvent>()));
        }

        // Verify that IsFeatureEnabled returns true if a variation does not get found in the feature
        // flag experiment but found in the rollout rule.
        [Test]
        public void TestIsFeatureEnabledGivenVariationNotFoundInFeatureExperimentButInRolloutRule()
        {
            var featureKey = "boolean_single_variable_feature";
            var userAttributes = new UserAttributes
            {
                { "device_type", "iPhone" },
                { "company", "Optimizely" }
            };

            Assert.True(Optimizely.IsFeatureEnabled(featureKey, TestUserId, userAttributes));

            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "The feature flag \"boolean_single_variable_feature\" is not used in any experiments."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "User \"testUserId\" does not meet the conditions to be in rollout rule for audience \"Chrome users\"."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "User \"testUserId\" does not meet the conditions to be in rollout rule for audience \"iPhone users in San Francisco\"."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "Assigned bucket [8408] to user [testUserId] with bucketing ID [testUserId]."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "The user \"testUserId\" is bucketed into a rollout for feature flag \"boolean_single_variable_feature\"."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "The user \"testUserId\" is not being experimented on feature \"boolean_single_variable_feature\"."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "Feature flag \"boolean_single_variable_feature\" is enabled for user \"testUserId\"."), Times.Once);
        }

        public void TestIsFeatureEnabledWithFeatureEnabledPropertyGivenFeatureExperiment()
        {
            var userId = "testUserId2";
            var featureKey = "double_single_variable_feature";
            var experiment = Config.GetExperimentFromKey("test_experiment_double_feature");
            var featureEnabledTrue = Config.GetVariationFromKey("test_experiment_double_feature", "control");
            var featureEnabledFalse = Config.GetVariationFromKey("test_experiment_double_feature", "variation");
            var featureFlag = Config.GetFeatureFlagFromKey(featureKey);
            var decisionTrue = new FeatureDecision(experiment, featureEnabledTrue, FeatureDecision.DECISION_SOURCE_FEATURE_TEST);
            var decisionFalse = new FeatureDecision(experiment, featureEnabledFalse, FeatureDecision.DECISION_SOURCE_FEATURE_TEST);

            DecisionServiceMock.Setup(ds => ds.GetVariationForFeature(featureFlag, TestUserId, Config, null)).Returns(decisionTrue);
            DecisionServiceMock.Setup(ds => ds.GetVariationForFeature(featureFlag, userId, Config, null)).Returns(decisionFalse);

            var optly = Helper.CreatePrivateOptimizely();
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
            var featureKey = "boolean_single_variable_feature";
            var featureFlag = Config.GetFeatureFlagFromKey(featureKey);

            // Verify that IsFeatureEnabled returns true when user is bucketed into the rollout rule's variation.
            Assert.True(Optimizely.IsFeatureEnabled("boolean_single_variable_feature", TestUserId));

            DecisionServiceMock.Setup(ds => ds.GetVariationForFeature(featureFlag, TestUserId, Config, null)).Returns<FeatureDecision>(null);
            var optly = Helper.CreatePrivateOptimizely();
            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);

            // Verify that IsFeatureEnabled returns false when user does not get bucketed into the rollout rule's variation.
            bool result = (bool)optly.Invoke("IsFeatureEnabled", featureKey, TestUserId, null);
            Assert.False(result);
        }

        #endregion // Test IsFeatureEnabled method

        #region Test NotificationCenter

        [Test]
        public void TestActivateListenerWithoutAttributes()
        {
            TestActivateListener(null);
        }

        [Test]
        public void TestActivateListenerWithAttributes()
        {
            var userAttributes = new UserAttributes
            {
               { "device_type", "iPhone" },
               { "company", "Optimizely" },
               { "location", "San Francisco" }
            };

            TestActivateListener(userAttributes);
        }

        public void TestActivateListener(UserAttributes userAttributes)
        {
            var experimentKey = "group_experiment_1";
            var variationKey = "group_exp_1_var_1";
            var featureKey = "boolean_feature";
            var experiment = Config.GetExperimentFromKey(experimentKey);
            var variation = Config.GetVariationFromKey(experimentKey, variationKey);
            var featureFlag = Config.GetFeatureFlagFromKey(featureKey);
            var decision = new FeatureDecision(experiment, variation, FeatureDecision.DECISION_SOURCE_FEATURE_TEST);
            
            // Mocking objects.
            NotificationCallbackMock.Setup(nc => nc.TestActivateCallback(It.IsAny<Experiment>(), It.IsAny<string>(),
                It.IsAny<UserAttributes>(), It.IsAny<Variation>(), It.IsAny<LogEvent>()));
            NotificationCallbackMock.Setup(nc => nc.TestAnotherActivateCallback(It.IsAny<Experiment>(), It.IsAny<string>(),
                It.IsAny<UserAttributes>(), It.IsAny<Variation>(), It.IsAny<LogEvent>()));
            NotificationCallbackMock.Setup(nc => nc.TestTrackCallback(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<UserAttributes>(), It.IsAny<EventTags>(), It.IsAny<LogEvent>()));
            DecisionServiceMock.Setup(ds => ds.GetVariation(experiment, TestUserId, It.IsAny<ProjectConfig>(), userAttributes)).Returns(variation);
            DecisionServiceMock.Setup(ds => ds.GetVariationForFeature(featureFlag, TestUserId, It.IsAny<ProjectConfig>(), userAttributes)).Returns(decision);

            var optly = Helper.CreatePrivateOptimizely();
            var optStronglyTyped = optly.GetObject() as Optimizely;

            // Adding notification listeners.
            var notificationType = NotificationCenter.NotificationType.Activate;
            optStronglyTyped. NotificationCenter.AddNotification(notificationType, NotificationCallbackMock.Object.TestActivateCallback);
            optStronglyTyped.NotificationCenter.AddNotification(notificationType, NotificationCallbackMock.Object.TestAnotherActivateCallback);
                        
            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);            

            // Calling Activate and IsFeatureEnabled.
            optly.Invoke("Activate", experimentKey, TestUserId, userAttributes);
            optly.Invoke("IsFeatureEnabled", featureKey, TestUserId, userAttributes);

            // Verify that all the registered callbacks are called once for both Activate and IsFeatureEnabled.
            EventDispatcherMock.Verify(dispatcher => dispatcher.DispatchEvent(It.IsAny<LogEvent>()), Times.Exactly(2));

            NotificationCallbackMock.Verify(nc => nc.TestActivateCallback(experiment, TestUserId, userAttributes, variation, It.IsAny<LogEvent>()), Times.Exactly(2));
            NotificationCallbackMock.Verify(nc => nc.TestAnotherActivateCallback(experiment, TestUserId, userAttributes, variation, It.IsAny<LogEvent>()), Times.Exactly(2));
        }

        [Test]
        public void TestTrackListenerWithoutAttributesAndEventTags()
        {
            TestTrackListener(null, null);
        }

        [Test]
        public void TestTrackListenerWithAttributesWithoutEventTags()
        {
            var userAttributes = new UserAttributes
            {
               { "device_type", "iPhone" },
               { "company", "Optimizely" },
               { "location", "San Francisco" }
            };

            TestTrackListener(userAttributes, null);
        }

        [Test]
        public void TestTrackListenerWithAttributesAndEventTags()
        {
            var userAttributes = new UserAttributes
            {
               { "device_type", "iPhone" },
               { "company", "Optimizely" },
               { "location", "San Francisco" }
            };

            var eventTags = new EventTags
            {
                { "revenue", 42 }
            };

            TestTrackListener(userAttributes, eventTags);
        }

        public void TestTrackListener(UserAttributes userAttributes, EventTags eventTags)
        {
            var experimentKey = "test_experiment";
            var variationKey = "control";
            var eventKey = "purchase";
            var experiment = Config.GetExperimentFromKey(experimentKey);
            var variation = Config.GetVariationFromKey(experimentKey, variationKey);
            var logEvent = new LogEvent("https://logx.optimizely.com/v1/events", OptimizelyHelper.SingleParameter,
                "POST", new Dictionary<string, string> { });

            var optly = Helper.CreatePrivateOptimizely();
            var optStronglyTyped = optly.GetObject() as Optimizely;

            // Mocking objects.
            NotificationCallbackMock.Setup(nc => nc.TestTrackCallback(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<UserAttributes>(), It.IsAny<EventTags>(), It.IsAny<LogEvent>()));
            NotificationCallbackMock.Setup(nc => nc.TestAnotherTrackCallback(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<UserAttributes>(), It.IsAny<EventTags>(), It.IsAny<LogEvent>()));
            DecisionServiceMock.Setup(ds => ds.GetVariation(experiment, TestUserId, Config, userAttributes)).Returns(variation);
            
            // Adding notification listeners.
            var notificationType = NotificationCenter.NotificationType.Track;
            optStronglyTyped.NotificationCenter.AddNotification(notificationType, NotificationCallbackMock.Object.TestTrackCallback);
            optStronglyTyped.NotificationCenter.AddNotification(notificationType, NotificationCallbackMock.Object.TestAnotherTrackCallback);
            
            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);            

            // Calling Track.
            optly.Invoke("Track", eventKey, TestUserId, userAttributes, eventTags);

            // Verify that all the registered callbacks for Track are called.
            EventDispatcherMock.Verify(dispatcher => dispatcher.DispatchEvent(It.IsAny<LogEvent>()), Times.Once);
            NotificationCallbackMock.Verify(nc => nc.TestTrackCallback(eventKey, TestUserId, userAttributes, eventTags, It.IsAny<LogEvent>()), Times.Exactly(1));
            NotificationCallbackMock.Verify(nc => nc.TestAnotherTrackCallback(eventKey, TestUserId, userAttributes, eventTags, It.IsAny<LogEvent>()), Times.Exactly(1));
        }

        #region Decision Listener

        [Test]
        public void TestActivateSendsDecisionNotificationWithActualVariationKey()
        {
            var experimentKey = "test_experiment";
            var variationKey = "variation";
            var experiment = Config.GetExperimentFromKey(experimentKey);
            var variation = Config.GetVariationFromKey(experimentKey, variationKey);
            var userAttributes = new UserAttributes
            {
               { "device_type", "iPhone" },
               { "company", "Optimizely" },
               { "location", "San Francisco" }
            };

            // Mocking objects.
            NotificationCallbackMock.Setup(nc => nc.TestDecisionCallback(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<UserAttributes>(), It.IsAny<Dictionary<string, object>>()));
            DecisionServiceMock.Setup(ds => ds.GetVariation(experiment, TestUserId, Config, userAttributes)).Returns(variation);

            var optly = Helper.CreatePrivateOptimizely();
            optly.SetFieldOrProperty("ProjectConfigManager", ConfigManager);
            var optStronglyTyped = optly.GetObject() as Optimizely;

            optStronglyTyped.NotificationCenter.AddNotification(NotificationCenter.NotificationType.Decision, NotificationCallbackMock.Object.TestDecisionCallback);
            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);

            optly.Invoke("Activate", experimentKey, TestUserId, userAttributes);
            var decisionInfo = new Dictionary<string, object>
            {
                { "experimentKey", experimentKey },
                { "variationKey", variationKey },
            };

            NotificationCallbackMock.Verify(nc => nc.TestDecisionCallback(DecisionNotificationTypes.AB_TEST, TestUserId, userAttributes, decisionInfo), Times.Once);
        }

        [Test]
        public void TestActivateSendsDecisionNotificationWithVariationKeyAndTypeFeatureTestForFeatureExperiment()
        {
            var experimentKey = "group_experiment_1";
            var variationKey = "group_exp_1_var_1";
            var experiment = Config.GetExperimentFromKey(experimentKey);
            var variation = Config.GetVariationFromKey(experimentKey, variationKey);
            var userAttributes = new UserAttributes
            {
               { "device_type", "iPhone" },
               { "company", "Optimizely" },
               { "location", "San Francisco" }
            };

            // Mocking objects.
            NotificationCallbackMock.Setup(nc => nc.TestDecisionCallback(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<UserAttributes>(), It.IsAny<Dictionary<string, object>>()));
            DecisionServiceMock.Setup(ds => ds.GetVariation(experiment, TestUserId, Config, userAttributes)).Returns(variation);

            var optly = Helper.CreatePrivateOptimizely();
            optly.SetFieldOrProperty("ProjectConfigManager", ConfigManager);
            var optStronglyTyped = optly.GetObject() as Optimizely;
            
            optStronglyTyped.NotificationCenter.AddNotification(NotificationCenter.NotificationType.Decision, NotificationCallbackMock.Object.TestDecisionCallback);
            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);
            
            optly.Invoke("Activate", experimentKey, TestUserId, userAttributes);
            var decisionInfo = new Dictionary<string, object>
            {
                { "experimentKey", experimentKey },
                { "variationKey", variationKey },
            };

            NotificationCallbackMock.Verify(nc => nc.TestDecisionCallback(DecisionNotificationTypes.FEATURE_TEST, TestUserId, userAttributes, decisionInfo), Times.Once);
        }

        [Test]
        public void TestActivateSendsDecisionNotificationWithNullVariationKey()
        {
            var experimentKey = "test_experiment";
            var experiment = Config.GetExperimentFromKey(experimentKey);
            
            NotificationCallbackMock.Setup(nc => nc.TestDecisionCallback(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<UserAttributes>(), It.IsAny<Dictionary<string, object>>()));
            DecisionServiceMock.Setup(ds => ds.GetVariation(experiment, TestUserId, Config, null)).Returns<Variation>(null);

            var optly = Helper.CreatePrivateOptimizely();
            var optStronglyTyped = optly.GetObject() as Optimizely;

            optStronglyTyped.NotificationCenter.AddNotification(NotificationCenter.NotificationType.Decision, NotificationCallbackMock.Object.TestDecisionCallback);
            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);

            optly.Invoke("Activate", experimentKey, TestUserId, null);
            var decisionInfo = new Dictionary<string, object>
            {
                { "experimentKey", experimentKey },
                { "variationKey", null },
            };

            NotificationCallbackMock.Verify(nc => nc.TestDecisionCallback(DecisionNotificationTypes.AB_TEST, TestUserId, new UserAttributes(), decisionInfo), Times.Once);
        }

        [Test]
        public void TestGetVariationSendsDecisionNotificationWithActualVariationKey()
        {
            var experimentKey = "test_experiment";
            var variationKey = "variation";
            var experiment = Config.GetExperimentFromKey(experimentKey);
            var variation = Config.GetVariationFromKey(experimentKey, variationKey);
            var userAttributes = new UserAttributes
            {
               { "device_type", "iPhone" },
               { "company", "Optimizely" },
               { "location", "San Francisco" }
            };

            // Mocking objects.
            NotificationCallbackMock.Setup(nc => nc.TestDecisionCallback(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<UserAttributes>(), It.IsAny<Dictionary<string, object>>()));
            DecisionServiceMock.Setup(ds => ds.GetVariation(experiment, TestUserId, Config, userAttributes)).Returns(variation);

            var optly = Helper.CreatePrivateOptimizely();

            optly.SetFieldOrProperty("ProjectConfigManager", ConfigManager);
            var optStronglyTyped = optly.GetObject() as Optimizely;

            optStronglyTyped.NotificationCenter.AddNotification(NotificationCenter.NotificationType.Decision, NotificationCallbackMock.Object.TestDecisionCallback);
            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);

            optly.Invoke("GetVariation", experimentKey, TestUserId, userAttributes);
            var decisionInfo = new Dictionary<string, object>
            {
                { "experimentKey", experimentKey },
                { "variationKey", variationKey },
            };

            NotificationCallbackMock.Verify(nc => nc.TestDecisionCallback(DecisionNotificationTypes.AB_TEST, TestUserId, userAttributes, decisionInfo), Times.Once);
        }

        [Test]
        public void TestGetVariationSendsDecisionNotificationWithVariationKeyAndTypeFeatureTestForFeatureExperiment()
        {
            var experimentKey = "group_experiment_1";
            var variationKey = "group_exp_1_var_1";
            var experiment = Config.GetExperimentFromKey(experimentKey);
            var variation = Config.GetVariationFromKey(experimentKey, variationKey);
            var userAttributes = new UserAttributes
            {
               { "device_type", "iPhone" },
               { "company", "Optimizely" },
               { "location", "San Francisco" }
            };

            // Mocking objects.
            NotificationCallbackMock.Setup(nc => nc.TestDecisionCallback(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<UserAttributes>(), It.IsAny<Dictionary<string, object>>()));
            DecisionServiceMock.Setup(ds => ds.GetVariation(experiment, TestUserId, Config, userAttributes)).Returns(variation);

            var optly = Helper.CreatePrivateOptimizely();
            var optStronglyTyped = optly.GetObject() as Optimizely;

            optStronglyTyped.NotificationCenter.AddNotification(NotificationCenter.NotificationType.Decision, NotificationCallbackMock.Object.TestDecisionCallback);
            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);
            optly.SetFieldOrProperty("ProjectConfigManager", ConfigManager);

            optly.Invoke("GetVariation", experimentKey, TestUserId, userAttributes);
            var decisionInfo = new Dictionary<string, object>
            {
                { "experimentKey", experimentKey },
                { "variationKey", variationKey },
            };

            NotificationCallbackMock.Verify(nc => nc.TestDecisionCallback(DecisionNotificationTypes.FEATURE_TEST, TestUserId, userAttributes, decisionInfo), Times.Once);
        }

        [Test]
        public void TestGetVariationSendsDecisionNotificationWithNullVariationKey()
        {
            var experimentKey = "test_experiment";
            var experiment = Config.GetExperimentFromKey(experimentKey);

            NotificationCallbackMock.Setup(nc => nc.TestDecisionCallback(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<UserAttributes>(), It.IsAny<Dictionary<string, object>>()));
            DecisionServiceMock.Setup(ds => ds.GetVariation(experiment, TestUserId, Config, null)).Returns<Variation>(null);

            var optly = Helper.CreatePrivateOptimizely();
            var optStronglyTyped = optly.GetObject() as Optimizely;

            optStronglyTyped.NotificationCenter.AddNotification(NotificationCenter.NotificationType.Decision, NotificationCallbackMock.Object.TestDecisionCallback);
            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);

            optly.Invoke("GetVariation", experimentKey, TestUserId, null);
            var decisionInfo = new Dictionary<string, object>
            {
                { "experimentKey", experimentKey },
                { "variationKey", null },
            };

            NotificationCallbackMock.Verify(nc => nc.TestDecisionCallback(DecisionNotificationTypes.AB_TEST, TestUserId, new UserAttributes(), decisionInfo), Times.Once);
        }
        public void TestIsFeatureEnabledSendsDecisionNotificationWithFeatureEnabledTrueForFeatureExperiment()
        {
            var featureKey = "double_single_variable_feature";
            var experiment = Config.GetExperimentFromKey("test_experiment_double_feature");
            var variation = Config.GetVariationFromKey("test_experiment_double_feature", "control");
            var featureFlag = Config.GetFeatureFlagFromKey(featureKey);
            var decision = new FeatureDecision(experiment, variation, FeatureDecision.DECISION_SOURCE_FEATURE_TEST);

            DecisionServiceMock.Setup(ds => ds.GetVariationForFeature(featureFlag, TestUserId, Config, null)).Returns(decision);
            NotificationCallbackMock.Setup(nc => nc.TestDecisionCallback(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<UserAttributes>(), It.IsAny<Dictionary<string, object>>()));
            DecisionServiceMock.Setup(ds => ds.GetVariation(experiment, TestUserId, ConfigManager.GetConfig(), null)).Returns(variation);

            var optly = Helper.CreatePrivateOptimizely();
            var optStronglyTyped = optly.GetObject() as Optimizely;

            optStronglyTyped.NotificationCenter.AddNotification(NotificationCenter.NotificationType.Decision, NotificationCallbackMock.Object.TestDecisionCallback);
            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);

            bool result = (bool)optly.Invoke("IsFeatureEnabled", featureKey, TestUserId, null);
            Assert.True(result);

            var decisionInfo = new Dictionary<string, object>
           {
                { "featureKey", featureKey },
                { "featureEnabled", true },
                { "source", FeatureDecision.DECISION_SOURCE_FEATURE_TEST },
                { "sourceExperimentKey", "test_experiment_double_feature" },
                { "sourceVariationKey", "control" },
            };

            NotificationCallbackMock.Verify(nc => nc.TestDecisionCallback(DecisionNotificationTypes.FEATURE, TestUserId, new UserAttributes(), decisionInfo), Times.Once);
        }

        [Test]
        public void TestIsFeatureEnabledSendsDecisionNotificationWithFeatureEnabledFalseForFeatureExperiment()
        {
            var featureKey = "double_single_variable_feature";
            var experiment = Config.GetExperimentFromKey("test_experiment_double_feature");
            var variation = Config.GetVariationFromKey("test_experiment_double_feature", "variation");
            var featureFlag = Config.GetFeatureFlagFromKey(featureKey);
            var decision = new FeatureDecision(experiment, variation, FeatureDecision.DECISION_SOURCE_FEATURE_TEST);
            
            DecisionServiceMock.Setup(ds => ds.GetVariationForFeature(featureFlag, TestUserId, Config, null)).Returns(decision);
            NotificationCallbackMock.Setup(nc => nc.TestDecisionCallback(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<UserAttributes>(), It.IsAny<Dictionary<string, object>>()));
            DecisionServiceMock.Setup(ds => ds.GetVariation(experiment, TestUserId, Config, null)).Returns(variation);

            var optly = Helper.CreatePrivateOptimizely();
            var optStronglyTyped = optly.GetObject() as Optimizely;

            optStronglyTyped.NotificationCenter.AddNotification(NotificationCenter.NotificationType.Decision, NotificationCallbackMock.Object.TestDecisionCallback);
            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);
            optly.SetFieldOrProperty("ProjectConfigManager", ConfigManager);

            bool result = (bool)optly.Invoke("IsFeatureEnabled", featureKey, TestUserId, null);
            Assert.False(result);
            
            var decisionInfo = new Dictionary<string, object>
            {
                { "featureKey", featureKey },
                { "featureEnabled", false },
                { "source", FeatureDecision.DECISION_SOURCE_FEATURE_TEST },
                { "sourceInfo", new Dictionary<string, string>
                    {
                        { "experimentKey", "test_experiment_double_feature" },
                        { "variationKey", "variation" },
                    }
                },
            };

            NotificationCallbackMock.Verify(nc => nc.TestDecisionCallback(DecisionNotificationTypes.FEATURE, TestUserId, new UserAttributes(), It.Is<Dictionary<string, object>>(info => TestData.CompareObjects(info, decisionInfo))), Times.Once);
        }

        [Test]
        public void TestIsFeatureEnabledSendsDecisionNotificationWithFeatureEnabledTrueForFeatureRollout()
        {
            var featureKey = "boolean_single_variable_feature";
            var userAttributes = new UserAttributes
            {
                { "device_type", "iPhone" },
                { "company", "Optimizely" }
            };
            var experiment = Config.GetRolloutFromId("166660").Experiments[0];
            var variation = Config.GetVariationFromKey("177770", "177771");
            var featureFlag = Config.GetFeatureFlagFromKey(featureKey);
            var decision = new FeatureDecision(experiment, variation, FeatureDecision.DECISION_SOURCE_ROLLOUT);

            DecisionServiceMock.Setup(ds => ds.GetVariationForFeature(featureFlag, TestUserId, Config, userAttributes)).Returns(decision);
            NotificationCallbackMock.Setup(nc => nc.TestDecisionCallback(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<UserAttributes>(), It.IsAny<Dictionary<string, object>>()));

            var optly = Helper.CreatePrivateOptimizely();
            optly.SetFieldOrProperty("ProjectConfigManager", ConfigManager);
            var optStronglyTyped = optly.GetObject() as Optimizely;

            optStronglyTyped.NotificationCenter.AddNotification(NotificationCenter.NotificationType.Decision, NotificationCallbackMock.Object.TestDecisionCallback);
            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);

            bool result = (bool)optly.Invoke("IsFeatureEnabled", featureKey, TestUserId, userAttributes);
            Assert.True(result);

            var decisionInfo = new Dictionary<string, object>
            {
                { "featureKey", featureKey },
                { "featureEnabled", true },
                { "source", FeatureDecision.DECISION_SOURCE_ROLLOUT },
                { "sourceInfo", new Dictionary<string, string>() },
            };

            NotificationCallbackMock.Verify(nc => nc.TestDecisionCallback(DecisionNotificationTypes.FEATURE, TestUserId, userAttributes, It.Is<Dictionary<string, object>>(info => TestData.CompareObjects(info, decisionInfo))), Times.Once);
        }

        [Test]
        public void TestIsFeatureEnabledSendsDecisionNotificationWithFeatureEnabledFalseForFeatureRollout()
        {
            var featureKey = "boolean_single_variable_feature";
            var userAttributes = new UserAttributes
            {
                { "device_type", "iPhone" },
                { "company", "Optimizely" }
            };
            var experiment = Config.GetRolloutFromId("166660").Experiments[3];
            var variation = Config.GetVariationFromKey("188880", "188881");
            var featureFlag = Config.GetFeatureFlagFromKey(featureKey);
            var decision = new FeatureDecision(experiment, variation, FeatureDecision.DECISION_SOURCE_ROLLOUT);

            DecisionServiceMock.Setup(ds => ds.GetVariationForFeature(featureFlag, TestUserId, Config, userAttributes)).Returns(decision);
            NotificationCallbackMock.Setup(nc => nc.TestDecisionCallback(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<UserAttributes>(), It.IsAny<Dictionary<string, object>>()));

            var optly = Helper.CreatePrivateOptimizely();
            var optStronglyTyped = optly.GetObject() as Optimizely;

            optStronglyTyped.NotificationCenter.AddNotification(NotificationCenter.NotificationType.Decision, NotificationCallbackMock.Object.TestDecisionCallback);
            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);
            optly.SetFieldOrProperty("ProjectConfigManager", ConfigManager);

            bool result = (bool)optly.Invoke("IsFeatureEnabled", featureKey, TestUserId, userAttributes);
            Assert.False(result);

            var decisionInfo = new Dictionary<string, object>
            {
                { "featureKey", featureKey },
                { "featureEnabled", false },
                { "source", FeatureDecision.DECISION_SOURCE_ROLLOUT },
                { "sourceInfo", new Dictionary<string, string>() },
            };

            NotificationCallbackMock.Verify(nc => nc.TestDecisionCallback(DecisionNotificationTypes.FEATURE, TestUserId, userAttributes, It.Is<Dictionary<string, object>>(info => TestData.CompareObjects(info, decisionInfo))), Times.Once);
        }

        [Test]
        public void TestIsFeatureEnabledSendsDecisionNotificationWithFeatureEnabledFalseWhenUserIsNotBucketed()
        {
            var featureKey = "boolean_single_variable_feature";
            var featureFlag = Config.GetFeatureFlagFromKey(featureKey);
            var decision = new FeatureDecision(null, null, FeatureDecision.DECISION_SOURCE_ROLLOUT);

            DecisionServiceMock.Setup(ds => ds.GetVariationForFeature(featureFlag, TestUserId, Config, null)).Returns(decision);
            NotificationCallbackMock.Setup(nc => nc.TestDecisionCallback(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<UserAttributes>(), It.IsAny<Dictionary<string, object>>()));

            var optly = Helper.CreatePrivateOptimizely();
            var optStronglyTyped = optly.GetObject() as Optimizely;

            optStronglyTyped.NotificationCenter.AddNotification(NotificationCenter.NotificationType.Decision, NotificationCallbackMock.Object.TestDecisionCallback);
            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);
            optly.SetFieldOrProperty("ProjectConfigManager", ConfigManager);

            bool result = (bool)optly.Invoke("IsFeatureEnabled", featureKey, TestUserId, null);
            Assert.False(result);

            var decisionInfo = new Dictionary<string, object>
            {
                { "featureKey", featureKey },
                { "featureEnabled", false },
                { "source", FeatureDecision.DECISION_SOURCE_ROLLOUT },
                { "sourceInfo", new Dictionary<string, string>() },
            };

            NotificationCallbackMock.Verify(nc => nc.TestDecisionCallback(DecisionNotificationTypes.FEATURE, TestUserId, new UserAttributes(), It.Is<Dictionary<string, object>>(info => TestData.CompareObjects(info, decisionInfo))), Times.Once);
        }

        [Test]
        public void TestGetEnabledFeaturesSendDecisionNotificationForBothEnabledAndDisabledFeatures()
        {
            string[] enabledFeatures =
            {
                "double_single_variable_feature",
                "boolean_single_variable_feature",
                "string_single_variable_feature"
            };

            var userAttributes = new UserAttributes
           {
                { "device_type", "iPhone" },
                { "location", "San Francisco" }
            };

            NotificationCallbackMock.Setup(nc => nc.TestDecisionCallback(It.IsAny<string>(), It.IsAny<string>(),
               It.IsAny<UserAttributes>(), It.IsAny<Dictionary<string, object>>()));
            OptimizelyMock.Object.NotificationCenter.AddNotification(NotificationCenter.NotificationType.Decision, NotificationCallbackMock.Object.TestDecisionCallback);

            var actualFeaturesList = OptimizelyMock.Object.GetEnabledFeatures(TestUserId, userAttributes);
            CollectionAssert.AreEquivalent(enabledFeatures, actualFeaturesList);

            NotificationCallbackMock.Verify(nc => nc.TestDecisionCallback(DecisionNotificationTypes.FEATURE, TestUserId, userAttributes, It.Is<Dictionary<string, object>>(info => TestData.CompareObjects(info, new Dictionary<string, object> {
                { "featureKey", "boolean_feature" },
                { "featureEnabled", false },
                { "source", FeatureDecision.DECISION_SOURCE_FEATURE_TEST },
                { "sourceInfo", new Dictionary<string, string>
                    {
                        { "experimentKey", "group_experiment_2" },
                        { "variationKey", "group_exp_2_var_1" },
                    }
                },
            }))), Times.Once);
            NotificationCallbackMock.Verify(nc => nc.TestDecisionCallback(DecisionNotificationTypes.FEATURE, TestUserId, userAttributes, It.Is<Dictionary<string, object>>(info => TestData.CompareObjects(info, new Dictionary<string, object> {
                { "featureKey", "double_single_variable_feature" },
                { "featureEnabled", true },
                { "source", FeatureDecision.DECISION_SOURCE_FEATURE_TEST },
                { "sourceInfo", new Dictionary<string, string>
                    {
                        { "experimentKey", "test_experiment_double_feature" },
                        { "variationKey", "control" },
                    }
                },
            }))), Times.Once);
            NotificationCallbackMock.Verify(nc => nc.TestDecisionCallback(DecisionNotificationTypes.FEATURE, TestUserId, userAttributes, It.Is<Dictionary<string, object>>(info => TestData.CompareObjects(info, new Dictionary<string, object> {
                { "featureKey", "integer_single_variable_feature" },
                { "featureEnabled", false },
                { "source", FeatureDecision.DECISION_SOURCE_FEATURE_TEST },
                { "sourceInfo", new Dictionary<string, string>
                    {
                        { "experimentKey", "test_experiment_integer_feature" },
                        { "variationKey", "control" },
                    }
                },
            }))), Times.Once);
            NotificationCallbackMock.Verify(nc => nc.TestDecisionCallback(DecisionNotificationTypes.FEATURE, TestUserId, userAttributes, It.Is<Dictionary<string, object>>(info => TestData.CompareObjects(info, new Dictionary<string, object> {
                { "featureKey", "boolean_single_variable_feature" },
                { "featureEnabled", true },
                { "source", FeatureDecision.DECISION_SOURCE_ROLLOUT },
                { "sourceInfo", new Dictionary<string, string>() },
            }))), Times.Once);
            NotificationCallbackMock.Verify(nc => nc.TestDecisionCallback(DecisionNotificationTypes.FEATURE, TestUserId, userAttributes, It.Is<Dictionary<string, object>>(info => TestData.CompareObjects(info, new Dictionary<string, object> {
                { "featureKey", "string_single_variable_feature" },
                { "featureEnabled", true },
                { "source", FeatureDecision.DECISION_SOURCE_FEATURE_TEST },
                { "sourceInfo", new Dictionary<string, string>
                    {
                        { "experimentKey", "test_experiment_with_feature_rollout" },
                        { "variationKey", "variation" },
                    }
                },
            }))), Times.Once);
            NotificationCallbackMock.Verify(nc => nc.TestDecisionCallback(DecisionNotificationTypes.FEATURE, TestUserId, userAttributes, It.Is<Dictionary<string, object>>(info => TestData.CompareObjects(info, new Dictionary<string, object> {
                { "featureKey", "multi_variate_feature" },
                { "featureEnabled", false },
                { "source", FeatureDecision.DECISION_SOURCE_ROLLOUT },
                { "sourceInfo", new Dictionary<string, string>() },
            }))), Times.Once);
            NotificationCallbackMock.Verify(nc => nc.TestDecisionCallback(DecisionNotificationTypes.FEATURE, TestUserId, userAttributes, It.Is<Dictionary<string, object>>(info => TestData.CompareObjects(info, new Dictionary<string, object> {
                { "featureKey", "mutex_group_feature" },
                { "featureEnabled", false },
                { "source", FeatureDecision.DECISION_SOURCE_FEATURE_TEST },
                { "sourceInfo", new Dictionary<string, string>
                    {
                        { "experimentKey", "group_experiment_2" },
                        { "variationKey", "group_exp_2_var_1" },
                    }
                },
            }))), Times.Once);
            NotificationCallbackMock.Verify(nc => nc.TestDecisionCallback(DecisionNotificationTypes.FEATURE, TestUserId, userAttributes, It.Is<Dictionary<string, object>>(info => TestData.CompareObjects(info, new Dictionary<string, object> {
                { "featureKey", "empty_feature" },
                { "featureEnabled", false },
                { "source", FeatureDecision.DECISION_SOURCE_ROLLOUT },
                { "sourceInfo", new Dictionary<string, string>() },
            }))), Times.Once);
            NotificationCallbackMock.Verify(nc => nc.TestDecisionCallback(DecisionNotificationTypes.FEATURE, TestUserId, userAttributes, It.Is<Dictionary<string, object>>(info => TestData.CompareObjects(info, new Dictionary<string, object> {
                { "featureKey", "no_rollout_experiment_feature" },
                { "featureEnabled", false },
                { "source", FeatureDecision.DECISION_SOURCE_ROLLOUT },
                { "sourceInfo", new Dictionary<string, string>() },
            }))), Times.Once);
        }

        [Test]
        public void TestGetFeatureVariableDoubleSendsNotificationWhenUserBuckedIntoFeatureExperimentAndVariationIsToggleOn()
        {
            var featureKey = "double_single_variable_feature";
            var featureFlag = Config.GetFeatureFlagFromKey(featureKey);
            var variableKey = "double_variable";
            var expectedValue = 42.42;
            var experiment = Config.GetExperimentFromKey("test_experiment_double_feature");
            var variation = Config.GetVariationFromKey("test_experiment_double_feature", "control");
            var decision = new FeatureDecision(experiment, variation, FeatureDecision.DECISION_SOURCE_FEATURE_TEST);

            DecisionServiceMock.Setup(ds => ds.GetVariationForFeature(featureFlag, TestUserId, Config, null)).Returns(decision);
            NotificationCallbackMock.Setup(nc => nc.TestDecisionCallback(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UserAttributes>(),
                It.IsAny<Dictionary<string, object>>()));

            var optly = Helper.CreatePrivateOptimizely();
            var optStronglyTyped = optly.GetObject() as Optimizely;

            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);
            optly.SetFieldOrProperty("ProjectConfigManager", ConfigManager);

            optStronglyTyped.NotificationCenter.AddNotification(NotificationCenter.NotificationType.Decision, NotificationCallbackMock.Object.TestDecisionCallback);

            var variableValue = (double)optly.Invoke("GetFeatureVariableDouble", featureKey, variableKey, TestUserId, null);
            Assert.AreEqual(expectedValue, variableValue);
            var decisionInfo = new Dictionary<string, object>
            {
                { "featureKey", featureKey },
                { "featureEnabled", true },
                { "variableKey", variableKey },
                { "variableValue", expectedValue },
                { "variableType", FEATUREVARIABLE_DOUBLETYPE },
                { "source", FeatureDecision.DECISION_SOURCE_FEATURE_TEST },
                { "sourceInfo", new Dictionary<string, string>
                    {
                        { "experimentKey", "test_experiment_double_feature" },
                        { "variationKey", "control" },
                    }
                },
            };

            NotificationCallbackMock.Verify(nc => nc.TestDecisionCallback(DecisionNotificationTypes.FEATURE_VARIABLE, TestUserId, new UserAttributes(), It.Is<Dictionary<string, object>>(info => TestData.CompareObjects(info, decisionInfo))), Times.Once);
        }

        [Test]
        public void TestGetFeatureVariableIntegerSendsNotificationWhenUserBuckedIntoFeatureExperimentAndVariationIsToggleOn()
        {
            var featureKey = "integer_single_variable_feature";
            var featureFlag = Config.GetFeatureFlagFromKey(featureKey);
            var variableKey = "integer_variable";
            var expectedValue = 13;
            var experiment = Config.GetExperimentFromKey("test_experiment_integer_feature");
            var variation = Config.GetVariationFromKey("test_experiment_integer_feature", "variation");
            var decision = new FeatureDecision(experiment, variation, FeatureDecision.DECISION_SOURCE_FEATURE_TEST);
            var userAttributes = new UserAttributes
            {
               { "device_type", "iPhone" },
               { "company", "Optimizely" },
               { "location", "San Francisco" }
            };

            DecisionServiceMock.Setup(ds => ds.GetVariationForFeature(featureFlag, TestUserId, Config, userAttributes)).Returns(decision);
            NotificationCallbackMock.Setup(nc => nc.TestDecisionCallback(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UserAttributes>(),
                It.IsAny<Dictionary<string, object>>()));

            var optly = Helper.CreatePrivateOptimizely();
            optly.SetFieldOrProperty("ProjectConfigManager", ConfigManager);

            var optStronglyTyped = optly.GetObject() as Optimizely;

            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);
            optStronglyTyped.NotificationCenter.AddNotification(NotificationCenter.NotificationType.Decision, NotificationCallbackMock.Object.TestDecisionCallback);

            var variableValue = (int)optly.Invoke("GetFeatureVariableInteger", featureKey, variableKey, TestUserId, userAttributes);
            Assert.AreEqual(expectedValue, variableValue);
            var decisionInfo = new Dictionary<string, object>
            {
                { "featureKey", featureKey },
                { "featureEnabled", true },
                { "variableKey", variableKey },
                { "variableValue", expectedValue },
                { "variableType", FEATUREVARIABLE_INTEGERTYPE },
                { "source", FeatureDecision.DECISION_SOURCE_FEATURE_TEST },
                { "sourceInfo", new Dictionary<string, string>
                    {
                        { "experimentKey", "test_experiment_integer_feature" },
                        { "variationKey", "variation" },
                    }
                },
            };

            NotificationCallbackMock.Verify(nc => nc.TestDecisionCallback(DecisionNotificationTypes.FEATURE_VARIABLE, TestUserId, userAttributes, It.Is<Dictionary<string, object>>(info => TestData.CompareObjects(info, decisionInfo))), Times.Once);
        }

        [Test]
        public void TestGetFeatureVariableDoubleSendsNotificationWhenUserBuckedIntoFeatureExperimentAndVariationIsToggleOff()
        {
            var featureKey = "double_single_variable_feature";
            var featureFlag = Config.GetFeatureFlagFromKey(featureKey);
            var variableKey = "double_variable";
            var expectedValue = 14.99;
            var experiment = Config.GetExperimentFromKey("test_experiment_double_feature");
            var variation = Config.GetVariationFromKey("test_experiment_double_feature", "variation");
            var decision = new FeatureDecision(experiment, variation, FeatureDecision.DECISION_SOURCE_FEATURE_TEST);

            DecisionServiceMock.Setup(ds => ds.GetVariationForFeature(featureFlag, TestUserId, Config, null)).Returns(decision);
            NotificationCallbackMock.Setup(nc => nc.TestDecisionCallback(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UserAttributes>(),
                It.IsAny<Dictionary<string, object>>()));

            var optly = Helper.CreatePrivateOptimizely();
            var optStronglyTyped = optly.GetObject() as Optimizely;

            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);
            optly.SetFieldOrProperty("ProjectConfigManager", ConfigManager);
            optStronglyTyped.NotificationCenter.AddNotification(NotificationCenter.NotificationType.Decision, NotificationCallbackMock.Object.TestDecisionCallback);

            var variableValue = (double)optly.Invoke("GetFeatureVariableDouble", featureKey, variableKey, TestUserId, null);
            Assert.AreEqual(expectedValue, variableValue);
            var decisionInfo = new Dictionary<string, object>
            {
                { "featureKey", featureKey },
                { "featureEnabled", false },
                { "variableKey", variableKey },
                { "variableValue", expectedValue },
                { "variableType", FEATUREVARIABLE_DOUBLETYPE },
                { "source", FeatureDecision.DECISION_SOURCE_FEATURE_TEST },
                { "sourceInfo", new Dictionary<string, string>
                    {
                        { "experimentKey", "test_experiment_double_feature" },
                        { "variationKey", "variation" },
                    }
                },
            };

            NotificationCallbackMock.Verify(nc => nc.TestDecisionCallback(DecisionNotificationTypes.FEATURE_VARIABLE, TestUserId, new UserAttributes(), It.Is<Dictionary<string, object>>(info => TestData.CompareObjects(info, decisionInfo))), Times.Once);
        }

        [Test]
        public void TestGetFeatureVariableIntegerSendsNotificationWhenUserBuckedIntoFeatureExperimentAndVariationIsToggleOff()
        {
            var featureKey = "integer_single_variable_feature";
            var featureFlag = Config.GetFeatureFlagFromKey(featureKey);
            var variableKey = "integer_variable";
            var expectedValue = 7;
            var experiment = Config.GetExperimentFromKey("test_experiment_integer_feature");
            var variation = Config.GetVariationFromKey("test_experiment_integer_feature", "control");
            var decision = new FeatureDecision(experiment, variation, FeatureDecision.DECISION_SOURCE_FEATURE_TEST);
            var userAttributes = new UserAttributes
            {
               { "device_type", "iPhone" },
               { "company", "Optimizely" },
               { "location", "San Francisco" }
            };

            DecisionServiceMock.Setup(ds => ds.GetVariationForFeature(featureFlag, TestUserId, Config, userAttributes)).Returns(decision);
            NotificationCallbackMock.Setup(nc => nc.TestDecisionCallback(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UserAttributes>(),
                It.IsAny<Dictionary<string, object>>()));

            var optly = Helper.CreatePrivateOptimizely();
            optly.SetFieldOrProperty("ProjectConfigManager", ConfigManager);
            var optStronglyTyped = optly.GetObject() as Optimizely;

            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);
            optStronglyTyped.NotificationCenter.AddNotification(NotificationCenter.NotificationType.Decision, NotificationCallbackMock.Object.TestDecisionCallback);

            var variableValue = (int)optly.Invoke("GetFeatureVariableInteger", featureKey, variableKey, TestUserId, userAttributes);
            Assert.AreEqual(expectedValue, variableValue);
            var decisionInfo = new Dictionary<string, object>
            {
                { "featureKey", featureKey },
                { "featureEnabled", false },
                { "variableKey", variableKey },
                { "variableValue", expectedValue },
                { "variableType", FEATUREVARIABLE_INTEGERTYPE },
                { "source", FeatureDecision.DECISION_SOURCE_FEATURE_TEST },
                { "sourceInfo", new Dictionary<string, string>
                    {
                        { "experimentKey", "test_experiment_integer_feature" },
                        { "variationKey", "control" },
                    }
                },
            };

            NotificationCallbackMock.Verify(nc => nc.TestDecisionCallback(DecisionNotificationTypes.FEATURE_VARIABLE, TestUserId, userAttributes, It.Is<Dictionary<string, object>>(info => TestData.CompareObjects(info, decisionInfo))), Times.Once);
        }

        [Test]
        public void TestGetFeatureVariableBooleanSendsNotificationWhenUserBuckedIntoRolloutAndVariationIsToggleOn()
        {
            var featureKey = "boolean_single_variable_feature";
            var featureFlag = Config.GetFeatureFlagFromKey(featureKey);
            var variableKey = "boolean_variable";
            var expectedValue = true;
            var experiment = Config.GetRolloutFromId("166660").Experiments[0];
            var variation = Config.GetVariationFromKey(experiment.Key, "177771");
            var decision = new FeatureDecision(experiment, variation, FeatureDecision.DECISION_SOURCE_ROLLOUT);

            DecisionServiceMock.Setup(ds => ds.GetVariationForFeature(featureFlag, TestUserId, Config, null)).Returns(decision);
            NotificationCallbackMock.Setup(nc => nc.TestDecisionCallback(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UserAttributes>(),
                It.IsAny<Dictionary<string, object>>()));

            var optly = Helper.CreatePrivateOptimizely();
            optly.SetFieldOrProperty("ProjectConfigManager", ConfigManager);
            var optStronglyTyped = optly.GetObject() as Optimizely;

            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);
            optStronglyTyped.NotificationCenter.AddNotification(NotificationCenter.NotificationType.Decision, NotificationCallbackMock.Object.TestDecisionCallback);

            var variableValue = (bool)optly.Invoke("GetFeatureVariableBoolean", featureKey, variableKey, TestUserId, null);
            Assert.AreEqual(expectedValue, variableValue);
            var decisionInfo = new Dictionary<string, object>
            {
                { "featureKey", featureKey },
                { "featureEnabled", true },
                { "variableKey", variableKey },
                { "variableValue", expectedValue },
                { "variableType", FEATUREVARIABLE_BOOLEANTYPE },
                { "source", FeatureDecision.DECISION_SOURCE_ROLLOUT },
                { "sourceInfo", new Dictionary<string, string>() },
            };

            NotificationCallbackMock.Verify(nc => nc.TestDecisionCallback(DecisionNotificationTypes.FEATURE_VARIABLE, TestUserId, new UserAttributes(), It.Is<Dictionary<string, object>>(info => TestData.CompareObjects(info, decisionInfo))), Times.Once);
        }

        [Test]
        public void TestGetFeatureVariableStringSendsNotificationWhenUserBuckedIntoRolloutAndVariationIsToggleOn()
        {
            var featureKey = "string_single_variable_feature";
            var featureFlag = Config.GetFeatureFlagFromKey(featureKey);
            var variableKey = "string_variable";
            var expectedValue = "cta_4";
            var experiment = Config.GetRolloutFromId("166661").Experiments[0];
            var variation = Config.GetVariationFromKey(experiment.Key, "177775");
            var decision = new FeatureDecision(experiment, variation, FeatureDecision.DECISION_SOURCE_ROLLOUT);
            var userAttributes = new UserAttributes
            {
               { "device_type", "iPhone" },
               { "company", "Optimizely" },
               { "location", "San Francisco" }
            };

            DecisionServiceMock.Setup(ds => ds.GetVariationForFeature(featureFlag, TestUserId, Config, userAttributes)).Returns(decision);
            NotificationCallbackMock.Setup(nc => nc.TestDecisionCallback(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UserAttributes>(),
                It.IsAny<Dictionary<string, object>>()));

            var optly = Helper.CreatePrivateOptimizely();

            optly.SetFieldOrProperty("ProjectConfigManager", ConfigManager);
            var optStronglyTyped = optly.GetObject() as Optimizely;

            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);
            optStronglyTyped.NotificationCenter.AddNotification(NotificationCenter.NotificationType.Decision, NotificationCallbackMock.Object.TestDecisionCallback);

            var variableValue = (string)optly.Invoke("GetFeatureVariableString", featureKey, variableKey, TestUserId, userAttributes);
            Assert.AreEqual(expectedValue, variableValue);
            var decisionInfo = new Dictionary<string, object>
            {
                { "featureKey", featureKey },
                { "featureEnabled", true },
                { "variableKey", variableKey },
                { "variableValue", expectedValue },
                { "variableType", FEATUREVARIABLE_STRINGTYPE },
                { "source", FeatureDecision.DECISION_SOURCE_ROLLOUT },
                { "sourceInfo", new Dictionary<string, string>() },
            };

            NotificationCallbackMock.Verify(nc => nc.TestDecisionCallback(DecisionNotificationTypes.FEATURE_VARIABLE, TestUserId, userAttributes, It.Is<Dictionary<string, object>>(info => TestData.CompareObjects(info, decisionInfo))), Times.Once);
        }

        [Test]
        public void TestGetFeatureVariableBooleanSendsNotificationWhenUserBuckedIntoRolloutAndVariationIsToggleOff()
        {
            var featureKey = "boolean_single_variable_feature";
            var featureFlag = Config.GetFeatureFlagFromKey(featureKey);
            var variableKey = "boolean_variable";
            var expectedValue = true;
            var experiment = Config.GetRolloutFromId("166660").Experiments[3];
            var variation = Config.GetVariationFromKey(experiment.Key, "177782");
            var decision = new FeatureDecision(experiment, variation, FeatureDecision.DECISION_SOURCE_ROLLOUT);

            DecisionServiceMock.Setup(ds => ds.GetVariationForFeature(featureFlag, TestUserId, Config, null)).Returns(decision);
            NotificationCallbackMock.Setup(nc => nc.TestDecisionCallback(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UserAttributes>(),
                It.IsAny<Dictionary<string, object>>()));

            var optly = Helper.CreatePrivateOptimizely();
            var optStronglyTyped = optly.GetObject() as Optimizely;

            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);
            optly.SetFieldOrProperty("ProjectConfigManager", ConfigManager);
            optStronglyTyped.NotificationCenter.AddNotification(NotificationCenter.NotificationType.Decision, NotificationCallbackMock.Object.TestDecisionCallback);

            var variableValue = (bool)optly.Invoke("GetFeatureVariableBoolean", featureKey, variableKey, TestUserId, null);
            Assert.AreEqual(expectedValue, variableValue);
            var decisionInfo = new Dictionary<string, object>
            {
                { "featureKey", featureKey },
                { "featureEnabled", false },
                { "variableKey", variableKey },
                { "variableValue", expectedValue },
                { "variableType", FEATUREVARIABLE_BOOLEANTYPE },
                { "source", FeatureDecision.DECISION_SOURCE_ROLLOUT },
                { "sourceInfo", new Dictionary<string, string>()},
            };

            NotificationCallbackMock.Verify(nc => nc.TestDecisionCallback(DecisionNotificationTypes.FEATURE_VARIABLE, TestUserId, new UserAttributes(), It.Is<Dictionary<string, object>>(info => TestData.CompareObjects(info, decisionInfo))), Times.Once);
        }

        [Test]
        public void TestGetFeatureVariableStringSendsNotificationWhenUserBuckedIntoRolloutAndVariationIsToggleOff()
        {
            var featureKey = "string_single_variable_feature";
            var featureFlag = Config.GetFeatureFlagFromKey(featureKey);
            var variableKey = "string_variable";
            var expectedValue = "wingardium leviosa";
            var experiment = Config.GetRolloutFromId("166661").Experiments[2];
            var variation = Config.GetVariationFromKey(experiment.Key, "177784");
            var decision = new FeatureDecision(experiment, variation, FeatureDecision.DECISION_SOURCE_ROLLOUT);
            var userAttributes = new UserAttributes
            {
               { "device_type", "iPhone" },
               { "company", "Optimizely" },
               { "location", "San Francisco" }
            };

            DecisionServiceMock.Setup(ds => ds.GetVariationForFeature(featureFlag, TestUserId, Config, userAttributes)).Returns(decision);
            NotificationCallbackMock.Setup(nc => nc.TestDecisionCallback(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UserAttributes>(),
                It.IsAny<Dictionary<string, object>>()));

            var optly = Helper.CreatePrivateOptimizely();
            optly.SetFieldOrProperty("ProjectConfigManager", ConfigManager);
            var optStronglyTyped = optly.GetObject() as Optimizely;

            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);
            optStronglyTyped.NotificationCenter.AddNotification(NotificationCenter.NotificationType.Decision, NotificationCallbackMock.Object.TestDecisionCallback);

            var variableValue = (string)optly.Invoke("GetFeatureVariableString", featureKey, variableKey, TestUserId, userAttributes);
            Assert.AreEqual(expectedValue, variableValue);
            var decisionInfo = new Dictionary<string, object>
            {
                { "featureKey", featureKey },
                { "featureEnabled", false },
                { "variableKey", variableKey },
                { "variableValue", expectedValue },
                { "variableType", FEATUREVARIABLE_STRINGTYPE },
                { "source", FeatureDecision.DECISION_SOURCE_ROLLOUT },
                { "sourceInfo", new Dictionary<string, string>() },
            };

            NotificationCallbackMock.Verify(nc => nc.TestDecisionCallback(DecisionNotificationTypes.FEATURE_VARIABLE, TestUserId, userAttributes, It.Is<Dictionary<string, object>>(info => TestData.CompareObjects(info, decisionInfo))), Times.Once);
        }

        [Test]
        public void TestGetFeatureVariableDoubleSendsNotificationWhenUserNotBuckedIntoBothFeatureExperimentAndRollout()
        {
            var featureKey = "double_single_variable_feature";
            var featureFlag = Config.GetFeatureFlagFromKey("double_single_variable_feature");
            var variableKey = "double_variable";
            var expectedValue = 14.99;
            var decision = new FeatureDecision(null, null, FeatureDecision.DECISION_SOURCE_ROLLOUT);

            DecisionServiceMock.Setup(ds => ds.GetVariationForFeature(featureFlag, TestUserId, Config, null)).Returns(decision);
            NotificationCallbackMock.Setup(nc => nc.TestDecisionCallback(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UserAttributes>(),
                It.IsAny<Dictionary<string, object>>()));

            var optly = Helper.CreatePrivateOptimizely();
            var optStronglyTyped = optly.GetObject() as Optimizely;

            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);
            optly.SetFieldOrProperty("ProjectConfigManager", ConfigManager);
            optStronglyTyped.NotificationCenter.AddNotification(NotificationCenter.NotificationType.Decision, NotificationCallbackMock.Object.TestDecisionCallback);

            var variableValue = (double)optly.Invoke("GetFeatureVariableDouble", featureKey, variableKey, TestUserId, null);
            Assert.AreEqual(expectedValue, variableValue);
            var decisionInfo = new Dictionary<string, object>
            {
                { "featureKey", featureKey },
                { "featureEnabled", false },
                { "variableKey", variableKey },
                { "variableValue", expectedValue },
                { "variableType", FEATUREVARIABLE_DOUBLETYPE },
                { "source", FeatureDecision.DECISION_SOURCE_ROLLOUT },
                { "sourceInfo", new Dictionary<string, string>() },
            };

            NotificationCallbackMock.Verify(nc => nc.TestDecisionCallback(DecisionNotificationTypes.FEATURE_VARIABLE, TestUserId, new UserAttributes(), It.Is<Dictionary<string, object>>(info => TestData.CompareObjects(info, decisionInfo))), Times.Once);
        }

        #endregion // Decision Listener

        #region DFM Notification
        [Test]
        public void TestDFMNotificationWhenProjectConfigIsUpdated()
        {
            NotificationCenter notificationCenter = new NotificationCenter();
            NotificationCallbackMock.Setup(notification => notification.TestConfigUpdateCallback());


            var httpManager = new HttpProjectConfigManager.Builder()
                                                          .WithSdkKey("QBw9gFM8oTn7ogY9ANCC1z")
                                                          .WithLogger(LoggerMock.Object)
                                                          .WithPollingInterval(TimeSpan.FromMilliseconds(1000))
                                                          .WithStartByDefault(false)
                                                          .WithBlockingTimeoutPeriod(TimeSpan.FromMilliseconds(500))
                                                          .WithNotificationCenter(notificationCenter)
                                                          .Build();

            var optimizely = new Optimizely(httpManager, notificationCenter);
            httpManager.Start();
            optimizely.NotificationCenter.AddNotification(NotificationCenter.NotificationType.OptimizelyConfigUpdate, NotificationCallbackMock.Object.TestConfigUpdateCallback);
            httpManager.OnReady().Wait(-1);

            NotificationCallbackMock.Verify(nc => nc.TestConfigUpdateCallback(), Times.Once);
        }

        [Test]
        public void TestDFMWhenDatafileProvidedDoesNotNotifyWithoutStart()
        {
            var httpManager = new HttpProjectConfigManager.Builder()
                .WithSdkKey("QBw9gFM8oTn7ogY9ANCC1z")
                .WithDatafile(TestData.Datafile)
                .WithLogger(LoggerMock.Object)
                .WithPollingInterval(TimeSpan.FromMilliseconds(1000))
                .WithBlockingTimeoutPeriod(TimeSpan.FromMilliseconds(500))                
                .Build();


            var optimizely = new Optimizely(httpManager);
            optimizely.NotificationCenter.AddNotification(NotificationCenter.NotificationType.OptimizelyConfigUpdate, NotificationCallbackMock.Object.TestConfigUpdateCallback);
            httpManager.OnReady().Wait(-1);

            NotificationCallbackMock.Verify(nc => nc.TestConfigUpdateCallback(), Times.Never);
        }



        #endregion // DFM Notification

        #endregion // Test NotificationCenter

        #region Test GetEnabledFeatures

        [Test]
        public void TestGetEnabledFeaturesWithInvalidDatafile()
        {
            var optly = new Optimizely("Random datafile", null, LoggerMock.Object);

            Assert.IsEmpty(optly.GetEnabledFeatures("some_user", null));
            
            LoggerMock.Verify(l => l.Log(LogLevel.ERROR, "Datafile has invalid format. Failing 'GetEnabledFeatures'."), Times.Once);

        }

        [Test]
        public void TestGetEnabledFeaturesWithNoFeatureEnabledForUser()
        {
            var userAttributes = new UserAttributes
            {
                { "device_type", "iPhone" },
                { "location", "San Francisco" }
            };

            OptimizelyMock.Setup(om => om.IsFeatureEnabled(It.IsAny<string>(), TestUserId, It.IsAny<UserAttributes>())).Returns(false);
            Assert.IsEmpty(OptimizelyMock.Object.GetEnabledFeatures(TestUserId, userAttributes));
        }

        [Test]
        public void TestGetEnabledFeaturesWithSomeFeaturesEnabledForUser()
        {
            string[] enabledFeatures = 
            {
                "boolean_feature",
                "double_single_variable_feature",
                "string_single_variable_feature",
                "multi_variate_feature",
                "empty_feature"
            };
            string[] notEnabledFeatures =
            {
                "integer_single_variable_feature",
                "boolean_single_variable_feature",
                "mutex_group_feature",
                "no_rollout_experiment_feature"
            };
            var userAttributes = new UserAttributes
            {
                { "device_type", "iPhone" },
                { "location", "San Francisco" }
            };

            OptimizelyMock.Setup(om => om.IsFeatureEnabled(It.IsIn<string>(enabledFeatures), TestUserId, 
                It.IsAny<UserAttributes>())).Returns(true);
            OptimizelyMock.Setup(om => om.IsFeatureEnabled(It.IsIn<string>(notEnabledFeatures), TestUserId,
                It.IsAny<UserAttributes>())).Returns(false);

            var actualFeaturesList = OptimizelyMock.Object.GetEnabledFeatures(TestUserId, userAttributes);
            
            // Verify that the returned feature list contains only enabledFeatures.
            CollectionAssert.AreEquivalent(enabledFeatures, actualFeaturesList);
            Array.ForEach(notEnabledFeatures, nef => CollectionAssert.DoesNotContain(actualFeaturesList, nef));
        }

        #endregion // Test GetEnabledFeatures

        #region Test ValidateStringInputs

        [Test]
        public void TestValidateStringInputsWithValidValues()
        {
            var optly = Helper.CreatePrivateOptimizely();

            bool result = (bool)optly.Invoke("ValidateStringInputs", new Dictionary<string, string> { { Optimizely.EXPERIMENT_KEY, "test_experiment" } });
            Assert.True(result);

            result = (bool)optly.Invoke("ValidateStringInputs", new Dictionary<string, string> { { Optimizely.EVENT_KEY, "buy_now_event" } });
            Assert.True(result);
        }

        [Test]
        public void TestValidateStringInputsWithInvalidValues()
        {
            var optly = Helper.CreatePrivateOptimizely();

            bool result = (bool)optly.Invoke("ValidateStringInputs", new Dictionary<string, string> { { Optimizely.EXPERIMENT_KEY, "" } });
            Assert.False(result);

            result = (bool)optly.Invoke("ValidateStringInputs", new Dictionary<string, string> { { Optimizely.EVENT_KEY, null } });
            Assert.False(result);
        }

        [Test]
        public void TestValidateStringInputsWithUserId()
        {
            var optly = Helper.CreatePrivateOptimizely();

            bool result = (bool)optly.Invoke("ValidateStringInputs", new Dictionary<string, string> { { Optimizely.USER_ID, "testUser" } });
            Assert.True(result);

            result = (bool)optly.Invoke("ValidateStringInputs", new Dictionary<string, string> { { Optimizely.USER_ID, "" } });
            Assert.True(result);

            result = (bool)optly.Invoke("ValidateStringInputs", new Dictionary<string, string> { { Optimizely.USER_ID, null } });
            Assert.False(result);
        }

        [Test]
        public void TestActivateValidateInputValues()
        {
            // Verify that ValidateStringInputs does not log error for valid values.
            var variation = Optimizely.Activate("test_experiment", "test_user");
            LoggerMock.Verify(l => l.Log(LogLevel.ERROR, "Provided User Id is in invalid format."), Times.Never);
            LoggerMock.Verify(l => l.Log(LogLevel.ERROR, "Provided Experiment Key is in invalid format."), Times.Never);

            // Verify that ValidateStringInputs logs error for invalid values.
            variation = Optimizely.Activate("", null);
            LoggerMock.Verify(l => l.Log(LogLevel.ERROR, "Provided User Id is in invalid format."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.ERROR, "Provided Experiment Key is in invalid format."), Times.Once);
        }

        [Test]
        public void TestGetVariationValidateInputValues()
        {
            // Verify that ValidateStringInputs does not log error for valid values.
            var variation = Optimizely.GetVariation("test_experiment", "test_user");
            LoggerMock.Verify(l => l.Log(LogLevel.ERROR, "Provided User Id is in invalid format."), Times.Never);
            LoggerMock.Verify(l => l.Log(LogLevel.ERROR, "Provided Experiment Key is in invalid format."), Times.Never);

            // Verify that ValidateStringInputs logs error for invalid values.
            variation = Optimizely.GetVariation("", null);
            LoggerMock.Verify(l => l.Log(LogLevel.ERROR, "Provided User Id is in invalid format."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.ERROR, "Provided Experiment Key is in invalid format."), Times.Once);
        }

        [Test]
        public void TestTrackValidateInputValues()
        {
            // Verify that ValidateStringInputs does not log error for valid values.
            Optimizely.Track("purchase", "test_user");
            LoggerMock.Verify(l => l.Log(LogLevel.ERROR, "Provided User Id is in invalid format."), Times.Never);
            LoggerMock.Verify(l => l.Log(LogLevel.ERROR, "Provided Event Key is in invalid format."), Times.Never);

            // Verify that ValidateStringInputs logs error for invalid values.
            Optimizely.Track("", null);
            LoggerMock.Verify(l => l.Log(LogLevel.ERROR, "Provided User Id is in invalid format."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.ERROR, "Provided Event Key is in invalid format."), Times.Once);
        }

        #endregion // Test ValidateStringInputs

        #region Test Audience Match Types

        [Test]
        public void TestActivateWithTypedAudiences()
        {
            var variation = OptimizelyWithTypedAudiences.Activate("typed_audience_experiment", "user1", new UserAttributes
            {
                { "house", "Gryffindor" }
            });

            Assert.AreEqual("A", variation.Key);
            EventDispatcherMock.Verify(dispatcher => dispatcher.DispatchEvent(It.IsAny<LogEvent>()), Times.Once);

            variation = OptimizelyWithTypedAudiences.Activate("typed_audience_experiment", "user1", new UserAttributes
            {
                { "lasers", 45.5 }
            });

            Assert.AreEqual("A", variation.Key);
            EventDispatcherMock.Verify(dispatcher => dispatcher.DispatchEvent(It.IsAny<LogEvent>()), Times.Exactly(2));
        }

        [Test]
        public void TestActivateExcludeUserFromExperimentWithTypedAudiences()
        {
            var variation = OptimizelyWithTypedAudiences.Activate("typed_audience_experiment", "user1", new UserAttributes
            {
                { "house", "Hufflepuff" }
            });

            Assert.Null(variation);
            EventDispatcherMock.Verify(dispatcher => dispatcher.DispatchEvent(It.IsAny<LogEvent>()), Times.Never);
        }

        [Test]
        public void TestTrackWithTypedAudiences()
        {
            OptimizelyWithTypedAudiences.Track("item_bought", "user1", new UserAttributes
            {
                { "house", "Welcome to Slytherin!" }
            });

            EventDispatcherMock.Verify(dispatcher => dispatcher.DispatchEvent(It.IsAny<LogEvent>()), Times.Once);
        }

        [Test]
        public void TestTrackDoesNotExcludeUserFromExperimentWhenAttributesMismatchWithTypedAudiences()
        {
            OptimizelyWithTypedAudiences.Track("item_bought", "user1", new UserAttributes
            {
                { "house", "Hufflepuff" }
            });

            EventDispatcherMock.Verify(dispatcher => dispatcher.DispatchEvent(It.IsAny<LogEvent>()), Times.Once);
        }

        [Test]
        public void TestIsFeatureEnabledWithTypedAudiences()
        {
            var featureEnabled = OptimizelyWithTypedAudiences.IsFeatureEnabled("feat_no_vars", "user1", new UserAttributes
            {
                { "favorite_ice_cream", "chocolate" }
            });

            Assert.True(featureEnabled);

            featureEnabled = OptimizelyWithTypedAudiences.IsFeatureEnabled("feat_no_vars", "user1", new UserAttributes
            {
                { "lasers", 45.5 }
            });

            Assert.True(featureEnabled);
        }

        [Test]
        public void TestIsFeatureEnabledExcludeUserFromExperimentWithTypedAudiences()
        {
            var featureEnabled = OptimizelyWithTypedAudiences.IsFeatureEnabled("feat", "user1", new UserAttributes { });
            Assert.False(featureEnabled);
        }

        [Test]
        public void TestGetFeatureVariableStringReturnVariableValueWithTypedAudiences()
        {
            var variableValue = OptimizelyWithTypedAudiences.GetFeatureVariableString("feat_with_var", "x", "user1", new UserAttributes
            {
                { "lasers", 71 }
            });

            Assert.AreEqual(variableValue, "xyz");

            variableValue = OptimizelyWithTypedAudiences.GetFeatureVariableString("feat_with_var", "x", "user1", new UserAttributes
            {
                { "should_do_it", true }
            });

            Assert.AreEqual(variableValue, "xyz");
        }

        [Test]
        public void TestGetFeatureVariableStringReturnDefaultVariableValueWithTypedAudiences()
        {
            var variableValue = OptimizelyWithTypedAudiences.GetFeatureVariableString("feat_with_var", "x", "user1", new UserAttributes
            {
                { "lasers", 50 }
            });

            Assert.AreEqual(variableValue, "x");
        }

        #endregion // Test Audience Match Types

        #region Test Audience Combinations

        [Test]
        public void TestActivateIncludesUserInExperimentWithComplexAudienceConditions()
        {
            var userAttributes = new UserAttributes
            {
                { "house", "Welcome to Slytherin!" },
                { "lasers", 45.5 }
            };

            // Should be included via substring match string audience with id '3988293898' and exact match number audience with id '3468206646'
            var variation = OptimizelyWithTypedAudiences.Activate("audience_combinations_experiment", "user1", userAttributes);
            Assert.AreEqual("A", variation.Key);

            EventDispatcherMock.Verify(dispatcher => dispatcher.DispatchEvent(It.IsAny<LogEvent>()), Times.Once);
        }

        [Test]
        public void TestActivateExcludesUserFromExperimentWithComplexAudienceConditions()
        {
            var userAttributes = new UserAttributes
            {
                { "house", "Hufflepuff" },
                { "lasers", 45.5 }
            };

            // Should be excluded as substring audience with id '3988293898' does not match, so the overall conditions fail.
            var variation = OptimizelyWithTypedAudiences.Activate("audience_combinations_experiment", "user1", userAttributes);
            Assert.Null(variation);

            EventDispatcherMock.Verify(dispatcher => dispatcher.DispatchEvent(It.IsAny<LogEvent>()), Times.Never);
        }

        [Test]
        public void TestTrackIncludesUserInExperimentWithComplexAudienceConditions()
        {
            var userAttributes = new UserAttributes
            {
                { "house", "Gryffindor" },
                { "should_do_it", true }
            };

            // Should be included via exact match string audience with id '3468206642' and exact match boolean audience with id '3468206646'
            OptimizelyWithTypedAudiences.Track("user_signed_up", "user1", userAttributes);

            EventDispatcherMock.Verify(dispatcher => dispatcher.DispatchEvent(It.IsAny<LogEvent>()), Times.Once);
        }

        [Test]
        public void TestTrackDoesNotExcludesUserFromExperimentWhenAttributesMismatchWithAudienceConditions()
        {
            var userAttributes = new UserAttributes
            {
                { "house", "Gryffindor" },
                { "should_do_it", false }
            };

            // Should be excluded as exact match boolean audience with id '3468206643' does not match so the overall conditions fail.
            OptimizelyWithTypedAudiences.Track("user_signed_up", "user1", userAttributes);

            EventDispatcherMock.Verify(dispatcher => dispatcher.DispatchEvent(It.IsAny<LogEvent>()), Times.Once);
        }

        [Test]
        public void TestIsFeatureEnabledIncludesUserInRolloutWithComplexAudienceConditions()
        {
            var userAttributes = new UserAttributes
            {
                { "house", "Welcome to Slytherin!" },
                { "favorite_ice_cream", "walls" }
            };

            // Should be included via substring match string audience with id '3988293898' and exists audience with id '3988293899'
            var result = OptimizelyWithTypedAudiences.IsFeatureEnabled("feat2", "user1", userAttributes);
            Assert.True(result);
        }

        [Test]
        public void TestIsFeatureEnabledExcludesUserFromRolloutWithComplexAudienceConditions()
        {
            var userAttributes = new UserAttributes
            {
                { "house", "Ravenclaw" },
                { "lasers", 45.5 }
            };

            // Should be excluded - substring match string audience with id '3988293898' does not match,
            // and no audience in the other branch of the 'and' matches either
            var result = OptimizelyWithTypedAudiences.IsFeatureEnabled("audience_combinations_experiment", "user1", userAttributes);
            Assert.False(result);
        }

        [Test]
        public void TestGetFeatureVariableIntegerReturnsVariableValueWithComplexAudienceConditions()
        {
            var userAttributes = new UserAttributes
            {
                { "house", "Gryffindor" },
                { "lasers", 700 }
            };

            // Should be included via substring match string audience with id '3988293898' and exists audience with id '3988293899'
            var value = OptimizelyWithTypedAudiences.GetFeatureVariableInteger("feat2_with_var", "z", "user1", userAttributes);
            Assert.AreEqual(150, value);
        }

        [Test]
        public void TestGetFeatureVariableIntegerReturnsDefaultValueWithComplexAudienceConditions()
        {
            var userAttributes = new UserAttributes {};

            // Should be excluded - no audiences match with no attributes.
            var value = OptimizelyWithTypedAudiences.GetFeatureVariableInteger("feat2_with_var", "z", "user1", userAttributes);
            Assert.AreEqual(10, value);
        }

        #endregion // Test Audience Combinations

        #region Disposable Optimizely

        [Test]
        public void TestOptimizelyDisposeAlsoDisposedConfigManager()
        {
            var httpManager = new HttpProjectConfigManager.Builder()
                                                          .WithSdkKey("QBw9gFM8oTn7ogY9ANCC1z")
                                                          .WithLogger(LoggerMock.Object)
                                                          .WithPollingInterval(TimeSpan.FromMilliseconds(5000))
                                                          .WithBlockingTimeoutPeriod(TimeSpan.FromMilliseconds(500))                                                          
                                                          .Build();
            var optimizely = new Optimizely(httpManager);
            optimizely.Dispose();

            Assert.True(optimizely.Disposed);
            Assert.True(httpManager.Disposed);
        }

        [Test]
        public void TestDisposeInvalidateObject()
        {
            var httpManager = new HttpProjectConfigManager.Builder()
                                                          .WithSdkKey("QBw9gFM8oTn7ogY9ANCC1z")
                                                          .WithLogger(LoggerMock.Object)
                                                          .WithPollingInterval(TimeSpan.FromMilliseconds(5000))
                                                          .WithBlockingTimeoutPeriod(TimeSpan.FromMilliseconds(500))
                                                          .Build();
            var optimizely = new Optimizely(httpManager);
            optimizely.Dispose();

            Assert.False(optimizely.IsValid);
        }

        [Test]
        public void TestAfterDisposeAPIsNoLongerValid()
        {
            var httpManager = new HttpProjectConfigManager.Builder()
                                                          .WithSdkKey("QBw9gFM8oTn7ogY9ANCC1z")
                                                          .WithDatafile(TestData.Datafile)
                                                          .WithLogger(LoggerMock.Object)                                                          
                                                          .WithPollingInterval(TimeSpan.FromMilliseconds(50000))
                                                          .WithBlockingTimeoutPeriod(TimeSpan.FromMilliseconds(500))
                                                          .Build(true);
            var optimizely = new Optimizely(httpManager);
            httpManager.Start();
            var activate = optimizely.Activate("test_experiment", TestUserId, new UserAttributes() {
                { "device_type", "iPhone" }, { "location", "San Francisco" } });
            Assert.NotNull(activate);
            optimizely.Dispose();
            var activateAfterDispose = optimizely.Activate("test_experiment", TestUserId, new UserAttributes() {
                { "device_type", "iPhone" }, { "location", "San Francisco" } });
            Assert.Null(activateAfterDispose);
        }

        [Test]
        public void TestNonDisposableConfigManagerDontCrash()
        {
            var fallbackConfigManager = new FallbackProjectConfigManager(Config);

            var optimizely = new Optimizely(fallbackConfigManager);
            optimizely.Dispose();
            Assert.True(optimizely.Disposed);
        }

        [Test]
        public void TestAfterDisposeAPIsShouldNotCrash()
        {
            var fallbackConfigManager = new FallbackProjectConfigManager(Config);

            var optimizely = new Optimizely(fallbackConfigManager);
            optimizely.Dispose();
            Assert.True(optimizely.Disposed);

            Assert.IsNull(optimizely.GetVariation(string.Empty, string.Empty));
            Assert.IsNull(optimizely.Activate(string.Empty, string.Empty));
            optimizely.Track(string.Empty, string.Empty);
            Assert.IsFalse(optimizely.IsFeatureEnabled(string.Empty, string.Empty));
            Assert.AreEqual(optimizely.GetEnabledFeatures(string.Empty).Count, 0);
            Assert.IsNull(optimizely.GetFeatureVariableBoolean(string.Empty, string.Empty, string.Empty));
            Assert.IsNull(optimizely.GetFeatureVariableString(string.Empty, string.Empty, string.Empty));
            Assert.IsNull(optimizely.GetFeatureVariableDouble(string.Empty, string.Empty, string.Empty));
            Assert.IsNull(optimizely.GetFeatureVariableInteger(string.Empty, string.Empty, string.Empty));

        }

        #endregion

        #region Test GetOptimizelyConfig

        [Test]
        public void TestGetOptimizelyConfigNullConfig()
        {
            var optly = new Optimizely(new FallbackProjectConfigManager(null));
            var optimizelyConfig = optly.GetOptimizelyConfig();

            Assert.IsNull(optimizelyConfig);
        }

        #endregion
    }
}
