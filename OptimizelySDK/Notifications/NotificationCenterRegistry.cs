﻿/* 
 * Copyright 2023, Optimizely
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * https://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using OptimizelySDK.Logger;
using System.Collections.Generic;

namespace OptimizelySDK.Notifications
{
    internal static class NotificationCenterRegistry
    {
        private static readonly object _mutex = new object();

        private static Dictionary<string, NotificationCenter> _notificationCenters =
            new Dictionary<string, NotificationCenter>();

        /// <summary>
        /// Thread-safe access to the NotificationCenter
        /// </summary>
        /// <param name="sdkKey">Retrieve NotificationCenter based on SDK key</param>
        /// <param name="logger">Logger to record events</param>
        /// <returns>NotificationCenter instance per SDK key</returns>
        public static NotificationCenter GetNotificationCenter(string sdkKey, ILogger logger = null)
        {
            if (sdkKey == null)
            {
                logger?.Log(LogLevel.ERROR, "No SDK key provided to GetNotificationCenter");
                return default;
            }

            NotificationCenter notificationCenter;
            lock (_mutex)
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

        /// <summary>
        /// Thread-safe removal of a NotificationCenter from the Registry 
        /// </summary>
        /// <param name="sdkKey">SDK key identifying the target</param>
        public static void RemoveNotificationCenter(string sdkKey)
        {
            if (sdkKey == null)
            {
                return;
            }

            lock (_mutex)
            {
                if (_notificationCenters.TryGetValue(sdkKey,
                        out NotificationCenter notificationCenter))
                {
                    notificationCenter.ClearAllNotifications();
                    _notificationCenters.Remove(sdkKey);
                }
            }
        }
    }
}
