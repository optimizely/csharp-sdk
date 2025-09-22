/*
 * Copyright 2017-2023, Optimizely
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
using System.IO;
using System.Linq;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using OptimizelySDK.Config;
using OptimizelySDK.Entity;
using OptimizelySDK.ErrorHandler;
using OptimizelySDK.Exceptions;
using OptimizelySDK.Logger;
using OptimizelySDK.Utils;

namespace OptimizelySDK.Tests
{
    [TestFixture]
    public class ProjectConfigTest
    {
        private Mock<ILogger> LoggerMock;
        private Mock<IErrorHandler> ErrorHandlerMock;
        private ProjectConfig Config;

        [SetUp]
        public void Setup()
        {
            LoggerMock = new Mock<ILogger>();
            ErrorHandlerMock = new Mock<IErrorHandler>();
            ErrorHandlerMock.Setup(e => e.HandleError(It.IsAny<Exception>()));

            Config = DatafileProjectConfig.Create(TestData.Datafile, LoggerMock.Object,
                ErrorHandlerMock.Object);
        }

        public static Dictionary<string, object> CreateDictionary(string name, object entityObject)
        {
            return new Dictionary<string, object>()
            {
                {
                    name, entityObject
                },
            };
        }

        [Test]
        public void TestInit()
        {
            // Check Version
            Assert.AreEqual("4", Config.Version);

            // Check Account ID
            Assert.AreEqual("1592310167", Config.AccountId);
            // Check Project ID
            Assert.AreEqual("7720880029", Config.ProjectId);
            // Check Revision
            Assert.AreEqual("15", Config.Revision);
            // Check SDK key
            Assert.AreEqual("TestData", Config.SDKKey);
            // Check Environment key
            Assert.AreEqual("Production", Config.EnvironmentKey);
            // Check SendFlagDecision
            Assert.IsTrue(Config.SendFlagDecisions);

            // Check Group ID Map
            var expectedGroupId = CreateDictionary("7722400015", Config.GetGroup("7722400015"));

            var actual = Config.GroupIdMap;
            Assert.IsTrue(TestData.CompareObjects(expectedGroupId, actual));

            // Check Experiment Key Map
            var experimentKeyMap = new Dictionary<string, object>()
            {
                {
                    "test_experiment", Config.GetExperimentFromKey("test_experiment")
                },
                {
                    "paused_experiment", Config.GetExperimentFromKey("paused_experiment")
                },
                {
                    "test_experiment_multivariate",
                    Config.GetExperimentFromKey("test_experiment_multivariate")
                },
                {
                    "test_experiment_with_feature_rollout",
                    Config.GetExperimentFromKey("test_experiment_with_feature_rollout")
                },
                {
                    "test_experiment_double_feature",
                    Config.GetExperimentFromKey("test_experiment_double_feature")
                },
                {
                    "test_experiment_integer_feature",
                    Config.GetExperimentFromKey("test_experiment_integer_feature")
                },
                {
                    "group_experiment_1", Config.GetExperimentFromKey("group_experiment_1")
                },
                {
                    "group_experiment_2", Config.GetExperimentFromKey("group_experiment_2")
                },
                {
                    "etag1", Config.GetExperimentFromKey("etag1")
                },
                {
                    "etag2", Config.GetExperimentFromKey("etag2")
                },
                {
                    "etag3", Config.GetExperimentFromKey("etag3")
                },
                {
                    "etag4", Config.GetExperimentFromKey("etag4")
                },
            };

            Assert.IsTrue(TestData.CompareObjects(experimentKeyMap, Config.ExperimentKeyMap));

            // Check Experiment ID Map

            var experimentIdMap = new Dictionary<string, object>()
            {
                {
                    "7716830082", Config.GetExperimentFromId("7716830082")
                },
                {
                    "7716830585", Config.GetExperimentFromId("7716830585")
                },
                {
                    "122230", Config.GetExperimentFromId("122230")
                },
                {
                    "122235", Config.GetExperimentFromId("122235")
                },
                {
                    "122238", Config.GetExperimentFromId("122238")
                },
                {
                    "122241", Config.GetExperimentFromId("122241")
                },
                {
                    "7723330021", Config.GetExperimentFromId("7723330021")
                },
                {
                    "7718750065", Config.GetExperimentFromId("7718750065")
                },
                {
                    "223", Config.GetExperimentFromId("223")
                },
                {
                    "118", Config.GetExperimentFromId("118")
                },
                {
                    "224", Config.GetExperimentFromId("224")
                },
                {
                    "119", Config.GetExperimentFromId("119")
                },
            };

            Assert.IsTrue(TestData.CompareObjects(experimentIdMap, Config.ExperimentIdMap));

            // Check Event key Map
            var eventKeyMap = new Dictionary<string, object>
            {
                {
                    "purchase", Config.GetEvent("purchase")
                },
            };
            Assert.IsTrue(TestData.CompareObjects(eventKeyMap, Config.EventKeyMap));

            // Check Attribute Key Map
            var attributeKeyMap = new Dictionary<string, object>
            {
                {
                    "device_type", Config.GetAttribute("device_type")
                },
                {
                    "location", Config.GetAttribute("location")
                },
                {
                    "browser_type", Config.GetAttribute("browser_type")
                },
                {
                    "boolean_key", Config.GetAttribute("boolean_key")
                },
                {
                    "integer_key", Config.GetAttribute("integer_key")
                },
                {
                    "double_key", Config.GetAttribute("double_key")
                },
            };
            Assert.IsTrue(TestData.CompareObjects(attributeKeyMap, Config.AttributeKeyMap));

            // Check Audience ID Map
            var audienceIdMap = new Dictionary<string, object>
            {
                {
                    "7718080042", Config.GetAudience("7718080042")
                },
                {
                    "11154", Config.GetAudience("11154")
                },
                {
                    "100", Config.GetAudience("100")
                },
            };
            Assert.IsTrue(TestData.CompareObjects(audienceIdMap, Config.AudienceIdMap));

            // Check Variation Key Map
            var expectedVariationKeyMap = new Dictionary<string, object>
            {
                {
                    "test_experiment", new Dictionary<string, object>
                    {
                        {
                            "control", Config.GetVariationFromKey("test_experiment", "control")
                        },
                        {
                            "variation", Config.GetVariationFromKey("test_experiment", "variation")
                        },
                    }
                },
                {
                    "paused_experiment", new Dictionary<string, object>
                    {
                        {
                            "control", Config.GetVariationFromKey("paused_experiment", "control")
                        },
                        {
                            "variation",
                            Config.GetVariationFromKey("paused_experiment", "variation")
                        },
                    }
                },
                {
                    "group_experiment_1", new Dictionary<string, object>
                    {
                        {
                            "group_exp_1_var_1",
                            Config.GetVariationFromKey("group_experiment_1", "group_exp_1_var_1")
                        },
                        {
                            "group_exp_1_var_2",
                            Config.GetVariationFromKey("group_experiment_1", "group_exp_1_var_2")
                        },
                    }
                },
                {
                    "group_experiment_2", new Dictionary<string, object>
                    {
                        {
                            "group_exp_2_var_1",
                            Config.GetVariationFromKey("group_experiment_2", "group_exp_2_var_1")
                        },
                        {
                            "group_exp_2_var_2",
                            Config.GetVariationFromKey("group_experiment_2", "group_exp_2_var_2")
                        },
                    }
                },
                {
                    "test_experiment_multivariate", new Dictionary<string, object>
                    {
                        {
                            "Fred",
                            Config.GetVariationFromKey("test_experiment_multivariate", "Fred")
                        },
                        {
                            "Feorge",
                            Config.GetVariationFromKey("test_experiment_multivariate", "Feorge")
                        },
                        {
                            "Gred",
                            Config.GetVariationFromKey("test_experiment_multivariate", "Gred")
                        },
                        {
                            "George",
                            Config.GetVariationFromKey("test_experiment_multivariate", "George")
                        },
                    }
                },
                {
                    "test_experiment_with_feature_rollout", new Dictionary<string, object>
                    {
                        {
                            "control", Config.GetVariationFromKey(
                                "test_experiment_with_feature_rollout",
                                "control")
                        },
                        {
                            "variation", Config.GetVariationFromKey(
                                "test_experiment_with_feature_rollout",
                                "variation")
                        },
                    }
                },
                {
                    "test_experiment_double_feature", new Dictionary<string, object>
                    {
                        {
                            "control",
                            Config.GetVariationFromKey("test_experiment_double_feature", "control")
                        },
                        {
                            "variation", Config.GetVariationFromKey(
                                "test_experiment_double_feature",
                                "variation")
                        },
                    }
                },
                {
                    "test_experiment_integer_feature", new Dictionary<string, object>
                    {
                        {
                            "control",
                            Config.GetVariationFromKey("test_experiment_integer_feature", "control")
                        },
                        {
                            "variation", Config.GetVariationFromKey(
                                "test_experiment_integer_feature",
                                "variation")
                        },
                    }
                },
                {
                    "177770", new Dictionary<string, object>
                    {
                        {
                            "177771", Config.GetVariationFromKey("177770", "177771")
                        },
                    }
                },
                {
                    "177772", new Dictionary<string, object>
                    {
                        {
                            "177773", Config.GetVariationFromKey("177772", "177773")
                        },
                    }
                },
                {
                    "177776", new Dictionary<string, object>
                    {
                        {
                            "177778", Config.GetVariationFromKey("177776", "177778")
                        },
                    }
                },
                {
                    "177774", new Dictionary<string, object>
                    {
                        {
                            "177775", Config.GetVariationFromKey("177774", "177775")
                        },
                    }
                },
                {
                    "177779", new Dictionary<string, object>
                    {
                        {
                            "177780", Config.GetVariationFromKey("177779", "177780")
                        },
                    }
                },
                {
                    "177781", new Dictionary<string, object>
                    {
                        {
                            "177782", Config.GetVariationFromKey("177781", "177782")
                        },
                    }
                },
                {
                    "177783", new Dictionary<string, object>
                    {
                        {
                            "177784", Config.GetVariationFromKey("177783", "177784")
                        },
                    }
                },
                {
                    "188880", new Dictionary<string, object>
                    {
                        {
                            "188881", Config.GetVariationFromKey("188880", "188881")
                        },
                    }
                },
                {
                    "etag1", new Dictionary<string, object>
                    {
                        {
                            "vtag1", Config.GetVariationFromKey("etag1", "vtag1")
                        },
                        {
                            "vtag2", Config.GetVariationFromKey("etag1", "vtag2")
                        },
                    }
                },
                {
                    "etag2", new Dictionary<string, object>
                    {
                        {
                            "vtag3", Config.GetVariationFromKey("etag2", "vtag3")
                        },
                        {
                            "vtag4", Config.GetVariationFromKey("etag2", "vtag4")
                        },
                    }
                },
                {
                    "etag3", new Dictionary<string, object>
                    {
                        {
                            "vtag5", Config.GetVariationFromKey("etag3", "vtag5")
                        },
                        {
                            "vtag6", Config.GetVariationFromKey("etag3", "vtag6")
                        },
                    }
                },
                {
                    "etag4", new Dictionary<string, object>
                    {
                        {
                            "vtag7", Config.GetVariationFromKey("etag4", "vtag7")
                        },
                        {
                            "vtag8", Config.GetVariationFromKey("etag4", "vtag8")
                        },
                    }
                },
            };

            Assert.IsTrue(TestData.CompareObjects(expectedVariationKeyMap, Config.VariationKeyMap));

            // Check Variation ID Map
            var expectedVariationIdMap = new Dictionary<string, object>
            {
                {
                    "test_experiment", new Dictionary<string, object>
                    {
                        {
                            "7722370027", Config.GetVariationFromId("test_experiment", "7722370027")
                        },
                        {
                            "7721010009", Config.GetVariationFromId("test_experiment", "7721010009")
                        },
                    }
                },
                {
                    "paused_experiment", new Dictionary<string, object>
                    {
                        {
                            "7722370427",
                            Config.GetVariationFromId("paused_experiment", "7722370427")
                        },
                        {
                            "7721010509",
                            Config.GetVariationFromId("paused_experiment", "7721010509")
                        },
                    }
                },
                {
                    "test_experiment_multivariate", new Dictionary<string, object>
                    {
                        {
                            "122231",
                            Config.GetVariationFromId("test_experiment_multivariate", "122231")
                        },
                        {
                            "122232",
                            Config.GetVariationFromId("test_experiment_multivariate", "122232")
                        },
                        {
                            "122233",
                            Config.GetVariationFromId("test_experiment_multivariate", "122233")
                        },
                        {
                            "122234",
                            Config.GetVariationFromId("test_experiment_multivariate", "122234")
                        },
                    }
                },
                {
                    "test_experiment_with_feature_rollout", new Dictionary<string, object>
                    {
                        {
                            "122236", Config.GetVariationFromId(
                                "test_experiment_with_feature_rollout",
                                "122236")
                        },
                        {
                            "122237", Config.GetVariationFromId(
                                "test_experiment_with_feature_rollout",
                                "122237")
                        },
                    }
                },
                {
                    "test_experiment_double_feature", new Dictionary<string, object>
                    {
                        {
                            "122239",
                            Config.GetVariationFromId("test_experiment_double_feature", "122239")
                        },
                        {
                            "122240",
                            Config.GetVariationFromId("test_experiment_double_feature", "122240")
                        },
                    }
                },
                {
                    "test_experiment_integer_feature", new Dictionary<string, object>
                    {
                        {
                            "122242",
                            Config.GetVariationFromId("test_experiment_integer_feature", "122242")
                        },
                        {
                            "122243",
                            Config.GetVariationFromId("test_experiment_integer_feature", "122243")
                        },
                    }
                },
                {
                    "group_experiment_1", new Dictionary<string, object>
                    {
                        {
                            "7722260071",
                            Config.GetVariationFromId("group_experiment_1", "7722260071")
                        },
                        {
                            "7722360022",
                            Config.GetVariationFromId("group_experiment_1", "7722360022")
                        },
                    }
                },
                {
                    "group_experiment_2", new Dictionary<string, object>
                    {
                        {
                            "7713030086",
                            Config.GetVariationFromId("group_experiment_2", "7713030086")
                        },
                        {
                            "7725250007",
                            Config.GetVariationFromId("group_experiment_2", "7725250007")
                        },
                    }
                },
                {
                    "177770", new Dictionary<string, object>
                    {
                        {
                            "177771", Config.GetVariationFromId("177770", "177771")
                        },
                    }
                },
                {
                    "177772", new Dictionary<string, object>
                    {
                        {
                            "177773", Config.GetVariationFromId("177772", "177773")
                        },
                    }
                },
                {
                    "177776", new Dictionary<string, object>
                    {
                        {
                            "177778", Config.GetVariationFromId("177776", "177778")
                        },
                    }
                },
                {
                    "177774", new Dictionary<string, object>
                    {
                        {
                            "177775", Config.GetVariationFromId("177774", "177775")
                        },
                    }
                },
                {
                    "177779", new Dictionary<string, object>
                    {
                        {
                            "177780", Config.GetVariationFromId("177779", "177780")
                        },
                    }
                },
                {
                    "177781", new Dictionary<string, object>
                    {
                        {
                            "177782", Config.GetVariationFromId("177781", "177782")
                        },
                    }
                },
                {
                    "177783", new Dictionary<string, object>
                    {
                        {
                            "177784", Config.GetVariationFromId("177783", "177784")
                        },
                    }
                },
                {
                    "188880", new Dictionary<string, object>
                    {
                        {
                            "188881", Config.GetVariationFromId("188880", "188881")
                        },
                    }
                },
                {
                    "etag1", new Dictionary<string, object>
                    {
                        {
                            "276", Config.GetVariationFromId("etag1", "276")
                        },
                        {
                            "277", Config.GetVariationFromId("etag1", "277")
                        },
                    }
                },
                {
                    "etag2", new Dictionary<string, object>
                    {
                        {
                            "278", Config.GetVariationFromId("etag2", "278")
                        },
                        {
                            "279", Config.GetVariationFromId("etag2", "279")
                        },
                    }
                },
                {
                    "etag3", new Dictionary<string, object>
                    {
                        {
                            "280", Config.GetVariationFromId("etag3", "280")
                        },
                        {
                            "281", Config.GetVariationFromId("etag3", "281")
                        },
                    }
                },
                {
                    "etag4", new Dictionary<string, object>
                    {
                        {
                            "282", Config.GetVariationFromId("etag4", "282")
                        },
                        {
                            "283", Config.GetVariationFromId("etag4", "283")
                        },
                    }
                },
            };

            Assert.IsTrue(TestData.CompareObjects(expectedVariationIdMap, Config.VariationIdMap));

            // Check Variation returns correct variable usage
            var featureVariableUsageInstance = new List<FeatureVariableUsage>
            {
                new FeatureVariableUsage
                {
                    Id = "155560",
                    Value = "F",
                },
                new FeatureVariableUsage
                {
                    Id = "155561",
                    Value = "red",
                },
            };

            var expectedVariationUsage = new Variation
            {
                Id = "122231",
                Key = "Fred",
                FeatureVariableUsageInstances = featureVariableUsageInstance,
                FeatureEnabled = true,
            };
            var actualVariationUsage =
                Config.GetVariationFromKey("test_experiment_multivariate", "Fred");

            Assertions.AreEqual(expectedVariationUsage, actualVariationUsage);

            // Check Feature Key map.
            var expectedFeatureKeyMap = new Dictionary<string, FeatureFlag>
            {
                {
                    "boolean_feature", Config.GetFeatureFlagFromKey("boolean_feature")
                },
                {
                    "double_single_variable_feature",
                    Config.GetFeatureFlagFromKey("double_single_variable_feature")
                },
                {
                    "integer_single_variable_feature",
                    Config.GetFeatureFlagFromKey("integer_single_variable_feature")
                },
                {
                    "boolean_single_variable_feature",
                    Config.GetFeatureFlagFromKey("boolean_single_variable_feature")
                },
                {
                    "string_single_variable_feature",
                    Config.GetFeatureFlagFromKey("string_single_variable_feature")
                },
                {
                    "multi_variate_feature", Config.GetFeatureFlagFromKey("multi_variate_feature")
                },
                {
                    "mutex_group_feature", Config.GetFeatureFlagFromKey("mutex_group_feature")
                },
                {
                    "empty_feature", Config.GetFeatureFlagFromKey("empty_feature")
                },
                {
                    "no_rollout_experiment_feature",
                    Config.GetFeatureFlagFromKey("no_rollout_experiment_feature")
                },
                {
                    "unsupported_variabletype",
                    Config.GetFeatureFlagFromKey("unsupported_variabletype")
                },
            };

            Assertions.AreEquivalent(expectedFeatureKeyMap, Config.FeatureKeyMap);

            // Check Feature Key map.
            var expectedRolloutIdMap = new Dictionary<string, Rollout>
            {
                {
                    "166660", Config.GetRolloutFromId("166660")
                },
                {
                    "166661", Config.GetRolloutFromId("166661")
                },
            };

            Assert.IsTrue(TestData.CompareObjects(expectedRolloutIdMap, Config.RolloutIdMap));
        }

        [Test]
        public void TestFlagVariations()
        {
            var allVariations = Config?.FlagVariationMap;

            var expectedVariationDict = new Dictionary<string, Variation>
            {
                {
                    "group_exp_1_var_1", new Variation
                    {
                        FeatureEnabled = true,
                        Id = "7722260071",
                        Key = "group_exp_1_var_1",
                        FeatureVariableUsageInstances = new List<FeatureVariableUsage>
                        {
                            new FeatureVariableUsage
                            {
                                Id = "155563",
                                Value = "groupie_1_v1",
                            },
                        },
                    }
                },
                {
                    "group_exp_1_var_2", new Variation
                    {
                        FeatureEnabled = true,
                        Id = "7722360022",
                        Key = "group_exp_1_var_2",
                        FeatureVariableUsageInstances = new List<FeatureVariableUsage>
                        {
                            new FeatureVariableUsage
                            {
                                Id = "155563",
                                Value = "groupie_1_v2",
                            },
                        },
                    }
                },
                {
                    "group_exp_2_var_1", new Variation
                    {
                        FeatureEnabled = false,
                        Id = "7713030086",
                        Key = "group_exp_2_var_1",
                        FeatureVariableUsageInstances = new List<FeatureVariableUsage>
                        {
                            new FeatureVariableUsage
                            {
                                Id = "155563",
                                Value = "groupie_2_v1",
                            },
                        },
                    }
                },
                {
                    "group_exp_2_var_2", new Variation
                    {
                        FeatureEnabled = false,
                        Id = "7725250007",
                        Key = "group_exp_2_var_2",
                        FeatureVariableUsageInstances = new List<FeatureVariableUsage>
                        {
                            new FeatureVariableUsage
                            {
                                Id = "155563",
                                Value = "groupie_2_v2",
                            },
                        },
                    }
                },
            };
            var filteredActualFlagVariations = allVariations["boolean_feature"];
            TestData.CompareObjects(expectedVariationDict, filteredActualFlagVariations);
        }

        [Test]
        public void TestIfSendFlagDecisionKeyIsMissingItShouldReturnFalse()
        {
            var tempConfig = DatafileProjectConfig.Create(TestData.SimpleABExperimentsDatafile,
                LoggerMock.Object, ErrorHandlerMock.Object);
            Assert.IsFalse(tempConfig.SendFlagDecisions);
        }

        [Test]
        public void TestGetAccountId()
        {
            Assert.AreEqual("1592310167", Config.AccountId);
        }

        [Test]
        public void TestGetProjectId()
        {
            Assert.AreEqual("7720880029", Config.ProjectId);
        }

        [Test]
        public void TestGetGroupValidId()
        {
            var group = Config.GetGroup("7722400015");
            Assert.AreEqual("7722400015", group.Id);
            Assert.AreEqual("random", group.Policy);
        }

        [Test]
        public void TestGetGroupInvalidId()
        {
            var group = Config.GetGroup("invalid_id");

            LoggerMock.Verify(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<string>()),
                Times.Exactly(1));
            LoggerMock.Verify(l =>
                l.Log(LogLevel.ERROR, @"Group ID ""invalid_id"" is not in datafile."));

            ErrorHandlerMock.Verify(e => e.HandleError(
                    It.Is<InvalidGroupException>(ex =>
                        ex.Message == "Provided group is not in datafile.")),
                Times.Once, "Failed");

            Assert.IsTrue(TestData.CompareObjects(group, new Group()));
        }

        [Test]
        public void TestGetExperimentValidKey()
        {
            var experiment = Config.GetExperimentFromKey("test_experiment");
            Assert.AreEqual("test_experiment", experiment.Key);
            Assert.AreEqual("7716830082", experiment.Id);
        }

        [Test]
        public void TestGetExperimentInvalidKey()
        {
            var experiment = Config.GetExperimentFromKey("invalid_key");

            LoggerMock.Verify(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<string>()), Times.Once);
            LoggerMock.Verify(l =>
                l.Log(LogLevel.ERROR, @"Experiment key ""invalid_key"" is not in datafile."));

            ErrorHandlerMock.Verify(e =>
                e.HandleError(It.Is<InvalidExperimentException>(ex =>
                    ex.Message == "Provided experiment is not in datafile.")));

            Assert.IsTrue(TestData.CompareObjects(new Experiment(), experiment));
        }

        [Test]
        public void TestGetExperimentValidId()
        {
            var experiment = Config.GetExperimentFromId("7716830082");
            Assert.AreEqual("7716830082", experiment.Id);
            Assert.AreEqual("test_experiment", experiment.Key);
        }

        [Test]
        public void TestGetExperimentInvalidId()
        {
            var experiment = Config.GetExperimentFromId("42");

            LoggerMock.Verify(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<string>()), Times.Once);
            LoggerMock.Verify(l =>
                l.Log(LogLevel.ERROR, @"Experiment ID ""42"" is not in datafile."));

            ErrorHandlerMock.Verify(e =>
                e.HandleError(It.Is<InvalidExperimentException>(ex =>
                    ex.Message == "Provided experiment is not in datafile.")));

            Assert.IsTrue(TestData.CompareObjects(new Experiment(), experiment));
        }

        [Test]
        public void TestGetEventValidKey()
        {
            var ev = Config.GetEvent("purchase");
            Assert.AreEqual("purchase", ev.Key);
            Assert.AreEqual("7718020063", ev.Id);

            Assert.IsTrue(TestData.CompareObjects(new object[]
            {
                "7716830082", "7723330021", "7718750065", "7716830585",
            }, ev.ExperimentIds));
        }

        [Test]
        public void TestGetEventInvalidKey()
        {
            var ev = Config.GetEvent("invalid_key");

            LoggerMock.Verify(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<string>()), Times.Once);
            LoggerMock.Verify(l =>
                l.Log(LogLevel.ERROR, @"Event key ""invalid_key"" is not in datafile."));

            ErrorHandlerMock.Verify(e =>
                e.HandleError(It.Is<InvalidEventException>(ex =>
                    ex.Message == "Provided event is not in datafile.")));

            Assert.IsTrue(TestData.CompareObjects(new Entity.Event(), ev));
        }

        [Test]
        public void TestGetAudienceValidId()
        {
            var audience = Config.GetAudience("7718080042");

            Assert.AreEqual("7718080042", audience.Id);
            Assert.AreEqual("iPhone users in San Francisco", audience.Name);
        }

        [Test]
        public void TestGetAudienceInvalidKey()
        {
            var audience = Config.GetAudience("invalid_id");

            LoggerMock.Verify(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<string>()), Times.Once);
            LoggerMock.Verify(l =>
                l.Log(LogLevel.ERROR, @"Audience ID ""invalid_id"" is not in datafile."));

            ErrorHandlerMock.Verify(e =>
                e.HandleError(It.Is<InvalidAudienceException>(ex =>
                    ex.Message == "Provided audience is not in datafile.")));
            Assert.IsTrue(TestData.CompareObjects(new Audience(), audience));
        }

        [Test]
        public void TestGetAttributeValidKey()
        {
            var attribute = Config.GetAttribute("device_type");

            Assert.AreEqual("device_type", attribute.Key);
            Assert.AreEqual("7723280020", attribute.Id);
        }

        [Test]
        public void TestGetAttributeInvalidKey()
        {
            var attribute = Config.GetAttribute("invalid_key");

            LoggerMock.Verify(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<string>()), Times.Once);
            LoggerMock.Verify(l =>
                l.Log(LogLevel.ERROR, @"Attribute key ""invalid_key"" is not in datafile."));

            ErrorHandlerMock.Verify(e =>
                e.HandleError(It.Is<InvalidAttributeException>(ex =>
                    ex.Message == "Provided attribute is not in datafile.")));
            Assert.AreEqual(new Entity.Attribute(), attribute);
        }

        /// <summary>
        /// EK = Experiment Key
        /// VK = Variation Key
        /// </summary>
        [Test]
        public void TestGetVariationFromKeyValidEKValidVK()
        {
            var variation = Config.GetVariationFromKey("test_experiment", "control");

            Assert.AreEqual("7722370027", variation.Id);
            Assert.AreEqual("control", variation.Key);
        }

        /// <summary>
        /// EK = Experiment Key
        /// VK = Variation Key
        /// </summary>
        [Test]
        public void TestGetVariationFromKeyValidEKInvalidVK()
        {
            var variation = Config.GetVariationFromKey("test_experiment", "invalid_key");

            LoggerMock.Verify(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<string>()), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.ERROR,
                @"No variation key ""invalid_key"" defined in datafile for experiment ""test_experiment""."));

            ErrorHandlerMock.Verify(e =>
                e.HandleError(It.Is<InvalidVariationException>(ex =>
                    ex.Message == "Provided variation is not in datafile.")));

            Assert.AreEqual(new Variation(), variation);
        }

        [Test]
        public void TestGetVariationFromKeyInvalidEK()
        {
            var variation = Config.GetVariationFromKey("invalid_experiment", "control");

            LoggerMock.Verify(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<string>()), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.ERROR,
                @"No variation key ""control"" defined in datafile for experiment ""invalid_experiment""."));

            ErrorHandlerMock.Verify(e =>
                e.HandleError(It.Is<InvalidVariationException>(ex =>
                    ex.Message == "Provided variation is not in datafile.")));
            Assert.AreEqual(new Variation(), variation);
        }

        [Test]
        public void TestGetVariationFromIdValidEKValidVId()
        {
            var variation = Config.GetVariationFromId("test_experiment", "7722370027");
            Assert.AreEqual("control", variation.Key);
            Assert.AreEqual("7722370027", variation.Id);
        }

        [Test]
        public void TestGetVariationFromIdValidEKInvalidVId()
        {
            var variation = Config.GetVariationFromId("test_experiment", "invalid_id");

            LoggerMock.Verify(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<string>()), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.ERROR,
                @"No variation ID ""invalid_id"" defined in datafile for experiment ""test_experiment""."));

            ErrorHandlerMock.Verify(e =>
                e.HandleError(It.Is<InvalidVariationException>(ex =>
                    ex.Message == "Provided variation is not in datafile.")));
            Assert.AreEqual(new Variation(), variation);
        }

        [Test]
        public void TestGetVariationFromIdInvalidEK()
        {
            var variation = Config.GetVariationFromId("invalid_experiment", "7722370027");

            LoggerMock.Verify(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<string>()), Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.ERROR,
                @"No variation ID ""7722370027"" defined in datafile for experiment ""invalid_experiment""."));

            ErrorHandlerMock.Verify(e =>
                e.HandleError(It.Is<InvalidVariationException>(ex =>
                    ex.Message == "Provided variation is not in datafile.")));
            Assert.AreEqual(new Variation(), variation);
        }

        [Test]
        public void TempProjectConfigTest()
        {
            var config = DatafileProjectConfig.Create(TestData.Datafile,
                new Mock<ILogger>().Object, new DefaultErrorHandler());
            Assert.IsNotNull(config);
            Assert.AreEqual("1592310167", config.AccountId);
        }

        // Test that getDatafile returns the expected datafile.
        [Test]
        public void TestProjectConfigDatafileIsSame()
        {
            var config = DatafileProjectConfig.Create(TestData.Datafile,
                new Mock<ILogger>().Object, new DefaultErrorHandler());
            Assert.AreEqual(config.ToDatafile(), TestData.Datafile);
        }

        // test set/get forced variation for the following cases:
        //      - valid and invalid user ID
        //      - valid and invalid experiment key
        //      - valid and invalid variation key, null variation key

        [Test]
        public void TestVariationFeatureEnabledProperty()
        {
            // Verify that featureEnabled property of variation is false if not defined.
            var variation = Config.GetVariationFromKey("test_experiment", "control");
            Assert.IsFalse(variation.IsFeatureEnabled);
        }

        [Test]
        public void TestBotFilteringValues()
        {
            // Verify that bot filtering value is true as defined in Config data.
            Assert.True(Config.BotFiltering.GetValueOrDefault());

            // Remove botFilering node and verify returned value in null.
            var projConfig = JObject.Parse(TestData.Datafile);
            if (projConfig.TryGetValue("botFiltering", out var token))
            {
                projConfig.Property("botFiltering").Remove();
                var configWithoutBotFilter = DatafileProjectConfig.Create(
                    JsonConvert.SerializeObject(projConfig),
                    LoggerMock.Object, ErrorHandlerMock.Object);

                // Verify that bot filtering is null when not defined in datafile.
                Assert.Null(configWithoutBotFilter.BotFiltering);
            }
        }

        [Test]
        public void TestGetAttributeIdWithReservedPrefix()
        {
            // Verify that attribute key is returned for reserved attribute key.
            Assert.AreEqual(Config.GetAttributeId(ControlAttributes.USER_AGENT_ATTRIBUTE),
                ControlAttributes.USER_AGENT_ATTRIBUTE);

            // Verify that attribute Id is returned for attribute key with reserved prefix that does not exist in datafile.
            Assert.AreEqual(Config.GetAttributeId("$opt_reserved_prefix_attribute"),
                "$opt_reserved_prefix_attribute");

            // Create config file copy with additional resered prefix attribute.
            var reservedPrefixAttrKey = "$opt_user_defined_attribute";
            var projConfig = JObject.Parse(TestData.Datafile);
            var attributes = (JArray)projConfig["attributes"];

            var reservedAttr = new Entity.Attribute
            {
                Id = "7723348204",
                Key = reservedPrefixAttrKey,
            };
            attributes.Add((JObject)JToken.FromObject(reservedAttr));

            // Verify that attribute Id is returned and warning is logged for attribute key with reserved prefix that exists in datafile.
            var reservedAttrConfig = DatafileProjectConfig.Create(
                JsonConvert.SerializeObject(projConfig), LoggerMock.Object,
                ErrorHandlerMock.Object);
            Assert.AreEqual(reservedAttrConfig.GetAttributeId(reservedPrefixAttrKey),
                reservedAttrConfig.GetAttribute(reservedPrefixAttrKey).Id);
            LoggerMock.Verify(l => l.Log(LogLevel.WARN,
                $@"Attribute {reservedPrefixAttrKey} unexpectedly has reserved prefix {DatafileProjectConfig.RESERVED_ATTRIBUTE_PREFIX}; using attribute ID instead of reserved attribute name."));
        }

        [Test]
        public void TestGetAttributeIdWithInvalidAttributeKey()
        {
            // Verify that null is returned when provided attribute key is invalid.
            Assert.Null(Config.GetAttributeId("invalid_attribute"));
            LoggerMock.Verify(l => l.Log(LogLevel.ERROR,
                @"Attribute key ""invalid_attribute"" is not in datafile."));
        }

        [Test]
        public void TestCreateThrowsWithNullDatafile()
        {
            var exception =
                Assert.Throws<ConfigParseException>(() =>
                    DatafileProjectConfig.Create(null, null, null));
            Assert.AreEqual("Unable to parse null datafile.", exception.Message);
        }

        [Test]
        public void TestCreateThrowsWithEmptyDatafile()
        {
            var exception =
                Assert.Throws<ConfigParseException>(() =>
                    DatafileProjectConfig.Create("", null, null));
            Assert.AreEqual("Unable to parse empty datafile.", exception.Message);
        }

        [Test]
        public void TestCreateThrowsWithUnsupportedDatafileVersion()
        {
            var exception = Assert.Throws<ConfigParseException>(() =>
                DatafileProjectConfig.Create(TestData.UnsupportedVersionDatafile, null, null));
            Assert.AreEqual(
                $"This version of the C# SDK does not support the given datafile version: 5",
                exception.Message);
        }

        [Test]
        public void TestCreateDoesNotThrowWithValidDatafile()
        {
            Assert.DoesNotThrow(() => DatafileProjectConfig.Create(TestData.Datafile, null, null));
        }

        [Test]
        public void TestExperimentAudiencesRetrivedFromTypedAudiencesFirstThenFromAudiences()
        {
            var typedConfig =
                DatafileProjectConfig.Create(TestData.TypedAudienceDatafile, null, null);
            var experiment = typedConfig.GetExperimentFromKey("feat_with_var_test");

            var expectedAudienceIds = new string[]
            {
                "3468206642", "3988293898", "3988293899", "3468206646", "3468206647", "3468206644",
                "3468206643",
            };
            Assert.That(expectedAudienceIds, Is.EquivalentTo(experiment.AudienceIds));
        }

        [Test]
        public void TestIsFeatureExperimentReturnsFalseForExperimentThatDoesNotBelongToAnyFeature()
        {
            var typedConfig =
                DatafileProjectConfig.Create(TestData.TypedAudienceDatafile, null, null);
            var experiment = typedConfig.GetExperimentFromKey("typed_audience_experiment");

            Assert.False(typedConfig.IsFeatureExperiment(experiment.Id));
        }

        [Test]
        public void TestIsFeatureExperimentReturnsTrueForExperimentThatBelongsToAFeature()
        {
            var typedConfig =
                DatafileProjectConfig.Create(TestData.TypedAudienceDatafile, null, null);
            var experiment = typedConfig.GetExperimentFromKey("feat2_with_var_test");

            Assert.True(typedConfig.IsFeatureExperiment(experiment.Id));
        }

        [Test]
        public void TestRolloutWithEmptyStringRolloutIdFromConfigFile()
        {
            var projectConfig =
                DatafileProjectConfig.Create(TestData.EmptyRolloutDatafile, null, null);
            Assert.IsNotNull(projectConfig);
            var featureFlag = projectConfig.FeatureKeyMap["empty_rollout_id"];

            var rollout = projectConfig.GetRolloutFromId(featureFlag.RolloutId);

            Assert.IsNull(rollout.Experiments);
            Assert.IsNull(rollout.Id);
        }

        [Test]
        public void TestRolloutWithEmptyStringRolloutId()
        {
            var rolloutId = string.Empty;

            var rollout = Config.GetRolloutFromId(rolloutId);

            Assert.IsNull(rollout.Experiments);
            Assert.IsNull(rollout.Id);
        }

        [Test]
        public void TestRolloutWithConsistingOfASingleSpaceRolloutId()
        {
            var rolloutId = " "; // single space

            var rollout = Config.GetRolloutFromId(rolloutId);

            Assert.IsNull(rollout.Experiments);
            Assert.IsNull(rollout.Id);
        }

        [Test]
        public void TestRolloutWithConsistingOfANullRolloutId()
        {
            string nullRolloutId = null;

            var rollout = Config.GetRolloutFromId(nullRolloutId);

            Assert.IsNull(rollout.Experiments);
            Assert.IsNull(rollout.Id);
        }

        private const string ZAIUS_HOST = "https://api.zaius.com";
        private const string ZAIUS_PUBLIC_KEY = "fake-public-key";

        [Test]
        public void TestProjectConfigWithOdpIntegration()
        {
            var datafileProjectConfig = DatafileProjectConfig.Create(
                TestData.OdpIntegrationDatafile, new NoOpLogger(),
                new NoOpErrorHandler());

            Assert.AreEqual(ZAIUS_HOST, datafileProjectConfig.HostForOdp);
            Assert.AreEqual(ZAIUS_PUBLIC_KEY, datafileProjectConfig.PublicKeyForOdp);
        }

        [Test]
        public void TestProjectConfigWithOdpIntegrationIncludesOtherFields()
        {
            var datafileProjectConfig = DatafileProjectConfig.Create(
                TestData.OdpIntegrationWithOtherFieldsDatafile, new NoOpLogger(),
                new NoOpErrorHandler());

            Assert.AreEqual(ZAIUS_HOST, datafileProjectConfig.HostForOdp);
            Assert.AreEqual(ZAIUS_PUBLIC_KEY, datafileProjectConfig.PublicKeyForOdp);
        }

        [Test]
        public void TestProjectConfigWithEmptyIntegrationCollection()
        {
            var datafileProjectConfig = DatafileProjectConfig.Create(
                TestData.EmptyIntegrationDatafile, new NoOpLogger(),
                new NoOpErrorHandler());

            Assert.IsNull(datafileProjectConfig.HostForOdp);
            Assert.IsNull(datafileProjectConfig.PublicKeyForOdp);
        }

        [Test]
        public void TestProjectConfigWithOtherIntegrationsInCollection()
        {
            var datafileProjectConfig = DatafileProjectConfig.Create(
                TestData.NonOdpIntegrationDatafile, new NoOpLogger(),
                new NoOpErrorHandler());

            Assert.IsNull(datafileProjectConfig.HostForOdp);
            Assert.IsNull(datafileProjectConfig.PublicKeyForOdp);
        }

        #region Holdout Integration Tests

        [Test]
        public void TestHoldoutDeserialization_FromDatafile()
        {
            // Test that holdouts can be deserialized from a datafile with holdouts
            var testDataPath = Path.Combine(TestContext.CurrentContext.TestDirectory,
                "TestData", "HoldoutTestData.json");
            var jsonContent = File.ReadAllText(testDataPath);
            var testData = JObject.Parse(jsonContent);

            var datafileJson = testData["datafileWithHoldouts"].ToString();

            var datafileProjectConfig = DatafileProjectConfig.Create(datafileJson,
                new NoOpLogger(), new NoOpErrorHandler()) as DatafileProjectConfig;

            Assert.IsNotNull(datafileProjectConfig.Holdouts);
            Assert.AreEqual(4, datafileProjectConfig.Holdouts.Length);
        }

        [Test]
        public void TestGetHoldoutsForFlag_Integration()
        {
            var testDataPath = Path.Combine(TestContext.CurrentContext.TestDirectory,
                "TestData", "HoldoutTestData.json");
            var jsonContent = File.ReadAllText(testDataPath);
            var testData = JObject.Parse(jsonContent);

            var datafileJson = testData["datafileWithHoldouts"].ToString();

            var datafileProjectConfig = DatafileProjectConfig.Create(datafileJson,
                new NoOpLogger(), new NoOpErrorHandler()) as DatafileProjectConfig;

            // Test GetHoldoutsForFlag method
            var holdoutsForFlag1 = datafileProjectConfig.GetHoldoutsForFlag("flag_1");
            Assert.IsNotNull(holdoutsForFlag1);
            Assert.AreEqual(4, holdoutsForFlag1.Length); // Global + excluded holdout (applies to all except flag_3/flag_4) + included holdout + empty holdout

            var holdoutsForFlag3 = datafileProjectConfig.GetHoldoutsForFlag("flag_3");
            Assert.IsNotNull(holdoutsForFlag3);
            Assert.AreEqual(2, holdoutsForFlag3.Length); // Global + empty holdout (excluded holdout excludes flag_3, included holdout doesn't include flag_3)

            var holdoutsForUnknownFlag = datafileProjectConfig.GetHoldoutsForFlag("unknown_flag");
            Assert.IsNotNull(holdoutsForUnknownFlag);
            Assert.AreEqual(3, holdoutsForUnknownFlag.Length); // Global + excluded holdout (unknown_flag not in excluded list) + empty holdout
        }

        [Test]
        public void TestGetHoldout_Integration()
        {
            var testDataPath = Path.Combine(TestContext.CurrentContext.TestDirectory,
                "TestData", "HoldoutTestData.json");
            var jsonContent = File.ReadAllText(testDataPath);
            var testData = JObject.Parse(jsonContent);

            var datafileJson = testData["datafileWithHoldouts"].ToString();

            var datafileProjectConfig = DatafileProjectConfig.Create(datafileJson,
                new NoOpLogger(), new NoOpErrorHandler()) as DatafileProjectConfig;

            // Test GetHoldout method
            var globalHoldout = datafileProjectConfig.GetHoldout("holdout_global_1");
            Assert.IsNotNull(globalHoldout);
            Assert.AreEqual("holdout_global_1", globalHoldout.Id);
            Assert.AreEqual("global_holdout", globalHoldout.Key);

            var invalidHoldout = datafileProjectConfig.GetHoldout("invalid_id");
            Assert.IsNull(invalidHoldout);
        }

        [Test]
        public void TestMissingHoldoutsField_BackwardCompatibility()
        {
            // Test that a datafile without holdouts field still works
            var datafileWithoutHoldouts = @"{
                ""version"": ""4"",
                ""rollouts"": [],
                ""projectId"": ""test_project"",
                ""experiments"": [],
                ""groups"": [],
                ""attributes"": [],
                ""audiences"": [],
                ""layers"": [],
                ""events"": [],
                ""revision"": ""1"",
                ""featureFlags"": []
            }";

            var datafileProjectConfig = DatafileProjectConfig.Create(datafileWithoutHoldouts,
                new NoOpLogger(), new NoOpErrorHandler()) as DatafileProjectConfig;

            Assert.IsNotNull(datafileProjectConfig.Holdouts);
            Assert.AreEqual(0, datafileProjectConfig.Holdouts.Length);

            // Methods should still work with empty holdouts
            var holdouts = datafileProjectConfig.GetHoldoutsForFlag("any_flag");
            Assert.IsNotNull(holdouts);
            Assert.AreEqual(0, holdouts.Length);

            var holdout = datafileProjectConfig.GetHoldout("any_id");
            Assert.IsNull(holdout);
        }

        #endregion

        [Test]
        public void TestCmabFieldPopulation()
        {

            var datafileJson = JObject.Parse(TestData.Datafile);
            var experiments = (JArray)datafileJson["experiments"];

            if (experiments.Count > 0)
            {
                var firstExperiment = (JObject)experiments[0];

                firstExperiment["cmab"] = new JObject
                {
                    ["attributeIds"] = new JArray { "7723280020", "7723348204" },
                    ["trafficAllocation"] = 4000
                };

                firstExperiment["trafficAllocation"] = new JArray();
            }

            var modifiedDatafile = datafileJson.ToString();
            var projectConfig = DatafileProjectConfig.Create(modifiedDatafile, LoggerMock.Object, ErrorHandlerMock.Object);
            var experimentWithCmab = projectConfig.GetExperimentFromKey("test_experiment");

            Assert.IsNotNull(experimentWithCmab.Cmab);
            Assert.AreEqual(2, experimentWithCmab.Cmab.AttributeIds.Count);
            Assert.Contains("7723280020", experimentWithCmab.Cmab.AttributeIds);
            Assert.Contains("7723348204", experimentWithCmab.Cmab.AttributeIds);
            Assert.AreEqual(4000, experimentWithCmab.Cmab.TrafficAllocation);

            var experimentWithoutCmab = projectConfig.GetExperimentFromKey("paused_experiment");

            Assert.IsNull(experimentWithoutCmab.Cmab);
        }
    }
}
