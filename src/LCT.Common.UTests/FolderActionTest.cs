// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.Common.Interface;
using Moq;
using NUnit.Framework;
using System;
using System.IO;

namespace LCT.Common.UTest
{
    [TestFixture]
    public class FolderActionTest
    {

        [Test]
        public void ValidateFolderPath_WhenFolderPathIsEmpty_ApplicationExit()
        {
            // Arrange
            var folderAction = new FolderAction();
            Mock<IEnvironmentHelper> environmentHelperMock = new Mock<IEnvironmentHelper>();
            environmentHelperMock.Setup(x => x.CallEnvironmentExit(-1));

            // Act & Assert
            Assert.Throws<System.IO.DirectoryNotFoundException>(() => folderAction.ValidateFolderPath("", environmentHelperMock.Object));
            environmentHelperMock.Verify(x => x.CallEnvironmentExit(-1), Times.Exactly(1));
        }

        [Test]
        public void ValidateFolderPath_WhenFolderPathIsNotProper_ThrowsDirectoryNotFoundException()
        {
            //Arrange
            var folderAction = new FolderAction();
            Mock<IEnvironmentHelper> environmentHelperMock = new Mock<IEnvironmentHelper>();
            environmentHelperMock.Setup(x => x.CallEnvironmentExit(-1));
            //Assert
            Assert.Throws<System.IO.DirectoryNotFoundException>(() => folderAction.ValidateFolderPath("test", environmentHelperMock.Object));
        }
        [Test]
        public void ZipFileToTargetDirectory_WhenPathIsNotProper_ReturnsFalse()
        {
            //Arrange
            string filePath = "dummypath";
            var folderAction = new FolderAction();

            var acutal = folderAction.ZipFileToTargetDirectory(filePath);

            //Assert
            Assert.IsFalse(acutal);
        }
        [Test]
        public void ZipFileToTargetDirectory_WhenPathIsEmpty_ThrowsArgumentExeception()
        {
            //Arrange
            string filePath = "";
            var folderAction = new FolderAction();

            //Assert
            Assert.Throws<System.ArgumentException>(() => folderAction.ZipFileToTargetDirectory(filePath));
        }

        [Test]
        public void CopyToTargetDirectory_PassingDirs_ReturnSuccess()
        {
            //Arrange
            string sourcePath = $"{Path.GetTempPath()}/sourcePath/";
            string targetPath = $"{Path.GetTempPath()}/targetPath/";
            var folderAction = new FolderAction();

            //Act
            var acutal = folderAction.CopyToTargetDirectory(sourcePath, targetPath);

            //Assert
            Assert.IsTrue(acutal);
        }

        [Test]
        public void CopyToTargetDirectory_PassingDirsWithFiles_ReturnSuccess()
        {
            //Arrange
            string sourcePath = $"{Path.GetTempPath()}\\SampleFolder";
            System.IO.Directory.CreateDirectory(sourcePath);
            System.IO.Directory.CreateDirectory(sourcePath + "\\SampleSubFolder");
            File.WriteAllText(sourcePath + "\\Sample.txt", "");
            string targetPath = $"{Path.GetTempPath()}/targetPath/";
            var folderAction = new FolderAction();

            //Act
            var acutal = folderAction.CopyToTargetDirectory(sourcePath, targetPath);

            //Assert
            Assert.IsTrue(acutal);
        }

    }
}
