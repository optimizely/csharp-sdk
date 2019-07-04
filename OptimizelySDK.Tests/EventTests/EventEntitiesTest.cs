using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using NUnit.Framework;
using OptimizelySDK.Entity;
using OptimizelySDK.Event.Entity;

namespace OptimizelySDK.Tests.EventTests
{
    [TestFixture]
    public class EventEntitiesTest
    {
        [Test]
        public void TestLegacyPayloadEqualsEntityPayload()
        {
            ///TODO: Need to revise this UT.
            //var guid = Guid.NewGuid();
            //var timeStamp = TestData.SecondsSince1970();

            //var expectdPayload = new Dictionary<string, object>
            //    {
            //    {"client_version", Optimizely.SDK_VERSION},
            //    {"project_id", "111001"},
            //    {"enrich_decisions", true},
            //    {"account_id", "12001"},
            //    {"client_name", "csharp-sdk"},
            //    {"anonymize_ip", false},
            //    {"revision", "2"},
            //    {"visitors", new object[]
            //        {
            //            new Dictionary<string, object>
            //            {
            //                //visitors[0].attributes
            //                {
            //                    "attributes", new object[]
            //                    {
            //                        new Dictionary<string, string>
            //                        {
            //                            {"entity_id", "111094"},
            //                            {"type", "custom"},
            //                            {"value", "test_value"},
            //                            {"key", "test_attribute"}
            //                        }
            //                    }
            //                },
            //                //visitors[0].visitor_id
            //                {"visitor_id", "test_user"},
            //                //visitors[0].snapshots
            //                {"snapshots", new object[]
            //                    {
            //                        //snapshots[0]
            //                        new Dictionary<string, object>
            //                        {
            //                            //snapshots[0].events
            //                            {
            //                                "events", new object[]
            //                                {
            //                                    new Dictionary<string, object>
            //                                    {
            //                                        {"uuid", guid},
            //                                        {"timestamp", timeStamp},
            //                                        {"revenue", 4200},
            //                                        {"value", 1.234},
            //                                        {"key", "event_with_multiple_running_experiments"},
            //                                        {"entity_id", "111095"},
            //                                        {
            //                                            "tags", new Dictionary<string, object>
            //                                            {
            //                                                {"non-revenue", "abc"},
            //                                                {"revenue", 4200},
            //                                                {"value", 1.234},
            //                                            }
            //                                        }

            //                                    }
            //                                }
            //                            }

            //                        }

            //                    }
            //                }

            //            }
            //        }

            //    }
            //};
            
            //var eventBatch = new EventBatch(accountId: "12001", projectId: "111001", clientVersion: Optimizely.SDK_VERSION,
            //    revision: "2", clientName: "csharp-sdk", anonymizeIP: false, enrichDecisions: true);

            //var visitorAttribute = new VisitorAttribute(entityId: "111094", type: "custom", value: "test_value", key: "test_attribute");
            
            //var snapshotEvent = new SnapshotEvent(uuid: guid.ToString(), entityId: "111095", key: "event_with_multiple_running_experiments",
            //    value: (long?)1.234, revenue: 4200, timestamp: timeStamp, eventTags: new EventTags
            //    {
            //        {"non-revenue", "abc"},
            //        {"revenue", 4200},
            //        {"value", 1.234}
            //    });

            //var snapshot = new Snapshot(events: new SnapshotEvent[] { snapshotEvent });

            //var visitor = new Visitor(
            //    snapshots: new Snapshot[] {
            //        snapshot
            //    },
            //    attributes: new VisitorAttribute[]{
            //        visitorAttribute},
            //    visitorId: "test_user");

            //eventBatch.Visitors.Add(visitor);

            //// Single Conversion Event
            //TestData.CompareObjects(expectdPayload, eventBatch);            
        }
    }
}
