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
using System;

namespace OptimizelySDK.Tests.DatafileManagement_Tests
{
    [TestFixture]
    public class PollingProjectConfigManagerTest
    {
        private Mock<ILogger> LoggerMock;

        [SetUp]
        public void Setup()
        {
            LoggerMock = new Mock<ILogger>();
            LoggerMock.Setup(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<string>()));
        }

        //[Test]
        //public void TestHttpConfigManagerReturnsCorrectProjectConfig()
        //{
        //    System.Diagnostics.Debug.WriteLine($"Main Thread: {System.Threading.Thread.CurrentThread.ManagedThreadId}");

        //    HttpProjectConfigManager httpManager = new HttpProjectConfigManager.Builder()
        //        .WithSdkKey("QBw9gFM8oTn7ogY9ANCC1z")
        //        .WithLogger(LoggerMock.Object)
        //        .Build();
            
        //    var configManager = new PollingProjectConfigManager(TimeSpan.FromSeconds(1), httpManager, LoggerMock.Object);
        //    var onReady = configManager.OnReady();

        //    // GetConfig returns null as OnReady feature is not resolved yet.
        //    if (!onReady.IsCompleted)
        //        Assert.Null(configManager.GetConfig());

        //    // Waiting for onReady to gets completed.
        //    var resolved = onReady.Result;
        //    Assert.NotNull(configManager.GetConfig());
        //}
    }
}
