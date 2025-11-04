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

using System.Collections.Generic;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using OptimizelySDK.Bucketing;
using OptimizelySDK.Config;
using OptimizelySDK.Entity;
using OptimizelySDK.ErrorHandler;
using OptimizelySDK.Logger;

namespace OptimizelySDK.Tests
{
    [TestFixture]
    public class BucketerBucketToEntityIdTest
    {
        [SetUp]
        public void SetUp()
        {
            _loggerMock = new Mock<ILogger>();
        }

        private const string ExperimentId = "bucket_entity_exp";
        private const string ExperimentKey = "bucket_entity_experiment";
        private const string GroupId = "group_1";

        private Mock<ILogger> _loggerMock;

        [Test]
        public void BucketToEntityIdAllowsBucketingWhenNoGroup()
        {
            var config = CreateConfig(new ConfigSetup { IncludeGroup = false });
            var experiment = config.GetExperimentFromKey(ExperimentKey);
            var bucketer = new Bucketer(_loggerMock.Object);

            var fullAllocation = CreateTrafficAllocations(new TrafficAllocation
            {
                EntityId = "entity_123",
                EndOfRange = 10000,
            });
            var fullResult = bucketer.BucketToEntityId(config, experiment, "bucketing_id", "user",
                fullAllocation);
            Assert.IsNotNull(fullResult.ResultObject);
            Assert.AreEqual("entity_123", fullResult.ResultObject);

            var zeroAllocation = CreateTrafficAllocations(new TrafficAllocation
            {
                EntityId = "entity_123",
                EndOfRange = 0,
            });
            var zeroResult = bucketer.BucketToEntityId(config, experiment, "bucketing_id", "user",
                zeroAllocation);
            Assert.IsNull(zeroResult.ResultObject);
        }

        [Test]
        public void BucketToEntityIdReturnsEntityIdWhenGroupAllowsUser()
        {
            var config = CreateConfig(new ConfigSetup
            {
                IncludeGroup = true,
                GroupPolicy = "random",
                GroupEndOfRange = 10000,
            });

            var experiment = config.GetExperimentFromKey(ExperimentKey);
            var bucketer = new Bucketer(_loggerMock.Object);

            var testCases = new[]
            {
                new { BucketingId = "ppid1", EntityId = "entity1" },
                new { BucketingId = "ppid2", EntityId = "entity2" },
                new { BucketingId = "ppid3", EntityId = "entity3" },
                new
                {
                    BucketingId =
                        "a very very very very very very very very very very very very very very very long ppd string",
                    EntityId = "entity4",
                },
            };

            foreach (var testCase in testCases)
            {
                var allocation = CreateTrafficAllocations(new TrafficAllocation
                {
                    EntityId = testCase.EntityId,
                    EndOfRange = 10000,
                });
                var result = bucketer.BucketToEntityId(config, experiment, testCase.BucketingId,
                    testCase.BucketingId, allocation);
                Assert.AreEqual(testCase.EntityId, result.ResultObject,
                    $"Failed for {testCase.BucketingId}");
            }
        }

        [Test]
        public void BucketToEntityIdReturnsNullWhenGroupRejectsUser()
        {
            var config = CreateConfig(new ConfigSetup
            {
                IncludeGroup = true,
                GroupPolicy = "random",
                GroupEndOfRange = 0,
            });

            var experiment = config.GetExperimentFromKey(ExperimentKey);
            var bucketer = new Bucketer(_loggerMock.Object);

            var allocation = CreateTrafficAllocations(new TrafficAllocation
            {
                EntityId = "entity1",
                EndOfRange = 10000,
            });
            var testCases = new[]
            {
                "ppid1",
                "ppid2",
                "ppid3",
                "a very very very very very very very very very very very very very very very long ppd string",
            };

            foreach (var bucketingId in testCases)
            {
                var result = bucketer.BucketToEntityId(config, experiment, bucketingId, bucketingId,
                    allocation);
                Assert.IsNull(result.ResultObject, $"Expected null for {bucketingId}");
            }
        }

