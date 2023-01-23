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
using OptimizelySDK.Odp;
using System;
using System.Collections.Generic;
using System.Threading;

namespace OptimizelySDK.Tests.OdpTests
{
    public class LruCacheTest
    {
        private List<string> _segments1And2;
        private List<string> _segments3And4;
        private List<string> _segments5And6;

        [SetUp]
        public void SetUp()
        {
            _segments1And2 = new List<string>
            {
                "segment1",
                "segment2",
            };
            _segments3And4 = new List<string>
            {
                "segment3",
                "segment4",
            };
            _segments5And6 = new List<string>
            {
                "segment5",
                "segment6",
            };
        }

        [Test]
        public void ShouldCreateSaveAndLookupOneItem()
        {
            var cache = new LruCache<string>();
            Assert.IsNull(cache.Lookup("key1"));

            cache.Save("key1", "value1");
            Assert.AreEqual("value1", cache.Lookup("key1"));
        }

        [Test]
        public void ShouldSaveAndLookupMultipleItems()
        {
            var cache = new LruCache<List<string>>();

            cache.Save("user1", _segments1And2);
            cache.Save("user2", _segments3And4);
            cache.Save("user3", _segments5And6);

            var cacheKeys = cache.CurrentCacheKeysForTesting();

            Assert.AreEqual("user3", cacheKeys[0]);
            Assert.AreEqual("user2", cacheKeys[1]);
            Assert.AreEqual("user1", cacheKeys[2]);

            // Lookup should move user1 to top of the list and push down others.
            Assert.AreEqual(_segments1And2, cache.Lookup("user1"));

            cacheKeys = cache.CurrentCacheKeysForTesting();
            Assert.AreEqual("user1", cacheKeys[0]);
            Assert.AreEqual("user3", cacheKeys[1]);
            Assert.AreEqual("user2", cacheKeys[2]);

            // Lookup should move user2 to the beginning of the list and push others.
            Assert.AreEqual(_segments3And4, cache.Lookup("user2"));

            cacheKeys = cache.CurrentCacheKeysForTesting();
            Assert.AreEqual("user2", cacheKeys[0]);
            Assert.AreEqual("user1", cacheKeys[1]);
            Assert.AreEqual("user3", cacheKeys[2]);

            // Lookup moves user3 to top and pushes others down.
            Assert.AreEqual(_segments5And6, cache.Lookup("user3"));

            cacheKeys = cache.CurrentCacheKeysForTesting();
            Assert.AreEqual("user3", cacheKeys[0]);
            Assert.AreEqual("user2", cacheKeys[1]);
            Assert.AreEqual("user1", cacheKeys[2]);
        }

        [Test]
        public void ShouldReorderListOnSave()
        {
            var cache = new LruCache<List<string>>();

            cache.Save("user1", _segments1And2);
            cache.Save("user2", _segments3And4);
            cache.Save("user3", _segments5And6);

            var cacheKeys = cache.CurrentCacheKeysForTesting();
            
            // last added should be at the top of the list
            Assert.AreEqual("user3",cacheKeys[0]);
            Assert.AreEqual("user2",cacheKeys[1]);
            Assert.AreEqual("user1" ,cacheKeys[2]);

            // save should move user1 to the top of the list and push down others.
            cache.Save("user1", _segments1And2);
            
            cacheKeys = cache.CurrentCacheKeysForTesting();
            Assert.AreEqual("user1",cacheKeys[0]);
            Assert.AreEqual("user3",cacheKeys[1]);
            Assert.AreEqual("user2",cacheKeys[2]);

            // save user2 should bring it to the top and push down others.
            cache.Save("user2", _segments3And4);
            
            cacheKeys = cache.CurrentCacheKeysForTesting();
            Assert.AreEqual("user2",cacheKeys[0]);
            Assert.AreEqual("user1",cacheKeys[1]);
            Assert.AreEqual("user3",cacheKeys[2]);

            // saving user3 again should return to the original insertion order.
            cache.Save("user3", _segments5And6);
            
            cacheKeys = cache.CurrentCacheKeysForTesting();
            Assert.AreEqual("user3",cacheKeys[0]);
            Assert.AreEqual("user2",cacheKeys[1]);
            Assert.AreEqual("user1" ,cacheKeys[2]);
        }

        [Test]
        public void ShouldHandleWhenCacheIsDisabled()
        {
            var cache = new LruCache<List<string>>(maxSize: 0);

            cache.Save("user1", _segments1And2);
            cache.Save("user2", _segments3And4);
            cache.Save("user3", _segments5And6);

            Assert.IsNull(cache.Lookup("user1"));
            Assert.IsNull(cache.Lookup("user2"));
            Assert.IsNull(cache.Lookup("user3"));
        }

        [Test]
        public void ShouldHandleWhenItemsExpire()
        {
            var cache = new LruCache<List<string>>(itemTimeout: TimeSpan.FromSeconds(1));

            cache.Save("user1", _segments1And2);

            Assert.AreEqual(_segments1And2, cache.Lookup("user1"));
            Assert.AreEqual(1, cache.CurrentCacheKeysForTesting().Length);

            Thread.Sleep(1200);

            Assert.IsNull(cache.Lookup("user1"));
            Assert.AreEqual(0, cache.CurrentCacheKeysForTesting().Length);
        }

        [Test]
        public void ShouldHandleWhenCacheReachesMaxSize()
        {
            var cache = new LruCache<List<string>>(maxSize: 2);

            cache.Save("user1", _segments1And2);
            cache.Save("user2", _segments3And4);
            cache.Save("user3", _segments5And6);

            Assert.AreEqual(2, cache.CurrentCacheKeysForTesting().Length);

            Assert.AreEqual(_segments5And6, cache.Lookup("user3"));
            Assert.AreEqual(_segments3And4, cache.Lookup("user2"));
            Assert.IsNull(cache.Lookup("user1"));
        }

        [Test]
        public void ShouldHandleWhenCacheIsReset()
        {
            var cache = new LruCache<List<string>>();

            cache.Save("user1", _segments1And2);
            cache.Save("user2", _segments3And4);
            cache.Save("user3", _segments5And6);

            Assert.AreEqual(_segments1And2, cache.Lookup("user1"));
            Assert.AreEqual(_segments3And4, cache.Lookup("user2"));
            Assert.AreEqual(_segments5And6, cache.Lookup("user3"));

            Assert.AreEqual(3, cache.CurrentCacheKeysForTesting().Length);

            cache.Reset();

            Assert.IsNull(cache.Lookup("user1"));
            Assert.IsNull(cache.Lookup("user2"));
            Assert.IsNull(cache.Lookup("user3"));

            Assert.AreEqual(0, cache.CurrentCacheKeysForTesting().Length);
        }
    }
}
