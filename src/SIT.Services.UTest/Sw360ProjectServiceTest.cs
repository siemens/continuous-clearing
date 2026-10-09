// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2026 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using SIT.APICommunications.Model;
using SIT.Facade.Interfaces;
using SIT.Services.Interface;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Text;
using System.Threading.Tasks;

namespace SIT.Services.UTest
{
    [TestFixture]
    public class Sw360ProjectServiceTest
    {
        [SetUp]
        public void Setup()
        {
            //implement
        }

        [Test]
        public async Task GetProjectNameByProjectIDFromSW360_InvalidSW360Credentials_HttpRequestException_ReturnsProjectNameAsEmpty()
        {
            // Arrange
            ProjectReleases projectReleases = new ProjectReleases();
            Mock<ISW360ApicommunicationFacade> sw360ApicommunicationFacadeMck = new Mock<ISW360ApicommunicationFacade>();
            sw360ApicommunicationFacadeMck.Setup(x => x.GetProjectById(It.IsAny<string>())).Throws<HttpRequestException>();
            ISw360ProjectService sw360ProjectService = new Sw360ProjectService(sw360ApicommunicationFacadeMck.Object);

            // Act
            var actualProjectName = await sw360ProjectService.GetProjectNameByProjectIDFromSW360("shdjdkhsdfdkfhdhifsodo", "TestProject", projectReleases);

            // Assert
            Assert.That(actualProjectName, Is.EqualTo(string.Empty), "GetProjectNameByProjectIDFromSW360 does not return empty on exception");
        }

        [Test]
        public async Task GetProjectNameByProjectIDFromSW360_InvalidSW360Credentials_AggregateException_ReturnsProjectNameAsEmpty()
        {
            // Arrange
            ProjectReleases projectReleases = new ProjectReleases();
            Mock<ISW360ApicommunicationFacade> sw360ApicommunicationFacadeMck = new Mock<ISW360ApicommunicationFacade>();
            sw360ApicommunicationFacadeMck.Setup(x => x.GetProjectById(It.IsAny<string>())).Throws<AggregateException>();
            ISw360ProjectService sw360ProjectService = new Sw360ProjectService(sw360ApicommunicationFacadeMck.Object);

            // Act
            var actualProjectName = await sw360ProjectService.GetProjectNameByProjectIDFromSW360("shdjdkhsdfdkfhdhifsodo", "TestProject", projectReleases);

            // Assert
            Assert.That(actualProjectName, Is.EqualTo(string.Empty), "GetProjectNameByProjectIDFromSW360 does not return empty on exception");
        }

        [Test]
        public async Task GetProjectNameByProjectIDFromSW360_ValidProjectIdAndName_ReturnsProjectNameAsEmpty()
        {
            // Arrange
            ProjectReleases projectsMapper = new ProjectReleases();
            projectsMapper.Name = "TestProject";

            Mock<ISW360ApicommunicationFacade> sw360ApicommunicationFacadeMck = new Mock<ISW360ApicommunicationFacade>();
            sw360ApicommunicationFacadeMck.Setup(x => x.GetProjectById(It.IsAny<string>())).Throws<HttpRequestException>();
            ISw360ProjectService sw360ProjectService = new Sw360ProjectService(sw360ApicommunicationFacadeMck.Object);

            // Act
            var actualProjectName = await sw360ProjectService.GetProjectNameByProjectIDFromSW360("shdjdkhsdfdkfhdhifsodo", "TestProject", projectsMapper);

            // Assert
            Assert.That(actualProjectName, Is.EqualTo(string.Empty), "Project Id not exist");
        }

