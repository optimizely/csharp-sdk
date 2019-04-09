using System;
namespace OptimizelySDK.DatafileManagement
{
    public interface ProjectConfigManager
    {
        ProjectConfig GetConfig();
        bool SetConfig(ProjectConfig projectConfig);
    }
}
