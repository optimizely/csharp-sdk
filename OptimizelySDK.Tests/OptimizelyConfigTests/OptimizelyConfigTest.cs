/*
 * Copyright 2020-2021, Optimizely
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
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using OptimizelySDK.Config;
using OptimizelySDK.Entity;
using OptimizelySDK.Logger;
using OptimizelySDK.OptlyConfig;
using OptimizelySDK.Tests.UtilsTests;
using System;
using System.Collections.Generic;

namespace OptimizelySDK.Tests.OptimizelyConfigTests
{
    [TestFixture]
    public class OptimizelyConfigTest
    {
        private Mock<ILogger> LoggerMock;

        [SetUp]
        public void Setup()
        {
            LoggerMock = new Mock<ILogger>();
            LoggerMock.Setup(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<string>()));
        }

        #region Test OptimizelyConfigService

        private static Type[] ParameterTypes = {
                typeof(ProjectConfig),
            };

        private PrivateObject CreatePrivateOptimizelyConfigService(ProjectConfig projectConfig)
        {
            return new PrivateObject(typeof(OptimizelyConfigService), ParameterTypes,
                    new object[]
                    {
                        projectConfig
                    });
        }

        [Test]
        public void TestGetOptimizelyConfigServiceSerializedAudiences()
        {
            var datafileProjectConfig = DatafileProjectConfig.Create(TestData.TypedAudienceDatafile, new NoOpLogger(), new ErrorHandler.NoOpErrorHandler());
            var optlyConfigService = CreatePrivateOptimizelyConfigService(datafileProjectConfig);

            var audienceConditions = new List<List<object>>
            {
                new List<object>() { "or", "3468206642", "3988293898" },
                new List<object>() { "or", "3468206642", "3988293898", "3468206646" },
                new List<object>() { "not", "3468206642" },
                new List<object>() { "or", "3468206642" },
                new List<object>() { "and", "3468206642" },
                new List<object>() { "3468206642" },
                new List<object>() { "3468206642", "3988293898" },
                new List<object>() { "and", new JArray() { "or", "3468206642", "3988293898" }, "3468206646" },
                new List<object>() { "and", new JArray() { "or", "3468206642", new JArray() { "and", "3988293898", "3468206646" } }, new JArray() { "and", "3988293899", new JArray() { "or", "3468206647", "3468206643" } } },
                new List<object>() { "and", "and" },
                new List<object>() { "not", new JArray() { "and", "3468206642", "3988293898" } },
                new List<object>() { },
                new List<object>() { "or", "3468206642", "999999999" },
            };

            var expectedAudienceOutputs = new List<string>
            {
                "\"exactString\" OR \"substringString\"",
                "\"exactString\" OR \"substringString\" OR \"exactNumber\"",
                "NOT \"exactString\"",
                "\"exactString\"",
                "\"exactString\"",
                "\"exactString\"",
                "\"exactString\" OR \"substringString\"",
                "(\"exactString\" OR \"substringString\") AND \"exactNumber\"",
                "(\"exactString\" OR (\"substringString\" AND \"exactNumber\")) AND (\"exists\" AND (\"gtNumber\" OR \"exactBoolean\"))",
                "",
                "NOT (\"exactString\" AND \"substringString\")",
                "",
                "\"exactString\" OR \"999999999\"",
            };

            for (int testNo = 0; testNo < audienceConditions.Count; testNo++)
            {
                var result = (string)optlyConfigService.Invoke("GetSerializedAudiences", audienceConditions[testNo], datafileProjectConfig.AudienceIdMap);
                Assert.AreEqual(result, expectedAudienceOutputs[testNo]);
            }
        }

        [Test]
        public void TestAfterDisposeGetOptimizelyConfigIsNoLongerValid()
        {
            var httpManager = new HttpProjectConfigManager.Builder()
                                                          .WithSdkKey("QBw9gFM8oTn7ogY9ANCC1z")
                                                          .WithDatafile(TestData.Datafile)
                                                          .WithPollingInterval(TimeSpan.FromMilliseconds(50000))
                                                          .WithBlockingTimeoutPeriod(TimeSpan.FromMilliseconds(500))
                                                          .Build(true);
            var optimizely = new Optimizely(httpManager);
            httpManager.Start();

            var optimizelyConfig = optimizely.GetOptimizelyConfig();

            Assert.NotNull(optimizelyConfig);
            Assert.NotNull(optimizelyConfig.ExperimentsMap);
            Assert.NotNull(optimizelyConfig.FeaturesMap);
            Assert.NotNull(optimizelyConfig.Revision);

            optimizely.Dispose();

            var optimizelyConfigAfterDispose = optimizely.GetOptimizelyConfig();
            Assert.Null(optimizelyConfigAfterDispose);
        }

        [Test]
        public void TestGetOptimizelyConfigServiceNullConfig()
        {
            OptimizelyConfig optimizelyConfig = new OptimizelyConfigService(null).GetOptimizelyConfig();
            Assert.IsNull(optimizelyConfig);
        }

        [Test]
        public void TestGetOptimizelyConfigWithDuplicateExperimentKeys()
        {
            var datafileProjectConfig = DatafileProjectConfig.Create(TestData.DuplicateExpKeysDatafile, new NoOpLogger(), new ErrorHandler.NoOpErrorHandler());
            var optimizelyConfigService = new OptimizelyConfigService(datafileProjectConfig);
            var optimizelyConfig = optimizelyConfigService.GetOptimizelyConfig();
            Assert.AreEqual(optimizelyConfig.ExperimentsMap.Count, 1);

            var experimentMapFlag1 = optimizelyConfig.FeaturesMap["flag1"].ExperimentsMap; //9300000007569
            var experimentMapFlag2 = optimizelyConfig.FeaturesMap["flag2"].ExperimentsMap; // 9300000007573
            Assert.AreEqual(experimentMapFlag1["targeted_delivery"].Id, "9300000007569");
            Assert.AreEqual(experimentMapFlag2["targeted_delivery"].Id, "9300000007573");
        }

        [Test]
        public void TestGetOptimizelyConfigWithDuplicateRuleKeys()
        {
            var datafileProjectConfig = DatafileProjectConfig.Create(TestData.DuplicateRuleKeysDatafile, new NoOpLogger(), new ErrorHandler.NoOpErrorHandler());
            var optimizelyConfigService = new OptimizelyConfigService(datafileProjectConfig);
            var optimizelyConfig = optimizelyConfigService.GetOptimizelyConfig();
            Assert.AreEqual(optimizelyConfig.ExperimentsMap.Count, 0);

            var rolloutFlag1 = optimizelyConfig.FeaturesMap["flag_1"].DeliveryRules[0]; // 9300000004977,
            var rolloutFlag2 = optimizelyConfig.FeaturesMap["flag_2"].DeliveryRules[0]; // 9300000004979
            var rolloutFlag3 = optimizelyConfig.FeaturesMap["flag_3"].DeliveryRules[0]; // 9300000004981
            Assert.AreEqual(rolloutFlag1.Id, "9300000004977");
            Assert.AreEqual(rolloutFlag1.Key, "targeted_delivery");
            Assert.AreEqual(rolloutFlag2.Id, "9300000004979");
            Assert.AreEqual(rolloutFlag2.Key, "targeted_delivery");
            Assert.AreEqual(rolloutFlag3.Id, "9300000004981");
            Assert.AreEqual(rolloutFlag3.Key, "targeted_delivery");
        }

        [Test]
        public void TestGetOptimizelyConfigSDKAndEnvironmentKeyDefault()
        {
            var datafileProjectConfig = DatafileProjectConfig.Create(TestData.DuplicateRuleKeysDatafile, new NoOpLogger(), new ErrorHandler.NoOpErrorHandler());
            var optimizelyConfigService = new OptimizelyConfigService(datafileProjectConfig);
            var optimizelyConfig = optimizelyConfigService.GetOptimizelyConfig();

            Assert.AreEqual(optimizelyConfig.SDKKey, "");
            Assert.AreEqual(optimizelyConfig.EnvironmentKey, "");
        }

        [Test]
        public void TestGetOptimizelyConfigService()
        {
            var datafileProjectConfig = DatafileProjectConfig.Create(TestData.TypedAudienceDatafile, new NoOpLogger(), new ErrorHandler.NoOpErrorHandler());
            var expectedflagVariations = new Dictionary<string, List<Variation>> {
                {
                    "feat_no_vars",
                    new List<Variation> {
                        new Variation
                        {
                            FeatureEnabled = true,
                            FeatureVariableUsageInstances = new List<FeatureVariableUsage>(),
                            Key = "11557362669",
                            Id = "11557362669",
                            VariableIdToVariableUsageInstanceMap = new Dictionary<string, FeatureVariableUsage>()
                        },
                        new Variation
                        {
                            FeatureEnabled = false,
                            FeatureVariableUsageInstances = new List<FeatureVariableUsage>(),
                            Key = "11475708558",
                            Id = "11475708558",
                            VariableIdToVariableUsageInstanceMap = new Dictionary<string, FeatureVariableUsage>()
                        },
                        new Variation
                        {
                            FeatureEnabled = true,
                            FeatureVariableUsageInstances = new List<FeatureVariableUsage>(),
                            Key = "11557362670",
                            Id = "11557362670",
                            VariableIdToVariableUsageInstanceMap = new Dictionary<string, FeatureVariableUsage>()
                        },
                        new Variation
                        {
                            FeatureEnabled = false,
                            FeatureVariableUsageInstances = new List<FeatureVariableUsage>(),
                            Key = "11475708559",
                            Id = "11475708559",
                            VariableIdToVariableUsageInstanceMap = new Dictionary<string, FeatureVariableUsage>()
                        }
                    }
                },
                {
                    "feat_with_var",
                    new List<Variation> {
                        new Variation
                        {
                            FeatureEnabled = true,
                            FeatureVariableUsageInstances = new List<FeatureVariableUsage>(),
                            Key = "11557362669",
                            Id = "11557362669",
                            VariableIdToVariableUsageInstanceMap = new Dictionary<string, FeatureVariableUsage>()
                        },
                        new Variation
                        {
                            FeatureEnabled = false,
                            FeatureVariableUsageInstances = new List<FeatureVariableUsage>(),
                            Key = "11475708558",
                            Id = "11475708558",
                            VariableIdToVariableUsageInstanceMap = new Dictionary<string, FeatureVariableUsage>()
                        },
                        new Variation
                        {
                            FeatureEnabled = true,
                            FeatureVariableUsageInstances = new List<FeatureVariableUsage>(),
                            Key = "11557362670",
                            Id = "11557362670",
                            VariableIdToVariableUsageInstanceMap = new Dictionary<string, FeatureVariableUsage>()
                        },
                        new Variation
                        {
                            FeatureEnabled = false,
                            FeatureVariableUsageInstances = new List<FeatureVariableUsage>(),
                            Key = "11475708559",
                            Id = "11475708559",
                            VariableIdToVariableUsageInstanceMap = new Dictionary<string, FeatureVariableUsage>()
                        }
                    }
                },
                {
                    "feat2",
                    new List<Variation> {
                        new Variation
                        {
                            FeatureEnabled = true,
                            FeatureVariableUsageInstances = new List<FeatureVariableUsage>(),
                            Key = "11557362669",
                            Id = "11557362669",
                            VariableIdToVariableUsageInstanceMap = new Dictionary<string, FeatureVariableUsage>()
                        },
                        new Variation
                        {
                            FeatureEnabled = false,
                            FeatureVariableUsageInstances = new List<FeatureVariableUsage>(),
                            Key = "11475708558",
                            Id = "11475708558",
                            VariableIdToVariableUsageInstanceMap = new Dictionary<string, FeatureVariableUsage>()
                        },
                        new Variation
                        {
                            FeatureEnabled = true,
                            FeatureVariableUsageInstances = new List<FeatureVariableUsage>(),
                            Key = "11557362670",
                            Id = "11557362670",
                            VariableIdToVariableUsageInstanceMap = new Dictionary<string, FeatureVariableUsage>()
                        },
                        new Variation
                        {
                            FeatureEnabled = false,
                            FeatureVariableUsageInstances = new List<FeatureVariableUsage>(),
                            Key = "11475708559",
                            Id = "11475708559",
                            VariableIdToVariableUsageInstanceMap = new Dictionary<string, FeatureVariableUsage>()
                        }
                    }
                },
                {
                    "feat2_with_var",
                    new List<Variation> {
                        new Variation
                        {
                            FeatureEnabled = true,
                            FeatureVariableUsageInstances = new List<FeatureVariableUsage>(),
                            Key = "11557362669",
                            Id = "11557362669",
                            VariableIdToVariableUsageInstanceMap = new Dictionary<string, FeatureVariableUsage>()
                        },
                        new Variation
                        {
                            FeatureEnabled = false,
                            FeatureVariableUsageInstances = new List<FeatureVariableUsage>(),
                            Key = "11475708558",
                            Id = "11475708558",
                            VariableIdToVariableUsageInstanceMap = new Dictionary<string, FeatureVariableUsage>()
                        },
                        new Variation
                        {
                            FeatureEnabled = true,
                            FeatureVariableUsageInstances = new List<FeatureVariableUsage>(),
                            Key = "11557362670",
                            Id = "11557362670",
                            VariableIdToVariableUsageInstanceMap = new Dictionary<string, FeatureVariableUsage>()
                        },
                        new Variation
                        {
                            FeatureEnabled = false,
                            FeatureVariableUsageInstances = new List<FeatureVariableUsage>(),
                            Key = "11475708559",
                            Id = "11475708559",
                            VariableIdToVariableUsageInstanceMap = new Dictionary<string, FeatureVariableUsage>()
                        }
                    }
                }
            };
            IDictionary<string, OptimizelyExperiment> experimentsMap = new Dictionary<string, OptimizelyExperiment>
            {
                {
                    "feat_with_var_test", new OptimizelyExperiment (
                        id: "11564051718",
                        key:"feat_with_var_test",
                        audiences: "",
                        variationsMap: new Dictionary<string, OptimizelyVariation>
                        {
                            {
                                "variation_2", new OptimizelyVariation (
                                    id: "11617170975",
                                    key: "variation_2",
                                    featureEnabled: true,
                                    variablesMap: new Dictionary<string, OptimizelyVariable>
                                    {
                                        {
                                            "x" , new OptimizelyVariable (
                                                id: "11535264366",
                                                key: "x",
                                                type: "string",
                                                value: "xyz")
                                        }
                                    })
                            }
                        }
                    )
                },
                {
                    "typed_audience_experiment", new OptimizelyExperiment (
                        id: "1323241597",
                        key:"typed_audience_experiment",
                        audiences: "",
                        variationsMap: new Dictionary<string, OptimizelyVariation>
                        {
                            {
                                "A", new OptimizelyVariation (
                                    id: "1423767503",
                                    key: "A",
                                    featureEnabled: null,
                                    variablesMap: new Dictionary<string, OptimizelyVariable> ())
                            }
                        }
                    )
                },
                {
                    "audience_combinations_experiment", new OptimizelyExperiment (
                        id: "1323241598",
                        key:"audience_combinations_experiment",
                        audiences: "(\"exactString\" OR \"substringString\") AND (\"exists\" OR \"exactNumber\" OR \"gtNumber\" OR \"ltNumber\" OR \"exactBoolean\")",
                        variationsMap: new Dictionary<string, OptimizelyVariation>
                        {
                            {
                                "A", new OptimizelyVariation (
                                    id: "1423767504",
                                    key: "A",
                                    featureEnabled: null,
                                    variablesMap: new Dictionary<string, OptimizelyVariable> ())
                            }
                        }
                    )
                },
                {
                    "feat2_with_var_test", new OptimizelyExperiment(
                        id: "1323241599",
                        key:"feat2_with_var_test",
                        audiences: "(\"exactString\" OR \"substringString\") AND (\"exists\" OR \"exactNumber\" OR \"gtNumber\" OR \"ltNumber\" OR \"exactBoolean\")",
                        variationsMap: new Dictionary<string, OptimizelyVariation>
                        {
                            {
                                "variation_2", new OptimizelyVariation (
                                    id: "1423767505",
                                    key: "variation_2",
                                    featureEnabled: true,
                                    variablesMap: new Dictionary<string, OptimizelyVariable>
                                    {
                                        {
                                            "z" , new OptimizelyVariable (
                                                id: "11535264367",
                                                key: "z",
                                                type: "integer",
                                                value: "150")
                                        }
                                    })
                            }
                        }
                    )
                }
            };

            var featuresMap = new Dictionary<string, OptimizelyFeature>
            {
                {
                    "feat_no_vars", new OptimizelyFeature (
                        id: "11477755619",
                        key: "feat_no_vars",
                        experimentRules: new List<OptimizelyExperiment>(),
                        deliveryRules: new List<OptimizelyExperiment>() { new OptimizelyExperiment(
                                            id: "11488548027",
                                            key:"feat_no_vars_rule",
                                            audiences: "",
                                            variationsMap: new Dictionary<string, OptimizelyVariation>
                                            {
                                                {
                                                    "11557362669", new OptimizelyVariation (
                                                        id: "11557362669",
                                                        key: "11557362669",
                                                        featureEnabled: true,
                                                        variablesMap: new Dictionary<string, OptimizelyVariable>())
                                                }
                                            }
                                        ) },
                        experimentsMap: new Dictionary<string, OptimizelyExperiment>(),
                        variablesMap: new Dictionary<string, OptimizelyVariable>())
                },
                {
                    "feat_with_var", new OptimizelyFeature (
                        id: "11567102051",
                        key: "feat_with_var",
                        experimentRules: new List<OptimizelyExperiment>() {
                            new OptimizelyExperiment(
                                    id: "11564051718",
                                    key:"feat_with_var_test",
                                    audiences: "",
                                    variationsMap: new Dictionary<string, OptimizelyVariation>
                                    {
                                        {
                                            "variation_2", new OptimizelyVariation (
                                                id: "11617170975",
                                                key: "variation_2",
                                                featureEnabled: true,
                                                variablesMap: new Dictionary<string, OptimizelyVariable>
                                                {
                                                    {
                                                        "x" , new OptimizelyVariable (
                                                            id: "11535264366",
                                                            key: "x",
                                                            type: "string",
                                                            value: "xyz")
                                                    }
                                                })
                                        }
                                    }
                                )
                        },
                        deliveryRules: new List<OptimizelyExperiment>() { new OptimizelyExperiment(
                                            id: "11630490911",
                                            key:"feat_with_var_rule",
                                            audiences: "",
                                            variationsMap: new Dictionary<string, OptimizelyVariation>
                                            {
                                                {
                                                    "11475708558", new OptimizelyVariation (
                                                        id: "11475708558",
                                                        key: "11475708558",
                                                        featureEnabled: false,
                                                        variablesMap: new Dictionary<string, OptimizelyVariable>()
                                                        {
                                                            { "x" , new OptimizelyVariable("11535264366", "x", "string", "x")  }
                                                        })
                                                }
                                            }
                                        ) },
                        experimentsMap: new Dictionary<string, OptimizelyExperiment>
                        {
                            {
                                "feat_with_var_test", new OptimizelyExperiment(
                                    id: "11564051718",
                                    key:"feat_with_var_test",
                                    audiences: "",
                                    variationsMap: new Dictionary<string, OptimizelyVariation>
                                    {
                                        {
                                            "variation_2", new OptimizelyVariation (
                                                id: "11617170975",
                                                key: "variation_2",
                                                featureEnabled: true,
                                                variablesMap: new Dictionary<string, OptimizelyVariable>
                                                {
                                                    {
                                                        "x" , new OptimizelyVariable (
                                                            id: "11535264366",
                                                            key: "x",
                                                            type: "string",
                                                            value: "xyz")
                                                    }
                                                })
                                        }
                                    }
                                )
                            }
                        },
                        variablesMap: new Dictionary<string, OptimizelyVariable>
                        {
                            {
                                "x", new OptimizelyVariable (id: "11535264366" , key: "x", type: "string", value: "x")
                            }
                        })
                },
                {
                    "feat2", new OptimizelyFeature (
                        id: "11567102052",
                        key: "feat2",
                        deliveryRules: new List<OptimizelyExperiment>() { new OptimizelyExperiment(
                                            id: "11488548028",
                                            key:"11488548028",
                                            audiences: "(\"exactString\" OR \"substringString\") AND (\"exists\" OR \"exactNumber\" OR \"gtNumber\" OR \"ltNumber\" OR \"exactBoolean\")",
                                            variationsMap: new Dictionary<string, OptimizelyVariation>
                                            {
                                                {
                                                    "11557362670", new OptimizelyVariation (
                                                        id: "11557362670",
                                                        key: "11557362670",
                                                        featureEnabled: true,
                                                        variablesMap: new Dictionary<string, OptimizelyVariable>()
                                                        )
                                                }
                                            }
                                        ) },
                        experimentRules: new List<OptimizelyExperiment>(),
                        experimentsMap: new Dictionary<string, OptimizelyExperiment>(),
                        variablesMap: new Dictionary<string, OptimizelyVariable>())
                },
                {
                    "feat2_with_var", new OptimizelyFeature (
                        id: "11567102053",
                        key: "feat2_with_var",
                        deliveryRules: new List<OptimizelyExperiment>()
                        {
                            new OptimizelyExperiment(
                                            id: "11630490912",
                                            key:"11630490912",
                                            audiences: "",
                                            variationsMap: new Dictionary<string, OptimizelyVariation>
                                            {
                                                {
                                                    "11475708559", new OptimizelyVariation (
                                                        id: "11475708559",
                                                        key: "11475708559",
                                                        featureEnabled: false,
                                                        variablesMap: new Dictionary<string, OptimizelyVariable>()
                                                        {
                                                            {
                                                                "z" , new OptimizelyVariable (
                                                                    id: "11535264367",
                                                                    key: "z",
                                                                    type: "integer",
                                                                    value: "10")
                                                            }
                                                        })
                                                }
                                            }
                                        )
                        },
                        experimentRules: new List<OptimizelyExperiment>()
                        {
                            new OptimizelyExperiment (
                                    id: "1323241599",
                                    key:"feat2_with_var_test",
                                    audiences: "(\"exactString\" OR \"substringString\") AND (\"exists\" OR \"exactNumber\" OR \"gtNumber\" OR \"ltNumber\" OR \"exactBoolean\")",
                                    variationsMap: new Dictionary<string, OptimizelyVariation>
                                    {
                                        {
                                            "variation_2", new OptimizelyVariation (
                                                id: "1423767505",
                                                key: "variation_2",
                                                featureEnabled: true,
                                                variablesMap: new Dictionary<string, OptimizelyVariable>
                                                {
                                                    {
                                                        "z" , new OptimizelyVariable (
                                                            id: "11535264367",
                                                            key: "z",
                                                            type: "integer",
                                                            value: "150")
                                                    }
                                                })
                                        }
                                    }
                                )
                        },
                        experimentsMap: new Dictionary<string, OptimizelyExperiment>
                        {
                            {
                                "feat2_with_var_test", new OptimizelyExperiment (
                                    id: "1323241599",
                                    key:"feat2_with_var_test",
                                    audiences: "(\"exactString\" OR \"substringString\") AND (\"exists\" OR \"exactNumber\" OR \"gtNumber\" OR \"ltNumber\" OR \"exactBoolean\")",
                                    variationsMap: new Dictionary<string, OptimizelyVariation>
                                    {
                                        {
                                            "variation_2", new OptimizelyVariation (
                                                id: "1423767505",
                                                key: "variation_2",
                                                featureEnabled: true,
                                                variablesMap: new Dictionary<string, OptimizelyVariable>
                                                {
                                                    {
                                                        "z" , new OptimizelyVariable (
                                                            id: "11535264367",
                                                            key: "z",
                                                            type: "integer",
                                                            value: "150")
                                                    }
                                                })
                                        }
                                    }
                                )
                            }
                        },
                        variablesMap: new Dictionary<string, OptimizelyVariable>
                        {
                            {
                                "z", new OptimizelyVariable (id: "11535264367" , key: "z", type: "integer", value: "10")
                            }
                        })
                }
            };

            OptimizelyConfig optimizelyConfig = new OptimizelyConfigService(datafileProjectConfig).GetOptimizelyConfig();
            OptimizelyConfig expectedOptimizelyConfig = new OptimizelyConfig(datafileProjectConfig.Revision,
                datafileProjectConfig.SDKKey,
                datafileProjectConfig.EnvironmentKey,
                attributes: new OptimizelyAttribute[]
                {
                    new OptimizelyAttribute
                    {
                       Id = "594015", Key = "house"
                    },
                    new OptimizelyAttribute
                    {
                       Id = "594016", Key = "lasers"
                    },
                    new OptimizelyAttribute
                    {
                       Id = "594017", Key = "should_do_it"
                    },
                    new OptimizelyAttribute
                    {
                       Id = "594018", Key = "favorite_ice_cream"
                    }
                },
                audiences: new OptimizelyAudience[]
                {
                    new OptimizelyAudience("0", "$$dummy", "{\"type\": \"custom_attribute\", \"name\": \"$opt_dummy_attribute\", \"value\": \"impossible_value\"}"),
                    new OptimizelyAudience("3468206643", "exactBoolean", "[\"and\",[\"or\",[\"or\",{\"name\":\"should_do_it\",\"type\":\"custom_attribute\",\"match\":\"exact\",\"value\":true}]]]"),
                    new OptimizelyAudience("3468206646", "exactNumber", "[\"and\",[\"or\",[\"or\",{\"name\":\"lasers\",\"type\":\"custom_attribute\",\"match\":\"exact\",\"value\":45.5}]]]"),
                    new OptimizelyAudience("3468206642", "exactString", "[\"and\", [\"or\", [\"or\", {\"name\": \"house\", \"type\": \"custom_attribute\", \"value\": \"Gryffindor\"}]]]"),
                    new OptimizelyAudience("3988293899", "exists", "[\"and\",[\"or\",[\"or\",{\"name\":\"favorite_ice_cream\",\"type\":\"custom_attribute\",\"match\":\"exists\"}]]]"),
                    new OptimizelyAudience("3468206647", "gtNumber", "[\"and\",[\"or\",[\"or\",{\"name\":\"lasers\",\"type\":\"custom_attribute\",\"match\":\"gt\",\"value\":70}]]]"),
                    new OptimizelyAudience("3468206644", "ltNumber", "[\"and\",[\"or\",[\"or\",{\"name\":\"lasers\",\"type\":\"custom_attribute\",\"match\":\"lt\",\"value\":1.0}]]]"),
                    new OptimizelyAudience("3468206645", "notChrome", "[\"and\", [\"or\", [\"not\", [\"or\", {\"name\": \"browser_type\", \"type\": \"custom_attribute\", \"value\":\"Chrome\"}]]]]"),
                    new OptimizelyAudience("3468206648", "notExist", "[\"not\",{\"name\":\"input_value\",\"type\":\"custom_attribute\",\"match\":\"exists\"}]"),
                    new OptimizelyAudience("3988293898", "substringString", "[\"and\",[\"or\",[\"or\",{\"name\":\"house\",\"type\":\"custom_attribute\",\"match\":\"substring\",\"value\":\"Slytherin\"}]]]"),
                },
                events: new OptimizelyEvent[]
                {
                    new OptimizelyEvent()
                    {
                       Id = "594089", Key = "item_bought", ExperimentIds = new string[] { "11564051718", "1323241597" }
                    },
                    new OptimizelyEvent()
                    {
                       Id = "594090", Key = "user_signed_up", ExperimentIds = new string[] { "1323241598", "1323241599" }
                    }
                },
                experimentsMap: experimentsMap,
                featuresMap: featuresMap,
                datafile: TestData.TypedAudienceDatafile);

            Assertions.AreEqual(expectedOptimizelyConfig, optimizelyConfig);
        }

        #endregion Test OptimizelyConfigService

        #region OptimizelyConfig entity tests

        [Test]
        public void TestOptimizelyConfigEntity()
        {
            OptimizelyConfig expectedOptlyFeature = new OptimizelyConfig("123",
                "testSdkKey",
                "Development",
                attributes: new OptimizelyAttribute[0],
                audiences: new OptimizelyAudience[0],
                events: new OptimizelyEvent[0],
                experimentsMap: new Dictionary<string, OptimizelyExperiment>(),
                featuresMap: new Dictionary<string, OptimizelyFeature>()
                );
            Assert.AreEqual(expectedOptlyFeature.Revision, "123");
            Assert.AreEqual(expectedOptlyFeature.SDKKey, "testSdkKey");
            Assert.AreEqual(expectedOptlyFeature.EnvironmentKey, "Development");
            Assert.AreEqual(expectedOptlyFeature.Attributes, new Entity.Attribute[0]);
            Assert.AreEqual(expectedOptlyFeature.Audiences, new OptimizelyAudience[0]);
            Assert.AreEqual(expectedOptlyFeature.Events, new Entity.Event[0]);
            Assert.AreEqual(expectedOptlyFeature.ExperimentsMap, new Dictionary<string, OptimizelyExperiment>());
            Assert.AreEqual(expectedOptlyFeature.FeaturesMap, new Dictionary<string, OptimizelyFeature>());
        }

        [Test]
        public void TestOptimizelyFeatureEntity()
        {
            OptimizelyFeature expectedOptlyFeature = new OptimizelyFeature("1", "featKey",
                new List<OptimizelyExperiment>(),
                new List<OptimizelyExperiment>(),
                new Dictionary<string, OptimizelyExperiment>(),
                new Dictionary<string, OptimizelyVariable>()
                );
            Assert.AreEqual(expectedOptlyFeature.Id, "1");
            Assert.AreEqual(expectedOptlyFeature.Key, "featKey");
            Assert.AreEqual(expectedOptlyFeature.ExperimentRules, new List<OptimizelyExperiment>());
            Assert.AreEqual(expectedOptlyFeature.DeliveryRules, new List<OptimizelyExperiment>());
            Assert.AreEqual(expectedOptlyFeature.Key, "featKey");
            Assert.AreEqual(expectedOptlyFeature.ExperimentsMap, new Dictionary<string, OptimizelyExperiment>());
            Assert.AreEqual(expectedOptlyFeature.VariablesMap, new Dictionary<string, OptimizelyVariable>());
        }

        [Test]
        public void TestOptimizelyExperimentEntity()
        {
            OptimizelyExperiment expectedOptlyExp = new OptimizelyExperiment("1", "exKey",
                "",
                new Dictionary<string, OptimizelyVariation> {
                    {
                        "varKey", new OptimizelyVariation("1", "varKey", true, new Dictionary<string, OptimizelyVariable>())
                    }
                });
            Assert.AreEqual(expectedOptlyExp.Id, "1");
            Assert.AreEqual(expectedOptlyExp.Key, "exKey");
            Assert.AreEqual(expectedOptlyExp.Audiences, "");
            Assert.AreEqual(expectedOptlyExp.VariationsMap["varKey"], new OptimizelyVariation("1", "varKey", true, new Dictionary<string, OptimizelyVariable>()));
        }

        [Test]
        public void TestOptimizelyVariationEntity()
        {
            OptimizelyVariation expectedOptlyVariation = new OptimizelyVariation("1", "varKey", true, new Dictionary<string, OptimizelyVariable> {
                { "variableKey", new OptimizelyVariable("varId", "variableKey", "integer", "2")}
            });
            Assert.AreEqual(expectedOptlyVariation.Id, "1");
            Assert.AreEqual(expectedOptlyVariation.Key, "varKey");
            Assert.AreEqual(expectedOptlyVariation.FeatureEnabled, true);
            Assert.AreEqual(expectedOptlyVariation.VariablesMap["variableKey"], new OptimizelyVariable("varId", "variableKey", "integer", "2"));
        }

        [Test]
        public void TestOptimizelyVariableEntity()
        {
            OptimizelyVariable expectedOptlyVariable = new OptimizelyVariable("varId", "variableKey", "integer", "2");
            Assert.AreEqual(expectedOptlyVariable.Id, "varId");
            Assert.AreEqual(expectedOptlyVariable.Key, "variableKey");
            Assert.AreEqual(expectedOptlyVariable.Type, "integer");
            Assert.AreEqual(expectedOptlyVariable.Value, "2");
        }

        #endregion OptimizelyConfig entity tests
    }
}
