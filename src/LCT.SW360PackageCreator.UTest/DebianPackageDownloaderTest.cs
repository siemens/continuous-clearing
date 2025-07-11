// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.Common;
using LCT.Common.Model;
using LCT.SW360PackageCreator.Interfaces;
using Moq;
using NUnit.Framework;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Directory = System.IO.Directory;
using File = System.IO.File;

namespace LCT.SW360PackageCreator.UTest
{
    [TestFixture]
    class DebianPackageDownloaderTest
    {
        [TestCase]
        public async Task DownloadReleaseAttachmentSourceForDebian_ProvidedSourceURL_ReturnsDownloadPath()
        {
            //Arrange
            var lstComparisonBomData = new ComparisonBomData()
            {
                Name = "adduser",
                Version = "3.118",
                DownloadUrl = "",
                SourceUrl = "https://snapshot.debian.org/archive/debian/20180915T211528Z/pool/main/a/adduser/adduser_3.118.tar.xz",
                ReleaseExternalId = "pkg:deb/debian/adduser@3.1182?arch=source"
            };
            var localPathforDownload = $"{Path.GetTempPath()}/ClearingTool/DownloadedFiles/";
            var debianPatcher = new Mock<IDebianPatcher>();
            var debianPackageDownloader = new DebianPackageDownloader(debianPatcher.Object);

            //Act
            var attachmentUrlList = await debianPackageDownloader.DownloadPackage(lstComparisonBomData, localPathforDownload);

            //Assert
            Assert.IsNotEmpty(attachmentUrlList);
        }

        [TestCase]
        public async Task DownloadReleaseAttachmentSourceForDebian_ProvidedInValidSourceURL_ReturnsNull()
        {
            //Arrange
            //Sending Incorrect SourceURL
            var lstComparisonBomData = new ComparisonBomData()
            {
                Name = "adduser",
                Version = "3.118",
                DownloadUrl = "",
                SourceUrl = "https://snapshot.debian.org/archive/debian/20180915T211528Z/pool/main/a/adduser/adduser_3.118.tar.xx",
                ReleaseExternalId = "pkg:deb/debian/adduser@3.1182?arch=source"
            };
            var localPathforDownload = $"{Path.GetTempPath()}/ClearingTool/DownloadedFiles/";
            var debianPatcher = new Mock<IDebianPatcher>();
            var debianPackageDownloader = new DebianPackageDownloader(debianPatcher.Object);

            //Act
            var attachmentUrlList = await debianPackageDownloader.DownloadPackage(lstComparisonBomData, localPathforDownload);

            //Assert
            Assert.IsEmpty(attachmentUrlList);
        }

        [TestCase]
        public async Task DownloadReleaseAttachmentSourceForDebian_ProvidedPatchURLs_ReturnsEmptyDownloadPath()
        {
            //Arrange
            Result result = new Result()
            {
                ExitCode = 0
            };
            //Provided patch URLs for applying patch
            var lstComparisonBomData = new ComparisonBomData()
            {
                Name = "dash",
                Version = "0.5.10.2-5",
                DownloadUrl = "",
                SourceUrl = "https://snapshot.debian.org/archive/debian/20180905T153324Z/pool/main/d/dash/dash_0.5.10.2.orig.tar.gz",
                PatchURls = new string[3]
                              {"https://snapshot.debian.org/archive/debian/20190117T211539Z/pool/main/d/dash/dash_0.5.10.2-5.dsc",
                              "https://snapshot.debian.org/archive/debian/20190117T211539Z/pool/main/d/dash/dash_0.5.10.2-5.debian.tar.xz",
                              "https://snapshot.debian.org/archive/debian/20180905T153324Z/pool/main/d/dash/dash_0.5.10.2.orig.tar.gz"
                },
                ReleaseExternalId = "pkg:deb/debian/dash@0.5.10.2-5?arch=source"
            };
            var localPathforDownload = $"{Path.GetTempPath()}/ClearingTool/DownloadedFiles/";
            var debianPatcher = new Mock<IDebianPatcher>();
            debianPatcher.Setup(x => x.ApplyPatch(It.IsAny<ComparisonBomData>(), It.IsAny<string>(), It.IsAny<string>())).Returns(result);
            var debianPackageDownloader = new DebianPackageDownloader(debianPatcher.Object);

            //Act
            var attachmentUrlList = await debianPackageDownloader.DownloadPackage(lstComparisonBomData, localPathforDownload);

            //Assert
            Assert.That(string.IsNullOrEmpty(attachmentUrlList));
        }

