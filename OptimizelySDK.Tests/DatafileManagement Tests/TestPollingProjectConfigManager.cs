using System;
using System.Threading;
using OptimizelySDK.DatafileManagement;
using OptimizelySDK.Logger;

namespace OptimizelySDK.Tests.DatafileManagementTests
{
    public class TestPollingProjectConfigManager : PollingProjectConfigManager
    {
        // default.
        int[] PollingSequence = new int[] { 500 };
        public int Counter = 0;

        public TestPollingProjectConfigManager(TimeSpan period, TimeSpan blockingTimeout, ILogger logger, int[] pollingSequence) : base(period, blockingTimeout, logger)
        {
            if (pollingSequence != null) {
                PollingSequence = pollingSequence;
            }
        }

        protected override ProjectConfig Poll()
        {
            TimeSpan WaitingTime = TimeSpan.FromMilliseconds(500);

            if (PollingSequence.Length > Counter) {
                WaitingTime = TimeSpan.FromMilliseconds(PollingSequence[Counter]);
            }

            Counter++;
            System.Threading.Tasks.Task.Delay(WaitingTime).Wait(-1);
            // Returning null, since we need to check polling functionality.
            return null;
        }
    }
}
