using System;
using System.Threading;
using OptimizelySDK.DatafileManagement;
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
        // default.
        TestPollingData[] PollingData;
        public int Counter = 0;

        public TestPollingProjectConfigManager(TimeSpan period, TimeSpan blockingTimeout, ILogger logger, int[] pollingSequence, bool startByDefault = true) : base(period, blockingTimeout, logger, startByDefault)
        {
            if (pollingSequence != null) {
                System.Collections.Generic.List<TestPollingData> pollingData = new System.Collections.Generic.List<TestPollingData>();
                foreach (var pollingTime in pollingSequence) {
                    pollingData.Add(new TestPollingData { PollingTime = pollingTime });
                }
                this.PollingData = pollingData.ToArray();
            }
        }

        public TestPollingProjectConfigManager(TimeSpan period, TimeSpan blockingTimeout, ILogger logger, TestPollingData[] pollingData, bool startByDefault = true) : base(period, blockingTimeout, logger, startByDefault)
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
            System.Diagnostics.Debug.WriteLine("response != null");
            System.Diagnostics.Debug.WriteLine(response != null);
            Counter++;
            System.Threading.Tasks.Task.Delay(waitingTime).Wait(-1);
            // Returning null, since we need to check polling functionality.
            return response;
        }
    }
}
