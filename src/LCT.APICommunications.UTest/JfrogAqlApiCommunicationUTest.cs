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
       
        
        [Test]
        public void JfrogAqlApiCommunication_CheckConnection_ReturnsInvalidOperationException()
        {
            // Arrange
            ArtifactoryCredentials repoCredentials = new ArtifactoryCredentials();
            string invalidDomainName = ""; // Invalid domain name
            int timeout = 30; // Timeout in seconds

            JfrogAqlApiCommunication jfrogApiCommunication = new JfrogAqlApiCommunication(invalidDomainName, repoCredentials, timeout);

            // Act & Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () => await jfrogApiCommunication.CheckConnection());
        }

        [Test]
        public void JfrogAqlApiCommunication_GetInternalComponentDataByRepo_ReturnsInvalidOperationException()
        {
            // Arrange
            ArtifactoryCredentials repoCredentials = new ArtifactoryCredentials();
            string invalidDomainName = ""; // Invalid domain name
            int timeout = 30; // Timeout in seconds
            string invalidRepoName = "invalid-repo-name"; // Invalid repo name

            JfrogAqlApiCommunication jfrogApiCommunication = new JfrogAqlApiCommunication(invalidDomainName, repoCredentials, timeout);

            // Act & Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () => await jfrogApiCommunication.GetInternalComponentDataByRepo(invalidRepoName));
        }        
        [Test]
        public void JfrogAqlApiCommunication_GetNpmComponentDataByRepo_ReturnsInvalidOperationException()
        {
            // Arrange
            ArtifactoryCredentials repoCredentials = new ArtifactoryCredentials();
            string invalidDomainName = ""; // Invalid domain name
            int timeout = 30; // Timeout in seconds
            string invalidRepoName = "invalid-npm-repo"; // Invalid repo name

            JfrogAqlApiCommunication jfrogApiCommunication = new JfrogAqlApiCommunication(invalidDomainName, repoCredentials, timeout);

            // Act & Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () => await jfrogApiCommunication.GetNpmComponentDataByRepo(invalidRepoName));
        }
        [Test]
        public void JfrogAqlApiCommunication_GetPypiComponentDataByRepo_ReturnsInvalidOperationException()
        {
            // Arrange
            ArtifactoryCredentials repoCredentials = new ArtifactoryCredentials();
            string invalidDomainName = ""; // Invalid domain name
            int timeout = 30; // Timeout in seconds
            string invalidRepoName = "invalid-pypi-repo"; // Invalid repo name

            JfrogAqlApiCommunication jfrogApiCommunication = new JfrogAqlApiCommunication(invalidDomainName, repoCredentials, timeout);

            // Act & Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () => await jfrogApiCommunication.GetPypiComponentDataByRepo(invalidRepoName));
        }
        [Test]
        public void JfrogAqlApiCommunication_GetPackageInfo_ReturnsArgumentException_WhenNoPackageNameOrPathProvided()
        {
            // Arrange
            ArtifactoryCredentials repoCredentials = new ArtifactoryCredentials();
            string invalidDomainName = ""; // Invalid domain name
            int timeout = 30; // Timeout in seconds

            JfrogAqlApiCommunication jfrogApiCommunication = new JfrogAqlApiCommunication(invalidDomainName, repoCredentials, timeout);

            // Create a ComponentsToArtifactory object with invalid parameters (both packageName and path are null or empty)
            ComponentsToArtifactory component = new ComponentsToArtifactory
            {
                JfrogPackageName = null,
                Path = null
            };

            // Act & Assert
            Assert.ThrowsAsync<ArgumentException>(async () => await jfrogApiCommunication.GetPackageInfo(component));
        }

    }
}
