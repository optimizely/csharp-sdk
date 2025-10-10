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
using OptimizelySDK.Cmab;
using OptimizelySDK.Config;
using OptimizelySDK.Entity;
using OptimizelySDK.ErrorHandler;
using OptimizelySDK.Logger;
using OptimizelySDK.Odp;
using OptimizelySDK.OptimizelyDecisions;
using AttributeEntity = OptimizelySDK.Entity.Attribute;

namespace OptimizelySDK.Tests.CmabTests
{
    [TestFixture]
    public class DefaultCmabServiceTest
    {
        private Mock<ICmabClient> _mockCmabClient;
        private LruCache<CmabCacheEntry> _cmabCache;
        private DefaultCmabService _cmabService;
        private ILogger _logger;
        private ProjectConfig _config;
        private Optimizely _optimizely;

        private const string TEST_RULE_ID = "exp1";
        private const string TEST_USER_ID = "user123";
        private const string AGE_ATTRIBUTE_ID = "66";
        private const string LOCATION_ATTRIBUTE_ID = "77";

        [SetUp]
        public void SetUp()
        {
            _mockCmabClient = new Mock<ICmabClient>(MockBehavior.Strict);
            _logger = new NoOpLogger();
            _cmabCache = new LruCache<CmabCacheEntry>(maxSize: 10, itemTimeout: TimeSpan.FromMinutes(5), logger: _logger);
            _cmabService = new DefaultCmabService(_cmabCache, _mockCmabClient.Object, _logger);

            _config = DatafileProjectConfig.Create(TestData.Datafile, _logger, new NoOpErrorHandler());
            _optimizely = new Optimizely(TestData.Datafile, null, _logger, new NoOpErrorHandler());
        }

        [Test]
        public void ReturnsDecisionFromCacheWhenHashMatches()
        {
            var experiment = CreateExperiment(TEST_RULE_ID, new List<string> { AGE_ATTRIBUTE_ID });
            var attributeMap = new Dictionary<string, AttributeEntity>
            {
                { AGE_ATTRIBUTE_ID, new AttributeEntity { Id = AGE_ATTRIBUTE_ID, Key = "age" } }
            };
            var projectConfig = CreateProjectConfig(TEST_RULE_ID, experiment, attributeMap);
            var userContext = CreateUserContext(TEST_USER_ID, new Dictionary<string, object> { { "age", 25 } });
            var filteredAttributes = new UserAttributes(new Dictionary<string, object> { { "age", 25 } });
            var cacheKey = DefaultCmabService.GetCacheKey(TEST_USER_ID, TEST_RULE_ID);

            _cmabCache.Save(cacheKey, new CmabCacheEntry
            {
                AttributesHash = DefaultCmabService.HashAttributes(filteredAttributes),
                CmabUuid = "uuid-cached",
                VariationId = "varA"
            });

            var decision = _cmabService.GetDecision(projectConfig, userContext, TEST_RULE_ID, null);

            Assert.IsNotNull(decision);
            Assert.AreEqual("varA", decision.VariationId);
            Assert.AreEqual("uuid-cached", decision.CmabUuid);
            _mockCmabClient.Verify(c => c.FetchDecision(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDictionary<string, object>>(), It.IsAny<string>(), It.IsAny<TimeSpan?>()), Times.Never);
        }

        [Test]
        public void IgnoresCacheWhenOptionSpecified()
        {
            var experiment = CreateExperiment(TEST_RULE_ID, new List<string> { AGE_ATTRIBUTE_ID });
            var attributeMap = new Dictionary<string, AttributeEntity>
            {
                { AGE_ATTRIBUTE_ID, new AttributeEntity { Id = AGE_ATTRIBUTE_ID, Key = "age" } }
            };
            var projectConfig = CreateProjectConfig(TEST_RULE_ID, experiment, attributeMap);
            var userContext = CreateUserContext(TEST_USER_ID, new Dictionary<string, object> { { "age", 25 } });
            var cacheKey = DefaultCmabService.GetCacheKey(TEST_USER_ID, TEST_RULE_ID);

            _mockCmabClient.Setup(c => c.FetchDecision(TEST_RULE_ID, TEST_USER_ID,
                It.Is<IDictionary<string, object>>(attrs => attrs != null && attrs.Count == 1 && attrs.ContainsKey("age") && (int)attrs["age"] == 25),
                It.IsAny<string>(),
                It.IsAny<TimeSpan?>())).Returns("varB");

            var decision = _cmabService.GetDecision(projectConfig, userContext, TEST_RULE_ID,
                new[] { OptimizelyDecideOption.IGNORE_CMAB_CACHE });

            Assert.IsNotNull(decision);
            Assert.AreEqual("varB", decision.VariationId);
            Assert.IsNull(_cmabCache.Lookup(cacheKey));
            _mockCmabClient.VerifyAll();
        }

