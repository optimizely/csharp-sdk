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