using System;
using NUnit.Framework;
using OptimizelySDK.Entity;

namespace OptimizelySDK.Tests.EntityTests
{
    [TestFixture]
    public class FeatureVariableTest
    {
        [Test]
        public void TestFeatureVariableTypeName()
        {
            Assert.AreEqual(FeatureVariable.GetFeatureVariableTypeName(FeatureVariable.VariableType.BOOLEAN), "GetFeatureVariableBoolean");
            Assert.AreEqual(FeatureVariable.GetFeatureVariableTypeName(FeatureVariable.VariableType.DOUBLE), "GetFeatureVariableDouble");
            Assert.AreEqual(FeatureVariable.GetFeatureVariableTypeName(FeatureVariable.VariableType.INTEGER), "GetFeatureVariableInteger");
            Assert.AreEqual(FeatureVariable.GetFeatureVariableTypeName(FeatureVariable.VariableType.STRING), "GetFeatureVariableString");
        }
    }
}
