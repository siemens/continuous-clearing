// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 
using LCT.Common.Model;
using NUnit.Framework;
using System.IO;
using System.Threading.Tasks;

namespace LCT.SW360PackageCreator.UTest
{
    class AlpinePackageDownloaderTest
    {
        [TestCase]
        public async Task DownloadSourceForAlpine_ProvidedSourceURL_ReturnsDownloadPath()
        {
            //Arrange
            var lstComparisonBomData = new ComparisonBomData()
            {
                Name = "apk-tools",
                Version = "2.12.9-r3",
                SourceUrl = "https://gitlab.alpinelinux.org/alpine/apk-tools/-/archive/v2.12.9/apk-tools-v2.12.9.tar.gz",
                AlpineSource= "https://gitlab.alpinelinux.org/alpine/apk-tools/-/archive/v$pkgver/apk-tools-v$pkgver.tar.gz\\n\\tfix-recursive-solve-1.patch\\n\\tfix-recursive-solve-2.patch\\n\\t_apk\\n\\t"

            };
            var localPathforDownload = $"{Directory.GetParent(Directory.GetCurrentDirectory())}\\ClearingTool\\DownloadedFiles\\";
            var alpinePackageDownloader = new AlpinePackageDownloader();

            //Act
            var downloadpath = await alpinePackageDownloader.DownloadPackage(lstComparisonBomData, localPathforDownload);

            //Assert
            Assert.IsNotEmpty(downloadpath);
        }
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
                AlpineSource =""

            };
            var localPathforDownload = $"{Directory.GetParent(Directory.GetCurrentDirectory())}\\ClearingTool\\DownloadedFiles\\";
            var alpinePackageDownloader = new AlpinePackageDownloader();

            //Act
            var downloadpath = await alpinePackageDownloader.DownloadPackage(lstComparisonBomData, localPathforDownload);

            //Assert
            Assert.IsEmpty(downloadpath);
        }

    }
}
