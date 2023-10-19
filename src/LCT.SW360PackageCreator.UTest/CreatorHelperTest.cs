// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using LCT.Common.Model;
using LCT.Common.Constants;
using LCT.Services.Interface;
using LCT.SW360PackageCreator;
using LCT.SW360PackageCreator.Interfaces;
using LCT.SW360PackageCreator.Model;
using LCT.APICommunications.Model;
using System.Threading.Tasks;
using CycloneDX.Models;
using System.Diagnostics;
using LCT.Common;
using Castle.Core.Internal;

namespace SW360ComponentCreator.UTest
{
    [TestFixture]
    class CreatorHelperTest
    {
        [Test]
        public void Test_GetDownloadUrlNotFoundList()
        {
            //Arrange
            var lstComparisonBomData = new List<ComparisonBomData>()
            {
                new ComparisonBomData()
                {
                    Name="test",
                    DownloadUrl=""
                }
                ,new ComparisonBomData()
                {
                     Name="test",
                   DownloadUrl=Dataconstant.DownloadUrlNotFound
                },
                new ComparisonBomData()
                {
                     Name="test",
                     DownloadUrl=Dataconstant.DownloadUrlNotFound
                }

            };
            IDictionary<string, IPackageDownloader> _packageDownloderList = new Dictionary<string, IPackageDownloader>
            {
                { "NPM", new PackageDownloader() },
                { "NUGET", new PackageDownloader() }
            };
            var creatorHelper = new CreatorHelper(_packageDownloderList);
            var actual = creatorHelper.GetDownloadUrlNotFoundList(lstComparisonBomData);

            Assert.That(actual.Count, Is.EqualTo(3));
        }

        [Test]
        public async Task DownloadReleaseAttachmentSource_ForNPMPackage_ReturnSuccess()
        {
            //Arrange
            var lstComparisonBomData = new ComparisonBomData()
            {
                Name = "@angular/animations",
                Version = "4.2.6",
                ReleaseExternalId = "pkg:npm/@angular/animations@4.2.6",
                SourceUrl = "https://github.com/angular/angular.git",
                DownloadUrl = "https://github.com/angular/angular.git"
            };
            IDictionary<string, IPackageDownloader> _packageDownloderList = new Dictionary<string, IPackageDownloader>
            {
                { "NPM", new PackageDownloader() },
                { "NUGET", new PackageDownloader() }
            };
            var creatorHelper = new CreatorHelper(_packageDownloderList);

            //Act
            var attachmentUrlList = await creatorHelper.DownloadReleaseAttachmentSource(lstComparisonBomData);

            //Assert
            Assert.That(attachmentUrlList.ContainsKey("SOURCE"));
        }

        [Test]
        public void Test_WriteCreatorKpiDataToConsole()
        {
            var mock = new Mock<ICreatorHelper>();
            mock.Object.WriteCreatorKpiDataToConsole(new CreatorKpiData());
            mock.Verify(x => x.WriteCreatorKpiDataToConsole(It.IsAny<CreatorKpiData>()), Times.Once);
        }

        [Test]
        public void InitializeSw360ProjectService_ForGivenAppSeetings_ReturnsSw360ServiceObject()
        {
            // Arrange
            CommonAppSettings appSettings = new CommonAppSettings();
            appSettings.SW360AuthTokenType = "Token";
            appSettings.Sw360Token = "uifhiopsjfposddkf[fopefp[ld[p[lfffuhdffdkf";
            appSettings.SW360URL = "http://localhost:8090";

            // Act
            ISw360ProjectService sw360ProjectService = CreatorHelper.InitializeSw360ProjectService(appSettings);

            // assert
            Assert.That(sw360ProjectService, Is.Not.Null);
        }

        [Test]
        public void InitializeSw360CreatorService_ForGivenAppSettings_ReturnsSw360Creatorervice()
        {
            // Arrange
            CommonAppSettings appSettings = new CommonAppSettings();
            appSettings.SW360AuthTokenType = "Token";
            appSettings.Sw360Token = "uifhiopsjfposddkf[fopefp[ld[p[lfffuhdffdkf";
            appSettings.SW360URL = "http://localhost:8090";

            // Act
            ISw360CreatorService sw360CreatorService = CreatorHelper.InitializeSw360CreatorService(appSettings);

            // assert
            Assert.That(sw360CreatorService, Is.Not.Null);
        }

