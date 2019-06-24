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
using OptimizelySDK.Config;
using OptimizelySDK.Logger;

namespace OptimizelySDK.Tests.DatafileManagementTests
{
    public class TestPollingData
    {
        public int PollingTime { get; set; }
        public ProjectConfig ConfigDatafile { get; set; }
        public ProjectConfig ConfigVersioned { 
            get {
                if (ConfigDatafile == null)
                    return null;
                if (ChangeVersion)
                    ConfigDatafile.Version = DateTime.Now.Ticks.ToString();

                return ConfigDatafile;
            }
        }
        public bool ChangeVersion { get; set; }
    }

    public class TestPollingProjectConfigManager : PollingProjectConfigManager
    {
        TestPollingData[] PollingData;
        public int Counter = 0;

        public TestPollingProjectConfigManager(TimeSpan period, TimeSpan blockingTimeout, bool autoUpdate, ILogger logger, int[] pollingSequence) 
            : base(period, blockingTimeout, autoUpdate, logger, null)
        {
            if (pollingSequence != null) {
                System.Collections.Generic.List<TestPollingData> pollingData = new System.Collections.Generic.List<TestPollingData>();
                foreach (var pollingTime in pollingSequence) {
                    pollingData.Add(new TestPollingData { PollingTime = pollingTime });
                }
                this.PollingData = pollingData.ToArray();
            }
        }

        public TestPollingProjectConfigManager(TimeSpan period, TimeSpan blockingTimeout, bool autoUpdate, ILogger logger, TestPollingData[] pollingData, bool startByDefault = true) 
            : base(period, blockingTimeout, autoUpdate, logger, null)
        {
            if (pollingData != null) {
                this.PollingData = pollingData;
            }
        }


        protected override ProjectConfig Poll()
        {
            TimeSpan waitingTime = TimeSpan.FromMilliseconds(500);
            ProjectConfig response = null;

            if (PollingData.Length > Counter) {
                waitingTime = TimeSpan.FromMilliseconds(PollingData[Counter].PollingTime);
                // Will automatically change version if ChangeVersion is true.
                response = PollingData[Counter].ConfigVersioned;
            }

            Counter++;
            System.Threading.Tasks.Task.Delay(waitingTime).Wait(-1);
            // Returning null, since we need to check polling functionality.
            return response;
        }
    }
}
