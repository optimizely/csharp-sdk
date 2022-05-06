using OptimizelySDK.Config.audience;

namespace OptimizelySDK.Entity
{
    public class NewAudience
    {
        /// <summary>
        /// Audience ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Audience Name
        /// </summary>
        public string Name { get; set; }

        // TODO need to ask if Object will work
        public Condition conditions { get; set; }

    }
}
