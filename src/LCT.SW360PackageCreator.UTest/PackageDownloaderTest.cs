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
        

         [Test]
         public void Download_ReturnsEmptyString_WhenTaggedVersionIsEmpty()
         {
                // Arrange
                var packageDownloader = new PackageDownloader();
                var component = new ComparisonBomData { Name = "test", Version = "invalid" }; // invalid version to force GetCorrectVersion to return empty
                var method = typeof(PackageDownloader).GetMethod("Download", BindingFlags.NonPublic | BindingFlags.Instance);

                // Act
                var result = (string)method.Invoke(packageDownloader, new object[] { component, "/tmp/" });

                // Assert
                Assert.That(result, Is.EqualTo(string.Empty));
         }

         [Test]
         public void GetBaseVersion_ReturnsMajorMinor_WhenBuildIsZeroAndRevisionMinusOne()
         {
                // Arrange
                var method = typeof(PackageDownloader).GetMethod("GetBaseVersion", BindingFlags.NonPublic | BindingFlags.Static);

                // Act
                var result = (string)method.Invoke(null, new object[] { "1.2.0" });

                // Assert
                Assert.That(result, Is.EqualTo("1.2"));
         }

          [Test]
         public void GetBaseVersion_ReturnsInput_WhenFormatIsInvalid()
         {
                // Arrange
                var method = typeof(PackageDownloader).GetMethod("GetBaseVersion", BindingFlags.NonPublic | BindingFlags.Static);

                // Act
                var result = (string)method.Invoke(null, new object[] { "notaversion" });

                // Assert
                Assert.That(result, Is.EqualTo("notaversion"));
         }

         [Test]
         public void GetGitCloneCommands_ReturnsExpectedCommands()
         {
                // Arrange
                var method = typeof(PackageDownloader).GetMethod("GetGitCloneCommands", BindingFlags.NonPublic | BindingFlags.Static);
                var component = new ComparisonBomData { DownloadUrl = "https://github.com/example/repo" };
                var tag = "v1.0.0";
                var path = "/tmp/file.tgz";

                // Act
                var result = (List<string>)method.Invoke(null, new object[] { component, tag, path });

                // Assert
                Assert.That(result, Has.Count.GreaterThan(0));
                Assert.That(result[0], Does.Contain("init"));
         }

         [Test]
         public void CheckIfAlreadyDownloaded_NoMatch_ReturnsFalse()
         {
                // Arrange
                var packageDownloader = new PackageDownloader();
                var component = new ComparisonBomData { DownloadUrl = "https://github.com/example/repo" };
                var tagVersion = "v1.0.0";
                var field = typeof(PackageDownloader).GetField("m_downloadedSourceInfos", BindingFlags.NonPublic | BindingFlags.Instance);
                field.SetValue(packageDownloader, new List<DownloadedSourceInfo>()); // empty list

                var method = typeof(PackageDownloader).GetMethod("CheckIfAlreadyDownloaded", BindingFlags.NonPublic | BindingFlags.Instance);
                object[] parameters = { component, tagVersion, null };

                // Act
                var result = (bool)method.Invoke(packageDownloader, parameters);

                // Assert
                Assert.IsFalse(result);
         }
    }
    
}
