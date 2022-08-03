using Moq;
using NUnit.Framework;
using OptimizelySDK.Logger;
using OptimizelySDK.Odp;

namespace OptimizelySDK.Tests.OdpTests
{
    [TestFixture]
    public class GraphQLManagerTest
    {
        private Mock<ILogger> MockLogger;
        
        [SetUp]
        public void Setup()
        {
            MockLogger = new Mock<ILogger>();
            MockLogger.Setup(i => i.Log(It.IsAny<LogLevel>(), It.IsAny<string>()));
        }

        [Test]
        public void ShouldParseSuccessfulResponse()
        {
            #region const string responseJson
            const string responseJson = @"
{
    ""data"": {
        ""customer"": {
            ""audiences"": {
                ""edges"": [
                {
                    ""node"": {
                        ""name"": ""has_email"",
                        ""state"": ""qualified"",
                    }
                },
                {
                    ""node"": {
                        ""name"": ""has_email_opted_in"",
                        ""state"": ""qualified"",
                    }
                },
              ]
            }
        }
    }
}";
            #endregion
            var manager = new GraphQLManager(MockLogger.Object);

            var response = manager.ParseResponse(responseJson);
            
            Assert.IsNull(response.Errors); 
            Assert.IsNotNull(response.Data);
        }
        
        [Test]
        public void ShouldParseErrorResponse()
        {
            #region const string responseJson
            const string responseJson = @"
{
   ""errors"": [
        {
            ""message"": ""Exception while fetching data (/customer) : java.lang.RuntimeException: could not resolve _fs_user_id = asdsdaddddd"",
            ""locations"": [
                {
                    ""line"": 2,
                    ""column"": 3
                }
            ],
            ""path"": [
            ""customer""
                ],
            ""extensions"": {
                ""classification"": ""InvalidIdentifierException""
            }
        }
    ],
    ""data"": {
        ""customer"": null
    }
}";
            #endregion
            var manager = new GraphQLManager(MockLogger.Object);

            var response = manager.ParseResponse(responseJson);
            
            Assert.IsNull(response.Data.Customer);
            Assert.IsNotNull(response.Errors);
        }
    }
}