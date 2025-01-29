// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.Common;
using LCT.APICommunications;
using LCT.APICommunications.Model;
using LCT.ArtifactoryUploader;
using LCT.ArtifactoryUploader.Model;
using Moq;
using NUnit.Framework;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading.Tasks;
using UnitTestUtilities;
using LCT.APICommunications.Interfaces;

namespace AritfactoryUploader.UTest
{
    [TestFixture]
    public class ArtifactoryValidatorTest
    {
        [Test]
        public async Task ValidateArtifactoryCredentials_ValidCredentials_ReturnsZero()
        {
            // Arrange
            HttpResponseMessage httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);
            Mock<IJfrogAqlApiCommunication> jfrogCommunicationMck = new Mock<IJfrogAqlApiCommunication>();
            jfrogCommunicationMck.Setup(x => x.CheckConnection()).ReturnsAsync(httpResponseMessage);
            ArtifactoryValidator artifactoryValidator = new ArtifactoryValidator(jfrogCommunicationMck.Object);

            // Act
            int result = await artifactoryValidator.ValidateArtifactoryCredentials();

            // Assert
            Assert.AreEqual(0, result);
        }

        [Test]
        public async Task ValidateArtifactoryCredentials_InvalidCredentials_ReturnsMinusOne()
        {
            // Arrange
            HttpResponseMessage httpResponseMessage = new HttpResponseMessage(HttpStatusCode.Unauthorized);
            Mock<IJfrogAqlApiCommunication> jfrogCommunicationMck = new Mock<IJfrogAqlApiCommunication>();
            jfrogCommunicationMck.Setup(x => x.CheckConnection()).ReturnsAsync(httpResponseMessage);
            ArtifactoryValidator artifactoryValidator = new ArtifactoryValidator(jfrogCommunicationMck.Object);

            // Act
            int result = await artifactoryValidator.ValidateArtifactoryCredentials();

            // Assert
            Assert.AreEqual(-1, result);
        }

        [Test]
        public async Task ValidateArtifactoryCredentials_HttpRequestException_ReturnsMinusOne()
        {
            // Arrange
            Mock<IJfrogAqlApiCommunication> jfrogCommunicationMck = new Mock<IJfrogAqlApiCommunication>();
            jfrogCommunicationMck.Setup(x => x.CheckConnection()).ThrowsAsync(new HttpRequestException());
            ArtifactoryValidator artifactoryValidator = new ArtifactoryValidator(jfrogCommunicationMck.Object);

            // Act
            int result = await artifactoryValidator.ValidateArtifactoryCredentials();

            // Assert
            Assert.AreEqual(-1, result);
        }

        [Test]
        public async Task ValidateArtifactoryCredentials_InputAppsettings_ThrowsHttpRequestException()
        {
            // Arrange
            CommonAppSettings appSettings = new CommonAppSettings()
            {
                ArtifactoryUploadApiKey = "tyyteye",
                ArtifactoryUploadUser = "user@test.com"
            };
            ArtifactoryCredentials artifactoryCredentials = new ArtifactoryCredentials()
            {
                ApiKey = "tyyteye",
                Email = "user@test.com"
            };
            Mock<NpmJfrogApiCommunication> jfrogCommunicationMck = new Mock<NpmJfrogApiCommunication>(UTParams.JFrogURL, "test", artifactoryCredentials, 100);
            ArtifactoryValidator artifactoryValidator = new ArtifactoryValidator(jfrogCommunicationMck.Object);
            jfrogCommunicationMck.Setup(x => x.GetApiKey()).ThrowsAsync(new HttpRequestException());

            // Act and Assert
            var isvalid= await artifactoryValidator.ValidateArtifactoryCredentials(appSettings);
            Assert.AreEqual(-1, isvalid);

        }
    }
}