        [Test]
        public async Task SetContentsForComparisonBOM_ProvidedValidBomDetails_ReturnsListOfComparisonBomData()
        {
            //Arrange
            var debianPatcher = new Mock<IDebianPatcher>();
            IDictionary<string, IPackageDownloader> _packageDownloderList = new Dictionary<string, IPackageDownloader>();
            _packageDownloderList.Add("DEBIAN", new DebianPackageDownloader(debianPatcher.Object));
            var creatorHelper = new CreatorHelper(_packageDownloderList);

            ReleasesInfo releasesInfo = new ReleasesInfo();
            releasesInfo.SourceCodeDownloadUrl = "https://snapshot.debian.org/archive/debian/20180915T211528Z/pool/main/a/adduser/adduser_3.118.tar.xz";

            List<Components> componentsAvailableInSw360 = new List<Components>();
            componentsAvailableInSw360.Add(new Components()
            {
                Name = "adduser",
                Version = "3.118",
                ComponentExternalId = "pkg:deb/debian/adduser?arch=source",
                ReleaseExternalId = "pkg:deb/debian/adduser@3.118?arch=source",
                SourceUrl = "https://snapshot.debian.org/archive/debian/20180915T211528Z/pool/main/a/adduser/adduser_3.118.tar.xz",
                DownloadUrl = "https://snapshot.debian.org/archive/debian/20180915T211528Z/pool/main/a/adduser/adduser_3.118.tar.xz"
            });

            List<Components> comparisonBomData = new List<Components>();
            comparisonBomData.Add(new Components()
            {
                Name = "adduser",
                Version = "3.118",
                ComponentExternalId = "pkg:deb/debian/adduser?arch=source",
                ReleaseExternalId = "pkg:deb/debian/adduser@3.118?arch=source",
                SourceUrl = "https://snapshot.debian.org/archive/debian/20180915T211528Z/pool/main/a/adduser/adduser_3.118.tar.xz",
                DownloadUrl = "https://snapshot.debian.org/archive/debian/20180915T211528Z/pool/main/a/adduser/adduser_3.118.tar.xz"
            });
            var iSW360Service = new Mock<ISW360Service>();
            iSW360Service.Setup(x => x.GetAvailableReleasesInSw360(comparisonBomData)).ReturnsAsync(componentsAvailableInSw360);
            iSW360Service.Setup(x => x.GetReleaseDataOfComponent(comparisonBomData[0].ReleaseLink)).ReturnsAsync(releasesInfo);

            //Act
            var data = await creatorHelper.SetContentsForComparisonBOM(comparisonBomData, iSW360Service.Object);

            //Assert
            Assert.That(data.Count > 0);
        }