        [Test]
        public async Task GetProjectNameByProjectIDFromSW360_ValidProjectNameAndId_ReturnsTheProjectName()
        {
            // Arrange
            string expectedName = "TestProject";
            ProjectReleases projectsMapper = new ProjectReleases();
            projectsMapper.Name = "TestProject";
            var projectDataSerialized = JsonConvert.SerializeObject(projectsMapper);
            HttpResponseMessage httpResponseMessage = new HttpResponseMessage();
            var content = new StringContent(projectDataSerialized, Encoding.UTF8, "application/json");
            httpResponseMessage.Content = content;

            Mock<ISW360ApicommunicationFacade> sw360ApicommunicationFacadeMck = new Mock<ISW360ApicommunicationFacade>();
            sw360ApicommunicationFacadeMck.Setup(x => x.GetProjectById(It.IsAny<string>())).ReturnsAsync(httpResponseMessage);
            ISw360ProjectService sw360ProjectService = new Sw360ProjectService(sw360ApicommunicationFacadeMck.Object);

            // Act
            var actualProjectName = await sw360ProjectService.GetProjectNameByProjectIDFromSW360("2c0a03b6d4edaf1b2ccdf64d0d0004f7", "TestProject", projectsMapper);

            // Assert
            Assert.That(actualProjectName, Is.EqualTo(expectedName), "Project Id not exist");
        }

        [Test]
        public async Task GetProjectNameByProjectIDFromSW360_ValidProjectNameAndId_ReturnsResponseAsEmpty()
        {
            // Arrange
            string expectedName = "";
            ProjectReleases projectsMapper = new ProjectReleases();
            projectsMapper.Name = "TestProject";
            var projectDataSerialized = JsonConvert.SerializeObject(projectsMapper);
            HttpResponseMessage httpResponseMessage = null;
            var content = new StringContent(projectDataSerialized, Encoding.UTF8, "application/json");

            Mock<ISW360ApicommunicationFacade> sw360ApicommunicationFacadeMck = new Mock<ISW360ApicommunicationFacade>();
            sw360ApicommunicationFacadeMck.Setup(x => x.GetProjectById(It.IsAny<string>())).ReturnsAsync(httpResponseMessage);
            ISw360ProjectService sw360ProjectService = new Sw360ProjectService(sw360ApicommunicationFacadeMck.Object);

            // Act
            var actualProjectName = await sw360ProjectService.GetProjectNameByProjectIDFromSW360("2c0a03b6d4edaf1b2ccdf64d0d0004f7", "TestProject", projectsMapper);

            // Assert
            Assert.That(actualProjectName, Is.EqualTo(expectedName), "Project Id not exist");
        }

        [Test]
        public async Task GetProjectNameByProjectIDFromSW360_ValidProjectNameAndId_ReturnsNotOK()
        {
            // Arrange
            string expectedName = "";
            ProjectReleases projectsMapper = new ProjectReleases();
            projectsMapper.Name = "TestProject";
            var projectDataSerialized = JsonConvert.SerializeObject(projectsMapper);
            HttpResponseMessage httpResponseMessage = new HttpResponseMessage(System.Net.HttpStatusCode.Forbidden);
            var content = new StringContent(projectDataSerialized, Encoding.UTF8, "application/json");
            httpResponseMessage.Content = content;

            Mock<ISW360ApicommunicationFacade> sw360ApicommunicationFacadeMck = new Mock<ISW360ApicommunicationFacade>();
            sw360ApicommunicationFacadeMck.Setup(x => x.GetProjectById(It.IsAny<string>())).ReturnsAsync(httpResponseMessage);
            ISw360ProjectService sw360ProjectService = new Sw360ProjectService(sw360ApicommunicationFacadeMck.Object);

            // Act
            var actualProjectName = await sw360ProjectService.GetProjectNameByProjectIDFromSW360("2c0a03b6d4edaf1b2ccdf64d0d0004f7", "TestProject", projectsMapper);

            // Assert
            Assert.That(actualProjectName, Is.EqualTo(expectedName), "Project Id not exist");
        }

