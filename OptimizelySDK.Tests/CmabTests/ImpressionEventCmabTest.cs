/* 
* Copyright 2025, Optimizely
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

using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using OptimizelySDK.Config;
using OptimizelySDK.Entity;
using OptimizelySDK.ErrorHandler;
using OptimizelySDK.Event;
using OptimizelySDK.Event.Entity;
using OptimizelySDK.Logger;

namespace OptimizelySDK.Tests.CmabTests
{
    [TestFixture]
    public class ImpressionEventCmabTest
    {
        private Mock<ILogger> _loggerMock;
        private Mock<IErrorHandler> _errorHandlerMock;
        private ProjectConfig _config;

        private const string TEST_USER_ID = "test_user";
        private const string TEST_CMAB_UUID = "cmab-uuid-12345";
        private const string TEST_EXPERIMENT_KEY = "test_experiment";
        private const string TEST_VARIATION_ID = "77210100090";

        [SetUp]
        public void SetUp()
        {
            _loggerMock = new Mock<ILogger>();
            _errorHandlerMock = new Mock<IErrorHandler>();

            _config = DatafileProjectConfig.Create(TestData.Datafile, _loggerMock.Object,
                _errorHandlerMock.Object);
        }

        /// <summary>
        /// Verifies that CreateImpressionEvent includes CMAB UUID in metadata
        /// </summary>
        [Test]
        public void TestCreateImpressionEventWithCmabUuid()
        {
            var experiment = _config.GetExperimentFromKey(TEST_EXPERIMENT_KEY);
            var variation = _config.GetVariationFromId(experiment.Key, TEST_VARIATION_ID);

            var impressionEvent = UserEventFactory.CreateImpressionEvent(
                _config,
                experiment,
                variation,
                TEST_USER_ID,
                null,
                TEST_EXPERIMENT_KEY,
                "experiment",
                true,
                TEST_CMAB_UUID);

            Assert.IsNotNull(impressionEvent);
            Assert.IsNotNull(impressionEvent.Metadata);
            Assert.AreEqual(TEST_CMAB_UUID, impressionEvent.Metadata.CmabUuid);
            Assert.AreEqual(experiment, impressionEvent.Experiment);
            Assert.AreEqual(variation, impressionEvent.Variation);
            Assert.AreEqual(TEST_USER_ID, impressionEvent.UserId);
        }

        /// <summary>
        /// Verifies that CreateImpressionEvent without CMAB UUID has null cmab_uuid
        /// </summary>
        [Test]
        public void TestCreateImpressionEventWithoutCmabUuid()
        {
            var experiment = _config.GetExperimentFromKey(TEST_EXPERIMENT_KEY);
            var variation = _config.GetVariationFromId(experiment.Key, TEST_VARIATION_ID);

            var impressionEvent = UserEventFactory.CreateImpressionEvent(
                _config,
                experiment,
                variation,
                TEST_USER_ID,
                null,
                TEST_EXPERIMENT_KEY,
                "experiment");

            Assert.IsNotNull(impressionEvent);
            Assert.IsNotNull(impressionEvent.Metadata);
            Assert.IsNull(impressionEvent.Metadata.CmabUuid);
            Assert.AreEqual(experiment, impressionEvent.Experiment);
            Assert.AreEqual(variation, impressionEvent.Variation);
        }

        /// <summary>
        /// Verifies that EventFactory includes cmab_uuid in the log event JSON
        /// </summary>
        [Test]
        public void TestEventFactoryCreateLogEventWithCmabUuid()
        {
            // Arrange
            var experiment = _config.GetExperimentFromKey(TEST_EXPERIMENT_KEY);
            var variation = _config.GetVariationFromId(experiment.Key, TEST_VARIATION_ID);

            var impressionEvent = UserEventFactory.CreateImpressionEvent(
                _config,
                experiment,
                variation,
                TEST_USER_ID,
                null,
                TEST_EXPERIMENT_KEY,
                "experiment",
                true,
                TEST_CMAB_UUID);

            var logEvent = EventFactory.CreateLogEvent(new UserEvent[] { impressionEvent }, _loggerMock.Object);

            Assert.IsNotNull(logEvent);

            var params_dict = logEvent.Params;

            Assert.IsNotNull(params_dict);
            Assert.IsTrue(params_dict.ContainsKey("visitors"));

            var visitors = (JArray)params_dict["visitors"];

            Assert.IsNotNull(visitors);
            Assert.AreEqual(1, visitors.Count);

            var visitor = visitors[0] as JObject;
            var snapshots = visitor["snapshots"] as JArray;

            Assert.IsNotNull(snapshots);
            Assert.Greater(snapshots.Count, 0);

            var snapshot = snapshots[0] as JObject;
            var decisions = snapshot["decisions"] as JArray;

            Assert.IsNotNull(decisions);
            Assert.Greater(decisions.Count, 0);

            var decision = decisions[0] as JObject;
            var metadata = decision["metadata"] as JObject;

            Assert.IsNotNull(metadata);

            Assert.IsTrue(metadata.ContainsKey("cmab_uuid"));
            Assert.AreEqual(TEST_CMAB_UUID, metadata["cmab_uuid"].ToString());
        }

        /// <summary>
        /// Verifies that EventFactory does not include cmab_uuid when not provided
        /// </summary>
        [Test]
        public void TestEventFactoryCreateLogEventWithoutCmabUuid()
        {
            var experiment = _config.GetExperimentFromKey(TEST_EXPERIMENT_KEY);
            var variation = _config.GetVariationFromId(experiment.Key, TEST_VARIATION_ID);

            var impressionEvent = UserEventFactory.CreateImpressionEvent(
                _config,
                experiment,
                variation,
                TEST_USER_ID,
                null,
                TEST_EXPERIMENT_KEY,
                "experiment");

            var logEvent = EventFactory.CreateLogEvent(new UserEvent[] { impressionEvent }, _loggerMock.Object);

            Assert.IsNotNull(logEvent);

            var params_dict = logEvent.Params;

            Assert.IsNotNull(params_dict);
            Assert.IsTrue(params_dict.ContainsKey("visitors"));

            var visitors = (JArray)params_dict["visitors"];

            Assert.IsNotNull(visitors);
            Assert.AreEqual(1, visitors.Count);

            var visitor = visitors[0] as JObject;
            var snapshots = visitor["snapshots"] as JArray;

            Assert.IsNotNull(snapshots);
            Assert.Greater(snapshots.Count, 0);

            var snapshot = snapshots[0] as JObject;
            var decisions = snapshot["decisions"] as JArray;

            Assert.IsNotNull(decisions);
            Assert.Greater(decisions.Count, 0);

            var decision = decisions[0] as JObject;
            var metadata = decision["metadata"] as JObject;

            Assert.IsNotNull(metadata);

			// Todo: If in test code is not acceptable
            // Verify cmab_uuid is either not present or is null
            if (metadata.ContainsKey("cmab_uuid"))
            {
                Assert.IsTrue(metadata["cmab_uuid"].Type == JTokenType.Null);
            }
        }
    }
}
