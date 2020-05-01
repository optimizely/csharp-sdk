---
title: "Customize error handler"
slug: "customize-error-handler-csharp"
hidden: false
createdAt: "2019-09-12T13:44:18.412Z"
updatedAt: "2019-09-12T13:48:38.080Z"
---
You can provide your own custom **error handler** logic to standardize across your production environment. 

This error handler is called when SDK is not executed as expected, it may be because of arguments provided to the SDK or running in an environment where network or any other disruptions occur.

See the code example below. If the error handler is not overridden, a no-op error handler is used by default.
[block:code]
{
  "codes": [
    {
      "code": "using System;\nusing OptimizelySDK.ErrorHandler;\n\n/**\n * Creates a CustomErrorHandler and calls HandleError when exception is raised by the SDK. \n **/\n/** CustomErrorHandler should be inherited by IErrorHandler, namespace of OptimizelySDK.ErrorHandler.\n **/\npublic class CustomErrorHandler : IErrorHandler\n{\n    /// <summary>\n    /// Handle exceptions when raised by the SDK.\n    /// </summary>\n    /// <param name=\"exception\">object of Exception raised by the SDK.</param>\n    public void HandleError(Exception exception)\n    {\n        throw new NotImplementedException();\n    }\n}\n\n",
      "language": "csharp"
    }
  ]
}
[/block]