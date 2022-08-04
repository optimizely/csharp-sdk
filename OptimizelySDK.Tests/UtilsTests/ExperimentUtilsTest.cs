﻿/* 
 * Copyright 2019-2022, Optimizely
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
using OptimizelySDK.Config;
using OptimizelySDK.Entity;
using OptimizelySDK.Logger;
using OptimizelySDK.Tests.Utils;
using OptimizelySDK.Utils;

namespace OptimizelySDK.Tests.UtilsTests
{
    [TestFixture]
    public class ExperimentUtilsTest
    {
        private ProjectConfig Config;
        private Mock<ILogger> LoggerMock;
        private ILogger Logger;

        [SetUp]
        public void Setup()
        {
            Config = DatafileProjectConfig.Create(TestData.TypedAudienceDatafile, null, null);
            LoggerMock = new Mock<ILogger>();
            LoggerMock.Setup(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<string>()));
            Logger = LoggerMock.Object;
        }

        #region DoesUserMeetAudienceConditions Tests

        [Test]
        public void TestDoesUserMeetAudienceConditionsReturnsTrueWithNoAudience()
        {
            var experiment = Config.GetExperimentFromKey("feat_with_var_test");
            experiment.AudienceIds = new string[] { };
            experiment.AudienceConditions = null;

            Assert.True(ExperimentUtils.DoesUserMeetAudienceConditions(Config, experiment, new UserAttributes().ToUserContext(), "experiment", experiment.Key, Logger).ResultObject);

            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, @"Evaluating audiences for experiment ""feat_with_var_test"": []."), Times.Once);
        }

        [Test]
        public void TestDoesUserMeetAudienceConditionsReturnsTrueWhenAudienceUsedInExperimentNoAttributesProvided()
        {
            var experiment = Config.GetExperimentFromKey("feat_with_var_test");
            experiment.AudienceIds = new string[] { "3468206648" };
            
            var boolResult = ExperimentUtils.DoesUserMeetAudienceConditions(Config, experiment, new UserAttributes { }.ToUserContext(), "experiment", experiment.Key, Logger);
            Assert.True(boolResult.ResultObject);

            Assert.AreEqual(boolResult.DecisionReasons.ToReport(true)[0], "Audiences for experiment \"feat_with_var_test\" collectively evaluated to TRUE");
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, @"Evaluating audiences for experiment ""feat_with_var_test"": [""3468206648""]."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, @"Starting to evaluate audience ""3468206648"" with conditions: [""not"",{""name"":""input_value"",""type"":""custom_attribute"",""match"":""exists""}]"), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, $@"Audience ""3468206648"" evaluated to TRUE"), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, @"Audiences for experiment ""feat_with_var_test"" collectively evaluated to TRUE"), Times.Once);
        }

        [Test]
        public void TestDoesUserMeetAudienceConditionsReturnsFalseIfNoAudienceInORConditionPass()
        {
            var experiment = Config.GetExperimentFromKey("feat_with_var_test");
            var userAttributes = new UserAttributes
            {
                { "house", "Ravenclaw" },
                { "lasers", 50 },
                { "should_do_it", false }
            };

            Assert.False(ExperimentUtils.DoesUserMeetAudienceConditions(Config, experiment, userAttributes.ToUserContext(), "experiment", experiment.Key, Logger).ResultObject);

            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, @"Evaluating audiences for experiment ""feat_with_var_test"": [""3468206642"",""3988293898"",""3988293899"",""3468206646"",""3468206647"",""3468206644"",""3468206643""]."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, @"Starting to evaluate audience ""3468206642"" with conditions: [""and"", [""or"", [""or"", {""name"": ""house"", ""type"": ""custom_attribute"", ""value"": ""Gryffindor""}]]]"), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, @"Audience ""3468206642"" evaluated to FALSE"), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, @"Starting to evaluate audience ""3988293899"" with conditions: [""and"",[""or"",[""or"",{""name"":""favorite_ice_cream"",""type"":""custom_attribute"",""match"":""exists""}]]]"), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, @"Audience ""3988293899"" evaluated to FALSE"), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, @"Starting to evaluate audience ""3468206646"" with conditions: [""and"",[""or"",[""or"",{""name"":""lasers"",""type"":""custom_attribute"",""match"":""exact"",""value"":45.5}]]]"), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, @"Audience ""3468206646"" evaluated to FALSE"), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, @"Starting to evaluate audience ""3468206647"" with conditions: [""and"",[""or"",[""or"",{""name"":""lasers"",""type"":""custom_attribute"",""match"":""gt"",""value"":70}]]]"), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, @"Audience ""3468206647"" evaluated to FALSE"), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, @"Starting to evaluate audience ""3468206644"" with conditions: [""and"",[""or"",[""or"",{""name"":""lasers"",""type"":""custom_attribute"",""match"":""lt"",""value"":1.0}]]]"), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, @"Audience ""3468206644"" evaluated to FALSE"), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, @"Starting to evaluate audience ""3468206643"" with conditions: [""and"",[""or"",[""or"",{""name"":""should_do_it"",""type"":""custom_attribute"",""match"":""exact"",""value"":true}]]]"), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, @"Audience ""3468206643"" evaluated to FALSE"), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, @"Audiences for experiment ""feat_with_var_test"" collectively evaluated to FALSE"), Times.Once);
        }

        [Test]
        public void TestDoesUserMeetAudienceConditionsReturnsTrueIfAnyAudienceInORConditionPass()
        {
            var experiment = Config.GetExperimentFromKey("feat_with_var_test");
            var userAttributes = new UserAttributes
            {
                { "house", "Gryffindor" },
                { "lasers", 50 },
                { "should_do_it", false }
            };

            Assert.True(ExperimentUtils.DoesUserMeetAudienceConditions(Config, experiment, userAttributes.ToUserContext(), "experiment", experiment.Key, Logger).ResultObject);

            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, @"Evaluating audiences for experiment ""feat_with_var_test"": [""3468206642"",""3988293898"",""3988293899"",""3468206646"",""3468206647"",""3468206644"",""3468206643""]."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, @"Starting to evaluate audience ""3468206642"" with conditions: [""and"", [""or"", [""or"", {""name"": ""house"", ""type"": ""custom_attribute"", ""value"": ""Gryffindor""}]]]"), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, @"Audience ""3468206642"" evaluated to TRUE"), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, @"Audiences for experiment ""feat_with_var_test"" collectively evaluated to TRUE"), Times.Once);
        }

        [Test]
        public void TestDoesUserMeetAudienceConditionsReturnsTrueIfAllAudiencesInANDConditionPass()
        {
            var experiment = Config.GetExperimentFromKey("audience_combinations_experiment");
            var userAttributes = new UserAttributes
            {
                { "house", "Gryffindor" },
                { "favorite_ice_cream", "walls" }
            };

            Assert.True(ExperimentUtils.DoesUserMeetAudienceConditions(Config, experiment, userAttributes.ToUserContext(), "experiment", experiment.Key, Logger).ResultObject);
        }

        [Test]
        public void TestDoesUserMeetAudienceConditionsReturnsFalseIfAnyAudienceInANDConditionDoesNotPass()
        {
            var experiment = Config.GetExperimentFromKey("audience_combinations_experiment");
            var userAttributes = new UserAttributes
            {
                { "house", "Gryffindor" },
                { "lasers", 50 }
            };

            Assert.False(ExperimentUtils.DoesUserMeetAudienceConditions(Config, experiment, userAttributes.ToUserContext(), "experiment", experiment.Key, Logger).ResultObject);

            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, @"Evaluating audiences for experiment ""audience_combinations_experiment"": [""and"",[""or"",""3468206642"",""3988293898""],[""or"",""3988293899"",""3468206646"",""3468206647"",""3468206644"",""3468206643""]]."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, @"Starting to evaluate audience ""3468206642"" with conditions: [""and"", [""or"", [""or"", {""name"": ""house"", ""type"": ""custom_attribute"", ""value"": ""Gryffindor""}]]]"), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, @"Audience ""3468206642"" evaluated to TRUE"), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, @"Starting to evaluate audience ""3988293899"" with conditions: [""and"",[""or"",[""or"",{""name"":""favorite_ice_cream"",""type"":""custom_attribute"",""match"":""exists""}]]]"), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, @"Audience ""3988293899"" evaluated to FALSE"), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, @"Starting to evaluate audience ""3468206646"" with conditions: [""and"",[""or"",[""or"",{""name"":""lasers"",""type"":""custom_attribute"",""match"":""exact"",""value"":45.5}]]]"), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, @"Audience ""3468206646"" evaluated to FALSE"), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, @"Starting to evaluate audience ""3468206647"" with conditions: [""and"",[""or"",[""or"",{""name"":""lasers"",""type"":""custom_attribute"",""match"":""gt"",""value"":70}]]]"), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, @"Audience ""3468206647"" evaluated to FALSE"), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, @"Starting to evaluate audience ""3468206644"" with conditions: [""and"",[""or"",[""or"",{""name"":""lasers"",""type"":""custom_attribute"",""match"":""lt"",""value"":1.0}]]]"), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, @"Audience ""3468206644"" evaluated to FALSE"), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, @"Starting to evaluate audience ""3468206643"" with conditions: [""and"",[""or"",[""or"",{""name"":""should_do_it"",""type"":""custom_attribute"",""match"":""exact"",""value"":true}]]]"), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, @"Audience condition {""type"":""custom_attribute"",""match"":""exact"",""name"":""should_do_it"",""value"":true} evaluated to UNKNOWN because no value was passed for user attribute ""should_do_it""."), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.DEBUG, @"Audience ""3468206643"" evaluated to UNKNOWN"), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.INFO, @"Audiences for experiment ""audience_combinations_experiment"" collectively evaluated to FALSE"), Times.Once);
        }

        [Test]
        public void TestDoesUserMeetAudienceConditionsReturnsTrueIfAudienceInNOTConditionDoesNotPass()
        {
            var experiment = Config.GetExperimentFromKey("feat_with_var_test");
            experiment.AudienceIds = new string[] { "3468206645" };
            var userAttributes = new UserAttributes
            {
                { "browser_type", "Safari" }
            };

            Assert.True(ExperimentUtils.DoesUserMeetAudienceConditions(Config, experiment, userAttributes.ToUserContext(), "experiment", experiment.Key, Logger).ResultObject);
        }

        [Test]
        public void TestDoesUserMeetAudienceConditionsReturnsFalseIfAudienceInNOTConditionGetsPassed()
        {
            var experiment = Config.GetExperimentFromKey("feat_with_var_test");
            experiment.AudienceIds = new string[] { "3468206645" };
            var userAttributes = new UserAttributes
            {
                { "browser_type", "Chrome" }
            };

            Assert.False(ExperimentUtils.DoesUserMeetAudienceConditions(Config, experiment, userAttributes.ToUserContext(), "experiment", experiment.Key, Logger).ResultObject);
        }

        #endregion // DoesUserMeetAudienceConditions Tests
    }
}
