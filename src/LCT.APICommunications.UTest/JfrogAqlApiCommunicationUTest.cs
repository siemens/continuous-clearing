using LCT.APICommunications.Model;
using Moq;
using Moq.Protected;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace LCT.APICommunications.UTest
{
    [TestFixture]
    public class JfrogAqlApiCommunicationUTest
    {
        private Mock<HttpMessageHandler> _mockHandler;
        private HttpClient _httpClient;
        private JfrogAqlApiCommunication _jfrogApiCommunication;
        private ArtifactoryCredentials _credentials;
        private string _repoDomainName;
        private int _timeout;
        [SetUp]
        public void SetUp()
        {
            // Setup test data
            _repoDomainName = "https://example.jfrog.io";
            _credentials = new ArtifactoryCredentials { Token = "sample-token" };
            _timeout = 30;

            // Mock HttpMessageHandler to simulate HTTP responses
            _mockHandler = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_mockHandler.Object);

            // Create instance of JfrogAqlApiCommunication
            _jfrogApiCommunication = new JfrogAqlApiCommunication(_repoDomainName, _credentials, _timeout);

        }
        [TearDown]
        public void TearDown()
        {
            // Cleanup mock interactions and reset state
            _mockHandler.Invocations.Clear(); // Clear mock invocations after each test
                                              // Add any other cleanup logic for shared resources or state between tests            
        }
        [Test]
        public async Task CheckConnection_ReturnsOkResponse()
        {
            // Arrange
            var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"apiKey\": \"sample-api-key\"}")
            };

            _mockHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get && req.RequestUri.ToString() == $"{_repoDomainName}/api/security/apiKey"),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(expectedResponse)
                .Verifiable();

            // Act
            var result = await _jfrogApiCommunication.CheckConnection();

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);            
        
        }
        [Test]
        public async Task GetInternalComponentDataByRepo_ReturnsOkResponse()
        {
            // Arrange
            string repoName = "my-repo";
            var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"repo\": \"my-repo\", \"path\": \"some/path\", \"name\": \"component-name\"}")
            };

            _mockHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Post && req.RequestUri.ToString() == $"{_repoDomainName}{ApiConstant.JfrogArtifactoryApiSearchAql}" &&
                        req.Content.ReadAsStringAsync().Result.Contains($"\"repo\":\"{repoName}\"")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(expectedResponse)
                .Verifiable();

            // Act
            var result = await _jfrogApiCommunication.GetInternalComponentDataByRepo(repoName);

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        }
        [Test]
        public async Task GetPackageInfo_ReturnsOkResponse()
        {
            // Arrange
            var component = new ComponentsToArtifactory
            {
                JfrogPackageName = "package-name",
                SrcRepoName = "my-repo",
                Path = "some/path",
                ComponentType = "Npm",
                Version = "1.0.0", // Assuming the version is also part of the component
                Name = "package-name" // Assuming "Name" is also a property on the component
            };

            var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"repo\": \"my-repo\", \"path\": \"some/path\", \"name\": \"package-name\"}")
            };

            // Build the expected AQL query string based on the component
            string expectedAqlQuery = "items.find({\"repo\":{\"$eq\":\"my-repo\"},\"@npm.name\":{\"$eq\":\"package-name\"},\"@npm.version\":{\"$eq\":\"1.0.0\"}}).include(\"repo\", \"path\", \"name\")";

            // Set up the mock handler to intercept the HTTP request
            _mockHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Post &&
                        req.RequestUri.ToString() == $"{_repoDomainName}{ApiConstant.JfrogArtifactoryApiSearchAql}" &&
                        req.Content.ReadAsStringAsync().Result.Contains(expectedAqlQuery)), // Validate that the expected query is included
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(expectedResponse)
                .Verifiable();

            // Act
            var result = await _jfrogApiCommunication.GetPackageInfo(component);

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);  // Assert the status code is OK (200)

        }
        [Test]
        public async Task GetPackageInfo_ReturnsOkResponseforPython()
        {
            // Arrange
            var component = new ComponentsToArtifactory
            {
                JfrogPackageName = "package-name",
                SrcRepoName = "my-repo",
                Path = "some/path",
                ComponentType = "Python",
                Version = "1.0.0", // Assuming the version is also part of the component
                Name = "package-name" // Assuming "Name" is also a property on the component
            };

            var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"repo\": \"my-repo\", \"path\": \"some/path\", \"name\": \"package-name\"}")
            };

            // Build the expected AQL query string based on the component
            string expectedAqlQuery = "items.find({\"repo\":{\"$eq\":\"my-repo\"},\"@pypi.normalized.name\":{\"$eq\":\"package-name\"},\"@pypi.version\":{\"$eq\":\"1.0.0\"}}).include(\"repo\", \"path\", \"name\")";

            // Set up the mock handler to intercept the HTTP request
            _mockHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Post &&
                        req.RequestUri.ToString() == $"{_repoDomainName}{ApiConstant.JfrogArtifactoryApiSearchAql}" &&
                        req.Content.ReadAsStringAsync().Result.Contains(expectedAqlQuery)), // Validate that the expected query is included
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(expectedResponse)
                .Verifiable();

            // Act
            var result = await _jfrogApiCommunication.GetPackageInfo(component);

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);  // Assert the status code is OK (200)

        }

        [Test]
        public async Task GetPackageInfo_ReturnsOkResponseforNuget()
        {
            // Arrange
            var component = new ComponentsToArtifactory
            {
                JfrogPackageName = "package-name",
                SrcRepoName = "my-repo",
                Path = "some/path",
                ComponentType = "nuget",
                Version = "1.0.0", // Assuming the version is also part of the component
                Name = "package-name" // Assuming "Name" is also a property on the component
            };

            var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"repo\": \"my-repo\", \"path\": \"some/path\", \"name\": \"package-name\"}")
            };

            // Build the expected AQL query string based on the component
            string expectedAqlQuery = "items.find({\"repo\":{\"$eq\":\"my-repo\"}}).include(\"repo\", \"path\", \"name\")";

            // Set up the mock handler to intercept the HTTP request
            _mockHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Post &&
                        req.RequestUri.ToString() == $"{_repoDomainName}{ApiConstant.JfrogArtifactoryApiSearchAql}" &&
                        req.Content.ReadAsStringAsync().Result.Contains(expectedAqlQuery)), // Validate that the expected query is included
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(expectedResponse)
                .Verifiable();

            // Act
            var result = await _jfrogApiCommunication.GetPackageInfo(component);

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);  // Assert the status code is OK (200)

        }
        [Test]
        public async Task GetNpmComponentDataByRepo_ReturnsSuccessfulResponse()
        {
            // Arrange
            var repoName = "my-npm-repo";
            var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"repo\": \"my-npm-repo\", \"path\": \"some/path\", \"name\": \"package-name\", \"@npm.name\": \"package-name\", \"@npm.version\": \"1.0.0\"}")
            };

            _mockHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Post && req.RequestUri.ToString() == $"{_repoDomainName}{ApiConstant.JfrogArtifactoryApiSearchAql}" &&
                        req.Content.ReadAsStringAsync().Result.Contains("\"repo\":\"my-npm-repo\"")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(expectedResponse)
                .Verifiable();

            // Act
            var result = await _jfrogApiCommunication.GetNpmComponentDataByRepo(repoName);

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            
        }

        [Test]
        public async Task GetPypiComponentDataByRepo_ReturnsSuccessfulResponse()
        {
            // Arrange
            var repoName = "my-pypi-repo";
            var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"repo\": \"my-pypi-repo\", \"path\": \"some/path\", \"name\": \"package-name\", \"@pypi.normalized.name\": \"package-name\", \"@pypi.version\": \"1.0.0\"}")
            };

            _mockHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Post && req.RequestUri.ToString() == $"{_repoDomainName}{ApiConstant.JfrogArtifactoryApiSearchAql}" &&
                        req.Content.ReadAsStringAsync().Result.Contains("\"repo\":\"my-pypi-repo\"")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(expectedResponse)
                .Verifiable();

            // Act
            var result = await _jfrogApiCommunication.GetPypiComponentDataByRepo(repoName);

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            
        }

    }
}
