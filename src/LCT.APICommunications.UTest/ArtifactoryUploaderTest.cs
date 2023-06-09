// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.APICommunications.Interfaces;
using LCT.APICommunications.Model;
using Moq;
using Newtonsoft.Json;
using System.Net;

namespace LCT.APICommunications.UTest
{
    public class ArtifactoryUploader
    {
        [SetUp]
        public void Setup()
        {
            // Method intentionally left empty.
        }

        [Test]
        public async Task UploadNPMPackageToArtifactory_InputEmptyCreds_ReturnsEmpty()
        {
            //Arrange
            int expectedCount = 0;
            ReleasesDetails releasesDetails = new ReleasesDetails();
            var sw360ApiCommunication = new Mock<ISw360ApiCommunication>();
            var artfactoryUploader = new ArtfactoryUploader(sw360ApiCommunication.Object);
            string sw360ReleaseId = String.Empty;
            string sw360releaseUrl = String.Empty;
            ArtifactoryCredentials credentials = new ArtifactoryCredentials();
            sw360ApiCommunication.Setup(x => x.GetReleaseById(It.IsAny<string>())).ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonConvert.SerializeObject(releasesDetails))
            });

            //Act
            var responseMessage = await artfactoryUploader.UploadNPMPackageToArtifactory(sw360ReleaseId, sw360releaseUrl, credentials);

            //Assert
            Assert.That(expectedCount, Is.EqualTo(responseMessage.Content.Headers.ContentLength), "Returns Zero count");
        }

        [Test]
        public void UploadNPMPackageToArtifactory_InputCreds_ReturnsInvalidOperationException()
        {
            //Arrange
            ReleasesDetails releasesDetails = new ReleasesDetails();
            releasesDetails.Name = "Angular";
            releasesDetails.Version = "1.1";
            releasesDetails.Embedded = new AttachmentEmbedded();
            var sw360ApiCommunication = new Mock<ISw360ApiCommunication>();
            var artfactoryUploader = new ArtfactoryUploader(sw360ApiCommunication.Object);
            string sw360ReleaseId = String.Empty;
            string sw360releaseUrl = String.Empty;
            ArtifactoryCredentials credentials = new ArtifactoryCredentials();
            sw360ApiCommunication.Setup(x => x.GetReleaseById(It.IsAny<string>())).ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonConvert.SerializeObject(releasesDetails))
            });

            //Act & Assert

            Assert.ThrowsAsync<InvalidOperationException>(async () => await artfactoryUploader.UploadNPMPackageToArtifactory(sw360ReleaseId, sw360releaseUrl, credentials));
        }


        [Test]
        public async Task UploadNUGETPackageToArtifactory_InputEmptyCreds_ReturnsEmpty()
        {
            //Arrange
            int expectedCount = 0;
            ReleasesDetails releasesDetails = new ReleasesDetails();
            var sw360ApiCommunication = new Mock<ISw360ApiCommunication>();
            var artfactoryUploader = new ArtfactoryUploader(sw360ApiCommunication.Object);
            string sw360ReleaseId = String.Empty;
            string sw360releaseUrl = String.Empty;
            ArtifactoryCredentials credentials = new ArtifactoryCredentials();
            sw360ApiCommunication.Setup(x => x.GetReleaseById(It.IsAny<string>())).ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonConvert.SerializeObject(releasesDetails))
            });

            //Act
            var responseMessage = await artfactoryUploader.UploadNUGETPackageToArtifactory(sw360ReleaseId, sw360releaseUrl, credentials);

            //Assert
            Assert.That(expectedCount, Is.EqualTo(responseMessage.Content.Headers.ContentLength), "Returns Zero count");
        }

        [Test]
        public void UploadPackageToRepo_InputEmptyCreds_ReturnsInvalidOperationException()
        {
            //Arrange
            ReleasesDetails releasesDetails = new ReleasesDetails();
            var sw360ApiCommunication = new Mock<ISw360ApiCommunication>();
            var artfactoryUploader = new ArtfactoryUploader(sw360ApiCommunication.Object);
            ComponentsToArtifactory componentsToArtifactory = new ComponentsToArtifactory();
            sw360ApiCommunication.Setup(x => x.GetReleaseById(It.IsAny<string>())).ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonConvert.SerializeObject(releasesDetails))
            });

            ///Act & Assert

            Assert.ThrowsAsync<InvalidOperationException>(async () => await artfactoryUploader.UploadPackageToRepo(componentsToArtifactory));
        }

        [Test]
        public void SetConfigurationValues_InputEmptyCreds_ReturnsVoid()
        {
            //Arrange
            bool returnValue = true;
            var artfactoryUploader = new ArtfactoryUploader();

            //Act
            artfactoryUploader.SetConfigurationValues();

            //Assert
            Assert.That(returnValue, Is.True);
        }
    }
}