        [Test]
        public async Task GetProjectLinkedReleasesByProjectId_InvalidSW360Credentials_HttpRequestException_ReturnsEmptyList()
        {
            // Arrange
            List<ReleaseLinked> expected = new List<ReleaseLinked>();
            Mock<ISW360ApicommunicationFacade> sw360ApicommunicationFacadeMck = new Mock<ISW360ApicommunicationFacade>();
            sw360ApicommunicationFacadeMck.Setup(x => x.GetProjectById(It.IsAny<string>())).Throws<HttpRequestException>();
            ISw360ProjectService sw360ProjectService = new Sw360ProjectService(sw360ApicommunicationFacadeMck.Object);

            // Act
            List<ReleaseLinked> actual = await sw360ProjectService.GetAlreadyLinkedReleasesByProjectId("shdjdkhsdfdkfhdhifsodo");

            // Assert
            Assert.That(actual.Count, Is.EqualTo(expected.Count), "GetProjectLinkedReleasesByProjectId does not return empty on exception");
        }

        [Test]
        public async Task GetProjectLinkedReleasesByProjectId_InvalidSW360Credentials_AggregateException_ReturnsEmptyList()
        {
            // Arrange
            List<ReleaseLinked> expected = new List<ReleaseLinked>();
            Mock<ISW360ApicommunicationFacade> sw360ApicommunicationFacadeMck = new Mock<ISW360ApicommunicationFacade>();
            sw360ApicommunicationFacadeMck.Setup(x => x.GetProjectById(It.IsAny<string>())).Throws<AggregateException>();
            ISw360ProjectService sw360ProjectService = new Sw360ProjectService(sw360ApicommunicationFacadeMck.Object);

            // Act
            List<ReleaseLinked> actual = await sw360ProjectService.GetAlreadyLinkedReleasesByProjectId("shdjdkhsdfdkfhdhifsodo");

            // Assert
            Assert.That(actual.Count, Is.EqualTo(expected.Count), "GetProjectLinkedReleasesByProjectId does not return empty on exception");
        }

        [Test]
        public async Task GetProjectLinkedReleasesByProjectId_ValidProjectId_ErrorBadRequest_ReturnsEmptyList()
        {
            // Arrange
            List<ReleaseLinked> expected = new List<ReleaseLinked>();
            HttpResponseMessage httpResponseMessage = new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest);
            Mock<ISW360ApicommunicationFacade> sw360ApicommunicationFacadeMck = new Mock<ISW360ApicommunicationFacade>();
            sw360ApicommunicationFacadeMck.Setup(x => x.GetProjectById(It.IsAny<string>())).ReturnsAsync(httpResponseMessage);
            ISw360ProjectService sw360ProjectService = new Sw360ProjectService(sw360ApicommunicationFacadeMck.Object);

            // Act
            List<ReleaseLinked> actual = await sw360ProjectService.GetAlreadyLinkedReleasesByProjectId("shdjdkhsdfdkfhdhifsodo");

            // Assert
            Assert.That(actual.Count, Is.EqualTo(expected.Count), "GetProjectLinkedReleasesByProjectId does not return empty on exception");
        }

        [Test]
        public async Task GetProjectLinkedReleasesByProjectId_ValidProjectId_ReturnsReleasesLinked()
        {
            // Arrange
            Self self = new Self() { Href = "http://md2pdvnc:8095/resource/api/releases/ff8d19674e737371be578cafec0c663e" };
            Links links = new Links() { Self = self };
            Sw360Releases sw360Releases = new Sw360Releases() { Name = "tslib", Version = "2.2.0", Links = links };
            ProjectReleases projectReleases = new ProjectReleases();
            projectReleases.Embedded = new ReleaseEmbedded() { Sw360Releases = new List<Sw360Releases>() { sw360Releases } };


            List<ReleaseLinked> expected = new List<ReleaseLinked>();
            HttpResponseMessage httpResponseMessage = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
            httpResponseMessage.Content = new ObjectContent<ProjectReleases>(projectReleases, new JsonMediaTypeFormatter(), "application/some-format");

            Mock<ISW360ApicommunicationFacade> sW360ApicommunicationFacadeMck = new Mock<ISW360ApicommunicationFacade>();
            sW360ApicommunicationFacadeMck.Setup(x => x.GetProjectById(It.IsAny<string>())).ReturnsAsync(httpResponseMessage);
            ISw360ProjectService sw360ProjectService = new Sw360ProjectService(sW360ApicommunicationFacadeMck.Object);

            // Act
            List<ReleaseLinked> actual = await sw360ProjectService.GetAlreadyLinkedReleasesByProjectId("shdjdkhsdfdkfhdhifsodo");

            // Assert
            Assert.That(actual.Count, Is.EqualTo(expected.Count), "GetProjectLinkedReleasesByProjectId does not return empty on exception");
        }

