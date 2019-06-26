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
            Assert.AreEqual(FeatureVariable.GetFeatureVariableTypeName(FeatureVariable.VariableType.BOOLEAN), "GetFeatureVariableBoolean");
            Assert.AreEqual(FeatureVariable.GetFeatureVariableTypeName(FeatureVariable.VariableType.DOUBLE), "GetFeatureVariableDouble");
            Assert.AreEqual(FeatureVariable.GetFeatureVariableTypeName(FeatureVariable.VariableType.INTEGER), "GetFeatureVariableInteger");
            Assert.AreEqual(FeatureVariable.GetFeatureVariableTypeName(FeatureVariable.VariableType.STRING), "GetFeatureVariableString");
        }
    }
}
