// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.Services.Interface;
using LCT.SW360PackageCreator;
using Moq;
using NUnit.Framework;
using System;
using System.IO;
using LCT.Common;
using System.Threading.Tasks;
using LCT.APICommunications.Model;
using LCT.Facade.Interfaces;
using LCT.APICommunications.Model.Foss;
using Newtonsoft.Json;
using System.Net.Http;

namespace LCT.SW360PackageCreator.UTest
{
    [TestFixture]
    public class CreatorValidatorTest
    {
        public Mock<ISw360ProjectService> mockISw360ProjectService;
        public Mock<ISW360ApicommunicationFacade> mockISW360ApicommunicationFacade;
        public Mock<ISW360CommonService> mockISW360CommonService;
        public Mock<ISw360CreatorService> mockISw360CreatorService;
        public Mock<ISW360Service> mockISW360Service;
        public CreatorValidatorTest()
        {
            mockISw360ProjectService = new Mock<ISw360ProjectService>(MockBehavior.Strict);
            mockISW360ApicommunicationFacade = new Mock<ISW360ApicommunicationFacade>(MockBehavior.Strict);
            mockISW360CommonService = new Mock<ISW360CommonService>(MockBehavior.Strict);
            mockISw360CreatorService = new Mock<ISw360CreatorService>(MockBehavior.Strict);
            mockISW360Service = new Mock<ISW360Service>(MockBehavior.Strict);

        }
        [TestCase]
        public async Task ValidateAppSettings_TestPositive()
        {
            //Arrange
            string projectName = "Test";
            ProjectReleases projectReleases=new ProjectReleases();
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
            await CreatorValidator.ValidateAppSettings(CommonAppSettings, mockISw360ProjectService.Object,projectReleases);

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
                   ProjectName= "Test"
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
                SW360=new SW360()
            };
            mockISw360ProjectService.Setup(x => x.GetProjectNameByProjectIDFromSW360(It.IsAny<string>(), It.IsAny<string>(),projectReleases))
                .ReturnsAsync(projectName);

            //Act

            //Assert
            Assert.ThrowsAsync<InvalidDataException>(() => CreatorValidator.ValidateAppSettings(CommonAppSettings, mockISw360ProjectService.Object, projectReleases));

        }
        [TestCase]
        public async Task TriggerFossologyValidation_TestPositive()
        {
            // Arrange
            var appSettings = new CommonAppSettings()
            {
                SW360 = new SW360()
                {
                    URL = "https://sw360.example.com"
                }
            };
            var responseBody = "{\"_embedded\":{\"sw360:releases\":[{\"_links\":{\"self\":{\"href\":\"https://sw360.example.com/resource/api/releases/123\"}}}]}}";
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
            var triggerStatusResponse = JsonConvert.SerializeObject(fossTriggerStatus);
            mockISW360ApicommunicationFacade.Setup(x => x.GetReleases()).ReturnsAsync(responseBody);
            mockISW360Service.Setup(x => x.GetReleaseDataOfComponent(It.IsAny<string>())).ReturnsAsync(releasesInfo);
            mockISw360CreatorService.Setup(x => x.TriggerFossologyProcessForValidation(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(fossTriggerStatus);
            mockISW360ApicommunicationFacade.Setup(x => x.GetReleaseById(It.IsAny<string>())).ReturnsAsync(new HttpResponseMessage
            {
                Content = new StringContent("{\"Name\":\"TestRelease\",\"Version\":\"1.0\",\"ClearingState\":\"APPROVED\"}")
            });
            mockISW360ApicommunicationFacade.Setup(x => x.TriggerFossologyProcess(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(triggerStatusResponse);
            // Act
            await CreatorValidator.TriggerFossologyValidation(appSettings, mockISW360ApicommunicationFacade.Object);
            // Assert
            mockISW360ApicommunicationFacade.Verify(x => x.GetReleases(), Times.Once);
            mockISW360ApicommunicationFacade.Verify(x => x.TriggerFossologyProcess(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

    }
}
