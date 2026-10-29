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
    }
}