        [Test]
        public void ResetsCacheWhenOptionSpecified()
        {
            var experiment = CreateExperiment(TEST_RULE_ID, new List<string> { AGE_ATTRIBUTE_ID });
            var attributeMap = new Dictionary<string, AttributeEntity>
            {
                { AGE_ATTRIBUTE_ID, new AttributeEntity { Id = AGE_ATTRIBUTE_ID, Key = "age" } }
            };
            var projectConfig = CreateProjectConfig(TEST_RULE_ID, experiment, attributeMap);
            var userContext = CreateUserContext(TEST_USER_ID, new Dictionary<string, object> { { "age", 25 } });
            var cacheKey = DefaultCmabService.GetCacheKey(TEST_USER_ID, TEST_RULE_ID);

            _cmabCache.Save(cacheKey, new CmabCacheEntry
            {
                AttributesHash = "stale",
                CmabUuid = "uuid-old",
                VariationId = "varOld"
            });

            _mockCmabClient.Setup(c => c.FetchDecision(TEST_RULE_ID, TEST_USER_ID,
                It.Is<IDictionary<string, object>>(attrs => attrs.Count == 1 && (int)attrs["age"] == 25),
                It.IsAny<string>(),
                It.IsAny<TimeSpan?>())).Returns("varNew");

            var decision = _cmabService.GetDecision(projectConfig, userContext, TEST_RULE_ID,
                new[] { OptimizelyDecideOption.RESET_CMAB_CACHE });

            Assert.IsNotNull(decision);
            Assert.AreEqual("varNew", decision.VariationId);
            var cachedEntry = _cmabCache.Lookup(cacheKey);
            Assert.IsNotNull(cachedEntry);
            Assert.AreEqual("varNew", cachedEntry.VariationId);
            Assert.AreEqual(decision.CmabUuid, cachedEntry.CmabUuid);
            _mockCmabClient.VerifyAll();
        }

        [Test]
        public void InvalidatesUserEntryWhenOptionSpecified()
        {
            var otherUserId = "other";
            var experiment = CreateExperiment(TEST_RULE_ID, new List<string> { AGE_ATTRIBUTE_ID });
            var attributeMap = new Dictionary<string, AttributeEntity>
            {
                { AGE_ATTRIBUTE_ID, new AttributeEntity { Id = AGE_ATTRIBUTE_ID, Key = "age" } }
            };
            var projectConfig = CreateProjectConfig(TEST_RULE_ID, experiment, attributeMap);
            var userContext = CreateUserContext(TEST_USER_ID, new Dictionary<string, object> { { "age", 25 } });

            var targetKey = DefaultCmabService.GetCacheKey(TEST_USER_ID, TEST_RULE_ID);
            var otherKey = DefaultCmabService.GetCacheKey(otherUserId, TEST_RULE_ID);

            _cmabCache.Save(targetKey, new CmabCacheEntry
            {
                AttributesHash = "old_hash",
                CmabUuid = "uuid-old",
                VariationId = "varOld"
            });
            _cmabCache.Save(otherKey, new CmabCacheEntry
            {
                AttributesHash = "other_hash",
                CmabUuid = "uuid-other",
                VariationId = "varOther"
            });

            _mockCmabClient.Setup(c => c.FetchDecision(TEST_RULE_ID, TEST_USER_ID,
                It.Is<IDictionary<string, object>>(attrs => attrs.Count == 1 && (int)attrs["age"] == 25),
                It.IsAny<string>(),
                It.IsAny<TimeSpan?>())).Returns("varNew");

            var decision = _cmabService.GetDecision(projectConfig, userContext, TEST_RULE_ID,
                new[] { OptimizelyDecideOption.INVALIDATE_USER_CMAB_CACHE });

            Assert.IsNotNull(decision);
            Assert.AreEqual("varNew", decision.VariationId);
            var updatedEntry = _cmabCache.Lookup(targetKey);
            Assert.IsNotNull(updatedEntry);
            Assert.AreEqual(decision.CmabUuid, updatedEntry.CmabUuid);
            Assert.AreEqual("varNew", updatedEntry.VariationId);

            var otherEntry = _cmabCache.Lookup(otherKey);
            Assert.IsNotNull(otherEntry);
            Assert.AreEqual("varOther", otherEntry.VariationId);
            _mockCmabClient.VerifyAll();
        }

