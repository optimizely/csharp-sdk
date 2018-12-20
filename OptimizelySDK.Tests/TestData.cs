/* 
 * Copyright 2017-2018, Optimizely
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
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace OptimizelySDK.Tests
{
    public class TestData
    {
        private static string cachedDataFile = null;
        private static string simpleABExperimentsDatafile = null;
        private static string unsupportedVersionDatafile = null;
        private static string typedAudienceDatafile = null;

        public static string Datafile
        {
            get
            {
                return cachedDataFile ?? (cachedDataFile = LoadJsonData());
            }
        }

        public static string SimpleABExperimentsDatafile
        {
            get 
            {
                return simpleABExperimentsDatafile ?? (simpleABExperimentsDatafile = LoadJsonData("simple_ab_experiments.json"));                
            }
        }

        public static string UnsupportedVersionDatafile
        {
            get
            {
                return unsupportedVersionDatafile ?? (unsupportedVersionDatafile = LoadJsonData("unsupported_version_datafile.json"));
            }
        }

        public static string TypedAudienceDatafile
        {
            get
            {
                return typedAudienceDatafile ?? (typedAudienceDatafile = LoadJsonData("typed_audience_datafile.json"));
            }
        }

        private static string LoadJsonData(string fileName = "TestData.json")
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = string.Format("OptimizelySDK.Tests.{0}", fileName);

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
                return reader.ReadToEnd();
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

        public static void ChangeGUIDAndTimeStamp(Dictionary<string, object> paramsObj, long timeStamp, Guid guid)
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
