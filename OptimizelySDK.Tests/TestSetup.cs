using NUnit.Framework;
using System.Globalization;
using System.Threading;

namespace OptimizelySDK.Tests
{
    [SetUpFixture]
    public class TestSetup
    {
        [SetUp]
        public void Init()
        {
            /* There are some issues doing assertions on tests with floating point numbers using the .ToString()
             * method, as it's culture dependent. EG: TestGetFeatureVariableValueForTypeGivenFeatureFlagIsNotEnabledForUser,
             * assigning the culture to English will make this kind of tests to work on others culture based systems. */
            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("en-US");
        }

        [TearDown]
        public void Cleanup()
        {
            // Empty, but required: https://nunit.org/nunitv2/docs/2.6.4/setupFixture.html
        }
    }
}
