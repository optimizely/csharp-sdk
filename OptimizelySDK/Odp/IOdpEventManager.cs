using OptimizelySDK.Odp.Entity;

namespace OptimizelySDK.Odp
{
    public interface IOdpEventManager
    {
        void UpdateSettings(OdpConfig odpConfig);

        void Start();

        void Stop();

        void RegisterVuid(string vuid);

        void IdentifyUser(string userId, string vuid);

        void SendEvent(OdpEvent odpEvent);
    }
}
