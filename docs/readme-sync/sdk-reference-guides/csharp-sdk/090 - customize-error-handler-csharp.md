---
title: "Customize error handler"
slug: "customize-error-handler-csharp"
hidden: false
createdAt: "2019-09-12T13:44:18.412Z"
updatedAt: "2019-09-12T13:48:38.080Z"
---

You can provide your own custom **error handler** logic to standardize across your production environment.

This error handler is called when the SDK is not executed as expected, which may be due to arguments provided to the SDK or running in an environment where network or other disruptions occur.

See the code example below. If the error handler is not overridden, a no-op error handler is used by default.

```csharp
using System;
using OptimizelySDK.ErrorHandler;

/**
 * Creates a CustomErrorHandler and calls HandleError when an exception is raised by the SDK.
 **/
/** CustomErrorHandler should be inherited by IErrorHandler, a namespace of OptimizelySDK.ErrorHandler.
 **/
public class CustomErrorHandler : IErrorHandler
{
    /// <summary>
    /// Handle exceptions when raised by the SDK.
    /// </summary>
    /// <param name="exception">object of Exception raised by the SDK.</param>
    public void HandleError(Exception exception)
    {
        throw new NotImplementedException();
    }
}
```