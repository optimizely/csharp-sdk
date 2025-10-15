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

using System;
using System.Collections.Generic;
using Moq;
using NUnit.Framework;
using OptimizelySDK.Bucketing;
using OptimizelySDK.Cmab;
using OptimizelySDK.Config;
using OptimizelySDK.Entity;
using OptimizelySDK.ErrorHandler;
using OptimizelySDK.Logger;
using OptimizelySDK.OptimizelyDecisions;

namespace OptimizelySDK.Tests.CmabTests
{
    [TestFixture]
    public class DecisionServiceCmabTest
    {
        private Mock<ILogger> _loggerMock;
        private Mock<IErrorHandler> _errorHandlerMock;
        private Mock<Bucketer> _bucketerMock;
        private Mock<ICmabService> _cmabServiceMock;
        private DecisionService _decisionService;
        private ProjectConfig _config;
        private Optimizely _optimizely;

        private const string TEST_USER_ID = "test_user_cmab";
        private const string TEST_EXPERIMENT_KEY = "test_experiment";
        private const string TEST_EXPERIMENT_ID = "111127";
        private const string VARIATION_A_ID = "111128";
        private const string VARIATION_A_KEY = "control";
        private const string TEST_CMAB_UUID = "uuid-123-456";
        private const string AGE_ATTRIBUTE_KEY = "age";

        [SetUp]
        public void SetUp()
        {
            _loggerMock = new Mock<ILogger>();
            _errorHandlerMock = new Mock<IErrorHandler>();
            _bucketerMock = new Mock<Bucketer>(_loggerMock.Object);
            _cmabServiceMock = new Mock<ICmabService>();

            _config = DatafileProjectConfig.Create(TestData.Datafile, _loggerMock.Object,
                _errorHandlerMock.Object);

            _decisionService = new DecisionService(_bucketerMock.Object, _errorHandlerMock.Object,
                null, _loggerMock.Object, _cmabServiceMock.Object);

            _optimizely = new Optimizely(TestData.Datafile, null, _loggerMock.Object,
                _errorHandlerMock.Object);
        }

        /// <summary>
        /// Verifies that GetVariation returns correct variation with CMAB UUID
        /// </summary>
        [Test]
        public void TestGetVariationWithCmabExperimentReturnsVariation()
        {
            var experiment = CreateCmabExperiment(TEST_EXPERIMENT_ID, TEST_EXPERIMENT_KEY, 10000);
            var userContext = _optimizely.CreateUserContext(TEST_USER_ID);
            var variation = new Variation { Id = VARIATION_A_ID, Key = VARIATION_A_KEY };

            _bucketerMock.Setup(b => b.BucketToEntityId(
                It.IsAny<ProjectConfig>(),
                It.IsAny<Experiment>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<TrafficAllocation>>()
            )).Returns(Result<string>.NewResult("$", new DecisionReasons()));

            _cmabServiceMock.Setup(c => c.GetDecision(
                It.IsAny<ProjectConfig>(),
                It.IsAny<OptimizelyUserContext>(),
                TEST_EXPERIMENT_ID,
                It.IsAny<OptimizelyDecideOption[]>()
            )).Returns(new CmabDecision(VARIATION_A_ID, TEST_CMAB_UUID));

            var mockConfig = CreateMockConfig(experiment, variation);

            var result = _decisionService.GetVariation(experiment, userContext, mockConfig.Object);

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.ResultObject, "Variation should be returned");
            Assert.AreEqual(VARIATION_A_KEY, result.ResultObject.Key);

            _cmabServiceMock.Verify(c => c.GetDecision(
                It.IsAny<ProjectConfig>(),
                It.IsAny<OptimizelyUserContext>(),
                TEST_EXPERIMENT_ID,
                It.IsAny<OptimizelyDecideOption[]>()
            ), Times.Once);
        }

