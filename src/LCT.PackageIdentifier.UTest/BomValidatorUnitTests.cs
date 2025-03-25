// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.APICommunications.Model;
using LCT.Common;
using LCT.Common.Interface;
using LCT.Services.Interface;
using Moq;
using NUnit.Framework;
using System;
using System.IO;
using System.Threading.Tasks;

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
            IFolderAction folderAction = new FolderAction();
            IFileOperations fileOperations = new FileOperations();

            mockISw360ProjectService.Setup(x => x.GetProjectNameByProjectIDFromSW360(It.IsAny<String>(), It.IsAny<string>(), projectReleases))
                .ReturnsAsync(projectName);

            mockIFileOperations.Setup(x => x.ValidateFilePath(It.IsAny<string>()))
                .Callback((string message) => { })
                .Verifiable();

            mockIFolderAction.Setup(x => x.ValidateFolderPath(It.IsAny<string>()))
                .Callback((string message) => { })
                .Verifiable();
            var CommonAppSettings = new CommonAppSettings(mockIFolderAction.Object, mockIFileOperations.Object)
            {
                SW360 = new SW360() { ProjectName = "Test" },
                Directory = new LCT.Common.Directory(mockIFolderAction.Object, mockIFileOperations.Object)
                {
                    InputFolder = ""
                }
            };
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
            IFolderAction folderAction = new FolderAction();
            IFileOperations fileOperations = new FileOperations();

            mockISw360ProjectService.Setup(x => x.GetProjectNameByProjectIDFromSW360(It.IsAny<string>(), It.IsAny<string>(), projectReleases))
                .ReturnsAsync(projectName);

            mockIFileOperations.Setup(x => x.ValidateFilePath(It.IsAny<string>()))
                .Callback((string message) => { })
                .Verifiable();

            mockIFolderAction.Setup(x => x.ValidateFolderPath(It.IsAny<string>()))
                .Callback((string message) => { })
                .Verifiable();

            var CommonAppSettings = new CommonAppSettings(mockIFolderAction.Object, mockIFileOperations.Object)
            {
                SW360 = new SW360() { ProjectName = "Test" },
                Directory = new LCT.Common.Directory(mockIFolderAction.Object, mockIFileOperations.Object)
                {
                    InputFolder = ""
                }
            };
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

            mockISw360ProjectService.Setup(x => x.GetProjectNameByProjectIDFromSW360(It.IsAny<String>(), It.IsAny<string>(), projectReleases))
                .ReturnsAsync(projectName);

            mockIFileOperations.Setup(x => x.ValidateFilePath(It.IsAny<string>()))
                .Callback((string message) => { })
                .Verifiable();

            mockIFolderAction.Setup(x => x.ValidateFolderPath(It.IsAny<string>()))
                .Callback((string message) => { })
                .Verifiable();
            IFolderAction folderAction = new FolderAction();
            IFileOperations fileOperations = new FileOperations();
            var CommonAppSettings = new CommonAppSettings(mockIFolderAction.Object, mockIFileOperations.Object)
            {
                SW360 = new SW360() { ProjectName = "Test" },
                Directory = new LCT.Common.Directory(mockIFolderAction.Object, mockIFileOperations.Object)
                {
                    InputFolder = ""
                }
            };

            //Act
            await BomValidator.ValidateAppSettings(CommonAppSettings, mockISw360ProjectService.Object, projectReleases);

            //Assert
            mockISw360ProjectService.Verify(x => x.GetProjectNameByProjectIDFromSW360(It.IsAny<string>(), It.IsAny<string>(), projectReleases), Times.AtLeastOnce);

        }
    }
}
