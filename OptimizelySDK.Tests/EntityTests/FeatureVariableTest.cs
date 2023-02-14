/* 
 * Copyright 2019-2020, Optimizely
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
using NUnit.Framework;
using OptimizelySDK.Entity;

namespace OptimizelySDK.Tests.EntityTests
{
    [TestFixture]
    public class FeatureVariableTest
    {
        [Test]
        public void TestFeatureVariableTypeName()
        {
            Assert.AreEqual(
                FeatureVariable.GetFeatureVariableTypeName(FeatureVariable.BOOLEAN_TYPE),
                "GetFeatureVariableBoolean");
            Assert.AreEqual(FeatureVariable.GetFeatureVariableTypeName(FeatureVariable.DOUBLE_TYPE),
                "GetFeatureVariableDouble");
            Assert.AreEqual(
                FeatureVariable.GetFeatureVariableTypeName(FeatureVariable.INTEGER_TYPE),
                "GetFeatureVariableInteger");
            Assert.AreEqual(FeatureVariable.GetFeatureVariableTypeName(FeatureVariable.STRING_TYPE),
                "GetFeatureVariableString");
            Assert.AreEqual(FeatureVariable.GetFeatureVariableTypeName(FeatureVariable.JSON_TYPE),
                "GetFeatureVariableJSON");
        }

        [Test]
        public void TestConstantValues()
        {
            Assert.AreEqual(FeatureVariable.BOOLEAN_TYPE, "boolean");
            Assert.AreEqual(FeatureVariable.DOUBLE_TYPE, "double");
            Assert.AreEqual(FeatureVariable.INTEGER_TYPE, "integer");
            Assert.AreEqual(FeatureVariable.STRING_TYPE, "string");
            Assert.AreEqual(FeatureVariable.JSON_TYPE, "json");
        }
    }
}
