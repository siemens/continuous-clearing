// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.APICommunications.Model;
using LCT.Common.Model;
using Newtonsoft.Json;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;

namespace LCT.APICommunications.UTest
{
    [TestFixture]
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
        public void SW360Apicommunication_TriggerFossologyProcess_ReturnsInvalidOperationException()
        {
            // Arrange
            var releaseId = "12345";
            var sw360link = "someLink";

            // Act
            SW360Apicommunication sW360Apicommunication = new SW360Apicommunication(connectionSettings);

            // Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () => await sW360Apicommunication.TriggerFossologyProcess(releaseId, sw360link));
        }
        [Test]
        public void SW360Apicommunication_GetComponentByExternalId_ReturnsInvalidOperationException()
        {
            // Arrange
            var purlId = "12345";
            var externalIdKey = "someLink";

            // Act
            SW360Apicommunication sW360Apicommunication = new SW360Apicommunication(connectionSettings);

            // Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () => await sW360Apicommunication.GetComponentByExternalId(purlId, externalIdKey));
        }
        [Test]
        public void SW360Apicommunication_UpdateLinkedRelease_ReturnsInvalidOperationException()
        {
            // Arrange
            var projectId = "12345";
            var releaseId = "someLink";
            UpdateLinkedRelease updateLinkedRelease = new UpdateLinkedRelease();
            // Act
            SW360Apicommunication sW360Apicommunication = new SW360Apicommunication(connectionSettings);

            // Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () => await sW360Apicommunication.UpdateLinkedRelease(projectId, releaseId, updateLinkedRelease));
        }
        [Test]
        public void SW360Apicommunication_CreateComponent_ReturnsInvalidOperationException()
        {
            // Arrange
            CreateComponent createComponentContent = new CreateComponent();

            // Act
            SW360Apicommunication sW360Apicommunication = new SW360Apicommunication(connectionSettings);

            // Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () => await sW360Apicommunication.CreateComponent(createComponentContent,""));
        }
        [Test]
        public void SW360Apicommunication_CreateRelease_ReturnsInvalidOperationException()
        {
            // Arrange
            Releases createReleaseContent = new Releases();

            // Act
            SW360Apicommunication sW360Apicommunication = new SW360Apicommunication(connectionSettings);

            // Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () => await sW360Apicommunication.CreateRelease(createReleaseContent, ""));
        }
        [Test]
        public void SW360Apicommunication_UpdateRelease_ReturnsInvalidOperationException()
        {
            // Arrange
            var releaseId = "12345";
            var jsonString = JsonConvert.SerializeObject(new { });
            var httpContent = new StringContent(jsonString, Encoding.UTF8, "application/json");

            // Act
            SW360Apicommunication sW360Apicommunication = new SW360Apicommunication(connectionSettings);

            // Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () => await sW360Apicommunication.UpdateRelease(releaseId, httpContent,""));
        }

        [Test]
        public void SW360Apicommunication_DownloadAttachmentUsingWebClient_ThrowsWebException()
        {
            // Arrange
            var attachmentDownloadLink = "https://example.com/attachment";
            var fileName = "test-file";

            // Act & Assert
            Assert.Throws<WebException>(() =>
                new SW360Apicommunication(connectionSettings).DownloadAttachmentUsingWebClient(attachmentDownloadLink, fileName));
        }
        [Test]
        public void SW360Apicommunication_GetAllReleasesWithAllData_ThrowsInvalidOperationException()
        {
            // Arrange
            var page = 1;
            var pageEntries = 10;
            string correlationId = Guid.NewGuid().ToString();
            // Act
            SW360Apicommunication sW360Apicommunication = new SW360Apicommunication(connectionSettings);

            // Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () => await sW360Apicommunication.GetAllReleasesWithAllData(page, pageEntries, correlationId));
        }

        [Test]
        public void SW360Apicommunication_AttachComponentSourceToSW360_ThrowsUriFormatException()
        {
            // Arrange
            AttachReport attachReport = new AttachReport
            {
                ReleaseId = "invalid-url",
                AttachmentFile = "test-file"
            };

            // Act
            SW360Apicommunication sW360Apicommunication = new SW360Apicommunication(connectionSettings);

            // Assert
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Assert.Throws<UriFormatException>(() => sW360Apicommunication.AttachComponentSourceToSW360(attachReport));
            }
            else
            {
                Assert.Throws<InvalidCastException>(() => sW360Apicommunication.AttachComponentSourceToSW360(attachReport));
            }
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
            Assert.ThrowsAsync<InvalidOperationException>(async () => await sW360Apicommunication.CheckFossologyProcessStatus("",""));
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
            Assert.ThrowsAsync<InvalidOperationException>(async () => await sW360Apicommunication.GetReleaseByCompoenentName("", ""));
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
        public void SW360Apicommunication_GetComponentByName_ReturnsInvalidOperationException()
        {
            //Arrange & Act
            SW360Apicommunication sW360Apicommunication = new SW360Apicommunication(connectionSettings);

            //Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () => await sW360Apicommunication.GetComponentByName("", ""));
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
