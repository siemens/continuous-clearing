// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.APICommunications.Model;
using LCT.APICommunications.Model.Foss;
using LCT.Common.Model;
using LCT.Facade.Interfaces;
using LCT.Services.Interface;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace LCT.Services.UTest
{
    [TestFixture]
    internal class Sw360ServiceTest
    {

        [Test]
        public async Task GetProjectNameByProjectIDFromSW360_ProvidedProjectIdReturnsProjectName()
        {
            // Arrange
            ProjectReleases projectsMapper = new ProjectReleases();
            projectsMapper.Name = "Test";
            var projectDataSerialized = JsonConvert.SerializeObject(projectsMapper);
            HttpResponseMessage httpResponseMessage = new HttpResponseMessage();
            var content = new StringContent(projectDataSerialized, Encoding.UTF8, "application/json");
            httpResponseMessage.Content = content;


            Mock<ISW360ApicommunicationFacade> swApiCommunicationFacade = new Mock<ISW360ApicommunicationFacade>();
            swApiCommunicationFacade.Setup(x => x.GetProjectById("4aa1165e2d23da3d383692eb9c000a43")).ReturnsAsync(httpResponseMessage);

            // Act
            ISw360ProjectService sw360Service = new Sw360ProjectService(swApiCommunicationFacade.Object);
            ProjectReleases projectReleases = await sw360Service.GetProjectNameByProjectIDFromSW360("4aa1165e2d23da3d383692eb9c000a43", "Test");

            // Assert
            Assert.AreEqual("Test", projectReleases.Name);
        }


        [Test]
        public async Task GetAvailableReleasesInSw360_ForGivenData_Returns0results()
        {
            // Arrange
            Components component = new Components() { Name = "Zone.js", Version = "1.0.0" };
            List<Components> components = new List<Components>();
            components.Add(component);
            ComponentsRelease componentRelease = new ComponentsRelease();
            componentRelease.Embedded = new ReleaseEmbedded();
            Mock<ISW360ApicommunicationFacade> swApiCommunicationFacade = new Mock<ISW360ApicommunicationFacade>();
            swApiCommunicationFacade.Setup(x => x.GetReleases()).ReturnsAsync(string.Empty);
                
            // Act
            ISW360Service sW360Service = new Sw360Service(swApiCommunicationFacade.Object);
            var result = await sW360Service.GetAvailableReleasesInSw360(components);

            // Assert
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        [TestCase("Zone.js", "1.0.0", false, false, true, false, true)]
        [TestCase("Zone.js", "1.0.0", false, false, true, false, false)]
        [TestCase("Zone.js", "1.0.0", true, false, true, false, false)]
        [TestCase("Zone.js", "1.0.0", false, true, true, false, false)]
        [TestCase("Zone.js", "1.1.0", false, false, false, true, false)]
        [TestCase("Zone.js", "1.1.0", true, false, false, false, false)]
        [TestCase("Zone.js", "1.1.0", false, true, false, false, false)]
        [TestCase("Zonee21.js", "1.1.0", false, true, false, false, false)]
        public async Task GetAvailableReleasesInSw360_ForGivenData_ReturnsAvailableReleases(
            string name, string version, bool setHttpException,
            bool setAggregateException, bool releasestate, bool compnentstate, bool availableCompExceptionOn)
        {
            // Arrange

            Self self = new Self() { Href = "http://localhost:8090/releases/eiurowieuowereerwe88384" };
            Links links = new Links() { Self = self };
            Sw360Releases sw360Releases = new Sw360Releases() { Name = "Zone.js", Version = "1.0.0", Links = links };
            sw360Releases.ExternalIds = new ExternalIds() { Package_Url = "pkg:npm/zone.js@1.0.0" };
            List<Sw360Releases> sw360ReleasesList = new List<Sw360Releases>();
            sw360ReleasesList.Add(sw360Releases);
            ReleaseEmbedded releaseEmbedded = new ReleaseEmbedded();
            releaseEmbedded.Sw360Releases = sw360ReleasesList;
            ComponentsRelease componentsRelease = new ComponentsRelease();
            componentsRelease.Embedded = releaseEmbedded;
            var componentsReleaseModelSerialized = JsonConvert.SerializeObject(componentsRelease);


            Components component = new Components() { Name = name, Version = version };
            component.ReleaseExternalId = "pkg:npm/zone.js@1.0.0";

            List<Components> components = new List<Components>();
            components.Add(component);
            ComponentsRelease componentRelease = new ComponentsRelease();
            componentRelease.Embedded = releaseEmbedded;

            //Components model
            Self self2 = new();
            Links links2 = new();
            Sw360Components sw360Components = new();
            var componentList = new List<Sw360Components>();
            ComponentEmbedded componentEmbedded = new();

            self2.Href = "http://localhost:8090/resource/api/components/uiweriwfoowefih87398r3ur093u0";
            links2.Self = self2;
            sw360Components.Name = "Zone.js";
            sw360Components.Links = links2;
            sw360Components.ExternalIds = new ExternalIds() { Package_Url = "pkg:npm/zone.js" };
            componentList.Add(sw360Components);
            componentEmbedded.Sw360components = componentList;

            ComponentsModel componentsModel = new ComponentsModel();
            componentsModel.Embedded = componentEmbedded;
            var componentsModelSerialized = JsonConvert.SerializeObject(componentsModel);

            // Release Status

            Releasestatus releasestatus = new Releasestatus();
            releasestatus.sw360Releases = sw360Releases;
            releasestatus.isReleaseExist = releasestate;

            // Component Status
            ComponentStatus componentStatus = new ComponentStatus();
            componentStatus.isComponentExist = compnentstate;

            Mock<ISW360ApicommunicationFacade> swApiCommunicationFacade = new Mock<ISW360ApicommunicationFacade>();
            swApiCommunicationFacade.Setup(x => x.GetReleases()).ReturnsAsync(componentsReleaseModelSerialized);
            swApiCommunicationFacade.Setup(x => x.GetComponents()).ReturnsAsync(componentsModelSerialized);

            Mock<ISW360CommonService> sw360CommonService = new Mock<ISW360CommonService>();
            sw360CommonService.Setup(x => x.GetReleaseDataByExternalId(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(releasestatus);
            sw360CommonService.Setup(x => x.GetComponentDataByExternalId(It.IsAny<string>(), It.IsAny<string>()))
                  .ReturnsAsync(componentStatus);

            if (availableCompExceptionOn)
            {
                swApiCommunicationFacade.Setup(x => x.GetComponents()).Throws<HttpRequestException>();
            }
            if (setHttpException)
            {
                sw360CommonService.Setup(x => x.GetComponentDataByExternalId(It.IsAny<string>(), It.IsAny<string>()))
                    .Throws<HttpRequestException>();
                sw360CommonService.Setup(x => x.GetReleaseDataByExternalId(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                    .Throws<HttpRequestException>();
            }
            if (setAggregateException)
            {
                sw360CommonService.Setup(x => x.GetComponentDataByExternalId(It.IsAny<string>(), It.IsAny<string>()))
                    .Throws<AggregateException>();
                sw360CommonService.Setup(x => x.GetReleaseDataByExternalId(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                    .Throws<AggregateException>();
            }

            // Act
            ISW360Service sW360Service = new Sw360Service(swApiCommunicationFacade.Object, sw360CommonService.Object);
            var result = await sW360Service.GetAvailableReleasesInSw360(components);

            // Assert
            Assert.That(result.Count, Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public async Task GetAvailableReleasesInSw360_ForGivenData_ThrowsHttpRequuestException()
        {
            // Arrange
            Components component = new Components() { Name = "Zone.js", Version = "1.0.0" };
            List<Components> components = new List<Components>();
            components.Add(component);
            ComponentsRelease componentRelease = new ComponentsRelease();
            componentRelease.Embedded = new ReleaseEmbedded();
            Mock<ISW360ApicommunicationFacade> swApiCommunicationFacade = new Mock<ISW360ApicommunicationFacade>();
            swApiCommunicationFacade.Setup(x => x.GetReleases()).Throws<HttpRequestException>();

            // Act
            ISW360Service sW360Service = new Sw360Service(swApiCommunicationFacade.Object);
            var result = await sW360Service.GetAvailableReleasesInSw360(components);

            // Assert
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public async Task GetReleaseInfoByReleaseId_ForGivenReleaseLink_ReturnsHttpResponseOK()
        {
            // Arrange
            string releaseId = "http://localhost:8090/resource/api/releases/uiweriwfoowefih87398r3ur093u0";
            Mock<ISW360ApicommunicationFacade> swApiCommunicationFacade = new Mock<ISW360ApicommunicationFacade>();
            HttpResponseMessage httpResponse = new HttpResponseMessage(HttpStatusCode.OK);
            swApiCommunicationFacade.Setup(x => x.GetReleaseById(It.IsAny<string>())).ReturnsAsync(httpResponse);

            // Act
            ISW360Service sW360Service = new Sw360Service(swApiCommunicationFacade.Object);
            HttpResponseMessage responseMessage = await sW360Service.GetReleaseInfoByReleaseId(releaseId);

            // Assert
            Assert.That(responseMessage.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Test]
        public async Task GetReleaseInfoByReleaseId_ForGivenReleaseLink_ReturnsHttpResponseNull()
        {
            // Arrange         
            string releaseId = "http://localhost:8090/resource/api/releases/uiweriwfoowefih87398r3ur093u0";
            Mock<ISW360ApicommunicationFacade> swApiCommunicationFacade = new Mock<ISW360ApicommunicationFacade>();
            swApiCommunicationFacade.Setup(x => x.GetReleaseById(It.IsAny<string>())).Throws<HttpRequestException>();

            // Act
            ISW360Service sW360Service = new Sw360Service(swApiCommunicationFacade.Object);
            HttpResponseMessage responseMessage = await sW360Service.GetReleaseInfoByReleaseId(releaseId);

            // Assert
            Assert.That(responseMessage, Is.EqualTo(null));
        }

        [Test]
        public async Task GetReleaseDataOfComponent_ForGivenReleaseLink_ReturnsReleasesInfo()
        {
            // Arrange
            ReleasesInfo releasesInfo = new ReleasesInfo();
            releasesInfo.CreatedBy = "Clearingbot@company.com";
            releasesInfo.Name = "zone.js";
            releasesInfo.Version = "v1.0.0";
            releasesInfo.SourceCodeDownloadUrl = "https://github.com/angular";
            string releaseInfoContent = JsonConvert.SerializeObject(releasesInfo);
            string releaseId = "http://localhost:8090/resource/api/releases/uiweriwfoowefih87398r3ur093u0";
            Mock<ISW360ApicommunicationFacade> swApiCommunicationFacade = new Mock<ISW360ApicommunicationFacade>();
            HttpResponseMessage httpResponse = new HttpResponseMessage(HttpStatusCode.OK);
            httpResponse.Content = new StringContent(releaseInfoContent);
            swApiCommunicationFacade.Setup(x => x.GetReleaseById(It.IsAny<string>())).ReturnsAsync(httpResponse);

            // Act
            ISW360Service sW360Service = new Sw360Service(swApiCommunicationFacade.Object);
            releasesInfo = await sW360Service.GetReleaseDataOfComponent(releaseId);

            // Assert
            Assert.That(releasesInfo.Name, Is.EqualTo("zone.js"));
        }

        [Test]
        public async Task GetReleaseDataOfComponent_ForGivenReleaseLink_ReturnsAggregateException()
        {
            // Arrange
            ReleasesInfo releasesInfo = new ReleasesInfo();
            releasesInfo.CreatedBy = "Clearingbot@company.com";
            releasesInfo.Name = "zone.js";
            releasesInfo.Version = "v1.0.0";
            releasesInfo.SourceCodeDownloadUrl = "https://github.com/angular";
            string releaseInfoContent = JsonConvert.SerializeObject(releasesInfo);
            string releaseId = "http://localhost:8090/resource/api/releases/uiweriwfoowefih87398r3ur093u0";
            Mock<ISW360ApicommunicationFacade> swApiCommunicationFacade = new Mock<ISW360ApicommunicationFacade>();
            HttpResponseMessage httpResponse = new HttpResponseMessage(HttpStatusCode.OK);
            httpResponse.Content = new StringContent(releaseInfoContent);
            swApiCommunicationFacade.Setup(x => x.GetReleaseById(It.IsAny<string>())).Throws<AggregateException>();

            // Act
            ISW360Service sW360Service = new Sw360Service(swApiCommunicationFacade.Object);
            releasesInfo = await sW360Service.GetReleaseDataOfComponent(releaseId);

            // Assert
            Assert.That(releasesInfo.Name, Is.Null);
        }

        [Test]
        public async Task GetComponentReleaseID_PassComponentNameVersion_ReturnsReleaseID()
        {
            // Arrange
            Self self = new Self() { Href = "http://localhost:8090/releases/eiurowieuowereerwe88384" };
            Links links = new Links() { Self = self };
            List<Sw360Releases> sw360Releases = new List<Sw360Releases>();
            sw360Releases.Add(new Sw360Releases() { Name = "Zone.js", Version = "1.0.0", Links = links });
            ReleaseEmbedded releaseEmbedded = new ReleaseEmbedded();
            releaseEmbedded.Sw360Releases = sw360Releases;
            ComponentsRelease componentsRelease = new ComponentsRelease();
            componentsRelease.Embedded = releaseEmbedded;

            string componentReleaseresponse = JsonConvert.SerializeObject(componentsRelease);
            Mock<ISW360ApicommunicationFacade> swApiCommunicationFacade = new Mock<ISW360ApicommunicationFacade>();
            swApiCommunicationFacade.Setup(x => x.GetReleaseByCompoenentName(It.IsAny<string>())).ReturnsAsync(componentReleaseresponse);

            // Act
            ISW360Service sW360Service = new Sw360Service(swApiCommunicationFacade.Object);
            string actual = await sW360Service.GetComponentReleaseID("Zone.js", "1.0.0");

            // Assert
            Assert.That(actual, Is.EqualTo("eiurowieuowereerwe88384"));
        }

        [Test]
        public async Task GetComponentReleaseID_PassComponentNameVersion_ReturnsReleaseIDEmptyOnException()
        {
            // Arrange
            Mock<ISW360ApicommunicationFacade> swApiCommunicationFacade = new Mock<ISW360ApicommunicationFacade>();
            swApiCommunicationFacade.Setup(x => x.GetReleaseByCompoenentName(It.IsAny<string>())).Throws<HttpRequestException>();

            // Act
            ISW360Service sW360Service = new Sw360Service(swApiCommunicationFacade.Object);
            string actual = await sW360Service.GetComponentReleaseID("Zone.js", "1.0.0");

            // Assert
            Assert.That(actual, Is.EqualTo(string.Empty));
        }

        [Test]
        public async Task GetAttachmentDownloadLink_ForGivenReleaseURL_ReturnsEmptyObjOnHttpReEXception()
        {
            // Arrange
            Mock<ISW360ApicommunicationFacade> swApiCommunicationFacade = new Mock<ISW360ApicommunicationFacade>();
            swApiCommunicationFacade.Setup(x => x.GetReleaseAttachments(It.IsAny<string>())).Throws<HttpRequestException>();

            // Act
            ISW360Service sW360Service = new Sw360Service(swApiCommunicationFacade.Object);
            Sw360AttachmentHash actual = await sW360Service.GetAttachmentDownloadLink("Release Attachment Link");

            // Assert
            Assert.That(actual.AttachmentLink, Is.Null);
        }

        [Test]
        public async Task GetAttachmentDownloadLink_ForInvalidReleaseURL_ReturnsEmptyObj()
        {
            // Arrange
            Mock<ISW360ApicommunicationFacade> swApiCommunicationFacade = new Mock<ISW360ApicommunicationFacade>();

            // Act
            ISW360Service sW360Service = new Sw360Service(swApiCommunicationFacade.Object);

            Sw360AttachmentHash actual = await sW360Service.GetAttachmentDownloadLink("");

            // Assert
            Assert.That(actual.AttachmentLink, Is.Null);
        }

        [Test]
        public async Task GetAttachmentDownloadLink_ForGivenReleaseURL_ReturnsSourceNotAvailableAsTrue()
        {
            // Arrange
            Sw360Attachments sw360Attachment = new Sw360Attachments();
            sw360Attachment.AttachmentType = "SOURCEnot";
            sw360Attachment.Sha1 = "6fb90be915d915d7d166a275adba8cdede66badf";
            sw360Attachment.Filename = "angular-9.1.1.zip";
            sw360Attachment.Links = null;

            List<Sw360Attachments> sw360Attachments = new List<Sw360Attachments>();
            sw360Attachments.Add(sw360Attachment);

            AttachmentEmbedded attachmentEmbedded = new AttachmentEmbedded();
            attachmentEmbedded.Sw360attachments = sw360Attachments;

            ReleaseAttachments releaseAttachments = new ReleaseAttachments();
            releaseAttachments.Embedded = attachmentEmbedded;

            string releaseAttachmentDataSerialized = JsonConvert.SerializeObject(releaseAttachments);

            Mock<ISW360ApicommunicationFacade> swApiCommunicationFacade = new Mock<ISW360ApicommunicationFacade>();
            swApiCommunicationFacade.Setup(x => x.GetReleaseAttachments(It.IsAny<string>()))
                .ReturnsAsync(releaseAttachmentDataSerialized);

            // Act
            ISW360Service sW360Service = new Sw360Service(swApiCommunicationFacade.Object);
            Sw360AttachmentHash actual = await sW360Service.GetAttachmentDownloadLink("https://localhost:8095/resource/api/attachments/68d6186468ea56a072d28944ea0446db");

            // Assert
            Assert.That(actual.AttachmentLink, Is.Null);
        }

        [Test]
        public async Task GetAttachmentDownloadLink_ForGivenReleaseURL_ReturnsSourceNotAvailableAsFalse()
        {
            // Arrange

            Sw360Attachments sw360Attachment = new Sw360Attachments();
            sw360Attachment.AttachmentType = "SOURCE";
            sw360Attachment.Sha1 = "6fb90be915d915d7d166a275adba8cdede66badf";
            sw360Attachment.Filename = "angular-9.1.1.zip";
            sw360Attachment.Links = null;
            List<Sw360Attachments> sw360Attachments = new List<Sw360Attachments>();
            sw360Attachments.Add(sw360Attachment);
            AttachmentEmbedded attachmentEmbedded = new AttachmentEmbedded();
            attachmentEmbedded.Sw360attachments = sw360Attachments;

            ReleaseAttachments releaseAttachments = new ReleaseAttachments();
            releaseAttachments.Embedded = attachmentEmbedded;

            string releaseAttachmentDataSerialized = JsonConvert.SerializeObject(releaseAttachments);

            Mock<ISW360ApicommunicationFacade> swApiCommunicationFacade = new Mock<ISW360ApicommunicationFacade>();
            swApiCommunicationFacade.Setup(x => x.GetReleaseAttachments(It.IsAny<string>()))
                .ReturnsAsync(releaseAttachmentDataSerialized);
            // Act
            ISW360Service sW360Service = new Sw360Service(swApiCommunicationFacade.Object);
            Sw360AttachmentHash actual = await sW360Service.GetAttachmentDownloadLink("https://localhost:8095/resource/api/attachments/68d6186468ea56a072d28944ea0446db");

            // Assert
            Assert.That(actual.AttachmentLink, Is.Empty);
        }

        [Test]
        public async Task GetAttachmentDownloadLink_ForGivenReleaseURL_ReturnsSourceDownloadLink()
        {
            // Arrange

            AttachmentLinks attachmentLinks = new AttachmentLinks();
            attachmentLinks.Self = new Self() { Href = "https://localhost:8095/resource/api/attachments/68d6186468ea56a072d28944ea0446db" };

            Sw360Attachments sw360Attachment = new Sw360Attachments();
            sw360Attachment.AttachmentType = "SOURCE";
            sw360Attachment.Sha1 = "6fb90be915d915d7d166a275adba8cdede66badf";
            sw360Attachment.Filename = "angular-9.1.1.zip";
            sw360Attachment.Links = attachmentLinks;

            List<Sw360Attachments> sw360Attachments = new List<Sw360Attachments>();
            sw360Attachments.Add(sw360Attachment);
            AttachmentEmbedded attachmentEmbedded = new AttachmentEmbedded();
            attachmentEmbedded.Sw360attachments = sw360Attachments;
            ReleaseAttachments releaseAttachments = new ReleaseAttachments();
            releaseAttachments.Embedded = attachmentEmbedded;

            string releaseAttachmentDataSerialized = JsonConvert.SerializeObject(releaseAttachments);

            SW360DownloadHref sW360DownloadHref = new SW360DownloadHref()
            { DownloadUrl = "https://localhost:8095/resource/api/releases/68d6186468ea56a072d28944ea04319f/attachments/68d6186468ea56a072d28944ea0446db" };
            SW360DownloadLinks sW360DownloadLinks = new SW360DownloadLinks();
            sW360DownloadLinks.Sw360DownloadLink = sW360DownloadHref;
            AttachmentLink attachmentLink = new AttachmentLink();
            attachmentLink.Links = sW360DownloadLinks;

            string attachmentLinkDataSerialized = JsonConvert.SerializeObject(attachmentLink);

            Mock<ISW360ApicommunicationFacade> swApiCommunicationFacade = new Mock<ISW360ApicommunicationFacade>();
            swApiCommunicationFacade.Setup(x => x.GetReleaseAttachments(It.IsAny<string>())).ReturnsAsync(releaseAttachmentDataSerialized);
            swApiCommunicationFacade.Setup(x => x.GetAttachmentInfo(It.IsAny<string>())).ReturnsAsync(attachmentLinkDataSerialized);

            // Act
            ISW360Service sW360Service = new Sw360Service(swApiCommunicationFacade.Object);
            Sw360AttachmentHash actual = await sW360Service.GetAttachmentDownloadLink("https://localhost:8095/resource/api/attachments/68d6186468ea56a072d28944ea0446db");

            // Assert
            Assert.That(actual.AttachmentLink, Is.Not.Null);
        }

        [Test]
        public async Task GetAttachmentDownloadLink_ForGivenReleaseURL_ReturnsEmptyObjOnAggregateEXception()
        {
            // Arrange
            Mock<ISW360ApicommunicationFacade> swApiCommunicationFacade = new Mock<ISW360ApicommunicationFacade>();
            swApiCommunicationFacade.Setup(x => x.GetReleaseAttachments(It.IsAny<string>())).Throws<AggregateException>();

            // Act
            ISW360Service sW360Service = new Sw360Service(swApiCommunicationFacade.Object);

            Sw360AttachmentHash actual = await sW360Service.GetAttachmentDownloadLink("Release Attachment Link");

            // Assert
            Assert.That(actual.AttachmentLink, Is.Null);
        }
    }
}
