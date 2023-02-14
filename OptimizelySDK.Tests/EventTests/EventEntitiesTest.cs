﻿/**
 *
 *    Copyright 2019-2020, Optimizely and contributors
 *
 *    Licensed under the Apache License, Version 2.0 (the "License");
 *    you may not use this file except in compliance with the License.
 *    You may obtain a copy of the License at
 *
 *        http://www.apache.org/licenses/LICENSE-2.0
 *
 *    Unless required by applicable law or agreed to in writing, software
 *    distributed under the License is distributed on an "AS IS" BASIS,
 *    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *    See the License for the specific language governing permissions and
 *    limitations under the License.
 */


using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using NUnit.Framework;
using OptimizelySDK.Entity;
using OptimizelySDK.Event.Entity;
using OptimizelySDK.Utils;

namespace OptimizelySDK.Tests.EventTests
{
    [TestFixture]
    public class EventEntitiesTest
    {
        [Test]
        public void TestImpressionEventEqualsSerializedPayload()
        {
            var guid = Guid.NewGuid();
            var timeStamp = TestData.SecondsSince1970();
            var userId = "TestUserId";

            var expectedPayload = new Dictionary<string, object>
            {
                {
                    "visitors", new object[]
                    {
                        new Dictionary<string, object>()
                        {
                            {
                                "snapshots", new object[]
                                {
                                    new Dictionary<string, object>
                                    {
                                        {
                                            "decisions", new object[]
                                            {
                                                new Dictionary<string, object>
                                                {
                                                    { "campaign_id", "7719770039" },
                                                    { "experiment_id", "7716830082" },
                                                    { "variation_id", "77210100090" },
                                                },
                                            }
                                        },
                                        {
                                            "events", new object[]
                                            {
                                                new Dictionary<string, object>
                                                {
                                                    { "entity_id", "7719770039" },
                                                    { "timestamp", timeStamp },
                                                    { "uuid", guid },
                                                    { "key", "campaign_activated" },
                                                },
                                            }
                                        },
                                    },
                                }
                            },
                            {
                                "attributes", new object[]
                                {
                                    new Dictionary<string, object>
                                    {
                                        { "entity_id", "7723280020" },
                                        { "key", "device_type" },
                                        { "type", "custom" },
                                        { "value", "iPhone" },
                                    },
                                    new Dictionary<string, object>
                                    {
                                        { "entity_id", ControlAttributes.BOT_FILTERING_ATTRIBUTE },
                                        { "key", ControlAttributes.BOT_FILTERING_ATTRIBUTE },
                                        { "type", "custom" },
                                        { "value", true },
                                    },
                                }
                            },
                            { "visitor_id", userId },
                        },
                    }
                },
                { "project_id", "7720880029" },
                { "account_id", "1592310167" },
                { "enrich_decisions", true },
                { "client_name", "csharp-sdk" },
                { "client_version", Optimizely.SDK_VERSION },
                { "revision", "15" },
                { "anonymize_ip", false },
            };

            var builder = new EventBatch.Builder();
            builder.WithAccountId("1592310167").
                WithProjectID("7720880029").
                WithClientVersion(Optimizely.SDK_VERSION).
                WithRevision("15").
                WithClientName("csharp-sdk").
                WithAnonymizeIP(false).
                WithEnrichDecisions(true);

            var visitorAttribute1 = new VisitorAttribute("7723280020", type: "custom",
                value: "iPhone", key: "device_type");
            var visitorAttribute2 = new VisitorAttribute(ControlAttributes.BOT_FILTERING_ATTRIBUTE,
                type: "custom", value: true, key: ControlAttributes.BOT_FILTERING_ATTRIBUTE);
            var snapshotEvent = new SnapshotEvent.Builder().WithUUID(guid.ToString()).
                WithEntityId("7719770039").
                WithKey("campaign_activated").
                WithValue(null).
                WithRevenue(null).
                WithTimeStamp(timeStamp).
                WithEventTags(null).
                Build();
            var metadata = new DecisionMetadata("experiment", "experiment_key", "7716830082");
            var decision = new Decision("7719770039", "7716830082", "77210100090");
            var snapshot = new Snapshot(new SnapshotEvent[] { snapshotEvent },
                new Decision[] { decision });

            var visitor = new Visitor(
                new Snapshot[]
                {
                    snapshot,
                },
                new VisitorAttribute[]
                {
                    visitorAttribute1, visitorAttribute2,
                },
                "test_user");

            builder.WithVisitors(new Visitor[] { visitor });

            var eventBatch = builder.Build();
            // Single Conversion Event
            TestData.CompareObjects(expectedPayload, eventBatch);
        }

