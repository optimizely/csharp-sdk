/* 
 * Copyright 2020, Optimizely
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
using OptimizelySDK.ErrorHandler;
using OptimizelySDK.Logger;
using OptimizelySDK.Utils;
using OptimizelySDK.Entity;
using OptimizelySDK.Config;
using Xunit;

namespace OptimizelySDK.XUnitTests.UtilsTests
{
    public class ValidatorTest
    {
        private ILogger Logger;
        private IErrorHandler ErrorHandler;
        private ProjectConfig Config;

        public ValidatorTest()
        {
            Logger = new DefaultLogger();
            ErrorHandler = new DefaultErrorHandler();

            Config = DatafileProjectConfig.Create(TestData.Datafile, Logger, ErrorHandler);
        }

        [Fact]
        public void TestValidateJsonSchemaValidFileWithAdditionalProperty()
        {
            Assert.True(Validator.ValidateJSONSchema(TestData.Datafile));

            var testDataJSON = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Collections.Generic.Dictionary<string, object>>(TestData.Datafile);
            Assert.True(testDataJSON.ContainsKey("tempproperty"));
        }

        [Fact]
        public void TestValidateJsonSchemaValidFile()
        {
            Assert.True(Validator.ValidateJSONSchema(TestData.Datafile));
        }

        [Fact]
        public void TestValidateJsonSchemaInvalidFile()
        {
            var invalidScehma = @"{""key1"": ""val1""}";
            Assert.False(Validator.ValidateJSONSchema(invalidScehma));
        }


        [Fact]
        public void TestValidateJsonSchemaNoJsonContent()
        {
            var invalidDataFile = @"Some Randaom file";
            Assert.False(Validator.ValidateJSONSchema(invalidDataFile));
        }


        /*
         * Strongly typed userAttributes can't be invalid. That's why these methods can't be tested.
         * It is 100% guaranteed the arguments will be Dicitonary<string, string>
         * public void TestAreAttributesValidValidAttributes()
         * public void TestAreAttributesValidInvalidAttributes()
         * 
         */


        [Fact]
        public void TestDoesUserMeetAudienceConditionsNoAudienceUsedInExperiment()
        {
            Assert.True(ExperimentUtils.DoesUserMeetAudienceConditions(Config, Config.GetExperimentFromKey("paused_experiment"), new UserAttributes(), "experiment", "paused_experiment", Logger));
        }

        [Fact]
        public void TestDoesUserMeetAudienceConditionsAudienceUsedInExperimentNoAttributesProvided()
        {
            Assert.False(ExperimentUtils.DoesUserMeetAudienceConditions(Config, Config.GetExperimentFromKey("test_experiment"), new UserAttributes(), "experiment", "test_experiment", Logger));

            Assert.False(ExperimentUtils.DoesUserMeetAudienceConditions(Config, Config.GetExperimentFromKey("test_experiment"), null, "experiment", "test_experiment", Logger));
        }

        [Fact]
        public void TestUserInExperimentAudienceMatch()
        {
            var userAttributes = new UserAttributes
            {
                {"device_type", "iPhone" },
                {"location", "San Francisco" }
            };
            Assert.True(ExperimentUtils.DoesUserMeetAudienceConditions(Config, Config.GetExperimentFromKey("test_experiment"), userAttributes, "experiment", "test_experiment", Logger));
        }

        [Fact]
        public void TestDoesUserMeetAudienceConditionsAudienceNoMatch()
        {
            var userAttributes = new UserAttributes
            {
                {"device_type", "Android" },
                {"location", "San Francisco" }
            };

            Assert.False(ExperimentUtils.DoesUserMeetAudienceConditions(Config, Config.GetExperimentFromKey("test_experiment"), null, "experiment", "test_experiment", Logger));
        }

        [Fact]
        public void TestAreEventTagsValidValidEventTags()
        {
            Assert.True(Validator.AreEventTagsValid(new System.Collections.Generic.Dictionary<string, object>()));
            Assert.True(Validator.AreEventTagsValid(new System.Collections.Generic.Dictionary<string, object>
            {
                {"location", "San Francisco" },
                {"browser", "Firefox" },
                {"revenue", 0 }
            }));
        }

        [Fact]
        public void TestAreEventTagsValidInvalidTags()
        {
            // Some of the tests cases are not applicable because C# is strongly typed.

            Assert.False(Validator.AreEventTagsValid(new System.Collections.Generic.Dictionary<string, object>
            {
                {"43",  "23"}
            }));
        }

        [Fact]
        public void TestIsUserAttributeValidWithValidValues()
        {
            var userAttributes = new UserAttributes
            {
                { "device_type", "Android" },
                { "is_firefox", true },
                { "num_users", 15 },
                { "pi_value", 3.14 }
            };

            foreach (var attribute in userAttributes)
                Assert.True(Validator.IsUserAttributeValid(attribute));
        }

        [Fact]
        public void TestIsUserAttributeValidWithInvalidValues()
        {
            var invalidUserAttributes = new UserAttributes
            {
                { "objects", new object() },
                { "arrays", new string[] { "a", "b", "c" } }
            };

            foreach (var attribute in invalidUserAttributes)
                Assert.False(Validator.IsUserAttributeValid(attribute));
        }

        [Fact]
        public void TestIsUserAttributeValidWithEmptyKeyOrValue()
        {
            var validUserAttributes = new UserAttributes
            {
                { "", "Android" },
                { "integer", 0 },
                { "string", string.Empty }
            };

            foreach (var attribute in validUserAttributes)
                Assert.True(Validator.IsUserAttributeValid(attribute));    
        }
    }
}