        [Test]
        public async Task GetUpdatedComponentsDetails_ProvidedValidBomDetails_ReturnsUpdatedBom()
        {
            //Arrange
            var debianPatcher = new Mock<IDebianPatcher>();
            IDictionary<string, IPackageDownloader> _packageDownloderList = new Dictionary<string, IPackageDownloader>();
            _packageDownloderList.Add("DEBIAN", new DebianPackageDownloader(debianPatcher.Object));
            var creatorHelper = new CreatorHelper(_packageDownloderList);
            Bom bom = new Bom()
            {
                Components = new List<Component>()
                {
                    new Component() { Name = "adduser",Version="3.118",BomRef= "pkg:deb/debian/adduser?type=source",Group="",Properties = new List<Property>() }
                }
            };

            List<ComparisonBomData> updatedCompareBomData = new List<ComparisonBomData>();
            updatedCompareBomData.Add(new ComparisonBomData()
            {
                Name = "adduser",
                Version = "3.118"
                
            });

            List<Components> componentsAvailableInSw360 = new List<Components>();
            componentsAvailableInSw360.Add(new Components()
            {
                Name = "adduser",
                Version = "3.118",
                ComponentExternalId = "pkg:deb/debian/adduser?arch=source",
                ReleaseExternalId = "pkg:deb/debian/adduser@3.118?arch=source",
                SourceUrl = "https://snapshot.debian.org/archive/debian/20180915T211528Z/pool/main/a/adduser/adduser_3.118.tar.xz",
                DownloadUrl = "https://snapshot.debian.org/archive/debian/20180915T211528Z/pool/main/a/adduser/adduser_3.118.tar.xz"
            });

            List<Components> comparisonBomData = new List<Components>();
            comparisonBomData.Add(new Components()
            {
                Name = "adduser",
                Version = "3.118",
                ComponentExternalId = "pkg:deb/debian/adduser?arch=source",
                ReleaseExternalId = "pkg:deb/debian/adduser@3.118?arch=source",
                SourceUrl = "https://snapshot.debian.org/archive/debian/20180915T211528Z/pool/main/a/adduser/adduser_3.118.tar.xz",
                DownloadUrl = "https://snapshot.debian.org/archive/debian/20180915T211528Z/pool/main/a/adduser/adduser_3.118.tar.xz"
            });
            var iSW360Service = new Mock<ISW360Service>();
            iSW360Service.Setup(x => x.GetAvailableReleasesInSw360(comparisonBomData)).ReturnsAsync(componentsAvailableInSw360);

            //Act
            Bom data = await creatorHelper.GetUpdatedComponentsDetails(comparisonBomData, updatedCompareBomData, iSW360Service.Object, bom);

            //Assert
            Assert.That(data.Components[0].Properties.Count > 0);
        }

        [Test]
        public void GetCreatorKpiData_UpdatedCompareBomData_ReturnsCreatorKpiData()
        {
            //Arrange
            var debianPatcher = new Mock<IDebianPatcher>();
            IDictionary<string, IPackageDownloader> _packageDownloderList = new Dictionary<string, IPackageDownloader>();
            _packageDownloderList.Add("DEBIAN", new DebianPackageDownloader(debianPatcher.Object));
            var creatorHelper = new CreatorHelper(_packageDownloderList);

            Program.CreatorStopWatch = new Stopwatch();
            Program.CreatorStopWatch.Start();

            List<ComparisonBomData> updatedCompareBomData = new List<ComparisonBomData>();
            updatedCompareBomData.Add(new ComparisonBomData()
            {
                Name = "adduser",
                Version = "3.118"
            });

            List<Components> componentsAvailableInSw360 = new List<Components>();
            componentsAvailableInSw360.Add(new Components()
            {
                Name = "adduser",
                Version = "3.118",
                ComponentExternalId = "pkg:deb/debian/adduser?arch=source",
                ReleaseExternalId = "pkg:deb/debian/adduser@3.118?arch=source",
                SourceUrl = "https://snapshot.debian.org/archive/debian/20180915T211528Z/pool/main/a/adduser/adduser_3.118.tar.xz",
                DownloadUrl = "https://snapshot.debian.org/archive/debian/20180915T211528Z/pool/main/a/adduser/adduser_3.118.tar.xz"
            });

            List<Components> comparisonBomData = new List<Components>();
            comparisonBomData.Add(new Components()
            {
                Name = "adduser",
                Version = "3.118",
                ComponentExternalId = "pkg:deb/debian/adduser?arch=source",
                ReleaseExternalId = "pkg:deb/debian/adduser@3.118?arch=source",
                SourceUrl = "https://snapshot.debian.org/archive/debian/20180915T211528Z/pool/main/a/adduser/adduser_3.118.tar.xz",
                DownloadUrl = "https://snapshot.debian.org/archive/debian/20180915T211528Z/pool/main/a/adduser/adduser_3.118.tar.xz"
            });
            var iSW360Service = new Mock<ISW360Service>();
            iSW360Service.Setup(x => x.GetAvailableReleasesInSw360(comparisonBomData)).ReturnsAsync(componentsAvailableInSw360);

            //Act
            CreatorKpiData data = creatorHelper.GetCreatorKpiData(updatedCompareBomData);

            //Assert
            Assert.That(data.ComponentsReadFromComparisonBOM > 0);
        }

