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
using OptimizelySDK.Tests.Utils;
using OptimizelySDK.Utils;
using AttributeEntity = OptimizelySDK.Entity.Attribute;

namespace OptimizelySDK.Tests.CmabTests
{
    [TestFixture]
    public class DefaultCmabServiceTest
    {
        [SetUp]
        public void SetUp()
        {
            _mockCmabClient = new Mock<ICmabClient>(MockBehavior.Strict);
            _logger = new NoOpLogger();
            _cmabCache = new LruCache<CmabCacheEntry>(10, TimeSpan.FromMinutes(5), _logger);
            _cmabService = new DefaultCmabService(_cmabCache, _mockCmabClient.Object, _logger);

            _config = DatafileProjectConfig.Create(TestData.Datafile, _logger,
                new NoOpErrorHandler());
            _optimizely = new Optimizely(TestData.Datafile, null, _logger, new NoOpErrorHandler());
        }

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

        [Test]
        public void ReturnsDecisionFromCacheWhenHashMatches()
        {
            var experiment = CreateExperiment(TEST_RULE_ID, new List<string> { AGE_ATTRIBUTE_ID });
            var attributeMap = new Dictionary<string, AttributeEntity>
            {
                { AGE_ATTRIBUTE_ID, new AttributeEntity { Id = AGE_ATTRIBUTE_ID, Key = "age" } },
            };
            var projectConfig = CreateProjectConfig(TEST_RULE_ID, experiment, attributeMap);
            var userContext = CreateUserContext(TEST_USER_ID,
                new Dictionary<string, object> { { "age", 25 } });
            var filteredAttributes =
                new UserAttributes(new Dictionary<string, object> { { "age", 25 } });
            var cacheKey = DefaultCmabService.GetCacheKey(TEST_USER_ID, TEST_RULE_ID);

            _cmabCache.Save(cacheKey, new CmabCacheEntry
            {
                AttributesHash = DefaultCmabService.HashAttributes(filteredAttributes),
                CmabUuid = "uuid-cached",
                VariationId = "varA",
            });

            var decision = _cmabService.GetDecision(projectConfig, userContext, TEST_RULE_ID);

            Assert.IsNotNull(decision);
            Assert.AreEqual("varA", decision.VariationId);
            Assert.AreEqual("uuid-cached", decision.CmabUuid);
            _mockCmabClient.Verify(
                c => c.FetchDecision(It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<IDictionary<string, object>>(), It.IsAny<string>(),
                    It.IsAny<TimeSpan?>()), Times.Never);
        }

