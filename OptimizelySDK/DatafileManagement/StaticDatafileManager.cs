using System;
using OptimizelySDK.ErrorHandler;
using OptimizelySDK.Logger;

namespace OptimizelySDK.DatafileManagement
{
    public class StaticDatafileManager : PollingProjectConfigManager
    {
        private ProjectConfig ProjectConfig;

        public StaticDatafileManager(string datafile = null, ILogger logger = null, IErrorHandler errorHandler = null) : base(new TimeSpan(), false, logger, errorHandler)
        {
            ProjectConfig = DatafileProjectConfig.Create(datafile, logger, errorHandler);
        }

        public override ProjectConfig FetchConfig()
        {
            return ProjectConfig;
        }
    }
}
