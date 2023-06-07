// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 


using LCT.APICommunications.Model;

namespace LCT.APICommunications.UTest
{
    public class NPMJfrogAPICommunicationUTest
    {
        [SetUp]
        public void Setup()
        {
            // Method intentionally left empty.
        }

        [Test]
        public void NpmJfrogApiCommunication_CopyPackageFromRemoteRepo_ReturnsInvalidOperationException()
        {
            //Arrange
            ArtifactoryCredentials repoCredentials = new ArtifactoryCredentials();

            //Act
            JfrogApicommunication jfrogApicommunication = new NpmJfrogApiCommunication("", "", repoCredentials);

            //Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () => await jfrogApicommunication.CopyPackageFromRemoteRepo(new UploadArgs(),""));
        }

        [Test]
        public void NpmJfrogApiCommunication_CopyFromRemoteRepo_ReturnsInvalidOperationException()
        {
            //Arrange
            ArtifactoryCredentials repoCredentials = new ArtifactoryCredentials();

            //Act
            JfrogApicommunication jfrogApicommunication = new NpmJfrogApiCommunication("", "", repoCredentials);

            //Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () => await jfrogApicommunication.CopyFromRemoteRepo(new ComponentsToArtifactory()));
        }

        [Test]
        public void NpmJfrogApiCommunication_UpdatePackagePropertiesInJfrog_ReturnsInvalidOperationException()
        {
            //Arrange
            ArtifactoryCredentials repoCredentials = new ArtifactoryCredentials();

            //Act
            JfrogApicommunication jfrogApicommunication = new NpmJfrogApiCommunication("", "", repoCredentials);

            //Assert
            Assert.ThrowsAsync<InvalidOperationException>(() => { jfrogApicommunication.UpdatePackagePropertiesInJfrog("", "", new UploadArgs()); return Task.CompletedTask; });
        }
    }
}
