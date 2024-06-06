// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.APICommunications.Model;
using LCT.Facade.Interfaces;
using LCT.Services.Interface;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using LCT.Common.Model;
using UnitTestUtilities;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using LCT.APICommunications.Model.Foss;
using System.Text;
using LCT.APICommunications;
using LCT.Services.Model;

namespace LCT.Services.UTest
{
    [TestFixture]
    public class Sw360CreatorServiceTest
    {
        [SetUp]
        public void Setup()
        {
            //implement
        }

        [Test]
        public async Task TriggerFossologyProcess_PassReleaseId_SuccessfullyTriggers()
        {
            // Arrange
            FossTriggerStatus fossTriggerStatus = new FossTriggerStatus();
            fossTriggerStatus.Links = new Links() { Self = new Self() { Href = "fossology upload link" } };
            var fossTriggerStatusSerialized = JsonConvert.SerializeObject(fossTriggerStatus);

            Mock<ISW360ApicommunicationFacade> sw360ApiCommMock = new Mock<ISW360ApicommunicationFacade>();
            sw360ApiCommMock.Setup(x => x.TriggerFossologyProcess(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(fossTriggerStatusSerialized);
            ISw360CreatorService sw360CreatorService = new Sw360CreatorService(sw360ApiCommMock.Object);

            // Act
            var actual = await sw360CreatorService.TriggerFossologyProcess("eruiuwecsjkdnfuieoyieoeiwu", "sw360link");

            // Assert
            Assert.That(actual, Is.Not.Null);
        }

        [Test]
        public async Task TriggerFossologyProcess_PassReleaseId_ThrowsHttpRequestException()
        {
            // Arrange
            Mock<ISW360ApicommunicationFacade> sw360ApiCommMock = new Mock<ISW360ApicommunicationFacade>();
            sw360ApiCommMock.Setup(x => x.TriggerFossologyProcess(It.IsAny<string>(), It.IsAny<string>()))
                .Throws<HttpRequestException>();
            ISw360CreatorService sw360CreatorService = new Sw360CreatorService(sw360ApiCommMock.Object);

            // Act
            var actual = await sw360CreatorService.TriggerFossologyProcess("eruiuwecsjkdnfuieoyieoeiwu", "sw360link");

            // Assert
            Assert.That(actual, Is.Null);
        }

        [Test]
        public async Task CheckFossologyProcessStatus_PassFossologyLink_ReturnsFossTriggerStatus()
        {
            // Arrange
            FossTriggerStatus fossTriggerStatus = new FossTriggerStatus();
            fossTriggerStatus.Links = new Links() { Self = new Self() { Href = "fossology upload link" } };
            var fossTriggerStatusSerialized = JsonConvert.SerializeObject(fossTriggerStatus);
            HttpResponseMessage httpResponseMessage = new HttpResponseMessage();
            var content = new StringContent(fossTriggerStatusSerialized, Encoding.UTF8, "application/json");
            httpResponseMessage.Content = content;
            Mock<ISW360ApicommunicationFacade> sw360ApiCommMock = new Mock<ISW360ApicommunicationFacade>();
            sw360ApiCommMock.Setup(x => x.CheckFossologyProcessStatus(It.IsAny<string>())).ReturnsAsync(httpResponseMessage);
            ISw360CreatorService sw360CreatorService = new Sw360CreatorService(sw360ApiCommMock.Object);

            // Act
            var actual = await sw360CreatorService.CheckFossologyProcessStatus("Fossologylink");

            // Assert
            Assert.That(actual, Is.Not.Null);
        }

        [Test]
        public async Task CheckFossologyProcessStatus_PassFossologyLink_ThrowsJsonReaderException()
        {
            // Arrange
            FossTriggerStatus fossTriggerStatus = new FossTriggerStatus();
            fossTriggerStatus.Links = new Links() { Self = new Self() { Href = "fossology upload link" } };
            var fossTriggerStatusSerialized = JsonConvert.SerializeObject(fossTriggerStatus);
            HttpResponseMessage httpResponseMessage = new HttpResponseMessage();
            var content = new StringContent(fossTriggerStatusSerialized, Encoding.UTF8, "application/json");
            httpResponseMessage.Content = content;
            Mock<ISW360ApicommunicationFacade> sw360ApiCommMock = new Mock<ISW360ApicommunicationFacade>();
            sw360ApiCommMock.Setup(x => x.CheckFossologyProcessStatus(It.IsAny<string>())).
                Throws<JsonReaderException>();
            ISw360CreatorService sw360CreatorService = new Sw360CreatorService(sw360ApiCommMock.Object);

            // Act
            var actual = await sw360CreatorService.CheckFossologyProcessStatus("Fossologylink");

            // Assert
            Assert.That(actual, Is.Null);
        }

        [Test]
        public async Task CheckFossologyProcessStatus_PassFossologyLink_ThrowsHttpRequestException()
        {
            // Arrange
            FossTriggerStatus fossTriggerStatus = new FossTriggerStatus();
            fossTriggerStatus.Links = new Links() { Self = new Self() { Href = "fossology upload link" } };
            var fossTriggerStatusSerialized = JsonConvert.SerializeObject(fossTriggerStatus);
            HttpResponseMessage httpResponseMessage = new HttpResponseMessage();
            var content = new StringContent(fossTriggerStatusSerialized, Encoding.UTF8, "application/json");
            httpResponseMessage.Content = content;
            Mock<ISW360ApicommunicationFacade> sw360ApiCommMock = new Mock<ISW360ApicommunicationFacade>();
            sw360ApiCommMock.Setup(x => x.CheckFossologyProcessStatus(It.IsAny<string>())).
                Throws<HttpRequestException>();
            ISw360CreatorService sw360CreatorService = new Sw360CreatorService(sw360ApiCommMock.Object);

            // Act
            var actual = await sw360CreatorService.CheckFossologyProcessStatus("Fossologylink");

            // Assert
            Assert.That(actual, Is.Null);
        }

        [Test]
        public async Task LinkReleasesToProject_InternalServerError_LinkingFailed()
        {
            // Arrange
            HttpResponseMessage httpResponseMessage = new HttpResponseMessage(HttpStatusCode.InternalServerError);
            Mock<ISW360ApicommunicationFacade> sw360ApiCommMock = new Mock<ISW360ApicommunicationFacade>();
            sw360ApiCommMock.Setup(x => x.LinkReleasesToProject(It.IsAny<HttpContent>(), It.IsAny<string>())).ReturnsAsync(httpResponseMessage);
            ISw360CreatorService sw360CreatorService = new Sw360CreatorService(sw360ApiCommMock.Object);

            // Act
            var releasesToBeLinked = new List<ReleaseLinked> { new ReleaseLinked { Name = "GitVersion.CommandLine", Version = "5.3.6", ReleaseId = "5e71f9fc2bf9438e9f20a94dddcc6a0f", Comment = "Linked by CA Tool", Relation = "UNKNOWN" } };
            var manuallyLinkedReleases = new List<ReleaseLinked> { new ReleaseLinked { Name = "", Version = "", ReleaseId = "b565b2cc63fd4cff8f4e68fa1b5d5bd3", Comment = "", Relation = "CONTAINED" } };
            bool isLinked = await sw360CreatorService.LinkReleasesToProject(releasesToBeLinked, manuallyLinkedReleases, "fgjk53657989u09ipoklyiutr");

            // Assert
            Assert.That(isLinked, Is.False, "Linking Release failed");
        }

        [Test]
        public async Task LinkReleasesToProject_HttpRequestException_LinkingFailed()
        {
            // Arrange
            Mock<ISW360ApicommunicationFacade> sw360ApiCommMock = new Mock<ISW360ApicommunicationFacade>();
            sw360ApiCommMock.Setup(x => x.LinkReleasesToProject(It.IsAny<HttpContent>(), It.IsAny<string>())).Throws<HttpRequestException>();
            ISw360CreatorService sw360CreatorService = new Sw360CreatorService(sw360ApiCommMock.Object);

            // Act

            var releasesToBeLinked = new List<ReleaseLinked> { new ReleaseLinked { Name = "GitVersion.CommandLine", Version = "5.3.6", ReleaseId = "5e71f9fc2bf9438e9f20a94dddcc6a0f", Comment = "Linked by CA Tool", Relation = "UNKNOWN" } };
            var manuallyLinkedReleases = new List<ReleaseLinked> { new ReleaseLinked { Name = "", Version = "", ReleaseId = "b565b2cc63fd4cff8f4e68fa1b5d5bd3", Comment = "", Relation = "CONTAINED" } };
            bool isLinked = await sw360CreatorService.LinkReleasesToProject(releasesToBeLinked, manuallyLinkedReleases, "fgjk53657989u09ipoklyiutr");

            // Assert
            Assert.That(isLinked, Is.False, "Linking Release failed");
        }

        [Test]
        public async Task LinkReleasesToProject_AggregateException_LinkingFailed()
        {
            // Arrange
            Mock<ISW360ApicommunicationFacade> sw360ApiCommMock = new Mock<ISW360ApicommunicationFacade>();
            sw360ApiCommMock.Setup(x => x.LinkReleasesToProject(It.IsAny<HttpContent>(), It.IsAny<string>())).Throws<AggregateException>();
            ISw360CreatorService sw360CreatorService = new Sw360CreatorService(sw360ApiCommMock.Object);

            // Act
            var releasesToBeLinked = new List<ReleaseLinked> { new ReleaseLinked { Name = "GitVersion.CommandLine", Version = "5.3.6", ReleaseId = "5e71f9fc2bf9438e9f20a94dddcc6a0f", Comment = "Linked by CA Tool", Relation = "UNKNOWN" } };
            var manuallyLinkedReleases = new List<ReleaseLinked> { new ReleaseLinked { Name = "", Version = "", ReleaseId = "b565b2cc63fd4cff8f4e68fa1b5d5bd3", Comment = "", Relation = "CONTAINED" } };
            bool isLinked = await sw360CreatorService.LinkReleasesToProject(releasesToBeLinked, manuallyLinkedReleases, "fgjk53657989u09ipoklyiutr");

            // Assert
            Assert.That(isLinked, Is.False, "Linking Release failed");
        }

        [Test]
        public async Task LinkReleasesToProject_ReleasesLinkedAndCommentUpdated_Success()
        {
            // Arrange
            HttpResponseMessage httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);
            Mock<ISW360ApicommunicationFacade> sw360ApiCommMock = new Mock<ISW360ApicommunicationFacade>();
            sw360ApiCommMock.Setup(x => x.LinkReleasesToProject(It.IsAny<HttpContent>(), It.IsAny<string>())).ReturnsAsync(httpResponseMessage);
            sw360ApiCommMock.Setup(x => x.UpdateLinkedRelease(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UpdateLinkedRelease>())).ReturnsAsync(httpResponseMessage);
            ISw360CreatorService sw360CreatorService = new Sw360CreatorService(sw360ApiCommMock.Object);

            // Act
            var releasesToBeLinked = new List<ReleaseLinked> { new ReleaseLinked { Name = "GitVersion.CommandLine", Version = "5.3.6", ReleaseId = "5e71f9fc2bf9438e9f20a94dddcc6a0f", Comment = "Linked by CA Tool", Relation = "UNKNOWN" } };
            var manuallyLinkedReleases = new List<ReleaseLinked> { new ReleaseLinked { Name = "", Version = "", ReleaseId = "b565b2cc63fd4cff8f4e68fa1b5d5bd3", Comment = "", Relation = "CONTAINED" } };
            bool isLinked = await sw360CreatorService.LinkReleasesToProject(releasesToBeLinked, manuallyLinkedReleases, "fgjk53657989u09ipoklyiutr");

            // Assert
            Assert.That(isLinked, Is.True, "Linking Release success");
        }

        [TestCase]
        public async Task Test_CreateComponentBasesOFswComaprisonBOM_Negative()
        {
            //Arrange
            Mock<ISW360ApicommunicationFacade> sw360ApiCommMock = new Mock<ISW360ApicommunicationFacade>();
            var ComparisonBomData = new ComparisonBomData()
            {
                Name = "Test"
            };
            var attachmentUrlList = new Dictionary<string, string>
            {
                { "Test", "Test" }
            };
            var httpResponseMessage = new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest);
            sw360ApiCommMock.Setup(x => x.CreateComponent(It.IsAny<CreateComponent>())).ReturnsAsync(httpResponseMessage);
            // Act
            var sw360CreatorService = new Sw360CreatorService(sw360ApiCommMock.Object);
            var actual = await sw360CreatorService.CreateComponentBasesOFswComaprisonBOM(ComparisonBomData, attachmentUrlList);

            //Assert
            Assert.AreEqual(false, actual.IsCreated);
        }

