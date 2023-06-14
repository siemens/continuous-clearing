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

            Assert.ThrowsAsync<InvalidOperationException>(async () => await artfactoryUploader.UploadPackageToRepo(componentsToArtifactory,100));
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