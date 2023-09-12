// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.Common.Model;
using LCT.SW360PackageCreator.Interfaces;
using Moq;
using NUnit.Framework;
using System.IO;
using System.Threading.Tasks;

namespace LCT.SW360PackageCreator.UTest
{
    [TestFixture]
    class PythonPackageCreatorTest
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
    }
}
