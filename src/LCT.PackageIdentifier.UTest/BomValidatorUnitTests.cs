﻿// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.Common.Interface;
using LCT.PackageIdentifier;
using LCT.Services.Interface;
using Moq;
using NUnit.Framework;
using System;
using System.IO;
using System.Threading.Tasks;
using LCT.Common;
using LCT.APICommunications.Model;

namespace LCT.PackageIdentifier.UTest
{
    [TestFixture]
    public class BomValidatorUnitTests
    {
        private readonly Mock<ISw360ProjectService> mockISw360ProjectService = new Mock<ISw360ProjectService>(MockBehavior.Strict);
        private readonly Mock<IFileOperations> mockIFileOperations = new Mock<IFileOperations>(MockBehavior.Strict);
        private readonly Mock<IFolderAction> mockIFolderAction = new Mock<IFolderAction>(MockBehavior.Strict);

        [TestCase]
        public async Task ValidateAppSettings_ProvidedProjectID_ReturnsProjectName()
        {
            //Arrange
            string projectName = "Test";
            ProjectReleases projectReleases = new ProjectReleases();
            var CommonAppSettings = new CommonAppSettings(mockIFolderAction.Object)
            {
                SW360ProjectName = "Test"
            };
            mockISw360ProjectService.Setup(x => x.GetProjectNameByProjectIDFromSW360(It.IsAny<String>(), It.IsAny<string>(), projectReleases))
                .ReturnsAsync(projectName);

            mockIFileOperations.Setup(x => x.ValidateFilePath(It.IsAny<string>()))
                .Callback((string message) => { })
                .Verifiable();

            mockIFolderAction.Setup(x => x.ValidateFolderPath(It.IsAny<string>()))
                .Callback((string message) => { })
                .Verifiable();
            CommonAppSettings.PackageFilePath = "";

            //Act
            await BomValidator.ValidateAppSettings(CommonAppSettings, mockISw360ProjectService.Object, projectReleases);

            //Assert
            mockISw360ProjectService.Verify(x => x.GetProjectNameByProjectIDFromSW360(It.IsAny<string>(), It.IsAny<string>(), projectReleases), Times.AtLeastOnce);

        }

        [TestCase]
        public Task ValidateAppSettings_ProvidedProjectID_ReturnsInvalidDataException()
        {
            //Arrange
            string projectName = null;
            ProjectReleases projectReleases = new ProjectReleases();
            var CommonAppSettings = new CommonAppSettings(mockIFolderAction.Object)
            {
                SW360ProjectName = "Test"
            };
            mockISw360ProjectService.Setup(x => x.GetProjectNameByProjectIDFromSW360(It.IsAny<string>(), It.IsAny<string>(),projectReleases))
                .ReturnsAsync(projectName);

            mockIFileOperations.Setup(x => x.ValidateFilePath(It.IsAny<string>()))
                .Callback((string message) => { })
                .Verifiable();

            mockIFolderAction.Setup(x => x.ValidateFolderPath(It.IsAny<string>()))
                .Callback((string message) => { })
                .Verifiable();

            //Act && Assert
            Assert.ThrowsAsync<InvalidDataException>(async () => await BomValidator.ValidateAppSettings(CommonAppSettings, mockISw360ProjectService.Object, projectReleases));
            return Task.CompletedTask;
        }

        [TestCase]
        public async Task ValidateAppSettings_ProvidedProjectID_EndsTheApplicationOnClosedProject()
        {
            //Arrange
            string projectName = "Test";
            ProjectReleases projectReleases = new ProjectReleases();
            projectReleases.clearingState = "CLOSED";
            var CommonAppSettings = new CommonAppSettings(mockIFolderAction.Object)
            {
                SW360ProjectName = "Test"
            };
            mockISw360ProjectService.Setup(x => x.GetProjectNameByProjectIDFromSW360(It.IsAny<String>(), It.IsAny<string>(), projectReleases))
                .ReturnsAsync(projectName);

            mockIFileOperations.Setup(x => x.ValidateFilePath(It.IsAny<string>()))
                .Callback((string message) => { })
                .Verifiable();

            mockIFolderAction.Setup(x => x.ValidateFolderPath(It.IsAny<string>()))
                .Callback((string message) => { })
                .Verifiable();
            CommonAppSettings.PackageFilePath = "";

            //Act
            await BomValidator.ValidateAppSettings(CommonAppSettings, mockISw360ProjectService.Object, projectReleases);

            //Assert
            mockISw360ProjectService.Verify(x => x.GetProjectNameByProjectIDFromSW360(It.IsAny<string>(), It.IsAny<string>(), projectReleases), Times.AtLeastOnce);

        }
    }
}
