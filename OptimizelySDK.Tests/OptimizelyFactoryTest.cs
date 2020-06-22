/**
 *
 *    Copyright 2020, Optimizely and contributors
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

using Moq;
using NUnit.Framework;
using OptimizelySDK.Config;
using OptimizelySDK.ErrorHandler;
using OptimizelySDK.Exceptions;
using OptimizelySDK.Logger;
using OptimizelySDK.Tests.ConfigTest;
using System;
using System.Collections.Generic;
namespace OptimizelySDK.Tests
{
    [TestFixture]
    public class OptimizelyFactoryTest
    {
        [Test]
        public void TestOptimizelyInstanceUsingConfigFile()
        {
            var optimizely = OptimizelyFactory.NewDefaultInstance();
            // Check values are loaded from app.config or not.
            var projectConfigManager = optimizely.ProjectConfigManager as HttpProjectConfigManager;
            Assert.NotNull(projectConfigManager);

            var actualConfigManagerProps = new ProjectConfigManagerProps(projectConfigManager);
            var expectedConfigManagerProps = new ProjectConfigManagerProps {
                //TODO: Add more properties and then assert.
                AutoUpdate = true,
            };

            Assert.IsTrue(TestData.CompareObjects(actualConfigManagerProps, expectedConfigManagerProps));
            optimizely.Dispose();
        }

        [Test]
        public void TestOptimizelyInstanceUsingSDKKey()
        {
            var optimizely = OptimizelyFactory.NewDefaultInstance("my-sdk-key");

            // Check values are loaded from app.config or not.
            var projectConfigManager = optimizely.ProjectConfigManager as HttpProjectConfigManager;
            Assert.NotNull(projectConfigManager);

            var actualConfigManagerProps = new ProjectConfigManagerProps(projectConfigManager);
            var expectedConfigManagerProps = new ProjectConfigManagerProps {
                //TODO: Add more properties and then assert.
                AutoUpdate = true,
            };

            optimizely.Dispose();
        }

        [Test]
        public void TestOptimizelyInstanceWithDatafileAccessToken()
        {
            var optimizely = OptimizelyFactory.NewDefaultInstance("my-sdk-key", null, "access-token");
            optimizely.Dispose();
        }

        [Test]
        public void TestOptimizelyWithProjectConfigManager()
        {
            var projectConfigManager = new HttpProjectConfigManager.Builder()
                .WithSdkKey("my-sdk-key")
                // TODO: Add more
                .WithAccessToken("access-token")
                .Build();

            var optimizely = OptimizelyFactory.NewDefaultInstance(projectConfigManager);
            optimizely.Dispose();
        }

        [Test]
        public void TestOptimizelyWithEventBatching()
        {

        }

    }

}
