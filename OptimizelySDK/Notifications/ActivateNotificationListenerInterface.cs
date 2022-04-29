using OptimizelySDK.Entity;
using OptimizelySDK.Event;

namespace OptimizelySDK.Notifications
{
    public interface ActivateNotificationListenerInterface
    {
        void OnActivate(Experiment experiment,
                           string userId,
                           UserAttributes attributes,
                           Variation variation,
                           LogEvent logEvent);
        }
    }
}
