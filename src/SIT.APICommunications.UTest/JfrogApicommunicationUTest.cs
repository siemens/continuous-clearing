// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2026 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using SIT.APICommunications.Interfaces;
using SIT.APICommunications.Model;
using System.Collections.Generic;
using System.Net;
namespace SIT.APICommunications.UTest
{
    [TestFixture]
    public class JfrogApicommunicationUTest
    {
        [SetUp]
        public void Setup()
        {
            // Method intentionally left empty.
        }

        [Test]
        public void JfrogApicommunication_CopyPackageFromRemoteRepo_ReturnsInvalidOperationException()
        {
            //Arrange
            ArtifactoryCredentials repoCredentials = new ArtifactoryCredentials();

            //Act
            IJFrogApiCommunication jfrogApicommunication = new NugetJfrogApiCommunication("", "", repoCredentials, 100);

            //Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () => await jfrogApicommunication.DeletePackageFromJFrogRepo("", ""));
        }

        [Test]
        public async Task SetPackagePropertiesInJfrog_WithNoProperties_ReturnsNoContent()
        {
            //Arrange
            ArtifactoryCredentials repoCredentials = new ArtifactoryCredentials();
            IJFrogApiCommunication jfrogApicommunication = new NpmJfrogApiCommunication("", "", repoCredentials, 100);
            var component = new ComponentsToArtifactory();

            //Act
            var response = await jfrogApicommunication.SetPackagePropertiesInJfrog(component, new Dictionary<string, string>());

            //Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        }

        [Test]
        public async Task SetPackagePropertiesInJfrog_WithNullProperties_ReturnsNoContent()
        {
            //Arrange
            ArtifactoryCredentials repoCredentials = new ArtifactoryCredentials();
            IJFrogApiCommunication jfrogApicommunication = new NpmJfrogApiCommunication("", "", repoCredentials, 100);
            var component = new ComponentsToArtifactory();

            //Act
            var response = await jfrogApicommunication.SetPackagePropertiesInJfrog(component, null);

            //Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        }

        [Test]
        public async Task SetPackagePropertiesInJfrog_WithUnresolvableDestination_ReturnsBadRequest()
        {
            //Arrange
            ArtifactoryCredentials repoCredentials = new ArtifactoryCredentials();
            IJFrogApiCommunication jfrogApicommunication = new NpmJfrogApiCommunication("", "", repoCredentials, 100);
            var component = new ComponentsToArtifactory
            {
                PackageType = PackageType.ClearedThirdParty,
                CopyPackageApiUrl = string.Empty
            };
            var properties = new Dictionary<string, string> { { "sw360projectname", "MyProject" } };

            //Act
            var response = await jfrogApicommunication.SetPackagePropertiesInJfrog(component, properties);

            //Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test]
        public async Task SetPackagePropertiesInJfrog_WithNullComponent_ReturnsNoContent()
        {
            //Arrange
            ArtifactoryCredentials repoCredentials = new ArtifactoryCredentials();
            IJFrogApiCommunication jfrogApicommunication = new NpmJfrogApiCommunication("", "", repoCredentials, 100);
            var properties = new Dictionary<string, string> { { "uploadedby", "SIT.Upload" } };

            //Act
            var response = await jfrogApicommunication.SetPackagePropertiesInJfrog(null, properties);

            //Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        }

        [Test]
        public async Task SetPackagePropertiesInJfrog_WithInternalPackageAndEmptyMoveUrl_ReturnsBadRequest()
        {
            //Arrange
            ArtifactoryCredentials repoCredentials = new ArtifactoryCredentials();
            IJFrogApiCommunication jfrogApicommunication = new NpmJfrogApiCommunication("", "", repoCredentials, 100);
            var component = new ComponentsToArtifactory
            {
                PackageType = PackageType.Internal,
                MovePackageApiUrl = string.Empty
            };
            var properties = new Dictionary<string, string> { { "uploadedby", "SIT.Upload" } };

            //Act
            var response = await jfrogApicommunication.SetPackagePropertiesInJfrog(component, properties);

            //Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test]
        public async Task SetPackagePropertiesInJfrog_WithUrlMissingDestinationMarker_ReturnsBadRequest()
        {
            //Arrange
            ArtifactoryCredentials repoCredentials = new ArtifactoryCredentials();
            IJFrogApiCommunication jfrogApicommunication = new NpmJfrogApiCommunication("", "", repoCredentials, 100);
            var component = new ComponentsToArtifactory
            {
                PackageType = PackageType.ClearedThirdParty,
                CopyPackageApiUrl = "https://jfrog.example.com/api/copy/source-repo/pkg"
            };
            var properties = new Dictionary<string, string> { { "uploadedby", "SIT.Upload" } };

            //Act
            var response = await jfrogApicommunication.SetPackagePropertiesInJfrog(component, properties);

            //Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test]
        public async Task SetPackagePropertiesInJfrog_WithOnlyEmptyPropertyValues_ReturnsNoContent()
        {
            //Arrange
            ArtifactoryCredentials repoCredentials = new ArtifactoryCredentials();
            IJFrogApiCommunication jfrogApicommunication = new NpmJfrogApiCommunication("", "", repoCredentials, 100);
            var component = new ComponentsToArtifactory
            {
                PackageType = PackageType.ClearedThirdParty,
                CopyPackageApiUrl = "https://jfrog.example.com/api/copy/source-repo/pkg?to=/dest-repo/pkg"
            };
            // Keys/values are empty so they are filtered out, leaving nothing to encode.
            var properties = new Dictionary<string, string> { { "", "" }, { "uploadedby", "" } };

            //Act
            var response = await jfrogApicommunication.SetPackagePropertiesInJfrog(component, properties);

            //Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        }

        [Test]
        public async Task SetPackagePropertiesInJfrog_WithValidDestinationAndProperties_SendsPutAndReturnsResponse()
        {
            //Arrange - host a loopback listener so the real PutAsync executes without touching production code
            using var listener = new HttpListener();
            int port = GetFreeTcpPort();
            string prefix = $"http://localhost:{port}/";
            listener.Prefixes.Add(prefix);
            listener.Start();

            string requestedRawUrl = null;
            string requestedMethod = null;
            var serverTask = Task.Run(async () =>
            {
                var context = await listener.GetContextAsync();
                requestedRawUrl = context.Request.Url.PathAndQuery;
                requestedMethod = context.Request.HttpMethod;
                context.Response.StatusCode = (int)HttpStatusCode.OK;
                context.Response.Close();
            });

            var repoCredentials = new ArtifactoryCredentials { Token = "dummy-token" };
            // DomainName is set from the constructor's first argument (trim trailing slash to match url building).
            IJFrogApiCommunication jfrogApicommunication =
                new NpmJfrogApiCommunication($"http://localhost:{port}", "", repoCredentials, 100);
            var component = new ComponentsToArtifactory
            {
                PackageType = PackageType.ClearedThirdParty,
                CopyPackageApiUrl = "https://jfrog.example.com/api/copy/source-repo/pkg?to=/dest-repo/pkg"
            };
            var properties = new Dictionary<string, string> { { "uploadedby", "SIT.Upload" } };

            //Act
            var response = await jfrogApicommunication.SetPackagePropertiesInJfrog(component, properties);
            await serverTask;
            listener.Stop();

            //Assert
            Assert.That(response, Is.Not.Null);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(requestedMethod, Is.EqualTo("PUT"));
            Assert.That(requestedRawUrl, Does.StartWith("/api/storage/dest-repo/pkg"));
            Assert.That(requestedRawUrl, Does.Contain("properties=uploadedby=SIT.Upload"));
            Assert.That(requestedRawUrl, Does.Contain("recursive=1"));
        }

        private static int GetFreeTcpPort()
        {
            var tcpListener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
            tcpListener.Start();
            int port = ((System.Net.IPEndPoint)tcpListener.LocalEndpoint).Port;
            tcpListener.Stop();
            return port;
        }
    }
}
