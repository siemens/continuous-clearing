// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
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
using LCT.Facade.Interfaces;
using System.Net.Http;
using System.Net;
using System.Text;
using System.IO;

namespace LCT.SW360PackageCreator.UTest
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
            CommonAppSettings appSettings = new CommonAppSettings()
            {
                SW360 = new SW360()
                {
                    AuthTokenType = "Token",
                    Token = "uifhiopsjfposddkf[fopefp[ld[p[lfffuhdffdkf",
                    URL = "http://localhost:8090"
                }
            };            

            // Act
            ISw360ProjectService sw360ProjectService = CreatorHelper.InitializeSw360ProjectService(appSettings);

            // assert
            Assert.That(sw360ProjectService, Is.Not.Null);
        }

        [Test]
        public void InitializeSw360CreatorService_ForGivenAppSettings_ReturnsSw360Creatorervice()
        {
            // Arrange
            CommonAppSettings appSettings = new CommonAppSettings()
            {
                SW360=new SW360()
                {
                    AuthTokenType= "Token",
                    Token= "uifhiopsjfposddkf[fopefp[ld[p[lfffuhdffdkf",
                    URL = "http://localhost:8090"
                }
            };
            
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
        public async Task SetContentsForComparisonBOM_ProvidedValidBomDetailsWithSw360InvalidCred_ReturnsEmpty()
        {
            //Arrange
            var debianPatcher = new Mock<IDebianPatcher>();
            IDictionary<string, IPackageDownloader> _packageDownloderList = new Dictionary<string, IPackageDownloader>
            {
                { "DEBIAN", new DebianPackageDownloader(debianPatcher.Object) }
            };
            var creatorHelper = new CreatorHelper(_packageDownloderList);

            ReleasesInfo releasesInfo = new ReleasesInfo();
            releasesInfo.SourceCodeDownloadUrl = "https://snapshot.debian.org/archive/debian/20180915T211528Z/pool/main/a/adduser/adduser_3.118.tar.xz";

            List<Components> componentsAvailableInSw360 = new List<Components>();
            List<Components> comparisonBomData = new List<Components>
            {
                new Components()
                {
                    Name = "adduser",
                    Version = "3.118",
                    ComponentExternalId = "pkg:deb/debian/adduser?arch=source",
                    ReleaseExternalId = "pkg:deb/debian/adduser@3.118?arch=source",
                    SourceUrl = "https://snapshot.debian.org/archive/debian/20180915T211528Z/pool/main/a/adduser/adduser_3.118.tar.xz",
                    DownloadUrl = "https://snapshot.debian.org/archive/debian/20180915T211528Z/pool/main/a/adduser/adduser_3.118.tar.xz"
                }
            };
            var iSW360Service = new Mock<ISW360Service>();

            //Mocking the Sw360 result as Empty with SuccessCode
            HttpResponseMessage responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("", Encoding.UTF8)
            };
            var iSW360ApicommunicationFacade = new Mock<ISW360ApicommunicationFacade>();
            iSW360ApicommunicationFacade.Setup(x => x.GetReleases()).ReturnsAsync(await responseMessage.Content.ReadAsStringAsync());
            iSW360Service.Setup(x => x.GetAvailableReleasesInSw360(comparisonBomData)).ReturnsAsync(componentsAvailableInSw360);
            iSW360Service.Setup(x => x.GetReleaseDataOfComponent(comparisonBomData[0].ReleaseLink)).ReturnsAsync(releasesInfo);

            //Act
            var data = await creatorHelper.SetContentsForComparisonBOM(comparisonBomData, iSW360Service.Object);

            //Assert
            Assert.That(data.Count.Equals(1));
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
            CreatorKpiData creatorKpiData=new( );
            //Act
            creatorHelper.GetCreatorKpiData(updatedCompareBomData, creatorKpiData);

            //Assert
            Assert.That(creatorKpiData.ComponentsReadFromComparisonBOM > 0);
        }

        [Test]
        public void WriteSourceNotFoundListToConsole_GetComparisionBomDatan_ReturnsNothing()
        {
            //Arrange
            CommonAppSettings appSettings = new CommonAppSettings()
            {
                SW360 = new SW360()
                {
                    AuthTokenType = "Token",
                    Token = "uifhiopsjfposddkf[fopefp[ld[p[lfffuhdffdkf",
                    URL = "http://localhost:8090"
                }
            };            

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

        [Test]
        [TestCase(Dataconstant.DownloadUrlNotFound, Dataconstant.NotUploaded)]
        [TestCase("", Dataconstant.Uploaded)]
        public void Test_ComponentsWithAndWithOutSourceDownloadUrl(string downloadUrl, string fossologyStatus)
        {
            // Arrange
            var creatorKpiData = new CreatorKpiData();
            var item = new ComparisonBomData();
            item.DownloadUrl = downloadUrl;
            item.FossologyUploadStatus = fossologyStatus;

            // Act
            CreatorHelper.ComponentsWithAndWithOutSourceDownloadUrl(ref creatorKpiData, item);

            // Assert
            Assert.AreEqual(1, creatorKpiData.ComponentsWithoutSourceDownloadUrl);

            if (item.FossologyUploadStatus == Dataconstant.NotUploaded)
            {
                Assert.AreEqual(1, creatorKpiData.ComponentsNotUploadedInFossology);
            }
            if (item.FossologyUploadStatus == Dataconstant.Uploaded)
            {
                Assert.AreEqual(1, creatorKpiData.ComponentsUploadedInFossology);
            }
        }

        [Test]
        [TestCase("download url")]
        public void Test_ComponentsWithAndWithOutSourceDownloadUrl(string downloadUrl)
        {
            // Arrange
            var creatorKpiData = new CreatorKpiData();
            var item = new ComparisonBomData();
            item.DownloadUrl = downloadUrl;

            // Act
            CreatorHelper.ComponentsWithAndWithOutSourceDownloadUrl(ref creatorKpiData, item);

            // Assert
            Assert.AreEqual(1, creatorKpiData.ComponentsWithSourceDownloadUrl);
        }

        [Test]
        [TestCase(Dataconstant.PackageUrlNotFound)]
        public void Test_ComponentsWithAndWithOutPackageDownload_url(string downloadUrl)
        {
            // Arrange
            var creatorKpiData = new CreatorKpiData();
            var item = new ComparisonBomData();
            item.DownloadUrl = downloadUrl;

            // Act
            CreatorHelper.ComponentsWithAndWithOutSourceDownloadUrl(ref creatorKpiData, item);

            // Assert
            Assert.AreEqual(1, creatorKpiData.ComponentsWithoutPackageUrl);
        }

        [Test]
        public void Test_GetReleaseLink_WhenReleaseLinkExists()
        {
            // Arrange
            var componentsAvailableInSw360 = new List<Components>
            {
                new Components { Name = "Component1", Version = "1.0", ReleaseLink = "https://example.com/release1" },
                new Components { Name = "Component2", Version = "2.0", ReleaseLink = "https://example.com/release2" },
                new Components { Name = "Component3", Version = "3.0", ReleaseLink = "https://example.com/release3" }
            };
            var name = "Component2";
            var version = "2.0";
            var expectedReleaseLink = "https://example.com/release2";

            // Act
            var actualReleaseLink = CreatorHelper.GetReleaseLink(componentsAvailableInSw360, name, version);

            // Assert
            Assert.AreEqual(expectedReleaseLink, actualReleaseLink);
        }

        [Test]
        public void Test_GetReleaseLink_WhenReleaseLinkDoesNotExist()
        {
            // Arrange
            var componentsAvailableInSw360 = new List<Components>
            {
                new Components { Name = "Component1", Version = "1.0", ReleaseLink = "https://example.com/release1" },
                new Components { Name = "Component2", Version = "2.0", ReleaseLink = "https://example.com/release2" },
                new Components { Name = "Component3", Version = "3.0", ReleaseLink = "https://example.com/release3" }
            };
            var name = "Component4";
            var version = "4.0";
            var expectedReleaseLink = string.Empty;

            // Act
            var actualReleaseLink = CreatorHelper.GetReleaseLink(componentsAvailableInSw360, name, version);

            // Assert
            Assert.AreEqual(expectedReleaseLink, actualReleaseLink);
        }

        [Test]
        public void Test_GetReleaseLink_WhenReleaseLinkDoesNotExist_versionDebian()
        {
            // Arrange
            var componentsAvailableInSw360 = new List<Components>
            {
                new Components { Name = "Component1", Version = "1.0", ReleaseLink = "https://example.com/release1" },
                new Components { Name = "Component2", Version = "2.0", ReleaseLink = "https://example.com/release2" },
                new Components { Name = "Component3", Version = "3.0", ReleaseLink = "https://example.com/release3" }
            };
            var name = "Component4";
            var version = "4.0+debian";
            var expectedReleaseLink = string.Empty;

            // Act
            var actualReleaseLink = CreatorHelper.GetReleaseLink(componentsAvailableInSw360, name, version);

            // Assert
            Assert.AreEqual(expectedReleaseLink, actualReleaseLink);
        }

        [Test]
        public void Test_GetFossologyUploadStatus_ReturnsCorrectStatus()
        {
            // Arrange
            string componentApprovedStatus = Dataconstant.NotAvailable;

            // Act
            string result = CreatorHelper.GetFossologyUploadStatus(componentApprovedStatus);

            // Assert
            Assert.AreEqual(Dataconstant.NotUploaded, result);
        }

        [Test]
        public void Test_GetFossologyUploadStatus_ReturnsCorrectStatus_WhenComponentApprovedStatusIsNewClearing()
        {
            // Arrange
            string componentApprovedStatus = Dataconstant.NewClearing;

            // Act
            string result = CreatorHelper.GetFossologyUploadStatus(componentApprovedStatus);

            // Assert
            Assert.AreEqual(Dataconstant.NotUploaded, result);
        }

        [Test]
        public void Test_GetFossologyUploadStatus_ReturnsCorrectStatus_WhenComponentApprovedStatusIsAlreadyUploaded()
        {
            // Arrange
            string componentApprovedStatus = Dataconstant.AlreadyUploaded;

            // Act
            string result = CreatorHelper.GetFossologyUploadStatus(componentApprovedStatus);

            // Assert
            Assert.AreEqual(Dataconstant.AlreadyUploaded, result);
        }

        [Test]
        public void Test_GetCreatedStatus_ReturnsCreated()
        {
            // Arrange
            string availabilityStatus = Dataconstant.Available;

            // Act
            string result = CreatorHelper.GetCreatedStatus(availabilityStatus);

            // Assert
            Assert.AreEqual(Dataconstant.Created, result);
        }

        [Test]
        public void Test_GetCreatedStatus_ReturnsNotCreated()
        {
            // Arrange
            string availabilityStatus = Dataconstant.NotAvailable;

            // Act
            string result = CreatorHelper.GetCreatedStatus(availabilityStatus);

            // Assert
            Assert.AreEqual(Dataconstant.NotCreated, result);
        }

        [Test]
        public void Test_GetApprovedStatus_ReturnsClearingState_WhenComponentAndReleaseAreAvailable()
        {
            // Arrange
            var componentAvailabelStatus = Dataconstant.Available;
            var releaseAvailbilityStatus = Dataconstant.Available;
            var releasesInfo = new ReleasesInfo
            {
                ClearingState = "Clearing in progress"
            };

            // Act
            var result = CreatorHelper.GetApprovedStatus(componentAvailabelStatus, releaseAvailbilityStatus, releasesInfo);

            // Assert
            Assert.AreEqual(releasesInfo.ClearingState, result);
        }

        [Test]
        public void Test_GetApprovedStatus_ReturnsNotAvailable_WhenComponentIsNotAvailable()
        {
            // Arrange
            var componentAvailabelStatus = Dataconstant.NotAvailable;
            var releaseAvailbilityStatus = Dataconstant.Available;
            var releasesInfo = new ReleasesInfo
            {
                ClearingState = "Clearing in progress"
            };

            // Act
            var result = CreatorHelper.GetApprovedStatus(componentAvailabelStatus, releaseAvailbilityStatus, releasesInfo);

            // Assert
            Assert.AreEqual(Dataconstant.NotAvailable, result);
        }

        [Test]
        public void Test_GetApprovedStatus_ReturnsNotAvailable_WhenReleaseIsNotAvailable()
        {
            // Arrange
            var componentAvailabelStatus = Dataconstant.Available;
            var releaseAvailbilityStatus = Dataconstant.NotAvailable;
            var releasesInfo = new ReleasesInfo
            {
                ClearingState = "Clearing in progress"
            };

            // Act
            var result = CreatorHelper.GetApprovedStatus(componentAvailabelStatus, releaseAvailbilityStatus, releasesInfo);

            // Assert
            Assert.AreEqual(Dataconstant.NotAvailable, result);
        }

        [Test]
        public void Test_GetApprovedStatus_ReturnsNotAvailable_WhenComponentAndReleaseAreNotAvailable()
        {
            // Arrange
            var componentAvailabelStatus = Dataconstant.NotAvailable;
            var releaseAvailbilityStatus = Dataconstant.NotAvailable;
            var releasesInfo = new ReleasesInfo
            {
                ClearingState = "Clearing in progress"
            };

            // Act
            var result = CreatorHelper.GetApprovedStatus(componentAvailabelStatus, releaseAvailbilityStatus, releasesInfo);

            // Assert
            Assert.AreEqual(Dataconstant.NotAvailable, result);
        }

        [Test]
        public void Test_GetComponentDownloadUrl_WhenReleaseExists()
        {
            // Arrange
            var mapper = new ComparisonBomData
            {
                ReleaseStatus = Dataconstant.Available
            };
            var item = new Components();
            var repoMock = new Mock<IRepository>();
            var releasesInfo = new ReleasesInfo
            {
                SourceCodeDownloadUrl = "https://example.com/sourcecode"
            };

            // Act
            var result = CreatorHelper.GetComponentDownloadUrl(mapper, item, repoMock.Object, releasesInfo);

            // Assert
            Assert.AreEqual(releasesInfo.SourceCodeDownloadUrl, result);
        }

        [Test]
        public void Test_GetMavenDownloadUrl_WhenReleaseExists()
        {
            // Arrange
            var mapper = new ComparisonBomData() { ReleaseStatus= Dataconstant.Available };
            var item = new Components();
            var releasesInfo = new ReleasesInfo
            {
                SourceCodeDownloadUrl = "https://example.com/sourcecode"
            };

            // Act
            var result = CreatorHelper.GetMavenDownloadUrl(mapper, item, releasesInfo);

            // Assert
            Assert.AreEqual(releasesInfo.SourceCodeDownloadUrl, result);
        }

        [Test]
        public void Test_GetMavenDownloadUrl_WhenReleaseDoesNotExist()
        {
            // Arrange
            var mapper = new ComparisonBomData
            {
                ReleaseStatus = "NotAvailable"
            };
            var item = new Components
            {
                Group = "com.example",
                Name = "example",
                Version = "1.0.0"
            };
            var releasesInfo = new ReleasesInfo();

            // Act
            var result = CreatorHelper.GetMavenDownloadUrl(mapper, item, releasesInfo);

            // Assert
            Assert.AreEqual("https://repo.maven.apache.org/maven2/com.example/example/1.0.0/example-1.0.0-sources.jar", result);
        }
        [Test]
        public void Test_GetAttachmentUrlListForMvn()
        {
            // Arrange
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string localPathforDownload = outFolder + @"..\..\..\src\LCT.SW360PackageCreator.UTest\ComponentCreatorUTFiles\";

            var component = new ComparisonBomData
            {
                Name = "TestComponent",
                Version = "1.0"
            };
            var attachmentUrlList = new Dictionary<string, string>();

            // Act
            CreatorHelper.GetAttachmentUrlListForMvn(localPathforDownload, component, ref attachmentUrlList);

            // Assert
            Assert.IsTrue(attachmentUrlList.ContainsKey("SOURCE"));
            Assert.AreEqual($"{localPathforDownload}{component.Name}-{component.Version}-sources.jar", attachmentUrlList["SOURCE"]);
        }
    }
}
