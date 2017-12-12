/* 
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

            var variationKey = Optimizely.GetVariation("test_experiment", "test_user", attributes);

            LoggerMock.Verify(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<string>()), Times.Exactly(4));
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "User \"test_user\" is not in the forced variation map."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "Assigned bucket [3037] to user [test_user] with bucketing ID [test_user]."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "User [test_user] is in variation [control] of experiment [test_experiment]."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "This decision will not be saved since the UserProfileService is null."), Times.Once);

            Assert.AreEqual("control", variationKey);
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

            var variationkey = optly.Invoke("Activate", "group_experiment_1", "user_1", null);

            EventBuilderMock.Verify(b => b.CreateImpressionEvent(It.IsAny<ProjectConfig>(), Config.GetExperimentFromKey("group_experiment_1"),
                    "7722360022", "user_1", null), Times.Once);

            LoggerMock.Verify(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<string>()), Times.Exactly(8));
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "Assigned bucket [1922] to user [user_1] with bucketing ID [user_1]."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "User [user_1] is in experiment [group_experiment_1] of group [7722400015]."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "Assigned bucket [9525] to user [user_1] with bucketing ID [user_1]."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "User [user_1] is in variation [group_exp_1_var_2] of experiment [group_experiment_1]."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "Activating user user_1 in experiment group_experiment_1."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "Dispatching impression event to URL logx.optimizely.com/decision with params {\"param1\":\"val1\",\"param2\":\"val2\"}."), Times.Once);

            Assert.AreEqual("group_exp_1_var_2", variationkey);
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

            var variationkey = optly.Invoke("Activate", "test_experiment", "test_user", OptimizelyHelper.UserAttributes);

            EventBuilderMock.Verify(b => b.CreateImpressionEvent(It.IsAny<ProjectConfig>(), Config.GetExperimentFromKey("test_experiment"),
                    "7722370027", "test_user", OptimizelyHelper.UserAttributes), Times.Once);

            LoggerMock.Verify(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<string>()), Times.Exactly(6));
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "Assigned bucket [3037] to user [test_user] with bucketing ID [test_user]."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "User [test_user] is in variation [control] of experiment [test_experiment]."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "Activating user test_user in experiment test_experiment."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "Dispatching impression event to URL logx.optimizely.com/decision with params {\"param1\":\"val1\"}."), Times.Once);

            Assert.AreEqual("control", variationkey);
        }

        [Test]
        public void TestActivateWithNullAttributes()
        {
            EventBuilderMock.Setup(b => b.CreateImpressionEvent(It.IsAny<ProjectConfig>(), It.IsAny<Experiment>(),
              It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UserAttributes>()))
              .Returns(new LogEvent("logx.optimizely.com/decision", OptimizelyHelper.SingleParameter, "POST", new Dictionary<string, string> { }));

            var optly = Helper.CreatePrivateOptimizely();
            optly.SetFieldOrProperty("EventBuilder", EventBuilderMock.Object);

            var variationkey = optly.Invoke("Activate", "test_experiment", "test_user", OptimizelyHelper.NullUserAttributes);

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
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "Dispatching impression event to URL logx.optimizely.com/decision with params {\"param1\":\"val1\"}."), Times.Once);

            Assert.AreEqual("control", variationkey);
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

            Assert.AreEqual("control", variation);
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

            var variationkey = optly.Invoke("Activate", "test_experiment", "test_user", OptimizelyHelper.UserAttributes);

            LoggerMock.Verify(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<string>()), Times.Exactly(7));
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "Assigned bucket [3037] to user [test_user] with bucketing ID [test_user]."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "User [test_user] is in variation [control] of experiment [test_experiment]."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "Activating user test_user in experiment test_experiment."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "Dispatching impression event to URL logx.optimizely.com/decision with params {\"param1\":\"val1\"}."), Times.Once);

            Assert.AreEqual("control", variationkey);
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

            //Check whitelisted experiment
            var variation = optimizely.GetVariation("etag1", "testUser1");

            Assert.AreEqual("vtag1", variation);

            //Set forced variation
            Assert.IsTrue(optimizely.SetForcedVariation("etag1", "testUser1", "vtag2"));
            var variation2 = optimizely.GetVariation("etag1", "testUser1");

            // verify forced variation preceeds whitelisted variation
            Assert.AreEqual("vtag2", variation2);

            // remove forced variation and verify whitelisted should be returned.
            Assert.IsTrue(optimizely.SetForcedVariation("etag1", "testUser1", null));
            var variation3 = optimizely.GetVariation("etag1", "testUser1");

            Assert.AreEqual("vtag1", variation3);

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
            var experimentId = "etag1";
            var userId = "testUser3";
            var variationId = "vtag2";
            var fbVariationId = "vtag1";

            UserProfile userProfile = new UserProfile(userId, new Dictionary<string, Decision>
            {
                { experimentId, new Decision(variationId)}
            });

            userProfileServiceMock.Setup(_ => _.Lookup(userId)).Returns(userProfile.ToMap());

            var optimizely = new Optimizely(TestData.NoAudienceProjectConfigV3, EventDispatcher, LoggerMock.Object, ErrorHandlerMock.Object, userProfileServiceMock.Object);

            var variationUserProfile = optimizely.GetVariation(experimentId, userId);

            Assert.AreEqual(variationUserProfile, variationId);

            //assign same user with different variation, forced variation have higher priority
            Assert.IsTrue(optimizely.SetForcedVariation(experimentId, userId, fbVariationId));

            var variation2 = optimizely.GetVariation(experimentId, userId);

            Assert.AreEqual(fbVariationId, variation2);

            //remove forced variation and re-check userprofile
            Assert.IsTrue(optimizely.SetForcedVariation(experimentId, userId, null));

            var variationUserProfile2 = optimizely.GetVariation(experimentId, userId);

            Assert.AreEqual(variationUserProfile2, variationId);

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
            var expectedVarationKey = "control";
            var experimentKey = "test_experiment";

            var userAttributes = new UserAttributes
            {
                {"device_type", "iPhone" },
                {"location", "San Francisco" }
            };

            Optimizely.Activate(experimentKey, TestUserId, userAttributes);

            // set variation
            Assert.IsTrue(Optimizely.SetForcedVariation(experimentKey, TestUserId, expectedForcedVariationKey), "Set forced variation to variation failed.");

            var actualForcedVariationKey = Optimizely.GetVariation(experimentKey, TestUserId, userAttributes);
            Assert.AreEqual(actualForcedVariationKey, expectedForcedVariationKey, string.Format(@"Forced variation key should be variation, but got ""{0}"".", expectedForcedVariationKey));

            // clear variation and check that the user gets bucketed normally
            Assert.IsTrue(Optimizely.SetForcedVariation(experimentKey, TestUserId, null), "Clear forced variation failed.");

            var actualVariationKey = Optimizely.GetVariation("test_experiment", "test_user", userAttributes);

            Assert.AreEqual(expectedVarationKey, actualVariationKey, string.Format(@"Variation key should be control, but got ""{0}"".", actualForcedVariationKey));
        }

        // check that the forced variation is set correctly
        [Test]
        public void TestSetForcedVariation()
        {
            var experimentKey = "test_experiment";
            var expectedVariationKey = "control";
            var expectedForcedVariationKey = "variation";

            var userAttributes = new UserAttributes
            {
                {"device_type", "iPhone" },
                {"location", "San Francisco" }
            };

            Optimizely.Activate(experimentKey, TestUserId, userAttributes);

            // test invalid experiment -. normal bucketing should occur
            Assert.IsFalse(Optimizely.SetForcedVariation("bad_experiment", TestUserId, "bad_control"), "Set variation to 'variation' should have failed  because of invalid experiment.");

            var variationKey = Optimizely.GetVariation(experimentKey, TestUserId, userAttributes);
            Assert.AreEqual(expectedVariationKey, variationKey);

            // test invalid variation -. normal bucketing should occur
            Assert.IsFalse(Optimizely.SetForcedVariation("test_experiment", TestUserId, "bad_variation"), "Set variation to 'bad_variation' should have failed.");

            variationKey = Optimizely.GetVariation("test_experiment", "test_user", userAttributes);
            Assert.AreEqual(expectedVariationKey, variationKey);

            // test valid variation -. the user should be bucketed to the specified forced variation
            Assert.IsTrue(Optimizely.SetForcedVariation(experimentKey, TestUserId, expectedForcedVariationKey), "Set variation to 'variation' failed.");

            var actualForcedVariationKey = Optimizely.GetVariation(experimentKey, TestUserId, userAttributes);
            Assert.AreEqual(expectedForcedVariationKey, actualForcedVariationKey);

            // make sure another setForcedVariation call sets a new forced variation correctly
            Assert.IsTrue(Optimizely.SetForcedVariation(experimentKey, "test_user2", expectedForcedVariationKey), "Set variation to 'variation' failed.");
            actualForcedVariationKey = Optimizely.GetVariation(experimentKey, "test_user2", userAttributes);

            Assert.AreEqual(expectedForcedVariationKey, actualForcedVariationKey);
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
            
            Assert.AreEqual("control", variation);
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

            Assert.AreEqual("variation", variation);
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
            var variationkey = optly.Invoke("Activate", "group_experiment_1", "user_1", null);

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
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "Dispatching impression event to URL logx.optimizely.com/decision with params {\"param1\":\"val1\",\"param2\":\"val2\"}."), Times.Once);

            Assert.AreEqual("group_exp_1_var_2", variationkey);
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
            var variationKeyControl = "control";
            var variationKeyVariation = "variation";
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
            var actualVariationKey = Optimizely.GetVariation(experimentKey, userId, userAttributes);
            Assert.AreEqual(variationKeyControl, actualVariationKey, string.Format("Invalid variation key \"{0}\" for getVariation.", actualVariationKey));

            // confirm that invalid audience returns null
            actualVariationKey = Optimizely.GetVariation(experimentKey, userId);
            Assert.AreEqual(null, actualVariationKey, string.Format("Invalid variation key \"{0}\" for getVariation with bucketing ID \"{1}\".", actualVariationKey, testBucketingIdControl));

            // confirm that a valid variation is bucketed with the bucketing ID
            actualVariationKey = Optimizely.GetVariation(experimentKey, userId, userAttributesWithBucketingId);
            Assert.AreEqual(variationKeyVariation, actualVariationKey, string.Format("Invalid variation key \"{0}\" for getVariation with bucketing ID \"{1}\".", actualVariationKey, testBucketingIdVariation));

            // confirm that invalid experiment with the bucketing ID returns null
            actualVariationKey = Optimizely.GetVariation("invalidExperimentKey", userId, userAttributesWithBucketingId);
            Assert.AreEqual(null, actualVariationKey, string.Format("Invalid variation key \"{0}\" for getVariation with bucketing ID \"{1}\".", actualVariationKey, testBucketingIdControl));
        }

        #endregion

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

            var optly = Helper.CreatePrivateOptimizely();
            var optStronglyTyped = optly.GetObject() as Optimizely;


            // Adding notification listeners.
            var notificationType = NotificationCenter.NotificationType.Activate;
            optStronglyTyped. NotificationCenter.AddNotification(notificationType, NotificationCallbackMock.Object.TestActivateCallback);
            optStronglyTyped.NotificationCenter.AddNotification(notificationType, NotificationCallbackMock.Object.TestAnotherActivateCallback);
                        
            optly.SetFieldOrProperty("DecisionService", DecisionServiceMock.Object);
            optly.SetFieldOrProperty("EventBuilder", EventBuilderMock.Object);

            // Calling Activate.
            optly.Invoke("Activate", experimentKey, TestUserId, userAttributes);

            // Verify that all the registered callbacks are called once for Activate.
            NotificationCallbackMock.Verify(nc => nc.TestActivateCallback(experiment, TestUserId, userAttributes, variation, logEvent), Times.Exactly(1));
            NotificationCallbackMock.Verify(nc => nc.TestAnotherActivateCallback(experiment, TestUserId, userAttributes, variation, logEvent), Times.Exactly(1));
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
