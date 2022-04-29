using System.Collections.Generic;

namespace OptimizelySDK.Notifications
{
    public interface SourceInfo
    {
        IDictionary<string, string> Get();
    }
}
