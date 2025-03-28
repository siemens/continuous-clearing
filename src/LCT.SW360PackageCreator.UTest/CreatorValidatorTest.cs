// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.APICommunications.Model;
using LCT.APICommunications.Model.Foss;
using LCT.Common;
using LCT.Common.Interface;
using LCT.Facade.Interfaces;
using LCT.Services.Interface;
using LCT.SW360PackageCreator.Model;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace LCT.SW360PackageCreator.UTest
{
    [TestFixture]
    public class CreatorValidatorTest
    {
        public Mock<ISw360ProjectService> mockISw360ProjectService;
        private Mock<ISW360ApicommunicationFacade> mockISW360ApicommunicationFacade;
        private Mock<ISW360Service> mockISW360Service;
        private Mock<ISw360CreatorService> mockISw360CreatorService;
        private Mock<IEnvironmentHelper> mockEnvironmentHelper;
        private Mock<HttpMessageHandler> mockHttpMessageHandler;
        private HttpClient httpClient;
        public CreatorValidatorTest()
        {
            mockISw360ProjectService = new Mock<ISw360ProjectService>(MockBehavior.Strict);
            mockISW360ApicommunicationFacade = new Mock<ISW360ApicommunicationFacade>();
            mockISW360Service = new Mock<ISW360Service>();
            mockISw360CreatorService = new Mock<ISw360CreatorService>();
            mockEnvironmentHelper = new Mock<IEnvironmentHelper>();
            mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            httpClient = new HttpClient(mockHttpMessageHandler.Object);

        }
        [TestCase]
        public async Task ValidateAppSettings_TestPositive()
        {
            //Arrange
            string projectName = "Test";
            ProjectReleases projectReleases = new ProjectReleases();
            var CommonAppSettings = new CommonAppSettings()
            {
                SW360 = new SW360()
                {
                    ProjectName = "Test"
                }
            };
            mockISw360ProjectService.Setup(x => x.GetProjectNameByProjectIDFromSW360(It.IsAny<String>(), It.IsAny<string>(), projectReleases))
                .ReturnsAsync(projectName);

            //Act
            await CreatorValidator.ValidateAppSettings(CommonAppSettings, mockISw360ProjectService.Object, projectReleases);

            //Assert
            mockISw360ProjectService.Verify(x => x.GetProjectNameByProjectIDFromSW360(It.IsAny<string>(), It.IsAny<string>(), projectReleases), Times.AtLeastOnce);
        }

        [TestCase]
        public async Task ValidateAppSettings_OnClosedProject_EndsTheApplication()
        {
            //Arrange
            string projectName = "Test";
            ProjectReleases projectReleases = new ProjectReleases();
            projectReleases.clearingState = "CLOSED";
            var CommonAppSettings = new CommonAppSettings()
            {
                SW360 = new SW360()
                {
                    ProjectName = "Test"
                }
            };

            mockISw360ProjectService.Setup(x => x.GetProjectNameByProjectIDFromSW360(It.IsAny<String>(), It.IsAny<string>(), projectReleases))
                .ReturnsAsync(projectName);

            //Act
            await CreatorValidator.ValidateAppSettings(CommonAppSettings, mockISw360ProjectService.Object, projectReleases);

            //Assert
            mockISw360ProjectService.Verify(x => x.GetProjectNameByProjectIDFromSW360(It.IsAny<string>(), It.IsAny<string>(), projectReleases), Times.AtLeastOnce);

        }

        [TestCase]
        public void ValidateAppSettings_TestNegative()
        {
            //Arrange
            string projectName = null;
            ProjectReleases projectReleases = new ProjectReleases();
            var CommonAppSettings = new CommonAppSettings()
            {
                SW360 = new SW360()
            };
            mockISw360ProjectService.Setup(x => x.GetProjectNameByProjectIDFromSW360(It.IsAny<string>(), It.IsAny<string>(), projectReleases))
                .ReturnsAsync(projectName);

            //Act

            //Assert
            Assert.ThrowsAsync<InvalidDataException>(() => CreatorValidator.ValidateAppSettings(CommonAppSettings, mockISw360ProjectService.Object, projectReleases));

        }
        [Test]
        public async Task FossologyUrlValidation_ValidUrl_ReturnsTrue()
        {
            // Arrange
            var appSettings = new CommonAppSettings()
            {
                SW360 = new SW360()
                {
                    Fossology = new Fossology()
                    {
                        URL = "https://stage.fossology.url"
                    }
                }
            };
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK);
            mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(responseMessage);
            // Act
            var result = await CreatorValidator.FossologyUrlValidation(appSettings, httpClient, mockEnvironmentHelper.Object);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public async Task FossologyUrlValidation_InvalidUrl_ReturnsFalse()
        {
            // Arrange
            var appSettings = new CommonAppSettings()
            {
                SW360 = new SW360()
                {
                    Fossology = new Fossology()
                    {
                        URL = "https://invalid.fossology.url"
                    }
                }
            };

            var responseMessage = new HttpResponseMessage(HttpStatusCode.NotFound);
            mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(responseMessage);

            // Act
            var result = await CreatorValidator.FossologyUrlValidation(appSettings, httpClient, mockEnvironmentHelper.Object);

            // Assert
            Assert.IsFalse(result);
        }


        [Test]
        public async Task FossologyUrlValidation_InvalidUri_ThrowsException()
        {
            // Arrange
            var appSettings = new CommonAppSettings()
            {
                SW360 = new SW360()
                {
                    Fossology = new Fossology()
                    {
                        URL = "invalid_uri"
                    }
                }
            };

            var result = await CreatorValidator.FossologyUrlValidation(appSettings, httpClient, mockEnvironmentHelper.Object);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public async Task FossologyUrlValidation_HttpRequestException_ReturnsFalse()
        {
            // Arrange
            var appSettings = new CommonAppSettings()
            {
                SW360 = new SW360()
                {
                    Fossology = new Fossology()
                    {
                        URL = "https://valid.fossology.url"
                    }
                }
            };

            mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ThrowsAsync(new HttpRequestException());

            // Act
            var result = await CreatorValidator.FossologyUrlValidation(appSettings, httpClient, mockEnvironmentHelper.Object);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public async Task GetAllReleasesDetails_WhenHttpRequestExceptionOccurs_ReturnsNull()
        {
            // Arrange
            mockISW360ApicommunicationFacade
                .Setup(x => x.GetAllReleasesWithAllData(It.IsAny<int>(), It.IsAny<int>()))
                .ThrowsAsync(new HttpRequestException());

            // Act
            var result = await InvokeGetAllReleasesDetails(mockISW360ApicommunicationFacade.Object, 1, 10);

            // Assert
            Assert.IsNull(result);
        }

        [Test]
        public async Task GetAllReleasesDetails_WhenInvalidOperationExceptionOccurs_ReturnsNull()
        {
            // Arrange
            mockISW360ApicommunicationFacade
                .Setup(x => x.GetAllReleasesWithAllData(It.IsAny<int>(), It.IsAny<int>()))
                .ThrowsAsync(new InvalidOperationException());

            // Act
            var result = await InvokeGetAllReleasesDetails(mockISW360ApicommunicationFacade.Object, 1, 10);

            // Assert
            Assert.IsNull(result);
        }

        [Test]
        public async Task GetAllReleasesDetails_WhenUriFormatExceptionOccurs_ReturnsNull()
        {
            // Arrange
            mockISW360ApicommunicationFacade
                .Setup(x => x.GetAllReleasesWithAllData(It.IsAny<int>(), It.IsAny<int>()))
                .ThrowsAsync(new UriFormatException());

            // Act
            var result = await InvokeGetAllReleasesDetails(mockISW360ApicommunicationFacade.Object, 1, 10);

            // Assert
            Assert.IsNull(result);
        }

        [Test]
        public async Task GetAllReleasesDetails_WhenTaskCanceledExceptionOccurs_ReturnsNull()
        {
            // Arrange
            mockISW360ApicommunicationFacade
                .Setup(x => x.GetAllReleasesWithAllData(It.IsAny<int>(), It.IsAny<int>()))
                .ThrowsAsync(new TaskCanceledException());

            // Act
            var result = await InvokeGetAllReleasesDetails(mockISW360ApicommunicationFacade.Object, 1, 10);

            // Assert
            Assert.IsNull(result);
        }

        private async Task<ReleasesAllDetails> InvokeGetAllReleasesDetails(ISW360ApicommunicationFacade facade, int page, int pageEntries)
        {
            // Use reflection to invoke the private static method
            var method = typeof(CreatorValidator).GetMethod("GetAllReleasesDetails", BindingFlags.NonPublic | BindingFlags.Static);
            return await (Task<ReleasesAllDetails>)method.Invoke(null, new object[] { facade, page, pageEntries });
        }


    }
}
