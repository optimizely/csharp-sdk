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

using OptimizelySDK.Entity;
using Xunit;

namespace OptimizelySDK.XUnitTests.EntityTests
{
    public class FeatureVariableTest
    {
        [Theory]
        [InlineData ("GetFeatureVariableBoolean", FeatureVariable.BOOLEAN_TYPE)]
        [InlineData ("GetFeatureVariableDouble", FeatureVariable.DOUBLE_TYPE)]
        [InlineData ("GetFeatureVariableInteger", FeatureVariable.INTEGER_TYPE)]
        [InlineData ("GetFeatureVariableString", FeatureVariable.STRING_TYPE)]
        [InlineData ("GetFeatureVariableJSON", FeatureVariable.JSON_TYPE)]
        public void TestFeatureVariableTypeName(string typeNameStr, string typeNameConst)
        {
            Assert.Equal(typeNameStr, FeatureVariable.GetFeatureVariableTypeName(typeNameConst));
        }

        [Theory]
        [InlineData("boolean", FeatureVariable.BOOLEAN_TYPE)]
        [InlineData("double", FeatureVariable.DOUBLE_TYPE)]
        [InlineData("integer", FeatureVariable.INTEGER_TYPE)]
        [InlineData("string", FeatureVariable.STRING_TYPE)]
        [InlineData("json", FeatureVariable.JSON_TYPE)]
        public void TestConstantValues(string typeNameStr, string typeNameConst)
        {
            Assert.Equal(typeNameStr, typeNameConst);
        }
    }
}
