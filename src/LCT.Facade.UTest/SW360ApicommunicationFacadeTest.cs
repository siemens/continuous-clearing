// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.APICommunications.Interfaces;
using LCT.APICommunications.Model;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace LCT.Facade.UTest
{
    [TestFixture]
    public class SW360ApicommunicationFacadeTest
    {
        SW360ApicommunicationFacade sW360ApicommunicationFacade;

        [SetUp]
        public void Setup()
        {
            // to be implemented
        }

        [Test]
        public async Task GetProjects_OnSuccess_RetursProjectInAString()
        {
            //Arange 
            Mock<ISw360ApiCommunication> mockSw360comm = new Mock<ISw360ApiCommunication>();
            mockSw360comm.Setup(x => x.GetProjects()).ReturnsAsync("TestProject");

            //Act          
            sW360ApicommunicationFacade = new SW360ApicommunicationFacade(mockSw360comm.Object);
            string actual = await sW360ApicommunicationFacade.GetProjects();

            //Assert
            Assert.That(actual, Is.EqualTo("TestProject"));
        }

        [Test]
        public async Task GetSw360Users_OnSuccess_RetursUsersInfoInAString()
        {
            //Arange 

            Mock<ISw360ApiCommunication> mockSw360comm = new Mock<ISw360ApiCommunication>();
            mockSw360comm.Setup(x => x.GetSw360Users()).ReturnsAsync("TestUser");

            //Act          
            sW360ApicommunicationFacade = new SW360ApicommunicationFacade(mockSw360comm.Object);
            string actual = await sW360ApicommunicationFacade.GetSw360Users();

            //Assert
            Assert.That(actual, Is.EqualTo("TestUser"));
        }

        [Test]
        public async Task GetProjectsByName_OnSuccess_RetursProjectnfoInAString()
        {
            //Arange 
            Mock<ISw360ApiCommunication> mockSw360comm = new Mock<ISw360ApiCommunication>();
            mockSw360comm.Setup(x => x.GetProjectsByName(It.IsAny<string>())).ReturnsAsync("ProjectInfo");

            //Act          
            sW360ApicommunicationFacade = new SW360ApicommunicationFacade(mockSw360comm.Object);
            string actual = await sW360ApicommunicationFacade.GetProjectsByName("Test");

            //Assert
            Assert.That(actual, Is.EqualTo("ProjectInfo"));
        }

        [Test]
        public async Task GetProjectById_ForGivenProjectId_RetursProjectnfoInAString()
        {
            //Arange 
            HttpResponseMessage httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);
            Mock<ISw360ApiCommunication> mockSw360comm = new Mock<ISw360ApiCommunication>();
            mockSw360comm.Setup(x => x.GetProjectById(It.IsAny<string>())).ReturnsAsync(httpResponseMessage);

            //Act          
            sW360ApicommunicationFacade = new SW360ApicommunicationFacade(mockSw360comm.Object);
            HttpResponseMessage actual = await sW360ApicommunicationFacade.GetProjectById("ewehieiwriosdjkhdjksdfd");

            //Assert
            Assert.That(actual.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Test]
        public async Task GetReleases_OnSuccess_ReturnsReleaseInfoInAString()
        {
            //Arange 
            Mock<ISw360ApiCommunication> mockSw360comm = new Mock<ISw360ApiCommunication>();
            mockSw360comm.Setup(x => x.GetReleases()).ReturnsAsync("Zone.js_v1.0.0");

            //Act          
            sW360ApicommunicationFacade = new SW360ApicommunicationFacade(mockSw360comm.Object);
            string actual = await sW360ApicommunicationFacade.GetReleases();

            //Assert
            Assert.That(actual, Is.EqualTo("Zone.js_v1.0.0"));
        }

        [Test]
        public async Task GetReleaseById_OnSuccess_ReturnsReleaseInfoInAString()
        {
            //Arange 
            HttpResponseMessage httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);
            Mock<ISw360ApiCommunication> mockSw360comm = new Mock<ISw360ApiCommunication>();
            mockSw360comm.Setup(x => x.GetReleaseById(It.IsAny<string>())).ReturnsAsync(httpResponseMessage);

            //Act          
            sW360ApicommunicationFacade = new SW360ApicommunicationFacade(mockSw360comm.Object);
            HttpResponseMessage actual = await sW360ApicommunicationFacade.GetReleaseById("8454858hjfjkdshldsfhiruewi");

            //Assert
            Assert.That(actual.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Test]
        public async Task GetReleaseByCompoenentName_OnSuccess_ReturnsReleaseInfoInAString()
        {
            //Arange 
            HttpResponseMessage httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);
            Mock<ISw360ApiCommunication> mockSw360comm = new Mock<ISw360ApiCommunication>();
            mockSw360comm.Setup(x => x.GetReleaseByCompoenentName(It.IsAny<string>())).ReturnsAsync("httpResponseMessage");

            //Act          
            sW360ApicommunicationFacade = new SW360ApicommunicationFacade(mockSw360comm.Object);
            string actual = await sW360ApicommunicationFacade.GetReleaseByCompoenentName("8454858hjfjkdshldsfhiruewi");

            //Assert
            Assert.That(actual, Is.EqualTo("httpResponseMessage"));
        }

        [Test]
        public async Task CheckFossologyProcessStatus_OnSuccess_ReturnsHttpOk()
        {
            //Arange 
            HttpResponseMessage httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);
            Mock<ISw360ApiCommunication> mockSw360comm = new Mock<ISw360ApiCommunication>();
            mockSw360comm.Setup(x => x.CheckFossologyProcessStatus(It.IsAny<string>())).ReturnsAsync(httpResponseMessage);

            //Act          
            sW360ApicommunicationFacade = new SW360ApicommunicationFacade(mockSw360comm.Object);
            HttpResponseMessage actual = await sW360ApicommunicationFacade.CheckFossologyProcessStatus(@"http:\\8454858hjfjkdshldsfhiruewi");

            //Assert
            Assert.That(actual.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Test]
        public async Task UpdateComponent_OnSuccess_ReturnsHttpOk()
        {
            //Arange 
            HttpResponseMessage httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);
            Mock<ISw360ApiCommunication> mockSw360comm = new Mock<ISw360ApiCommunication>();
            mockSw360comm.Setup(x => x.UpdateComponent(It.IsAny<string>(), It.IsAny<HttpContent>())).ReturnsAsync(httpResponseMessage);
            StringContent content = new StringContent(JsonConvert.SerializeObject("string content"), Encoding.UTF8, "application/json");
            //Act          
            sW360ApicommunicationFacade = new SW360ApicommunicationFacade(mockSw360comm.Object);
            HttpResponseMessage actual = await sW360ApicommunicationFacade.UpdateComponent(@"http:\\8454858hjfjkdshldsfhiruewi", content);

            //Assert
            Assert.That(actual.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Test]
        public async Task UpdateComponent_OnSuccess_ReturnsHttpOkOnTestModeAsTrue()
        {
            //Arange 
            HttpResponseMessage httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);
            Mock<ISw360ApiCommunication> mockSw360comm = new Mock<ISw360ApiCommunication>();
            mockSw360comm.Setup(x => x.UpdateComponent(It.IsAny<string>(), It.IsAny<HttpContent>())).ReturnsAsync(httpResponseMessage);
            StringContent content = new StringContent(JsonConvert.SerializeObject("string content"), Encoding.UTF8, "application/json");
            //Act          
            sW360ApicommunicationFacade = new SW360ApicommunicationFacade(mockSw360comm.Object);
            HttpResponseMessage actual = await sW360ApicommunicationFacade.UpdateComponent(@"http:\\8454858hjfjkdshldsfhiruewi", content);

            //Assert
            Assert.That(actual.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Test]
        public async Task GetReleaseByLink_ForGivenReleaseLink_ReturnsHttpResponse()
        {
            //Arrange 
            HttpResponseMessage httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);
            Mock<ISw360ApiCommunication> mockSw360comm = new Mock<ISw360ApiCommunication>();
            mockSw360comm.Setup(x => x.GetReleaseByLink(It.IsAny<string>())).ReturnsAsync(httpResponseMessage);

            //Act          
            sW360ApicommunicationFacade = new SW360ApicommunicationFacade(mockSw360comm.Object);
            HttpResponseMessage actual = await sW360ApicommunicationFacade.GetReleaseByLink(
                "http://md2pdvnc:8095/group/guest/components/-/component/release/detailRelease/51b3523a1b038d6b4caadc1ada38667c/51b3523a1b038d6b4caadc1ada46ca38#/tab-Summary");

            //Assert
            Assert.That(actual.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Test]
        public async Task LinkReleasesToProject_OnTestMode_ForGivenReleaseIdArray_ReturnsHttpResponse()
        {
            //Arrange 
            HttpResponseMessage httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);
            Mock<ISw360ApiCommunication> mockSw360comm = new Mock<ISw360ApiCommunication>();
            mockSw360comm.Setup(x => x.LinkReleasesToProject(It.IsAny<HttpContent>(), It.IsAny<string>())).ReturnsAsync(httpResponseMessage);

            //Act          
            sW360ApicommunicationFacade = new SW360ApicommunicationFacade(mockSw360comm.Object);
            StringContent content = new StringContent(JsonConvert.SerializeObject("{ \"12345\" : { \"releaseRelation\" : \"UNKNOWN\", \"comment\" : \"Test Comment 1\" }, \"54321\" : { \"releaseRelation\" : \"UNKNOWN\", \"comment\" : \"Test Comment 2\" } }"), Encoding.UTF8, "application/json");
            HttpResponseMessage actual = await sW360ApicommunicationFacade.LinkReleasesToProject(content, "ieuroejfklsndksdoldmfiosdfowemlfiwe");

            //Assert
            Assert.That(actual.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Test]
        public async Task LinkReleasesToProject_OnNonTestMode_ForGivenReleaseIdArray_ReturnsHttpResponse()
        {
            //Arrange 
            HttpResponseMessage httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);
            Mock<ISw360ApiCommunication> mockSw360comm = new Mock<ISw360ApiCommunication>();
            mockSw360comm.Setup(x => x.LinkReleasesToProject(It.IsAny<HttpContent>(), It.IsAny<string>())).ReturnsAsync(httpResponseMessage);

            //Act          
            sW360ApicommunicationFacade = new SW360ApicommunicationFacade(mockSw360comm.Object);
            StringContent content = new StringContent(JsonConvert.SerializeObject("{ \"12345\" : { \"releaseRelation\" : \"UNKNOWN\", \"comment\" : \"Test Comment 1\" }, \"54321\" : { \"releaseRelation\" : \"UNKNOWN\", \"comment\" : \"Test Comment 2\" } }"), Encoding.UTF8, "application/json");
            HttpResponseMessage actual = await sW360ApicommunicationFacade.LinkReleasesToProject(content, "ieuroejfklsndksdoldmfiosdfowemlfiwe");

            //Assert
            Assert.That(actual.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Test]        
        public async Task CreateComponent_OnNonTestMode_ForGivenReleaseIdArray_ReturnsHttpResponse()
        {
            //Arrange 
            CreateComponent createComponent = new CreateComponent();
            HttpResponseMessage httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);
            Mock<ISw360ApiCommunication> mockSw360comm = new Mock<ISw360ApiCommunication>();
            mockSw360comm.Setup(x => x.CreateComponent(It.IsAny<CreateComponent>())).ReturnsAsync(httpResponseMessage);

            //Act          
            sW360ApicommunicationFacade = new SW360ApicommunicationFacade(mockSw360comm.Object);
            HttpResponseMessage actual = await sW360ApicommunicationFacade.CreateComponent(createComponent);

            //Assert
            Assert.That(actual.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Test]
        public async Task CreateRelease_OnTestMode_ForGivenReleaseIdArray_ReturnsHttpResponse()
        {
            //Arrange 
            Releases releases = new Releases();
            HttpResponseMessage httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);
            Mock<ISw360ApiCommunication> mockSw360comm = new Mock<ISw360ApiCommunication>();
            mockSw360comm.Setup(x => x.CreateRelease(It.IsAny<Releases>())).ReturnsAsync(httpResponseMessage);

            //Act          
            sW360ApicommunicationFacade = new SW360ApicommunicationFacade(mockSw360comm.Object);
            HttpResponseMessage actual = await sW360ApicommunicationFacade.CreateRelease(releases);

            //Assert
            Assert.That(actual.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Test]
        public async Task CreateRelease_OnNonTestMode_ForGivenReleaseIdArray_ReturnsHttpResponse()
        {
            //Arrange 
            Releases releases = new Releases();
            HttpResponseMessage httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);
            Mock<ISw360ApiCommunication> mockSw360comm = new Mock<ISw360ApiCommunication>();
            mockSw360comm.Setup(x => x.CreateRelease(It.IsAny<Releases>())).ReturnsAsync(httpResponseMessage);

            //Act          
            sW360ApicommunicationFacade = new SW360ApicommunicationFacade(mockSw360comm.Object);
            HttpResponseMessage actual = await sW360ApicommunicationFacade.CreateRelease(releases);

            //Assert
            Assert.That(actual.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Test]
        public async Task GetReleaseOfComponentById_ForGivenComponentId_ReturnsReleaseInfoInAstring()
        {
            //Arrange 
            Mock<ISw360ApiCommunication> mockSw360comm = new Mock<ISw360ApiCommunication>();
            mockSw360comm.Setup(x => x.GetReleaseOfComponentById(It.IsAny<string>())).ReturnsAsync("{name:zone.js,version:0.1.1}");

            //Act          
            sW360ApicommunicationFacade = new SW360ApicommunicationFacade(mockSw360comm.Object);
            string actual = await sW360ApicommunicationFacade.GetReleaseOfComponentById("ewifwekfnlskifklsdfklsdijf");

            //Assert
            Assert.That(actual, Is.EqualTo("{name:zone.js,version:0.1.1}"));
        }

        [Test]
        public async Task GetReleaseAttachments_ForGivenReleaseAttachmentUrl_ReturnsReleaseAttachmentInfoInAString()
        {
            //Arrange 

            Mock<ISw360ApiCommunication> mockSw360comm = new Mock<ISw360ApiCommunication>();
            mockSw360comm.Setup(x => x.GetReleaseAttachments(It.IsAny<string>())).ReturnsAsync("{name:zone.js,version:0.1.1,type:Binary}");

            //Act          
            sW360ApicommunicationFacade = new SW360ApicommunicationFacade(mockSw360comm.Object);
            string actual = await sW360ApicommunicationFacade.GetReleaseAttachments("ewifwekfnlskifklsdfklsdijf");

            //Assert
            Assert.That(actual, Is.EqualTo("{name:zone.js,version:0.1.1,type:Binary}"));
        }

        [Test]
        public async Task GetAttachmentInfo_ForGivenReleaseAttachmentUrl_ReturnsReleaseAttachmentInfoInAString()
        {
            //Arrange 
            Mock<ISw360ApiCommunication> mockSw360comm = new Mock<ISw360ApiCommunication>();
            mockSw360comm.Setup(x => x.GetAttachmentInfo(It.IsAny<string>())).ReturnsAsync("{name:zone.js,version:0.1.1,type:Binary}");

            //Act          
            sW360ApicommunicationFacade = new SW360ApicommunicationFacade(mockSw360comm.Object);
            string actual = await sW360ApicommunicationFacade.GetAttachmentInfo("ewifwekfnlskifklsdfklsdijf");

            //Assert
            Assert.That(actual, Is.EqualTo("{name:zone.js,version:0.1.1,type:Binary}"));
        }

        [Test]
        public void DownloadAttachmentUsingWebClient_ForGivenReleaseAttachmentUrl_ReturnsVoid()
        {
            //Arrange 
            Mock<ISw360ApiCommunication> mockSw360comm = new Mock<ISw360ApiCommunication>();
            mockSw360comm.Setup(x => x.DownloadAttachmentUsingWebClient(It.IsAny<string>(), It.IsAny<string>()));

            //Act          
            sW360ApicommunicationFacade = new SW360ApicommunicationFacade(mockSw360comm.Object);
            sW360ApicommunicationFacade.DownloadAttachmentUsingWebClient("", "zone.js_1.0.0_source.zip");

            //Assert
            Assert.That(true);
        }

        [Test]
        public async Task UpdateRelease_OnNonTestMode_ForGivenReleaseId_ReturnsHttpResponse()
        {
            //Arrange 
            string uploadContent = string.Empty;
            HttpContent content = new StringContent(uploadContent, Encoding.UTF8, "application/json");
            HttpResponseMessage httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);
            Mock<ISw360ApiCommunication> mockSw360comm = new Mock<ISw360ApiCommunication>();
            mockSw360comm.Setup(x => x.UpdateRelease(It.IsAny<string>(), It.IsAny<HttpContent>())).ReturnsAsync(httpResponseMessage);

            //Act          
            sW360ApicommunicationFacade = new SW360ApicommunicationFacade(mockSw360comm.Object);
            HttpResponseMessage actual = await sW360ApicommunicationFacade.UpdateRelease("ReleasjndaIdsdjflsd394392io", content);

            //Assert
            Assert.That(actual.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Test]
        public void AttachComponentSourceToSW360_ForGivenAttachReport_ReturnsSuccessString()
        {
            //Arrange 
            AttachReport attachReport = new AttachReport();
            Mock<ISw360ApiCommunication> mockSw360comm = new Mock<ISw360ApiCommunication>();
            mockSw360comm.Setup(x => x.AttachComponentSourceToSW360(It.IsAny<AttachReport>())).Returns("SuccessInfo");

            //Act          
            sW360ApicommunicationFacade = new SW360ApicommunicationFacade(mockSw360comm.Object);
            string actual = sW360ApicommunicationFacade.AttachComponentSourceToSW360(attachReport);

            //Assert
            Assert.That(actual, Is.EqualTo("SuccessInfo"));
        }

        [Test]
        public async Task GetComponentDetailsByUrl_ForGivenComponentLink_ReturnsHttpResponse()
        {
            //Arange 
            HttpResponseMessage httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);
            Mock<ISw360ApiCommunication> mockSw360comm = new Mock<ISw360ApiCommunication>();
            mockSw360comm.Setup(x => x.GetComponentDetailsByUrl(It.IsAny<string>())).ReturnsAsync(httpResponseMessage);

            //Act          
            sW360ApicommunicationFacade = new SW360ApicommunicationFacade(mockSw360comm.Object);
            HttpResponseMessage actual = await sW360ApicommunicationFacade.GetComponentDetailsByUrl("http://localhost:8090/group/guest/components/-/component/detail/df4b068f87dfd9e09a1e016b4b148b49#/tab-Releases");

            //Assert
            Assert.That(actual.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Test]
        public async Task GetComponentByName_ForGivenComponentName_ReturnsComponentDetailInfoAsString()
        {
            //Arange 
            Mock<ISw360ApiCommunication> mockSw360comm = new Mock<ISw360ApiCommunication>();
            mockSw360comm.Setup(x => x.GetComponentByName(It.IsAny<string>())).ReturnsAsync("ComponentInfo");

            //Act          
            sW360ApicommunicationFacade = new SW360ApicommunicationFacade(mockSw360comm.Object);
            string actual = await sW360ApicommunicationFacade.GetComponentByName("Zone.js");

            //Assert
            Assert.That(actual, Is.EqualTo("ComponentInfo"));
        }

        [Test]
        public async Task GetComponentUsingName_ForGivenComponent_ReturnsHttpResponse()
        {
            //Arange 
            HttpResponseMessage httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);
            Mock<ISw360ApiCommunication> mockSw360comm = new Mock<ISw360ApiCommunication>();
            mockSw360comm.Setup(x => x.GetComponentUsingName(It.IsAny<string>())).ReturnsAsync(httpResponseMessage);

            //Act          
            sW360ApicommunicationFacade = new SW360ApicommunicationFacade(mockSw360comm.Object);
            HttpResponseMessage actual = await sW360ApicommunicationFacade.GetComponentUsingName("@angular/animation");

            //Assert
            Assert.That(actual.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Test]
        public async Task UpdateLinkedRelease_ForGivenComponent_ReturnsHttpResponse()
        {
            //Arange 
            HttpResponseMessage httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);
            UpdateLinkedRelease updateLinkedRelease = new UpdateLinkedRelease();
            Mock<ISw360ApiCommunication> mockSw360comm = new Mock<ISw360ApiCommunication>();
            mockSw360comm.Setup(x => x.UpdateLinkedRelease(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UpdateLinkedRelease>())).ReturnsAsync(httpResponseMessage);

            //Act          
            sW360ApicommunicationFacade = new SW360ApicommunicationFacade(mockSw360comm.Object);
            HttpResponseMessage actual = await sW360ApicommunicationFacade.UpdateLinkedRelease("ProejctID945u4t", "Releasdsaindsds", updateLinkedRelease);

            //Assert
            Assert.That(actual.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Test]
        public async Task GetReleaseByExternalId_ForGivenComponent_ReturnsHttpResponse()
        {
            //Arange 
            HttpResponseMessage httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);
            Mock<ISw360ApiCommunication> mockSw360comm = new Mock<ISw360ApiCommunication>();
            mockSw360comm.Setup(x => x.GetReleaseByExternalId(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(httpResponseMessage);

            //Act          
            sW360ApicommunicationFacade = new SW360ApicommunicationFacade(mockSw360comm.Object);
            HttpResponseMessage actual = await sW360ApicommunicationFacade.GetReleaseByExternalId("pkg:npm:@angular/animation@1.0.0");

            //Assert
            Assert.That(actual.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Test]
        public async Task GetComponentByExternalId_ForGivenComponent_ReturnsHttpResponse()
        {
            //Arange 
            HttpResponseMessage httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);
            Mock<ISw360ApiCommunication> mockSw360comm = new Mock<ISw360ApiCommunication>();
            mockSw360comm.Setup(x => x.GetComponentByExternalId(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(httpResponseMessage);

            //Act          
            sW360ApicommunicationFacade = new SW360ApicommunicationFacade(mockSw360comm.Object);
            HttpResponseMessage actual = await sW360ApicommunicationFacade.GetComponentByExternalId("pkg:npm:@angular/animation");

            //Assert
            Assert.That(actual.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Test]
        public async Task GetProjectsByTag_ForGivenComponent_ReturnsHttpResponse()
        {
            //Arange 

            HttpResponseMessage httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);
            Mock<ISw360ApiCommunication> mockSw360comm = new Mock<ISw360ApiCommunication>();
            mockSw360comm.Setup(x => x.GetProjectsByTag(It.IsAny<string>())).ReturnsAsync(httpResponseMessage);

            //Act          
            sW360ApicommunicationFacade = new SW360ApicommunicationFacade(mockSw360comm.Object);
            HttpResponseMessage actual = await sW360ApicommunicationFacade.GetProjectsByTag("SI DG EA-P&R");

            //Assert
            Assert.That(actual.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }
    }
}
