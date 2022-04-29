using OptimizelySDK.Entity;
using OptimizelySDK.Event;

namespace OptimizelySDK.Notifications
{
    public  class ActivateNotification
    {
        private readonly Experiment _Experiment;
        private readonly string UserId;
        private readonly UserAttributes Attributes;
        private readonly Variation Variation;
        private readonly LogEvent Event;
        public ActivateNotification(Experiment experiment, string userId, UserAttributes attributes, Variation variation, LogEvent logEvent) {
            _Experiment = experiment;
            UserId = userId;
            Attributes = attributes;
            Variation = variation;
            Event = logEvent;
        }

        public Experiment GetExperiment()
        {
            return _Experiment;
        }

        public string GetUserId()
        {
            return UserId;
        }

        public UserAttributes GetAttributes()
        {
            return Attributes;
        }

        public Variation GetVariation()
        {
            return Variation;
        }

        public LogEvent GetEvent()
        {
            return Event;
        }

    }
}
