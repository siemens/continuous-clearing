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

namespace AritfactoryUploader.UTest
{
    [TestFixture]
    public class ArtifactoryValidatorTest
    {
        [Test]
        public async Task ValidateArtifactoryCredentials_InputAppsettings_ReturnsIsvalid()
        {
            //Arrange
            JfrogKey jfrogKey = new JfrogKey() { ApiKey = "tyyteye" };
            HttpResponseMessage httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ObjectContent<JfrogKey>(jfrogKey, new JsonMediaTypeFormatter(), "application/some-format")
            };
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
            jfrogCommunicationMck.Setup(x => x.GetApiKey()).ReturnsAsync(httpResponseMessage);



            //Act

            await artifactoryValidator.ValidateArtifactoryCredentials(appSettings);

            //Assert
            jfrogCommunicationMck.Verify(x => x.GetApiKey(), Times.AtLeastOnce);
        }
    }
}
