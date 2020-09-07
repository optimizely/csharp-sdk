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

using Moq;
using OptimizelySDK.Config;
using OptimizelySDK.Entity;
using OptimizelySDK.ErrorHandler;
using OptimizelySDK.Event;
using OptimizelySDK.Logger;
using OptimizelySDK.Notifications;
using System.Collections.Generic;
using Xunit;
using NotificationType = OptimizelySDK.Notifications.NotificationCenter.NotificationType;

namespace OptimizelySDK.XUnitTests.NotificationTests
{
    public class NotificationCenterTests
    {
        private Mock<ILogger> LoggerMock;
        private NotificationCenter NotificationCenter;
        private TestNotificationCallbacks TestNotificationCallbacks;

        private NotificationType NotificationTypeActivate = NotificationType.Activate;
        private NotificationType NotificationTypeTrack = NotificationType.Track;

        public NotificationCenterTests()
        {
            LoggerMock = new Mock<ILogger>();
            LoggerMock.Setup(i => i.Log(It.IsAny<LogLevel>(), It.IsAny<string>()));

            NotificationCenter = new NotificationCenter(LoggerMock.Object);
            TestNotificationCallbacks = new TestNotificationCallbacks();
        }

        [Fact]
        [System.Obsolete]
        public void TestAddAndRemoveNotificationListener()
        {
            // Verify that callback added successfully.
            Assert.Equal(1, NotificationCenter.AddNotification(NotificationTypeActivate, TestNotificationCallbacks.TestActivateCallback));
            Assert.Equal(1, NotificationCenter.NotificationsCount);

            // Verify that callback removed successfully.
            Assert.True(NotificationCenter.RemoveNotification(1));
            Assert.Equal(0, NotificationCenter.NotificationsCount);

            //Verify return false with invalid ID. 
            Assert.False(NotificationCenter.RemoveNotification(1));

            // Verify that callback added successfully and return right notification ID.
            Assert.Equal(NotificationCenter.NotificationId, NotificationCenter.AddNotification(NotificationTypeActivate, TestNotificationCallbacks.TestActivateCallback));
            Assert.Equal(1, NotificationCenter.NotificationsCount);
        }

        [Fact]
        [System.Obsolete]
        public void TestAddMultipleNotificationListeners()
        {
            NotificationCenter.AddNotification(NotificationTypeActivate, TestNotificationCallbacks.TestActivateCallback);
            NotificationCenter.AddNotification(NotificationTypeActivate, TestNotificationCallbacks.TestAnotherActivateCallback);

            // Verify that multiple notifications will be added for same notification type.
            Assert.Equal(2, NotificationCenter.NotificationsCount);

            // Verify that notifications of other types will also gets added successfully.
            NotificationCenter.AddNotification(NotificationTypeTrack, TestNotificationCallbacks.TestTrackCallback);
            Assert.Equal(3, NotificationCenter.NotificationsCount);

            // Verify that notifications of other types will also gets added successfully.
            NotificationCenter.AddNotification(NotificationType.OptimizelyConfigUpdate, TestNotificationCallbacks.TestConfigUpdateCallback);
            Assert.Equal(4, NotificationCenter.NotificationsCount);

            // Verify that notifications of other types will also gets added successfully.
            NotificationCenter.AddNotification(NotificationType.LogEvent, TestNotificationCallbacks.TestLogEventCallback);
            Assert.Equal(5, NotificationCenter.NotificationsCount);
        }

        [Fact]
        [System.Obsolete]
        public void TestAddSameNotificationListenerMultipleTimes()
        {
            NotificationCenter.AddNotification(NotificationTypeActivate, TestNotificationCallbacks.TestActivateCallback);

            // Verify that adding same callback multiple times will gets failed.
            Assert.Equal(-1, NotificationCenter.AddNotification(NotificationTypeActivate, TestNotificationCallbacks.TestActivateCallback));
            Assert.Equal(1, NotificationCenter.NotificationsCount);
            LoggerMock.Verify(l => l.Log(LogLevel.ERROR, "The notification callback already exists."), Times.Once);
        }

