// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
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
using LCT.Services.Interface;
using LCT.APICommunications.Model;
using LCT.APICommunications.Interfaces;
using LCT.APICommunications;
using LCT.Facade.Interfaces;
using LCT.Facade;
using LCT.Services;

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
                Npm = new LCT.Common.Model.Config
                {
                    JfrogThirdPartyDestRepoName = "npm-test",
                },
                Conan = new LCT.Common.Model.Config
                {
                    JfrogThirdPartyDestRepoName = "conan-test",
                },
                JfrogNpmSrcRepo = "test",
                TimeOut = 100,
                Release = false
            };

            IJFrogService jFrogService = GetJfrogService(CommonAppSettings);
            PackageUploadHelper.jFrogService = jFrogService;

            Program.UploaderStopWatch = new Stopwatch();
            Program.UploaderStopWatch.Start();
            Thread.Sleep(10);
            Program.UploaderStopWatch.Stop();
            //Act
            await PackageUploader.UploadPackageToArtifactory(CommonAppSettings);

            // Assert
            Assert.That(8, Is.EqualTo(PackageUploader.uploaderKpiData.PackagesToBeUploaded), "Checks for no of cleared third party components");
            Assert.That(2, Is.EqualTo(PackageUploader.uploaderKpiData.DevPackagesToBeUploaded), "Checks for no of development components");
            Assert.That(2, Is.EqualTo(PackageUploader.uploaderKpiData.InternalPackagesToBeUploaded), "Checks for no of internal components");
            Assert.That(12, Is.EqualTo(PackageUploader.uploaderKpiData.ComponentInComparisonBOM), "Checks for no of components in BOM");
            Assert.That(10, Is.EqualTo(PackageUploader.uploaderKpiData.PackagesNotExistingInRemoteCache), "Checks for no of components not present in remote cache");
            Assert.That(2, Is.EqualTo(PackageUploader.uploaderKpiData.PackagesNotUploadedDueToError), "Checks for no of components not uploaded due to error");
        }


        private static IJFrogService GetJfrogService(CommonAppSettings appSettings)
        {
            ArtifactoryCredentials artifactoryUpload = new ArtifactoryCredentials()
            {
                ApiKey = appSettings.ArtifactoryUploadApiKey
            };
            IJfrogAqlApiCommunication jfrogAqlApiCommunication =
                new JfrogAqlApiCommunication(appSettings.JFrogApi, artifactoryUpload, appSettings.TimeOut);
            IJfrogAqlApiCommunicationFacade jFrogApiCommunicationFacade =
                new JfrogAqlApiCommunicationFacade(jfrogAqlApiCommunication);
            IJFrogService jFrogService = new JFrogService(jFrogApiCommunicationFacade);
            return jFrogService;
        }
    }
}
