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

#if !NETSTANDARD1_6 && !NET35

using Xunit;
using System.Configuration;

namespace OptimizelySDK.XUnitTests
{
    public class ClientConfigHandlerTest
    {

        [Fact]
        public void TestHTTPAppConfigSection()
        {
            var configSection = ConfigurationManager.GetSection("optlySDKConfigSection") as OptimizelySDKConfigSection;
            var httpSetting = configSection.HttpProjectConfig;
            Assert.NotNull(httpSetting);
            Assert.True(httpSetting.AutoUpdate);
            Assert.Equal(10000, httpSetting.BlockingTimeOutPeriod);
            Assert.Equal("https://cdn.optimizely.com/data/{0}.json", httpSetting.Format);
            Assert.True(httpSetting.DefaultStart);
            Assert.Equal(2000, httpSetting.PollingInterval);
            Assert.Equal("43214321", httpSetting.SDKKey);
            Assert.Equal("www.testurl.com", httpSetting.Url);
            Assert.Equal("testingtoken123", httpSetting.DatafileAccessToken);
        }

        [Fact]
        public void TestBatchEventAppConfigSection()
        {
            var configSection = ConfigurationManager.GetSection("optlySDKConfigSection") as OptimizelySDKConfigSection;
            var batchSetting = configSection.BatchEventProcessor;
            Assert.NotNull(batchSetting);
            Assert.Equal(10, batchSetting.BatchSize);
            Assert.Equal(2000, batchSetting.FlushInterval);
            Assert.Equal(10000, batchSetting.TimeoutInterval);
            Assert.True(batchSetting.DefaultStart);
        }

    }
}
#endif
