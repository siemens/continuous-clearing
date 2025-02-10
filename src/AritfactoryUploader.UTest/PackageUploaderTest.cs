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
    [TestFixture]
    public class PackageUploaderTest
    {
        [Test]
        public async Task UploadPackageToArtifactory_GivenAppsettings()
        {
            //Arrange

            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            //string comparisonBOMPath = outFolder + @"\ArtifactoryUTTestFiles\CyclonedxBom.json";

            CommonAppSettings commonAppSettings = new CommonAppSettings();
            commonAppSettings.Directory = new LCT.Common.Directory(new FolderAction(), new FileOperations())
            {
                OutputFolder = outFolder + @"\ArtifactoryUTTestFiles"
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
                ProjectName = "test"
            };

            IJFrogService jFrogService = GetJfrogService(commonAppSettings);
            PackageUploadHelper.jFrogService = jFrogService;
            UploadToArtifactory.jFrogService = jFrogService;
            ArtfactoryUploader.jFrogService= jFrogService;

            Program.UploaderStopWatch = new Stopwatch();
            Program.UploaderStopWatch.Start();
            Thread.Sleep(10);
            Program.UploaderStopWatch.Stop();
            //Act
            await PackageUploader.UploadPackageToArtifactory(commonAppSettings);

            // Assert
            Assert.That(0, Is.EqualTo(PackageUploader.uploaderKpiData.PackagesToBeUploaded), "Checks for no of cleared third party components");
            Assert.That(4, Is.EqualTo(PackageUploader.uploaderKpiData.DevPackagesToBeUploaded), "Checks for no of development components");
            Assert.That(0, Is.EqualTo(PackageUploader.uploaderKpiData.InternalPackagesToBeUploaded), "Checks for no of internal components");
            Assert.That(6, Is.EqualTo(PackageUploader.uploaderKpiData.ComponentInComparisonBOM), "Checks for no of components in BOM");
            Assert.That(0, Is.EqualTo(PackageUploader.uploaderKpiData.PackagesNotExistingInRemoteCache), "Checks for no of components not present in remote cache");
            Assert.That(3, Is.EqualTo(PackageUploader.uploaderKpiData.PackagesNotUploadedDueToError), "Checks for no of components not uploaded due to error");
        }

        [Test]
        [TestCase("NPM")]
        [TestCase("CONAN")]
        [TestCase("NUGET")]
        [TestCase("MAVEN")]
        [TestCase("DEBIAN")]
        [TestCase("PYTHON")]
        [TestCase("tes")]
        public void DisplayAllSettings_GivenListOfComponents_ReturnPackageSettings(string type)
        {
            //Arrange

            CommonAppSettings commonAppSettings = new CommonAppSettings();
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
                        new() { Name = "npm-test" }
                    },
                    InternalRepos = ["npm-test"],
                    DevRepos = ["npm-test"],
                    RemoteRepos = ["npm-test"]
                },
                Include = [],
                Exclude = []
            };

            //commonAppSettings.Conan.Artifactory.ThirdPartyRepos.Add(new ThirdPartyRepo() { Name = "conan-test" });
            commonAppSettings.TimeOut = 100;

            commonAppSettings.Conan = new Config()
            {
                Artifactory = new Artifactory()
                {
                    ThirdPartyRepos = new List<ThirdPartyRepo>()
                    {
                        new() { Name = "conan-test" }
                    },
                    InternalRepos = ["conan-test"],
                    DevRepos = ["conan-test"],
                    RemoteRepos = ["conan-test"]
                },
                Include = [],
                Exclude = []
            };

            commonAppSettings.Nuget = new Config()
            {
                Artifactory = new Artifactory()
                {
                    ThirdPartyRepos = new List<ThirdPartyRepo>()
                    {
                        new() { Name = "nuget-test" }
                    },
                    InternalRepos = ["nuget-test"],
                    DevRepos = ["nuget-test"],
                    RemoteRepos = ["nuget-test"]
                },
                Include = [],
                Exclude = []
            };

            commonAppSettings.Debian = new Config()
            {
                Artifactory = new Artifactory()
                {
                    ThirdPartyRepos = new List<ThirdPartyRepo>()
                    {
                        new() { Name = "debian-test" }
                    },
                    InternalRepos = ["debian-test"],
                    DevRepos = ["debian-test"],
                    RemoteRepos = ["debian-test"]
                },
                Include = [],
                Exclude = []
            };

            commonAppSettings.Maven = new Config()
            {
                Artifactory = new Artifactory()
                {
                    ThirdPartyRepos = new List<ThirdPartyRepo>()
                    {
                        new() { Name = "maven-test" }
                    },
                    InternalRepos = ["maven-test"],
                    DevRepos = ["maven-test"],
                    RemoteRepos = ["maven-test"]
                },
                Include = [],
                Exclude = []
            };

            commonAppSettings.Poetry = new Config()
            {
                Artifactory = new Artifactory()
                {
                    ThirdPartyRepos = new List<ThirdPartyRepo>()
                    {
                        new() { Name = "poetry-test" }
                    },
                    InternalRepos = ["poetry-test"],
                    DevRepos = ["poetry-test"],
                    RemoteRepos = ["poetry-test"]
                },
                Include = [],
                Exclude = []
            };


            List<Component> m_ComponentsInBOM = new()
            {
                new Component {
                Name="test",
                Version="1.0.0",
                Properties=new List<Property>()
                {
                new Property{Name=Dataconstant.Cdx_ProjectType,Value=type}
                }
            }
            };
            //Act
            PackageUploader.DisplayAllSettings(m_ComponentsInBOM, commonAppSettings);

            //Assert
            Assert.Pass();
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
