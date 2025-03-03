// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 


using LCT.APICommunications.Model;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http;
using System.Threading;

namespace LCT.APICommunications.UTest
{
    [TestFixture]
    public class DebainJfrogAPICommunicationUTest
    {
        private Mock<HttpMessageHandler> _mockHandler;
        private HttpClient _httpClient;
        private DebianJfrogAPICommunication _debianJfrogAPICommunication;
        private ArtifactoryCredentials _credentials;
        private string _repoDomainName;
        private string _srcrepoName;
        private int _timeout;
        [SetUp]
        public void Setup()
        {
            // Setup the necessary parameters for creating an instance of DebianJfrogAPICommunication
            _repoDomainName = "https://example.jfrog.io";
            _srcrepoName = "my-repo";
            _credentials = new ArtifactoryCredentials { Token = "sample-token" };
            _timeout = 30;

            // Mock the HttpMessageHandler to mock the HTTP calls
            _mockHandler = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_mockHandler.Object);

            // Initialize the DebianJfrogAPICommunication object
            _debianJfrogAPICommunication = new DebianJfrogAPICommunication(_repoDomainName, _srcrepoName, _credentials, _timeout);
        }

        [Test]
        public void DebainJfrogApiCommunication_CopyFromRemoteRepo_ReturnsInvalidOperationException()
        {
            //Arrange
            ArtifactoryCredentials repoCredentials = new ArtifactoryCredentials();

            //Act
            JfrogApicommunication jfrogApicommunication = new DebianJfrogAPICommunication("", "", repoCredentials,100);

            //Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () => await jfrogApicommunication.CopyFromRemoteRepo(new ComponentsToArtifactory()));
        }

        [Test]
        public void DebainJfrogApiCommunication_UpdatePackagePropertiesInJfrog_ReturnsInvalidOperationException()
        {
            //Arrange
            ArtifactoryCredentials repoCredentials = new ArtifactoryCredentials();

            //Act
            JfrogApicommunication jfrogApicommunication = new DebianJfrogAPICommunication("", "", repoCredentials, 100);

            //Assert
            Assert.ThrowsAsync<InvalidOperationException>(() => { jfrogApicommunication.UpdatePackagePropertiesInJfrog("", "", new UploadArgs()); return Task.CompletedTask; });
        }

        [Test]
        public void DebainJfrogApiCommunication_GetPackageInfo_ReturnsInvalidOperationException()
        {
            //Arrange
            ArtifactoryCredentials repoCredentials = new ArtifactoryCredentials();

            //Act
            JfrogApicommunication jfrogApicommunication = new DebianJfrogAPICommunication("", "", repoCredentials, 100);

            //Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () => await jfrogApicommunication.GetPackageInfo(new ComponentsToArtifactory()));
        }
        [Test]
        public async Task GetApiKey_ReturnsSuccessfulResponse()
        {
            // Arrange
            var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK);

            // Mock the SendAsync method to return the expected response
            _mockHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Get && req.RequestUri.ToString() == $"{_repoDomainName}/api/security/apiKey"),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(expectedResponse)
                .Verifiable();

            // Act
            var result = await _debianJfrogAPICommunication.GetApiKey();

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        }
        [Test]
        public async Task MoveFromRepo_ReturnsSuccessfulResponse()
        {
            // Arrange
            var component = new ComponentsToArtifactory
            {
                MovePackageApiUrl = $"{_repoDomainName}/api/move/package"  // Example URL
            };

            var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("Move successful")
            };

            // Mock the SendAsync method to return the expected response
            _mockHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Post && req.RequestUri.ToString() == component.MovePackageApiUrl),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(expectedResponse)
                .Verifiable();

            // Act
            var result = await _debianJfrogAPICommunication.MoveFromRepo(component);

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        }
    }
    public static class HttpMessageHandlerExtensions
    {
        public static Mock<HttpMessageHandler> SetupRequest(this Mock<HttpMessageHandler> mockHandler, HttpMethod method, string requestUri)
        {
            var request = new HttpRequestMessage(method, requestUri);

            mockHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(req => req.Method == method && req.RequestUri.ToString() == requestUri), ItExpr.IsAny<System.Threading.CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage())
                .Verifiable();

            return mockHandler;
        }
    }
}
