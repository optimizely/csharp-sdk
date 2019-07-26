/* 
 * Copyright 2017, 2019, Optimizely
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

using OptimizelySDK.Entity;
using OptimizelySDK.Event;
using OptimizelySDK.Logger;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OptimizelySDK.Notifications
{
    /// <summary>
    /// NotificationCenter class for sending notifications.
    /// </summary>
    public class NotificationCenter
    {
        /// <summary>
        /// Enum representing notification types.
        /// </summary>
        public enum NotificationType
        {
            Activate,   // Activate called.
            Track,      // Track called.
            Decision,    // A decision is made in the system. i.e. user activation, feature access or feature-variable value retrieval.
            OptimizelyConfigUpdate, // When datafile is updated using HttpProjectConfigManager.
            LogEvent    // LogEvent notification sends on flushing batch-event. // When datafile is updated using HttpProjectConfigManager.
        };

        /// <summary>
        /// Delegate for activate notifcations.
        /// </summary>
        /// <param name="experiment">The experiment entity</param>
        /// <param name="userId">The user identifier</param>
        /// <param name="userAttributes">Associative array of attributes for the user</param>
        /// <param name="variation">The variation entity</param>
        /// <param name="logEvent">The impression event</param>
        [Obsolete("ActivateCallback is deprecated. Use DecisionCallback instead.")]
        public delegate void ActivateCallback(Experiment experiment, string userId, UserAttributes userAttributes,
            Variation variation, LogEvent logEvent);

        /// <summary>
        /// Delegate for track notifcations.
        /// </summary>
        /// <param name="eventKey">The event key</param>
        /// <param name="userId">The user identifier</param>
        /// <param name="userAttributes">Associative array of attributes for the user</param>
        /// <param name="eventTags">Associative array of EventTags representing metadata associated with the event</param>
        /// <param name="logEvent">The conversion event</param>
        public delegate void TrackCallback(string eventKey, string userId, UserAttributes userAttributes, EventTags eventTags,
            LogEvent logEvent);

        /// <summary>
        /// Delegate for decision notifications.
        /// </summary>
        /// <param name="type">Decision-Info type</param>
        /// <param name="userId">The user identifier</param>
        /// <param name="userAttributes">Associative array of attributes for the user</param>
        /// <param name="decisionInfo">Dictionary containing decision information</param>
        public delegate void DecisionCallback(string type, string userId, UserAttributes userAttributes, Dictionary<string, object> decisionInfo);

        /// <summary>
        /// Delegate for project config update.
        /// </summary>
        public delegate void OptimizelyConfigUpdateCallback();

        /// <summary>
        /// Delegate for batch-event flushing.
        /// </summary>
        /// <param name="logEvent">The log event</param>
        public delegate void LogEventCallback(LogEvent logEvent);

        private ILogger Logger;

        // Notification Id represeting number of notifications.
        public int NotificationId { get; private set; } = 1;

        // Associative array of notification type to notification id and notification pair.
        private Dictionary<NotificationType, Dictionary<int, object>> Notifications =
            new Dictionary<NotificationType, Dictionary<int, object>>();

        /// <summary>
        /// Property representing total notifications count.
        /// </summary>
        public int NotificationsCount {
            get {
                int notificationsCount = 0;
                foreach (var notificationsMap in Notifications.Values) {
                    notificationsCount += notificationsMap.Count;
                }

                return notificationsCount;
            }
        }

        /// <summary>
        /// NotificationCenter constructor
        /// </summary>
        /// <param name="logger">The logger object</param>
        public NotificationCenter(ILogger logger = null)
        {
            Logger = logger ?? new NoOpLogger();

            foreach (NotificationType notificationType in Enum.GetValues(typeof(NotificationType))) {
                Notifications[notificationType] = new Dictionary<int, object>();
            }
        }

        public int GetNotificationCount(NotificationType notificationType)
        {
            return Notifications.ContainsKey(notificationType) ? Notifications[notificationType].Count : 0;
        }

        /// <summary>
        /// Add a notification callback of decision type to the notification center.
        /// </summary>
        /// <param name="notificationType">Notification type</param>
        /// <param name="activateCallback">Callback function to call when event gets triggered</param>
        /// <returns>int | 0 for invalid notification type, -1 for adding existing notification
        /// or the notification id of newly added notification.</returns>
        [Obsolete("ActivateCallback is deprecated. Use DecisionCallback instead.")]
        public int AddNotification(NotificationType notificationType, ActivateCallback activateCallback)
        {
            if (!IsNotificationTypeValid(notificationType, NotificationType.Activate))
                return 0;

            return AddNotification(notificationType, (object)activateCallback);
        }

        /// <summary>
        /// Add a notification callback of track type to the notification center.
        /// </summary>
        /// <param name="notificationType">Notification type</param>
        /// <param name="trackCallback">Callback function to call when event gets triggered</param>
        /// <returns>int | 0 for invalid notification type, -1 for adding existing notification
        /// or the notification id of newly added notification.</returns>
        public int AddNotification(NotificationType notificationType, TrackCallback trackCallback)
        {
            if (!IsNotificationTypeValid(notificationType, NotificationType.Track))
                return 0;

            return AddNotification(notificationType, (object)trackCallback);
        }

        /// <summary>
        /// Add a notification callback of decision type to the notification center.
        /// </summary>
        /// <param name="notificationType">Notification type</param>
        /// <param name="decisionCallback">Callback function to call when event gets triggered</param>
        /// <returns>int | 0 for invalid notification type, -1 for adding existing notification
        /// or the notification id of newly added notification.</returns>
        public int AddNotification(NotificationType notificationType, DecisionCallback decisionCallback)
        {
            if (!IsNotificationTypeValid(notificationType, NotificationType.Decision))
                return 0;

            return AddNotification(notificationType, (object)decisionCallback);
        }

        /// <summary>
        /// Add a notification callback of config-update type to the notification center.
        /// </summary>
        /// <param name="notificationType">Notification type</param>
        /// <param name="optimizelyConfigUpdate">Callback function to call when event gets triggered</param>
        /// <returns>0 for invalid notification type, -1 for adding existing notification
        /// or the notification id of newly added notification.</returns>
        public int AddNotification(NotificationType notificationType, OptimizelyConfigUpdateCallback optimizelyConfigUpdate)
        {
            if (!IsNotificationTypeValid(notificationType, NotificationType.OptimizelyConfigUpdate))
                return 0;

            return AddNotification(notificationType, (object)optimizelyConfigUpdate);
        }

        /// <summary>
        /// Add a notification callback of logEvent type to the notification center.
        /// </summary>
        /// <param name="notificationType">Notification type</param>
        /// <param name="logEventCallback">Callback function to call when event gets triggered</param>
        /// <returns>0 for invalid notification type, -1 for adding existing notification
        /// or the notification id of newly added notification.</returns>
        public int AddNotification(NotificationType notificationType, LogEventCallback logEventCallback)
        {
            if (!IsNotificationTypeValid(notificationType, NotificationType.LogEvent))
                return 0;

            return AddNotification(notificationType, (object)logEventCallback);
        }

        /// <summary>
        /// Validate notification type.
        /// </summary>
        /// <param name="providedNotificationType">Provided notification type</param>
        /// <param name="expectedNotificationType">expected notification type</param>
        /// <returns>true if notification type is valid, false otherwise</returns>
        private bool IsNotificationTypeValid(NotificationType providedNotificationType, NotificationType expectedNotificationType)
        {
            if (providedNotificationType != expectedNotificationType) {
                Logger.Log(LogLevel.ERROR, $@"Invalid notification type provided for ""{expectedNotificationType}"" callback.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Add a notification callback to the notification center.
        /// </summary>
        /// <param name="notificationType">Notification type</param>
        /// <param name="notificationCallback">Callback function to call when event gets triggered</param>
        /// <returns> -1 for adding existing notification or the notification id of newly added notification.</returns>
        private int AddNotification(NotificationType notificationType, object notificationCallback)
        {
            var notificationHoldersList = Notifications[notificationType];

            if (!Notifications.ContainsKey(notificationType) || Notifications[notificationType].Count == 0)
                Notifications[notificationType][NotificationId] = notificationCallback;
            else {
                foreach (var notification in this.Notifications[notificationType]) {
                    if ((Delegate)notification.Value == (Delegate)notificationCallback) {
                        Logger.Log(LogLevel.ERROR, "The notification callback already exists.");
                        return -1;
                    }
                }

                Notifications[notificationType][NotificationId] = notificationCallback;
            }

            int retVal = NotificationId;
            NotificationId += 1;

            return retVal;
        }

        /// <summary>
        /// Remove a previously added notification callback.
        /// </summary>
        /// <param name="notificationId">Id of notification</param>
        /// <returns>Returns true if found and removed, false otherwise.</returns>
        public bool RemoveNotification(int notificationId)
        {
            foreach (var key in Notifications.Keys) {
                if (Notifications[key] != null && Notifications[key].Any(notification => notification.Key == notificationId)) {
                    Notifications[key].Remove(notificationId);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Remove all notifications for the specified notification type.
        /// </summary>
        /// <param name="notificationType">The notification type</param>
        public void ClearNotifications(NotificationType notificationType)
        {
            Notifications[notificationType].Clear();
        }

        /// <summary>
        /// Removes all notifications.
        /// </summary>
        public void ClearAllNotifications()
        {
            foreach (var notificationsMap in Notifications.Values) {
                notificationsMap.Clear();
            }
        }

        /// <summary>
        /// Fire notifications of specified notification type when the event gets triggered.
        /// </summary>
        /// <param name="notificationType">The notification type</param>
        /// <param name="args">Arguments to pass in notification callbacks</param>
        public void SendNotifications(NotificationType notificationType, params object[] args)
        {
            foreach (var notification in Notifications[notificationType]) {
                try {
                    Delegate d = notification.Value as Delegate;
                    d.DynamicInvoke(args);
                } catch (Exception exception) {
                    Logger.Log(LogLevel.ERROR, "Problem calling notify callback. Error: " + exception.Message);
                }
            }
        }
    }
}