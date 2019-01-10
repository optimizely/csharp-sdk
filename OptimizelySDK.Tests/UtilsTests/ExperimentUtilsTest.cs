/* 
 * Copyright 2019, Optimizely
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

using NUnit.Framework;
using OptimizelySDK.Entity;
using OptimizelySDK.Utils;

namespace OptimizelySDK.Tests.UtilsTests
{
    [TestFixture]
    public class ExperimentUtilsTest
    {
        private ProjectConfig Config;

        [SetUp]
        public void Setup()
        {
            Config = ProjectConfig.Create(TestData.TypedAudienceDatafile, null, null);
        }
        
        #region IsUserInExperiment Tests

        [Test]
        public void TestIsUserInExperimentReturnsTrueWithNoAudience()
        {
            var experiment = Config.GetExperimentFromKey("feat_with_var_test");
            experiment.AudienceIds = new string[] { };
            experiment.AudienceConditions = null;

            Assert.True(ExperimentUtils.IsUserInExperiment(Config, experiment, null));
        }

        [Test]
        public void TestIsUserInExperimentReturnsTrueWhenAudienceUsedInExperimentNoAttributesProvided()
        {
            var experiment = Config.GetExperimentFromKey("feat_with_var_test");
            experiment.AudienceIds = new string[] { "3468206648" };

            Assert.True(ExperimentUtils.IsUserInExperiment(Config, experiment, null));
            Assert.True(ExperimentUtils.IsUserInExperiment(Config, experiment, new UserAttributes { }));
        }

        [Test]
        public void TestIsUserInExperimentReturnsFalseIfNoAudienceInORConditionPass()
        {
            var experiment = Config.GetExperimentFromKey("feat_with_var_test");
            var userAttributes = new UserAttributes
            {
                { "house", "Ravenclaw" },
                { "lasers", 50 },
                { "should_do_it", false }
            };

            Assert.False(ExperimentUtils.IsUserInExperiment(Config, experiment, userAttributes));
        }

        [Test]
        public void TestIsUserInExperimentReturnsTrueIfAnyAudienceInORConditionPass()
        {
            var experiment = Config.GetExperimentFromKey("feat_with_var_test");
            var userAttributes = new UserAttributes
            {
                { "house", "Gryffindor" },
                { "lasers", 50 },
                { "should_do_it", false }
            };

            Assert.True(ExperimentUtils.IsUserInExperiment(Config, experiment, userAttributes));
        }

        [Test]
        public void TestIsUserInExperimentReturnsTrueIfAllAudiencesInANDConditionPass()
        {
            var experiment = Config.GetExperimentFromKey("audience_combinations_experiment");
            var userAttributes = new UserAttributes
            {
                { "house", "Gryffindor" },
                { "favorite_ice_cream", "walls" }
            };

            Assert.True(ExperimentUtils.IsUserInExperiment(Config, experiment, userAttributes));
        }

        [Test]
        public void TestIsUserInExperimentReturnsFalseIfAnyAudienceInANDConditionDoesNotPass()
        {
            var experiment = Config.GetExperimentFromKey("audience_combinations_experiment");
            var userAttributes = new UserAttributes
            {
                { "house", "Gryffindor" },
                { "lasers", 50 }
            };

            Assert.False(ExperimentUtils.IsUserInExperiment(Config, experiment, userAttributes));
        }

        [Test]
        public void TestIsUserInExperimentReturnsTrueIfAudienceInNOTConditionDoesNotPass()
        {
            var experiment = Config.GetExperimentFromKey("feat_with_var_test");
            experiment.AudienceIds = new string[] { "3468206645" };
            var userAttributes = new UserAttributes
            {
                { "browser_type", "Safari" }
            };

            Assert.True(ExperimentUtils.IsUserInExperiment(Config, experiment, userAttributes));
        }

        [Test]
        public void TestIsUserInExperimentReturnsFalseIfAudienceInNOTConditionGetsPassed()
        {
            var experiment = Config.GetExperimentFromKey("feat_with_var_test");
            experiment.AudienceIds = new string[] { "3468206645" };
            var userAttributes = new UserAttributes
            {
                { "browser_type", "Chrome" }
            };

            Assert.False(ExperimentUtils.IsUserInExperiment(Config, experiment, userAttributes));
        }

        #endregion // IsUserInExperiment Tests
    }
}
