/**
 *
 *    Copyright 2021, Optimizely and contributors
 *
 *    Licensed under the Apache License, Version 2.0 (the "License");
 *    you may not use this file except in compliance with the License.
 *    You may obtain a copy of the License at
 *
 *        http://www.apache.org/licenses/LICENSE-2.0
 *
 *    Unless required by applicable law or agreed to in writing, software
 *    distributed under the License is distributed on an "AS IS" BASIS,
 *    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *    See the License for the specific language governing permissions and
 *    limitations under the License.
 */

using NUnit.Framework;

namespace OptimizelySDK.Tests
{
    [TestFixture]
    public class ForcedDecisionsStoreTest
    {
        [Test]
        public void ForcedDecisionStoreGetSetForcedDecisionWithBothRuleAndFlagKey()
        {
            var expectedForcedDecision1 = new OptimizelyForcedDecision("sample_variation_key");
            var expectedForcedDecision2 = new OptimizelyForcedDecision("sample_variation_key_2");
            var context1 = new OptimizelyDecisionContext("flag_key", "rule_key");
            var context2 = new OptimizelyDecisionContext("flag_key", "rule_key1");
            var forcedDecisionStore = new ForcedDecisionsStore();
            forcedDecisionStore[context1] = expectedForcedDecision1;
            forcedDecisionStore[context2] = expectedForcedDecision2;

            Assert.AreEqual(forcedDecisionStore.Count, 2);
            Assert.AreEqual(forcedDecisionStore[context1].VariationKey, expectedForcedDecision1.VariationKey);
            Assert.AreEqual(forcedDecisionStore[context2].VariationKey, expectedForcedDecision2.VariationKey);
        }

        [Test]
        public void ForcedDecisionStoreNullFlagKeyForcedDecisionContext()
        {
            var expectedForcedDecision = new OptimizelyForcedDecision("sample_variation_key");
            var context = new OptimizelyDecisionContext(null, "rule_key");
            var forcedDecisionStore = new ForcedDecisionsStore();
            forcedDecisionStore[context] = expectedForcedDecision;

            Assert.AreEqual(forcedDecisionStore.Count, 0);
        }

        [Test]
        public void ForcedDecisionStoreNullContextForcedDecisionContext()
        {
            var expectedForcedDecision = new OptimizelyForcedDecision("sample_variation_key");
            OptimizelyDecisionContext context = null;
            var forcedDecisionStore = new ForcedDecisionsStore();
            forcedDecisionStore[context] = expectedForcedDecision;

            Assert.AreEqual(forcedDecisionStore.Count, 0);
        }

        [Test]
        public void ForcedDecisionStoreGetForcedDecisionWithBothRuleAndFlagKey()
        {
            var expectedForcedDecision1 = new OptimizelyForcedDecision("sample_variation_key");
            var context1 = new OptimizelyDecisionContext("flag_key", "rule_key");
            var NullFlagKeyContext = new OptimizelyDecisionContext(null, "rule_key");
            var forcedDecisionStore = new ForcedDecisionsStore();
            forcedDecisionStore[context1] = expectedForcedDecision1;

            Assert.AreEqual(forcedDecisionStore.Count, 1);
            Assert.AreEqual(forcedDecisionStore[context1].VariationKey, expectedForcedDecision1.VariationKey);
            Assert.IsNull(forcedDecisionStore[NullFlagKeyContext]);
        }

        [Test]
        public void ForcedDecisionStoreRemoveForcedDecisionTrue()
        {
            var expectedForcedDecision1 = new OptimizelyForcedDecision("sample_variation_key");
            var expectedForcedDecision2 = new OptimizelyForcedDecision("sample_variation_key_2");
            var context1 = new OptimizelyDecisionContext("flag_key", "rule_key");
            var context2 = new OptimizelyDecisionContext("flag_key", "rule_key1");
            var forcedDecisionStore = new ForcedDecisionsStore();
            forcedDecisionStore[context1] = expectedForcedDecision1;
            forcedDecisionStore[context2] = expectedForcedDecision2;

            Assert.AreEqual(forcedDecisionStore.Count, 2);
            Assert.IsTrue(forcedDecisionStore.Remove(context2));
            Assert.AreEqual(forcedDecisionStore.Count, 1);
            Assert.AreEqual(forcedDecisionStore[context1].VariationKey, expectedForcedDecision1.VariationKey);
            Assert.IsNull(forcedDecisionStore[context2]);
        }

        [Test]
        public void ForcedDecisionStoreRemoveForcedDecisionContextRuleKeyNotMatched()
        {
            var expectedForcedDecision = new OptimizelyForcedDecision("sample_variation_key");
            var contextNotMatched = new OptimizelyDecisionContext("flag_key", "");
            var context = new OptimizelyDecisionContext("flag_key", "rule_key");
            var forcedDecisionStore = new ForcedDecisionsStore();
            forcedDecisionStore[context] = expectedForcedDecision;

            Assert.AreEqual(forcedDecisionStore.Count, 1);
            Assert.IsFalse(forcedDecisionStore.Remove(contextNotMatched));
            Assert.AreEqual(forcedDecisionStore.Count, 1);
        }

        [Test]
        public void ForcedDecisionStoreRemoveAllForcedDecisionContext()
        {
            var expectedForcedDecision = new OptimizelyForcedDecision("sample_variation_key");
            var context = new OptimizelyDecisionContext("flag_key", "rule_key");
            var forcedDecisionStore = new ForcedDecisionsStore();
            forcedDecisionStore[context] = expectedForcedDecision;

            Assert.AreEqual(forcedDecisionStore.Count, 1);
            forcedDecisionStore.RemoveAll();
            Assert.AreEqual(forcedDecisionStore.Count, 0);
        }

    }
}
