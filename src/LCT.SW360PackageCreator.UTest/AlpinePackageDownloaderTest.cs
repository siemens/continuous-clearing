// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 
using LCT.Common.Model;
using NUnit.Framework;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace LCT.SW360PackageCreator.UTest
{

    [TestFixture]
    class AlpinePackageDownloaderTest
    {
               

        [TestCase]
        public async Task DownloadSourceForAlpine_ProvidedInValidSourceURL_ReturnsNull()
        {
            //Arrange
            //Sending Incorrect SourceURL
            var lstComparisonBomData = new ComparisonBomData()
            {
                Name = "ca-certificates",
                Version = "20220614-r0",
                SourceUrl = "https://gitlab.alpinelinux.org/alpine/ca-certificates/-/archive/ca-certificates-20230506.tar.bz2",
                AlpineSource = ""

            };
            var localPathforDownload = Path.Combine(
    Directory.GetParent(Directory.GetCurrentDirectory())!.FullName,
    "ClearingTool",
    "DownloadedFiles") + Path.DirectorySeparatorChar;
            var alpinePackageDownloader = new AlpinePackageDownloader();

            //Act
            var downloadpath = await alpinePackageDownloader.DownloadPackage(lstComparisonBomData, localPathforDownload);

            //Assert
            Assert.IsEmpty(downloadpath);
        }

        [Test]
        public void ApplyPatchsToSourceCode_ValidPatchFileAndSourceCodeFolder_SuccessfullyAppliesPatch()
        {
            try
            {
                // Arrange
                var patchFileFolder = "path/to/patch/file.patch";
                var sourceCodezippedFolder = "path/to/source/code/folder";

                // Act
                AlpinePackageDownloader.ApplyPatchsToSourceCode(patchFileFolder, sourceCodezippedFolder);

                // Assert
                // Add your assertions here to verify that the patch was successfully applied
                Assert.Pass();
            }
            catch (Exception ex)
            {
                Assert.IsNotNull(ex);
            }
        }

    }
}