        [Test]
        public void FetchesNewDecisionWhenHashDiffers()
        {
            var experiment = CreateExperiment(TEST_RULE_ID, new List<string> { AGE_ATTRIBUTE_ID });
            var attributeMap = new Dictionary<string, AttributeEntity>
            {
                { AGE_ATTRIBUTE_ID, new AttributeEntity { Id = AGE_ATTRIBUTE_ID, Key = "age" } }
            };
            var projectConfig = CreateProjectConfig(TEST_RULE_ID, experiment, attributeMap);
            var userContext = CreateUserContext(TEST_USER_ID, new Dictionary<string, object> { { "age", 25 } });

            var cacheKey = DefaultCmabService.GetCacheKey(TEST_USER_ID, TEST_RULE_ID);
            _cmabCache.Save(cacheKey, new CmabCacheEntry
            {
                AttributesHash = "different_hash",
                CmabUuid = "uuid-old",
                VariationId = "varOld"
            });

            _mockCmabClient.Setup(c => c.FetchDecision(TEST_RULE_ID, TEST_USER_ID,
                It.Is<IDictionary<string, object>>(attrs => attrs.Count == 1 && (int)attrs["age"] == 25),
                It.IsAny<string>(),
                It.IsAny<TimeSpan?>())).Returns("varUpdated");

            var decision = _cmabService.GetDecision(projectConfig, userContext, TEST_RULE_ID, null);

            Assert.IsNotNull(decision);
            Assert.AreEqual("varUpdated", decision.VariationId);
            var cachedEntry = _cmabCache.Lookup(cacheKey);
            Assert.IsNotNull(cachedEntry);
            Assert.AreEqual("varUpdated", cachedEntry.VariationId);
            Assert.AreEqual(decision.CmabUuid, cachedEntry.CmabUuid);
            _mockCmabClient.VerifyAll();
        }

        [Test]
        public void FiltersAttributesBeforeCallingClient()
        {
            var experiment = CreateExperiment(TEST_RULE_ID, new List<string> { AGE_ATTRIBUTE_ID, LOCATION_ATTRIBUTE_ID });
            var attributeMap = new Dictionary<string, AttributeEntity>
            {
                { AGE_ATTRIBUTE_ID, new AttributeEntity { Id = AGE_ATTRIBUTE_ID, Key = "age" } },
                { LOCATION_ATTRIBUTE_ID, new AttributeEntity { Id = LOCATION_ATTRIBUTE_ID, Key = "location" } }
            };
            var projectConfig = CreateProjectConfig(TEST_RULE_ID, experiment, attributeMap);
            var userContext = CreateUserContext(TEST_USER_ID, new Dictionary<string, object>
            {
                { "age", 25 },
                { "location", "USA" },
                { "extra", "value" }
            });

            _mockCmabClient.Setup(c => c.FetchDecision(TEST_RULE_ID, TEST_USER_ID,
                It.Is<IDictionary<string, object>>(attrs => attrs.Count == 2 &&
                                                            (int)attrs["age"] == 25 &&
                                                            (string)attrs["location"] == "USA" &&
                                                            !attrs.ContainsKey("extra")),
                It.IsAny<string>(),
                It.IsAny<TimeSpan?>())).Returns("varFiltered");

            var decision = _cmabService.GetDecision(projectConfig, userContext, TEST_RULE_ID, null);

            Assert.IsNotNull(decision);
            Assert.AreEqual("varFiltered", decision.VariationId);
            _mockCmabClient.VerifyAll();
        }

        [Test]
        public void HandlesMissingCmabConfiguration()
        {
            var experiment = CreateExperiment(TEST_RULE_ID, null);
            var attributeMap = new Dictionary<string, AttributeEntity>();
            var projectConfig = CreateProjectConfig(TEST_RULE_ID, experiment, attributeMap);
            var userContext = CreateUserContext(TEST_USER_ID, new Dictionary<string, object> { { "age", 25 } });

            _mockCmabClient.Setup(c => c.FetchDecision(TEST_RULE_ID, TEST_USER_ID,
                It.Is<IDictionary<string, object>>(attrs => attrs.Count == 0),
                It.IsAny<string>(),
                It.IsAny<TimeSpan?>())).Returns("varDefault");

            var decision = _cmabService.GetDecision(projectConfig, userContext, TEST_RULE_ID, null);

            Assert.IsNotNull(decision);
            Assert.AreEqual("varDefault", decision.VariationId);
            _mockCmabClient.VerifyAll();
        }

