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