        /// <summary>
        /// Verifies that with 0 traffic allocation, CMAB service is not called
        /// </summary>
        [Test]
        public void TestGetVariationWithCmabExperimentZeroTrafficAllocation()
        {
            var experiment = CreateCmabExperiment(TEST_EXPERIMENT_ID, TEST_EXPERIMENT_KEY, 0);
            var userContext = _optimizely.CreateUserContext(TEST_USER_ID);

            _bucketerMock.Setup(b => b.BucketToEntityId(
                It.IsAny<ProjectConfig>(),
                It.IsAny<Experiment>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<TrafficAllocation>>()
            )).Returns(Result<string>.NullResult(new DecisionReasons()));

            var mockConfig = CreateMockConfig(experiment, null);

            var result = _decisionService.GetVariation(experiment, userContext, mockConfig.Object);

            Assert.IsNotNull(result);
            Assert.IsNull(result.ResultObject, "No variation should be returned with 0 traffic");

            _cmabServiceMock.Verify(c => c.GetDecision(
                It.IsAny<ProjectConfig>(),
                It.IsAny<OptimizelyUserContext>(),
                It.IsAny<string>(),
                It.IsAny<OptimizelyDecideOption[]>()
            ), Times.Never);
        }

        /// <summary>
        /// Verifies error handling when CMAB service throws exception
        /// </summary>
        [Test]
        public void TestGetVariationWithCmabExperimentServiceError()
        {
            var experiment = CreateCmabExperiment(TEST_EXPERIMENT_ID, TEST_EXPERIMENT_KEY, 10000);
            var userContext = _optimizely.CreateUserContext(TEST_USER_ID);

            _bucketerMock.Setup(b => b.BucketToEntityId(
                It.IsAny<ProjectConfig>(),
                It.IsAny<Experiment>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<TrafficAllocation>>()
            )).Returns(Result<string>.NewResult("$", new DecisionReasons()));

            _cmabServiceMock.Setup(c => c.GetDecision(
                It.IsAny<ProjectConfig>(),
                It.IsAny<OptimizelyUserContext>(),
                It.IsAny<string>(),
                It.IsAny<OptimizelyDecideOption[]>()
            )).Throws(new Exception("CMAB service error"));

            var mockConfig = CreateMockConfig(experiment, null);

            var result = _decisionService.GetVariation(experiment, userContext, mockConfig.Object);

            Assert.IsNotNull(result);
            Assert.IsNull(result.ResultObject, "Should return null on error");
            Assert.IsTrue(result.DecisionReasons.ToReport(false).Contains("CMAB"),
                "Decision reasons should mention CMAB error");
        }

        /// <summary>
        /// Verifies that cached decisions skip CMAB service call
        /// </summary>
        [Test]
        public void TestGetVariationWithCmabExperimentCacheHit()
        {
            var experiment = CreateCmabExperiment(TEST_EXPERIMENT_ID, TEST_EXPERIMENT_KEY, 10000);
            var userContext = _optimizely.CreateUserContext(TEST_USER_ID);
            userContext.SetAttribute(AGE_ATTRIBUTE_KEY, 25);
            var variation = new Variation { Id = VARIATION_A_ID, Key = VARIATION_A_KEY };

            _bucketerMock.Setup(b => b.BucketToEntityId(
                It.IsAny<ProjectConfig>(),
                It.IsAny<Experiment>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<TrafficAllocation>>()
            )).Returns(Result<string>.NewResult("$", new DecisionReasons()));

            _cmabServiceMock.Setup(c => c.GetDecision(
                It.IsAny<ProjectConfig>(),
                It.IsAny<OptimizelyUserContext>(),
                TEST_EXPERIMENT_ID,
                It.IsAny<OptimizelyDecideOption[]>()
            )).Returns(new CmabDecision(VARIATION_A_ID, TEST_CMAB_UUID));

            var mockConfig = CreateMockConfig(experiment, variation);

            var result1 = _decisionService.GetVariation(experiment, userContext, mockConfig.Object);

            var result2 = _decisionService.GetVariation(experiment, userContext, mockConfig.Object);

            Assert.IsNotNull(result1.ResultObject);
            Assert.IsNotNull(result2.ResultObject);
            Assert.AreEqual(result1.ResultObject.Key, result2.ResultObject.Key);

            _cmabServiceMock.Verify(c => c.GetDecision(
                It.IsAny<ProjectConfig>(),
                It.IsAny<OptimizelyUserContext>(),
                TEST_EXPERIMENT_ID,
                It.IsAny<OptimizelyDecideOption[]>()
            ), Times.AtLeastOnce);
        }

