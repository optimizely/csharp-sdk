/* 
 * Copyright 2018, Optimizely
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

using NUnit.Framework;
using OptimizelySDK.Utils;
using System;

namespace OptimizelySDK.Tests.UtilsTests
{
    public class ExtensionMethodsTest
    {
        [Test]
        public void TestGetAllMessagesReturnsAllInnerExceptionMessages()
        {
            var exception = new Exception("Outer exception.", new Exception("Inner exception.", new Exception("Second level inner exception.")));
            var expectedMessage = "Outer exception. Inner exception. Second level inner exception.";
            
            Assert.AreEqual(expectedMessage, exception.GetAllMessages());
        }
    }
}
