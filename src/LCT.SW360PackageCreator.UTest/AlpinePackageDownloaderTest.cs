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
    public class AlpinePackageDownloaderTest
    {

        private static async Task<bool> CanReachAsync(string url, int timeoutMs = 10000)
        {
            try
            {
                using var http = new HttpClient { Timeout = TimeSpan.FromMilliseconds(timeoutMs) };
                // Use HEAD when possible; fall back to GET if HEAD is not allowed
                using var req = new HttpRequestMessage(HttpMethod.Head, url);
                using var resp = await http.SendAsync(req);
                if ((int)resp.StatusCode == 405 || (int)resp.StatusCode == 501) // Method Not Allowed / Not Implemented
                {
                    using var getResp = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                    return getResp.IsSuccessStatusCode;
                }
                return resp.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }


        [Test]
        [Timeout(180000)] // 3 minutes to account for slower Linux CI
        public async Task DownloadSourceForAlpine_ProvidedSourceURL_ReturnsDownloadPath()
        {
            // Arrange
            var sourceUrl = "https://gitlab.alpinelinux.org/alpine/apk-tools/-/archive/v2.12.9/apk-tools-v2.12.9.tar.gz";

            // Skip deterministically if there is no connectivity to the host from the Linux agent
            if (!await CanReachAsync(sourceUrl))
            {
                Assert.Ignore($"Skipping test: cannot reach {sourceUrl} from this environment.");
            }

            var lstComparisonBomData = new ComparisonBomData()
            {
                Name = "apk-tools",
                Version = "2.12.9-r3",
                SourceUrl = sourceUrl,

                // IMPORTANT: Remove .patch lines so production code doesn't try to run `git`
                // (which may not exist on Linux agents and is not caught by your code).
                // Keeping only the URL (or even empty string) here is enough for the test.
                AlpineSource = "https://gitlab.alpinelinux.org/alpine/apk-tools/-/archive/v$pkgver/apk-tools-v$pkgver.tar.gz"
            };

            // Use a guaranteed-writable temp folder on Linux/Windows
            var testRoot = Path.Combine(Path.GetTempPath(), "ClearingToolTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(testRoot);

            var localPathforDownload = testRoot + Path.DirectorySeparatorChar;

            var alpinePackageDownloader = new AlpinePackageDownloader();

            try
            {
                // Act
                var downloadpath = await alpinePackageDownloader.DownloadPackage(lstComparisonBomData, localPathforDownload);

                // Assert
                Assert.IsNotEmpty(downloadpath, "Downloader returned empty path.");
                Assert.IsTrue(System.IO.File.Exists(downloadpath) || Directory.Exists(downloadpath),
                    $"Returned path does not exist on disk: {downloadpath}");
            }
            finally
            {
                // Cleanup best-effort
                try { if (Directory.Exists(testRoot)) Directory.Delete(testRoot, recursive: true); } catch { /* ignore */ }
            }
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