        [Test]
        public void IgnoresCacheWhenOptionSpecified()
        {
            var experiment = CreateExperiment(TEST_RULE_ID, new List<string> { AGE_ATTRIBUTE_ID });
            var attributeMap = new Dictionary<string, AttributeEntity>
            {
                { AGE_ATTRIBUTE_ID, new AttributeEntity { Id = AGE_ATTRIBUTE_ID, Key = "age" } },
            };
            var projectConfig = CreateProjectConfig(TEST_RULE_ID, experiment, attributeMap);
            var userContext = CreateUserContext(TEST_USER_ID,
                new Dictionary<string, object> { { "age", 25 } });
            var cacheKey = DefaultCmabService.GetCacheKey(TEST_USER_ID, TEST_RULE_ID);

            _mockCmabClient.Setup(c => c.FetchDecision(TEST_RULE_ID, TEST_USER_ID,
                It.Is<IDictionary<string, object>>(attrs =>
                    attrs != null && attrs.Count == 1 && attrs.ContainsKey("age") &&
                    (int)attrs["age"] == 25),
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
                { AGE_ATTRIBUTE_ID, new AttributeEntity { Id = AGE_ATTRIBUTE_ID, Key = "age" } },
            };
            var projectConfig = CreateProjectConfig(TEST_RULE_ID, experiment, attributeMap);
            var userContext = CreateUserContext(TEST_USER_ID,
                new Dictionary<string, object> { { "age", 25 } });
            var cacheKey = DefaultCmabService.GetCacheKey(TEST_USER_ID, TEST_RULE_ID);

            _cmabCache.Save(cacheKey, new CmabCacheEntry
            {
                AttributesHash = "stale",
                CmabUuid = "uuid-old",
                VariationId = "varOld",
            });

            _mockCmabClient.Setup(c => c.FetchDecision(TEST_RULE_ID, TEST_USER_ID,
                It.Is<IDictionary<string, object>>(attrs =>
                    attrs.Count == 1 && (int)attrs["age"] == 25),
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
                { AGE_ATTRIBUTE_ID, new AttributeEntity { Id = AGE_ATTRIBUTE_ID, Key = "age" } },
            };
            var projectConfig = CreateProjectConfig(TEST_RULE_ID, experiment, attributeMap);
            var userContext = CreateUserContext(TEST_USER_ID,
                new Dictionary<string, object> { { "age", 25 } });

            var targetKey = DefaultCmabService.GetCacheKey(TEST_USER_ID, TEST_RULE_ID);
            var otherKey = DefaultCmabService.GetCacheKey(otherUserId, TEST_RULE_ID);

            _cmabCache.Save(targetKey, new CmabCacheEntry
            {
                AttributesHash = "old_hash",
                CmabUuid = "uuid-old",
                VariationId = "varOld",
            });
            _cmabCache.Save(otherKey, new CmabCacheEntry
            {
                AttributesHash = "other_hash",
                CmabUuid = "uuid-other",
                VariationId = "varOther",
            });

            _mockCmabClient.Setup(c => c.FetchDecision(TEST_RULE_ID, TEST_USER_ID,
                It.Is<IDictionary<string, object>>(attrs =>
                    attrs.Count == 1 && (int)attrs["age"] == 25),
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
                { AGE_ATTRIBUTE_ID, new AttributeEntity { Id = AGE_ATTRIBUTE_ID, Key = "age" } },
            };
            var projectConfig = CreateProjectConfig(TEST_RULE_ID, experiment, attributeMap);
            var userContext = CreateUserContext(TEST_USER_ID,
                new Dictionary<string, object> { { "age", 25 } });

            var cacheKey = DefaultCmabService.GetCacheKey(TEST_USER_ID, TEST_RULE_ID);
            _cmabCache.Save(cacheKey, new CmabCacheEntry
            {
                AttributesHash = "different_hash",
                CmabUuid = "uuid-old",
                VariationId = "varOld",
            });

            _mockCmabClient.Setup(c => c.FetchDecision(TEST_RULE_ID, TEST_USER_ID,
                It.Is<IDictionary<string, object>>(attrs =>
                    attrs.Count == 1 && (int)attrs["age"] == 25),
                It.IsAny<string>(),
                It.IsAny<TimeSpan?>())).Returns("varUpdated");

            var decision = _cmabService.GetDecision(projectConfig, userContext, TEST_RULE_ID);

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
            var experiment = CreateExperiment(TEST_RULE_ID,
                new List<string> { AGE_ATTRIBUTE_ID, LOCATION_ATTRIBUTE_ID });
            var attributeMap = new Dictionary<string, AttributeEntity>
            {
                { AGE_ATTRIBUTE_ID, new AttributeEntity { Id = AGE_ATTRIBUTE_ID, Key = "age" } },
                {
                    LOCATION_ATTRIBUTE_ID,
                    new AttributeEntity { Id = LOCATION_ATTRIBUTE_ID, Key = "location" }
                },
            };
            var projectConfig = CreateProjectConfig(TEST_RULE_ID, experiment, attributeMap);
            var userContext = CreateUserContext(TEST_USER_ID, new Dictionary<string, object>
            {
                { "age", 25 },
                { "location", "USA" },
                { "extra", "value" },
            });

            _mockCmabClient.Setup(c => c.FetchDecision(TEST_RULE_ID, TEST_USER_ID,
                It.Is<IDictionary<string, object>>(attrs => attrs.Count == 2 &&
                                                            (int)attrs["age"] == 25 &&
                                                            (string)attrs["location"] == "USA" &&
                                                            !attrs.ContainsKey("extra")),
                It.IsAny<string>(),
                It.IsAny<TimeSpan?>())).Returns("varFiltered");

            var decision = _cmabService.GetDecision(projectConfig, userContext, TEST_RULE_ID);

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
            var userContext = CreateUserContext(TEST_USER_ID,
                new Dictionary<string, object> { { "age", 25 } });

            _mockCmabClient.Setup(c => c.FetchDecision(TEST_RULE_ID, TEST_USER_ID,
                It.Is<IDictionary<string, object>>(attrs => attrs.Count == 0),
                It.IsAny<string>(),
                It.IsAny<TimeSpan?>())).Returns("varDefault");

            var decision = _cmabService.GetDecision(projectConfig, userContext, TEST_RULE_ID);

            Assert.IsNotNull(decision);
            Assert.AreEqual("varDefault", decision.VariationId);
            _mockCmabClient.VerifyAll();
        }

        [Test]
        public void AttributeHashIsStableRegardlessOfOrder()
        {
            var experiment = CreateExperiment(TEST_RULE_ID,
                new List<string> { AGE_ATTRIBUTE_ID, LOCATION_ATTRIBUTE_ID });
            var attributeMap = new Dictionary<string, AttributeEntity>
            {
                { AGE_ATTRIBUTE_ID, new AttributeEntity { Id = AGE_ATTRIBUTE_ID, Key = "a" } },
                {
                    LOCATION_ATTRIBUTE_ID,
                    new AttributeEntity { Id = LOCATION_ATTRIBUTE_ID, Key = "b" }
                },
            };
            var projectConfig = CreateProjectConfig(TEST_RULE_ID, experiment, attributeMap);

            var firstContext = CreateUserContext(TEST_USER_ID, new Dictionary<string, object>
            {
                { "b", 2 },
                { "a", 1 },
            });

            _mockCmabClient.Setup(c => c.FetchDecision(TEST_RULE_ID, TEST_USER_ID,
                It.IsAny<IDictionary<string, object>>(),
                It.IsAny<string>(),
                It.IsAny<TimeSpan?>())).Returns("varStable");

            var firstDecision = _cmabService.GetDecision(projectConfig, firstContext, TEST_RULE_ID);
            Assert.IsNotNull(firstDecision);
            Assert.AreEqual("varStable", firstDecision.VariationId);

            var secondContext = CreateUserContext(TEST_USER_ID, new Dictionary<string, object>
            {
                { "a", 1 },
                { "b", 2 },
            });

            var secondDecision =
                _cmabService.GetDecision(projectConfig, secondContext, TEST_RULE_ID);

            Assert.IsNotNull(secondDecision);
            Assert.AreEqual("varStable", secondDecision.VariationId);
            _mockCmabClient.Verify(c => c.FetchDecision(TEST_RULE_ID, TEST_USER_ID,
                    It.IsAny<IDictionary<string, object>>(), It.IsAny<string>(),
                    It.IsAny<TimeSpan?>()),
                Times.Once);
        }

        [Test]
        public void UsesExpectedCacheKeyFormat()
        {
            var experiment = CreateExperiment(TEST_RULE_ID, new List<string> { AGE_ATTRIBUTE_ID });
            var attributeMap = new Dictionary<string, AttributeEntity>
            {
                { AGE_ATTRIBUTE_ID, new AttributeEntity { Id = AGE_ATTRIBUTE_ID, Key = "age" } },
            };
            var projectConfig = CreateProjectConfig(TEST_RULE_ID, experiment, attributeMap);
            var userContext = CreateUserContext(TEST_USER_ID,
                new Dictionary<string, object> { { "age", 25 } });

            _mockCmabClient.Setup(c => c.FetchDecision(TEST_RULE_ID, TEST_USER_ID,
                It.IsAny<IDictionary<string, object>>(),
                It.IsAny<string>(),
                It.IsAny<TimeSpan?>())).Returns("varKey");

            var decision = _cmabService.GetDecision(projectConfig, userContext, TEST_RULE_ID);
            Assert.IsNotNull(decision);
            Assert.AreEqual("varKey", decision.VariationId);

            var cacheKey = DefaultCmabService.GetCacheKey(TEST_USER_ID, TEST_RULE_ID);
            var cachedEntry = _cmabCache.Lookup(cacheKey);
            Assert.IsNotNull(cachedEntry);
            Assert.AreEqual(decision.CmabUuid, cachedEntry.CmabUuid);
        }

        [Test]
        public void ConstructorWithoutConfigUsesDefaultCacheSettings()
        {
            var cache = new LruCache<CmabCacheEntry>(CmabConstants.DEFAULT_CACHE_SIZE,
                CmabConstants.DEFAULT_CACHE_TTL, _logger);
            var client = new DefaultCmabClient(null,
                new CmabRetryConfig(1, TimeSpan.FromMilliseconds(100)), _logger);
            var service = new DefaultCmabService(cache, client, _logger);
            var internalCache = GetInternalCache(service) as LruCache<CmabCacheEntry>;

            Assert.IsNotNull(internalCache);
            Assert.AreEqual(CmabConstants.DEFAULT_CACHE_SIZE, internalCache.MaxSizeForTesting);
            Assert.AreEqual(CmabConstants.DEFAULT_CACHE_TTL, internalCache.TimeoutForTesting);
        }

        [Test]
        public void ConstructorAppliesCustomCacheSize()
        {
            var cache = new LruCache<CmabCacheEntry>(42, CmabConstants.DEFAULT_CACHE_TTL, _logger);
            var client = new DefaultCmabClient(null,
                new CmabRetryConfig(1, TimeSpan.FromMilliseconds(100)), _logger);
            var service = new DefaultCmabService(cache, client, _logger);
            var internalCache = GetInternalCache(service) as LruCache<CmabCacheEntry>;

            Assert.IsNotNull(internalCache);
            Assert.AreEqual(42, internalCache.MaxSizeForTesting);
            Assert.AreEqual(CmabConstants.DEFAULT_CACHE_TTL, internalCache.TimeoutForTesting);
        }

        [Test]
        public void ConstructorAppliesCustomCacheTtl()
        {
            var expectedTtl = TimeSpan.FromMinutes(3);
            var cache = new LruCache<CmabCacheEntry>(CmabConstants.DEFAULT_CACHE_SIZE, expectedTtl,
                _logger);
            var client = new DefaultCmabClient(null,
                new CmabRetryConfig(1, TimeSpan.FromMilliseconds(100)), _logger);
            var service = new DefaultCmabService(cache, client, _logger);
            var internalCache = GetInternalCache(service) as LruCache<CmabCacheEntry>;

            Assert.IsNotNull(internalCache);
            Assert.AreEqual(CmabConstants.DEFAULT_CACHE_SIZE, internalCache.MaxSizeForTesting);
            Assert.AreEqual(expectedTtl, internalCache.TimeoutForTesting);
        }

        [Test]
        public void ConstructorAppliesCustomCacheSizeAndTtl()
        {
            var expectedTtl = TimeSpan.FromSeconds(90);
            var cache = new LruCache<CmabCacheEntry>(5, expectedTtl, _logger);
            var client = new DefaultCmabClient(null,
                new CmabRetryConfig(1, TimeSpan.FromMilliseconds(100)), _logger);
            var service = new DefaultCmabService(cache, client, _logger);
            var internalCache = GetInternalCache(service) as LruCache<CmabCacheEntry>;

            Assert.IsNotNull(internalCache);
            Assert.AreEqual(5, internalCache.MaxSizeForTesting);
            Assert.AreEqual(expectedTtl, internalCache.TimeoutForTesting);
        }

        [Test]
        public void ConstructorUsesProvidedCustomCacheInstance()
        {
            var customCache = new LruCache<CmabCacheEntry>(3, TimeSpan.FromSeconds(5), _logger);
            var client = new DefaultCmabClient(null,
                new CmabRetryConfig(1, TimeSpan.FromMilliseconds(100)), _logger);
            var service = new DefaultCmabService(customCache, client, _logger);
            var cache = GetInternalCache(service);

            Assert.IsNotNull(cache);
            Assert.AreSame(customCache, cache);
        }

        [Test]
        public void ConstructorAcceptsAnyICacheImplementation()
        {
            var fakeCache = new FakeCache();
            var client = new DefaultCmabClient(null,
                new CmabRetryConfig(1, TimeSpan.FromMilliseconds(100)), _logger);
            var service = new DefaultCmabService(fakeCache, client, _logger);
            var cache = GetInternalCache(service);

            Assert.IsNotNull(cache);
            Assert.AreSame(fakeCache, cache);
            Assert.IsInstanceOf<ICacheWithRemove<CmabCacheEntry>>(cache);
        }

        [Test]
        public void ConstructorCreatesDefaultClientWhenNoneProvided()
        {
            var cache = new LruCache<CmabCacheEntry>(CmabConstants.DEFAULT_CACHE_SIZE,
                CmabConstants.DEFAULT_CACHE_TTL, _logger);
            var client = new DefaultCmabClient(null,
                new CmabRetryConfig(1, TimeSpan.FromMilliseconds(100)), _logger);
            var service = new DefaultCmabService(cache, client, _logger);
            var internalClient = GetInternalClient(service);

            Assert.IsInstanceOf<DefaultCmabClient>(internalClient);
        }

        [Test]
        public void ConstructorUsesProvidedClientInstance()
        {
            var mockClient = new Mock<ICmabClient>().Object;
            var cache = new LruCache<CmabCacheEntry>(CmabConstants.DEFAULT_CACHE_SIZE,
                CmabConstants.DEFAULT_CACHE_TTL, _logger);
            var service = new DefaultCmabService(cache, mockClient, _logger);
            var client = GetInternalClient(service);

            Assert.AreSame(mockClient, client);
        }

        [Test]
        public void ConcurrentRequestsForSameUserUseCacheAfterFirstNetworkCall()
        {
            var experiment = CreateExperiment(TEST_RULE_ID, new List<string> { AGE_ATTRIBUTE_ID });
            var attributeMap = new Dictionary<string, AttributeEntity>
            {
                { AGE_ATTRIBUTE_ID, new AttributeEntity { Id = AGE_ATTRIBUTE_ID, Key = "age" } },
            };
            var projectConfig = CreateProjectConfig(TEST_RULE_ID, experiment, attributeMap);
            var userContext = CreateUserContext(TEST_USER_ID,
                new Dictionary<string, object> { { "age", 25 } });

            var clientCallCount = 0;
            var clientCallLock = new object();

            _mockCmabClient.Setup(c => c.FetchDecision(
                TEST_RULE_ID,
                TEST_USER_ID,
                It.Is<IDictionary<string, object>>(attrs =>
                    attrs != null && attrs.Count == 1 && attrs.ContainsKey("age") &&
                    (int)attrs["age"] == 25),
                It.IsAny<string>(),
                It.IsAny<TimeSpan?>()))
                .Returns(() =>
                {
                    lock (clientCallLock)
                    {
                        clientCallCount++;
                    }
                    System.Threading.Thread.Sleep(100);

                    return "varConcurrent";
                });

            var tasks = new System.Threading.Tasks.Task<CmabDecision>[10];

            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i] = System.Threading.Tasks.Task.Run(() =>
                    _cmabService.GetDecision(projectConfig, userContext, TEST_RULE_ID));
            }

            System.Threading.Tasks.Task.WaitAll(tasks);

            foreach (var task in tasks)
            {
                Assert.IsNotNull(task.Result);
                Assert.AreEqual("varConcurrent", task.Result.VariationId);
            }

            Assert.AreEqual(1, clientCallCount,
                "Client should only be called once - subsequent requests should use cache");

            _mockCmabClient.VerifyAll();
        }

        [Test]
        public void SameUserRuleCombinationUsesConsistentLock()
        {
            var userId = "test_user";
            var ruleId = "test_rule";

            var index1 = _cmabService.GetLockIndex(userId, ruleId);
            var index2 = _cmabService.GetLockIndex(userId, ruleId);
            var index3 = _cmabService.GetLockIndex(userId, ruleId);

            Assert.AreEqual(index1, index2, "Same user/rule should always use same lock");
            Assert.AreEqual(index2, index3, "Same user/rule should always use same lock");
        }

        [Test]
        public void LockStripingDistribution()
        {
            var testCases = new[]
            {
                new { UserId = "user1", RuleId = "rule1" },
                new { UserId = "user2", RuleId = "rule1" },
                new { UserId = "user1", RuleId = "rule2" },
                new { UserId = "user3", RuleId = "rule3" },
                new { UserId = "user4", RuleId = "rule4" },
            };

            var lockIndices = new HashSet<int>();
            foreach (var testCase in testCases)
            {
                var index = _cmabService.GetLockIndex(testCase.UserId, testCase.RuleId);

                Assert.GreaterOrEqual(index, 0, "Lock index should be non-negative");
                Assert.Less(index, 1000, "Lock index should be less than NUM_LOCK_STRIPES (1000)");

                lockIndices.Add(index);
            }

            Assert.Greater(lockIndices.Count, 1,
                "Different user/rule combinations should generally use different locks");
        }

        private static ICacheWithRemove<CmabCacheEntry> GetInternalCache(DefaultCmabService service)
        {
            return Reflection.GetFieldValue<ICacheWithRemove<CmabCacheEntry>, DefaultCmabService>(service,
                "_cmabCache");
        }

        private static ICmabClient GetInternalClient(DefaultCmabService service)
        {
            return Reflection.GetFieldValue<ICmabClient, DefaultCmabService>(service,
                "_cmabClient");
        }

        private sealed class FakeCache : ICacheWithRemove<CmabCacheEntry>
        {
            public void Save(string key, CmabCacheEntry value) { }

            public CmabCacheEntry Lookup(string key)
            {
                return null;
            }

            public void Reset() { }

            public void Remove(string key) { }
        }

        private OptimizelyUserContext CreateUserContext(string userId,
            IDictionary<string, object> attributes
        )
        {
            var userContext = _optimizely.CreateUserContext(userId);

            foreach (var attr in attributes)
            {
                userContext.SetAttribute(attr.Key, attr.Value);
            }

            return userContext;
        }

        private static ProjectConfig CreateProjectConfig(string ruleId, Experiment experiment,
            Dictionary<string, AttributeEntity> attributeMap
        )
        {
            var mockConfig = new Mock<ProjectConfig>();
            var experimentMap = new Dictionary<string, Experiment>();
            if (experiment != null)
            {
                experimentMap[ruleId] = experiment;
            }

            mockConfig.SetupGet(c => c.ExperimentIdMap).Returns(experimentMap);
            mockConfig.SetupGet(c => c.AttributeIdMap).
                Returns(attributeMap ?? new Dictionary<string, AttributeEntity>());
            return mockConfig.Object;
        }

        private static Experiment CreateExperiment(string ruleId, List<string> attributeIds)
        {
            return new Experiment
            {
                Id = ruleId,
                Cmab = attributeIds == null ? null : new Entity.Cmab(attributeIds),
            };
        }

        

        
    }
}