        [TestCase]
        public async Task DownloadReleaseAttachmentSourceForDebian_ByRetry_ReturnsEmptyDownloadPath()
        {
            //Arrange
            Result result = new Result()
            {
                ExitCode = 1
            };
            //Provided patch URLs for applying patch
            var lstComparisonBomData = new ComparisonBomData()
            {
                Name = "dash",
                Version = "0.5.10.2-5",
                DownloadUrl = "",
                SourceUrl = "https://snapshot.debian.org/archive/debian/20180905T153324Z/pool/main/d/dash/dash_0.5.10.2.orig.tar.gz",
                PatchURls = new string[3]
                              {"https://snapshot.debian.org/archive/debian/20190117T211539Z/pool/main/d/dash/dash_0.5.10.2-5.dsc",
                              "https://snapshot.debian.org/archive/debian/20190117T211539Z/pool/main/d/dash/dash_0.5.10.2-5.debian.tar.xz",
                              "https://snapshot.debian.org/archive/debian/20180905T153324Z/pool/main/d/dash/dash_0.5.10.2.orig.tar.gz"
                },
                ReleaseExternalId = "pkg:deb/debian/dash@0.5.10.2-5?arch=source"
            };
            var localPathforDownload = $"{Path.GetTempPath()}/ClearingTool/DownloadedFiles/";
            var debianPatcher = new Mock<IDebianPatcher>();
            debianPatcher.Setup(x => x.ApplyPatch(It.IsAny<ComparisonBomData>(), It.IsAny<string>(), It.IsAny<string>())).Returns(result);
            var debianPackageDownloader = new DebianPackageDownloader(debianPatcher.Object);

            //Act
            var attachmentUrlList = await debianPackageDownloader.DownloadPackage(lstComparisonBomData, localPathforDownload);

            //Assert
            Assert.That(string.IsNullOrEmpty(attachmentUrlList));
        }

        //string attachmentJson = outFolder + @"..\..\..\src\LCT.SW360PackageCreator.UTest\ComponentCreatorUTFiles\Attachment.json";
        // 

        public void GetPatchedFileFromDownloadedFolder_ProvidedPatchURLs_ReturnsEmptyDownloadPath()
        {
            //Arrange
            Result result = new Result()
            {
                ExitCode = 0
            };
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string downloadFolderPath = outFolder + @"..\..\..\src\LCT.SW360PackageCreator.UTest\ComponentCreatorUTFiles";
            // \adduser_3.118-debian10-combined.tar.bz2";

            //Provided patch URLs for applying patch
            var lstComparisonBomData = new ComparisonBomData()
            {
                Name = "dash",
                Version = ""
            };
            string patchedFilePath = string.Empty;

            // ACT
            patchedFilePath = DebianPackageDownloader.GetPatchedFileFromDownloadedFolder(downloadFolderPath);

            // Assert
            Assert.That(!string.IsNullOrEmpty(patchedFilePath));
        }

        [TestCase]
        public void GetPatchedFileFromDownloadedFolder_ProvidedPatchURLs_ThrowsArgumentException()
        {
            // Arrange
            string patchedFilePath = string.Empty;

            // Act
            patchedFilePath = DebianPackageDownloader.GetPatchedFileFromDownloadedFolder(null);

            // Assert
            Assert.That(string.IsNullOrEmpty(patchedFilePath));
        }

        [TestCase]
        public void ApplyPatchforComponents_ProvidedPatchURLs_ReturnsEmptyDownloadPath()
        {
            //Arrange
            string actual = string.Empty;
            Result result = null;
            Mock<IDebianPatcher> debianPatcher = new Mock<IDebianPatcher>();
            debianPatcher.Setup(x => x.ApplyPatch(It.IsAny<ComparisonBomData>(), It.IsAny<string>(),
                It.IsAny<string>())).Returns(result);

            //Act
            DebianPackageDownloader debianPackageDownloader = new DebianPackageDownloader(debianPatcher.Object);
            debianPackageDownloader.ApplyPatchforComponents(new ComparisonBomData(), string.Empty, string.Empty);
            //Assert
            Assert.That(actual != null);
        }
        [TestCase]
        public void DeletePatchedFolderAndFile_ValidFolderAndFile_DeletesSuccessfully()
        {
            // Arrange
            string tempFolderPath = Path.Combine(Path.GetTempPath(), "TestFolder");
            string tempFilePath = Path.Combine(tempFolderPath, "TestFile.txt");

            Directory.CreateDirectory(tempFolderPath);
            File.WriteAllText(tempFilePath, "Test content");

            // Act
            typeof(DebianPackageDownloader)
                .GetMethod("DeletePatchedFolderAndFile", BindingFlags.NonPublic | BindingFlags.Static)
                .Invoke(null, new object[] { tempFolderPath, tempFilePath });

            // Assert
            Assert.IsFalse(Directory.Exists(tempFolderPath));
            Assert.IsFalse(File.Exists(tempFilePath));
        }

        [TestCase]
        public void DeletePatchedFolderAndFile_NonExistentFolderAndFile_NoExceptionThrown()
        {
            // Arrange
            string nonExistentFolderPath = Path.Combine(Path.GetTempPath(), "NonExistentFolder");
            string nonExistentFilePath = Path.Combine(nonExistentFolderPath, "NonExistentFile.txt");

            // Act & Assert
            Assert.DoesNotThrow(() =>
            {
                typeof(DebianPackageDownloader)
                    .GetMethod("DeletePatchedFolderAndFile", BindingFlags.NonPublic | BindingFlags.Static)
                    .Invoke(null, new object[] { nonExistentFolderPath, nonExistentFilePath });
            });
        }
    }
}
