/* 
 * Copyright 2020, Optimizely
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
using OptimizelySDK.AudienceConditions;
using OptimizelySDK.Entity;
using OptimizelySDK.Logger;
using Xunit;

namespace OptimizelySDK.XUnitTests.AudienceConditionsTests
{
    public class ConditionsTest
    {
        private Mock<ICondition> TrueConditionMock;
        private Mock<ICondition> FalseConditionMock;
        private Mock<ICondition> NullConditionMock;

        private ICondition TrueCondition;
        private ICondition FalseCondition;
        private ICondition NullCondition;

        private ILogger Logger;
        private Mock<ILogger> MockLogger;

        public ConditionsTest()
        {
            TrueConditionMock = new Mock<ICondition>();
            TrueConditionMock.Setup(condition => condition.Evaluate(It.IsAny<ProjectConfig>(), It.IsAny<UserAttributes>(), It.IsAny<ILogger>())).Returns(true);
            FalseConditionMock = new Mock<ICondition>();
            FalseConditionMock.Setup(condition => condition.Evaluate(It.IsAny<ProjectConfig>(), It.IsAny<UserAttributes>(), It.IsAny<ILogger>())).Returns(false);
            NullConditionMock = new Mock<ICondition>();
            NullConditionMock.Setup(condition => condition.Evaluate(It.IsAny<ProjectConfig>(), It.IsAny<UserAttributes>(), It.IsAny<ILogger>())).Returns((bool?)null);

            TrueCondition = TrueConditionMock.Object;
            FalseCondition = FalseConditionMock.Object;
            NullCondition = NullConditionMock.Object;

            MockLogger = new Mock<ILogger>();
            MockLogger.Setup(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<string>()));
            Logger = MockLogger.Object;
        }

        #region AND Condition Tests

        [Fact]
        public void TestAndEvaluatorReturnsTrueWhenAllOperandsEvaluateToTrue()
        {
            ICondition[] conditions = new ICondition[] { TrueCondition, TrueCondition };
            var andCondition = new AndCondition { Conditions = conditions };

            Assert.True(andCondition.Evaluate(null, null, Logger));
        }

        [Fact]
        public void TestAndEvaluatorReturnsFalseWhenAnyOperandEvaluatesToFalse()
        {
            ICondition[] conditions = new ICondition[] { FalseCondition, TrueCondition };
            var andCondition = new AndCondition { Conditions = conditions };

            Assert.False(andCondition.Evaluate(null, null, Logger));

            // Should not be called due to short circuiting.
            TrueConditionMock.Verify(condition => condition.Evaluate(It.IsAny<ProjectConfig>(), It.IsAny<UserAttributes>(), Logger), Times.Never);
        }

        [Fact]
        public void TestAndEvaluatorReturnsNullWhenAllOperandsEvaluateToNull()
        {
            ICondition[] conditions = new ICondition[] { NullCondition, NullCondition };
            var andCondition = new AndCondition { Conditions = conditions };

            Assert.Null(andCondition.Evaluate(null, null, Logger));
        }

        [Fact]
        public void TestAndEvaluatorReturnsNullWhenOperandsEvaluateToTrueAndNull()
        {
            ICondition[] conditions = new ICondition[] { TrueCondition, NullCondition, TrueCondition };
            var andCondition = new AndCondition { Conditions = conditions };

            Assert.Null(andCondition.Evaluate(null, null, Logger));
        }

        [Fact]
        public void TestAndEvaluatorReturnsFalseWhenOperandsEvaluateToFalseAndNull()
        {
            ICondition[] conditions = new ICondition[] { NullCondition, FalseCondition, NullCondition };
            var andCondition = new AndCondition { Conditions = conditions };

            Assert.False(andCondition.Evaluate(null, null, Logger));
        }

        [Fact]
        public void TestAndEvaluatorReturnsFalseWhenOperandsEvaluateToTrueFalseAndNull()
        {
            ICondition[] conditions = new ICondition[] { TrueCondition, FalseCondition, NullCondition };
            var andCondition = new AndCondition { Conditions = conditions };

            Assert.False(andCondition.Evaluate(null, null, Logger));
        }

        #endregion // AND Condition Tests

        #region OR Condition Tests

        [Fact]
        public void TestOrEvaluatorReturnsTrueWhenAnyOperandEvaluatesToTrue()
        {
            ICondition[] conditions = new ICondition[] { TrueCondition, FalseCondition, NullCondition };
            var orCondition = new OrCondition { Conditions = conditions };

            Assert.True(orCondition.Evaluate(null, null, Logger));
        }

        [Fact]
        public void TestOrEvaluatorReturnsFalseWhenAllOperandsEvaluatesToFalse()
        {
            ICondition[] conditions = new ICondition[] { FalseCondition, FalseCondition };
            var orCondition = new OrCondition { Conditions = conditions };

            Assert.False(orCondition.Evaluate(null, null, Logger));
        }

        [Fact]
        public void TestOrEvaluatorReturnsNullWhenOperandsEvaluateToFalseAndNull()
        {
            ICondition[] conditions = new ICondition[] { FalseCondition, NullCondition, FalseCondition };
            var orCondition = new OrCondition { Conditions = conditions };

            Assert.Null(orCondition.Evaluate(null, null, Logger));
        }

        [Fact]
        public void TestOrEvaluatorReturnsTrueWhenOperandsEvaluateToTrueAndNull()
        {
            ICondition[] conditions = new ICondition[] { TrueCondition, NullCondition, TrueCondition };
            var orCondition = new OrCondition { Conditions = conditions };

            Assert.True(orCondition.Evaluate(null, null, Logger));
        }

        [Fact]
        public void TestOrEvaluatorReturnsTrueWhenOperandsEvaluateToFalseTrueAndNull()
        {
            ICondition[] conditions = new ICondition[] { FalseCondition, NullCondition, TrueCondition };
            var orCondition = new OrCondition { Conditions = conditions };

            Assert.True(orCondition.Evaluate(null, null, Logger));
        }

        #endregion // OR Condition Tests

        #region NOT Condition Tests

        [Fact]
        public void TestNotEvaluatorReturnsNullWhenOperandEvaluateToNull()
        {
            var notCondition = new NotCondition { Condition = NullCondition };
            Assert.Null(notCondition.Evaluate(null, null, Logger));
        }

        [Fact]
        public void TestNotEvaluatorReturnsTrueWhenOperandEvaluateToFalse()
        {
            var notCondition = new NotCondition { Condition = FalseCondition };
            Assert.True(notCondition.Evaluate(null, null, Logger));
        }

        [Fact]
        public void TestNotEvaluatorReturnsFalseWhenOperandEvaluateToTrue()
        {
            var notCondition = new NotCondition { Condition = TrueCondition };
            Assert.False(notCondition.Evaluate(null, null, Logger));
        }

        [Fact]
        public void TestNotEvaluatorReturnsNullWhenConditionIsNull()
        {
            var notCondition = new NotCondition { Condition = null };
            Assert.Null(notCondition.Evaluate(null, null, Logger));
        }

        #endregion // NOT Condition Tests
    }
}
