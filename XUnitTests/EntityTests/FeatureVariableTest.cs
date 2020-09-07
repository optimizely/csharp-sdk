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
        [Fact]
        public void TestFeatureVariableTypeName()
        {
            Assert.Equal("GetFeatureVariableBoolean", FeatureVariable.GetFeatureVariableTypeName(FeatureVariable.BOOLEAN_TYPE));
            Assert.Equal("GetFeatureVariableDouble", FeatureVariable.GetFeatureVariableTypeName(FeatureVariable.DOUBLE_TYPE));
            Assert.Equal("GetFeatureVariableInteger", FeatureVariable.GetFeatureVariableTypeName(FeatureVariable.INTEGER_TYPE));
            Assert.Equal("GetFeatureVariableString", FeatureVariable.GetFeatureVariableTypeName(FeatureVariable.STRING_TYPE));
            Assert.Equal("GetFeatureVariableJSON", FeatureVariable.GetFeatureVariableTypeName(FeatureVariable.JSON_TYPE));
        }

        [Fact]
        public void TestConstantValues()
        {
            Assert.Equal("boolean", FeatureVariable.BOOLEAN_TYPE);
            Assert.Equal("double", FeatureVariable.DOUBLE_TYPE);
            Assert.Equal("integer", FeatureVariable.INTEGER_TYPE);
            Assert.Equal("string", FeatureVariable.STRING_TYPE);
            Assert.Equal("json", FeatureVariable.JSON_TYPE);
        }
    }
}
