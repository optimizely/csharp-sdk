﻿/* 
 * Copyright 2017, Optimizely
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

namespace OptimizelySDK.Tests
{
    [TestFixture]
    public class OptimizelyTest
    {
        private Mock<ILogger> LoggerMock;
        private ProjectConfig Config;
        private Mock<EventBuilder> EventBuilderMock;
        private Mock<IErrorHandler> ErrorHandlerMock;
        private Optimizely Optimizely;
        private IEventDispatcher EventDispatcher;
        private const string TestUserId = "testUserId";
        private OptimizelyHelper Helper;
        private Mock<Optimizely> OptimizelyMock;
        private Mock<DecisionService> DecisionServiceMock;
        private NotificationCenter NotificationCenter;
        private Mock<TestNotificationCallbacks> NotificationCallbackMock;
        private Variation VariationWithKeyControl;
        private Variation VariationWithKeyVariation;
        private Variation GroupVariation;

        #region Test Life Cycle
        [SetUp]
        public void Initialize()
        {
            LoggerMock = new Mock<ILogger>();
            LoggerMock.Setup(i => i.Log(It.IsAny<LogLevel>(), It.IsAny<string>()));

            ErrorHandlerMock = new Mock<IErrorHandler>();
            ErrorHandlerMock.Setup(e => e.HandleError(It.IsAny<Exception>()));

            EventBuilderMock = new Mock<EventBuilder>(new Bucketer(LoggerMock.Object));

            EventBuilderMock.Setup(b => b.CreateImpressionEvent(It.IsAny<ProjectConfig>(), It.IsAny<Experiment>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UserAttributes>()));

            EventBuilderMock.Setup(b => b.CreateConversionEvent(It.IsAny<ProjectConfig>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, Variation>>(), It.IsAny<string>(), It.IsAny<UserAttributes>(), It.IsAny<EventTags>()));

            Config = ProjectConfig.Create(
                content: TestData.Datafile,
                logger: LoggerMock.Object,
                errorHandler: new NoOpErrorHandler());

            EventDispatcher = new ValidEventDispatcher();
            Optimizely = new Optimizely(TestData.Datafile, EventDispatcher, LoggerMock.Object, ErrorHandlerMock.Object);

            Helper = new OptimizelyHelper
            {
                Datafile = TestData.Datafile,
                EventDispatcher = EventDispatcher,
                Logger = LoggerMock.Object,
                ErrorHandler = ErrorHandlerMock.Object,
                SkipJsonValidation = false,
            };

            OptimizelyMock = new Mock<Optimizely>(TestData.Datafile, EventDispatcher, LoggerMock.Object, ErrorHandlerMock.Object, null, false)
            {
                CallBase = true
            };

            DecisionServiceMock = new Mock<DecisionService>(new Bucketer(LoggerMock.Object), ErrorHandlerMock.Object,
                Config, null, LoggerMock.Object);

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
            EventBuilderMock = null;
        }
        #endregion

        #region OptimizelyHelper
        private class OptimizelyHelper
        {
            static Type[] ParameterTypes = new[]
            {
                typeof(string),
                typeof(IEventDispatcher),
                typeof(ILogger),
                typeof(IErrorHandler),
                typeof(bool)
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
                        SkipJsonValidation
                    });
            }
        }
        #endregion

        #region Test Validate
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
            Assert.IsTrue(optimizely.IsValid);
        }

        [Test]
        public void TestValidatePreconditionsExperimentNotRunning()
        {
            var optly = Helper.CreatePrivateOptimizely();
            bool result = (bool)optly.Invoke("ValidatePreconditions",
                Config.GetExperimentFromKey("paused_experiment"),
                TestUserId, new UserAttributes { });
            Assert.IsFalse(result);
        }

        [Test]
        public void TestValidatePreconditionsExperimentRunning()
        {
            var optly = Helper.CreatePrivateOptimizely();
            bool result = (bool)optly.Invoke("ValidatePreconditions",
                Config.GetExperimentFromKey("test_experiment"),
                TestUserId,
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
                "user1", new UserAttributes { });
            Assert.IsTrue(result);
        }

        [Test]
        public void TestValidatePreconditionsUserInForcedVariationInExperiment()
        {
            var optly = Helper.CreatePrivateOptimizely();
            bool result = (bool)optly.Invoke("ValidatePreconditions",
                Config.GetExperimentFromKey("test_experiment"),
                "user1", new UserAttributes { });
            Assert.IsTrue(result);
        }

        [Test]
        public void TestValidatePreconditionsUserNotInForcedVariationNotInExperiment()
        {
            var optly = Helper.CreatePrivateOptimizely();
            bool result = (bool)optly.Invoke("ValidatePreconditions",
                Config.GetExperimentFromKey("test_experiment"),
                TestUserId, new UserAttributes { });
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

            LoggerMock.Verify(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<string>()), Times.Exactly(4));
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
            optly.SetFieldOrProperty("EventBuilder", EventBuilderMock.Object);

            var result = optly.Invoke("Activate", "test_experiment", "not_in_variation_user", OptimizelyHelper.UserAttributes);

            EventBuilderMock.Verify(b => b.CreateImpressionEvent(It.IsAny<ProjectConfig>(), It.IsAny<Experiment>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UserAttributes>()), Times.Never);

            LoggerMock.Verify(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<string>()), Times.Exactly(4));
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

            EventBuilderMock.Setup(b => b.CreateImpressionEvent(It.IsAny<ProjectConfig>(), It.IsAny<Experiment>(),
               It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UserAttributes>()))
               .Returns(new LogEvent("logx.optimizely.com/decision", parameters, "POST", new Dictionary<string, string> { }));

            var optly = Helper.CreatePrivateOptimizely();
            optly.SetFieldOrProperty("EventBuilder", EventBuilderMock.Object);

            var variation = (Variation)optly.Invoke("Activate", "group_experiment_1", "user_1", null);
            
            EventBuilderMock.Verify(b => b.CreateImpressionEvent(It.IsAny<ProjectConfig>(), Config.GetExperimentFromKey("group_experiment_1"),
                    "7722360022", "user_1", null), Times.Once);

            LoggerMock.Verify(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<string>()), Times.Exactly(8));
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "Assigned bucket [1922] to user [user_1] with bucketing ID [user_1]."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "User [user_1] is in experiment [group_experiment_1] of group [7722400015]."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "Assigned bucket [9525] to user [user_1] with bucketing ID [user_1]."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "User [user_1] is in variation [group_exp_1_var_2] of experiment [group_experiment_1]."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "Activating user user_1 in experiment group_experiment_1."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, @"Dispatching impression event to URL logx.optimizely.com/decision with params {""param1"":""val1"",""param2"":""val2""}."), Times.Once);

            Assert.IsTrue(TestData.CompareObjects(GroupVariation, variation));
        }
        #endregion

        #region Test Activate
        [Test]
        public void TestActivateAudienceNoAttributes()
        {
            var optly = Helper.CreatePrivateOptimizely();
            optly.SetFieldOrProperty("EventBuilder", EventBuilderMock.Object);

            var variationkey = optly.Invoke("Activate", "test_experiment", "test_user", null);

            EventBuilderMock.Verify(b => b.CreateImpressionEvent(It.IsAny<ProjectConfig>(), It.IsAny<Experiment>(),
              It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UserAttributes>()), Times.Never);

            LoggerMock.Verify(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<string>()), Times.Exactly(3));
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "User \"test_user\" does not meet conditions to be in experiment \"test_experiment\"."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "Not activating user test_user."), Times.Once);

            Assert.IsNull(variationkey);
        }

        [Test]
        public void TestActivateWithAttributes()
        {
            EventBuilderMock.Setup(b => b.CreateImpressionEvent(It.IsAny<ProjectConfig>(), It.IsAny<Experiment>(),
              It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UserAttributes>()))
              .Returns(new LogEvent("logx.optimizely.com/decision", OptimizelyHelper.SingleParameter, "POST", new Dictionary<string, string> { }));

            var optly = Helper.CreatePrivateOptimizely();
            optly.SetFieldOrProperty("EventBuilder", EventBuilderMock.Object);

            var variation = (Variation)optly.Invoke("Activate", "test_experiment", "test_user", OptimizelyHelper.UserAttributes);

            EventBuilderMock.Verify(b => b.CreateImpressionEvent(It.IsAny<ProjectConfig>(), Config.GetExperimentFromKey("test_experiment"),
                    "7722370027", "test_user", OptimizelyHelper.UserAttributes), Times.Once);

            LoggerMock.Verify(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<string>()), Times.Exactly(6));
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "Assigned bucket [3037] to user [test_user] with bucketing ID [test_user]."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "User [test_user] is in variation [control] of experiment [test_experiment]."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "Activating user test_user in experiment test_experiment."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, @"Dispatching impression event to URL logx.optimizely.com/decision with params {""param1"":""val1""}."), Times.Once);

            Assert.IsTrue(TestData.CompareObjects(VariationWithKeyControl, variation));
        }

        [Test]
        public void TestActivateWithNullAttributes()
        {
            EventBuilderMock.Setup(b => b.CreateImpressionEvent(It.IsAny<ProjectConfig>(), It.IsAny<Experiment>(),
              It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UserAttributes>()))
              .Returns(new LogEvent("logx.optimizely.com/decision", OptimizelyHelper.SingleParameter, "POST", new Dictionary<string, string> { }));

            var optly = Helper.CreatePrivateOptimizely();
            optly.SetFieldOrProperty("EventBuilder", EventBuilderMock.Object);

            var variation = (Variation)optly.Invoke("Activate", "test_experiment", "test_user", OptimizelyHelper.NullUserAttributes);

            EventBuilderMock.Verify(b => b.CreateImpressionEvent(It.IsAny<ProjectConfig>(), Config.GetExperimentFromKey("test_experiment"),
                    "7722370027", "test_user", OptimizelyHelper.UserAttributes), Times.Once);

            LoggerMock.Verify(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<string>()), Times.Exactly(9));

            //"User "test_user" is not in the forced variation map."
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "User \"test_user\" is not in the forced variation map."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "Assigned bucket [3037] to user [test_user] with bucketing ID [test_user]."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "User [test_user] is in variation [control] of experiment [test_experiment]."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.ERROR, "[UserAttributes] Null value for key null_value removed and will not be sent to results."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.ERROR, "[UserAttributes] Null value for key wont_be_sent removed and will not be sent to results."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.ERROR, "[UserAttributes] Null value for key bad_food removed and will not be sent to results."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "Activating user test_user in experiment test_experiment."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, @"Dispatching impression event to URL logx.optimizely.com/decision with params {""param1"":""val1""}."), Times.Once);

            Assert.IsTrue(TestData.CompareObjects(VariationWithKeyControl, variation));
        }

        [Test]
        public void TestActivateExperimentNotRunning()
        {
            var optly = Helper.CreatePrivateOptimizely();
            optly.SetFieldOrProperty("EventBuilder", EventBuilderMock.Object);

            var variationkey = optly.Invoke("Activate", "paused_experiment", "test_user", null);

            EventBuilderMock.Verify(b => b.CreateImpressionEvent(It.IsAny<ProjectConfig>(), It.IsAny<Experiment>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UserAttributes>()), Times.Never);

            LoggerMock.Verify(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<string>()), Times.Exactly(2));
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "Experiment \"paused_experiment\" is not running."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "Not activating user test_user."), Times.Once);

            Assert.IsNull(variationkey);
        }
        #endregion

        #region Test GetVariation
        [Test]
        public void TestGetVariationInvalidOptimizelyObject()
        {
            var optly = new Optimizely("Random datafile", null, LoggerMock.Object);
            var variationkey = optly.Activate("some_experiment", "some_user");
            LoggerMock.Verify(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<string>()), Times.Exactly(2));
            LoggerMock.Verify(l => l.Log(LogLevel.ERROR, "Datafile has invalid format. Failing 'activate'."), Times.Once);
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

            LoggerMock.Verify(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<string>()), Times.Exactly(4));
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
            LoggerMock.Verify(l => l.Log(LogLevel.ERROR, "Datafile has invalid format. Failing 'track'."), Times.Once);
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

            //LoggerMock.Verify(l => l.Log(LogLevel.ERROR, "Provided attributes are in an invalid format."), Times.Once);
            ErrorHandlerMock.Verify(e => e.HandleError(It.IsAny<InvalidAttributeException>()), Times.Once);
        }

        [Test]
        public void TestTrackNoAttributesNoEventValue()
        {
            EventBuilderMock.Setup(b => b.CreateConversionEvent(It.IsAny<ProjectConfig>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string,Variation>>(), It.IsAny<string>(), It.IsAny<UserAttributes>(), It.IsAny<EventTags>()))
              .Returns(new LogEvent("logx.optimizely.com/track", OptimizelyHelper.SingleParameter,
                                        "POST", new Dictionary<string, string> { }));

            var optly = Helper.CreatePrivateOptimizely();
            optly.SetFieldOrProperty("EventBuilder", EventBuilderMock.Object);
            optly.Invoke("Track", "purchase", "test_user", null, null);

            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "User \"test_user\" does not meet conditions to be in experiment \"test_experiment\"."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "Not tracking user \"test_user\" for experiment \"test_experiment\""), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "Experiment \"paused_experiment\" is not running."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "Not tracking user \"test_user\" for experiment \"paused_experiment\""), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "Tracking event purchase for user test_user."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "Dispatching conversion event to URL logx.optimizely.com/track with params {\"param1\":\"val1\"}."), Times.Once);
        }

        [Test]
        public void TestTrackWithAttributesNoEventValue()
        {
            EventBuilderMock.Setup(b => b.CreateConversionEvent(It.IsAny<ProjectConfig>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, Variation>>(), It.IsAny<string>(), It.IsAny<UserAttributes>(), It.IsAny<EventTags>()))
             .Returns(new LogEvent("logx.optimizely.com/track", OptimizelyHelper.SingleParameter,
                                       "POST", new Dictionary<string, string> { }));

            var optly = Helper.CreatePrivateOptimizely();
            optly.SetFieldOrProperty("EventBuilder", EventBuilderMock.Object);
            optly.Invoke("Track", "purchase", "test_user", OptimizelyHelper.UserAttributes, null);

            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "Experiment \"paused_experiment\" is not running."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "Not tracking user \"test_user\" for experiment \"paused_experiment\""), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "Tracking event purchase for user test_user."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "Dispatching conversion event to URL logx.optimizely.com/track with params {\"param1\":\"val1\"}."), Times.Once);
        }

        [Test]
        public void TestTrackNoAttributesWithEventValue()
        {
            EventBuilderMock.Setup(b => b.CreateConversionEvent(It.IsAny<ProjectConfig>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, Variation>>(), It.IsAny<string>(), It.IsAny<UserAttributes>(), It.IsAny<EventTags>()))
             .Returns(new LogEvent("logx.optimizely.com/track", OptimizelyHelper.SingleParameter,
                                       "POST", new Dictionary<string, string> { }));

            var optly = Helper.CreatePrivateOptimizely();
            optly.SetFieldOrProperty("EventBuilder", EventBuilderMock.Object);
            optly.Invoke("Track", "purchase", "test_user", null, new EventTags
            {
                { "revenue", 42 }
            });

            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "User \"test_user\" does not meet conditions to be in experiment \"test_experiment\"."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "Not tracking user \"test_user\" for experiment \"test_experiment\""), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "Experiment \"paused_experiment\" is not running."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "Not tracking user \"test_user\" for experiment \"paused_experiment\""), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "Tracking event purchase for user test_user."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "Dispatching conversion event to URL logx.optimizely.com/track with params {\"param1\":\"val1\"}."), Times.Once);
        }

        [Test]
        public void TestTrackWithAttributesWithEventValue()
        {
            var attributes = new UserAttributes
            {
                { "device_type", "iPhone" },
                { "company", "Optimizely" },
            };

            EventBuilderMock.Setup(b => b.CreateConversionEvent(It.IsAny<ProjectConfig>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, Variation>>(), It.IsAny<string>(), It.IsAny<UserAttributes>(), It.IsAny<EventTags>()))
             .Returns(new LogEvent("logx.optimizely.com/track", OptimizelyHelper.SingleParameter,
                                       "POST", new Dictionary<string, string> { }));

            var optly = Helper.CreatePrivateOptimizely();
            optly.SetFieldOrProperty("EventBuilder", EventBuilderMock.Object);
            optly.Invoke("Track", "purchase", "test_user", attributes, new EventTags
            {
                { "revenue", 42 }
            });

            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "User \"test_user\" does not meet conditions to be in experiment \"test_experiment\"."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "Not tracking user \"test_user\" for experiment \"test_experiment\""), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "Experiment \"paused_experiment\" is not running."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "Not tracking user \"test_user\" for experiment \"paused_experiment\""), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "Tracking event purchase for user test_user."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "Dispatching conversion event to URL logx.optimizely.com/track with params {\"param1\":\"val1\"}."), Times.Once);
        }

        [Test]
        public void TestTrackWithNullAttributesWithNullEventValue()
        {
            EventBuilderMock.Setup(b => b.CreateConversionEvent(It.IsAny<ProjectConfig>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, Variation>>(), It.IsAny<string>(), It.IsAny<UserAttributes>(), It.IsAny<EventTags>()))
             .Returns(new LogEvent("logx.optimizely.com/track", OptimizelyHelper.SingleParameter,
                                       "POST", new Dictionary<string, string> { }));

            var optly = Helper.CreatePrivateOptimizely();
            optly.SetFieldOrProperty("EventBuilder", EventBuilderMock.Object);
            optly.Invoke("Track", "purchase", "test_user", OptimizelyHelper.NullUserAttributes, new EventTags
            {
                { "revenue", 42 },
                { "wont_send_null", null}
            });

            LoggerMock.Verify(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<string>()), Times.Exactly(21));

            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "User \"test_user\" is not in the forced variation map."), Times.Exactly(3));
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "Assigned bucket [3037] to user [test_user] with bucketing ID [test_user]."));
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "User [test_user] is in variation [control] of experiment [test_experiment]."));
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "This decision will not be saved since the UserProfileService is null."));
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "Assigned bucket [4517] to user [test_user] with bucketing ID [test_user]."));
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "User [test_user] is not in experiment [group_experiment_1] of group [7722400015]."));
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "Assigned bucket [4517] to user [test_user] with bucketing ID [test_user]."));
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "User [test_user] is in experiment [group_experiment_2] of group [7722400015]."));
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "Assigned bucket [9871] to user [test_user] with bucketing ID [test_user]."));
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "User [test_user] is in variation [group_exp_2_var_2] of experiment [group_experiment_2]."));
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "This decision will not be saved since the UserProfileService is null."));
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "Experiment \"paused_experiment\" is not running."));
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "Not tracking user \"test_user\" for experiment \"paused_experiment\""));
            LoggerMock.Verify(l => l.Log(LogLevel.ERROR, "[UserAttributes] Null value for key null_value removed and will not be sent to results."));
            LoggerMock.Verify(l => l.Log(LogLevel.ERROR, "[UserAttributes] Null value for key wont_be_sent removed and will not be sent to results."));
            LoggerMock.Verify(l => l.Log(LogLevel.ERROR, "[UserAttributes] Null value for key bad_food removed and will not be sent to results."));
            LoggerMock.Verify(l => l.Log(LogLevel.ERROR, "[EventTags] Null value for key wont_send_null removed and will not be sent to results."));
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "Tracking event purchase for user test_user."));
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "Dispatching conversion event to URL logx.optimizely.com/track with params {\"param1\":\"val1\"}."));
        }
        #endregion

        #region Test Invalid Dispatch
        [Test]
        public void TestInvalidDispatchImpressionEvent()
        {
            EventBuilderMock.Setup(b => b.CreateImpressionEvent(It.IsAny<ProjectConfig>(), It.IsAny<Experiment>(),
              It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Entity.UserAttributes>()))
              .Returns(new LogEvent("logx.optimizely.com/decision", OptimizelyHelper.SingleParameter, "POST", new Dictionary<string, string> { }));

            var optly = Helper.CreatePrivateOptimizely();
            optly.SetFieldOrProperty("EventBuilder", EventBuilderMock.Object);
            optly.SetFieldOrProperty("EventDispatcher", new InvalidEventDispatcher());

            var variation = (Variation)optly.Invoke("Activate", "test_experiment", "test_user", OptimizelyHelper.UserAttributes);

            LoggerMock.Verify(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<string>()), Times.Exactly(7));
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "Assigned bucket [3037] to user [test_user] with bucketing ID [test_user]."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "User [test_user] is in variation [control] of experiment [test_experiment]."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "Activating user test_user in experiment test_experiment."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, @"Dispatching impression event to URL logx.optimizely.com/decision with params {""param1"":""val1""}."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.ERROR, "Unable to dispatch impression event. Error Invalid dispatch event"), Times.Once);

            Assert.IsTrue(TestData.CompareObjects(VariationWithKeyControl, variation));
        }

        [Test]
        public void TestInvalidDispatchConversionEvent()
        {
            EventBuilderMock.Setup(b => b.CreateConversionEvent(It.IsAny<ProjectConfig>(), It.IsAny<string>(),
                 It.IsAny<Dictionary<string, Variation>>(), It.IsAny<string>(), It.IsAny<UserAttributes>(), It.IsAny<EventTags>()))
               .Returns(new LogEvent("logx.optimizely.com/track", OptimizelyHelper.SingleParameter,
                                         "POST", new Dictionary<string, string> { }));

            var optly = Helper.CreatePrivateOptimizely();
            optly.SetFieldOrProperty("EventBuilder", EventBuilderMock.Object);
            optly.SetFieldOrProperty("EventDispatcher", new InvalidEventDispatcher());

            optly.Invoke("Track", "purchase", "test_user", null, null);

            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "User \"test_user\" does not meet conditions to be in experiment \"test_experiment\"."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "Not tracking user \"test_user\" for experiment \"test_experiment\""), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "Experiment \"paused_experiment\" is not running."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "Not tracking user \"test_user\" for experiment \"paused_experiment\""), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "Tracking event purchase for user test_user."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "Dispatching conversion event to URL logx.optimizely.com/track with params {\"param1\":\"val1\"}."), Times.Once);
        }
        #endregion

        #region Test Misc
        /* Start 1 */
        public void TestTrackNoAttributesWithInvalidEventValue()
        {
            EventBuilderMock.Setup(b => b.CreateConversionEvent(It.IsAny<ProjectConfig>(), It.IsAny<string>(),
                 It.IsAny<Dictionary<string, Variation>>(), It.IsAny<string>(), It.IsAny<UserAttributes>(), It.IsAny<EventTags>()))
               .Returns(new LogEvent("logx.optimizely.com/track", OptimizelyHelper.SingleParameter,
                                         "POST", new Dictionary<string, string> { }));

            var optly = Helper.CreatePrivateOptimizely();
            optly.SetFieldOrProperty("EventBuilder", EventBuilderMock.Object);
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

            EventBuilderMock.Setup(b => b.CreateConversionEvent(It.IsAny<ProjectConfig>(), It.IsAny<string>(),
                 It.IsAny<Dictionary<string, Variation>>(), It.IsAny<string>(), It.IsAny<UserAttributes>(), It.IsAny<EventTags>()))
               .Returns(new LogEvent("logx.optimizely.com/track", OptimizelyHelper.SingleParameter,
                                         "POST", new Dictionary<string, string> { }));

            var optly = Helper.CreatePrivateOptimizely();
            optly.SetFieldOrProperty("EventBuilder", EventBuilderMock.Object);
            optly.SetFieldOrProperty("EventDispatcher", new ValidEventDispatcher());
            optly.Invoke("Track", "purchase", "test_user", null, new Dictionary<string, object>
            {
                {"revenue", 42 }
            });

        }
        
        [Test]
        public void TestForcedVariationPreceedsWhitelistedVariation()
        {
            var optimizely = new Optimizely(TestData.ValidDataFileV3, EventDispatcher, LoggerMock.Object, ErrorHandlerMock.Object);
            var projectConfig = ProjectConfig.Create(TestData.ValidDataFileV3, LoggerMock.Object, ErrorHandlerMock.Object);
            Variation expectedVariation1 = projectConfig.GetVariationFromKey("etag1", "vtag1");
            Variation expectedVariation2 = projectConfig.GetVariationFromKey("etag1", "vtag2");

            //Check whitelisted experiment
            var variation = optimizely.GetVariation("etag1", "testUser1");
            Assert.IsTrue(TestData.CompareObjects(expectedVariation1, variation));

            //Set forced variation
            Assert.IsTrue(optimizely.SetForcedVariation("etag1", "testUser1", "vtag2"));
            variation = optimizely.GetVariation("etag1", "testUser1");

            // verify forced variation preceeds whitelisted variation
            Assert.IsTrue(TestData.CompareObjects(expectedVariation2, variation));

            // remove forced variation and verify whitelisted should be returned.
            Assert.IsTrue(optimizely.SetForcedVariation("etag1", "testUser1", null));
            variation = optimizely.GetVariation("etag1", "testUser1");

            Assert.IsTrue(TestData.CompareObjects(expectedVariation1, variation));

            LoggerMock.Verify(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<string>()), Times.Exactly(7));
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "User \"testUser1\" is not in the forced variation map."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "User \"testUser1\" is forced in variation \"vtag1\"."), Times.Exactly(2));
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "Set variation \"277\" for experiment \"223\" and user \"testUser1\" in the forced variation map."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "Variation \"vtag2\" is mapped to experiment \"etag1\" and user \"testUser1\" in the forced variation map"), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "Variation mapped to experiment \"etag1\" has been removed for user \"testUser1\"."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "No experiment \"etag1\" mapped to user \"testUser1\" in the forced variation map."), Times.Once);
        }

        [Test]
        public void TestForcedVariationPreceedsUserProfile()
        {
            var userProfileServiceMock = new Mock<UserProfileService>();
            var experimentKey = "etag1";
            var userId = "testUser3";
            var variationKey = "vtag2";
            var fbVariationKey = "vtag1";
            

            UserProfile userProfile = new UserProfile(userId, new Dictionary<string, Decision>
            {
                { experimentKey, new Decision(variationKey)}
            });

            userProfileServiceMock.Setup(_ => _.Lookup(userId)).Returns(userProfile.ToMap());

            var optimizely = new Optimizely(TestData.NoAudienceProjectConfigV3, EventDispatcher, LoggerMock.Object, ErrorHandlerMock.Object, userProfileServiceMock.Object);
            var projectConfig = ProjectConfig.Create(TestData.NoAudienceProjectConfigV3, LoggerMock.Object, ErrorHandlerMock.Object);
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

            LoggerMock.Verify(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<string>()), Times.Exactly(13));
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

            EventBuilderMock.Setup(b => b.CreateImpressionEvent(Config, It.IsAny<Experiment>(), It.IsAny <string>()/*"group_exp_1_var_2"*/, "user_1", null))
               .Returns(new LogEvent("logx.optimizely.com/decision", parameters, "POST", new Dictionary<string, string> { }));

            var optly = Helper.CreatePrivateOptimizely();
            optly.SetFieldOrProperty("EventBuilder", EventBuilderMock.Object);
            optly.SetFieldOrProperty("Config", Config);

            // Set forced variation
            Assert.True((bool)optly.Invoke("SetForcedVariation", experimentKey, userId, variationKey), "Set variation for paused experiment should have failed.");
            
            // Activate
            var variation = (Variation)optly.Invoke("Activate", "group_experiment_1", "user_1", null);
            
            EventBuilderMock.Verify(b => b.CreateImpressionEvent(It.IsAny<ProjectConfig>(), Config.GetExperimentFromKey("group_experiment_1"),
                    "7722360022", "user_1", null), Times.Once);

            LoggerMock.Verify(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<string>()), Times.Exactly(9));
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, string.Format(@"Set variation ""{0}"" for experiment ""{1}"" and user ""{2}"" in the forced variation map.", variationId, experimentId, userId)), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "User \"user_1\" is not in the forced variation map."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "Assigned bucket [1922] to user [user_1] with bucketing ID [user_1]."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "User [user_1] is in experiment [group_experiment_1] of group [7722400015]."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "Assigned bucket [9525] to user [user_1] with bucketing ID [user_1]."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "User [user_1] is in variation [group_exp_1_var_2] of experiment [group_experiment_1]."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "This decision will not be saved since the UserProfileService is null."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "Activating user user_1 in experiment group_experiment_1."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, @"Dispatching impression event to URL logx.optimizely.com/decision with params {""param1"":""val1"",""param2"":""val2""}."), Times.Once);

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

            EventBuilderMock.Setup(b => b.CreateConversionEvent(It.IsAny<ProjectConfig>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, Variation>>(), It.IsAny<string>(), It.IsAny<UserAttributes>(), It.IsAny<EventTags>()))
                .Returns(new LogEvent("logx.optimizely.com/track", parameters, "POST", new Dictionary<string, string> { }));

            var optly = Helper.CreatePrivateOptimizely();
            optly.SetFieldOrProperty("EventBuilder", EventBuilderMock.Object);

            // Set forced variation
            Assert.True((bool)optly.Invoke("SetForcedVariation", experimentKey, userId, variationKey), "Set variation for paused experiment should have failed.");

            // Track
            optly.Invoke("Track", "purchase", "test_user", null, null);

            LoggerMock.Verify(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<string>()), Times.Exactly(15));
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, string.Format(@"Set variation ""{0}"" for experiment ""{1}"" and user ""{2}"" in the forced variation map.", variationId, experimentId, userId)), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "Variation \"control\" is mapped to experiment \"test_experiment\" and user \"test_user\" in the forced variation map"), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "No experiment \"group_experiment_1\" mapped to user \"test_user\" in the forced variation map."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "Assigned bucket [4517] to user [test_user] with bucketing ID [test_user]."), Times.Exactly(2));
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "User [test_user] is not in experiment [group_experiment_1] of group [7722400015]."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "No experiment \"group_experiment_2\" mapped to user \"test_user\" in the forced variation map."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "Assigned bucket [4517] to user [test_user] with bucketing ID [test_user]."), Times.Exactly(2));
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "User [test_user] is in experiment [group_experiment_2] of group [7722400015]."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "Assigned bucket [9871] to user [test_user] with bucketing ID [test_user]."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "User [test_user] is in variation [group_exp_2_var_2] of experiment [group_experiment_2]."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "This decision will not be saved since the UserProfileService is null."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "Experiment \"paused_experiment\" is not running."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "Not tracking user \"test_user\" for experiment \"paused_experiment\""), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "Tracking event purchase for user test_user."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "Dispatching conversion event to URL logx.optimizely.com/track with params {\"param1\":\"val1\"}."), Times.Once);
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
               { DecisionService.RESERVED_ATTRIBUTE_KEY_BUCKETING_ID, testBucketingIdVariation }
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

        #region Test GetFeatureVariable<Type> Typecasting

        [Test]
        public void TestGetFeatureVariableBooleanReturnTypecastedValue()
        {
            var featureKey = "featureKey";
            var variableKeyTrue = "varTrue";
            var variableKeyFalse = "varFalse";
            var variableKeyNonBoolean = "varNonBoolean";
            var variableKeyNull = "varNull";
            var featureVariableType = FeatureVariable.VariableType.BOOLEAN;

            OptimizelyMock.Setup(om => om.GetFeatureVariableValueForType(It.IsAny<string>(), variableKeyTrue, It.IsAny<string>(),
                It.IsAny<UserAttributes>(), featureVariableType)).Returns("True");
            Assert.AreEqual(true, OptimizelyMock.Object.GetFeatureVariableBoolean(featureKey, variableKeyTrue, TestUserId, null));

            OptimizelyMock.Setup(om => om.GetFeatureVariableValueForType(It.IsAny<string>(), variableKeyFalse, It.IsAny<string>(),
                It.IsAny<UserAttributes>(), featureVariableType)).Returns("false");
            Assert.AreEqual(false, OptimizelyMock.Object.GetFeatureVariableBoolean(featureKey, variableKeyFalse, TestUserId, null));

            OptimizelyMock.Setup(om => om.GetFeatureVariableValueForType(It.IsAny<string>(), variableKeyNonBoolean, It.IsAny<string>(),
                It.IsAny<UserAttributes>(), featureVariableType)).Returns("non_boolean_value");

            Assert.Null(OptimizelyMock.Object.GetFeatureVariableBoolean(featureKey, variableKeyNonBoolean, TestUserId, null));
            LoggerMock.Verify(l => l.Log(LogLevel.ERROR, $@"Unable to cast variable value ""non_boolean_value"" to type ""{featureVariableType}""."));

            OptimizelyMock.Setup(om => om.GetFeatureVariableValueForType(It.IsAny<string>(), variableKeyNull, It.IsAny<string>(),
                It.IsAny<UserAttributes>(), featureVariableType)).Returns<string>(null);
            Assert.Null(OptimizelyMock.Object.GetFeatureVariableBoolean(featureKey, variableKeyNull, TestUserId, null));
        }

        [Test]
        public void TestGetFeatureVariableDoubleReturnTypecastedValue()
        {
            var featureKey = "featureKey";
            var variableKeyDouble = "varDouble";
            var variableKeyInt = "varInt";
            var variableKeyNonDouble = "varNonDouble";
            var variableKeyNull = "varNull";
            var featureVariableType = FeatureVariable.VariableType.DOUBLE;

            OptimizelyMock.Setup(om => om.GetFeatureVariableValueForType(It.IsAny<string>(), variableKeyDouble, It.IsAny<string>(),
                It.IsAny<UserAttributes>(), featureVariableType)).Returns("100.54");
            Assert.AreEqual(100.54, OptimizelyMock.Object.GetFeatureVariableDouble(featureKey, variableKeyDouble, TestUserId, null));

            OptimizelyMock.Setup(om => om.GetFeatureVariableValueForType(It.IsAny<string>(), variableKeyInt, It.IsAny<string>(),
                It.IsAny<UserAttributes>(), featureVariableType)).Returns("100");
            Assert.AreEqual(100, OptimizelyMock.Object.GetFeatureVariableDouble(featureKey, variableKeyInt, TestUserId, null));

            OptimizelyMock.Setup(om => om.GetFeatureVariableValueForType(It.IsAny<string>(), variableKeyNonDouble, It.IsAny<string>(),
                It.IsAny<UserAttributes>(), featureVariableType)).Returns("non_double_value");

            Assert.Null(OptimizelyMock.Object.GetFeatureVariableDouble(featureKey, variableKeyNonDouble, TestUserId, null));
            LoggerMock.Verify(l => l.Log(LogLevel.ERROR, $@"Unable to cast variable value ""non_double_value"" to type ""{featureVariableType}""."));

            OptimizelyMock.Setup(om => om.GetFeatureVariableValueForType(It.IsAny<string>(), variableKeyNull, It.IsAny<string>(),
                It.IsAny<UserAttributes>(), featureVariableType)).Returns<string>(null);
            Assert.Null(OptimizelyMock.Object.GetFeatureVariableDouble(featureKey, variableKeyNull, TestUserId, null));
        }

        [Test]
        public void TestGetFeatureVariableIntegerReturnTypecastedValue()
        {
            var featureKey = "featureKey";
            var variableKeyInt = "varInt";
            var variableKeyDouble = "varDouble";
            var variableNonInt = "varNonInt";
            var variableKeyNull = "varNull";
            var featureVariableType = FeatureVariable.VariableType.INTEGER;

            OptimizelyMock.Setup(om => om.GetFeatureVariableValueForType(It.IsAny<string>(), variableKeyInt, It.IsAny<string>(),
                It.IsAny<UserAttributes>(), featureVariableType)).Returns("100");
            Assert.AreEqual(100, OptimizelyMock.Object.GetFeatureVariableInteger(featureKey, variableKeyInt, TestUserId, null));

            OptimizelyMock.Setup(om => om.GetFeatureVariableValueForType(It.IsAny<string>(), variableKeyDouble, It.IsAny<string>(),
                It.IsAny<UserAttributes>(), featureVariableType)).Returns("100.45");

            Assert.Null(OptimizelyMock.Object.GetFeatureVariableInteger(featureKey, variableKeyDouble, TestUserId, null));
            LoggerMock.Verify(l => l.Log(LogLevel.ERROR, $@"Unable to cast variable value ""100.45"" to type ""{featureVariableType}""."));

            OptimizelyMock.Setup(om => om.GetFeatureVariableValueForType(It.IsAny<string>(), variableNonInt, It.IsAny<string>(),
                It.IsAny<UserAttributes>(), featureVariableType)).Returns("non_integer_value");

            Assert.Null(OptimizelyMock.Object.GetFeatureVariableInteger(featureKey, variableNonInt, TestUserId, null));
            LoggerMock.Verify(l => l.Log(LogLevel.ERROR, $@"Unable to cast variable value ""non_integer_value"" to type ""{featureVariableType}""."));

            OptimizelyMock.Setup(om => om.GetFeatureVariableValueForType(It.IsAny<string>(), variableKeyNull, It.IsAny<string>(),
                It.IsAny<UserAttributes>(), featureVariableType)).Returns<string>(null);
            Assert.Null(OptimizelyMock.Object.GetFeatureVariableInteger(featureKey, variableKeyNull, TestUserId, null));
        }

        [Test]
        public void TestGetFeatureVariableStringReturnTypecastedValue()
        {
            var featureKey = "featureKey";
            var variableKeyString = "varString1";
            var variableKeyIntString = "varString2";
            var variableKeyNull = "varNull";
            var featureVariableType = FeatureVariable.VariableType.STRING;

            OptimizelyMock.Setup(om => om.GetFeatureVariableValueForType(It.IsAny<string>(), variableKeyString, It.IsAny<string>(),
                It.IsAny<UserAttributes>(), featureVariableType)).Returns("Test String");
            Assert.AreEqual("Test String", OptimizelyMock.Object.GetFeatureVariableString(featureKey, variableKeyString, TestUserId, null));

            OptimizelyMock.Setup(om => om.GetFeatureVariableValueForType(It.IsAny<string>(), variableKeyIntString, It.IsAny<string>(),
                It.IsAny<UserAttributes>(), featureVariableType)).Returns("123");
            Assert.AreEqual("123", OptimizelyMock.Object.GetFeatureVariableString(featureKey, variableKeyIntString, TestUserId, null));

            OptimizelyMock.Setup(om => om.GetFeatureVariableValueForType(It.IsAny<string>(), variableKeyNull, It.IsAny<string>(),
                It.IsAny<UserAttributes>(), featureVariableType)).Returns<string>(null);
            Assert.Null(OptimizelyMock.Object.GetFeatureVariableString(featureKey, variableKeyNull, TestUserId, null));
        }

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
            Assert.IsNull(Optimizely.GetFeatureVariableValueForType(null, variableKey, TestUserId, null, variableType));
            Assert.IsNull(Optimizely.GetFeatureVariableValueForType("", variableKey, TestUserId, null, variableType));

            // Passing null and empty variable key.
            Assert.IsNull(Optimizely.GetFeatureVariableValueForType(featureKey, null, TestUserId, null, variableType));
            Assert.IsNull(Optimizely.GetFeatureVariableValueForType(featureKey, "", TestUserId, null, variableType));

            // Passing null and empty user Id.
            Assert.IsNull(Optimizely.GetFeatureVariableValueForType(featureKey, variableKey, null, null, variableType));
            Assert.IsNull(Optimizely.GetFeatureVariableValueForType(featureKey, variableKey, "", null, variableType));

            LoggerMock.Verify(l => l.Log(LogLevel.ERROR, "Feature flag key must not be empty."), Times.Exactly(2));
            LoggerMock.Verify(l => l.Log(LogLevel.ERROR, "Variable key must not be empty."), Times.Exactly(2));
            LoggerMock.Verify(l => l.Log(LogLevel.ERROR, "User ID must not be empty."), Times.Exactly(2));
        }

        // Should return null and log error message when feature key or variable key does not get found.
        [Test]
        public void TestGetFeatureVariableValueForTypeGivenFeatureKeyOrVariableKeyNotFound()
        {
            var featureKey = "this_feature_should_never_be_found_in_the_datafile_unless_the_datafile_creator_got_insane";
            var variableKey = "this_variable_should_never_be_found_in_the_datafile_unless_the_datafile_creator_got_insane";
            var variableType = FeatureVariable.VariableType.BOOLEAN;

            Assert.IsNull(Optimizely.GetFeatureVariableValueForType(featureKey, variableKey, TestUserId, null, variableType));
            Assert.IsNull(Optimizely.GetFeatureVariableValueForType("double_single_variable_feature", variableKey, TestUserId, null, variableType));

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

            Assert.IsNull(Optimizely.GetFeatureVariableValueForType("double_single_variable_feature", "double_variable", TestUserId, null, variableTypeBool));
            Assert.IsNull(Optimizely.GetFeatureVariableValueForType("boolean_single_variable_feature", "boolean_variable", TestUserId, null, variableTypeDouble));
            Assert.IsNull(Optimizely.GetFeatureVariableValueForType("integer_single_variable_feature", "integer_variable", TestUserId, null, variableTypeString));
            Assert.IsNull(Optimizely.GetFeatureVariableValueForType("string_single_variable_feature", "string_variable", TestUserId, null, variableTypeInt));

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
            var variableKey = "double_variable";
            var variableType = FeatureVariable.VariableType.DOUBLE;
            var expectedValue = "14.99";

            DecisionServiceMock.Setup(ds => ds.GetVariationForFeature(featureFlag, TestUserId, null)).Returns<FeatureDecision>(null);

            var optly = Helper.CreatePrivateOptimizely();
            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);

            string variableValue = (string)optly.Invoke("GetFeatureVariableValueForType", featureKey, variableKey, TestUserId, null, variableType);
            Assert.AreEqual(expectedValue, variableValue);

            LoggerMock.Verify(l => l.Log(LogLevel.INFO,
                $@"User ""{TestUserId}"" is not in any variation for feature flag ""{featureKey}"", returning default value ""{variableValue}""."));
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
            var expectedDecision = new FeatureDecision(experiment.Id, differentVariation.Id, FeatureDecision.DECISION_SOURCE_EXPERIMENT);
            var variableKey = "double_variable";
            var variableType = FeatureVariable.VariableType.DOUBLE;
            var expectedValue = "14.99";

            // Mock GetVariationForFeature method to return variation of different feature.
            DecisionServiceMock.Setup(ds => ds.GetVariationForFeature(featureFlag, TestUserId, null)).Returns(expectedDecision);

            var optly = Helper.CreatePrivateOptimizely();
            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);

            string variableValue = (string)optly.Invoke("GetFeatureVariableValueForType", featureKey, variableKey, TestUserId, null, variableType);
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
            var expectedValue = "42.42";
            var experiment = Config.GetExperimentFromKey("test_experiment_double_feature");
            var variation = Config.GetVariationFromKey("test_experiment_double_feature", "control");
            var decision = new FeatureDecision(experiment.Id, variation.Id, FeatureDecision.DECISION_SOURCE_EXPERIMENT);

            DecisionServiceMock.Setup(ds => ds.GetVariationForFeature(featureFlag, TestUserId, null)).Returns(decision);

            var optly = Helper.CreatePrivateOptimizely();
            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);

            string variableValue = (string)optly.Invoke("GetFeatureVariableValueForType", featureKey, variableKey, TestUserId, null, variableType);
            Assert.AreEqual(expectedValue, variableValue);

            LoggerMock.Verify(l => l.Log(LogLevel.INFO,
                $@"Returning variable value ""{variableValue}"" for variation ""{variation.Key}"" of feature flag ""{featureKey}""."));
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

            LoggerMock.Verify(l => l.Log(LogLevel.ERROR, "User ID must not be empty."), Times.Exactly(2));
            LoggerMock.Verify(l => l.Log(LogLevel.ERROR, "Feature flag key must not be empty."), Times.Exactly(2));
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
            var tempConfig = ProjectConfig.Create(TestData.Datafile, LoggerMock.Object, new NoOpErrorHandler());
            var featureFlag = tempConfig.GetFeatureFlagFromKey("multi_variate_feature");

            var optly = Helper.CreatePrivateOptimizely();
            optly.SetFieldOrProperty("Config", tempConfig);

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
            
            DecisionServiceMock.Setup(ds => ds.GetVariationForFeature(featureFlag, TestUserId, null)).Returns<FeatureDecision>(null);

            var optly = Helper.CreatePrivateOptimizely();
            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);

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
            var decision = new FeatureDecision(experiment.Id, variation.Id, FeatureDecision.DECISION_SOURCE_ROLLOUT);

            DecisionServiceMock.Setup(ds => ds.GetVariationForFeature(featureFlag, TestUserId, null)).Returns(decision);

            var optly = Helper.CreatePrivateOptimizely();
            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);

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
            var decision = new FeatureDecision(experiment.Id, variation.Id, FeatureDecision.DECISION_SOURCE_EXPERIMENT);

            DecisionServiceMock.Setup(ds => ds.GetVariationForFeature(featureFlag, TestUserId, null)).Returns(decision);

            var optly = Helper.CreatePrivateOptimizely();
            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);

            bool result = (bool)optly.Invoke("IsFeatureEnabled", featureKey, TestUserId, null);
            Assert.True(result);

            // SendImpressionEvent() gets called.
            LoggerMock.Verify(l => l.Log(LogLevel.INFO,
                $@"The user ""{TestUserId}"" is not being experimented on feature ""{featureKey}""."), Times.Never);

            LoggerMock.Verify(l => l.Log(LogLevel.INFO,
                $@"Feature flag ""{featureKey}"" is enabled for user ""{TestUserId}""."));
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
            var experimentKey = "test_experiment";
            var variationKey = "control";
            var featureKey = "boolean_feature";
            var experiment = Config.GetExperimentFromKey(experimentKey);
            var variation = Config.GetVariationFromKey(experimentKey, variationKey);
            var featureFlag = Config.GetFeatureFlagFromKey(featureKey);
            var decision = new FeatureDecision(experiment.Id, variation.Id, FeatureDecision.DECISION_SOURCE_EXPERIMENT);
            var logEvent = new LogEvent("https://logx.optimizely.com/v1/events", OptimizelyHelper.SingleParameter,
                "POST", new Dictionary<string, string> { });

            // Mocking objects.
            NotificationCallbackMock.Setup(nc => nc.TestActivateCallback(It.IsAny<Experiment>(), It.IsAny<string>(),
                It.IsAny<UserAttributes>(), It.IsAny<Variation>(), It.IsAny<LogEvent>()));
            NotificationCallbackMock.Setup(nc => nc.TestAnotherActivateCallback(It.IsAny<Experiment>(), It.IsAny<string>(),
                It.IsAny<UserAttributes>(), It.IsAny<Variation>(), It.IsAny<LogEvent>()));
            NotificationCallbackMock.Setup(nc => nc.TestTrackCallback(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<UserAttributes>(), It.IsAny<EventTags>(), It.IsAny<LogEvent>()));
            EventBuilderMock.Setup(ebm => ebm.CreateImpressionEvent(It.IsAny<ProjectConfig>(), It.IsAny<Experiment>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UserAttributes>())).Returns(logEvent);
            DecisionServiceMock.Setup(ds => ds.GetVariation(experiment, TestUserId, userAttributes)).Returns(variation);
            DecisionServiceMock.Setup(ds => ds.GetVariationForFeature(featureFlag, TestUserId, userAttributes)).Returns(decision);

            var optly = Helper.CreatePrivateOptimizely();
            var optStronglyTyped = optly.GetObject() as Optimizely;


            // Adding notification listeners.
            var notificationType = NotificationCenter.NotificationType.Activate;
            optStronglyTyped. NotificationCenter.AddNotification(notificationType, NotificationCallbackMock.Object.TestActivateCallback);
            optStronglyTyped.NotificationCenter.AddNotification(notificationType, NotificationCallbackMock.Object.TestAnotherActivateCallback);
                        
            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);
            optly.SetFieldOrProperty("EventBuilder", EventBuilderMock.Object);

            // Calling Activate and IsFeatureEnabled.
            optly.Invoke("Activate", experimentKey, TestUserId, userAttributes);
            optly.Invoke("IsFeatureEnabled", featureKey, TestUserId, userAttributes);

            // Verify that all the registered callbacks are called once for both Activate and IsFeatureEnabled.
            NotificationCallbackMock.Verify(nc => nc.TestActivateCallback(experiment, TestUserId, userAttributes, variation, logEvent), Times.Exactly(2));
            NotificationCallbackMock.Verify(nc => nc.TestAnotherActivateCallback(experiment, TestUserId, userAttributes, variation, logEvent), Times.Exactly(2));
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
            EventBuilderMock.Setup(ebm => ebm.CreateConversionEvent(It.IsAny<ProjectConfig>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, Variation>>(), It.IsAny<string>(), It.IsAny<UserAttributes>(), 
                It.IsAny<EventTags>())).Returns(logEvent);
            DecisionServiceMock.Setup(ds => ds.GetVariation(experiment, TestUserId, userAttributes)).Returns(variation);
            
            // Adding notification listeners.
            var notificationType = NotificationCenter.NotificationType.Track;
            optStronglyTyped.NotificationCenter.AddNotification(notificationType, NotificationCallbackMock.Object.TestTrackCallback);
            optStronglyTyped.NotificationCenter.AddNotification(notificationType, NotificationCallbackMock.Object.TestAnotherTrackCallback);
            
            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);
            optly.SetFieldOrProperty("EventBuilder", EventBuilderMock.Object);

            // Calling Track.
            optly.Invoke("Track", eventKey, TestUserId, userAttributes, eventTags);

            // Verify that all the registered callbacks for Track are called.
            NotificationCallbackMock.Verify(nc => nc.TestTrackCallback(eventKey, TestUserId, userAttributes, eventTags, logEvent), Times.Exactly(1));
            NotificationCallbackMock.Verify(nc => nc.TestAnotherTrackCallback(eventKey, TestUserId, userAttributes, eventTags, logEvent), Times.Exactly(1));
        }

        #endregion // Test NotificationCenter
    }
}