        [TestCase]
        public async Task CreateComponentBasesOFswComaprisonBOM_ForGivenBOMData_ReturnsEmptyStatusOnHttpRequestException()
        {
            //Arrange
            Mock<ISW360ApicommunicationFacade> sw360ApiCommMock = new Mock<ISW360ApicommunicationFacade>();
            var ComparisonBomData = new ComparisonBomData()
            {
                Name = "Test"
            };
            var attachmentUrlList = new Dictionary<string, string>
            {
                { "Test", "Test" }
            };
            sw360ApiCommMock.Setup(x => x.CreateComponent(It.IsAny<CreateComponent>())).Throws<HttpRequestException>();

            // Act
            var sw360CreatorService = new Sw360CreatorService(sw360ApiCommMock.Object);
            var expected = await sw360CreatorService.CreateComponentBasesOFswComaprisonBOM(ComparisonBomData, attachmentUrlList);

            //Assert
            Assert.That(expected.IsCreated, Is.False);
        }

        [TestCase]
        public async Task Test_CreateReleaseForComponent()
        {
            //Arrange
            var ComparisonBomData = new ComparisonBomData()
            {
                Name = "Test",
                Version = "1",

            };
            var componentId = "1";
            var attachmentUrlList = new Dictionary<string, string>
            {
                { "fossology url", UTParams.FossologyURL }
            };
            var releases = new Releases()
            {
                Links = new Links
                {
                    Self = new Self
                    {
                        Href = "/1"
                    }
                }
            };
            HttpResponseMessage httpResponseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonConvert.SerializeObject(releases)),
            };
            httpResponseMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            Mock<ISW360ApicommunicationFacade> sw360ApiCommMock = new Mock<ISW360ApicommunicationFacade>();

