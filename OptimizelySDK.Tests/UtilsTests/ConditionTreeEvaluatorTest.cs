/* 
 * Copyright 2019, Optimizely
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
using OptimizelySDK.Utils;

namespace OptimizelySDK.Tests.UtilsTests
{
    /// <summary>
    /// Interface for mocking Evaluate method.
    /// </summary>
    public interface ILeafEvaluator
    {
        bool? Evaluate(JToken condition);
    }

    [TestFixture]
    public class ConditionTreeEvaluatorTest
    {
        private Mock<ILeafEvaluator> LeafEvaluatorMock;
        private ConditionTreeEvaluator ConditionTreeEvaluator = null;

        private object[] AndConditions = null;
        private object[] OrConditions = null;
        private object[] NotCondition = null;
        private object[] NoOperatorConditions = null;

        [TestFixtureSetUp]
        public void Initialize()
        {
            ConditionTreeEvaluator = new ConditionTreeEvaluator();
            LeafEvaluatorMock = new Mock<ILeafEvaluator>();
            LeafEvaluatorMock.Setup(le => le.Evaluate(It.IsAny<JToken>()));

            string NoOperatorConditionsStr = @"[{""name"": ""device_type"", ""type"": ""custom_attribute"", ""value"": ""iPhone""}, {""name"": ""num_users"", ""type"": ""custom_attribute"", ""value"": 15, ""match"": ""exact""}]";
            string NotConditionStr = @"[""not"", [""or"", [""or"", {""name"": ""device_type"", ""type"": ""custom_attribute"", ""value"": ""iPhone"", ""match"": ""exact""}]]]";
            string AndConditionStr = @"[""and"", 
                                        [""or"", [""or"", {""name"": ""device_type"", ""type"": ""custom_attribute"", ""value"": ""iPhone"", ""match"": ""substring""}]], 
                                        [""or"", [""or"", {""name"": ""num_users"", ""type"": ""custom_attribute"", ""value"": 15, ""match"": ""exact""}]], 
                                        [""or"", [""or"", {""name"": ""decimal_value"", ""type"": ""custom_attribute"", ""value"": 3.14, ""match"": ""gt""}]]
                                       ]";
            string OrConditionStr = @"[""or"", 
                                        [""or"", [""or"", {""name"": ""device_type"", ""type"": ""custom_attribute"", ""value"": ""iPhone"", ""match"": ""substring""}]], 
                                        [""or"", [""or"", {""name"": ""num_users"", ""type"": ""custom_attribute"", ""value"": 15, ""match"": ""exact""}]], 
                                        [""or"", [""or"", {""name"": ""decimal_value"", ""type"": ""custom_attribute"", ""value"": 3.14, ""match"": ""gt""}]]
                                      ]";

            AndConditions = JsonConvert.DeserializeObject<object[]>(AndConditionStr);
            OrConditions = JsonConvert.DeserializeObject<object[]>(OrConditionStr);
            NotCondition = JsonConvert.DeserializeObject<object[]>(NotConditionStr);
            NoOperatorConditions = JsonConvert.DeserializeObject<object[]>(NoOperatorConditionsStr);
        }

        [TestFixtureTearDown]
        public void TestCleanUp()
        {
            ConditionTreeEvaluator = null;
            NoOperatorConditions = null;
            AndConditions = null;
            OrConditions = null;
            NotCondition = null;
            NoOperatorConditions = null;
        }

        #region Evaluate Tests
        [Test]
        public void TestEvaluateWhenLeafConditionEvaluatorReturnsTrue()
        {
            Assert.That(ConditionTreeEvaluator.Evaluate(AndConditions, a => true), Is.True);
        }

        [Test]
        public void TestEvaluateWhenLeafConditionEvaluatorReturnsFalse()
        {
            Assert.That(ConditionTreeEvaluator.Evaluate(AndConditions, a => false), Is.False);
        }

        [Test]
        public void TestEvaluateWhenLeafConditionEvaluatorReturnsNull()
        {
            Assert.That(ConditionTreeEvaluator.Evaluate(AndConditions, a => null), Is.Null);
        }

        #endregion // Evaluate Tests

        #region AND condition Tests

        [Test]
        public void TestAndEvaluatorReturnsTrueWhenAllOperandsEvaluateToTrue()
        {
            Assert.That(ConditionTreeEvaluator.Evaluate(AndConditions, a => true), Is.True);
        }

        [Test]
        public void TestAndEvaluatorReturnsFalseWhenAnyOperandEvaluatesToFalse()
        {
            LeafEvaluatorMock.SetupSequence(le => le.Evaluate(It.IsAny<JToken>()))
                .Returns(true)
                .Returns(false)
                .Returns(true);

            Assert.That(ConditionTreeEvaluator.Evaluate(AndConditions, LeafEvaluatorMock.Object.Evaluate), Is.False);
        }

        [Test]
        public void TestAndEvaluatorReturnsNullWhenAllOperandsEvaluateToNull()
        {
            Assert.That(ConditionTreeEvaluator.Evaluate(AndConditions, a => null), Is.Null);
        }

        [Test]
        public void TestAndEvaluatorReturnsNullWhenOperandsEvaluateToTrueAndNull()
        {
            LeafEvaluatorMock.SetupSequence(le => le.Evaluate(It.IsAny<JToken>()))
                .Returns(true)
                .Returns(null)
                .Returns(true);

            Assert.That(ConditionTreeEvaluator.Evaluate(AndConditions, LeafEvaluatorMock.Object.Evaluate), Is.Null);
        }

        [Test]
        public void TestAndEvaluatorReturnsFalseWhenOperandsEvaluateToFalseAndNull()
        {
            LeafEvaluatorMock.SetupSequence(le => le.Evaluate(It.IsAny<JToken>()))
                .Returns(null)
                .Returns(false)
                .Returns(null);

            Assert.That(ConditionTreeEvaluator.Evaluate(AndConditions, LeafEvaluatorMock.Object.Evaluate), Is.False);
        }

        [Test]
        public void TestAndEvaluatorReturnsFalseWhenOperandsEvaluateToTrueFalseAndNull()
        {
            LeafEvaluatorMock.SetupSequence(le => le.Evaluate(It.IsAny<JToken>()))
                .Returns(true)
                .Returns(false)
                .Returns(null);

            Assert.That(ConditionTreeEvaluator.Evaluate(AndConditions, LeafEvaluatorMock.Object.Evaluate), Is.False);
        }

        #endregion // AND condition Tests

        #region OR condition Tests

        [Test]
        public void TestOrEvaluatorReturnsTrueWhenAnyOperandEvaluatesToTrue()
        {
            LeafEvaluatorMock.SetupSequence(le => le.Evaluate(It.IsAny<JToken>()))
                .Returns(true)
                .Returns(false)
                .Returns(true);

            Assert.That(ConditionTreeEvaluator.Evaluate(OrConditions, LeafEvaluatorMock.Object.Evaluate), Is.True);
        }

        [Test]
        public void TestOrEvaluatorReturnsFalseWhenAllOperandsEvaluatesToFalse()
        {
            Assert.That(ConditionTreeEvaluator.Evaluate(OrConditions, a => false), Is.False);
        }

        [Test]
        public void TestOrEvaluatorReturnsNullWhenAllOperandsEvaluateToNull()
        {
            Assert.That(ConditionTreeEvaluator.Evaluate(OrConditions, a => null), Is.Null);
        }

        [Test]
        public void TestOrEvaluatorReturnsTrueWhenOperandsEvaluateToTrueAndNull()
        {
            LeafEvaluatorMock.SetupSequence(le => le.Evaluate(It.IsAny<JToken>()))
                .Returns(true)
                .Returns(null)
                .Returns(true);

            Assert.That(ConditionTreeEvaluator.Evaluate(OrConditions, LeafEvaluatorMock.Object.Evaluate), Is.True);
        }

        [Test]
        public void TestOrEvaluatorReturnsNullWhenOperandsEvaluateToFalseAndNull()
        {
            LeafEvaluatorMock.SetupSequence(le => le.Evaluate(It.IsAny<JToken>()))
                .Returns(false)
                .Returns(null)
                .Returns(false);

            Assert.That(ConditionTreeEvaluator.Evaluate(OrConditions, LeafEvaluatorMock.Object.Evaluate), Is.Null);
        }

        [Test]
        public void TestOrEvaluatorReturnsTrueWhenOperandsEvaluateToFalseTrueAndNull()
        {
            LeafEvaluatorMock.SetupSequence(le => le.Evaluate(It.IsAny<JToken>()))
                .Returns(false)
                .Returns(true)
                .Returns(null);

            Assert.That(ConditionTreeEvaluator.Evaluate(OrConditions, LeafEvaluatorMock.Object.Evaluate), Is.True);
        }

        [Test]
        public void TestOrEvaluatorReturnsFalseWhenAllOperandsEvaluateToFalse()
        {
            Assert.That(ConditionTreeEvaluator.Evaluate(OrConditions, a => false), Is.False);
        }

        #endregion // OR condition Tests

        #region NOT condition Tests

        [Test]
        public void TestNotEvaluatorReturnsNullWhenOperandEvaluateToNull()
        {
            Assert.That(ConditionTreeEvaluator.Evaluate(NotCondition, a => null), Is.Null);
        }

        [Test]
        public void TestNotEvaluatorReturnsTrueWhenOperandEvaluateToFalse()
        {
            Assert.That(ConditionTreeEvaluator.Evaluate(NotCondition, a => false), Is.True);
        }

        [Test]
        public void TestNotEvaluatorReturnsFalseWhenOperandEvaluateToTrue()
        {
            Assert.That(ConditionTreeEvaluator.Evaluate(NotCondition, a => true), Is.False);
        }

        [Test]
        public void TestNotEvaluatorReturnsNullWhenThereAreNoOperands()
        {
            Assert.That(ConditionTreeEvaluator.Evaluate(@"[""not""]", a => null), Is.Null);
        }

        #endregion // NOT condition Tests

        #region Implicit Operator Tests

        [Test]
        public void TestEvaluatorAssumesOrOperatorWithUnrecognizedOperator()
        {
            LeafEvaluatorMock.SetupSequence(le => le.Evaluate(It.IsAny<JToken>()))
                .Returns(false)
                .Returns(true);

            Assert.That(ConditionTreeEvaluator.Evaluate(NoOperatorConditions, LeafEvaluatorMock.Object.Evaluate), Is.True);
            Assert.That(ConditionTreeEvaluator.Evaluate(NoOperatorConditions, a => false), Is.False);
        }

        #endregion // Implicit Operator Tests
    }
}
