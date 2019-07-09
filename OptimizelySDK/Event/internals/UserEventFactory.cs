using OptimizelySDK.Entity;
using OptimizelySDK.Event.Entity;

namespace OptimizelySDK.Event.internals
{
    public class UserEventFactory
    {
        public static ImpressionEvent CreateImpressionEvent(ProjectConfig projectConfig,
                                                            Experiment activatedExperiment,
                                                            Variation variation,
                                                            string userId,
                                                            VisitorAttribute[] visitorAttributes)
        {

            var eventContext = new EventContext.Builder()
                .WithAccountId(projectConfig.AccountId)
                .WithAnonymizeIP(projectConfig.AnonymizeIP)
                .WithRevision(projectConfig.Revision)
                .Build();

            return new ImpressionEvent.Builder()
                .WithEventContext(eventContext)
                .WithBotFilteringEnabled(projectConfig.BotFiltering)
                .WithExperiment(activatedExperiment)
                .WithUserId(userId)
                .WithVariation(variation)
                .WithVisitorAttributes(visitorAttributes)
                .Build();
            
        }

        public static ConversionEvent CreateConversionEvent(ProjectConfig projectConfig,
                                                            string userId,
                                                            string eventId,
                                                            VisitorAttribute[] visitorAttributes,
                                                            EventTags eventTags)
        {
            

            var eventContext = new EventContext.Builder()
                    .WithAccountId(projectConfig.AccountId)
                    .WithAnonymizeIP(projectConfig.AnonymizeIP)
                    .WithRevision(projectConfig.Revision)
                    .Build();

            return new ConversionEvent.Builder()
                .WithBotFilteringEnabled(projectConfig.BotFiltering)
                .WithEventContext(eventContext)
                .WithEventTags(eventTags)
                .WithEvent(projectConfig.GetEvent(eventId))
                .WithUserId(userId)
                .WithVisitorAttributes(visitorAttributes)
                .Build();
            
        }
    }
}
