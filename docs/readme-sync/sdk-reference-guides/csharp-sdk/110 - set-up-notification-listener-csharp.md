---
title: "Set up notification listener"
slug: "set-up-notification-listener-csharp"
hidden: false
createdAt: "2019-09-12T13:44:24.921Z"
updatedAt: "2020-02-10T19:46:50.861Z"
---

Notification listeners trigger a callback function that you define when certain actions are triggered in the SDK.

The most common use case is to send a stream of all feature flag decisions to an analytics provider or to an internal data warehouse to join it with other data that you have about your users.

To track feature usage:
1. Sign up for an analytics provider of your choice (for example, Segment)
2. Set up a notification listener of type 'DECISION'
3. Follow your analytics provider's documentation and send events from within the decision listener callback

Steps 1 and 3 aren't covered in this documentation. However, setting up a 'DECISION' notification listener is covered below.

# Set up a DECISION notification listener

The `DECISION` notification listener enables you to be notified whenever the SDK determines what decision value to return for a feature. The callback is triggered with the decision type, associated decision information, user ID, and attributes.

`DECISION` listeners are triggered in multiple cases. Please see the tables at the end of this section for complete detail.

To set up a `DECISION` listener:
  1. Define a callback to be called when the `DECISION` event is triggered
  2. Add the callback to the notification center on the Optimizely instance
```csharp
using OptimizelySDK;
using OptimizelySDK.Entity;
using OptimizelySDK.Event;
using OptimizelySDK.Notifications;
using NotificationType = OptimizelySDK.Notifications.NotificationCenter.NotificationType;

var optimizelyClient = new Optimizely(datafile);

NotificationCenter.DecisionCallback OnDecision = (type, userId, userAttributes, decisionInfo) =>
{
  if (type == "feature")
  {
    Console.WriteLine(string.Format("Feature access related information: {0}", decisionInfo.ToString()));
    // Send data to analytics provider here
  }
};

// Add a Decision notification listener
int decisionListenerId = optimizely.NotificationCenter.AddNotification(NotificationType.Decision, OnDecision);

// Remove notification listener
optimizelyClient.NotificationCenter.RemoveNotification(decisionListenerId);

// Clear all notification listeners of a certain type
optimizelyClient.NotificationCenter.ClearNotifications(NotificationType.Decision);

// Clear all notifications
optimizelyClient.NotificationCenter.ClearAllNotifications();
```
The tables below show the information provided to the listener when it is triggered.
| Field         | Type   | Description                                                                                                                |
|---------------|--------|----------------------------------------------------------------------------------------------------------------------------|
| **type**      | string | - `feature`: Returned when you use the Is Feature Enabled to determine if the user has access to one specific feature, or Get Enabled Features method to determine if the user has access to multiple features.<br>- `ab-test`: Returned when you use activate or get_variation to determine the variation for a user, and the given experiment is not associated with any feature.<br>- `feature-test`: Returned when you use activate or get_variation to determine the variation for a user, and the given experiment is associated with some feature.<br>- `feature-variable`: Returned when you use one of the get_feature_variable methods to determine the value of some feature variable. Such as get_feature_variable_boolean. |
| **decision info** | map    | Key-value map that consists of data corresponding to the decision and based on the `type`.                                |
| **user ID**   | string | The user ID.                                                                                                               |
| **attributes** | map    | A map of custom key-value string pairs specifying attributes for the user that are used for audience targeting. Non-string values are only supported in the 3.0 SDK and above. |
---
title: "Decision Listener Information"
---

## Decision Info Values

### feature

| Field             | Decision Info Values                                                                                                 |
|-------------------|----------------------------------------------------------------------------------------------------------------------|
| **featureKey**     | String id of the feature.                                                                                           |
| **featureEnabled** | True or false based on whether the feature is enabled for the user.                                                 |
| **source**         | String denoting how user gained access to the feature. Value is:<br>   -  `feature-test` if the feature became enabled or disabled for the user because of some experiment associated with the feature.<br>   -  `rollout` if the feature became enabled or disabled for the user because of the rollout configuration associated with the feature. |
| **sourceInfo**     | Empty if the source is rollout. Holds `experimentKey` and `variationKey` if the source is feature-test.            |

### ab-test

| Field             | Decision Info Values                                   |
|-------------------|--------------------------------------------------------|
| **experimentKey** | String key of the experiment.                         |
| **variationKey**  | String key of the variation to which the user got bucketed. |

### feature-test

| Field             | Decision Info Values                                   |
|-------------------|--------------------------------------------------------|
| **experimentKey** | String key of the experiment.                         |
| **variationKey**  | String key of the variation to which the user got bucketed. |

### feature-variable

| Field             | Decision Info Values                                                                                                 |
|-------------------|----------------------------------------------------------------------------------------------------------------------|
| **featureKey**     | String id of the feature.                                                                                           |
| **featureEnabled** | True or false based on whether the feature is enabled for the user.                                                 |
| **source**         | String denoting how user gained access to the feature. Value is:<br>   -  `feature-test` if the feature became enabled or disabled for the user because of some experiment associated with the feature.<br>   -  `rollout` if the feature became enabled or disabled for the user because of the rollout configuration associated with the feature. |
| **variableKey**    | String key of the feature variable.                                                                                 |
| **variableValue**  | Mixed value of the feature variable for this user.                                                                  |
| **variableType**   | String type of the feature variable. Can be one of boolean, double, integer, string.                              |
| **sourceInfo**     | Map denoting source of decision. Empty if the source is rollout. Holds `experimentKey` and `variationKey` if the source is feature-test. |
