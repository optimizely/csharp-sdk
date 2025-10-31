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
using OptimizelySDK.Odp;
using AttributeEntity = OptimizelySDK.Entity.Attribute;

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
                It.IsAny<ExperimentCore>(),
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
            Assert.IsNotNull(result.ResultObject, "VariationDecisionResult should be returned");
            Assert.IsNotNull(result.ResultObject.Variation, "Variation should be returned");
            Assert.AreEqual(VARIATION_A_KEY, result.ResultObject.Variation.Key);
            Assert.AreEqual(VARIATION_A_ID, result.ResultObject.Variation.Id);
            Assert.AreEqual(TEST_CMAB_UUID, result.ResultObject.CmabUuid);

            var reasons = result.DecisionReasons.ToReport(true);
            var expectedMessage =
                $"CMAB decision fetched for user [{TEST_USER_ID}] in experiment [{TEST_EXPERIMENT_KEY}].";
            Assert.Contains(expectedMessage, reasons);

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
                It.IsAny<ExperimentCore>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<TrafficAllocation>>()
            )).Returns(Result<string>.NullResult(new DecisionReasons()));

            var mockConfig = CreateMockConfig(experiment, null);

            var result = _decisionService.GetVariation(experiment, userContext, mockConfig.Object);

            Assert.IsNotNull(result);
            Assert.IsNull(result.ResultObject, "No variation should be returned with 0 traffic");
            Assert.IsNull(result.ResultObject?.CmabUuid);

            var reasons = result.DecisionReasons.ToReport(true);
            var expectedMessage =
                $"User [{TEST_USER_ID}] not in CMAB experiment [{TEST_EXPERIMENT_KEY}] due to traffic allocation.";
            Assert.Contains(expectedMessage, reasons);

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
                It.IsAny<ExperimentCore>(),
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
            // CmabUuid is now in VariationDecisionResult, not DecisionReasons

            var reasonsList = result.DecisionReasons.ToReport(true);
            Assert.IsTrue(reasonsList.Exists(reason =>
                    reason.Contains(
                        $"Failed to fetch CMAB decision for experiment [{TEST_EXPERIMENT_KEY}].")),
                $"Decision reasons should include CMAB fetch failure. Actual reasons: {string.Join(", ", reasonsList)}");
            Assert.IsTrue(reasonsList.Exists(reason => reason.Contains("Error: CMAB service error")),
                $"Decision reasons should include CMAB service error text. Actual reasons: {string.Join(", ", reasonsList)}");

            _cmabServiceMock.Verify(c => c.GetDecision(
                It.IsAny<ProjectConfig>(),
                It.IsAny<OptimizelyUserContext>(),
                TEST_EXPERIMENT_ID,
                It.IsAny<OptimizelyDecideOption[]>()
            ), Times.Once);
        }

        /// <summary>
        /// Verifies behavior when CMAB service returns an unknown variation ID
        /// </summary>
        [Test]
        public void TestGetVariationWithCmabExperimentUnknownVariationId()
        {
            var experiment = CreateCmabExperiment(TEST_EXPERIMENT_ID, TEST_EXPERIMENT_KEY, 10000);
            var userContext = _optimizely.CreateUserContext(TEST_USER_ID);

            _bucketerMock.Setup(b => b.BucketToEntityId(
                It.IsAny<ProjectConfig>(),
                It.IsAny<ExperimentCore>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<TrafficAllocation>>()
            )).Returns(Result<string>.NewResult("$", new DecisionReasons()));

            const string unknownVariationId = "unknown_var";
            _cmabServiceMock.Setup(c => c.GetDecision(
                It.IsAny<ProjectConfig>(),
                It.IsAny<OptimizelyUserContext>(),
                TEST_EXPERIMENT_ID,
                It.IsAny<OptimizelyDecideOption[]>()
            )).Returns(new CmabDecision(unknownVariationId, TEST_CMAB_UUID));

            var mockConfig = CreateMockConfig(experiment, null);

            var result = _decisionService.GetVariation(experiment, userContext, mockConfig.Object);

            Assert.IsNotNull(result);
            Assert.IsNull(result.ResultObject);
            // CmabUuid is now in VariationDecisionResult, not DecisionReasons

            var reasons = result.DecisionReasons.ToReport(true);
            var expectedMessage =
                $"User [{TEST_USER_ID}] bucketed into invalid variation [{unknownVariationId}] for CMAB experiment [{TEST_EXPERIMENT_KEY}].";
            Assert.Contains(expectedMessage, reasons);

            _cmabServiceMock.Verify(c => c.GetDecision(
                It.IsAny<ProjectConfig>(),
                It.IsAny<OptimizelyUserContext>(),
                TEST_EXPERIMENT_ID,
                It.IsAny<OptimizelyDecideOption[]>()
            ), Times.Once);
        }

        /// <summary>
        /// Verifies that cached decisions skip CMAB service call
        /// </summary>
        [Test]
        public void TestGetVariationWithCmabExperimentCacheHit()
        {
            var attributeIds = new List<string> { "age_attr_id" };
            var experiment = CreateCmabExperiment(TEST_EXPERIMENT_ID, TEST_EXPERIMENT_KEY, 10000,
                attributeIds);
            var variation = new Variation { Id = VARIATION_A_ID, Key = VARIATION_A_KEY };

            _bucketerMock.Setup(b => b.BucketToEntityId(
                It.IsAny<ProjectConfig>(),
                It.IsAny<ExperimentCore>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<TrafficAllocation>>()
            )).Returns(Result<string>.NewResult("$", new DecisionReasons()));

            var attributeMap = new Dictionary<string, AttributeEntity>
            {
                { "age_attr_id", new AttributeEntity { Id = "age_attr_id", Key = AGE_ATTRIBUTE_KEY } }
            };
            var mockConfig = CreateMockConfig(experiment, variation, attributeMap);

            var cmabClientMock = new Mock<ICmabClient>(MockBehavior.Strict);
            cmabClientMock.Setup(c => c.FetchDecision(
                    TEST_EXPERIMENT_ID,
                    TEST_USER_ID,
                    It.Is<IDictionary<string, object>>(attrs =>
                        attrs.Count == 1 && attrs.ContainsKey(AGE_ATTRIBUTE_KEY) &&
                        (int)attrs[AGE_ATTRIBUTE_KEY] == 25),
                    It.IsAny<string>(),
                    It.IsAny<TimeSpan?>()))
                .Returns(VARIATION_A_ID);

            var cache = new LruCache<CmabCacheEntry>(maxSize: 10,
                itemTimeout: TimeSpan.FromMinutes(5),
                logger: new NoOpLogger());
            var cmabService = new DefaultCmabService(cache, cmabClientMock.Object, new NoOpLogger());
            var decisionService = new DecisionService(_bucketerMock.Object, _errorHandlerMock.Object,
                null, _loggerMock.Object, cmabService);

            var userContext = _optimizely.CreateUserContext(TEST_USER_ID);
            userContext.SetAttribute(AGE_ATTRIBUTE_KEY, 25);

            var result1 = decisionService.GetVariation(experiment, userContext, mockConfig.Object);
            var result2 = decisionService.GetVariation(experiment, userContext, mockConfig.Object);

            Assert.IsNotNull(result1.ResultObject);
            Assert.IsNotNull(result2.ResultObject);
            Assert.AreEqual(result1.ResultObject.Variation.Key, result2.ResultObject.Variation.Key);
            Assert.IsNotNull(result1.ResultObject.CmabUuid);
            Assert.AreEqual(result1.ResultObject.CmabUuid, result2.ResultObject.CmabUuid);

            cmabClientMock.Verify(c => c.FetchDecision(
                    TEST_EXPERIMENT_ID,
                    TEST_USER_ID,
                    It.Is<IDictionary<string, object>>(attrs =>
                        attrs.Count == 1 && (int)attrs[AGE_ATTRIBUTE_KEY] == 25),
                    It.IsAny<string>(),
                    It.IsAny<TimeSpan?>()),
                Times.Once);

            var reasons = result2.DecisionReasons.ToReport(true);
            var expectedMessage =
                $"CMAB decision fetched for user [{TEST_USER_ID}] in experiment [{TEST_EXPERIMENT_KEY}].";
            Assert.Contains(expectedMessage, reasons);
        }

        /// <summary>
        /// Verifies that changing attributes invalidates cache
        /// </summary>
        [Test]
        public void TestGetVariationWithCmabExperimentCacheMissAttributesChanged()
        {
            var attributeIds = new List<string> { "age_attr_id" };
            var experiment = CreateCmabExperiment(TEST_EXPERIMENT_ID, TEST_EXPERIMENT_KEY, 10000,
                attributeIds);
            var variation = new Variation { Id = VARIATION_A_ID, Key = VARIATION_A_KEY };
            var attributeMap = new Dictionary<string, AttributeEntity>
            {
                { "age_attr_id", new AttributeEntity { Id = "age_attr_id", Key = AGE_ATTRIBUTE_KEY } }
            };
            var mockConfig = CreateMockConfig(experiment, variation, attributeMap);

            _bucketerMock.Setup(b => b.BucketToEntityId(
                It.IsAny<ProjectConfig>(),
                It.IsAny<ExperimentCore>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<TrafficAllocation>>()
            )).Returns(Result<string>.NewResult("$", new DecisionReasons()));

            var cmabClientMock = new Mock<ICmabClient>(MockBehavior.Strict);
            cmabClientMock.Setup(c => c.FetchDecision(
                    TEST_EXPERIMENT_ID,
                    TEST_USER_ID,
                    It.Is<IDictionary<string, object>>(attrs => attrs.ContainsKey(AGE_ATTRIBUTE_KEY)),
                    It.IsAny<string>(),
                    It.IsAny<TimeSpan?>()))
                .Returns(VARIATION_A_ID);

            var cache = new LruCache<CmabCacheEntry>(maxSize: 10,
                itemTimeout: TimeSpan.FromMinutes(5),
                logger: new NoOpLogger());
            var cmabService = new DefaultCmabService(cache, cmabClientMock.Object, new NoOpLogger());
            var decisionService = new DecisionService(_bucketerMock.Object, _errorHandlerMock.Object,
                null, _loggerMock.Object, cmabService);

            var userContext = _optimizely.CreateUserContext(TEST_USER_ID);

            userContext.SetAttribute(AGE_ATTRIBUTE_KEY, 25);
            var result1 = decisionService.GetVariation(experiment, userContext, mockConfig.Object);

            userContext.SetAttribute(AGE_ATTRIBUTE_KEY, 30);
            var result2 = decisionService.GetVariation(experiment, userContext, mockConfig.Object);

            Assert.IsNotNull(result1.ResultObject);
            Assert.IsNotNull(result2.ResultObject);
            Assert.IsNotNull(result1.ResultObject.CmabUuid);
            Assert.IsNotNull(result2.ResultObject.CmabUuid);
            Assert.AreNotEqual(result1.ResultObject.CmabUuid, result2.ResultObject.CmabUuid);

            cmabClientMock.Verify(c => c.FetchDecision(
                    TEST_EXPERIMENT_ID,
                    TEST_USER_ID,
                    It.Is<IDictionary<string, object>>(attrs =>
                        attrs.ContainsKey(AGE_ATTRIBUTE_KEY) && (int)attrs[AGE_ATTRIBUTE_KEY] == 25),
                    It.IsAny<string>(),
                    It.IsAny<TimeSpan?>()),
                Times.Once);
            cmabClientMock.Verify(c => c.FetchDecision(
                    TEST_EXPERIMENT_ID,
                    TEST_USER_ID,
                    It.Is<IDictionary<string, object>>(attrs =>
                        attrs.ContainsKey(AGE_ATTRIBUTE_KEY) && (int)attrs[AGE_ATTRIBUTE_KEY] == 30),
                    It.IsAny<string>(),
                    It.IsAny<TimeSpan?>()),
                Times.Once);
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
                It.IsAny<ExperimentCore>(),
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
            Assert.AreEqual(VARIATION_A_KEY, result.ResultObject.Variation.Key);
            Assert.AreEqual(TEST_CMAB_UUID, result.ResultObject.CmabUuid);
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
                It.IsAny<ExperimentCore>(),
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
            Assert.IsTrue(result.ResultObject.Variation.FeatureEnabled == true);
            Assert.AreEqual(TEST_CMAB_UUID, result.ResultObject.CmabUuid);
        }

        /// <summary>
        /// Verifies only relevant attributes are sent to CMAB service
        /// </summary>
        [Test]
        public void TestGetDecisionForCmabExperimentAttributeFiltering()
        {
            var attributeIds = new List<string> { "age_attr_id", "location_attr_id" };
            var experiment = CreateCmabExperiment(TEST_EXPERIMENT_ID, TEST_EXPERIMENT_KEY, 10000,
                attributeIds);
            var variation = new Variation { Id = VARIATION_A_ID, Key = VARIATION_A_KEY };
            var attributeMap = new Dictionary<string, AttributeEntity>
            {
                { "age_attr_id", new AttributeEntity { Id = "age_attr_id", Key = "age" } },
                { "location_attr_id", new AttributeEntity { Id = "location_attr_id", Key = "location" } }
            };
            var mockConfig = CreateMockConfig(experiment, variation, attributeMap);

            _bucketerMock.Setup(b => b.BucketToEntityId(
                It.IsAny<ProjectConfig>(),
                It.IsAny<ExperimentCore>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<TrafficAllocation>>()
            )).Returns(Result<string>.NewResult("$", new DecisionReasons()));

            var cmabClientMock = new Mock<ICmabClient>(MockBehavior.Strict);
            cmabClientMock.Setup(c => c.FetchDecision(
                    TEST_EXPERIMENT_ID,
                    TEST_USER_ID,
                    It.Is<IDictionary<string, object>>(attrs =>
                        attrs.Count == 2 && (int)attrs["age"] == 25 &&
                        (string)attrs["location"] == "USA" &&
                        !attrs.ContainsKey("extra")),
                    It.IsAny<string>(),
                    It.IsAny<TimeSpan?>()))
                .Returns(VARIATION_A_ID);

            var cache = new LruCache<CmabCacheEntry>(maxSize: 10,
                itemTimeout: TimeSpan.FromMinutes(5),
                logger: new NoOpLogger());
            var cmabService = new DefaultCmabService(cache, cmabClientMock.Object, new NoOpLogger());
            var decisionService = new DecisionService(_bucketerMock.Object, _errorHandlerMock.Object,
                null, _loggerMock.Object, cmabService);

            var userContext = _optimizely.CreateUserContext(TEST_USER_ID);
            userContext.SetAttribute("age", 25);
            userContext.SetAttribute("location", "USA");
            userContext.SetAttribute("extra", "value");

            var result = decisionService.GetVariation(experiment, userContext, mockConfig.Object);

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.ResultObject);
            Assert.IsNotNull(result.ResultObject.CmabUuid);

            cmabClientMock.VerifyAll();
        }

        /// <summary>
        ///     Verifies CMAB service receives an empty attribute payload when no CMAB attribute IDs are
        ///     configured
        /// </summary>
        [Test]
        public void TestGetDecisionForCmabExperimentNoAttributeIds()
        {
            var experiment = CreateCmabExperiment(TEST_EXPERIMENT_ID, TEST_EXPERIMENT_KEY, 10000,
                null);
            var variation = new Variation { Id = VARIATION_A_ID, Key = VARIATION_A_KEY };
            var mockConfig = CreateMockConfig(experiment, variation, new Dictionary<string, AttributeEntity>());

            _bucketerMock.Setup(b => b.BucketToEntityId(
                It.IsAny<ProjectConfig>(),
                It.IsAny<ExperimentCore>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<TrafficAllocation>>()
            )).Returns(Result<string>.NewResult("$", new DecisionReasons()));

            var cmabClientMock = new Mock<ICmabClient>(MockBehavior.Strict);
            cmabClientMock.Setup(c => c.FetchDecision(
                    TEST_EXPERIMENT_ID,
                    TEST_USER_ID,
                    It.Is<IDictionary<string, object>>(attrs => attrs.Count == 0),
                    It.IsAny<string>(),
                    It.IsAny<TimeSpan?>()))
                .Returns(VARIATION_A_ID);

            var cache = new LruCache<CmabCacheEntry>(maxSize: 10,
                itemTimeout: TimeSpan.FromMinutes(5),
                logger: new NoOpLogger());
            var cmabService = new DefaultCmabService(cache, cmabClientMock.Object, new NoOpLogger());
            var decisionService = new DecisionService(_bucketerMock.Object, _errorHandlerMock.Object,
                null, _loggerMock.Object, cmabService);

            var userContext = _optimizely.CreateUserContext(TEST_USER_ID);
            userContext.SetAttribute("age", 25);
            userContext.SetAttribute("location", "USA");

            var result = decisionService.GetVariation(experiment, userContext, mockConfig.Object);

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.ResultObject);
            Assert.IsNotNull(result.ResultObject.CmabUuid);

            cmabClientMock.VerifyAll();
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
                ForcedVariations = new Dictionary<string, string>(), // UserIdToKeyVariations is an alias for this
                Cmab = new Entity.Cmab(attributeIds ?? new List<string>())
                {
                    TrafficAllocation = trafficAllocation
                }
            };
        }

        /// <summary>
        /// Creates a mock ProjectConfig with the experiment and variation
        /// </summary>
        private Mock<ProjectConfig> CreateMockConfig(Experiment experiment, Variation variation,
            Dictionary<string, AttributeEntity> attributeMap = null)
        {
            var mockConfig = new Mock<ProjectConfig>();

            var experimentMap = new Dictionary<string, Experiment>
            {
                { experiment.Id, experiment }
            };

            mockConfig.Setup(c => c.ExperimentIdMap).Returns(experimentMap);
            mockConfig.Setup(c => c.GetExperimentFromKey(experiment.Key)).Returns(experiment);
            mockConfig.Setup(c => c.GetExperimentFromId(experiment.Id)).Returns(experiment);

            if (variation != null)
            {
                mockConfig.Setup(c => c.GetVariationFromIdByExperimentId(experiment.Id,
                    variation.Id)).Returns(variation);
            }

            mockConfig.Setup(c => c.AttributeIdMap)
                .Returns(attributeMap ?? new Dictionary<string, AttributeEntity>());

            return mockConfig;
        }

        #endregion
    }
}
