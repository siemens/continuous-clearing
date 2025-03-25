// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.APICommunications.Interfaces;
using LCT.ArtifactoryUploader;
using Moq;
using NUnit.Framework;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

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
    }
}