        [Fact]
        [System.Obsolete]
        public void TestAddInvalidNotificationListeners()
        {
            // Verify that AddNotification gets failed on adding invalid notification listeners.
            Assert.Equal(0, NotificationCenter.AddNotification(NotificationTypeTrack,
                TestNotificationCallbacks.TestActivateCallback));


            LoggerMock.Verify(l => l.Log(LogLevel.ERROR, $@"Invalid notification type provided for ""{NotificationTypeActivate}"" callback."),
                Times.Once);

            // Verify that no notifion has been added.
            Assert.Equal(0, NotificationCenter.NotificationsCount);
        }

        [Fact]
        [System.Obsolete]
        public void TestClearNotifications()
        {
            // Add decision notifications.
            NotificationCenter.AddNotification(NotificationTypeActivate, TestNotificationCallbacks.TestActivateCallback);
            NotificationCenter.AddNotification(NotificationTypeActivate, TestNotificationCallbacks.TestAnotherActivateCallback);

            // Add track notification.
            NotificationCenter.AddNotification(NotificationTypeTrack, TestNotificationCallbacks.TestTrackCallback);

            // Verify that callbacks added successfully.
            Assert.Equal(3, NotificationCenter.NotificationsCount);

            // Add config update callback.
            NotificationCenter.AddNotification(NotificationType.OptimizelyConfigUpdate, TestNotificationCallbacks.TestConfigUpdateCallback);
            // Verify that callbacks added successfully.
            Assert.Equal(4, NotificationCenter.NotificationsCount);


            // Verify that only decision callbacks are removed.
            NotificationCenter.ClearNotifications(NotificationTypeActivate);
            Assert.Equal(2, NotificationCenter.NotificationsCount);

            // Verify that ClearNotifications does not break on calling twice for same type.
            NotificationCenter.ClearNotifications(NotificationTypeActivate);
            NotificationCenter.ClearNotifications(NotificationTypeActivate);

            // Verify that ClearNotifications does not break after calling ClearAllNotifications.
            NotificationCenter.ClearAllNotifications();
            NotificationCenter.ClearNotifications(NotificationTypeTrack);
        }

        [Fact]
        [System.Obsolete]
        public void TestClearAllNotifications()
        {
            // Add decision notifications.
            NotificationCenter.AddNotification(NotificationTypeActivate, TestNotificationCallbacks.TestActivateCallback);
            NotificationCenter.AddNotification(NotificationTypeActivate, TestNotificationCallbacks.TestAnotherActivateCallback);

            // Add track notification.
            NotificationCenter.AddNotification(NotificationTypeTrack, TestNotificationCallbacks.TestTrackCallback);

            // Verify that callbacks added successfully.
            Assert.Equal(3, NotificationCenter.NotificationsCount);

            // Verify that ClearAllNotifications remove all the callbacks.
            NotificationCenter.ClearAllNotifications();
            Assert.Equal(0, NotificationCenter.NotificationsCount);

            // Verify that ClearAllNotifications does not break on calling twice or after ClearNotifications.
            NotificationCenter.ClearNotifications(NotificationTypeActivate);
            NotificationCenter.ClearAllNotifications();
            NotificationCenter.ClearAllNotifications();
        }

