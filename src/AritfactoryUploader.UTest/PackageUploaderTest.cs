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
using CycloneDX.Models;
using LCT.Common.Constants;
using LCT.Common.Model;
using System.Collections.Generic;

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

        [Test]
        public void DisplayAllSettings_GivenListOfComponents_ReturnPackageSettings()
        {
            //Arrange
            CommonAppSettings CommonAppSettings = new CommonAppSettings()
            {

                JFrogApi = UTParams.JFrogURL,
                Npm = new Config
                {
                    JfrogThirdPartyDestRepoName = "npm-test",
                    JfrogDevDestRepoName = "npm-test",
                    JfrogInternalDestRepoName = "npm-test",
                    Include = { },
                    Exclude = { },
                },
                Conan = new Config
                {
                    JfrogThirdPartyDestRepoName = "conan-test",
                    JfrogDevDestRepoName = "conan-test",
                    JfrogInternalDestRepoName = "conan-test",
                    Include = { },
                    Exclude = { },
                },
                Nuget = new Config
                {
                    JfrogThirdPartyDestRepoName = "nuget-test",
                    JfrogDevDestRepoName = "nuget-test",
                    JfrogInternalDestRepoName = "nuget-test",
                    Include = { },
                    Exclude = { },
                },
                Python = new Config
                {
                    JfrogThirdPartyDestRepoName = "python-test",
                    JfrogDevDestRepoName = "python-test",
                    JfrogInternalDestRepoName = "python-test",
                    Include = { },
                    Exclude = { },
                },
                Maven = new Config
                {
                    JfrogThirdPartyDestRepoName = "maven-test",
                    JfrogDevDestRepoName = "maven-test",
                    JfrogInternalDestRepoName = "maven-test",
                    Include = { },
                    Exclude = { },
                },
                Debian = new Config
                {
                    JfrogThirdPartyDestRepoName = "debian-test",
                    JfrogDevDestRepoName = "debian-test",
                    JfrogInternalDestRepoName = "debian-test",
                    Include = { },
                    Exclude = { },
                },
                TimeOut = 100,
                Release = false
            };
            List<Component> m_ComponentsInBOM = new()
            {
                new Component {
                Name="test",
                Version="1.0.0",
                Properties=new List<Property>()
                {
                new Property{Name=Dataconstant.Cdx_ProjectType,Value="NPM"},
                new Property{Name=Dataconstant.Cdx_ProjectType,Value="CONAN"},
                new Property{Name=Dataconstant.Cdx_ProjectType,Value="NUGET"},
                new Property{Name=Dataconstant.Cdx_ProjectType,Value="MAVEN"},
                new Property{Name=Dataconstant.Cdx_ProjectType,Value="DEBIAN"},
                new Property{Name=Dataconstant.Cdx_ProjectType,Value="PYTHON"}
                }
            }
            };
            //Act
            PackageUploader.DisplayAllSettings(m_ComponentsInBOM, CommonAppSettings);

            //Assert
            Assert.IsTrue(true);
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
