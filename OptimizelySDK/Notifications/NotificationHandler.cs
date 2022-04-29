
namespace OptimizelySDK.Notifications
{
    public interface NotificationHandler<T>
    {
        void Handle(T message);
    }
}
