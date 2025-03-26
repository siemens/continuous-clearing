// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
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
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Text;
using System.Threading.Tasks;

namespace LCT.Services.UTest
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
    }
}
