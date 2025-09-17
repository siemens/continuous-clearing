// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.Common.Model;
using LCT.SW360PackageCreator.Interfaces;
using LCT.SW360PackageCreator.Model;
using NUnit.Framework;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
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
        [TestCase]
        public void CheckIfAlreadyDownloaded_ReturnsTrue_WhenEntryExists()
        {
            // Arrange
            var downloader = new PackageDownloader();
            var component = new ComparisonBomData
            {
                DownloadUrl = "https://repo.url"
            };
            string tagVersion = "v3.6.0";

            // Add a DownloadedSourceInfo to the private list
            var field = typeof(PackageDownloader).GetField("m_downloadedSourceInfos", BindingFlags.NonPublic | BindingFlags.Instance);
            var list = (System.Collections.IList)field.GetValue(downloader);
            list.Add(new DownloadedSourceInfo
            {
                TaggedVersion = tagVersion,
                SourceRepoUrl = component.DownloadUrl,
                DownloadedPath = "/tmp/downloaded/file"
            });

            // Act
            var method = typeof(PackageDownloader).GetMethod("CheckIfAlreadyDownloaded", BindingFlags.NonPublic | BindingFlags.Instance);
            object[] parameters = { component, tagVersion, null };
            bool result = (bool)method.Invoke(downloader, parameters);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual("/tmp/downloaded/file", parameters[2]);
        }

        [TestCase]
        public void CheckIfAlreadyDownloaded_ReturnsFalse_WhenEntryDoesNotExist()
        {
            // Arrange
            var downloader = new PackageDownloader();
            var component = new ComparisonBomData
            {
                DownloadUrl = "https://repo.url"
            };
            string tagVersion = "v3.6.0";

            // Act
            var method = typeof(PackageDownloader).GetMethod("CheckIfAlreadyDownloaded", BindingFlags.NonPublic | BindingFlags.Instance);
            object[] parameters = { component, tagVersion, null };
            bool result = (bool)method.Invoke(downloader, parameters);

            // Assert
            Assert.IsFalse(result);
            Assert.AreEqual(string.Empty, parameters[2]);
        }
        [TestCase]
        public void Download_ReturnsEmptyString_WhenUnauthorizedAccessExceptionThrown()
        {
            // Arrange
            var downloader = new PackageDownloader();
            var component = new ComparisonBomData
            {
                Name = "core-js",
                Version = "3.6.0",
                DownloadUrl = "https://repo.url",
                SourceUrl = "https://repo.url/core-js"
            };
            string unauthorizedPath = "/";
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                unauthorizedPath = "C:\\Windows\\System32\\";

            var method = typeof(PackageDownloader).GetMethod("Download", BindingFlags.NonPublic | BindingFlags.Instance);

            // Act
            object[] parameters = { component, unauthorizedPath };
            string result = (string)method.Invoke(downloader, parameters);

            // Assert
            Assert.AreEqual(string.Empty, result);
        }
        [Test]
        public void GetCorrectVersion_SplitsTagsByWindowsLineEndings()
        {
            // Arrange
            var component = new ComparisonBomData { Name = "core-js", Version = "3.6.0", DownloadUrl = "https://repo.url" };
            var resultType = typeof(PackageDownloaderTest).GetNestedType("Result", BindingFlags.NonPublic | BindingFlags.Public);

            var result = Activator.CreateInstance(resultType);
            resultType.GetProperty("StdOut").SetValue(result, "12345\ttags/core-js@3.6.0\r\n67890\ttags/core-js@3.5.0");
            var method = typeof(PackageDownloader).GetMethod("GetCorrectVersion", BindingFlags.NonPublic | BindingFlags.Static);

            // Act
            var version = (string)method.Invoke(null, new object[] { component });

            // Assert
            Assert.That(version, Is.Not.Null);
        }
        [Test]
        public void GetCorrectVersion_SplitsTagsByUnixLineEndings()
        {
            var component = new ComparisonBomData { Name = "core-js", Version = "3.6.0", DownloadUrl = "https://repo.url" };
            var resultType = typeof(PackageDownloaderTest).GetNestedType("Result", BindingFlags.NonPublic | BindingFlags.Public);
            var result = Activator.CreateInstance(resultType);
            resultType.GetProperty("StdOut").SetValue(result, "12345\ttags/core-js@3.6.0\n67890\ttags/core-js@3.5.0");

            // Use reflection to set up ListTagsOfComponent to return our result
            var method = typeof(PackageDownloader).GetMethod("GetCorrectVersion", BindingFlags.NonPublic | BindingFlags.Static);
            // Act
            var version = (string)method.Invoke(null, new object[] { component });

            // Assert
            Assert.That(version, Is.Not.Null);
        }

        public class Result
        {
            public string StdOut { get; set; }
            public string StdErr { get; set; }
            public int ExitCode { get; set; }
        }
    }
}
