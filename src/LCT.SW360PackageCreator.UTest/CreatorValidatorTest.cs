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

namespace SW360ComponentCreator.UTest
{
    [TestFixture]
    public class CreatorValidatorTest
    {
        public Mock<ISw360ProjectService> mockISw360ProjectService;
        public CreatorValidatorTest()
        {
            mockISw360ProjectService = new Mock<ISw360ProjectService>(MockBehavior.Strict);

        }
        [TestCase]
        public async Task ValidateAppSettings_TestPositive()
        {
            //Arrange
            ProjectReleases projectReleases = new ProjectReleases();
            projectReleases.Name = "Test";
            var CommonAppSettings = new CommonAppSettings();
            CommonAppSettings.SW360ProjectName = "Test";
            mockISw360ProjectService.Setup(x => x.GetProjectNameByProjectIDFromSW360(It.IsAny<String>(), It.IsAny<string>()))
                .ReturnsAsync(projectReleases);

            //Act
            await CreatorValidator.ValidateAppSettings(CommonAppSettings, mockISw360ProjectService.Object);

            //Assert
            mockISw360ProjectService.Verify(x => x.GetProjectNameByProjectIDFromSW360(It.IsAny<string>(), It.IsAny<string>()), Times.AtLeastOnce);

        }
        [TestCase]
        public void ValidateAppSettings_TestNegative()
        {
            //Arrange
            ProjectReleases projectReleases = new ProjectReleases();
            projectReleases.Name = null;
            var CommonAppSettings = new CommonAppSettings();
            mockISw360ProjectService.Setup(x => x.GetProjectNameByProjectIDFromSW360(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(projectReleases);

            //Assert
            Assert.ThrowsAsync<InvalidDataException>(() => CreatorValidator.ValidateAppSettings(CommonAppSettings, mockISw360ProjectService.Object));

        }

    }
}
