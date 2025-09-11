// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.Common.Model;
using LCT.SW360PackageCreator.Interfaces;
using LCT.SW360PackageCreator.Model;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
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
        [TestCase]
        public async Task DownloadReleaseAttachmentSourceForNPMByPassingInvalidVersion_ProvidedvalidComparisonBomData_ReturnsEmptyString()
        {
            //Arrange
            var lstComparisonBomData = new ComparisonBomData()
            {
                Name = "core-js",
                Version = "v3.6.0"
            };
            var localPathforDownload = $"{Path.GetTempPath()}/ClearingTool/DownloadedFiles/";
            IPackageDownloader packageDownloader = new PackageDownloader();

            //Act
            string path = await packageDownloader.DownloadPackage(lstComparisonBomData, localPathforDownload);

            //Assert
            Assert.That(string.IsNullOrEmpty(path));
        }

        [Test]
        public void CheckIfAlreadyDownloaded_MatchingEntry_ReturnsTrueAndSetsPath()
        {
            // Arrange
            var packageDownloader = new PackageDownloader();
            var tagVersion = "v1.0.0";
            var downloadUrl = "https://github.com/example/repo";
            var expectedPath = "/tmp/downloaded/source";
            var component = new ComparisonBomData { DownloadUrl = downloadUrl };

            // Prepare the private m_downloadedSourceInfos list
            var downloadedSourceInfos = new List<DownloadedSourceInfo>
            {
               new DownloadedSourceInfo
               {
                  TaggedVersion = tagVersion,
                  SourceRepoUrl = downloadUrl,
                  DownloadedPath = expectedPath
               }
            };

            // Use reflection to set the private field
            var field = typeof(PackageDownloader).GetField("m_downloadedSourceInfos", BindingFlags.NonPublic | BindingFlags.Instance);
            field.SetValue(packageDownloader, downloadedSourceInfos);

            // Act
            var method = typeof(PackageDownloader).GetMethod("CheckIfAlreadyDownloaded", BindingFlags.NonPublic | BindingFlags.Instance);
            object[] parameters = { component, tagVersion, null };
            var result = (bool)method.Invoke(packageDownloader, parameters);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(expectedPath, parameters[2]);
        }
    }
}
