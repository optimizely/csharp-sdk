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
                It.IsAny<IEnumerable<Experiment>>(), It.IsAny<string>(), It.IsAny<UserAttributes>(), It.IsAny<EventTags>()));

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
        }

        [TestFixtureTearDown]
        public void Cleanup()
        {
            LoggerMock = null;
            Config = null;
            EventBuilderMock = null;
        }
        
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
            var optly = Helper.CreatePrivateOptimizely();
            var attributes = new UserAttributes
            {
                { "device_type", "iPhone" },
                { "location", "San Francisco" }
                
            };
            bool result = (bool)optly.Invoke("ValidatePreconditions",
                Config.GetExperimentFromKey("test_experiment"),
                "user1", attributes);
            Assert.IsTrue(result);
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
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "Assigned bucket [8495] to user [not_in_variation_user]"), Times.Once);
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
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "Assigned bucket [1922] to user [user_1]"), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "User [user_1] is in experiment [group_experiment_1] of group [7722400015]."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "Assigned bucket [9525] to user [user_1]"), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "User [user_1] is in variation [group_exp_1_var_2] of experiment [group_experiment_1]."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "Activating user user_1 in experiment group_experiment_1."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "Dispatching impression event to URL OptimizelySDK.Event.LogEvent with params {\"param1\":\"val1\",\"param2\":\"val2\"}."), Times.Once);

            Assert.AreEqual("group_exp_1_var_2", variationkey);
        }

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
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "Assigned bucket [3037] to user [test_user]"), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "User [test_user] is in variation [control] of experiment [test_experiment]."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "Activating user test_user in experiment test_experiment."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "Dispatching impression event to URL OptimizelySDK.Event.LogEvent with params {\"param1\":\"val1\"}."), Times.Once);

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
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "Assigned bucket [3037] to user [test_user]"), Times.Once);
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
                It.IsAny<IEnumerable<Experiment>>(), It.IsAny<string>(), It.IsAny<UserAttributes>(), It.IsAny<EventTags>()))
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
                It.IsAny<IEnumerable<Experiment>>(), It.IsAny<string>(), It.IsAny<UserAttributes>(), It.IsAny<EventTags>()))
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
                It.IsAny<IEnumerable<Experiment>>(), It.IsAny<string>(), It.IsAny<UserAttributes>(), It.IsAny<EventTags>()))
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
                It.IsAny<IEnumerable<Experiment>>(), It.IsAny<string>(), It.IsAny<UserAttributes>(), It.IsAny<EventTags>()))
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
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "Assigned bucket [3037] to user [test_user]"), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "User [test_user] is in variation [control] of experiment [test_experiment]."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, "Activating user test_user in experiment test_experiment."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, "Dispatching impression event to URL OptimizelySDK.Event.LogEvent with params {\"param1\":\"val1\"}."), Times.Once);

            Assert.AreEqual("control", variationkey);
        }

        [Test]
        public void TestInvalidDispatchConversionEvent()
        {
            EventBuilderMock.Setup(b => b.CreateConversionEvent(It.IsAny<ProjectConfig>(), It.IsAny<string>(),
                 It.IsAny<IEnumerable<Experiment>>(), It.IsAny<string>(), It.IsAny<UserAttributes>(), It.IsAny<EventTags>()))
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

        /* Start 1 */
     public void TestTrackNoAttributesWithInvalidEventValue()
     {
            EventBuilderMock.Setup(b => b.CreateConversionEvent(It.IsAny<ProjectConfig>(), It.IsAny<string>(),
                 It.IsAny<IEnumerable<Experiment>>(), It.IsAny<string>(), It.IsAny<UserAttributes>(), It.IsAny<EventTags>()))
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
                 It.IsAny<IEnumerable<Experiment>>(), It.IsAny<string>(), It.IsAny<UserAttributes>(), It.IsAny<EventTags>()))
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

        /* End */

        
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
            var expectedForcedVariation = "variation";

            var userAttributes = new UserAttributes
            {
                {"device_type", "iPhone" },
                {"location", "San Francisco" }
            };

            Optimizely.Activate(experimentKey, TestUserId, userAttributes);

            Assert.IsTrue(Optimizely.SetForcedVariation(experimentKey, TestUserId, expectedForcedVariation), "Set variation to 'variation' failed.");

            // call getForcedVariation with valid experiment key and valid user ID
            var actualForcedVariationKey = Optimizely.GetForcedVariation("test_experiment", TestUserId);
            Assert.AreEqual(expectedForcedVariation, actualForcedVariationKey);
            
            // call getForcedVariation with invalid experiment and valid userID
            actualForcedVariationKey = Optimizely.GetForcedVariation("invalid_experiment", TestUserId);
            Assert.Null(actualForcedVariationKey);

            // call getForcedVariation with valid experiment and invalid userID
            actualForcedVariationKey = Optimizely.GetForcedVariation("test_experiment", "invalid_user");
            Assert.Null(actualForcedVariationKey);

            // call getForcedVariation with an experiment that"s not running
            Assert.IsTrue(Optimizely.SetForcedVariation("paused_experiment", "test_user2", "variation"), "Set variation to 'variation' failed.");
            actualForcedVariationKey = Optimizely.GetForcedVariation("paused_experiment", "test_user2");

            Assert.AreEqual("variation", actualForcedVariationKey);
            // confirm that the second setForcedVariation call did not invalidate the first call to that method
            actualForcedVariationKey = Optimizely.GetForcedVariation("test_experiment", TestUserId);

            Assert.AreEqual(expectedForcedVariation, actualForcedVariationKey);
        }
    }
}