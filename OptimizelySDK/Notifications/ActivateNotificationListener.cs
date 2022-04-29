using OptimizelySDK.Entity;
using OptimizelySDK.Event;
using System;

namespace OptimizelySDK.Notifications
{
    public abstract class ActivateNotificationListener : NotificationListener, NotificationHandler<ActivateNotification>, ActivateNotificationListenerInterface
    {
        public void Handle(ActivateNotification message)
        {
            throw new NotImplementedException();
        }

        public void Notify(params object[] args)
        {
            throw new NotImplementedException();
        }

        public abstract void OnActivate(Experiment experiment,
                                    string userId,
                                    UserAttributes attributes,
                                    Variation variation,
                                    LogEvent logEvent);
        }
}