        [Test]
        public void BucketToEntityIdAllowsBucketingWhenGroupOverlapping()
        {
            var config = CreateConfig(new ConfigSetup
            {
                IncludeGroup = true,
                GroupPolicy = "overlapping",
                GroupEndOfRange = 10000,
            });

            var experiment = config.GetExperimentFromKey(ExperimentKey);
            var bucketer = new Bucketer(_loggerMock.Object);

            var allocation = CreateTrafficAllocations(new TrafficAllocation
            {
                EntityId = "entity_overlapping",
                EndOfRange = 10000,
            });
            var result =
                bucketer.BucketToEntityId(config, experiment, "bucketing_id", "user", allocation);
            Assert.AreEqual("entity_overlapping", result.ResultObject);
        }

        private static IList<TrafficAllocation> CreateTrafficAllocations(
            params TrafficAllocation[] allocations
        )
        {
            return new List<TrafficAllocation>(allocations);
        }

        private ProjectConfig CreateConfig(ConfigSetup setup)
        {
            if (setup == null)
            {
                setup = new ConfigSetup();
            }

            var datafile = BuildDatafile(setup);
            return DatafileProjectConfig.Create(datafile, _loggerMock.Object,
                new NoOpErrorHandler());
        }

        private static string BuildDatafile(ConfigSetup setup)
        {
            var variations = new object[]
            {
                new Dictionary<string, object>
                {
                    { "id", "var_1" },
                    { "key", "variation_1" },
                    { "variables", new object[0] },
                },
            };

            var experiment = new Dictionary<string, object>
            {
                { "status", "Running" },
                { "key", ExperimentKey },
                { "layerId", "layer_1" },
                { "id", ExperimentId },
                { "audienceIds", new string[0] },
                { "audienceConditions", "[]" },
                { "forcedVariations", new Dictionary<string, string>() },
                { "variations", variations },
                {
                    "trafficAllocation", new object[]
                    {
                        new Dictionary<string, object>
                        {
                            { "entityId", "var_1" },
                            { "endOfRange", 10000 },
                        },
                    }
                },
            };

            object[] groups;
            if (setup.IncludeGroup)
            {
                var groupExperiment = new Dictionary<string, object>(experiment);
                groupExperiment["trafficAllocation"] = new object[0];

                groups = new object[]
                {
                    new Dictionary<string, object>
                    {
                        { "id", GroupId },
                        { "policy", setup.GroupPolicy },
                        {
                            "trafficAllocation", new object[]
                            {
                                new Dictionary<string, object>
                                {
                                    { "entityId", ExperimentId },
                                    { "endOfRange", setup.GroupEndOfRange },
                                },
                            }
                        },
                        { "experiments", new object[] { groupExperiment } },
                    },
                };
            }
            else
            {
                groups = new object[0];
            }

            var datafile = new Dictionary<string, object>
            {
                { "version", "4" },
                { "projectId", "project_1" },
                { "accountId", "account_1" },
                { "revision", "1" },
                { "environmentKey", string.Empty },
                { "sdkKey", string.Empty },
                { "sendFlagDecisions", false },
                { "anonymizeIP", false },
                { "botFiltering", false },
                { "attributes", new object[0] },
                { "audiences", new object[0] },
                { "typedAudiences", new object[0] },
                { "events", new object[0] },
                { "featureFlags", new object[0] },
                { "rollouts", new object[0] },
                { "integrations", new object[0] },
                { "holdouts", new object[0] },
                { "groups", groups },
                { "experiments", new object[] { experiment } },
                { "segments", new object[0] },
            };

            return JsonConvert.SerializeObject(datafile);
        }

        private class ConfigSetup
        {
            public bool IncludeGroup { get; set; }
            public string GroupPolicy { get; set; }
            public int GroupEndOfRange { get; set; }
        }
    }
}
