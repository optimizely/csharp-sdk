---
title: "Implement a user profile service"
slug: "implement-a-user-profile-csharp"
hidden: false
createdAt: "2019-09-12T13:43:49.170Z"
updatedAt: "2019-09-12T13:45:24.114Z"
---
Use a **User Profile Service** to persist information about your users and ensure variation assignments are sticky. For example, if you are working on a backend website, you can create an implementation that reads and saves user profiles from a Redis or memcached store. 

In the C# SDK, there is no default implementation. Implementing a User Profile Service is optional and is only necessary if you want to keep variation assignments sticky even when experiment conditions are changed while it is running (for example, audiences, attributes, variation pausing, and traffic distribution). Otherwise, the C# SDK is stateless and rely on deterministic bucketing to return consistent assignments. See [How bucketing works](doc:how-bucketing-works) for more information.
## Implement a Service

To implement a User Profile Service, you can refer to the code samples provided below. Your User Profile Service should expose two functions with the following signatures:

- `lookup`: Takes a user ID string and returns a user profile matching the specified schema.
- `save`: Takes a user profile and persists it.

If you intend to use the User Profile Service purely for tracking purposes and not sticky bucketing, you can implement only the `save` method and always return `null` from the `lookup` method.

Here's an example implementation using C#:

```csharp
using System.Collections.Generic;
using OptimizelySDK;
using OptimizelySDK.Bucketing;

class InMemoryUserProfileService : UserProfileService
{
    private Dictionary<string, Dictionary<string, object>> userProfiles = new Dictionary<string, Dictionary<string, object>>();

    public override Dictionary<string, object> Lookup(string userId)
    {
        // Retrieve and return user profile
        // Replace with the actual userprofile variable
        return null;
    }

    public override void Save(Dictionary<string, object> userProfile)
    {
        // Save user profile
        // Implement the logic to persist the user profile data
    }
}

var optimizelyClient = new Optimizely(
    datafile: datafile,
    userProfileService: new InMemoryUserProfileService()
);
```
## User Profile JSON Schema

The following JSON schema represents the structure of a user profile object. This schema can be used to define user profiles within your User Profile Service.

Use the `experiment_bucket_map` field to override the default bucketing behavior and specify an alternate experiment variation for a given user. For each experiment that you want to override, add an object to the `experiment_bucket_map`. Use the experiment ID as the key and include a `variation_id` property that specifies the desired variation. If there is no entry for an experiment, the default bucketing behavior persists.

In the example below, `^[a-zA-Z0-9]+$` represents the pattern for an experiment ID:

```json
{
  "title": "UserProfile",
  "type": "object",
  "properties": {
    "user_id": {"type": "string"},
    "experiment_bucket_map": {
      "type": "object",
      "patternProperties": {
        "^[a-zA-Z0-9]+$": {
          "type": "object",
          "properties": {
            "variation_id": {"type": "string"}
          },
          "required": ["variation_id"]
        }
      }
    }
  },
  "required": ["user_id", "experiment_bucket_map"]
}
```
The C# SDK uses the User Profile Service you provide to override Optimizely's default bucketing behavior in cases when an experiment assignment has been saved.

When implementing your own User Profile Service, we recommend loading the user profiles into the User Profile Service on initialization and avoiding performing expensive, blocking lookups on the lookup function to minimize the performance impact of incorporating the service.

When implementing in a multi-server or stateless environment, we suggest using this interface with a backend like [Cassandra](http://cassandra.apache.org/) or [Redis](https://redis.io/). You can decide how long you want to keep your sticky bucketing around by configuring these services.