        [Fact]
        [System.Obsolete]
        public void TestSendNotifications()
        {
            var config = DatafileProjectConfig.Create(TestData.Datafile, LoggerMock.Object, new NoOpErrorHandler());
            var logEventMocker = new Mock<LogEvent>("http://mockedurl", new Dictionary<string, object>(), "POST", new Dictionary<string, string>());
            // Mocking notification callbacks.
            var notificationCallbackMock = new Mock<TestNotificationCallbacks>();

            notificationCallbackMock.Setup(nc => nc.TestActivateCallback(It.IsAny<Experiment>(), It.IsAny<string>(),
                It.IsAny<UserAttributes>(), It.IsAny<Variation>(), It.IsAny<LogEvent>()));

            notificationCallbackMock.Setup(nc => nc.TestAnotherActivateCallback(It.IsAny<Experiment>(),
                It.IsAny<string>(), It.IsAny<UserAttributes>(), It.IsAny<Variation>(), It.IsAny<LogEvent>()));

            notificationCallbackMock.Setup(nc => nc.TestLogEventCallback(It.IsAny<LogEvent>()));

            // Adding decision notifications.
            NotificationCenter.AddNotification(NotificationTypeActivate, notificationCallbackMock.Object.TestActivateCallback);
            NotificationCenter.AddNotification(NotificationTypeActivate, notificationCallbackMock.Object.TestAnotherActivateCallback);

            // Adding track notifications.
            NotificationCenter.AddNotification(NotificationTypeTrack, notificationCallbackMock.Object.TestTrackCallback);

            // Fire decision type notifications.
            NotificationCenter.SendNotifications(NotificationTypeActivate, config.GetExperimentFromKey("test_experiment"),
                "testUser", new UserAttributes(), config.GetVariationFromId("test_experiment", "7722370027"), logEventMocker.Object);

            // Verify that only the registered notifications of decision type are called.
            notificationCallbackMock.Verify(nc => nc.TestActivateCallback(It.IsAny<Experiment>(), It.IsAny<string>(),
                It.IsAny<UserAttributes>(), It.IsAny<Variation>(), It.IsAny<LogEvent>()), Times.Once);

            notificationCallbackMock.Verify(nc => nc.TestAnotherActivateCallback(It.IsAny<Experiment>(), It.IsAny<string>(),
                It.IsAny<UserAttributes>(), It.IsAny<Variation>(), It.IsAny<LogEvent>()), Times.Once);

            notificationCallbackMock.Verify(nc => nc.TestTrackCallback(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<UserAttributes>(), It.IsAny<EventTags>(), It.IsAny<LogEvent>()), Times.Never);

            // Add logEvent Notification.
            NotificationCenter.AddNotification(NotificationType.LogEvent, notificationCallbackMock.Object.TestLogEventCallback);

            // Fire logEvent Notification.
            NotificationCenter.SendNotifications(NotificationType.LogEvent, logEventMocker.Object);

            // Verify that registered notifications of logEvent type are called.
            notificationCallbackMock.Verify(nc => nc.TestLogEventCallback(It.IsAny<LogEvent>()), Times.Once);

            // Verify that after clearing notifications, SendNotification should not call any notification
            // which were previously registered. 
            NotificationCenter.ClearAllNotifications();
            notificationCallbackMock.ResetCalls();

            NotificationCenter.SendNotifications(NotificationTypeActivate, config.GetExperimentFromKey("test_experiment"),
                "testUser", new UserAttributes(), config.GetVariationFromId("test_experiment", "7722370027"), null);


            // Again verify notifications which were registered are not called. 
            notificationCallbackMock.Verify(nc => nc.TestActivateCallback(It.IsAny<Experiment>(), It.IsAny<string>(),
                It.IsAny<UserAttributes>(), It.IsAny<Variation>(), It.IsAny<LogEvent>()), Times.Never);

            notificationCallbackMock.Verify(nc => nc.TestAnotherActivateCallback(It.IsAny<Experiment>(), It.IsAny<string>(),
                It.IsAny<UserAttributes>(), It.IsAny<Variation>(), It.IsAny<LogEvent>()), Times.Never);

            notificationCallbackMock.Verify(nc => nc.TestTrackCallback(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<UserAttributes>(), It.IsAny<EventTags>(), It.IsAny<LogEvent>()), Times.Never);
        }

    }

    #region Test Notification callbacks class.

    /// <summary>
    /// Test class containing dummy notification callbacks.
    /// </summary>
    public class TestNotificationCallbacks
    {
        public virtual void TestActivateCallback(Experiment experiment, string userId, UserAttributes userAttributes,
            Variation variation, LogEvent logEvent) {
        }

        public virtual void TestAnotherActivateCallback(Experiment experiment, string userId, UserAttributes userAttributes,
            Variation variation, LogEvent logEvent) {
        }
        
        public virtual void TestTrackCallback(string eventKey, string userId, UserAttributes userAttributes,
            EventTags eventTags, LogEvent logEvent) {
        }

        public virtual void TestAnotherTrackCallback(string eventKey, string userId, UserAttributes userAttributes,
            EventTags eventTags, LogEvent logEvent) {
        }
        
        public virtual void TestDecisionCallback(string type, string userId, UserAttributes userAttributes,
            Dictionary<string, object> decisionInfo) {
        }

        public virtual void TestConfigUpdateCallback() {
        }

        public virtual void TestLogEventCallback(LogEvent logEvent) {
        }
    }
    #endregion // Test Notification callbacks class.
}
