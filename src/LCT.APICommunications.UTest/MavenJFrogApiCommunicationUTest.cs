using LCT.APICommunications.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LCT.APICommunications.UTest
{
    public class MavenJFrogApiCommunicationUTest
    {
        [Test]
        public void MavenJfrogApiCommunication_CopyFromRemoteRepo_ReturnsInvalidOperationException()
        {
            //Arrange
            ArtifactoryCredentials repoCredentials = new ArtifactoryCredentials();

            //Act
            JfrogApicommunication jfrogApicommunication = new MavenJfrogApiCommunication("", "", repoCredentials, 100);

            //Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () => await jfrogApicommunication.CopyFromRemoteRepo(new ComponentsToArtifactory()));
        }

        [Test]
        public void MavenJfrogApiCommunication_UpdatePackagePropertiesInJfrog_ReturnsInvalidOperationException()
        {
            //Arrange
            ArtifactoryCredentials repoCredentials = new ArtifactoryCredentials();

            //Act
            JfrogApicommunication jfrogApicommunication = new MavenJfrogApiCommunication("", "", repoCredentials, 100);

            //Assert
            Assert.ThrowsAsync<InvalidOperationException>(() => { jfrogApicommunication.UpdatePackagePropertiesInJfrog("", "", new UploadArgs()); return Task.CompletedTask; });
        }

        [Test]
        public void MavenJfrogApiCommunication_GetPackageInfo_ReturnsInvalidOperationException()
        {
            //Arrange
            ArtifactoryCredentials repoCredentials = new ArtifactoryCredentials();

            //Act
            JfrogApicommunication jfrogApicommunication = new MavenJfrogApiCommunication("", "", repoCredentials, 100);

            //Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () => await jfrogApicommunication.GetPackageInfo(new ComponentsToArtifactory()));
        }
    }
}
