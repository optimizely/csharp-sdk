/*
 * Copyright 2022, Optimizely
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
using NUnit.Framework;
using OptimizelySDK.Entity;
using OptimizelySDK.Logger;
using OptimizelySDK.OptimizelyDecisions;
using OptimizelySDK.OptlyConfig;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace OptimizelySDK.Tests
{
    /// <summary>
    /// Simplifies assertions and provides more insight into which particular piece is failing for a value.
    /// especially helpful for Test Driven Development
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class AssertionTest
    {
        private Mock<ILogger> LoggerMock;

        [SetUp]
        public void Initialize()
        {
            LoggerMock = new Mock<ILogger>();
        }

        #region Basic asserts

        [Test]
        public void HasItemsTestNotNullButEqualNumberOfItems()
        {
            var expected = new string[] { "abc", "def" };
            var actual = new string[] { "abc", "ghi" };

            Assert.IsTrue(Assertions.HasItems(expected, actual, false));
        }

        [Test]
        public void AreEquivalentTestBothAreEqual()
        {
            var expected = new string[] { "abc", "def" };
            var actual = new string[] { "abc", "def" };

            Assertions.AreEquivalent(expected, actual);
        }

        [Test]
        public void AreEquivalentDictTestBothAreEqual()
        {
            Dictionary<string, string> expected = new Dictionary<string, string> { { "a", "valA" }, { "b", "valB" } };
            Dictionary<string, string> actual = new Dictionary<string, string> { { "a", "valA" }, { "b", "valB" } };

            Assertions.AreEquivalent(expected, actual);
        }

        [Test]
        public void AreEqualOptimizelyJSONTest()
        {
            OptimizelyJSON expected = new OptimizelyJSON("{\"abc\":\"def\"}", null, LoggerMock.Object);
            OptimizelyJSON actual = new OptimizelyJSON("{\"abc\":\"def\"}", null, LoggerMock.Object);
            Assertions.AreEqual(expected, actual);
        }

        #endregion Basic asserts

        #region

        [Test]
        public static void AreEqualOptimizelyForcedDecisionTest()
        {
            var expected = new OptimizelyForcedDecision("varKey");
            var actual = new OptimizelyForcedDecision("varKey");
            Assertions.AreEqual(expected, actual);
        }

        #endregion

        #region OptimizelyAttribute

        [Test]
        public void AreEquivalentOptimizelyAttributeTest()
        {
            var expected = new OptimizelyAttribute[] { new OptimizelyAttribute() { Id = "1", Key = "thisKey" } };
            var actual = new OptimizelyAttribute[] { new OptimizelyAttribute() { Id = "1", Key = "thisKey" } };
            Assertions.AreEquivalent(expected, actual);
        }

        [Test]
        public void AreEqualOptimizelyAttributeTest()
        {
            var expected = new OptimizelyAttribute() { Id = "1", Key = "thisKey" };
            var actual = new OptimizelyAttribute() { Id = "1", Key = "thisKey" };
            Assertions.AreEqual(expected, actual);
        }

        #endregion OptimizelyAttribute

        #region OptimizelyConfig

        [Test]
        public void AreEqualOptimizelyConfigTest()
        {
            OptimizelyConfig expected = new OptimizelyConfig("1", "sdkKey", "envKey", new OptimizelyAttribute[0], new OptimizelyAudience[0], new OptimizelyEvent[0],
                new Dictionary<string, OptimizelyExperiment>
                {
                    { "1",
                        new OptimizelyExperiment("1", "ex1", "", new Dictionary<string, OptimizelyVariation>
                        {
                            { "var1", new OptimizelyVariation("var1", "varKey", false, null) }
                        })
                    }
                },
                new Dictionary<string, OptimizelyFeature>
                {
                    {
                        "2",
                        new OptimizelyFeature("2", "feat2", new List<OptimizelyExperiment> {
                            { new OptimizelyExperiment("1", "ex1", "", new Dictionary<string, OptimizelyVariation>
                                {
                                    { "var1", new OptimizelyVariation("var1", "varKey", false, null) }
                                })
                            }
                        },
                        new List<OptimizelyExperiment> {
                            { new OptimizelyExperiment("1", "ex1", "", new Dictionary<string, OptimizelyVariation>
                                {
                                    { "var1", new OptimizelyVariation("var1", "varKey", false, null) }
                                })
                            }
                        }, new Dictionary<string, OptimizelyExperiment> {
                            { "ex1" , new OptimizelyExperiment("1", "ex1", "", new Dictionary<string, OptimizelyVariation>
                                {
                                    { "var1", new OptimizelyVariation("var1", "varKey", false, null) }
                                })
                            } 
                        }, new Dictionary<string, OptimizelyVariable>
                            {
                                { "s", new OptimizelyVariable("1", "varKKey", "1", "2")  }
                            }
                        )
                    }
                }
                );
            OptimizelyConfig actual = new OptimizelyConfig("1", "sdkKey", "envKey", new OptimizelyAttribute[0], new OptimizelyAudience[0], new OptimizelyEvent[0],
                new Dictionary<string, OptimizelyExperiment>
                {
                    { "1",
                        new OptimizelyExperiment("1", "ex1", "", new Dictionary<string, OptimizelyVariation>
                        {
                            { "var1", new OptimizelyVariation("var1", "varKey", false, null) }
                        })
                    }
                },
                new Dictionary<string, OptimizelyFeature>
                {
                    {
                        "2",
                        new OptimizelyFeature("2", "feat2", new List<OptimizelyExperiment> {
                            { new OptimizelyExperiment("1", "ex1", "", new Dictionary<string, OptimizelyVariation>
                                {
                                    { "var1", new OptimizelyVariation("var1", "varKey", false, null) }
                                })
                            }
                        },
                        new List<OptimizelyExperiment> {
                            { new OptimizelyExperiment("1", "ex1", "", new Dictionary<string, OptimizelyVariation>
                                {
                                    { "var1", new OptimizelyVariation("var1", "varKey", false, null) }
                                })
                            }
                        }, new Dictionary<string, OptimizelyExperiment> {
                            { "ex1" , new OptimizelyExperiment("1", "ex1", "", new Dictionary<string, OptimizelyVariation>
                                {
                                    { "var1", new OptimizelyVariation("var1", "varKey", false, null) }
                                })
                            }
                        }, new Dictionary<string, OptimizelyVariable>
                            {
                                { "s", new OptimizelyVariable("1", "varKKey", "1", "2")  }
                            }
                        )
                    }
                }
                );
            Assertions.AreEqual(expected, actual);
        }

        #endregion OptimizelyConfig

        #region OptimizelyUserContext

        [Test]
        public static void AreEqual()
        {
            OptimizelyUserContext expected = new OptimizelyUserContext(null, "test",
                new UserAttributes { { "1", "test" } }, null, null); 
            OptimizelyUserContext actual = new OptimizelyUserContext(null, "test",
                new UserAttributes { { "1", "test" } }, null, null); ;
            Assertions.AreEqual(expected, actual);
        }

        #endregion

        #region OptimizelyEvent

        [Test]
        public static void AreEquivalent()
        {
            OptimizelyEvent[] expected = new OptimizelyEvent[] { new OptimizelyEvent() { ExperimentIds = new string[] { "1", "2" }, Id = "1", Key = "keyEvent" } };
            OptimizelyEvent[] actual = new OptimizelyEvent[] { new OptimizelyEvent() { ExperimentIds = new string[] { "1", "2" }, Id = "1", Key = "keyEvent" } };
            Assertions.AreEquivalent(expected, actual);
        }

        #endregion OptimizelyEvent

        #region OptimizelyDecision

        [Test]
        public static void AreEqualOptimizelyDecision()
        {
            OptimizelyDecision expected = new OptimizelyDecision("varKey", false, null, "testRuleKey", "testFlagKey", null, new string[0]);
            OptimizelyDecision actual = new OptimizelyDecision("varKey", false, null, "testRuleKey", "testFlagKey", null, new string[0]);
            Assertions.AreEqual(expected, actual);
        }

        #endregion

    }
}
