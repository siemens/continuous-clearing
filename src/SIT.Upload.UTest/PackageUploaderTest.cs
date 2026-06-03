// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using SIT.APICommunications;
using SIT.APICommunications.Interfaces;
using SIT.APICommunications.Model;
using SIT.Common;
using SIT.Common.Model;
using SIT.Facade;
using SIT.Facade.Interfaces;
using SIT.Services;
using SIT.Services.Interface;
using NUnit.Framework;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using UnitTestUtilities;

namespace SIT.Upload.UTest
{
    [TestFixture]
    public class PackageUploaderTest
    {
        [Test]
        public async Task UploadPackageToArtifactory_GivenAppsettings()
        {
            //Arrange

            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);

            CommonAppSettings commonAppSettings = new CommonAppSettings();
            commonAppSettings.Directory = new SIT.Common.Directory()
            {
                OutputFolder = Path.GetFullPath(Path.Combine(outFolder, "ArtifactoryUTTestFiles"))
            };

            commonAppSettings.Jfrog = new Jfrog()
            {
                URL = UTParams.JFrogURL,
                DryRun = false,
            };

            commonAppSettings.Npm = new Config()
            {
                Artifactory = new Artifactory()
                {
                    ThirdPartyRepos = new List<ThirdPartyRepo>()
                    {
                        new() { Name = "npm -test" }
                    }
                }
            };
            commonAppSettings.Conan = new Config()
            {
                Artifactory = new Artifactory()
                {
                    ThirdPartyRepos = new List<ThirdPartyRepo>()
                    {
                        new() { Name = "conan-test" }
                    }
                }
            };
            commonAppSettings.TimeOut = 100;
            commonAppSettings.SW360 = new SW360()
            {
                ProjectName = "Test"
            };
            commonAppSettings.SbomSigning = new SbomSigningConfig
            {
                SBOMSignVerify = false
            };
            IJFrogService jFrogService = GetJfrogService(commonAppSettings);
            PackageUploadHelper.JFrogService = jFrogService;
            UploadToArtifactory.JFrogService = jFrogService;
            ArtifactoryUploader.JFrogService = jFrogService;

            Program.UploaderStopWatch = new Stopwatch();
            Program.UploaderStopWatch.Start();
            await Task.Delay(10);
            Program.UploaderStopWatch.Stop();
            //Act
            await PackageUploader.UploadPackageToArtifactory(commonAppSettings);

            // Assert
            Assert.That(0, Is.EqualTo(PackageUploader.uploaderKpiData.PackagesToBeUploaded), "Checks for no of cleared third party components");
            Assert.That(6, Is.EqualTo(PackageUploader.uploaderKpiData.ComponentInComparisonBOM), "Checks for no of components in BOM");
            Assert.That(0, Is.EqualTo(PackageUploader.uploaderKpiData.PackagesNotExistingInRemoteCache), "Checks for no of components not present in remote cache");
            Assert.That(3, Is.EqualTo(PackageUploader.uploaderKpiData.PackagesNotUploadedDueToError), "Checks for no of components not uploaded due to error");
        }

        private static IJFrogService GetJfrogService(CommonAppSettings appSettings)
        {
            ArtifactoryCredentials artifactoryUpload = new ArtifactoryCredentials()
            {
                Token = appSettings.Jfrog.Token
            };
            IJfrogAqlApiCommunication jfrogAqlApiCommunication =
                new JfrogAqlApiCommunication(appSettings.Jfrog.URL, artifactoryUpload, appSettings.TimeOut);
            IJfrogAqlApiCommunicationFacade jFrogApiCommunicationFacade =
                new JfrogAqlApiCommunicationFacade(jfrogAqlApiCommunication);
            IJFrogService jFrogService = new JFrogService(jFrogApiCommunicationFacade);
            return jFrogService;
        }
    }
}
