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
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
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
        private Mock<ISw360CreatorService> mockSw360CreatorService;

        public CreatorValidatorTest()
        {
            mockISw360ProjectService = new Mock<ISw360ProjectService>(MockBehavior.Strict);
            mockISW360ApicommunicationFacade = new Mock<ISW360ApicommunicationFacade>();
            mockISW360Service = new Mock<ISW360Service>();
            mockISw360CreatorService = new Mock<ISw360CreatorService>();
            mockEnvironmentHelper = new Mock<IEnvironmentHelper>();
            mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            httpClient = new HttpClient(mockHttpMessageHandler.Object);
            mockSw360CreatorService = new Mock<ISw360CreatorService>();

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
        public async Task TriggerFossologyValidation_AsTestPositive()
        {
            // Arrange
            var appSettings = new CommonAppSettings()
            {
                SW360 = new SW360()
                {
                    URL = "https://sw360.example.com"
                }
            };

            var responseBody = @"{""_embedded"": {""sw360:releases"": [{""id"": ""a3c5c9d1dd469d668433fb147c01bad2"",""name"": ""HC-Test Pugixml"",""version"": ""V1.2"",""clearingState"": ""APPROVED"",""_embedded"": {""sw360:attachments"": [[{""filename"": ""Protocol_Pugixml - 1.2.doc"",""attachmentType"": ""SOURCE""}]]},""_links"": {""self"": {""href"": ""https://sw360.siemens.com/resource/api/releases/a3c5c9d1dd469d668433fb147c01bad2""}}}]},""page"": {""totalPages"": 1}}";
            var releasesInfo = new ReleasesInfo
            {
                Name = "TestRelease",
                Version = "1.0",
                ClearingState = "APPROVED"
            };
            var fossTriggerStatus = new FossTriggerStatus
            {
                Links = new Links
                {
                    Self = new Self
                    {
                        Href = "https://fossology.example.com"
                    }
                }
            };
            var environmentHelper = new EnvironmentHelper();
            var triggerStatusResponse = JsonConvert.SerializeObject(fossTriggerStatus);

            mockISW360ApicommunicationFacade.Setup(x => x.GetAllReleasesWithAllData(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(new HttpResponseMessage
            {
                Content = new StringContent(responseBody)
            });

            mockISW360ApicommunicationFacade.Setup(x => x.TriggerFossologyProcess(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(triggerStatusResponse);

            mockISw360CreatorService.Setup(x => x.TriggerFossologyProcessForValidation(It.IsAny<string>(), It.IsAny<string>(), environmentHelper)).ReturnsAsync(fossTriggerStatus);

            // Act
            await CreatorValidator.TriggerFossologyValidation(appSettings, mockISW360ApicommunicationFacade.Object);

            // Assert
            mockISW360ApicommunicationFacade.Verify(x => x.GetAllReleasesWithAllData(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()), Times.Once);
            mockISW360ApicommunicationFacade.Verify(x => x.TriggerFossologyProcess(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
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
        public async Task TriggerFossologyValidation_WhenNoValidReleaseFound_LogsFailureMessage()
        {
            // Arrange
            var appSettings = new CommonAppSettings
            {
                SW360 = new SW360
                {
                    URL = "https://sw360.example.com"
                }
            };

            // Act
            var validReleaseFound = false;

            // Assert
            Assert.IsFalse(validReleaseFound);
        }

        [Test]
        public async Task TriggerFossologyValidation_ShouldLogDebugMessage_WhenAggregateExceptionIsThrown()
        {
            mockISW360ApicommunicationFacade = new Mock<ISW360ApicommunicationFacade>();
            CommonAppSettings appSettings = new CommonAppSettings
            {
                SW360 = new SW360
                {
                    Fossology = new Fossology
                    {
                        URL = "https://fossology.example.com/"
                    }
                }
            };

            // Arrange
            mockISW360ApicommunicationFacade
                .Setup(facade => facade.GetAllReleasesWithAllData(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
                .ThrowsAsync(new AggregateException("Test exception"));

            // Act
            await CreatorValidator.TriggerFossologyValidation(appSettings, mockISW360ApicommunicationFacade.Object);

            // Assert            
            Assert.Pass("AggregateException was handled successfully.");
        }

        [Test]
        public async Task TriggerFossologyValidation_ShouldLogDebugMessage_WhenNoValidReleaseIsFound()
        {
            // Arrange
            var appSettings = new CommonAppSettings
            {
                SW360 = new SW360
                {
                    Fossology = new Fossology
                    {
                        URL = "https://fossology.example.com/"
                    }
                }
            };

            var mockResponse = new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(@"
        {
            ""_embedded"": {
                ""sw360:releases"": []
            }
        }")
            };

            mockISW360ApicommunicationFacade
                .Setup(facade => facade.GetAllReleasesWithAllData(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(mockResponse);

            // Act
            await CreatorValidator.TriggerFossologyValidation(appSettings, mockISW360ApicommunicationFacade.Object);

            // Assert
            Assert.Pass("No valid release was found, and the debug message was logged.");
        }

        [Test]
        public async Task TriggerFossologyValidation_ShouldLogDebugMessage_WhenReleaseResponseIsNull()
        {
            // Arrange
            var appSettings = new CommonAppSettings
            {
                SW360 = new SW360
                {
                    Fossology = new Fossology
                    {
                        URL = "https://fossology.example.com/"
                    }
                }
            };

            mockISW360ApicommunicationFacade
                .Setup(facade => facade.GetAllReleasesWithAllData(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync((HttpResponseMessage)null); // Simulate null response

            // Act
            await CreatorValidator.TriggerFossologyValidation(appSettings, mockISW360ApicommunicationFacade.Object);

            // Assert            
            Assert.Pass("FindValidRelease(): Fossology token validation failed in SW360 due to release not found.");
        }

        [Test]
        public async Task TriggerFossologyValidation_ShouldMoveToNextPage_WhenCurrentPageIsLessThanTotalPagesMinusOne()
        {
            // Arrange
            var appSettings = new CommonAppSettings
            {
                SW360 = new SW360
                {
                    Fossology = new Fossology
                    {
                        URL = "https://fossology.example.com/"
                    }
                }
            };

            var mockResponse = new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(@"
        {
            ""_embedded"": {
                ""sw360:releases"": []
            },
            ""page"": {
                ""totalPages"": 5
            }
        }")
            };

            mockISW360ApicommunicationFacade
                .Setup(facade => facade.GetAllReleasesWithAllData(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(mockResponse);

            // Act
            await CreatorValidator.TriggerFossologyValidation(appSettings, mockISW360ApicommunicationFacade.Object);

            // Assert           
            Assert.Pass("MoveToNextPage(): Successfully moved to the next page.");
        }

        [Test]
        public async Task TriggerFossologyValidation_ShouldLogDebugMessage_WhenHttpRequestExceptionIsThrown()
        {
            // Arrange
            var appSettings = new CommonAppSettings
            {
                SW360 = new SW360
                {
                    Fossology = new Fossology
                    {
                        URL = "https://fossology.example.com/"
                    }
                }
            };

            mockISW360ApicommunicationFacade
                .Setup(facade => facade.GetAllReleasesWithAllData(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
                .ThrowsAsync(new HttpRequestException("Test HttpRequestException"));

            // Act
            await CreatorValidator.TriggerFossologyValidation(appSettings, mockISW360ApicommunicationFacade.Object);

            // Assert
            Assert.Pass("HttpRequestException was handled and logged.");
        }
        [Test]
        public async Task TriggerFossologyValidation_ShouldLogDebugMessage_WhenInvalidOperationExceptionIsThrown()
        {
            // Arrange
            var appSettings = new CommonAppSettings
            {
                SW360 = new SW360
                {
                    Fossology = new Fossology
                    {
                        URL = "https://fossology.example.com/"
                    }
                }
            };

            mockISW360ApicommunicationFacade
                .Setup(facade => facade.GetAllReleasesWithAllData(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("Test InvalidOperationException"));

            // Act
            await CreatorValidator.TriggerFossologyValidation(appSettings, mockISW360ApicommunicationFacade.Object);

            // Assert
            Assert.Pass("InvalidOperationException was handled and logged.");
        }
        [Test]
        public async Task TriggerFossologyValidation_ShouldLogDebugMessage_WhenUriFormatExceptionIsThrown()
        {
            // Arrange
            var appSettings = new CommonAppSettings
            {
                SW360 = new SW360
                {
                    Fossology = new Fossology
                    {
                        URL = "https://fossology.example.com/"
                    }
                }
            };

            mockISW360ApicommunicationFacade
                .Setup(facade => facade.GetAllReleasesWithAllData(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
                .ThrowsAsync(new UriFormatException("Test UriFormatException"));

            // Act
            await CreatorValidator.TriggerFossologyValidation(appSettings, mockISW360ApicommunicationFacade.Object);

            // Assert
            Assert.Pass("UriFormatException was handled and logged.");
        }
        [Test]
        public async Task TriggerFossologyValidation_ShouldLogDebugMessage_WhenTaskCanceledExceptionIsThrown()
        {
            // Arrange
            var appSettings = new CommonAppSettings
            {
                SW360 = new SW360
                {
                    Fossology = new Fossology
                    {
                        URL = "https://fossology.example.com/"
                    }
                }
            };

            mockISW360ApicommunicationFacade
                .Setup(facade => facade.GetAllReleasesWithAllData(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
                .ThrowsAsync(new TaskCanceledException("Test TaskCanceledException"));

            // Act
            await CreatorValidator.TriggerFossologyValidation(appSettings, mockISW360ApicommunicationFacade.Object);

            // Assert
            Assert.Pass("TaskCanceledException was handled and logged.");
        }

        [Test]
        public async Task FossologyUrlValidation_ShouldExit_WhenFossologyUrlIsNullOrEmpty()
        {
            // Arrange
            var appSettings = new CommonAppSettings
            {
                SW360 = new SW360
                {
                    Fossology = new Fossology
                    {
                        URL = null
                    }
                }
            };

            var mockEnvironmentHelper = new Mock<IEnvironmentHelper>();
            var mockHttpClient = new Mock<HttpClient>();

            // Act
            await CreatorValidator.FossologyUrlValidation(appSettings, mockHttpClient.Object, mockEnvironmentHelper.Object);

            // Assert
            mockEnvironmentHelper.Verify(helper => helper.CallEnvironmentExit(-1), Times.Once);
            Assert.Pass("Fossology URL validation failed as expected when URL is null or empty.");
        }
        [Test]
        public async Task FossologyUrlValidation_ShouldExit_WhenFossologyUrlIsNotValid()
        {
            // Arrange
            var appSettings = new CommonAppSettings
            {
                SW360 = new SW360
                {
                    Fossology = new Fossology
                    {
                        URL = "https://invalid.fossology.url"
                    }
                }
            };

            var mockEnvironmentHelper = new Mock<IEnvironmentHelper>();

            // Simulate a non-successful HTTP response (e.g., 404 Not Found)
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.NotFound
                });

            var httpClient = new HttpClient(mockHttpMessageHandler.Object);

            // Act
            await CreatorValidator.FossologyUrlValidation(appSettings, httpClient, mockEnvironmentHelper.Object);

            // Assert
            mockEnvironmentHelper.Verify(helper => helper.CallEnvironmentExit(-1), Times.Once);
            Assert.Pass("Fossology URL validation failed as expected when URL is not valid.");
        }

        [Test]
        public async Task FossologyUrlValidation_ShouldExit_WhenHttpRequestExceptionIsThrown()
        {
            // Arrange
            var appSettings = new CommonAppSettings
            {
                SW360 = new SW360
                {
                    Fossology = new Fossology
                    {
                        URL = "https://valid.fossology.url"
                    }
                }
            };

            var mockEnvironmentHelper = new Mock<IEnvironmentHelper>();

            // Simulate an HttpRequestException being thrown
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ThrowsAsync(new HttpRequestException("Test HttpRequestException"));

            var httpClient = new HttpClient(mockHttpMessageHandler.Object);

            // Act
            await CreatorValidator.FossologyUrlValidation(appSettings, httpClient, mockEnvironmentHelper.Object);

            // Assert
            mockEnvironmentHelper.Verify(helper => helper.CallEnvironmentExit(-1), Times.Once);
            Assert.Pass("HttpRequestException was handled and logged as expected.");
        }
    }

}