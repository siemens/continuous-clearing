// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using NUnit.Framework;
using System.IO;

namespace LCT.Common.UTest
{
    [TestFixture]
    public class FolderActionTest
    {

        [Test]
        public void ValidateFolderPath_WhenFolderPathIsEmpty_ThrowsArgumentException()
        {
            //Arrange
            var folderAction = new FolderAction();
            //Assert
            Assert.Throws<System.ArgumentException>(() => folderAction.ValidateFolderPath(""));
        }

        [Test]
        public void ValidateFolderPath_WhenFolderPathIsNotProper_ThrowsDirectoryNotFoundException()
        {
            //Arrange
            var folderAction = new FolderAction();
            //Assert
            Assert.Throws<System.IO.DirectoryNotFoundException>(() => folderAction.ValidateFolderPath("test"));
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
        [Test]
        public void CopyToTargetDirectory_WhenIOExceptionOccurs_ReturnsFalse()
        {
            // Arrange
            string sourcePath = $"{Path.GetTempPath()}/sourcePath/";
            string targetPath = $"{Path.GetTempPath()}/targetPath/";
            var folderAction = new FolderAction();

            // Simulate IOException by creating a file with the same name as the target directory
            File.WriteAllText(targetPath, "This will cause an IOException");

            // Act
            var actual = folderAction.CopyToTargetDirectory(sourcePath, targetPath);

            // Assert
            Assert.IsFalse(actual);

            // Cleanup
            File.Delete(targetPath);
        }

        [Test]
        public void CopyToTargetDirectory_WhenSecurityExceptionOccurs_ReturnsFalse()
        {
            // Arrange
            string sourcePath = $"{Path.GetTempPath()}/sourcePath/";
            string targetPath = $"{Path.GetTempPath()}/targetPath/";
            var folderAction = new FolderAction();

            // Simulate SecurityException by using a restricted directory (e.g., root of C drive)
            string restrictedPath = "C:\\RestrictedPath";

            // Act
            var actual = folderAction.CopyToTargetDirectory(sourcePath, restrictedPath);

            // Assert
            Assert.IsFalse(actual);
        }

    }
}
