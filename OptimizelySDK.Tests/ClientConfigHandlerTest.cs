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

#if !NETSTANDARD1_6 && !NET35

using NUnit.Framework;
using System.Configuration;

namespace OptimizelySDK.Tests
{
    [TestFixture]
    public class ClientConfigHandlerTest
    {

        [Test]
        public void TestHTTPAppConfigSection()
        {
            var configSection = ConfigurationManager.GetSection("optlySDKConfigSection") as OptimizelySDKConfigSection;
            var httpSetting = configSection.HttpProjectConfig;
            Assert.IsNotNull(httpSetting);
            Assert.IsTrue(httpSetting.AutoUpdate);
            Assert.AreEqual(httpSetting.BlockingTimeOutInMs, 10000);
            Assert.AreEqual(httpSetting.DatafileUrlFormat, "https://cdn.optimizely.com/data/{0}.json");
            Assert.IsTrue(httpSetting.DefaultStart);
            Assert.AreEqual(httpSetting.PollingIntervalInMs, 2000);
            Assert.AreEqual(httpSetting.SDKKey, "43214321");
            Assert.AreEqual(httpSetting.Url, "www.testurl.com");
        }

        [Test]
        public void TestBatchEventAppConfigSection()
        {
            var configSection = ConfigurationManager.GetSection("optlySDKConfigSection") as OptimizelySDKConfigSection;
            var batchSetting = configSection.BatchEventProcessor;
            Assert.IsNotNull(batchSetting);
            Assert.AreEqual(batchSetting.BatchSize, 10);
            Assert.AreEqual(batchSetting.FlushIntervalInMs, 2000);
            Assert.AreEqual(batchSetting.TimeoutIntervalInMs, 10000);
            Assert.IsTrue(batchSetting.DefaultStart);
        }

    }
}
#endif