        [Test]
        public async Task GetAlreadyLinkedReleasesByProjectId_PassProjectId_SuccessFullyReturnsReleaseLinked()
        {
            // Arrange
            Self self = new Self() { Href = "http://md2pdvnc:8095/resource/api/releases/ff8d19674e737371be578cafec0c663e" };
            Links links = new Links() { Self = self };

            Sw360Releases sw360Releases = new Sw360Releases() { Name = "tslib", Version = "2.2.0", Links = links };
            Sw360LinkedRelease sw360LinkedRelease = new Sw360LinkedRelease();
            sw360LinkedRelease.Release = "http://md2pdvnc:8095/resource/api/releases/ff8d19674e737371be578cafec0c663e";
            List<Sw360LinkedRelease> sw360LinkedReleases = new List<Sw360LinkedRelease>();
            sw360LinkedReleases.Add(sw360LinkedRelease);
            ProjectReleases projectReleases = new ProjectReleases();
            projectReleases.Embedded =
                new ReleaseEmbedded() { Sw360Releases = new List<Sw360Releases>() { sw360Releases } };
            projectReleases.LinkedReleases = sw360LinkedReleases;


            List<ReleaseLinked> expected = new List<ReleaseLinked>();
            HttpResponseMessage httpResponseMessage = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
            httpResponseMessage.Content = new ObjectContent<ProjectReleases>(projectReleases, new JsonMediaTypeFormatter(), "application/some-format");

            Mock<ISW360ApicommunicationFacade> sW360ApicommunicationFacadeMck = new Mock<ISW360ApicommunicationFacade>();
            sW360ApicommunicationFacadeMck.Setup(x => x.GetProjectById(It.IsAny<string>())).ReturnsAsync(httpResponseMessage);
            ISw360ProjectService sw360ProjectService = new Sw360ProjectService(sW360ApicommunicationFacadeMck.Object);

            // Act
            List<ReleaseLinked> actual = await sw360ProjectService.GetAlreadyLinkedReleasesByProjectId("shdjdkhsdfdkfhdhifsodo");


            // Assert
            Assert.That(actual.Count, Is.GreaterThan(0));
        }

        [Test]
        public async Task UpdateProjectAdditionalData_ValidProjectId_AddsKeyAndReturnsTrue()
        {
            // Arrange
            ProjectReleases projectReleases = new ProjectReleases();
            HttpResponseMessage getResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
            getResponse.Content = new ObjectContent<ProjectReleases>(projectReleases, new JsonMediaTypeFormatter(), "application/json");
            HttpResponseMessage patchResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK);

            HttpContent capturedContent = null;
            Mock<ISW360ApicommunicationFacade> sw360ApicommunicationFacadeMck = new Mock<ISW360ApicommunicationFacade>();
            sw360ApicommunicationFacadeMck.Setup(x => x.GetProjectById(It.IsAny<string>())).ReturnsAsync(getResponse);
            sw360ApicommunicationFacadeMck.Setup(x => x.UpdateProject(It.IsAny<string>(), It.IsAny<HttpContent>()))
                .Callback<string, HttpContent>((_, content) => capturedContent = content)
                .ReturnsAsync(patchResponse);
            ISw360ProjectService sw360ProjectService = new Sw360ProjectService(sw360ApicommunicationFacadeMck.Object);

            // Act
            bool actual = await sw360ProjectService.UpdateProjectAdditionalData("projectId", "SITCreateLastSyncAt", "2026-10-08T00:00:00.0000000Z");

