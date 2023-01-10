using OptimizelySDK.Logger;
using System;
using System.Collections.Generic;
namespace OptimizelySDK.Notifications
{
    internal class NotificationRegistry
    {
        private static object _mutex = new object();
        private static Dictionary<string, NotificationCenter> _notificationCenters;

        private NotificationRegistry()
        {
        }

        public static NotificationCenter GetNotificationCenter(string sdkKey, ILogger logger = null)
        {
            NotificationCenter notificationCenter;
            lock(_mutex)
            {
                if (_notificationCenters.ContainsKey(sdkKey))
                {
                    notificationCenter = _notificationCenters[sdkKey];
                }
                else
                {
                    notificationCenter = new NotificationCenter(logger);
                    _notificationCenters[sdkKey] = notificationCenter;
                }
            }
            return notificationCenter;
        }
    }
}

