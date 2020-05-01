---
title: "Activate"
slug: "activate-csharp"
hidden: true
createdAt: "2019-09-12T13:51:40.641Z"
updatedAt: "2019-09-12T20:31:54.396Z"
---
Activates an A/B test for the specified user to start an experiment: determines whether they qualify for the experiment, buckets a qualified user into a variation, and sends an impression event to Optimizely.
[block:api-header]
{
  "title": "Version"
}
[/block]
3.1.1
[block:api-header]
{
  "title": "Description"
}
[/block]
This method requires an experiment key, user ID, and (optionally) attributes. The experiment key must match the experiment key you created when you set up the experiment in the Optimizely app. The user ID string uniquely identifies the participant in the experiment.

If the user qualifies for the experiment, the method returns the variation key that was chosen. If the user was not eligible—for example, because the experiment was not running in this environment or the user didn't match the targeting attributes and audience conditions—then the method returns null.

Activate respects the configuration of the experiment specified in the datafile. The method:
 * Evaluates the user attributes for audience targeting.
 * Includes the user attributes in the impression event to support [results segmentation](doc:analyze-results#section-segment-results).
 * Hashes the user ID or bucketing ID to apply traffic allocation.
 * Respects forced bucketing and whitelisting.
 * Triggers an impression event if the user qualifies for the experiment.

Activate also respects customization of the SDK client. Throughout this process, this method:
  * Logs its decisions via the logger.
  * Triggers impressions via the event dispatcher.
  * Raises errors via the error handler.
  * Remembers variation assignments via the User Profile Service.
  * Alerts notification listeners, as applicable.
[block:callout]
{
  "type": "info",
  "title": "Note",
  "body": "For more information on how the variation is chosen, see [How bucketing works](how-bucketing-works)."
}
[/block]

[block:api-header]
{
  "title": "Parameters"
}
[/block]
The parameter names for C# are listed below.
[block:parameters]
{
  "data": {
    "h-0": "Parameter",
    "h-1": "Type",
    "h-2": "Description",
    "0-0": "**experiment key**\n*required*",
    "0-1": "string",
    "1-0": "**user ID**\n*required*",
    "1-1": "string",
    "0-2": "The experiment to activate.",
    "1-2": "The user ID.",
    "2-0": "**userAttributes**\n*optional*",
    "2-1": "map",
    "2-2": "A map of custom key-value string pairs specifying attributes for the user that are used for audience targeting and results segmentation. Non-string values are only supported in the 3.0 SDK and above."
  },
  "cols": 3,
  "rows": 3
}
[/block]

[block:api-header]
{
  "title": "Returns"
}
[/block]
<returns>null|Variation Representing variation</returns>
[block:api-header]
{
  "title": "Example"
}
[/block]

[block:code]
{
  "codes": [
    {
      "code": "using OptimizelySDK.Entity;\n\nvar attributes = new UserAttributes {\n  { \"device\", \"iPhone\" },\n  { \"lifetime\", 24738388 },\n  { \"is_logged_in\", true },\n};\n\nvar variation = optimizelyClient.Activate(\"my_experiment_key\", \"user_123\", attributes);\n\n",
      "language": "csharp"
    }
  ]
}
[/block]

[block:api-header]
{
  "title": "Side effects"
}
[/block]
The table lists other other Optimizely functionality that may be triggered by using this method.
[block:parameters]
{
  "data": {
    "h-0": "Functionality",
    "h-1": "Description",
    "0-0": "Impressions",
    "0-1": "Accessing this method triggers an impression if the user is included in an active A/B test. \n\nSee [Implement impressions](doc:implement-impressions) for guidance on when to use Activate versus [Get Variation](doc:get-variation-csharp).",
    "1-0": "Notification Listeners",
    "1-1": "In SDKs v3.0 and earlier: Activate invokes the `ACTIVATE` [notification listener](doc:set-up-notification-listener-csharp) if the user is included in an active A/B test.\n\nIn SDKs v3.1 and later: Invokes the `DECISION` notification listener if this listener is enabled."
  },
  "cols": 2,
  "rows": 2
}
[/block]

[block:api-header]
{
  "title": "Notes"
}
[/block]
### Activate versus Get Variation
Use Activate when the visitor actually sees the experiment. Use Get Variation when you need to know which bucket a visitor is in before showing the visitor the experiment. Impressions are tracked by [Is Feature Enabled](doc:is-feature-enabled) when there is a feature test running on the feature and the visitor qualifies for that feature test.

For example, suppose you want your web server to show a visitor variation_1 but don't want the visitor to count until they open a feature that isn't visible when the variation loads, like a modal. In this case, use Get Variation in the backend to specify that your web server should respond with variation_1, and use Activate in the front end when the visitor sees the experiment.

Also, use Get Variation when you're trying to align your Optimizely results with client-side third-party analytics. In this case, use Get Variation to retrieve the variation&mdash;and even show it to the visitor&mdash;but only call Activate when the analytics call goes out.

See [Implement impressions](doc:implement-impressions) for more information about whether to use Activate or Get Variation for a call.
[block:callout]
{
  "type": "info",
  "title": "Note",
  "body": "Conversion events can only be attributed to experiments with previously tracked impressions. Impressions are tracked by Activate, not by Get Variation. As a general rule, Optimizely impressions are required for experiment results and not only for billing."
}
[/block]

[block:api-header]
{
  "title": "Source files"
}
[/block]
The language/platform source files containing the implementation for C# is [Optimizely.cs](https://github.com/optimizely/csharp-sdk/blob/master/OptimizelySDK/Optimizely.cs).