/* 
 * Copyright 2021, Optimizely
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

using NUnit.Framework;
using System.Globalization;
using System.Threading;

namespace OptimizelySDK.Tests
{
    [SetUpFixture]
    public class TestSetup
    {
        [SetUp]
        public void Init()
        {
            /* There are some issues doing assertions on tests with floating point numbers using the .ToString()
             * method, as it's culture dependent. EG: TestGetFeatureVariableValueForTypeGivenFeatureFlagIsNotEnabledForUser,
             * assigning the culture to English will make this kind of tests to work on others culture based systems. */
            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("en-US");
        }

        [TearDown]
        public void Cleanup()
        {
            // Empty, but required: https://nunit.org/nunitv2/docs/2.6.4/setupFixture.html
        }
    }
}
