/* 
 * Copyright 2022, Optimizely
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * https://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using NUnit.Framework;
using OptimizelySDK.Entity;

namespace OptimizelySDK.Tests.EntityTests
{
    [TestFixture]
    public class IntegrationTest
    {
        private const string KEY = "test-key";

        private const string HOST = "api.example.com";
        private const string PUBLIC_KEY = "FAk3-pUblic-K3y";

        [Test]
        public void ToStringWithNoHostShouldSucceed()
        {
            var integration = new Integration()
            {
                Key = KEY,
                PublicKey = PUBLIC_KEY,
            };

            var stringValue = integration.ToString();

            Assert.True(stringValue.Contains($@"key='{KEY}'"));
            Assert.True(stringValue.Contains($@"publicKey='{PUBLIC_KEY}'"));
            Assert.False(stringValue.Contains("host"));
            Assert.False(stringValue.Contains(HOST));
        }

        [Test]
        public void ToStringWithNoPublicKeyShouldSucceed()
        {
            var integration = new Integration()
            {
                Key = KEY,
                Host = HOST,
            };

            var stringValue = integration.ToString();

            Assert.True(stringValue.Contains($@"key='{KEY}'"));
            Assert.True(stringValue.Contains($@"host='{HOST}'"));
            Assert.False(stringValue.Contains("publicKey"));
            Assert.False(stringValue.Contains(PUBLIC_KEY));
        }

        [Test]
        public void ToStringWithAllPropertiesShouldSucceed()
        {
            var integration = new Integration()
            {
                Key = KEY,
                Host = HOST,
                PublicKey = PUBLIC_KEY,
            };

            var stringValue = integration.ToString();

            Assert.True(
                stringValue.Contains($@"key='{KEY}', host='{HOST}', publicKey='{PUBLIC_KEY}'"));
        }

        [Test]
        public void ToStringWithOnlyKeyShouldSucceed()
        {
            var integration = new Integration()
            {
                Key = KEY,
            };

            var stringValue = integration.ToString();

            Assert.True(stringValue.Contains($@"key='{KEY}'"));
            Assert.False(stringValue.Contains("host"));
            Assert.False(stringValue.Contains(HOST));
            Assert.False(stringValue.Contains("publicKey"));
            Assert.False(stringValue.Contains(PUBLIC_KEY));
        }
    }
}
