/* 
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
using System.IO;
using System.Reflection;

namespace OptimizelySDK.Utils
{
    internal class Schema
    {
        private static string cache = null;

        public static string GetSchemaJson()
        {
            if (cache != null)
                return cache;

            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "OptimizelySDK.Utils.schema.json";

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
                return cache = reader.ReadToEnd();
        }
    }
}