// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using ArtifactoryUploader;
using LCT.ArtifactoryUploader;
using LCT.Common;
using NUnit.Framework;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnitTestUtilities;

namespace AritfactoryUploader.UTest
{
    public class PackageUploaderTest
    {
        [Test]
        public async Task UploadPackageToArtifactory_GivenAppsettings()
        {
            //Arrange

            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string comparisonBOMPath = outFolder + @"\ArtifactoryUTTestFiles\CyclonedxBom.json";
            CommonAppSettings CommonAppSettings = new CommonAppSettings()
            {
                BomFilePath = comparisonBOMPath,
                JFrogApi = UTParams.JFrogURL,
                JfrogNpmDestRepoName = "npm-test",
                JfrogNpmSrcRepo = "test",

            };

            Program.UploaderStopWatch = new Stopwatch();
            Program.UploaderStopWatch.Start();
            Thread.Sleep(10);
            Program.UploaderStopWatch.Stop();
            //Act
            await PackageUploader.UploadPackageToArtifactory(CommonAppSettings);

            // Assert
            Assert.That(12, Is.EqualTo(PackageUploader.uploaderKpiData.PackagesToBeUploaded), "Checks for no of components");
        }
    }
}