        [Test]
        public void TestConversionEventEqualsSerializedPayload()
        {
            var guid = Guid.NewGuid();
            var timeStamp = TestData.SecondsSince1970();

            var expectdPayload = new Dictionary<string, object>
            {
                { "client_version", Optimizely.SDK_VERSION },
                { "project_id", "111001" },
                { "enrich_decisions", true },
                { "account_id", "12001" },
                { "client_name", "csharp-sdk" },
                { "anonymize_ip", false },
                { "revision", "2" },
                {
                    "visitors", new object[]
                    {
                        new Dictionary<string, object>
                        {
                            //visitors[0].attributes
                            {
                                "attributes", new object[]
                                {
                                    new Dictionary<string, string>
                                    {
                                        { "entity_id", "111094" },
                                        { "type", "custom" },
                                        { "value", "test_value" },
                                        { "key", "test_attribute" },
                                    },
                                }
                            },
                            //visitors[0].visitor_id
                            { "visitor_id", "test_user" },
                            //visitors[0].snapshots
                            {
                                "snapshots", new object[]
                                {
                                    //snapshots[0]
                                    new Dictionary<string, object>
                                    {
                                        //snapshots[0].events
                                        {
                                            "events", new object[]
                                            {
                                                new Dictionary<string, object>
                                                {
                                                    { "uuid", guid },
                                                    { "timestamp", timeStamp },
                                                    { "revenue", 4200 },
                                                    { "value", 1.234 },
                                                    {
                                                        "key",
                                                        "event_with_multiple_running_experiments"
                                                    },
                                                    { "entity_id", "111095" },
                                                    {
                                                        "tags", new Dictionary<string, object>
                                                        {
                                                            { "non-revenue", "abc" },
                                                            { "revenue", 4200 },
                                                            { "value", 1.234 },
                                                        }
                                                    },
                                                },
                                            }
                                        },
                                    },
                                }
                            },
                        },
                    }
                },
            };

            var builder = new EventBatch.Builder();
            builder.WithAccountId("12001").
                WithProjectID("111001").
                WithClientVersion(Optimizely.SDK_VERSION).
                WithRevision("2").
                WithClientName("csharp-sdk").
                WithAnonymizeIP(false).
                WithEnrichDecisions(true);

            var visitorAttribute = new VisitorAttribute("111094", type: "custom",
                value: "test_value", key: "test_attribute");

            var snapshotEvent = new SnapshotEvent.Builder().WithUUID(guid.ToString()).
                WithEntityId("111095").
                WithKey("event_with_multiple_running_experiments").
                WithValue((long?)1.234).
                WithRevenue(4200).
                WithTimeStamp(timeStamp).
                WithEventTags(new EventTags
                {
                    { "non-revenue", "abc" },
                    { "revenue", 4200 },
                    { "value", 1.234 },
                }).
                Build();

            var snapshot = new Snapshot(new SnapshotEvent[] { snapshotEvent });

            var visitor = new Visitor(
                new Snapshot[]
                {
                    snapshot,
                },
                new VisitorAttribute[]
                {
                    visitorAttribute,
                },
                "test_user");

            builder.WithVisitors(new Visitor[] { visitor });

            var eventBatch = builder.Build();
            // Single Conversion Event
            TestData.CompareObjects(expectdPayload, eventBatch);
        }
    }
}
