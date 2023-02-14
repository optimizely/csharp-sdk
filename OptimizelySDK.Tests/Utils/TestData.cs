﻿/* 
 * Copyright 2017-2020, 2022-2023 Optimizely
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json.Linq;

namespace OptimizelySDK.Tests
{
    public class TestData
    {
        private static string cachedDataFile = null;
        private static string simpleABExperimentsDatafile = null;
        private static string unsupportedVersionDatafile = null;
        private static string typedAudienceDatafile = null;
        private static string emptyRolloutDatafile = null;
        private static string emptyDatafile = null;
        private static string duplicateExpKeysDatafile = null;
        private static string duplicateRuleKeysDatafile = null;
        private static string odpSegmentsDatafile = null;

        private static string emptyIntegrationDatafile = null;
        private static string nonOdpIntegrationDatafile = null;
        private static string odpIntegrationDatafile = null;
        private static string odpIntegrationWithOtherFieldsDatafile = null;

        public static string Datafile => cachedDataFile ?? (cachedDataFile = LoadJsonData());

        public static string DuplicateExpKeysDatafile =>
            duplicateExpKeysDatafile ??
            (duplicateExpKeysDatafile = LoadJsonData("similar_exp_keys.json"));

        public static string DuplicateRuleKeysDatafile =>
            duplicateRuleKeysDatafile ?? (duplicateRuleKeysDatafile =
                LoadJsonData("similar_rule_keys_bucketing.json"));

        public static string SimpleABExperimentsDatafile =>
            simpleABExperimentsDatafile ?? (simpleABExperimentsDatafile =
                LoadJsonData("simple_ab_experiments.json"));

        public static string UnsupportedVersionDatafile =>
            unsupportedVersionDatafile ?? (unsupportedVersionDatafile =
                LoadJsonData("unsupported_version_datafile.json"));

        public static string EmptyRolloutDatafile =>
            emptyRolloutDatafile ??
            (emptyRolloutDatafile = LoadJsonData("EmptyRolloutRule.json"));

        public static string EmptyDatafile =>
            emptyDatafile ?? (emptyDatafile = LoadJsonData("emptydatafile.json"));

        public static string EmptyIntegrationDatafile =>
            emptyIntegrationDatafile ?? (emptyIntegrationDatafile =
                LoadJsonData("IntegrationEmptyDatafile.json"));

        public static string NonOdpIntegrationDatafile =>
            nonOdpIntegrationDatafile ?? (nonOdpIntegrationDatafile =
                LoadJsonData("IntegrationNonOdpDatafile.json"));

        public static string OdpIntegrationDatafile =>
            odpIntegrationDatafile ?? (odpIntegrationDatafile =
                LoadJsonData("IntegrationOdpDatafile.json"));

        public static string OdpIntegrationWithOtherFieldsDatafile =>
            odpIntegrationWithOtherFieldsDatafile ??
            (odpIntegrationWithOtherFieldsDatafile =
                LoadJsonData("IntegrationOdpWithOtherFieldsDatafile.json"));

        public static string TypedAudienceDatafile =>
            typedAudienceDatafile ?? (typedAudienceDatafile =
                LoadJsonData("typed_audience_datafile.json"));

        public static string OdpSegmentsDatafile =>
            odpSegmentsDatafile ??
            (odpSegmentsDatafile = LoadJsonData("OdpSegmentsDatafile.json"));

        private static string LoadJsonData(string fileName = "TestData.json")
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = string.Format("OptimizelySDK.Tests.{0}", fileName);

            using (var stream = assembly.GetManifestResourceStream(resourceName))
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }

        public static bool CompareObjects(object o1, object o2)
        {
            var str1 = Newtonsoft.Json.JsonConvert.SerializeObject(o1);
            var str2 = Newtonsoft.Json.JsonConvert.SerializeObject(o2);
            var jtoken1 = JToken.Parse(str1);
            var jtoken2 = JToken.Parse(str2);

            return JToken.DeepEquals(jtoken1, jtoken2);
        }

        public static long SecondsSince1970()
        {
            return (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
        }

        public static void ChangeGUIDAndTimeStamp(Dictionary<string, object> paramsObj,
            long timeStamp, Guid guid
        )
        {
            // Path from where to find
            // visitors.[0].snapshots.[0].events.[0].uuid or timestamp

            var visitor = (paramsObj["visitors"] as object[])[0] as Dictionary<string, object>;

            var snapshot = (visitor["snapshots"] as object[])[0] as Dictionary<string, object>;

            var @event = (snapshot["events"] as object[])[0] as Dictionary<string, object>;

            @event["uuid"] = guid;
            @event["timestamp"] = timeStamp;
        }
    }
}
