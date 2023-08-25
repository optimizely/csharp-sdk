---
title: "Customize logger"
slug: "customize-logger-csharp"
hidden: false
createdAt: "2019-09-12T13:44:11.768Z"
updatedAt: "2019-09-12T13:47:56.603Z"
---

The **logger** logs information about your experiments to help you with debugging. You can customize where log information is sent and what kind of information is tracked.

To improve your experience setting up the SDK and configuring your production environment, we recommend that you pass in a logger for your Optimizely client. See the code example below.

```csharp
using OptimizelySDK.Logger;

/**
 * Log a message at a certain level.
 * - Parameter level: The priority level of the log.
 * - Parameter message: The message to log.
 **/
public class CustomLogger : ILogger
{
    private LogLevel MinLogLevel;

    public CustomLogger(LogLevel minLogLevel)
    {
        this.MinLogLevel = minLogLevel;
    }

    public void Log(LogLevel level, string message)
    {
        if (MinLogLevel <= level) {
            switch (level) {
                case LogLevel.DEBUG:
                    // DEBUG log message
                    break;
                case LogLevel.INFO:
                    // INFO log message
                    break;
                case LogLevel.WARN:
                    // WARNING log message
                    break;
                case LogLevel.ERROR:
                    // ERROR log message
                    break;
            }
        }
    }
}
```
# Log levels

The table below lists the log levels for the C# SDK.

| Log Level                            | Explanation                                                                                                          |
|--------------------------------------|----------------------------------------------------------------------------------------------------------------------|
| **OptimizelySDK.Logger.LogLevel.ERROR** | Events that prevent feature flags from functioning correctly (for example, invalid datafile in initialization and invalid feature keys) are logged. The user can take action to correct. |
| **OptimizelySDK.Logger.LogLevel.WARN**  | Events that don't prevent feature flags from functioning correctly, but can have unexpected outcomes (for example, future API deprecation, logger or error handler are not set properly, and nil values from getters) are logged. |
| **OptimizelySDK.Logger.LogLevel.INFO**  | Events of significance (for example, activate started, activate succeeded, tracking started, and tracking succeeded) are logged. This is helpful in showing the lifecycle of an API call. |
| **OptimizelySDK.Logger.LogLevel.DEBUG** | Any information related to errors that can help us debug the issue (for example, the feature flag is not running, user is not included in the rollout) are logged. |
