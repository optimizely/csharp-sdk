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
using OptimizelySDK.DatafileManagement;
using OptimizelySDK.Logger;

namespace OptimizelySDK.Tests.DatafileManagement_Tests
{
    [TestFixture]
    public class HttpProjectConfigManagerTest
    {
        private Mock<ILogger> LoggerMock;
        
        [SetUp]
        public void Setup()
        {
            LoggerMock = new Mock<ILogger>();
            LoggerMock.Setup(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<string>()));
        }

        [Test]
        public void TestHttpConfigManagerRetreiveProjectConfigByURL()
        {
            HttpProjectConfigManager httpManager = new HttpProjectConfigManager.Builder()
                .WithUrl("https://cdn.optimizely.com/datafiles/QBw9gFM8oTn7ogY9ANCC1z.json")
                .WithLogger(LoggerMock.Object)
                .Build();

            Assert.NotNull(httpManager.GetConfig());
        }

        [Test]
        public void TestHttpConfigManagerRetreiveProjectConfigBySDKKey()
        {
            HttpProjectConfigManager httpManager = new HttpProjectConfigManager.Builder()
                .WithSdkKey("QBw9gFM8oTn7ogY9ANCC1z")
                .WithLogger(LoggerMock.Object)
                .Build();

            Assert.NotNull(httpManager.GetConfig());
        }

        [Test]
        public void TestHttpConfigManagerRetreiveProjectConfigByFormat()
        {
            HttpProjectConfigManager httpManager = new HttpProjectConfigManager.Builder()
                .WithSdkKey("10192104166")
                .WithFormat("https://cdn.optimizely.com/json/{0}.json")
                .WithLogger(LoggerMock.Object)
                .Build();

            Assert.NotNull(httpManager.GetConfig());
        }

        [Test]
        public void TestHttpConfigManagerRetreiveNullWithInvalidURL()
        {
            HttpProjectConfigManager httpManager = new HttpProjectConfigManager.Builder()
                .WithUrl("https://cdn.optimizely.com/daaaatafilessss/QBw9gFM8oTn7ogY9ANCC1z.json")
                .WithFormat("https://cdn.optimizely.com/json/{0}.json")
                .WithLogger(LoggerMock.Object)
                .Build();

            Assert.Null(httpManager.GetConfig());
        }
    }
}