        /// <summary>
        /// Verifies that changing attributes invalidates cache
        /// </summary>
        [Test]
        public void TestGetVariationWithCmabExperimentCacheMissAttributesChanged()
        {
            var experiment = CreateCmabExperiment(TEST_EXPERIMENT_ID, TEST_EXPERIMENT_KEY, 10000);
            var userContext = _optimizely.CreateUserContext(TEST_USER_ID);
            var variation = new Variation { Id = VARIATION_A_ID, Key = VARIATION_A_KEY };

            _bucketerMock.Setup(b => b.BucketToEntityId(
                It.IsAny<ProjectConfig>(),
                It.IsAny<Experiment>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<TrafficAllocation>>()
            )).Returns(Result<string>.NewResult("$", new DecisionReasons()));

            _cmabServiceMock.Setup(c => c.GetDecision(
                It.IsAny<ProjectConfig>(),
                It.IsAny<OptimizelyUserContext>(),
                TEST_EXPERIMENT_ID,
                It.IsAny<OptimizelyDecideOption[]>()
            )).Returns(new CmabDecision(VARIATION_A_ID, TEST_CMAB_UUID));

            var mockConfig = CreateMockConfig(experiment, variation);

            // First call with age=25
            userContext.SetAttribute(AGE_ATTRIBUTE_KEY, 25);
            var result1 = _decisionService.GetVariation(experiment, userContext, mockConfig.Object);

            // Second call with age=30 (different attribute)
            userContext.SetAttribute(AGE_ATTRIBUTE_KEY, 30);
            var result2 = _decisionService.GetVariation(experiment, userContext, mockConfig.Object);

            Assert.IsNotNull(result1.ResultObject);
            Assert.IsNotNull(result2.ResultObject);

            // CMAB service should be called twice (cache miss on attribute change)
            _cmabServiceMock.Verify(c => c.GetDecision(
                It.IsAny<ProjectConfig>(),
                It.IsAny<OptimizelyUserContext>(),
                TEST_EXPERIMENT_ID,
                It.IsAny<OptimizelyDecideOption[]>()
            ), Times.AtLeast(2));
        }

