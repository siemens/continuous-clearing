// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.APICommunications.Model;
using LCT.APICommunications.Model.AQL;
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
    internal class JFrogServiceUTest
    {
        [Test]
        public async Task GetInternalComponentDataByRepo_GetsRepoInfo_Successfully()
        {
            // Arrange

            AqlResult aqlResult = new AqlResult()
            {
                Name = "saap-api-node-2.26.3-LicenseClearing.16.sha-058fada.tgz",
                Path = "@testfolder/-/folder",
                Repo = "energy-dev-npm-egll"
            };

            IList<AqlResult> results = new List<AqlResult>();
            results.Add(aqlResult);

            AqlResponse aqlResponse = new AqlResponse();
            aqlResponse.Results = results;

            var aqlResponseSerialized = JsonConvert.SerializeObject(aqlResponse);
            var content = new StringContent(aqlResponseSerialized, Encoding.UTF8, "application/json");
            HttpResponseMessage httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);
            httpResponseMessage.Content = content;


            Mock<IJfrogAqlApiCommunicationFacade> mockJfrogApiComFacade =
                new Mock<IJfrogAqlApiCommunicationFacade>();
            mockJfrogApiComFacade
                .Setup(x => x.GetInternalComponentDataByRepo(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(httpResponseMessage);

            // Act
            IJFrogService jFrogService = new JFrogService(mockJfrogApiComFacade.Object);
            IList<AqlResult> actual = await jFrogService.GetInternalComponentDataByRepo("energy-dev-npm-egll");

            // Assert
            Assert.That(actual.Count, Is.GreaterThan(0));
        }

        [Test]
        public async Task GetInternalComponentDataByRepo_ResultsWith_NoContent()
        {
            // Arrange

            HttpResponseMessage httpResponseMessage = new HttpResponseMessage(HttpStatusCode.NoContent);

            Mock<IJfrogAqlApiCommunicationFacade> mockJfrogApiComFacade =
                new Mock<IJfrogAqlApiCommunicationFacade>();
            mockJfrogApiComFacade
                .Setup(x => x.GetInternalComponentDataByRepo(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(httpResponseMessage);

            // Act
            IJFrogService jFrogService = new JFrogService(mockJfrogApiComFacade.Object);
            IList<AqlResult> actual = await jFrogService.GetInternalComponentDataByRepo("energy-dev-npm-egll");

            // Assert
            Assert.That(actual.Count, Is.EqualTo(0));
        }

        [Test]
        public async Task GetInternalComponentDataByRepo_ResultsWith_EmptyHttpResponse()
        {
            // Arrange
            HttpResponseMessage httpResponseMessage = null;
            Mock<IJfrogAqlApiCommunicationFacade> mockJfrogApiComFacade =
                new Mock<IJfrogAqlApiCommunicationFacade>();
            mockJfrogApiComFacade
                .Setup(x => x.GetInternalComponentDataByRepo(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(httpResponseMessage);

            // Act
            IJFrogService jFrogService = new JFrogService(mockJfrogApiComFacade.Object);
            IList<AqlResult> actual = await jFrogService.GetInternalComponentDataByRepo("energy-dev-npm-egll");

            // Assert
            Assert.That(actual.Count, Is.EqualTo(0));
        }

        [Test]
        public async Task GetInternalComponentDataByRepo_ResultsWith_HttpRequestException()
        {
            // Arrange
            Mock<IJfrogAqlApiCommunicationFacade> mockJfrogApiComFacade =
                new Mock<IJfrogAqlApiCommunicationFacade>();
            mockJfrogApiComFacade
                .Setup(x => x.GetInternalComponentDataByRepo(It.IsAny<string>(), It.IsAny<string>())).
                Throws<HttpRequestException>();

            // Act
            IJFrogService jFrogService = new JFrogService(mockJfrogApiComFacade.Object);
            IList<AqlResult> actual = await jFrogService.GetInternalComponentDataByRepo("energy-dev-npm-egll");

            // Assert
            Assert.That(actual.Count, Is.EqualTo(0));
        }

        [Test]
        public async Task GetInternalComponentDataByRepo_ResultsWith_InvalidOperationException()
        {
            // Arrange
            Mock<IJfrogAqlApiCommunicationFacade> mockJfrogApiComFacade =
                new Mock<IJfrogAqlApiCommunicationFacade>();
            mockJfrogApiComFacade
                .Setup(x => x.GetInternalComponentDataByRepo(It.IsAny<string>(), It.IsAny<string>())).
                Throws<InvalidOperationException>();

            // Act
            IJFrogService jFrogService = new JFrogService(mockJfrogApiComFacade.Object);
            IList<AqlResult> actual = await jFrogService.GetInternalComponentDataByRepo("energy-dev-npm-egll");

            // Assert
            Assert.That(actual.Count, Is.EqualTo(0));
        }

        [Test]
        public async Task GetInternalComponentDataByRepo_ResultsWith_TaskCanceledException()
        {
            // Arrange
            Mock<IJfrogAqlApiCommunicationFacade> mockJfrogApiComFacade =
                new Mock<IJfrogAqlApiCommunicationFacade>();
            mockJfrogApiComFacade
                .Setup(x => x.GetInternalComponentDataByRepo(It.IsAny<string>(), It.IsAny<string>())).
                Throws<TaskCanceledException>();

            // Act
            IJFrogService jFrogService = new JFrogService(mockJfrogApiComFacade.Object);
            IList<AqlResult> actual = await jFrogService.GetInternalComponentDataByRepo("energy-dev-npm-egll");

            // Assert
            Assert.That(actual.Count, Is.EqualTo(0));
        }

        [Test]
        public async Task GetPackageInfo_GetsPackageInfo_Successfully()
        {
            // Arrange
            ComponentsToArtifactory component = new ComponentsToArtifactory
            {
                SrcRepoName = "energy-dev-npm-egll",
                JfrogPackageName = "saap-api-node-2.26.3-LicenseClearing.16.sha-058fada.tgz",
                Path = string.Empty
            };
            AqlResult aqlResult = new AqlResult()
            {
                Name = "saap-api-node-2.26.3-LicenseClearing.16.sha-058fada.tgz",
                Path = "@testfolder/-/folder",
                Repo = "energy-dev-npm-egll"
            };

            IList<AqlResult> results = new List<AqlResult>();
            results.Add(aqlResult);

            AqlResponse aqlResponse = new AqlResponse();
            aqlResponse.Results = results;

            var aqlResponseSerialized = JsonConvert.SerializeObject(aqlResponse);
            var content = new StringContent(aqlResponseSerialized, Encoding.UTF8, "application/json");
            HttpResponseMessage httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);
            httpResponseMessage.Content = content;


            Mock<IJfrogAqlApiCommunicationFacade> mockJfrogApiComFacade =
                new Mock<IJfrogAqlApiCommunicationFacade>();
            mockJfrogApiComFacade
                .Setup(x => x.GetPackageInfo(component, It.IsAny<string>()))
                .ReturnsAsync(httpResponseMessage);

            // Act
            IJFrogService jFrogService = new JFrogService(mockJfrogApiComFacade.Object);
            AqlResult actual = await jFrogService.GetPackageInfo(component);

            // Assert
            Assert.NotNull(actual);
        }

        [Test]
        public async Task GetPackageInfo_ResultsWith_NoContent()
        {
            // Arrange
            ComponentsToArtifactory component = new ComponentsToArtifactory
            {
                SrcRepoName = "energy-dev-npm-egll",
                JfrogPackageName = "saap-api-node-2.26.3-LicenseClearing.16.sha-058fada.tgz",
                Path = string.Empty
            };
            HttpResponseMessage httpResponseMessage = new HttpResponseMessage(HttpStatusCode.NoContent);

            Mock<IJfrogAqlApiCommunicationFacade> mockJfrogApiComFacade =
                new Mock<IJfrogAqlApiCommunicationFacade>();
            mockJfrogApiComFacade
                .Setup(x => x.GetPackageInfo(component, It.IsAny<string>()))
                .ReturnsAsync(httpResponseMessage);

            // Act
            IJFrogService jFrogService = new JFrogService(mockJfrogApiComFacade.Object);
            AqlResult actual = await jFrogService.GetPackageInfo(component);

            // Assert
            Assert.Null(actual);
        }

        [Test]
        public async Task GetPackageInfo_ResultsWith_HttpRequestException()
        {
            // Arrange
            ComponentsToArtifactory component = new ComponentsToArtifactory
            {
                SrcRepoName = "energy-dev-npm-egll",
                JfrogPackageName = "saap-api-node-2.26.3-LicenseClearing.16.sha-058fada.tgz",
                Path = string.Empty
            };
            Mock<IJfrogAqlApiCommunicationFacade> mockJfrogApiComFacade =
                new Mock<IJfrogAqlApiCommunicationFacade>();
            mockJfrogApiComFacade
                .Setup(x => x.GetPackageInfo(component, It.IsAny<string>())).
                Throws<HttpRequestException>();

            // Act
            IJFrogService jFrogService = new JFrogService(mockJfrogApiComFacade.Object);
            AqlResult actual = await jFrogService.GetPackageInfo(component);

            // Assert
            Assert.Null(actual);
        }

        [Test]
        public async Task GetPackageInfo_ResultsWith_TaskCanceledException()
        {
            // Arrange
            ComponentsToArtifactory component = new ComponentsToArtifactory
            {
                SrcRepoName = "energy-dev-npm-egll",
                JfrogPackageName = "saap-api-node-2.26.3-LicenseClearing.16.sha-058fada.tgz",
                Path = string.Empty
            };
            Mock<IJfrogAqlApiCommunicationFacade> mockJfrogApiComFacade =
                new Mock<IJfrogAqlApiCommunicationFacade>();
            mockJfrogApiComFacade
                .Setup(x => x.GetPackageInfo(component, It.IsAny<string>())).
                Throws<TaskCanceledException>();

            // Act
            IJFrogService jFrogService = new JFrogService(mockJfrogApiComFacade.Object);
            AqlResult actual = await jFrogService.GetPackageInfo(component);

            // Assert
            Assert.Null(actual);
        }

        [Test]
        public async Task GetPackageInfo_ResultsWith_InvalidOperationException()
        {
            // Arrange
            ComponentsToArtifactory component = new ComponentsToArtifactory
            {
                SrcRepoName = "energy-dev-npm-egll",
                JfrogPackageName = "saap-api-node-2.26.3-LicenseClearing.16.sha-058fada.tgz",
                Path = string.Empty
            };
            Mock<IJfrogAqlApiCommunicationFacade> mockJfrogApiComFacade =
                new Mock<IJfrogAqlApiCommunicationFacade>();
            mockJfrogApiComFacade
                .Setup(x => x.GetPackageInfo(component, It.IsAny<string>())).
                Throws<InvalidOperationException>();

            // Act
            IJFrogService jFrogService = new JFrogService(mockJfrogApiComFacade.Object);
            AqlResult actual = await jFrogService.GetPackageInfo(component);

            // Assert
            Assert.Null(actual);
        }
        [Test]
        public async Task GetNpmComponentDataByRepo_GetsRepoInfo_Successfully()
        {
            // Arrange
            AqlResult aqlResult = new AqlResult()
            {
                Name = "example-package-1.0.0.tgz",
                Path = "@testfolder/-/folder",
                Repo = "npm-repo"
            };

            IList<AqlResult> results = new List<AqlResult> { aqlResult };

            AqlResponse aqlResponse = new AqlResponse { Results = results };

            var aqlResponseSerialized = JsonConvert.SerializeObject(aqlResponse);
            var content = new StringContent(aqlResponseSerialized, Encoding.UTF8, "application/json");
            HttpResponseMessage httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = content
            };

            Mock<IJfrogAqlApiCommunicationFacade> mockJfrogApiComFacade = new Mock<IJfrogAqlApiCommunicationFacade>();
            mockJfrogApiComFacade
                .Setup(x => x.GetNpmComponentDataByRepo(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(httpResponseMessage);

            // Act
            IJFrogService jFrogService = new JFrogService(mockJfrogApiComFacade.Object);
            IList<AqlResult> actual = await jFrogService.GetNpmComponentDataByRepo("npm-repo");

            // Assert
            Assert.That(actual.Count, Is.GreaterThan(0));
            Assert.That(actual[0].Name, Is.EqualTo("example-package-1.0.0.tgz"));
        }

        [Test]
        public async Task GetNpmComponentDataByRepo_ResultsWith_NoContent()
        {
            // Arrange
            HttpResponseMessage httpResponseMessage = new HttpResponseMessage(HttpStatusCode.NoContent);

            Mock<IJfrogAqlApiCommunicationFacade> mockJfrogApiComFacade = new Mock<IJfrogAqlApiCommunicationFacade>();
            mockJfrogApiComFacade
                .Setup(x => x.GetNpmComponentDataByRepo(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(httpResponseMessage);

            // Act
            IJFrogService jFrogService = new JFrogService(mockJfrogApiComFacade.Object);
            IList<AqlResult> actual = await jFrogService.GetNpmComponentDataByRepo("npm-repo");

            // Assert
            Assert.That(actual.Count, Is.EqualTo(0));
        }

        [Test]
        public async Task GetNpmComponentDataByRepo_ResultsWith_HttpRequestException()
        {
            // Arrange
            Mock<IJfrogAqlApiCommunicationFacade> mockJfrogApiComFacade = new Mock<IJfrogAqlApiCommunicationFacade>();
            mockJfrogApiComFacade
                .Setup(x => x.GetNpmComponentDataByRepo(It.IsAny<string>(), It.IsAny<string>()))
                .Throws<HttpRequestException>();

            // Act
            IJFrogService jFrogService = new JFrogService(mockJfrogApiComFacade.Object);
            IList<AqlResult> actual = await jFrogService.GetNpmComponentDataByRepo("npm-repo");

            // Assert
            Assert.That(actual.Count, Is.EqualTo(0));
        }

        [Test]
        public async Task GetNpmComponentDataByRepo_ResultsWith_InvalidOperationException()
        {
            // Arrange
            Mock<IJfrogAqlApiCommunicationFacade> mockJfrogApiComFacade = new Mock<IJfrogAqlApiCommunicationFacade>();
            mockJfrogApiComFacade
                .Setup(x => x.GetNpmComponentDataByRepo(It.IsAny<string>(), It.IsAny<string>()))
                .Throws<InvalidOperationException>();

            // Act
            IJFrogService jFrogService = new JFrogService(mockJfrogApiComFacade.Object);
            IList<AqlResult> actual = await jFrogService.GetNpmComponentDataByRepo("npm-repo");

            // Assert
            Assert.That(actual.Count, Is.EqualTo(0));
        }

        [Test]
        public async Task GetNpmComponentDataByRepo_ResultsWith_TaskCanceledException()
        {
            // Arrange
            Mock<IJfrogAqlApiCommunicationFacade> mockJfrogApiComFacade = new Mock<IJfrogAqlApiCommunicationFacade>();
            mockJfrogApiComFacade
                .Setup(x => x.GetNpmComponentDataByRepo(It.IsAny<string>(), It.IsAny<string>()))
                .Throws<TaskCanceledException>();

            // Act
            IJFrogService jFrogService = new JFrogService(mockJfrogApiComFacade.Object);
            IList<AqlResult> actual = await jFrogService.GetNpmComponentDataByRepo("npm-repo");

            // Assert
            Assert.That(actual.Count, Is.EqualTo(0));
        }
    }
}