            // Assert
            Assert.That(actual, Is.True);
            string body = await capturedContent.ReadAsStringAsync();
            Assert.That(body, Does.Contain("SITCreateLastSyncAt"));
        }

        [Test]
        public async Task UpdateProjectAdditionalData_ProjectNotFound_ReturnsFalse()
        {
            // Arrange
            HttpResponseMessage getResponse = new HttpResponseMessage(System.Net.HttpStatusCode.NotFound);
            Mock<ISW360ApicommunicationFacade> sw360ApicommunicationFacadeMck = new Mock<ISW360ApicommunicationFacade>();
            sw360ApicommunicationFacadeMck.Setup(x => x.GetProjectById(It.IsAny<string>())).ReturnsAsync(getResponse);
            ISw360ProjectService sw360ProjectService = new Sw360ProjectService(sw360ApicommunicationFacadeMck.Object);

            // Act
            bool actual = await sw360ProjectService.UpdateProjectAdditionalData("projectId", "SITCreateLastSyncAt", "value");

            // Assert
            Assert.That(actual, Is.False);
            sw360ApicommunicationFacadeMck.Verify(x => x.UpdateProject(It.IsAny<string>(), It.IsAny<HttpContent>()), Times.Never);
        }

        [Test]
        public async Task UpdateProjectAdditionalData_HttpRequestException_ReturnsFalse()
        {
            // Arrange
            Mock<ISW360ApicommunicationFacade> sw360ApicommunicationFacadeMck = new Mock<ISW360ApicommunicationFacade>();
            sw360ApicommunicationFacadeMck.Setup(x => x.GetProjectById(It.IsAny<string>())).Throws<HttpRequestException>();
            ISw360ProjectService sw360ProjectService = new Sw360ProjectService(sw360ApicommunicationFacadeMck.Object);

            // Act
            bool actual = await sw360ProjectService.UpdateProjectAdditionalData("projectId", "SITCreateLastSyncAt", "value");

            // Assert
            Assert.That(actual, Is.False);
        }

        [Test]
        public async Task UpdateProjectAdditionalData_NullGetResponse_ReturnsFalse()
        {
            // Arrange
            Mock<ISW360ApicommunicationFacade> sw360ApicommunicationFacadeMck = new Mock<ISW360ApicommunicationFacade>();
            sw360ApicommunicationFacadeMck.Setup(x => x.GetProjectById(It.IsAny<string>())).ReturnsAsync((HttpResponseMessage)null);
            ISw360ProjectService sw360ProjectService = new Sw360ProjectService(sw360ApicommunicationFacadeMck.Object);

            // Act
            bool actual = await sw360ProjectService.UpdateProjectAdditionalData("projectId", "SITCreateLastSyncAt", "value");

            // Assert
            Assert.That(actual, Is.False);
            sw360ApicommunicationFacadeMck.Verify(x => x.UpdateProject(It.IsAny<string>(), It.IsAny<HttpContent>()), Times.Never);
        }

        [Test]
        public async Task UpdateProjectAdditionalData_ExistingAdditionalData_PreservesEntriesAndReturnsTrue()
        {
            // Arrange
            ProjectReleases projectReleases = new ProjectReleases
            {
                AdditionalData = new Dictionary<string, string> { { "existingKey", "existingValue" } }
            };
            HttpResponseMessage getResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
            getResponse.Content = new ObjectContent<ProjectReleases>(projectReleases, new JsonMediaTypeFormatter(), "application/json");
            HttpResponseMessage patchResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK);

            HttpContent capturedContent = null;
            Mock<ISW360ApicommunicationFacade> sw360ApicommunicationFacadeMck = new Mock<ISW360ApicommunicationFacade>();
            sw360ApicommunicationFacadeMck.Setup(x => x.GetProjectById(It.IsAny<string>())).ReturnsAsync(getResponse);
            sw360ApicommunicationFacadeMck.Setup(x => x.UpdateProject(It.IsAny<string>(), It.IsAny<HttpContent>()))
                .Callback<string, HttpContent>((_, content) => capturedContent = content)
                .ReturnsAsync(patchResponse);
            ISw360ProjectService sw360ProjectService = new Sw360ProjectService(sw360ApicommunicationFacadeMck.Object);

            // Act
            bool actual = await sw360ProjectService.UpdateProjectAdditionalData("projectId", "SITCreateLastSyncAt", "2026-10-08T00:00:00");

            // Assert
            Assert.That(actual, Is.True);
            string body = await capturedContent.ReadAsStringAsync();
            Assert.That(body, Does.Contain("existingKey"));
            Assert.That(body, Does.Contain("SITCreateLastSyncAt"));
        }

        [Test]
        public async Task UpdateProjectAdditionalData_PatchFails_ReturnsFalse()
        {
            // Arrange
            ProjectReleases projectReleases = new ProjectReleases();
            HttpResponseMessage getResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
            getResponse.Content = new ObjectContent<ProjectReleases>(projectReleases, new JsonMediaTypeFormatter(), "application/json");
            HttpResponseMessage patchResponse = new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest)
            {
                Content = new StringContent("{\"message\":\"Unauthorized user or empty commit message passed.\"}", Encoding.UTF8, "application/json")
            };

            Mock<ISW360ApicommunicationFacade> sw360ApicommunicationFacadeMck = new Mock<ISW360ApicommunicationFacade>();
            sw360ApicommunicationFacadeMck.Setup(x => x.GetProjectById(It.IsAny<string>())).ReturnsAsync(getResponse);
            sw360ApicommunicationFacadeMck.Setup(x => x.UpdateProject(It.IsAny<string>(), It.IsAny<HttpContent>())).ReturnsAsync(patchResponse);
            ISw360ProjectService sw360ProjectService = new Sw360ProjectService(sw360ApicommunicationFacadeMck.Object);

            // Act
            bool actual = await sw360ProjectService.UpdateProjectAdditionalData("projectId", "SITCreateLastSyncAt", "value");

            // Assert
            Assert.That(actual, Is.False);
        }

        [Test]
        public async Task UpdateProjectAdditionalData_NullPatchResponse_ReturnsFalse()
        {
            // Arrange
            ProjectReleases projectReleases = new ProjectReleases();
            HttpResponseMessage getResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
            getResponse.Content = new ObjectContent<ProjectReleases>(projectReleases, new JsonMediaTypeFormatter(), "application/json");

            Mock<ISW360ApicommunicationFacade> sw360ApicommunicationFacadeMck = new Mock<ISW360ApicommunicationFacade>();
            sw360ApicommunicationFacadeMck.Setup(x => x.GetProjectById(It.IsAny<string>())).ReturnsAsync(getResponse);
            sw360ApicommunicationFacadeMck.Setup(x => x.UpdateProject(It.IsAny<string>(), It.IsAny<HttpContent>())).ReturnsAsync((HttpResponseMessage)null);
            ISw360ProjectService sw360ProjectService = new Sw360ProjectService(sw360ApicommunicationFacadeMck.Object);

            // Act
            bool actual = await sw360ProjectService.UpdateProjectAdditionalData("projectId", "SITCreateLastSyncAt", "value");

            // Assert
            Assert.That(actual, Is.False);
        }

        [Test]
        public async Task UpdateProjectAdditionalData_AggregateException_ReturnsFalse()
        {
            // Arrange
            Mock<ISW360ApicommunicationFacade> sw360ApicommunicationFacadeMck = new Mock<ISW360ApicommunicationFacade>();
            sw360ApicommunicationFacadeMck.Setup(x => x.GetProjectById(It.IsAny<string>())).Throws<AggregateException>();
            ISw360ProjectService sw360ProjectService = new Sw360ProjectService(sw360ApicommunicationFacadeMck.Object);

            // Act
            bool actual = await sw360ProjectService.UpdateProjectAdditionalData("projectId", "SITCreateLastSyncAt", "value");

            // Assert
            Assert.That(actual, Is.False);
        }
    }
}
