// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.Common.Model;
using Newtonsoft.Json;
using System.Text;

namespace LCT.APICommunications.UTest
{
    public class SW360ApicommunicationUTest
    {
        readonly SW360ConnectionSettings connectionSettings = new SW360ConnectionSettings();

        [SetUp]
        public void Setup()
        {
            connectionSettings.Timeout = 5;
            connectionSettings.SW360AuthTokenType = "Token";
        }

        [Test]
        public void SW360Apicommunication_GetProjects_ReturnsInvalidOperationException()
        {
            //Act
            SW360Apicommunication sW360Apicommunication = new SW360Apicommunication(connectionSettings);

            //Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () => await sW360Apicommunication.GetProjects());
        }

        [Test]
        public void SW360Apicommunication_GetSw360Users_ReturnsInvalidOperationException()
        {
            //Act
            SW360Apicommunication sW360Apicommunication = new SW360Apicommunication(connectionSettings);

            //Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () => await sW360Apicommunication.GetSw360Users());
        }

        [Test]
        public void SW360Apicommunication_GetProjectsByName_ReturnsInvalidOperationException()
        {
            //Act
            SW360Apicommunication sW360Apicommunication = new SW360Apicommunication(connectionSettings);

            //Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () => await sW360Apicommunication.GetProjectsByName(""));
        }

        [Test]
        public void SW360Apicommunication_GetProjectsByTag_ReturnsInvalidOperationException()
        {
            //Act
            SW360Apicommunication sW360Apicommunication = new SW360Apicommunication(connectionSettings);

            //Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () => await sW360Apicommunication.GetProjectsByTag(""));
        }

        [Test]
        public void SW360Apicommunication_CheckFossologyProcessStatus_ReturnsInvalidOperationException()
        {
            //Act
            SW360Apicommunication sW360Apicommunication = new SW360Apicommunication(connectionSettings);

            //Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () => await sW360Apicommunication.CheckFossologyProcessStatus(""));
        }

        [Test]
        public void SW360Apicommunication_GetReleaseByLink_ReturnsInvalidOperationException()
        {
            //Act
            SW360Apicommunication sW360Apicommunication = new SW360Apicommunication(connectionSettings);

            //Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () => await sW360Apicommunication.GetReleaseByLink(""));
        }

        [Test]
        public void SW360Apicommunication_GetComponentUsingName_ReturnsInvalidOperationException()
        {
            //Act
            SW360Apicommunication sW360Apicommunication = new SW360Apicommunication(connectionSettings);

            //Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () => await sW360Apicommunication.GetComponentUsingName(""));
        }

        [Test]
        public void SW360Apicommunication_GetReleaseAttachments_ReturnsInvalidOperationException()
        {
            //Act
            SW360Apicommunication sW360Apicommunication = new SW360Apicommunication(connectionSettings);

            //Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () => await sW360Apicommunication.GetReleaseAttachments(""));
        }

        [Test]
        public void SW360Apicommunication_GetAttachmentInfo_ReturnsInvalidOperationException()
        {
            //Arrange & Act
            SW360Apicommunication sW360Apicommunication = new SW360Apicommunication(connectionSettings);

            //Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () => await sW360Apicommunication.GetAttachmentInfo(""));
        }

        [Test]
        public void SW360Apicommunication_DownloadAttachmentUsingWebClient_ReturnsUriFormatException()
        {
            //Arrange & Act
            SW360Apicommunication sW360Apicommunication = new SW360Apicommunication(connectionSettings);

            //Assert
            Assert.ThrowsAsync<UriFormatException>(() => { sW360Apicommunication.DownloadAttachmentUsingWebClient("attachmentDownloadLink", ""); return Task.CompletedTask; });
        }

        [Test]
        public void SW360Apicommunication_GetReleaseByCompoenentName_ReturnsInvalidOperationException()
        {
            //Arrange & Act
            SW360Apicommunication sW360Apicommunication = new SW360Apicommunication(connectionSettings);

            //Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () => await sW360Apicommunication.GetReleaseByCompoenentName(""));
        }

        [Test]
        public void SW360Apicommunication_GetComponentDetailsByUrl_ReturnsInvalidOperationException()
        {
            //Arrange & Act
            SW360Apicommunication sW360Apicommunication = new SW360Apicommunication(connectionSettings);

            //Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () => await sW360Apicommunication.GetComponentDetailsByUrl(""));
        }

        [Test]
        public void SW360Apicommunication_UpdateComponent_ReturnsInvalidOperationException()
        {
            //Arrange
            HttpContent httpContent;
            var jsonString = JsonConvert.SerializeObject("");
            httpContent = new StringContent(jsonString, Encoding.UTF8, "application/json");
            
            //Arrange
            SW360Apicommunication sW360Apicommunication = new SW360Apicommunication(connectionSettings);

            //Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () => await sW360Apicommunication.UpdateComponent("", httpContent));
        }
    }
}
