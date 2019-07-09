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
                                                            UserAttributes userAttributes)
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
                .WithVisitorAttributes(EventFactory.BuildAttributeList(userAttributes, projectConfig))
                .Build();
            
        }

        public static ConversionEvent CreateConversionEvent(ProjectConfig projectConfig,
                                                            string userId,
                                                            string eventId,
                                                            UserAttributes userAttributes,
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
                .WithVisitorAttributes(EventFactory.BuildAttributeList(userAttributes, projectConfig))
                .Build();
            
        }
    }
}
