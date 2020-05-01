---
title: "Pass in audience attributes"
slug: "pass-in-audience-attributes-csharp"
hidden: false
createdAt: "2019-09-12T13:44:29.855Z"
updatedAt: "2019-09-12T20:30:05.154Z"
---
You can pass strings, numbers, Booleans, and null as user attribute values. The example below shows how to pass in attributes.
[block:code]
{
  "codes": [
    {
      "code": "UserAttributes attributes = new UserAttributes\n{\n  { \"DEVICE\", \"iPhone\" },\n  { \"AD_SOURCE\", \"my_campaign\" }\n};\n\nbool enabled = OptimizelyClient.IsFeatureEnabled(\"new_feature\", \"user123\", attributes);\n\n",
      "language": "csharp"
    }
  ]
}
[/block]

[block:callout]
{
  "type": "warning",
  "title": "Important",
  "body": "During audience evaluation, note that if you don't pass a valid attribute value for a given audience condition—for example, if you pass a string when the audience condition requires a Boolean, or if you simply forget to pass a value—then that condition will be skipped. The [SDK logs](doc:customize-logger-csharp) will include warnings when this occurs."
}
[/block]