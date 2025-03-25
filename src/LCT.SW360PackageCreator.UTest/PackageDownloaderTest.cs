// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.Common.Model;
using LCT.SW360PackageCreator.Interfaces;
using NUnit.Framework;
using System.IO;
using System.Threading.Tasks;

namespace LCT.SW360PackageCreator.UTest
{
    [TestFixture]
    public class PackageDownloaderTest
    {
        [TestCase]
        public async Task DownloadReleaseAttachmentSourceForNPM_ProvidedvalidComparisonBomData_ReturnsEmptyString()
        {
            //Arrange
            var lstComparisonBomData = new ComparisonBomData()
            {
                Name = "core-js",
                Version = "3.6.4"
            };
            var localPathforDownload = $"{Path.GetTempPath()}/ClearingTool/DownloadedFiles/";
            IPackageDownloader packageDownloader = new PackageDownloader();

            //Act
            string path = await packageDownloader.DownloadPackage(lstComparisonBomData, localPathforDownload);

            //Assert
            Assert.That(string.IsNullOrEmpty(path));
        }

        [TestCase]
        public async Task DownloadReleaseAttachmentSourceForNPM_ProvidedInValidComparisonBomData_ReturnsEmptyString()
        {
            //Arrange
            var lstComparisonBomData = new ComparisonBomData()
            {
                Name = "core-jsjs",
                Version = "3.6.44"
            };
            var localPathforDownload = $"{Path.GetTempPath()}/ClearingTool/DownloadedFiles/";
            IPackageDownloader packageDownloader = new PackageDownloader();

            //Act
            string path = await packageDownloader.DownloadPackage(lstComparisonBomData, localPathforDownload);

            //Assert
            Assert.That(string.IsNullOrEmpty(path));
        }
    }
}
