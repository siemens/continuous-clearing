// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.APICommunications.Model;

namespace LCT.APICommunications.UTest
{
    public class NugetJfrogAPICommunicationUTest
    {
        [SetUp]
        public void Setup()
        {
            // Method intentionally left empty.
        }

        [Test]
        public void NugetJfrogApiCommunication_GetApiKey_ReturnsInvalidOperationException()
        {
            //Arrange
            ArtifactoryCredentials repoCredentials = new ArtifactoryCredentials();

            //Act
            JfrogApicommunication jfrogApicommunication = new NugetJfrogApiCommunication("", "", repoCredentials, 100);

            //Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () => await jfrogApicommunication.GetApiKey());
        }

        [Test]
        public void NugetJfrogApiCommunication_CopyFromRemoteRepo_ReturnsInvalidOperationException()
        {
            //Arrange
            ArtifactoryCredentials repoCredentials = new ArtifactoryCredentials();
            ComponentsToArtifactory cmpts = new ComponentsToArtifactory();

            //Act
            JfrogApicommunication jfrogApicommunication = new NugetJfrogApiCommunication("", "", repoCredentials, 100);

            //Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () => await jfrogApicommunication.CopyFromRemoteRepo(cmpts));
        }

        [Test]
        public void NugetJfrogApiCommunication_GetPackageInfo_ReturnsInvalidOperationException()
        {
            //Arrange
            ArtifactoryCredentials repoCredentials = new ArtifactoryCredentials();
            ComponentsToArtifactory cmpts = new ComponentsToArtifactory();

            //Act
            JfrogApicommunication jfrogApicommunication = new NugetJfrogApiCommunication("", "", repoCredentials, 100);

            //Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () => await jfrogApicommunication.GetPackageInfo(cmpts));
        }

        [Test]
        public void NugetJfrogApiCommunication_UpdatePackagePropertiesInJfrog_ReturnsInvalidOperationException()
        {
            //Arrange
            ArtifactoryCredentials repoCredentials = new ArtifactoryCredentials();
            UploadArgs uploadArgs = new UploadArgs();

            //Act
            JfrogApicommunication jfrogApicommunication = new NugetJfrogApiCommunication("", "", repoCredentials, 100);

            //Assert
            Assert.ThrowsAsync<InvalidOperationException>(() => { jfrogApicommunication.UpdatePackagePropertiesInJfrog("", "", uploadArgs); return Task.CompletedTask; });
        }
    }
}
