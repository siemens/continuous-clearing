// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 


using LCT.APICommunications.Model;
using LCT.Common.Interface;
using Moq;
using System.Net;
using System.Reflection;

namespace LCT.APICommunications.UTest
{

    [TestFixture]
    public class NPMJfrogAPICommunicationUTest
    {
        [SetUp]
        public void Setup()
        {
            // Method intentionally left empty.
        }

        [Test]
        public void NpmJfrogApiCommunication_CopyFromRemoteRepo_ReturnsInvalidOperationException()
        {
            //Arrange
            ArtifactoryCredentials repoCredentials = new ArtifactoryCredentials();

            //Act
            JfrogApicommunication jfrogApicommunication = new NpmJfrogApiCommunication("", "", repoCredentials, 100);

            //Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () => await jfrogApicommunication.CopyFromRemoteRepo(new ComponentsToArtifactory()));
        }
        [Test]
        public void NpmJfrogApiCommunication_MoveFromRepo_ReturnsInvalidOperationException()
        {
            //Arrange
            ArtifactoryCredentials repoCredentials = new ArtifactoryCredentials();

            //Act
            JfrogApicommunication jfrogApicommunication = new NpmJfrogApiCommunication("", "", repoCredentials, 100);

            //Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () => await jfrogApicommunication.MoveFromRepo(new ComponentsToArtifactory()));
        }
        [Test]
        public void NpmJfrogApiCommunication_GetApiKey_ReturnsInvalidOperationException()
        {
            //Arrange
            ArtifactoryCredentials repoCredentials = new ArtifactoryCredentials();

            //Act
            JfrogApicommunication jfrogApicommunication = new NpmJfrogApiCommunication("", "", repoCredentials, 100);

            //Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () => await jfrogApicommunication.GetApiKey());
        }
        [Test]
        public void NpmJfrogApiCommunication_UpdatePackagePropertiesInJfrog_ReturnsInvalidOperationException()
        {
            //Arrange
            ArtifactoryCredentials repoCredentials = new ArtifactoryCredentials();

            //Act
            JfrogApicommunication jfrogApicommunication = new NpmJfrogApiCommunication("", "", repoCredentials, 100);

            //Assert
            Assert.ThrowsAsync<InvalidOperationException>(() => { jfrogApicommunication.UpdatePackagePropertiesInJfrog("", "", new UploadArgs()); return Task.CompletedTask; });
        }

        [Test]
        public void NpmJfrogApiCommunication_GetPackageInfo_ReturnsInvalidOperationException()
        {
            //Arrange
            ArtifactoryCredentials repoCredentials = new ArtifactoryCredentials();

            //Act
            JfrogApicommunication jfrogApicommunication = new NpmJfrogApiCommunication("", "", repoCredentials, 100);

            //Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () => await jfrogApicommunication.GetPackageInfo(new ComponentsToArtifactory()));
        }
        [Test]
        public async Task NpmJfrogApiCommunication_GetPackageInfo_EnsuresSuccessStatusCode()
        {
            // Arrange
            var mockHttpMessageHandler = new MockHttpMessageHandler();
            mockHttpMessageHandler.SetResponse(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK
            });

            var mockHttpClient = new HttpClient(mockHttpMessageHandler);
            var repoCredentials = new ArtifactoryCredentials { Token = "dummyToken" };
            var component = new ComponentsToArtifactory { PackageInfoApiUrl = "http://dummyurl.com" };

            var jfrogApiCommunication = new NpmJfrogApiCommunication("", "", repoCredentials, 100);
            typeof(NpmJfrogApiCommunication)
                .GetField("environmentHelper", BindingFlags.NonPublic | BindingFlags.Static)
                ?.SetValue(null, new Mock<IEnvironmentHelper>().Object);

            // Act
            var response = await jfrogApiCommunication.GetPackageInfo(component);

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        // Custom HttpMessageHandler to mock SendAsync
        public class MockHttpMessageHandler : HttpMessageHandler
        {
            private HttpResponseMessage _response;

            public void SetResponse(HttpResponseMessage response)
            {
                _response = response;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(_response);
            }
        }
    }
}
