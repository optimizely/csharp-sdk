﻿/* 
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

using NUnit.Framework;
using OptimizelySDK.Config;

namespace OptimizelySDK.Tests.DatafileManagement_Tests
{
    [TestFixture]
    public class AtomicProjectConfigManagerTest
    {
        private FallbackProjectConfigManager ConfigManager;

        [Test]
        public void TestStaticProjectConfigManagerReturnsCorrectProjectConfig()
        {
            var expectedConfig = DatafileProjectConfig.Create(TestData.TypedAudienceDatafile, null, null);
            ConfigManager = new FallbackProjectConfigManager(expectedConfig);

            Assert.True(TestData.CompareObjects(expectedConfig, ConfigManager.GetConfig()));
        }
    }
}
