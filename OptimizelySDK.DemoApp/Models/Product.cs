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

using System.IO;

namespace OptimizelySDK.DemoApp.Models
{
    public class Product
    {
        private const string IMAGES_DIR = "~/Content/Images";

        public int Id { get; set; }
        public string Name { get; set; }
        public string Color { get; set; }
        public string Category { get; set; }
        public decimal Price { get; set; }

        private string imagePath;
        public string ImagePath
        {
            get
            {
                return imagePath;
            }
            set
            {
                imagePath = Path.Combine(IMAGES_DIR, value);
            }
        }
    }
}