            sw360ApiCommMock.Setup(x => x.CreateRelease(It.IsAny<Releases>())).ReturnsAsync(httpResponseMessage);
            sw360ApiCommMock.Setup(x => x.AttachComponentSourceToSW360(It.IsAny<AttachReport>())).Returns(string.Empty);

            // Act
            var sw360CreatorService = new Sw360CreatorService(sw360ApiCommMock.Object);
            var expected = await sw360CreatorService.CreateReleaseForComponent(ComparisonBomData, componentId, attachmentUrlList);

            //Assert
            Assert.That(expected.IsCreated, Is.True);
        }

        public static async Task Test_CreateReleaseForComponent_BadRequest()
        {
            //Arrange
            var ComparisonBomData = new ComparisonBomData()
            {
                Name = "Test"
            };
            var componentId = "1";
            var attachmentUrlList = new Dictionary<string, string>
            {
                { "Test", "Test" }
            };
            var httpResponseMessage = new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest);
            Mock<ISW360ApicommunicationFacade> sw360ApiCommMock = new Mock<ISW360ApicommunicationFacade>();
            sw360ApiCommMock.Setup(x => x.CreateRelease(It.IsAny<Releases>())).ReturnsAsync(httpResponseMessage);
            // Act
            var sw360CreatorService = new Sw360CreatorService(sw360ApiCommMock.Object);
            var expected = await sw360CreatorService.CreateReleaseForComponent(ComparisonBomData, componentId, attachmentUrlList);

            //Assert
            Assert.That(expected.IsCreated, Is.False);
        }

        [Test]
        public async Task CreateReleaseForComponent_ForGivenData_REturnsCreatedStatusFalsOnEXception()
        {
            //Arrange
            var ComparisonBomData = new ComparisonBomData()
            {
                Name = "Test"
            };
            var componentId = "1";
            var attachmentUrlList = new Dictionary<string, string>
            {
                { "Test", "Test" }
            };
            Mock<ISW360ApicommunicationFacade> sw360ApiCommMock = new Mock<ISW360ApicommunicationFacade>();
            sw360ApiCommMock.Setup(x => x.CreateRelease(It.IsAny<Releases>())).Throws<HttpRequestException>();

            // Act
            var sw360CreatorService = new Sw360CreatorService(sw360ApiCommMock.Object);
            var expected = await sw360CreatorService.CreateReleaseForComponent(ComparisonBomData, componentId, attachmentUrlList);

            //Assert
            Assert.That(expected.IsCreated, Is.False);
        }

        [TestCase]
        public async Task Test_CreateReleaseForComponent_Conflict()
        {
            //Arrange
            var ComparisonBomData = new ComparisonBomData()
            {
                Name = "Test",
                Version = "1",

            };
            var componentId = "1";
            var attachmentUrlList = new Dictionary<string, string>
            {
                { "fossology url", UTParams.FossologyURL }
            };
            ReleaseIdOfComponent releaseIdOfComponent = new ReleaseIdOfComponent()
            {
                Name = "Test",
                Embedded = new ReleaseEmbedded()
                {
                    Sw360Releases = new List<Sw360Releases>
                   {
                       new Sw360Releases
                       {
                       Name="Test",
                       Version="1",
                       Links=new Links
                       {
                           Self=new Self
                           {
                               Href="/1"
                           }
                       }
                       }
                   }
                }
            };


            var resBody = JsonConvert.SerializeObject(releaseIdOfComponent);
            Mock<ISW360ApicommunicationFacade> sw360ApiCommMock = new Mock<ISW360ApicommunicationFacade>();
            sw360ApiCommMock.Setup(x => x.GetReleaseByCompoenentName(It.IsAny<string>())).ReturnsAsync(resBody);
            sw360ApiCommMock.Setup(x => x.CreateRelease(It.IsAny<Releases>())).ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Conflict));
            // Act
            var sw360CreatorService = new Sw360CreatorService(sw360ApiCommMock.Object);
            var actual = await sw360CreatorService.CreateReleaseForComponent(ComparisonBomData, componentId, attachmentUrlList);

            //Assert
            Assert.AreEqual(true, actual.IsCreated);

        }

        [Test]
        public async Task GetComponentId_ForGivenNameVersion_ReturnsReleaseIdNullOnException()
        {
            // Arrange
            Mock<ISW360ApicommunicationFacade> sw360ApiCommMock = new Mock<ISW360ApicommunicationFacade>();
            sw360ApiCommMock.Setup(x => x.GetComponentByName(It.IsAny<string>())).Throws<HttpRequestException>();

            // Act
            ISw360CreatorService sw360CreatorService = new Sw360CreatorService(sw360ApiCommMock.Object);
            var actual = await sw360CreatorService.GetComponentId("@angular/animations");

            // Assert
            Assert.That(actual, Is.EqualTo(string.Empty));
        }

        [Test]
        public async Task GetComponentId_ForGivenNameVersion_ReturnsReleaseIdNullOnAggregateException()
        {
            // Arrange
            Mock<ISW360ApicommunicationFacade> sw360ApiCommMock = new Mock<ISW360ApicommunicationFacade>();
            sw360ApiCommMock.Setup(x => x.GetComponentByName(It.IsAny<string>())).Throws<AggregateException>();

            // Act
            ISw360CreatorService sw360CreatorService = new Sw360CreatorService(sw360ApiCommMock.Object);
            var actual = await sw360CreatorService.GetComponentId("@angular/animations");

            // Assert
            Assert.That(actual, Is.EqualTo(string.Empty));
        }

        [Test]
        public async Task GetComponentId_ForGivenNameVersion_ReturnsReleaseId()
        {
            // Arrange

            Self self = new();
            Links links = new();
            Sw360Components sw360Components = new();
            var componentList = new List<Sw360Components>();
            ComponentEmbedded componentEmbedded = new();

            self.Href = "http://localhost:8090/resource/api/components/uiweriwfoowefih87398r3ur093u0";
            links.Self = self;
            sw360Components.Name = "Zone.js";
            sw360Components.Links = links;
            sw360Components.ExternalIds = new ExternalIds() { Package_Url = "pkg:npm/zone.js" };
            componentList.Add(sw360Components);
            componentEmbedded.Sw360components = componentList;

            ComponentsModel componentsModel = new ComponentsModel();
            componentsModel.Embedded = componentEmbedded;

            var componentsModelSerialized = JsonConvert.SerializeObject(componentsModel);

            Mock<ISW360ApicommunicationFacade> sw360ApiCommMock = new Mock<ISW360ApicommunicationFacade>();
            sw360ApiCommMock.Setup(x => x.GetComponentByName(It.IsAny<string>())).ReturnsAsync(componentsModelSerialized);

            // Act
            ISw360CreatorService sw360CreatorService = new Sw360CreatorService(sw360ApiCommMock.Object);
            var actual = await sw360CreatorService.GetComponentId("Zone.js");

            // Assert
            Assert.That(actual, Is.EqualTo("uiweriwfoowefih87398r3ur093u0"));
        }

        [Test]
        public async Task GetReleaseInfo_PassReleaseId_ThrowsHttpRequestExceptionOnServerError()
        {
            // Arrange
            Mock<ISW360ApicommunicationFacade> sw360ApiCommMock = new Mock<ISW360ApicommunicationFacade>();
            sw360ApiCommMock.Setup(x => x.GetReleaseById(It.IsAny<string>())).Throws<HttpRequestException>();

            // Act
            var sw360CreatorService = new Sw360CreatorService(sw360ApiCommMock.Object);
            var actual = await sw360CreatorService.GetReleaseInfo("euryieryeirtoeriutoiertoeritu");

            // Assert
            Assert.That(actual, Is.Null);
        }

        [Test]
        public async Task GetReleaseInfo_PassReleaseId_ThrowsAggregateExceptionOnServerError()
        {
            // Arrange
            Mock<ISW360ApicommunicationFacade> sw360ApiCommMock = new Mock<ISW360ApicommunicationFacade>();
            sw360ApiCommMock.Setup(x => x.GetReleaseById(It.IsAny<string>())).Throws<AggregateException>();

            // Act
            var sw360CreatorService = new Sw360CreatorService(sw360ApiCommMock.Object);
            var actual = await sw360CreatorService.GetReleaseInfo("euryieryeirtoeriutoiertoeritu");

            // Assert
            Assert.That(actual, Is.Null);
        }

        [Test]
        public async Task GetReleaseIdByName_ForGivenNameVersion_ReturnsReleaseIdNullOnException()
        {
            // Arrange
            Mock<ISW360ApicommunicationFacade> sw360ApiCommMock = new Mock<ISW360ApicommunicationFacade>();
            sw360ApiCommMock.Setup(x => x.GetReleaseByCompoenentName(It.IsAny<string>())).Throws<HttpRequestException>();

            // Act
            var sw360CreatorService = new Sw360CreatorService(sw360ApiCommMock.Object);
            var actual = await sw360CreatorService.GetReleaseIdByName("@angular/animations", "1.10.20");

            // Assert
            Assert.That(actual, Is.EqualTo(string.Empty));
        }

        [Test]
        public async Task GetReleaseIDofComponent_ForGivenNameVersion_ReturnsReleaseIdNullOnException()
        {
            // Arrange
            Mock<ISW360ApicommunicationFacade> sw360ApiCommMock = new Mock<ISW360ApicommunicationFacade>();
            sw360ApiCommMock.Setup(x => x.GetReleaseOfComponentById(It.IsAny<string>())).Throws<HttpRequestException>();

            // Act
            var sw360CreatorService = new Sw360CreatorService(sw360ApiCommMock.Object);
            var actual = await sw360CreatorService.GetReleaseIDofComponent("@angular/animations", "1.10.20", "ueiwrowieewrrowie8393");

            // Assert
            Assert.That(actual, Is.Empty);
        }

        [Test]
        public async Task GetReleaseIDofComponent_ForGivenNameVersion_ReturnsReleaseId()
        {
            // Arrange
            Self self = new Self() { Href = "http://localhost:8090/releases/eiurowieuowereerwe88384" };
            Links links = new Links() { Self = self };
            List<Sw360Releases> sw360Releases = new List<Sw360Releases>
            {
                new Sw360Releases() { Name = "@angular/animations", Version = "1.10.20", Links = links }
            };
            ReleaseEmbedded releaseEmbedded = new ReleaseEmbedded
            {
                Sw360Releases = sw360Releases
            };
            ReleaseIdOfComponent releaseIdOfComponent = new ReleaseIdOfComponent
            {
                Name = "@angular/animations",
                Embedded = releaseEmbedded
            };

            string releaseResponse = JsonConvert.SerializeObject(releaseIdOfComponent);

            Mock<ISW360ApicommunicationFacade> sw360ApiCommMock = new Mock<ISW360ApicommunicationFacade>();
            sw360ApiCommMock.Setup(x => x.GetReleaseOfComponentById(It.IsAny<string>())).ReturnsAsync(releaseResponse);

            // Act
            var sw360CreatorService = new Sw360CreatorService(sw360ApiCommMock.Object);
            var actual = await sw360CreatorService.GetReleaseIDofComponent("@angular/animations", "1.10.20", "ueiwrowieewrrowie8393");

            // Assert
            Assert.That(actual, Is.EqualTo("eiurowieuowereerwe88384"));
        }

        [Test]
        public async Task UpdatePurlIdForExistingRelease_PassComponentId_ReturnsFalseOnHttpRequestException()
        {
            // Arrange
            Mock<ISW360ApicommunicationFacade> sw360ApiCommMock = new Mock<ISW360ApicommunicationFacade>();
            sw360ApiCommMock.Setup(x => x.UpdateRelease(It.IsAny<string>(), It.IsAny<HttpContent>())).Throws<HttpRequestException>();
            ComparisonBomData comparisonBomData = new ComparisonBomData
            {
                ReleaseExternalId = "pkg:npm/%40angular/common"
            };
            ReleasesInfo releasesInfo = new ReleasesInfo
            {
                ExternalIds = new Dictionary<string, string>()
            };

            // Act
            var sw360CreatorService = new Sw360CreatorService(sw360ApiCommMock.Object);
            var actual = await sw360CreatorService.UpdatePurlIdForExistingRelease(comparisonBomData, "kjsdiwejjefojffwoje", releasesInfo);

            // Assert
            Assert.That(actual, Is.False);
        }

        [Test]
        public async Task UpdatePurlIdForExistingRelease_PassComponentId_ReturnsTrueOnSuccessfullUpdate()
        {
            // Arrange
            HttpResponseMessage httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);
            Mock<ISW360ApicommunicationFacade> sw360ApiCommMock = new Mock<ISW360ApicommunicationFacade>();
            sw360ApiCommMock.Setup(x => x.UpdateRelease(It.IsAny<string>(), It.IsAny<HttpContent>())).ReturnsAsync(httpResponseMessage);
            ComparisonBomData comparisonBomData = new ComparisonBomData
            {
                ReleaseExternalId = "pkg:npm/%40angular/common/10.0.2"
            };
            ReleasesInfo releasesInfo = new ReleasesInfo
            {
                ExternalIds = new Dictionary<string, string>()
            };

            // Act
            var sw360CreatorService = new Sw360CreatorService(sw360ApiCommMock.Object);
            var actual = await sw360CreatorService.UpdatePurlIdForExistingRelease(comparisonBomData, "kjsdiwejjefojffwoje", releasesInfo);

            // Assert
            Assert.That(actual, Is.True);
        }
        [Test]
        public async Task UpdateMultiplePurlIdForExistingRelease_PassComponentId_ReturnsTrueOnSuccessfulUpdate()
        {
            // Arrange
            HttpResponseMessage httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);
            Mock<ISW360ApicommunicationFacade> sw360ApiCommMock = new Mock<ISW360ApicommunicationFacade>();
            sw360ApiCommMock.Setup(x => x.UpdateRelease(It.IsAny<string>(), It.IsAny<HttpContent>())).ReturnsAsync(httpResponseMessage);
            ComparisonBomData comparisonBomData = new ComparisonBomData
            {
                ReleaseExternalId = "pkg:npm/%40angular/common/10.0.2,pkg:npm/%40angular/common/10.0.3"
            };
            ReleasesInfo releasesInfo = new ReleasesInfo
            {
                ExternalIds = new Dictionary<string, string>()
            };

            // Act
            var sw360CreatorService = new Sw360CreatorService(sw360ApiCommMock.Object);
            var actual = await sw360CreatorService.UpdatePurlIdForExistingRelease(comparisonBomData, "kjsdiwejjefojffwoje", releasesInfo);

            // Assert
            Assert.That(actual, Is.True);
        }

        [Test]
        public async Task UpdatePurlIdForExistingComponent_PassComponentId_ReturnsFalseOnHttpRequestException()
        {
            // Arrange
            Mock<ISW360ApicommunicationFacade> sw360ApiCommMock = new Mock<ISW360ApicommunicationFacade>();
            sw360ApiCommMock.Setup(x => x.GetReleaseOfComponentById(It.IsAny<string>())).Throws<HttpRequestException>();
            ComparisonBomData comparisonBomData = new ComparisonBomData
            {
                ComponentExternalId = "pkg:npm/%40angular/common"
            };

            // Act
            var sw360CreatorService = new Sw360CreatorService(sw360ApiCommMock.Object);
            var actual = await sw360CreatorService.UpdatePurlIdForExistingComponent(comparisonBomData, "kjsdiwejjefojffwoje");

            // Assert
            Assert.That(actual, Is.False);
        }

        [Test]
        public async Task UpdatePurlIdForExistingComponent_PassComponentId_ReturnsFalseOnAggregateException()
        {
            // Arrange
            Mock<ISW360ApicommunicationFacade> sw360ApiCommMock = new Mock<ISW360ApicommunicationFacade>();
            sw360ApiCommMock.Setup(x => x.GetReleaseOfComponentById(It.IsAny<string>())).Throws<AggregateException>();
            ComparisonBomData comparisonBomData = new ComparisonBomData
            {
                ComponentExternalId = "pkg:npm/%40angular/common"
            };

            // Act
            var sw360CreatorService = new Sw360CreatorService(sw360ApiCommMock.Object);
            var actual = await sw360CreatorService.UpdatePurlIdForExistingComponent(comparisonBomData, "kjsdiwejjefojffwoje");

            // Assert
            Assert.That(actual, Is.False);
        }

        [Test]
        public async Task UpdatePurlIdForExistingComponent_PassComponentId_ReturnsTrueOnSuccesssfulupdate()
        {
            // Arrange
            ComponentPurlId componentPurlId = new ComponentPurlId
            {
                ExternalIds = new Dictionary<string, string>() { { "test", "dssds" } }
            };
            string externalIDResponse = JsonConvert.SerializeObject(componentPurlId);
            HttpResponseMessage httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);
            Mock<ISW360ApicommunicationFacade> sw360ApiCommMock = new Mock<ISW360ApicommunicationFacade>();
            sw360ApiCommMock.Setup(x => x.GetReleaseOfComponentById(It.IsAny<string>())).ReturnsAsync(externalIDResponse);
            sw360ApiCommMock.Setup(x => x.UpdateComponent(It.IsAny<string>(), It.IsAny<HttpContent>())).ReturnsAsync(httpResponseMessage);
            ComparisonBomData comparisonBomData = new ComparisonBomData
            {
                ComponentExternalId = "pkg:npm/%40angular/common"
            };

            // Act
            var sw360CreatorService = new Sw360CreatorService(sw360ApiCommMock.Object);
            var actual = await sw360CreatorService.UpdatePurlIdForExistingComponent(comparisonBomData, "kjsdiwejjefojffwoje");

            // Assert
            Assert.That(actual, Is.True);
        }

        [Test]
        public async Task UpdatePurlIdForExistingComponent_PassPackageurl_ReturnsTrueOnSuccesssfulupdate()
        {
            // Arrange
            ComponentPurlId componentPurlId = new ComponentPurlId
            {
                ExternalIds = new Dictionary<string, string>() { { "package-url", "dssds" } }
            };
            string externalIDResponse = JsonConvert.SerializeObject(componentPurlId);
            HttpResponseMessage httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);
            Mock<ISW360ApicommunicationFacade> sw360ApiCommMock = new Mock<ISW360ApicommunicationFacade>();
            sw360ApiCommMock.Setup(x => x.GetReleaseOfComponentById(It.IsAny<string>())).ReturnsAsync(externalIDResponse);
            sw360ApiCommMock.Setup(x => x.UpdateComponent(It.IsAny<string>(), It.IsAny<HttpContent>())).ReturnsAsync(httpResponseMessage);
            ComparisonBomData comparisonBomData = new ComparisonBomData
            {
                ComponentExternalId = "pkg:npm/%40angular/common"
            };

            // Act
            var sw360CreatorService = new Sw360CreatorService(sw360ApiCommMock.Object);
            var actual = await sw360CreatorService.UpdatePurlIdForExistingComponent(comparisonBomData, "kjsdiwejjefojffwoje");

            // Assert
            Assert.That(actual, Is.True);
        }

        [Test]
        public async Task UpdatePurlIdForExistingRelease_PassPackageurl_ReturnsTrueOnSuccesssfulupdate()
        {
            // Arrange
            HttpResponseMessage httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);
            Mock<ISW360ApicommunicationFacade> sw360ApiCommMock = new Mock<ISW360ApicommunicationFacade>();
            sw360ApiCommMock.Setup(x => x.UpdateRelease(It.IsAny<string>(), It.IsAny<HttpContent>())).ReturnsAsync(httpResponseMessage);
            ComparisonBomData comparisonBomData = new ComparisonBomData
            {
                ReleaseExternalId = "pkg:npm/%40angular/common@10.0.2"
            };
            ReleasesInfo releasesInfo = new ReleasesInfo
            {
                ExternalIds = new Dictionary<string, string>() { { "package-url", "dssds" } }
            };

            // Act
            var sw360CreatorService = new Sw360CreatorService(sw360ApiCommMock.Object);
            var actual = await sw360CreatorService.UpdatePurlIdForExistingRelease(comparisonBomData, "kjsdiwejjefojffwoje", releasesInfo);

            // Assert
            Assert.That(actual, Is.True);
        }

        [Test]
        public async Task UpdateMultiplePurlIdForExistingComponent_PassComponentId_ReturnsTrueOnSuccesssfulupdate()
        {
            // Arrange
            ComponentPurlId componentPurlId = new ComponentPurlId
            {
                ExternalIds = new Dictionary<string, string>() { { "test", "dssds" } }
            };
            string externalIDResponse = JsonConvert.SerializeObject(componentPurlId);
            HttpResponseMessage httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);
            Mock<ISW360ApicommunicationFacade> sw360ApiCommMock = new Mock<ISW360ApicommunicationFacade>();
            sw360ApiCommMock.Setup(x => x.GetReleaseOfComponentById(It.IsAny<string>())).ReturnsAsync(externalIDResponse);
            sw360ApiCommMock.Setup(x => x.UpdateComponent(It.IsAny<string>(), It.IsAny<HttpContent>())).ReturnsAsync(httpResponseMessage);
            ComparisonBomData comparisonBomData = new ComparisonBomData
            {
                ComponentExternalId = "pkg:npm/%40angular/common,pkg:npm/Testname"
            };

            // Act
            var sw360CreatorService = new Sw360CreatorService(sw360ApiCommMock.Object);
            var actual = await sw360CreatorService.UpdatePurlIdForExistingComponent(comparisonBomData, "kjsdiwejjefojffwoje");

            // Assert
            Assert.That(actual, Is.True);
        }

        [Test]
        public async Task GetReleaseByExternalId_PassReleaseInfo_SuccessfullyReturnsReleaseId()
        {
            // Arrange

            Self self = new();
            Links links = new();
            Sw360Releases sw360Releases = new();

            self.Href = "http://localhost:8090/resource/api/releases/uiweriwfoowefih87398r3ur093u0";
            links.Self = self;
            sw360Releases.Name = "Zone.js";
            sw360Releases.Version = "1.0.0";
            sw360Releases.Links = links;
            sw360Releases.ExternalIds = new ExternalIds() { Package_Url = "pkg:npm/zone.js@1.0.0" };
            Releasestatus releasestatus = new Releasestatus();
            releasestatus.sw360Releases = sw360Releases;

            Mock<ISW360ApicommunicationFacade> sw360ApiCommMock = new Mock<ISW360ApicommunicationFacade>();

            Mock<ISW360CommonService> sw360CommonServiceMock = new Mock<ISW360CommonService>();
            sw360CommonServiceMock
                .Setup(x => x.GetReleaseDataByExternalId(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(releasestatus);

            // Act
            var sw360CreatorService = new Sw360CreatorService(sw360ApiCommMock.Object, sw360CommonServiceMock.Object);
            var actual = await sw360CreatorService.GetReleaseByExternalId("Zone.js", "1.0.0", "pkg:npm/zone.js@1.0.0");


            // Assert
            Assert.That(actual, Is.Not.Null);
        }



        [Test]
        public async Task GetComponentIdUsingExternalId_PassComponentInfo_SuccessfullyReturnsComponentId()
        {
            // Arrange

            Self self = new();
            Links links = new();
            Sw360Components sw360Components = new();

            self.Href = "http://localhost:8090/resource/api/releases/uiweriwfoowefih87398r3ur093u0";
            links.Self = self;
            sw360Components.Name = "Zone.js";
            sw360Components.Links = links;
            sw360Components.ExternalIds = new ExternalIds() { Package_Url = "pkg:npm/zone.js@1.0.0" };
            ComponentStatus componentStatus = new ComponentStatus();
            componentStatus.Sw360components = sw360Components;

            Mock<ISW360ApicommunicationFacade> sw360ApiCommMock = new Mock<ISW360ApicommunicationFacade>();

            Mock<ISW360CommonService> sw360CommonServiceMock = new Mock<ISW360CommonService>();
            sw360CommonServiceMock
                .Setup(x => x.GetComponentDataByExternalId(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(componentStatus);

            // Act
            var sw360CreatorService = new Sw360CreatorService(sw360ApiCommMock.Object, sw360CommonServiceMock.Object);
            var actual = await sw360CreatorService.GetComponentIdUsingExternalId("Zone.js", "pkg:npm/zone.js@1.0.0");


            // Assert
            Assert.That(actual, Is.Not.Null);
        }

        [Test]
        public async Task UdpateSW360ReleaseContent_ForGivenComponentInfo_ThorwsHttpRequestExceptionOnError()
        {
            // Arrange
            UpdateReleaseAdditinoalData updateRelease = new UpdateReleaseAdditinoalData();
            var content = new StringContent(
                         JsonConvert.SerializeObject(updateRelease),
                         Encoding.UTF8,
                         ApiConstant.ApplicationJson);
            Components component = new Components() { Name = "Zone.js", Version = "1.0.0" };
            Mock<ISW360ApicommunicationFacade> sw360ApiCommMock = new Mock<ISW360ApicommunicationFacade>();
            sw360ApiCommMock.Setup(x => x.GetReleaseById(It.IsAny<string>())).Throws<HttpRequestException>();
            sw360ApiCommMock.Setup(x => x.UpdateRelease(It.IsAny<string>(), It.IsAny<HttpContent>())).Throws<HttpRequestException>();

            // Act
            var sw360CreatorService = new Sw360CreatorService(sw360ApiCommMock.Object);
            var actual = await sw360CreatorService.UdpateSW360ReleaseContent(component, "foss url");

            // Assert
            Assert.That(actual, Is.False);
        }

        [Test]
        public async Task UdpateSW360ReleaseContent_ForGivenComponentInfo_ThorwsAggregateExceptionOnError()
        {
            // Arrange
            UpdateReleaseAdditinoalData updateRelease = new UpdateReleaseAdditinoalData();
            var content = new StringContent(
                         JsonConvert.SerializeObject(updateRelease),
                         Encoding.UTF8,
                         ApiConstant.ApplicationJson);
            Components component = new Components() { Name = "Zone.js", Version = "1.0.0" };
            Mock<ISW360ApicommunicationFacade> sw360ApiCommMock = new Mock<ISW360ApicommunicationFacade>();
            sw360ApiCommMock.Setup(x => x.GetReleaseById(It.IsAny<string>())).Throws<AggregateException>();
            sw360ApiCommMock.Setup(x => x.UpdateRelease(It.IsAny<string>(), It.IsAny<HttpContent>())).Throws<AggregateException>();

            // Act
            var sw360CreatorService = new Sw360CreatorService(sw360ApiCommMock.Object);
            var actual = await sw360CreatorService.UdpateSW360ReleaseContent(component, "foss url");

            // Assert
            Assert.That(actual, Is.False);
        }
       [Test]
        public async Task CreateComponentBasesOFswComaprisonBOM_HttpRequestException_ReturnsFalse()
        {
            // Arrange
            var componentInfo = new ComparisonBomData
            {
                Name = "TestComponent",
                Version = "1.0",
                ComponentExternalId = "pkg:npm/%40angular/common"
            };
            var attachmentUrlList = new Dictionary<string, string>();

            var mockApiFacade = new Mock<ISW360ApicommunicationFacade>();
            mockApiFacade.Setup(x => x.CreateComponent(It.IsAny<CreateComponent>())).ThrowsAsync(new HttpRequestException());

            var sw360CreatorService = new Sw360CreatorService(mockApiFacade.Object);

            // Act
            var result = await sw360CreatorService.CreateComponentBasesOFswComaprisonBOM(componentInfo, attachmentUrlList);

            // Assert
            Assert.That(result.IsCreated, Is.False);
            Assert.That(result.ReleaseStatus.IsCreated, Is.False);
        }
       [Test]
        public async Task CheckFossologyProcessStatus_JsonReaderException_ReturnsNull()
        {
            // Arrange
            string link = "http://example.com/fossology/process/status";
            var mockApiFacade = new Mock<ISW360ApicommunicationFacade>();

            var invalidJsonResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("invalid json")
            };

            mockApiFacade.Setup(x => x.CheckFossologyProcessStatus(It.IsAny<string>())).ReturnsAsync(invalidJsonResponse);

            var sw360CreatorService = new Sw360CreatorService(mockApiFacade.Object);

            // Act
            var result = await sw360CreatorService.CheckFossologyProcessStatus(link);

            // Assert
            Assert.IsNull(result);
        }

        [Test]
        public async Task CheckFossologyProcessStatus_HttpRequestException_ReturnsNull()
        {
            // Arrange
            string link = "http://example.com/fossology/process/status";
            var mockApiFacade = new Mock<ISW360ApicommunicationFacade>();

            mockApiFacade.Setup(x => x.CheckFossologyProcessStatus(It.IsAny<string>()))
                         .ThrowsAsync(new HttpRequestException("Request failed"));

            var sw360CreatorService = new Sw360CreatorService(mockApiFacade.Object);

            // Act
            var result = await sw360CreatorService.CheckFossologyProcessStatus(link);

            // Assert
            Assert.IsNull(result);
        }
        

        [Test]
        public async Task GetReleaseIDofComponent_HttpRequestException_ReturnsEmptyString()
        {
            // Arrange
            string componentName = "TestComponent";
            string componentVersion = "1.0";
            string componentId = "componentId123";

            var mockApiFacade = new Mock<ISW360ApicommunicationFacade>();

            mockApiFacade.Setup(x => x.GetReleaseOfComponentById(It.IsAny<string>()))
                         .ThrowsAsync(new HttpRequestException("Request failed"));

            var sw360CreatorService = new Sw360CreatorService(mockApiFacade.Object);

            // Act
            var result = await sw360CreatorService.GetReleaseIDofComponent(componentName, componentVersion, componentId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(string.Empty, result);
        }

        [Test]
        public async Task GetReleaseIDofComponent_ReleaseIdNotFound_ReturnsEmptyString()
        {
            // Arrange
            string componentName = "TestComponent";
            string componentVersion = "1.0";
            string componentId = "componentId123";

            var mockApiFacade = new Mock<ISW360ApicommunicationFacade>();

            var releaseResponseBody = JsonConvert.SerializeObject(new
            {
                // Simulated JSON response structure without matching release
                releases = new[]
                {
                new { name = "OtherComponent", version = "2.0", id = "otherReleaseId" }
            }
            });

            mockApiFacade.Setup(x => x.GetReleaseOfComponentById(It.IsAny<string>())).ReturnsAsync(releaseResponseBody);

            var sw360CreatorService = new Sw360CreatorService(mockApiFacade.Object);

            // Act
            var result = await sw360CreatorService.GetReleaseIDofComponent(componentName, componentVersion, componentId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(string.Empty, result);
        }
        

        [Test]
        public async Task GetReleaseIdByName_HttpRequestException_ReturnsEmptyString()
        {
            // Arrange
            string componentName = "TestComponent";
            string componentVersion = "1.0";

            var mockApiFacade = new Mock<ISW360ApicommunicationFacade>();

            mockApiFacade.Setup(x => x.GetReleaseByCompoenentName(It.IsAny<string>()))
                         .ThrowsAsync(new HttpRequestException("Request failed"));

            var sw360CreatorService = new Sw360CreatorService(mockApiFacade.Object);

            // Act
            var result = await sw360CreatorService.GetReleaseIdByName(componentName, componentVersion);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(string.Empty, result);
        }

        [Test]
        public async Task GetReleaseIdByName_ReleaseIdNotFound_ReturnsEmptyString()
        {
            // Arrange
            string componentName = "TestComponent";
            string componentVersion = "1.0";

            var mockApiFacade = new Mock<ISW360ApicommunicationFacade>();

            var responseBody = JsonConvert.SerializeObject(new
            {
                // Simulated JSON response structure without matching release
                releases = new[]
                {
                new { name = "OtherComponent", version = "2.0", id = "otherReleaseId" }
            }
            });

            mockApiFacade.Setup(x => x.GetReleaseByCompoenentName(It.IsAny<string>())).ReturnsAsync(responseBody);

            var sw360CreatorService = new Sw360CreatorService(mockApiFacade.Object);

            // Act
            var result = await sw360CreatorService.GetReleaseIdByName(componentName, componentVersion);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(string.Empty, result);
        }
        [Test]
        public async Task GetComponentId_HttpRequestException_ReturnsEmptyString()
        {
            // Arrange
            string componentName = "TestComponent";

            var mockApiFacade = new Mock<ISW360ApicommunicationFacade>();

            mockApiFacade.Setup(x => x.GetComponentByName(It.IsAny<string>()))
                         .ThrowsAsync(new HttpRequestException("Request failed"));

            var sw360CreatorService = new Sw360CreatorService(mockApiFacade.Object);

            // Act
            var result = await sw360CreatorService.GetComponentId(componentName);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(string.Empty, result);
        }
        [Test]
        public async Task GetComponentId_AggregateException_ReturnsEmptyString()
        {
            // Arrange
            string componentName = "TestComponent";

            var mockApiFacade = new Mock<ISW360ApicommunicationFacade>();

            mockApiFacade.Setup(x => x.GetComponentByName(It.IsAny<string>()))
                         .ThrowsAsync(new AggregateException("Request failed"));

            var sw360CreatorService = new Sw360CreatorService(mockApiFacade.Object);

            // Act
            var result = await sw360CreatorService.GetComponentId(componentName);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(string.Empty, result);
        }

        
    }
}



