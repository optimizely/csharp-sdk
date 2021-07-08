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

using System;
using Moq;
using NUnit.Framework;
using OptimizelySDK.Config;
using OptimizelySDK.Logger;
using OptimizelySDK.OptlyConfig;
using System.Collections.Generic;
using System.Threading;

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
        public void TestPollingGivenOnlySdkKeyGetOptimizelyConfig()
        {
            HttpProjectConfigManager httpManager = new HttpProjectConfigManager.Builder()
               .WithSdkKey("QBw9gFM8oTn7ogY9ANCC1z")
               .WithLogger(LoggerMock.Object)
               .WithPollingInterval(TimeSpan.FromMilliseconds(1000))
               .WithBlockingTimeoutPeriod(TimeSpan.FromMilliseconds(500))
               .WithStartByDefault()
               .Build(true);

            Assert.NotNull(httpManager.GetConfig());

            var optimizely = new Optimizely(httpManager);

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
        public void TestPollingMultipleTimesGetOptimizelyConfig()
        {
            HttpProjectConfigManager httpManager = new HttpProjectConfigManager.Builder()
               .WithSdkKey("QBw9gFM8oTn7ogY9ANCC1z")
               .WithLogger(LoggerMock.Object)
               .WithPollingInterval(TimeSpan.FromMilliseconds(100))
               .WithBlockingTimeoutPeriod(TimeSpan.FromMilliseconds(500))
               .WithStartByDefault()
               .Build(true);

            Assert.NotNull(httpManager.GetConfig());

            var optimizely = new Optimizely(httpManager);

            var optimizelyConfig = optimizely.GetOptimizelyConfig();

            Assert.NotNull(optimizelyConfig);
            Assert.NotNull(optimizelyConfig.ExperimentsMap);
            Assert.NotNull(optimizelyConfig.FeaturesMap);
            Assert.NotNull(optimizelyConfig.Revision);

            Thread.Sleep(210);

            optimizelyConfig = optimizely.GetOptimizelyConfig();

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
        public void TestGetOptimizelyConfigService()
        {
            var datafileProjectConfig = DatafileProjectConfig.Create(TestData.TypedAudienceDatafile, new NoOpLogger(), new ErrorHandler.NoOpErrorHandler());
            IDictionary<string, OptimizelyExperiment> experimentsMap = new Dictionary<string, OptimizelyExperiment>
            {
                {
                    "feat_with_var_test", new OptimizelyExperiment (
                        id: "11564051718",
                        key:"feat_with_var_test",
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
                        experimentsMap: new Dictionary<string, OptimizelyExperiment>(),
                        variablesMap: new Dictionary<string, OptimizelyVariable>())
                },
                {
                    "feat_with_var", new OptimizelyFeature (
                        id: "11567102051",
                        key: "feat_with_var",
                        experimentsMap: new Dictionary<string, OptimizelyExperiment>
                        {
                            {
                                "feat_with_var_test", new OptimizelyExperiment(
                                    id: "11564051718",
                                    key:"feat_with_var_test",
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
                        experimentsMap: new Dictionary<string, OptimizelyExperiment>(),
                        variablesMap: new Dictionary<string, OptimizelyVariable>())
                },
                {
                    "feat2_with_var", new OptimizelyFeature (
                        id: "11567102053",
                        key: "feat2_with_var",
                        experimentsMap: new Dictionary<string, OptimizelyExperiment>
                        {
                            {
                                "feat2_with_var_test", new OptimizelyExperiment (
                                    id: "1323241599",
                                    key:"feat2_with_var_test",
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
            OptimizelyConfig expectedOptimizelyConfig = new OptimizelyConfig(datafileProjectConfig.Revision, datafileProjectConfig.SDKKey, datafileProjectConfig.EnvironmentKey, experimentsMap, featuresMap);
            Assert.IsTrue(TestData.CompareObjects(optimizelyConfig, expectedOptimizelyConfig));
        }

        #endregion

        #region OptimizelyConfig entity tests

        [Test]
        public void TestOptimizelyConfigEntity()
        {
            OptimizelyConfig expectedOptlyFeature = new OptimizelyConfig("123",
                "testSdkKey",
                "Development",
                new Dictionary<string, OptimizelyExperiment>(),
                new Dictionary<string, OptimizelyFeature>()
                );
            Assert.AreEqual(expectedOptlyFeature.Revision, "123");
            Assert.AreEqual(expectedOptlyFeature.SDKKey, "testSdkKey");
            Assert.AreEqual(expectedOptlyFeature.EnvironmentKey, "Development");
            Assert.AreEqual(expectedOptlyFeature.ExperimentsMap, new Dictionary<string, OptimizelyExperiment>());
            Assert.AreEqual(expectedOptlyFeature.FeaturesMap, new Dictionary<string, OptimizelyFeature>());
        }

        [Test]
        public void TestOptimizelyFeatureEntity()
        {
            OptimizelyFeature expectedOptlyFeature = new OptimizelyFeature("1", "featKey",
                new Dictionary<string, OptimizelyExperiment>(),
                new Dictionary<string, OptimizelyVariable>()
                );
            Assert.AreEqual(expectedOptlyFeature.Id, "1");
            Assert.AreEqual(expectedOptlyFeature.Key, "featKey");
            Assert.AreEqual(expectedOptlyFeature.ExperimentsMap, new Dictionary<string, OptimizelyExperiment>());
            Assert.AreEqual(expectedOptlyFeature.VariablesMap, new Dictionary<string, OptimizelyVariable>());
        }

        [Test]
        public void TestOptimizelyExperimentEntity()
        {
            OptimizelyExperiment expectedOptlyExp = new OptimizelyExperiment("1", "exKey",
                new Dictionary<string, OptimizelyVariation> {
                    {
                        "varKey", new OptimizelyVariation("1", "varKey", true, new Dictionary<string, OptimizelyVariable>())
                    }
                });
            Assert.AreEqual(expectedOptlyExp.Id, "1");
            Assert.AreEqual(expectedOptlyExp.Key, "exKey");
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

        #endregion
    }
}
