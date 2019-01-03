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

namespace OptimizelySDK.Tests.EntityTests
{
    [TestFixture]
    public class ExperimentTest
    {
        private ProjectConfig Config;

        [TestFixtureSetUp]
        public void Setup()
        {
            Config = ProjectConfig.Create(TestData.TypedAudienceDatafile, null, null);
        }

        [TestFixtureTearDown]
        public void Cleanup()
        {
            Config = null;
        }

        #region GetAudienceConditionsOrIds Tests

        [Test]
        public void TestGetAudienceConditionsOrIdsRetrieveAudienceIdsIfConditionsNotExist()
        {
            var experiment = Config.GetExperimentFromKey("feat_with_var_test");
            Assert.True(TestData.CompareObjects(experiment.AudienceIds, experiment.GetAudienceConditionsOrIds()));
        }

        [Test]
        public void TestGetAudienceConditionsOrIdsRetrieveAudienceConditionsIfExist()
        {
            var experiment = Config.GetExperimentFromKey("audience_combinations_experiment");
            Assert.True(TestData.CompareObjects(experiment.AudienceConditionsList, experiment.GetAudienceConditionsOrIds()));

            // Verify that Audience conditions are returned in case of empty array.
            experiment = new Entity.Experiment();
            experiment.AudienceConditions = Newtonsoft.Json.Linq.JToken.FromObject(new object[] { });
            Assert.True(TestData.CompareObjects(experiment.AudienceConditionsList, experiment.GetAudienceConditionsOrIds()));
        }

        #endregion // GetAudienceConditionsOrIds Tests
    }
}