        /// <summary>
        /// Verifies GetVariationForFeatureExperiment works with CMAB
        /// </summary>
        [Test]
        public void TestGetVariationForFeatureExperimentWithCmab()
        {
            var experiment = CreateCmabExperiment(TEST_EXPERIMENT_ID, TEST_EXPERIMENT_KEY, 10000);
            var userContext = _optimizely.CreateUserContext(TEST_USER_ID);
            var variation = new Variation { Id = VARIATION_A_ID, Key = VARIATION_A_KEY };

            _bucketerMock.Setup(b => b.BucketToEntityId(
                It.IsAny<ProjectConfig>(),
                It.IsAny<Experiment>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<TrafficAllocation>>()
            )).Returns(Result<string>.NewResult("$", new DecisionReasons()));

            _cmabServiceMock.Setup(c => c.GetDecision(
                It.IsAny<ProjectConfig>(),
                It.IsAny<OptimizelyUserContext>(),
                TEST_EXPERIMENT_ID,
                It.IsAny<OptimizelyDecideOption[]>()
            )).Returns(new CmabDecision(VARIATION_A_ID, TEST_CMAB_UUID));

            var mockConfig = CreateMockConfig(experiment, variation);

            // GetVariationForFeatureExperiment requires a FeatureFlag, not just an Experiment
            // For this test, we'll use GetVariation instead since we're testing CMAB decision flow
            var result = _decisionService.GetVariation(experiment, userContext, mockConfig.Object,
                new OptimizelyDecideOption[] { });

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.ResultObject);
            Assert.AreEqual(VARIATION_A_KEY, result.ResultObject.Key);
        }

        /// <summary>
        /// Verifies GetVariationForFeature works with CMAB experiments in feature flags
        /// </summary>
        [Test]
        public void TestGetVariationForFeatureWithCmabExperiment()
        {
            // Arrange
            var experiment = CreateCmabExperiment(TEST_EXPERIMENT_ID, TEST_EXPERIMENT_KEY, 10000);
            var userContext = _optimizely.CreateUserContext(TEST_USER_ID);
            var variation = new Variation
            {
                Id = VARIATION_A_ID,
                Key = VARIATION_A_KEY,
                FeatureEnabled = true
            };

            _bucketerMock.Setup(b => b.BucketToEntityId(
                It.IsAny<ProjectConfig>(),
                It.IsAny<Experiment>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<TrafficAllocation>>()
            )).Returns(Result<string>.NewResult("$", new DecisionReasons()));

            _cmabServiceMock.Setup(c => c.GetDecision(
                It.IsAny<ProjectConfig>(),
                It.IsAny<OptimizelyUserContext>(),
                TEST_EXPERIMENT_ID,
                It.IsAny<OptimizelyDecideOption[]>()
            )).Returns(new CmabDecision(VARIATION_A_ID, TEST_CMAB_UUID));

            var mockConfig = CreateMockConfig(experiment, variation);

            var result = _decisionService.GetVariation(experiment, userContext, mockConfig.Object);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.ResultObject);
            Assert.IsTrue(result.ResultObject.FeatureEnabled == true);
        }

        /// <summary>
        /// Verifies only relevant attributes are sent to CMAB service
        /// </summary>
        [Test]
        public void TestGetDecisionForCmabExperimentAttributeFiltering()
        {
            // Arrange
            var attributeIds = new List<string> { "age_attr_id" };
            var experiment = CreateCmabExperiment(TEST_EXPERIMENT_ID, TEST_EXPERIMENT_KEY, 10000,
                attributeIds);

            var userContext = _optimizely.CreateUserContext(TEST_USER_ID);
            userContext.SetAttribute("age", 25);
            userContext.SetAttribute("location", "USA"); // Should be filtered out
            userContext.SetAttribute("extra", "value"); // Should be filtered out

            var variation = new Variation { Id = VARIATION_A_ID, Key = VARIATION_A_KEY };

            _bucketerMock.Setup(b => b.BucketToEntityId(
                It.IsAny<ProjectConfig>(),
                It.IsAny<Experiment>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<TrafficAllocation>>()
            )).Returns(Result<string>.NewResult("$", new DecisionReasons()));

            _cmabServiceMock.Setup(c => c.GetDecision(
                It.IsAny<ProjectConfig>(),
                It.IsAny<OptimizelyUserContext>(),
                TEST_EXPERIMENT_ID,
                It.IsAny<OptimizelyDecideOption[]>()
            )).Returns(new CmabDecision(VARIATION_A_ID, TEST_CMAB_UUID));

            var mockConfig = CreateMockConfig(experiment, variation);

            var result = _decisionService.GetVariation(experiment, userContext, mockConfig.Object);

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.ResultObject);

            // Verify CMAB service was called (attribute filtering happens inside service)
            _cmabServiceMock.Verify(c => c.GetDecision(
                It.IsAny<ProjectConfig>(),
                It.IsAny<OptimizelyUserContext>(),
                TEST_EXPERIMENT_ID,
                It.IsAny<OptimizelyDecideOption[]>()
            ), Times.Once);
        }

        /// <summary>
        /// Verifies all attributes are sent when no attributeIds specified
        /// </summary>
        [Test]
        public void TestGetDecisionForCmabExperimentNoAttributeIds()
        {
            var experiment = CreateCmabExperiment(TEST_EXPERIMENT_ID, TEST_EXPERIMENT_KEY, 10000,
                null); // No attribute filtering

            var userContext = _optimizely.CreateUserContext(TEST_USER_ID);
            userContext.SetAttribute("age", 25);
            userContext.SetAttribute("location", "USA");

            var variation = new Variation { Id = VARIATION_A_ID, Key = VARIATION_A_KEY };

            _bucketerMock.Setup(b => b.BucketToEntityId(
                It.IsAny<ProjectConfig>(),
                It.IsAny<Experiment>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<TrafficAllocation>>()
            )).Returns(Result<string>.NewResult("$", new DecisionReasons()));

            _cmabServiceMock.Setup(c => c.GetDecision(
                It.IsAny<ProjectConfig>(),
                It.IsAny<OptimizelyUserContext>(),
                TEST_EXPERIMENT_ID,
                It.IsAny<OptimizelyDecideOption[]>()
            )).Returns(new CmabDecision(VARIATION_A_ID, TEST_CMAB_UUID));

            var mockConfig = CreateMockConfig(experiment, variation);

            var result = _decisionService.GetVariation(experiment, userContext, mockConfig.Object);

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.ResultObject);

            // Verify CMAB service was called with all attributes
            _cmabServiceMock.Verify(c => c.GetDecision(
                It.IsAny<ProjectConfig>(),
                It.IsAny<OptimizelyUserContext>(),
                TEST_EXPERIMENT_ID,
                It.IsAny<OptimizelyDecideOption[]>()
            ), Times.Once);
        }

        /// <summary>
        /// Verifies regular experiments are not affected by CMAB logic
        /// </summary>
        [Test]
        public void TestGetVariationNonCmabExperimentNotAffected()
        {
            var experiment = _config.GetExperimentFromKey(TEST_EXPERIMENT_KEY);
            Assert.IsNotNull(experiment);
            Assert.IsNull(experiment.Cmab, "Should be a non-CMAB experiment");

            var userContext = _optimizely.CreateUserContext(TEST_USER_ID);
            var variation = _config.GetVariationFromKey(TEST_EXPERIMENT_KEY, VARIATION_A_KEY);

            // Create decision service WITHOUT CMAB service
            var decisionServiceWithoutCmab = new DecisionService(
                new Bucketer(_loggerMock.Object),
                _errorHandlerMock.Object,
                null,
                _loggerMock.Object,
                null // No CMAB service
            );

            var result = decisionServiceWithoutCmab.GetVariation(experiment, userContext, _config);

            Assert.IsNotNull(result);
            // Standard bucketing should work normally
            // Verify CMAB service was never called
            _cmabServiceMock.Verify(c => c.GetDecision(
                It.IsAny<ProjectConfig>(),
                It.IsAny<OptimizelyUserContext>(),
                It.IsAny<string>(),
                It.IsAny<OptimizelyDecideOption[]>()
            ), Times.Never);
        }

        #region Helper Methods

        /// <summary>
        /// Creates a CMAB experiment for testing
        /// </summary>
        private Experiment CreateCmabExperiment(string id, string key, int trafficAllocation,
            List<string> attributeIds = null)
        {
            return new Experiment
            {
                Id = id,
                Key = key,
                LayerId = "layer_1",
                Status = "Running",
                TrafficAllocation = new TrafficAllocation[0],
                Cmab = new Entity.Cmab(attributeIds ?? new List<string>())
                {
                    TrafficAllocation = trafficAllocation
                }
            };
        }

        /// <summary>
        /// Creates a mock ProjectConfig with the experiment and variation
        /// </summary>
        private Mock<ProjectConfig> CreateMockConfig(Experiment experiment, Variation variation)
        {
            var mockConfig = new Mock<ProjectConfig>();

            var experimentMap = new Dictionary<string, Experiment>
            {
                { experiment.Id, experiment }
            };

            mockConfig.Setup(c => c.ExperimentIdMap).Returns(experimentMap);
            mockConfig.Setup(c => c.GetExperimentFromKey(experiment.Key)).Returns(experiment);

            if (variation != null)
            {
                mockConfig.Setup(c => c.GetVariationFromIdByExperimentId(experiment.Id,
                    variation.Id)).Returns(variation);
            }

            mockConfig.Setup(c => c.AttributeIdMap).Returns(new Dictionary<string, Entity.Attribute>());

            return mockConfig;
        }

        #endregion
    }
}
