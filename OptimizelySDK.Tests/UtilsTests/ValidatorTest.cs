﻿/* 
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

using NUnit.Framework;
using OptimizelySDK.Config;
using OptimizelySDK.Entity;
using OptimizelySDK.ErrorHandler;
using OptimizelySDK.Logger;
using OptimizelySDK.Tests.Utils;
using OptimizelySDK.Utils;

namespace OptimizelySDK.Tests.UtilsTests
{
    [TestFixture]
    public class ValidatorTest
    {
        private ILogger Logger;
        private IErrorHandler ErrorHandler;
        private ProjectConfig Config;

        [TestFixtureSetUp]
        public void Setup()
        {
            Logger = new DefaultLogger();
            ErrorHandler = new DefaultErrorHandler();

            Config = DatafileProjectConfig.Create(TestData.Datafile, Logger, ErrorHandler);
        }

        [Test]
        public void TestValidateJsonSchemaValidFileWithAdditionalProperty()
        {
            Assert.IsTrue(Validator.ValidateJSONSchema(TestData.Datafile));

            var testDataJSON =
                Newtonsoft.Json.JsonConvert.
                    DeserializeObject<System.Collections.Generic.Dictionary<string, object>>(
                        TestData.Datafile);
            Assert.IsTrue(testDataJSON.ContainsKey("tempproperty"));
        }

        [Test]
        public void TestValidateJsonSchemaValidFile()
        {
            Assert.IsTrue(Validator.ValidateJSONSchema(TestData.Datafile));
        }

        [Test]
        public void TestValidateJsonSchemaInvalidFile()
        {
            var invalidScehma = @"{""key1"": ""val1""}";
            Assert.IsFalse(Validator.ValidateJSONSchema(invalidScehma));
        }


        [Test]
        public void TestValidateJsonSchemaNoJsonContent()
        {
            var invalidDataFile = @"Some Randaom file";
            Assert.IsFalse(Validator.ValidateJSONSchema(invalidDataFile));
        }


        /*
         * Strongly typed userAttributes can't be invalid. That's why these methods can't be tested.
         * It is 100% guaranteed the arguments will be Dicitonary<string, string>
         * public void TestAreAttributesValidValidAttributes()
         * public void TestAreAttributesValidInvalidAttributes()
         * 
         */


        [Test]
        public void TestDoesUserMeetAudienceConditionsNoAudienceUsedInExperiment()
        {
            Assert.IsTrue(ExperimentUtils.DoesUserMeetAudienceConditions(Config,
                    Config.GetExperimentFromKey("paused_experiment"),
                    new UserAttributes().ToUserContext(), "experiment", "paused_experiment",
                    Logger).
                ResultObject);
        }

        [Test]
        public void TestDoesUserMeetAudienceConditionsAudienceUsedInExperimentNoAttributesProvided()
        {
            Assert.IsFalse(ExperimentUtils.DoesUserMeetAudienceConditions(Config,
                    Config.GetExperimentFromKey("test_experiment"),
                    new UserAttributes().ToUserContext(), "experiment", "test_experiment", Logger).
                ResultObject);

            Assert.IsFalse(ExperimentUtils.DoesUserMeetAudienceConditions(Config,
                    Config.GetExperimentFromKey("test_experiment"), null, "experiment",
                    "test_experiment", Logger).
                ResultObject);
        }

        [Test]
        public void TestUserInExperimentAudienceMatch()
        {
            var userAttributes = new UserAttributes
            {
                { "device_type", "iPhone" },
                { "location", "San Francisco" },
            };
            Assert.IsTrue(ExperimentUtils.DoesUserMeetAudienceConditions(Config,
                    Config.GetExperimentFromKey("test_experiment"), userAttributes.ToUserContext(),
                    "experiment", "test_experiment", Logger).
                ResultObject);
        }

        [Test]
        public void TestDoesUserMeetAudienceConditionsAudienceNoMatch()
        {
            Assert.IsFalse(ExperimentUtils.DoesUserMeetAudienceConditions(Config,
                    Config.GetExperimentFromKey("test_experiment"),
                    new UserAttributes().ToUserContext(), "experiment", "test_experiment", Logger).
                ResultObject);
        }

        [Test]
        public void TestAreEventTagsValidValidEventTags()
        {
            Assert.IsTrue(
                Validator.AreEventTagsValid(
                    new System.Collections.Generic.Dictionary<string, object>()));
            Assert.IsTrue(Validator.AreEventTagsValid(
                new System.Collections.Generic.Dictionary<string, object>
                {
                    { "location", "San Francisco" },
                    { "browser", "Firefox" },
                    { "revenue", 0 },
                }));
        }

        [Test]
        public void TestAreEventTagsValidInvalidTags()
        {
            // Some of the tests cases are not applicable because C# is strongly typed.

            Assert.IsFalse(Validator.AreEventTagsValid(
                new System.Collections.Generic.Dictionary<string, object>
                {
                    { "43", "23" },
                }));
        }

        [Test]
        public void TestIsUserAttributeValidWithValidValues()
        {
            var userAttributes = new UserAttributes
            {
                { "device_type", "Android" },
                { "is_firefox", true },
                { "num_users", 15 },
                { "pi_value", 3.14 },
            };

            foreach (var attribute in userAttributes)
            {
                Assert.True(Validator.IsUserAttributeValid(attribute));
            }
        }

        [Test]
        public void TestIsUserAttributeValidWithInvalidValues()
        {
            var invalidUserAttributes = new UserAttributes
            {
                { "objects", new object() },
                { "arrays", new string[] { "a", "b", "c" } },
            };

            foreach (var attribute in invalidUserAttributes)
            {
                Assert.False(Validator.IsUserAttributeValid(attribute));
            }
        }

        [Test]
        public void TestIsUserAttributeValidWithEmptyKeyOrValue()
        {
            var validUserAttributes = new UserAttributes
            {
                { "", "Android" },
                { "integer", 0 },
                { "string", string.Empty },
            };

            foreach (var attribute in validUserAttributes)
            {
                Assert.True(Validator.IsUserAttributeValid(attribute));
            }
        }
    }
}