        [Test]
        public void AttributeHashIsStableRegardlessOfOrder()
        {
            var experiment = CreateExperiment(TEST_RULE_ID, new List<string> { AGE_ATTRIBUTE_ID, LOCATION_ATTRIBUTE_ID });
            var attributeMap = new Dictionary<string, AttributeEntity>
            {
                { AGE_ATTRIBUTE_ID, new AttributeEntity { Id = AGE_ATTRIBUTE_ID, Key = "a" } },
                { LOCATION_ATTRIBUTE_ID, new AttributeEntity { Id = LOCATION_ATTRIBUTE_ID, Key = "b" } }
            };
            var projectConfig = CreateProjectConfig(TEST_RULE_ID, experiment, attributeMap);

            var firstContext = CreateUserContext(TEST_USER_ID, new Dictionary<string, object>
            {
                { "b", 2 },
                { "a", 1 }
            });

            _mockCmabClient.Setup(c => c.FetchDecision(TEST_RULE_ID, TEST_USER_ID,
                It.IsAny<IDictionary<string, object>>(),
                It.IsAny<string>(),
                It.IsAny<TimeSpan?>())).Returns("varStable");

            var firstDecision = _cmabService.GetDecision(projectConfig, firstContext, TEST_RULE_ID, null);
            Assert.IsNotNull(firstDecision);
            Assert.AreEqual("varStable", firstDecision.VariationId);

            var secondContext = CreateUserContext(TEST_USER_ID, new Dictionary<string, object>
            {
                { "a", 1 },
                { "b", 2 }
            });

            var secondDecision = _cmabService.GetDecision(projectConfig, secondContext, TEST_RULE_ID, null);

            Assert.IsNotNull(secondDecision);
            Assert.AreEqual("varStable", secondDecision.VariationId);
            _mockCmabClient.Verify(c => c.FetchDecision(TEST_RULE_ID, TEST_USER_ID,
                It.IsAny<IDictionary<string, object>>(), It.IsAny<string>(), It.IsAny<TimeSpan?>()), Times.Once);
        }

        [Test]
        public void UsesExpectedCacheKeyFormat()
        {
            var experiment = CreateExperiment(TEST_RULE_ID, new List<string> { AGE_ATTRIBUTE_ID });
            var attributeMap = new Dictionary<string, AttributeEntity>
            {
                { AGE_ATTRIBUTE_ID, new AttributeEntity { Id = AGE_ATTRIBUTE_ID, Key = "age" } }
            };
            var projectConfig = CreateProjectConfig(TEST_RULE_ID, experiment, attributeMap);
            var userContext = CreateUserContext(TEST_USER_ID, new Dictionary<string, object> { { "age", 25 } });

            _mockCmabClient.Setup(c => c.FetchDecision(TEST_RULE_ID, TEST_USER_ID,
                It.IsAny<IDictionary<string, object>>(),
                It.IsAny<string>(),
                It.IsAny<TimeSpan?>())).Returns("varKey");

            var decision = _cmabService.GetDecision(projectConfig, userContext, TEST_RULE_ID, null);
            Assert.IsNotNull(decision);
            Assert.AreEqual("varKey", decision.VariationId);

            var cacheKey = DefaultCmabService.GetCacheKey(TEST_USER_ID, TEST_RULE_ID);
            var cachedEntry = _cmabCache.Lookup(cacheKey);
            Assert.IsNotNull(cachedEntry);
            Assert.AreEqual(decision.CmabUuid, cachedEntry.CmabUuid);
        }

        private OptimizelyUserContext CreateUserContext(string userId, IDictionary<string, object> attributes)
        {
            var userContext = _optimizely.CreateUserContext(userId);

            foreach (var attr in attributes)
            {
                userContext.SetAttribute(attr.Key, attr.Value);
            }

            return userContext;
        }

        private static ProjectConfig CreateProjectConfig(string ruleId, Experiment experiment,
            Dictionary<string, AttributeEntity> attributeMap)
        {
            var mockConfig = new Mock<ProjectConfig>();
            var experimentMap = new Dictionary<string, Experiment>();
            if (experiment != null)
            {
                experimentMap[ruleId] = experiment;
            }

            mockConfig.SetupGet(c => c.ExperimentIdMap).Returns(experimentMap);
            mockConfig.SetupGet(c => c.AttributeIdMap).Returns(attributeMap ?? new Dictionary<string, AttributeEntity>());
            return mockConfig.Object;
        }

        private static Experiment CreateExperiment(string ruleId, List<string> attributeIds)
        {
            return new Experiment
            {
                Id = ruleId,
                Cmab = attributeIds == null ? null : new Entity.Cmab(attributeIds)
            };
        }

    }
}
