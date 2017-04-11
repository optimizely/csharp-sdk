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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OptimizelySDK.ErrorHandler;
using OptimizelySDK.Logger;
using OptimizelySDK.Utils;
using OptimizelySDK.Entity;

namespace OptimizelySDK.Tests.UtilsTests
{
    [TestClass]
    public class ValidatorTest
    {
        private ILogger Logger;
        private IErrorHandler ErrorHandler;
        private ProjectConfig Config;

        [TestInitialize]
        public void Setup()
        {
            Logger = new DefaultLogger();
            ErrorHandler = new DefaultErrorHandler();

            Config = ProjectConfig.Create(TestData.Datafile, Logger, ErrorHandler);
        }

        [TestMethod]
        public void TestValidateJsonSchemaValidFileWithAdditionalProperty()
        {
            Assert.IsTrue(Validator.ValidateJSONSchema(TestData.Datafile));

            var testDataJSON = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Collections.Generic.Dictionary<string, object>>(TestData.Datafile);
            Assert.IsTrue(testDataJSON.ContainsKey("tempproperty"));
        }

        [TestMethod]
        public void TestValidateJsonSchemaValidFile()
        {
            Assert.IsTrue(Validator.ValidateJSONSchema(TestData.Datafile));
        }

        [TestMethod]
        public void TestValidateJsonSchemaInvalidFile()
        {
            var invalidScehma = @"{""key1"": ""val1""}";
            Assert.IsFalse(Validator.ValidateJSONSchema(invalidScehma));
        }


        [TestMethod]
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


        [TestMethod]
        public void TestIsUserInExperimentNoAudienceUsedInExperiment()
        {
            Assert.IsTrue(Validator.IsUserInExperiment(Config, Config.GetExperimentFromKey("paused_experiment"), new UserAttributes()));
        }

        [TestMethod]
        public void TestIsUserInExperimentAudienceUsedInExperimentNoAttributesProvided()
        {
            Assert.IsFalse(Validator.IsUserInExperiment(Config, Config.GetExperimentFromKey("test_experiment"), new UserAttributes()));

            Assert.IsFalse(Validator.IsUserInExperiment(Config, Config.GetExperimentFromKey("test_experiment"), null));
        }

        [TestMethod]
        public void TestUserInExperimentAudienceMatch()
        {
            var userAttributes = new UserAttributes
            {
                {"device_type", "iPhone" },
                {"location", "San Francisco" }
            };
            Assert.IsTrue(Validator.IsUserInExperiment(Config, Config.GetExperimentFromKey("test_experiment"), userAttributes));
        }

        [TestMethod]
        public void TestIsUserInExperimentAudienceNoMatch()
        {
            var userAttributes = new UserAttributes
            {
                {"device_type", "Android" },
                {"location", "San Francisco" }
            };

            Assert.IsFalse(Validator.IsUserInExperiment(Config, Config.GetExperimentFromKey("test_experiment"), null));
        }

        [TestMethod]
        public void TestAreEventTagsValidValidEventTags()
        {
            Assert.IsTrue(Validator.AreEventTagsValid(new System.Collections.Generic.Dictionary<string, object>()));
            Assert.IsTrue(Validator.AreEventTagsValid(new System.Collections.Generic.Dictionary<string, object>
            {
                {"location", "San Francisco" },
                {"browser", "Firefox" },
                {"revenue", 0 }
            }));
        }

        public void TestAreEventTagsValidInvalidTags()
        {
            // Some of the tests cases are not applicable because C# is strongly typed.

            Assert.IsFalse(Validator.AreEventTagsValid(new System.Collections.Generic.Dictionary<string, object>
            {
                {"43",  "23"}
            }));
        }

    }
}