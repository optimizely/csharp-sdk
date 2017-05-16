﻿/* 
 * Copyright 2017, Optimizely
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
using System.IO;
using System.Reflection;

namespace OptimizelySDK.Tests
{
    public class TestData
    {
        private static string cachedDataFile = null;
        private static string validCachedDataFileV3 = null;
        private static string noAudienceProjectConfigV3 = null;


        public static string Datafile
        {
            get
            {
                return cachedDataFile ?? (cachedDataFile = LoadJsonData());
            }
        }

		public static string NoAudienceProjectConfigV3
		{
			get
			{
				return noAudienceProjectConfigV3 ?? (noAudienceProjectConfigV3 = LoadJsonData("NoAudienceProjectConfigV3.json"));
			}
		}

        public static string ValidCachedDataFileV3
		{
			get
			{
				return validCachedDataFileV3 ?? (validCachedDataFileV3 = LoadJsonData("ValidCachedDataFileV3.json"));
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
            var jtoken1 = JToken.Parse(Newtonsoft.Json.JsonConvert.SerializeObject(o1));
            var jtoken2 = JToken.Parse(Newtonsoft.Json.JsonConvert.SerializeObject(o2));

            return JToken.DeepEquals(jtoken1, jtoken2);
        }

    }
}