        [Test]
        public void WriteSourceNotFoundListToConsole_GetComparisionBomDatan_ReturnsNothing()
        {
            //Arrange
            CommonAppSettings appSettings = new CommonAppSettings();
            appSettings.SW360AuthTokenType = "Token";
            appSettings.Sw360Token = "uifhiopsjfposddkf[fopefp[ld[p[lfffuhdffdkf";
            appSettings.SW360URL = "http://localhost:8090";

            List<ComparisonBomData> compareBomData = new List<ComparisonBomData>();
            compareBomData.Add(new ComparisonBomData()
            {
                Name = "adduser",
                Version = "3.118",
                IsReleaseCreated = Dataconstant.NotCreated
            });
            var debianPatcher = new Mock<IDebianPatcher>();
            IDictionary<string, IPackageDownloader> _packageDownloderList = new Dictionary<string, IPackageDownloader>();
            _packageDownloderList.Add("DEBIAN", new DebianPackageDownloader(debianPatcher.Object));
            var creatorHelper = new CreatorHelper(_packageDownloderList);

            //Act
            creatorHelper.WriteSourceNotFoundListToConsole(compareBomData, appSettings);

            //Assert
            Assert.IsTrue(true);
        }

        [Test]
        public async Task DownloadReleaseAttachmentSource_ForPythonPackage_ReturnSuccess()
        {
            //Arrange
            var lstComparisonBomData = new ComparisonBomData()
            {
                Name = "cachecontrol",
                Version = "0.12.11",
                ReleaseExternalId = "pkg:pypi/cachecontrol@0.12.11",
                SourceUrl = "https://files.pythonhosted.org/packages/46/9b/34215200b0c2b2229d7be45c1436ca0e8cad3b10de42cfea96983bd70248/CacheControl-0.12.11.tar.gz",
                DownloadUrl = "https://files.pythonhosted.org/packages/46/9b/34215200b0c2b2229d7be45c1436ca0e8cad3b10de42cfea96983bd70248/CacheControl-0.12.11.tar.gz"
            };
            IDictionary<string, IPackageDownloader> _packageDownloderList = new Dictionary<string, IPackageDownloader>
            {
                { "PYTHON", new PackageDownloader() }
            };
            var creatorHelper = new CreatorHelper(_packageDownloderList);

            //Act
            var attachmentUrlList = await creatorHelper.DownloadReleaseAttachmentSource(lstComparisonBomData);

            //Assert
            Assert.That(attachmentUrlList.ContainsKey("SOURCE"));
        }

        [Test]
        public async Task DownloadReleaseAttachmentSource_ForPythonPackage_ReturnFailure()
        {
            //Arrange
            var lstComparisonBomData = new ComparisonBomData()
            {
                Name = "cachecontrol22",
                Version = "0.12.1122",
                ReleaseExternalId = "pkg:pypi/cachecontrol22@0.12.1122",
                SourceUrl = "https://files.pythonhosted.org/packages/46/9b/34215200b0c2b2229d7be45c1436ca0e8cad3b10de42cfea96983bd70248/CacheControl22-0.12.1122.tar.gz",
                DownloadUrl = "https://files.pythonhosted.org/packages/46/9b/34215200b0c2b2229d7be45c1436ca0e8cad3b10de42cfea96983bd70248/CacheControl22-0.12.1122.tar.gz"
            };
            IDictionary<string, IPackageDownloader> _packageDownloderList = new Dictionary<string, IPackageDownloader>
            {
                { "PYTHON", new PackageDownloader() }
            };
            var creatorHelper = new CreatorHelper(_packageDownloderList);

            //Act
            var attachmentUrlList = await creatorHelper.DownloadReleaseAttachmentSource(lstComparisonBomData);

            //Assert
            Assert.That(attachmentUrlList.IsNullOrEmpty);
        }

    }
}
