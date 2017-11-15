/* 
 * Copyright 2017, Optimizely
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
using NUnit.Framework;
using OptimizelySDK.Entity;
using OptimizelySDK.ErrorHandler;
using OptimizelySDK.Event;
using OptimizelySDK.Logger;
using OptimizelySDK.Notifications;

namespace OptimizelySDK.Tests.NotificationTests
{
    public class NotificationCenterTests
    {
        private Mock<ILogger> LoggerMock;
        private NotificationCenter NotificationCenter;
        private TestNotificationCallbacks TestNotificationCallbacks;
        private NotificationCenter.NotificationType NotificationTypeDecision = NotificationCenter.NotificationType.Decision;
        private NotificationCenter.NotificationType NotificationTypeTrack = NotificationCenter.NotificationType.Track;
        private NotificationCenter.NotificationType NotificationTypeFeatureAccess = NotificationCenter.NotificationType.FeatureAccess;

        [SetUp]
        public void Setup()
        {
            LoggerMock = new Mock<ILogger>();
            LoggerMock.Setup(i => i.Log(It.IsAny<LogLevel>(), It.IsAny<string>()));

            NotificationCenter = new NotificationCenter(LoggerMock.Object);
            TestNotificationCallbacks = new TestNotificationCallbacks();
        }

        [Test]
        public void TestAddAndRemoveNotificationListener()
        {
            // Verify that callback added successfully.
            Assert.AreEqual(1, NotificationCenter.AddNotification(NotificationTypeDecision, TestNotificationCallbacks.TestDecisionCallback));
            Assert.AreEqual(1, NotificationCenter.NotificationsCount);

            // Verify that callback removed successfully.
            Assert.AreEqual(true, NotificationCenter.RemoveNotification(1));
            Assert.AreEqual(0, NotificationCenter.NotificationsCount);
        }

        [Test]
        public void TestAddMultipleNotificationListeners()
        {
            NotificationCenter.AddNotification(NotificationTypeDecision, TestNotificationCallbacks.TestDecisionCallback);
            NotificationCenter.AddNotification(NotificationTypeDecision, TestNotificationCallbacks.TestAnotherDecisionCallback);

            // Verify that multiple notifications will be added for same notification type.
            Assert.AreEqual(2, NotificationCenter.NotificationsCount);

            // Verify that notifications of other types will also gets added successfully.
            NotificationCenter.AddNotification(NotificationTypeTrack, TestNotificationCallbacks.TestTrackCallback);
            Assert.AreEqual(3, NotificationCenter.NotificationsCount);
        }

        [Test]
        public void TestAddSameNotificationListenerMultipleTimes()
        {
            NotificationCenter.AddNotification(NotificationTypeDecision, TestNotificationCallbacks.TestDecisionCallback);

            // Verify that adding same callback multiple times will gets failed.
            Assert.AreEqual(-1, NotificationCenter.AddNotification(NotificationTypeDecision, TestNotificationCallbacks.TestDecisionCallback));
            Assert.AreEqual(1, NotificationCenter.NotificationsCount);
            LoggerMock.Verify(l => l.Log(LogLevel.ERROR, "The notification callback already exists."), Times.Once);
        }

        [Test]
        public void TestAddInvalidNotificationListeners()
        {
            // Verify that AddNotification gets failed on adding invalid notification listeners.
            Assert.AreEqual(0, NotificationCenter.AddNotification(NotificationTypeTrack,
                TestNotificationCallbacks.TestDecisionCallback));
            Assert.AreEqual(0, NotificationCenter.AddNotification(NotificationTypeFeatureAccess,
                TestNotificationCallbacks.TestTrackCallback));
            Assert.AreEqual(0, NotificationCenter.AddNotification(NotificationTypeDecision,
                TestNotificationCallbacks.TestFeatureAccessCallback));

            LoggerMock.Verify(l => l.Log(LogLevel.ERROR, $@"Invalid notification type provided for ""{NotificationTypeDecision}"" callback."),
                Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.ERROR, $@"Invalid notification type provided for ""{NotificationTypeTrack}"" callback."),
                Times.Once);
            LoggerMock.Verify(l => l.Log(LogLevel.ERROR, $@"Invalid notification type provided for ""{NotificationTypeFeatureAccess}"" callback."),
                Times.Once);

            // Verify that no notifion has been added.
            Assert.AreEqual(0, NotificationCenter.NotificationsCount);
        }

        [Test]
        public void TestClearNotifications()
        {
            // Add decision notifications.
            NotificationCenter.AddNotification(NotificationTypeDecision, TestNotificationCallbacks.TestDecisionCallback);
            NotificationCenter.AddNotification(NotificationTypeDecision, TestNotificationCallbacks.TestAnotherDecisionCallback);

            // Add track notification.
            NotificationCenter.AddNotification(NotificationTypeTrack, TestNotificationCallbacks.TestTrackCallback);

            // Verify that callbacks added successfully.
            Assert.AreEqual(3, NotificationCenter.NotificationsCount);

            // Verify that only decision callbacks are removed.
            NotificationCenter.ClearNotifications(NotificationTypeDecision);
            Assert.AreEqual(1, NotificationCenter.NotificationsCount);
            
            // Verify that ClearNotifications does not break on calling twice for same type.
            NotificationCenter.ClearNotifications(NotificationTypeDecision);
            NotificationCenter.ClearNotifications(NotificationTypeDecision);

            // Verify that ClearNotifications does not break after calling ClearAllNotifications.
            NotificationCenter.ClearAllNotifications();
            NotificationCenter.ClearNotifications(NotificationTypeTrack);
        }

        [Test]
        public void TestClearAllNotifications()
        {
            // Add decision notifications.
            NotificationCenter.AddNotification(NotificationTypeDecision, TestNotificationCallbacks.TestDecisionCallback);
            NotificationCenter.AddNotification(NotificationTypeDecision, TestNotificationCallbacks.TestAnotherDecisionCallback);

            // Add track notification.
            NotificationCenter.AddNotification(NotificationTypeTrack, TestNotificationCallbacks.TestTrackCallback);

            // Verify that callbacks added successfully.
            Assert.AreEqual(3, NotificationCenter.NotificationsCount);

            // Verify that ClearAllNotifications remove all the callbacks.
            NotificationCenter.ClearAllNotifications();
            Assert.AreEqual(0, NotificationCenter.NotificationsCount);

            // Verify that ClearAllNotifications does not break on calling twice or after ClearNotifications.
            NotificationCenter.ClearNotifications(NotificationTypeDecision);
            NotificationCenter.ClearAllNotifications();
            NotificationCenter.ClearAllNotifications();
        }

        [Test]
        public void TestFireNotifications()
        {
            var config = ProjectConfig.Create(TestData.Datafile, LoggerMock.Object, new NoOpErrorHandler());

            // Mocking notification callbacks.
            var notificationCallbackMock = new Mock<TestNotificationCallbacks>();
            notificationCallbackMock.Setup(nc => nc.TestDecisionCallback(It.IsAny<Experiment>(), It.IsAny<string>(),
                It.IsAny<UserAttributes>(), It.IsAny<Variation>(), It.IsAny<LogEvent>()));
            notificationCallbackMock.Setup(nc => nc.TestAnotherDecisionCallback(It.IsAny<Experiment>(), 
                It.IsAny<string>(), It.IsAny<UserAttributes>(), It.IsAny<Variation>(), It.IsAny<LogEvent>()));

            // Adding decision notifications.
            NotificationCenter.AddNotification(NotificationTypeDecision, notificationCallbackMock.Object.TestDecisionCallback);
            NotificationCenter.AddNotification(NotificationTypeDecision, notificationCallbackMock.Object.TestAnotherDecisionCallback);

            // Adding track notifications.
            NotificationCenter.AddNotification(NotificationTypeTrack, notificationCallbackMock.Object.TestTrackCallback);
            
            // Firing decision type notifications.
            NotificationCenter.FireNotifications(NotificationTypeDecision, config.GetExperimentFromKey("test_experiment"), 
                "testUser", new UserAttributes(), config.GetVariationFromId("test_experiment", "7722370027"), null);

            // Verify that only the registered notifications of decision type are called.
            notificationCallbackMock.Verify(nc => nc.TestDecisionCallback(It.IsAny<Experiment>(), It.IsAny<string>(), 
                It.IsAny<UserAttributes>(), It.IsAny<Variation>(), It.IsAny<LogEvent>()), Times.Exactly(1));
            notificationCallbackMock.Verify(nc => nc.TestAnotherDecisionCallback(It.IsAny<Experiment>(), It.IsAny<string>(),
                It.IsAny<UserAttributes>(), It.IsAny<Variation>(), It.IsAny<LogEvent>()), Times.Exactly(1));
            notificationCallbackMock.Verify(nc => nc.TestTrackCallback(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<UserAttributes>(), It.IsAny<EventTags>(), It.IsAny<LogEvent>()), Times.Never);

            // Verify that FireNotifications does not break when no notification exists.
            NotificationCenter.ClearAllNotifications();
            NotificationCenter.FireNotifications(NotificationTypeDecision, config.GetExperimentFromKey("test_experiment"),
                "testUser", new UserAttributes(), config.GetVariationFromId("test_experiment", "7722370027"), null);
        }
    }

    #region Test Notification callbacks class.

    /// <summary>
    /// Test class containing dummy notification callbacks.
    /// </summary>
    public class TestNotificationCallbacks
    {
        public virtual void TestDecisionCallback(Experiment experiment, string userId, UserAttributes userAttributes,
            Variation variation, LogEvent logEvent) {
        }

        public virtual void TestAnotherDecisionCallback(Experiment experiment, string userId, UserAttributes userAttributes,
            Variation variation, LogEvent logEvent) {
        }

        public static void TestStaticDecisionCallback(Experiment experiment, string userId, UserAttributes userAttributes,
            Variation variation, LogEvent logEvent) {
        }

        public virtual void TestTrackCallback(string eventKey, string userId, UserAttributes userAttributes,
            EventTags eventTags, LogEvent logEvent) {
        }

        public virtual void TestAnotherTrackCallback(string eventKey, string userId, UserAttributes userAttributes,
            EventTags eventTags, LogEvent logEvent) {
        }

        public virtual void TestFeatureAccessCallback(string featureKey, string userId, UserAttributes userAttributes,
            Variation variation) {
        }
    }
    #endregion // Test Notification callbacks class.
}
