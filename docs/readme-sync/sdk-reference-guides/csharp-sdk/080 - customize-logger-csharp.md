---
title: "Customize logger"
slug: "customize-logger-csharp"
hidden: false
createdAt: "2019-09-12T13:44:11.768Z"
updatedAt: "2019-09-12T13:47:56.603Z"
---
The **logger** logs information about your experiments to help you with debugging. You can customize where log information is sent and what kind of information is tracked.

To improve your experience setting up the SDK and configuring your production environment, we recommend that you pass in a logger for your Optimizely client. See the code example below. 
[block:code]
{
  "codes": [
    {
      "code": "using OptimizelySDK.Logger;\n\n/**\n * Log a message at a certain level.\n * - Parameter level: The priority level of the log.\n * - Parameter message: The message to log.\n **/\npublic class CustomLogger : ILogger\n{\n    private LogLevel MinLogLevel;\n\n    public CustomLogger(LogLevel minLogLevel)\n    {\n        this.MinLogLevel = minLogLevel;\n    }\n    public void Log(LogLevel level, string message)\n    {\n        if (MinLogLevel <= level) {\n            switch (level) {\n                case LogLevel.DEBUG:\n                    // DEBUG log message\n                    break;\n                case LogLevel.INFO:\n                    // INFO log message\n                    break;\n                case LogLevel.WARN:\n                    // WARNING log message\n                    break;\n                case LogLevel.ERROR:\n                    // ERROR log message\n                    break;\n            }\n        }\n    }\n}\n\n",
      "language": "csharp"
    }
  ]
}
[/block]

[block:api-header]
{
  "title": "Log levels"
}
[/block]
The table below lists the log levels for the C# SDK.
[block:parameters]
{
  "data": {
    "h-0": "Log Level",
    "h-1": "Explanation",
    "0-0": "**OptimizelySDK.Logger.LogLevel.ERROR**",
    "0-1": "Events that prevent feature flags from functioning correctly (for example, invalid datafile in initialization and invalid feature keys) are logged. The user can take action to correct.",
    "1-0": "**OptimizelySDK.Logger.LogLevel.WARN**",
    "1-1": "Events that don't prevent feature flags from functioning correctly, but can have unexpected outcomes (for example, future API deprecation, logger or error handler are not set properly, and nil values from getters) are logged.",
    "2-0": "**OptimizelySDK.Logger.LogLevel.INFO**",
    "2-1": "Events of significance (for example, activate started, activate succeeded, tracking started, and tracking succeeded) are logged. This is helpful in showing the lifecycle of an API call.",
    "3-1": "Any information related to errors that can help us debug the issue (for example, the feature flag is not running, user is not included in the rollout) are logged.",
    "3-0": "**OptimizelySDK.Logger.LogLevel.DEBUG**"
  },
  "cols": 2,
  "rows": 4
}
[/block]