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

            var responseJson = @"
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

            var response = manager.ParseJson(responseJson);

            Assert.IsNull(response.Errors);
            Assert.IsNotNull(response.Data);
            Assert.IsNotNull(response.Data.Customer);
            Assert.IsNotNull(response.Data.Customer.Audiences);
            Assert.IsNotNull(response.Data.Customer.Audiences.Edges);
            Assert.IsTrue(response.Data.Customer.Audiences.Edges.Length == 2);
            var node = response.Data.Customer.Audiences.Edges[0].Node;
            Assert.IsTrue(node.Name == "has_email");
            Assert.IsTrue(node.State == "qualified");
            node = response.Data.Customer.Audiences.Edges[1].Node;
            Assert.IsTrue(node.Name == "has_email_opted_in");
            Assert.IsTrue(node.State == "qualified");
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

            var response = manager.ParseJson(responseJson);

            Assert.IsNull(response.Data.Customer);
            Assert.IsNotNull(response.Errors);
            Assert.IsTrue(response.Errors.Length == 1);
            var error = response.Errors[0];
            Assert.IsTrue(error.Message.Contains("asdsdaddddd"));
            Assert.IsTrue(error.Locations.Length == 1);
            var location = error.Locations[0];
            Assert.IsTrue(location.Line == 2);
            Assert.IsTrue(location.Column == 3);
            Assert.IsTrue(error.Path.Length == 1);
            Assert.IsTrue(error.Path[0] == "customer");
            Assert.IsTrue(error.Extensions.Classification=="InvalidIdentifierException");
        }

        [Test]
        public void ShouldFetchValidQualifiedSegments()
        {
            
        }

        [Test]
        public void ShouldHandleEmptyQualifiedSegments()
        {
            
        }

        [Test]
        public void ShouldHandleErrorWithInvalidIdentifier()
        {
            
        }

        [Test]
        public void ShouldHandleOtherException()
        {
            
        }
        
        [Test]
        public void ShouldHandleBadResponse()
        {
            
        }
        
        [Test]
        public void ShouldHandleNetworkError()
        {
            
        }
        
        [Test]
        public void ShouldHandle400HttpCode()
        {
            
        }
        
        [Test]
        public void ShouldHandle500HttpCode()
        {
            
        }
    }
}