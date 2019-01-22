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
using NUnit.Framework;
using OptimizelySDK.AudienceConditions;
using OptimizelySDK.Entity;

namespace OptimizelySDK.Tests.AudienceConditionsTests
{
    [TestFixture]
    public class ConditionsTest
    {
        private Mock<ICondition> TrueConditionMock;
        private Mock<ICondition> FalseConditionMock;
        private Mock<ICondition> NullConditionMock;

        private ICondition TrueCondition;
        private ICondition FalseCondition;
        private ICondition NullCondition;

        [TestFixtureSetUp]
        public void Initialize()
        {
            TrueConditionMock = new Mock<ICondition>();
            TrueConditionMock.Setup(condition => condition.Evaluate(It.IsAny<ProjectConfig>(), It.IsAny<UserAttributes>())).Returns(true);
            FalseConditionMock = new Mock<ICondition>();
            FalseConditionMock.Setup(condition => condition.Evaluate(It.IsAny<ProjectConfig>(), It.IsAny<UserAttributes>())).Returns(false);
            NullConditionMock = new Mock<ICondition>();
            NullConditionMock.Setup(condition => condition.Evaluate(It.IsAny<ProjectConfig>(), It.IsAny<UserAttributes>())).Returns((bool?)null);

            TrueCondition = TrueConditionMock.Object;
            FalseCondition = FalseConditionMock.Object;
            NullCondition = NullConditionMock.Object;
        }

        #region AND Condition Tests

        [Test]
        public void TestAndEvaluatorReturnsTrueWhenAllOperandsEvaluateToTrue()
        {
            ICondition[] conditions = new ICondition[] { TrueCondition, TrueCondition };
            var andCondition = new AndCondition { Conditions = conditions };

            Assert.That(andCondition.Evaluate(null, null), Is.True);
        }

        [Test]
        public void TestAndEvaluatorReturnsFalseWhenAnyOperandEvaluatesToFalse()
        {
            ICondition[] conditions = new ICondition[] { FalseCondition, TrueCondition };
            var andCondition = new AndCondition { Conditions = conditions };

            Assert.That(andCondition.Evaluate(null, null), Is.False);

            // Should not be called due to short circuiting.
            TrueConditionMock.Verify(condition => condition.Evaluate(It.IsAny<ProjectConfig>(), It.IsAny<UserAttributes>()), Times.Never);
        }

        [Test]
        public void TestAndEvaluatorReturnsNullWhenAllOperandsEvaluateToNull()
        {
            ICondition[] conditions = new ICondition[] { NullCondition, NullCondition };
            var andCondition = new AndCondition { Conditions = conditions };

            Assert.That(andCondition.Evaluate(null, null), Is.Null);
        }

        [Test]
        public void TestAndEvaluatorReturnsNullWhenOperandsEvaluateToTrueAndNull()
        {
            ICondition[] conditions = new ICondition[] { TrueCondition, NullCondition, TrueCondition };
            var andCondition = new AndCondition { Conditions = conditions };

            Assert.That(andCondition.Evaluate(null, null), Is.Null);
        }

        [Test]
        public void TestAndEvaluatorReturnsFalseWhenOperandsEvaluateToFalseAndNull()
        {
            ICondition[] conditions = new ICondition[] { NullCondition, FalseCondition, NullCondition };
            var andCondition = new AndCondition { Conditions = conditions };

            Assert.That(andCondition.Evaluate(null, null), Is.False);
        }

        [Test]
        public void TestAndEvaluatorReturnsFalseWhenOperandsEvaluateToTrueFalseAndNull()
        {
            ICondition[] conditions = new ICondition[] { TrueCondition, FalseCondition, NullCondition };
            var andCondition = new AndCondition { Conditions = conditions };

            Assert.That(andCondition.Evaluate(null, null), Is.False);
        }

        #endregion // AND Condition Tests

        #region OR Condition Tests

        [Test]
        public void TestOrEvaluatorReturnsTrueWhenAnyOperandEvaluatesToTrue()
        {
            ICondition[] conditions = new ICondition[] { TrueCondition, FalseCondition, NullCondition };
            var orCondition = new OrCondition { Conditions = conditions };

            Assert.That(orCondition.Evaluate(null, null), Is.True);
        }

        [Test]
        public void TestOrEvaluatorReturnsFalseWhenAllOperandsEvaluatesToFalse()
        {
            ICondition[] conditions = new ICondition[] { FalseCondition, FalseCondition };
            var orCondition = new OrCondition { Conditions = conditions };

            Assert.That(orCondition.Evaluate(null, null), Is.False);
        }

        [Test]
        public void TestOrEvaluatorReturnsNullWhenOperandsEvaluateToFalseAndNull()
        {
            ICondition[] conditions = new ICondition[] { FalseCondition, NullCondition, FalseCondition };
            var orCondition = new OrCondition { Conditions = conditions };

            Assert.That(orCondition.Evaluate(null, null), Is.Null);
        }

        [Test]
        public void TestOrEvaluatorReturnsTrueWhenOperandsEvaluateToTrueAndNull()
        {
            ICondition[] conditions = new ICondition[] { TrueCondition, NullCondition, TrueCondition };
            var orCondition = new OrCondition { Conditions = conditions };

            Assert.That(orCondition.Evaluate(null, null), Is.True);
        }

        [Test]
        public void TestOrEvaluatorReturnsTrueWhenOperandsEvaluateToFalseTrueAndNull()
        {
            ICondition[] conditions = new ICondition[] { FalseCondition, NullCondition, TrueCondition };
            var orCondition = new OrCondition { Conditions = conditions };

            Assert.That(orCondition.Evaluate(null, null), Is.True);
        }

        #endregion // OR Condition Tests

        #region NOT Condition Tests

        [Test]
        public void TestNotEvaluatorReturnsNullWhenOperandEvaluateToNull()
        {
            var notCondition = new NotCondition { Condition = NullCondition };
            Assert.That(notCondition.Evaluate(null, null), Is.Null);
        }

        [Test]
        public void TestNotEvaluatorReturnsTrueWhenOperandEvaluateToFalse()
        {
            var notCondition = new NotCondition { Condition = FalseCondition };
            Assert.That(notCondition.Evaluate(null, null), Is.True);
        }

        [Test]
        public void TestNotEvaluatorReturnsFalseWhenOperandEvaluateToTrue()
        {
            var notCondition = new NotCondition { Condition = TrueCondition };
            Assert.That(notCondition.Evaluate(null, null), Is.False);
        }

        #endregion // NOT Condition Tests
    }
}
