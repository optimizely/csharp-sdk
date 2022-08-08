﻿/* 
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
using System.Collections.Generic;
using System.Threading;

namespace OptimizelySDK.Tests.OdpTests
{
    [TestFixture]
    public class LruCacheTest
    {
        private readonly List<string> _segments1And2 = new List<string>
        {
            "segment1",
            "segment2",
        };

        private readonly List<string> _segments3And4 = new List<string>
        {
            "segment3",
            "segment4",
        };

        private readonly List<string> _segments5And6 = new List<string>
        {
            "segment5",
            "segment6",
        };

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

            var itemKeys = cache.ReadCurrentCache().Keys.ToKeyCollection();
            Assert.AreEqual("user1", itemKeys[0]);
            Assert.AreEqual("user2", itemKeys[1]);
            Assert.AreEqual("user3", itemKeys[2]);

            Assert.AreEqual(_segments1And2, cache.Lookup("user1"));

            itemKeys = cache.ReadCurrentCache().Keys.ToKeyCollection();
            // Lookup should move user1 to bottom of the list and push up others.
            Assert.AreEqual("user2", itemKeys[0]);
            Assert.AreEqual("user3", itemKeys[1]);
            Assert.AreEqual("user1", itemKeys[2]);

            Assert.AreEqual(_segments3And4, cache.Lookup("user2"));

            itemKeys = cache.ReadCurrentCache().Keys.ToKeyCollection();
            // Lookup should move user2 to bottom of the list and push up others.
            Assert.AreEqual("user3", itemKeys[0]);
            Assert.AreEqual("user1", itemKeys[1]);
            Assert.AreEqual("user2", itemKeys[2]);

            Assert.AreEqual(_segments5And6, cache.Lookup("user3"));

            itemKeys = cache.ReadCurrentCache().Keys.ToKeyCollection();
            // Lookup should move user3 to bottom of the list and push up others.
            Assert.AreEqual("user1", itemKeys[0]);
            Assert.AreEqual("user2", itemKeys[1]);
            Assert.AreEqual("user3", itemKeys[2]);
        }

        [Test]
        public void ShouldReorderListOnSave()
        {
            var cache = new LruCache<List<string>>();
            
            cache.Save("user1", _segments1And2);
            cache.Save("user2", _segments3And4);
            cache.Save("user3", _segments5And6);

            var itemKeys = cache.ReadCurrentCache().Keys.ToKeyCollection();
            Assert.AreEqual("user1", itemKeys[0]);
            Assert.AreEqual("user2", itemKeys[1]);
            Assert.AreEqual("user3", itemKeys[2]);

            cache.Save("user1", _segments1And2);

            itemKeys = cache.ReadCurrentCache().Keys.ToKeyCollection();
            // save should move user1 to bottom of the list and push up others.
            Assert.AreEqual("user2", itemKeys[0]);
            Assert.AreEqual("user3", itemKeys[1]);
            Assert.AreEqual("user1", itemKeys[2]);

            cache.Save("user2", _segments3And4);

            itemKeys = cache.ReadCurrentCache().Keys.ToKeyCollection();
            // save should move user2 to bottom of the list and push up others.
            Assert.AreEqual("user3", itemKeys[0]);
            Assert.AreEqual("user1", itemKeys[1]);
            Assert.AreEqual("user2", itemKeys[2]);

            cache.Save("user3", _segments5And6);

            itemKeys = cache.ReadCurrentCache().Keys.ToKeyCollection();
            // save should move user3 to bottom of the list and push up others.
            Assert.AreEqual("user1", itemKeys[0]);
            Assert.AreEqual("user2", itemKeys[1]);
            Assert.AreEqual("user3", itemKeys[2]);
        }

        [Test]
        public void ShouldHandleWhenCacheIsDisabled()
        {
            var cache = new LruCache<List<string>>(0, LruCache<object>.DEFAULT_TIMEOUT_SECONDS);

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
            var cache = new LruCache<List<string>>(LruCache<object>.DEFAULT_MAX_SIZE, 1);

            cache.Save("user1", _segments1And2);

            Assert.AreEqual(_segments1And2, cache.Lookup("user1"));
            Assert.AreEqual(1, cache.ReadCurrentCache().Count);

            Thread.Sleep(1200);

            Assert.IsNull(cache.Lookup("user1"));
            Assert.AreEqual(0, cache.ReadCurrentCache().Count);
        }

        [Test]
        public void ShouldHandleWhenCacheReachesMaxSize()
        {
            var cache = new LruCache<List<string>>(2, LruCache<object>.DEFAULT_TIMEOUT_SECONDS);

            cache.Save("user1", _segments1And2);
            cache.Save("user2", _segments3And4);
            cache.Save("user3", _segments5And6);

            Assert.AreEqual(2, cache.ReadCurrentCache().Count);

            Assert.AreEqual(_segments5And6, cache.Lookup("user3"));
            Assert.AreEqual(_segments3And4, cache.Lookup("user2"));
            Assert.IsNull(cache.Lookup("user1"));
        }

        [Test]
        public void ShouldHandleWhenCacheIsReset() { 
            var cache = new LruCache<List<string>>();
            
            cache.Save("user1", _segments1And2);
            cache.Save("user2", _segments3And4);
            cache.Save("user3", _segments5And6);

            Assert.AreEqual(_segments1And2, cache.Lookup("user1"));
            Assert.AreEqual(_segments3And4, cache.Lookup("user2"));
            Assert.AreEqual(_segments5And6, cache.Lookup("user3"));

            Assert.AreEqual(3, cache.ReadCurrentCache().Count);

            cache.Reset();

            Assert.IsNull(cache.Lookup("user1"));
            Assert.IsNull(cache.Lookup("user2"));
            Assert.IsNull(cache.Lookup("user3"));

            Assert.AreEqual(0, cache.ReadCurrentCache().Count);}
    }
}