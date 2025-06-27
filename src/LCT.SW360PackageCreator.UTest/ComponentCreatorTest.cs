// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

// Ignore Spelling: Fossology Expections Dx Sbom Bom Doesnt LCT

using CycloneDX.Models;
using LCT.APICommunications;
using LCT.APICommunications.Model;
using LCT.APICommunications.Model.Foss;
using LCT.Common;
using LCT.Common.Constants;
using LCT.Common.Interface;
using LCT.Common.Model;
using LCT.Facade.Interfaces;
using LCT.Services;
using LCT.Services.Interface;
using LCT.Services.Model;
using LCT.SW360PackageCreator.Interfaces;
using LCT.SW360PackageCreator.Model;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using UnitTestUtilities;
using static System.Net.WebRequestMethods;


namespace LCT.SW360PackageCreator.UTest
{
    [TestFixture]
    public class ComponentCreatorTest
    {
        private Mock<ISw360CreatorService> _mockSw360CreatorService;
        private CommonAppSettings _appSettings;
        private ComponentCreator _componentCreator;
        private Mock<ICreatorHelper> _creatorHelperMock;
        [SetUp]
        public void Setup()
        {
            _mockSw360CreatorService = new Mock<ISw360CreatorService>();
            _creatorHelperMock = new Mock<ICreatorHelper>();
            _appSettings = new CommonAppSettings
            {
                SW360 = new SW360
                {
                    Fossology = new Fossology
                    {
                        URL = "https://fossology.example.com/"
                    }
                }
            };
            _componentCreator = new ComponentCreator();
        }
        [Test]
        public async Task IfAlreadyReleaseExistsUploadSourceCodeAndUrlInSW360_ShouldNotDownloadAttachments_WhenApprovedStatusIsNotNewClearing()
        {
            // Arrange
            var item = new ComparisonBomData { ApprovedStatus = "NotClearing" };
            var releasesInfo = new ReleasesInfo();
            var releaseId = "release123";

            // Act
            await ComponentCreator.IfAlreadyReleaseExistsUploadSourceCodeAndUrlInSW360(item, releasesInfo, releaseId, _creatorHelperMock.Object, _mockSw360CreatorService.Object);

            // Assert
            _creatorHelperMock.Verify(x => x.DownloadReleaseAttachmentSource(It.IsAny<ComparisonBomData>()), Times.Never);
        }
        [Test]
        public async Task IfAlreadyReleaseExistsUploadSourceCodeAndUrlInSW360_ShouldNotDownloadAttachments_WhenAttachmentsArePresent()
        {
            // Arrange
            var item = new ComparisonBomData { ApprovedStatus = Dataconstant.NewClearing };
            var releasesInfo = new ReleasesInfo
            {
                Embedded = new AttachmentEmbedded
                {
                    Sw360attachments = new List<Sw360Attachments>
                    {
                        new Sw360Attachments { AttachmentType = "SOURCE" }
                    }
                }
            };
            var releaseId = "release123";

            // Act
            await ComponentCreator.IfAlreadyReleaseExistsUploadSourceCodeAndUrlInSW360(item, releasesInfo, releaseId, _creatorHelperMock.Object, _mockSw360CreatorService.Object);

            // Assert
            _creatorHelperMock.Verify(x => x.DownloadReleaseAttachmentSource(It.IsAny<ComparisonBomData>()), Times.Never);
        }

        [Test]
        public async Task IfAlreadyReleaseExistsUploadSourceCodeAndUrlInSW360_ShouldUpdateSourceCodeDownloadURL_WhenSourceCodeDownloadUrlIsEmpty()
        {
            // Arrange
            var item = new ComparisonBomData { ApprovedStatus = Dataconstant.NewClearing };
            var releasesInfo = new ReleasesInfo { SourceCodeDownloadUrl = string.Empty };
            var releaseId = "release123";
            var attachmentUrlList = new Dictionary<string, string> { { "SOURCE", "http://example.com/source" } };

            _creatorHelperMock.Setup(x => x.DownloadReleaseAttachmentSource(item))
                .ReturnsAsync(attachmentUrlList);

            _mockSw360CreatorService.Setup(x => x.UpdateSourceCodeDownloadURLForExistingRelease(item, attachmentUrlList, releaseId))
                .ReturnsAsync(true);

            // Act
            await ComponentCreator.IfAlreadyReleaseExistsUploadSourceCodeAndUrlInSW360(item, releasesInfo, releaseId, _creatorHelperMock.Object, _mockSw360CreatorService.Object);

            // Assert
            _mockSw360CreatorService.Verify(x => x.UpdateSourceCodeDownloadURLForExistingRelease(item, attachmentUrlList, releaseId), Times.Once);
        }

        [Test]
        public async Task IfAlreadyReleaseExistsUploadSourceCodeAndUrlInSW360_ShouldAttachSourcesToReleases_WhenAttachmentUrlListIsNotEmpty()
        {
            // Arrange
            var item = new ComparisonBomData { ApprovedStatus = Dataconstant.NewClearing,DownloadUrl= "http://example.com/source" };
            var releasesInfo = new ReleasesInfo { SourceCodeDownloadUrl = "http://example.com/download" };
            var releaseId = "release123";
            var attachmentUrlList = new Dictionary<string, string> { { "SOURCE", "http://example.com/source" } };

            _creatorHelperMock.Setup(x => x.DownloadReleaseAttachmentSource(item))
                .ReturnsAsync(attachmentUrlList);

            _mockSw360CreatorService.Setup(x => x.AttachSourcesToReleasesCreated(releaseId, attachmentUrlList, item))
                .Returns("http://example.com/attachment");

            // Act
            await ComponentCreator.IfAlreadyReleaseExistsUploadSourceCodeAndUrlInSW360(item, releasesInfo, releaseId, _creatorHelperMock.Object, _mockSw360CreatorService.Object);

            // Assert
            Assert.AreEqual("http://example.com/attachment", item.ReleaseAttachmentLink);
            Assert.AreEqual("http://example.com/source", item.DownloadUrl);
        }

        [Test]
        public async Task IfAlreadyReleaseExistsUploadSourceCodeAndUrlInSW360_ShouldNotAttachSources_WhenAttachmentUrlListIsEmpty()
        {
            // Arrange
            var item = new ComparisonBomData { ApprovedStatus = Dataconstant.NewClearing };
            var releasesInfo = new ReleasesInfo { SourceCodeDownloadUrl = "http://example.com/download" };
            var releaseId = "release123";
            var attachmentUrlList = new Dictionary<string, string>();

            _creatorHelperMock.Setup(x => x.DownloadReleaseAttachmentSource(item))
                .ReturnsAsync(attachmentUrlList);

            // Act
            await ComponentCreator.IfAlreadyReleaseExistsUploadSourceCodeAndUrlInSW360(item, releasesInfo, releaseId, _creatorHelperMock.Object, _mockSw360CreatorService.Object);

            // Assert
            Assert.IsNull(item.ReleaseAttachmentLink);
        }

        [Test]
        public async Task TriggeringFossologyUploadAndUpdateAdditionalData_FossologyDisabled_ShouldSetNotUploaded()
        {
            // Arrange
            _appSettings.SW360.Fossology.EnableTrigger = false;
            var item = new ComparisonBomData
            {
                Name = "TestComponent",
                Version = "1.0.0",
                ApprovedStatus = "NEW_CLEARING",
                ClearingState = Dataconstant.ScanAvailableState
            };

            // Act
            await ComponentCreator.TriggeringFossologyUploadAndUpdateAdditionalData(item, _mockSw360CreatorService.Object, _appSettings);

            // Assert
            Assert.AreEqual(Dataconstant.NotUploaded, item.FossologyUploadStatus);
            _mockSw360CreatorService.VerifyNoOtherCalls();
        }
        [Test]
        public async Task TriggeringFossologyUploadAndUpdateAdditionalData_FossologyAlreadyUploaded_ShouldNotTriggerAgain()
        {
            // Arrange
            var item = new ComparisonBomData
            {
                Name = "TestComponent",
                Version = "1.0.0",
                ApprovedStatus = "NEW_CLEARING",
                ClearingState = Dataconstant.ScanAvailableState,
                FossologyLink = "https://fossology.example.com/upload/12345",
                FossologyUploadId = "12345"
            };

            // Act
            await ComponentCreator.TriggeringFossologyUploadAndUpdateAdditionalData(item, _mockSw360CreatorService.Object, _appSettings);

            // Assert
            Assert.AreEqual(Dataconstant.NotUploaded, item.FossologyUploadStatus);
            _mockSw360CreatorService.VerifyNoOtherCalls();
        }
        [Test]
        public async Task TriggeringFossologyUploadAndUpdateAdditionalData_FossologyUploadFails_ShouldSetNotUploaded()
        {
            // Arrange
            _appSettings.SW360.Fossology.EnableTrigger = true;
            var item = new ComparisonBomData
            {
                Name = "TestComponent",
                Version = "1.0.0",
                ApprovedStatus = "NEW_CLEARING",
                ClearingState = Dataconstant.ScanAvailableState
            };

            _mockSw360CreatorService
                .Setup(service => service.TriggerFossologyProcess(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new FossTriggerStatus());

            // Act
            await ComponentCreator.TriggeringFossologyUploadAndUpdateAdditionalData(item, _mockSw360CreatorService.Object, _appSettings);

            // Assert
            Assert.AreEqual(Dataconstant.NotUploaded, item.FossologyUploadStatus);
            _mockSw360CreatorService.Verify(service => service.TriggerFossologyProcess(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }


        [Test]
        public async Task UpdateFossologyStatus_FossologyAlreadyUploaded_ShouldSetStatusToUploaded()
        {
            // Arrange
            var item = new ComparisonBomData
            {
                FossologyLink = "https://fossology.example.com/upload/12345",
                FossologyUploadId = "12345",
                Name = "TestComponent",
                Version = "1.0.0"
            };
            string formattedName = "TestComponent";

            // Act
            await ComponentCreator.UpdateFossologyStatus(item, _mockSw360CreatorService.Object, _appSettings, formattedName);

            // Assert
            Assert.AreEqual(Dataconstant.AlreadyUploaded, item.FossologyUploadStatus);
            _mockSw360CreatorService.Verify(x => x.UpdateSW360ReleaseContent(It.IsAny<Components>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task UpdateFossologyStatus_FossologyUploadIdExistsButNoLink_ShouldUpdateFossologyLinkAndStatus()
        {
            // Arrange
            var item = new ComparisonBomData
            {
                FossologyLink = null,
                FossologyUploadId = "12345",
                Name = "TestComponent",
                Version = "1.0.0",
                ReleaseID = "67890"
            };
            string formattedName = "TestComponent";
            string fossologyUrl = "http://fossology" + ApiConstant.FossUploadJobUrlSuffix + "12345";
            _appSettings.SW360.Fossology.URL = "http://fossology/";
            _mockSw360CreatorService
                .Setup(s => s.UpdateSW360ReleaseContent(It.IsAny<Components>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            // Act
            var result = await ComponentCreator.UpdateFossologyStatus(item, _mockSw360CreatorService.Object, _appSettings, formattedName);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(fossologyUrl, item.FossologyLink);
            Assert.AreEqual(Dataconstant.Uploaded, item.FossologyUploadStatus);
        }

        [Test]
        public async Task UpdateFossologyStatus_NoFossologyUploadIdOrLink_ShouldNotUpdateStatus()
        {
            // Arrange
            var item = new ComparisonBomData
            {
                FossologyLink = null,
                FossologyUploadId = null,
                Name = "TestComponent",
                Version = "1.0.0"
            };
            string formattedName = "TestComponent";

            // Act
            await ComponentCreator.UpdateFossologyStatus(item, _mockSw360CreatorService.Object, _appSettings, formattedName);

            // Assert
            Assert.IsNull(item.FossologyUploadStatus);
            Assert.IsNull(item.FossologyLink);
            _mockSw360CreatorService.Verify(x => x.UpdateSW360ReleaseContent(It.IsAny<Components>(), It.IsAny<string>()), Times.Never);
        }
        [Test]
        public void GetFormattedName_ParentReleaseNameIsDifferent_ShouldReturnFormattedName()
        {
            // Arrange
            var item = new ComparisonBomData
            {
                ParentReleaseName = "ParentRelease",
                Name = "ChildRelease"
            };

            // Act
            var result = ComponentCreator.GetFormattedName(item);

            // Assert
            Assert.AreEqual("ParentRelease\\ChildRelease", result);
        }

        [Test]
        public void GetFormattedName_ParentReleaseNameIsSameAsName_ShouldReturnName()
        {
            // Arrange
            var item = new ComparisonBomData
            {
                ParentReleaseName = "ReleaseName",
                Name = "ReleaseName"
            };

            // Act
            var result = ComponentCreator.GetFormattedName(item);

            // Assert
            Assert.AreEqual("ReleaseName", result);
        }
        [Test]
        public async Task CreateComponentInSw360_ShouldCreateComponentsAndWriteFiles()
        {
            // Arrange
           
            var appSettings = new CommonAppSettings
            {
                SW360 = new SW360
                {
                    URL = "http://sw360.example.com",
                    ProjectID = "projectId",
                    ProjectName = "projectName"
                },
                Directory = new Common.Directory()
                {
                    OutputFolder = "outputFolder"
                },
                Mode = "production" // Ensure IsTestMode is false
            };

            var parsedBomData = new List<ComparisonBomData>
            {
                new ComparisonBomData { Name = "Component1", Version = "1.0.0" }
            };

            var mockSw360CreatorService = new Mock<ISw360CreatorService>();
            var mockSw360Service = new Mock<ISW360Service>();
            var mockSw360ProjectService = new Mock<ISw360ProjectService>();
            var mockFileOperations = new Mock<IFileOperations>();
            var mockCreatorHelper = new Mock<ICreatorHelper>();

            var componentCreator = new ComponentCreator();

            mockCreatorHelper.Setup(x => x.GetUpdatedComponentsDetails(It.IsAny<List<Components>>(), It.IsAny<List<ComparisonBomData>>(), It.IsAny<ISW360Service>(), It.IsAny<Bom>()))
                .ReturnsAsync(new Bom());

            mockCreatorHelper.Setup(x => x.GetDownloadUrlNotFoundList(It.IsAny<List<ComparisonBomData>>()))
                .Returns(new List<ComparisonBomData>());

            mockCreatorHelper.Setup(x => x.GetCreatorKpiData(It.IsAny<List<ComparisonBomData>>()))
                .Returns(new CreatorKpiData());

            mockSw360ProjectService.Setup(x => x.GetAlreadyLinkedReleasesByProjectId(It.IsAny<string>()))
                .ReturnsAsync(new List<ReleaseLinked>());

            mockSw360CreatorService.Setup(x => x.LinkReleasesToProject(It.IsAny<List<ReleaseLinked>>(), It.IsAny<List<ReleaseLinked>>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            // Act
            await componentCreator.CreateComponentInSw360(appSettings, mockSw360CreatorService.Object, mockSw360Service.Object, mockSw360ProjectService.Object, mockFileOperations.Object, mockCreatorHelper.Object, parsedBomData);

            // Assert
            mockFileOperations.Verify(x => x.WriteContentToOutputBomFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            mockCreatorHelper.Verify(x => x.WriteCreatorKpiDataToConsole(It.IsAny<CreatorKpiData>()), Times.Once);
            mockCreatorHelper.Verify(x => x.WriteSourceNotFoundListToConsole(It.IsAny<List<ComparisonBomData>>(), It.IsAny<CommonAppSettings>()), Times.Once);
            mockSw360CreatorService.Verify(x => x.LinkReleasesToProject(It.IsAny<List<ReleaseLinked>>(), It.IsAny<List<ReleaseLinked>>(), It.IsAny<string>()), Times.Once);
        }

        [Test]
        public void LinkReleasesToProject()
        {
            string releaseUrl = " \"href\" : \"" + UTParams.SW360URL + "/resource/api/releases/811609589f3798e2345634bdd4013a60\"\n ";

            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            List<string> releaseid = new List<string>();
            handlerMock
              .Protected()
             // Setup the PROTECTED method to mock
             .Setup<Task<HttpResponseMessage>>(
             "SendAsync",
             ItExpr.IsAny<HttpRequestMessage>(),
             ItExpr.IsAny<CancellationToken>())
             // prepare the expected response of the mocked http call
             .ReturnsAsync(new HttpResponseMessage()
             {
                 StatusCode = HttpStatusCode.OK,
                 Content = new StringContent("{\n  \"_embedded\" : {\n \"sw360:releases\" :" +
               " [ {\n  \"name\" : \"yuicompressor\",\n      " +
               "\"version\" : \"2.4.8\",\n   " +
               "   \"_links\" : " +
               "{\n        \"self\" : {\n         " + releaseUrl +
               "       }\n      }\n    }]}}")
             })
              .Verifiable();
            // ASSERT
            Assert.NotNull(releaseid);
        }

        [Test]
        public void CreateReleaseForComponent_ReleaseCreatedSuccessfully()
        {
            string releaseUrl = " \"href\" : \"" + UTParams.SW360URL + "/resource/api/releases/811609589f3798e2345634bdd4013a60\"\n ";

            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
              .Protected()
             // Setup the PROTECTED method to mock
             .Setup<Task<HttpResponseMessage>>(
             "SendAsync",
             ItExpr.IsAny<HttpRequestMessage>(),
             ItExpr.IsAny<CancellationToken>())
             // prepare the expected response of the mocked http call
             .ReturnsAsync(new HttpResponseMessage()
             {
                 StatusCode = HttpStatusCode.OK,
                 Content = new StringContent("{\n  \"_embedded\" : {\n \"sw360:releases\" :" +
               " [ {\n  \"name\" : \"yuicompressor\",\n      " +
               "\"version\" : \"2.4.8\",\n   " +
               "   \"_links\" : " +
               "{\n        \"self\" : {\n         " + releaseUrl +
               "       }\n      }\n    }]}}")
             })
              .Verifiable();
            // use real http client with mocked handler here
            // var httpClient = new HttpClient(handlerMock.Object) { };..

            ComparisonBomData packageLockMapper = new ComparisonBomData
            {
                Name = "test",
                Version = "1.2",
                SourceUrl = "",
                ApprovedStatus = "Available",
                ComponentStatus = "Available",
                DownloadUrl = "",
                FossologyUploadStatus = "Not uploaded",
                IsComponentCreated = "",
                IsReleaseCreated = ""
            };
            Dictionary<string, string> attachmentURL = new Dictionary<string, string>();
            Mock<ISW360ApicommunicationFacade> sw360ApiCommunicationService = new Mock<ISW360ApicommunicationFacade>();

            HttpResponseMessage responseMessage = new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.OK
            };
            sw360ApiCommunicationService.Setup(x => x.CreateRelease(It.IsAny<Releases>())).ReturnsAsync(responseMessage);
            var subjectUnderTest = new Sw360CreatorService(sw360ApiCommunicationService.Object);
            var result = subjectUnderTest.CreateReleaseForComponent(packageLockMapper, "811609589f3798e2345634bdd4013a60", attachmentURL);
            Assert.IsNotNull(result);
        }

        [Test]
        public void WriteAttachmentsJSONFile_FileCreatedSuccessfully()
        {
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string attachmentJson = Path.GetFullPath(Path.Combine(outFolder, "..", "..", "src", "LCT.SW360PackageCreator.UTest", "ComponentCreatorUTFiles", "Attachment.json"));

            string json = "";
            if (System.IO.File.Exists(attachmentJson))
            {
                json = System.IO.File.ReadAllText(attachmentJson);
            }

            dynamic array = JsonConvert.DeserializeObject(json);
            Assert.IsNotNull(array);
        }

        [Test]
        public async Task TriggerFossologyProcess_ProvidedReleaseId_ReturnsSuccess()
        {
            //Act
            var sw360CreatorServiceMock = new Mock<ISw360CreatorService>(MockBehavior.Default);
            FossTriggerStatus fossTriggerStatus = new FossTriggerStatus()
            {
                Content = new Content() { Message = "triggered succesfully" },
                Links = new Links() { Self = new Self() { Href = "test_link" } },
            };
            CheckFossologyProcess checkFossologyProcess = new CheckFossologyProcess()
            {
                Status = "FAILURE"
            };

            ComparisonBomData item = new ComparisonBomData()
            {
                Name = "test",
                Version = "1",
                ReleaseID = "89768ae1b0ea9dc061328b8f32792cbd"

            };
            
           
            CommonAppSettings appSettings = new CommonAppSettings()
            {
                SW360 = new SW360() { URL = "http://localhost:8081/" },
                Directory = new Common.Directory()
            };
            sw360CreatorServiceMock.Setup(x => x.TriggerFossologyProcess(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(fossTriggerStatus);

            sw360CreatorServiceMock.Setup(x => x.CheckFossologyProcessStatus(It.IsAny<string>())).ReturnsAsync(checkFossologyProcess);
            //Act
            await ComponentCreator.TriggerFossologyProcess(item, sw360CreatorServiceMock.Object, appSettings);

            // Assert
            sw360CreatorServiceMock.Verify(x => x.TriggerFossologyProcess(It.IsAny<string>(), It.IsAny<string>()), Times.AtLeastOnce);

        }

        [Test]
        public async Task TriggerFossologyProcess_ProvidedReleaseId_ReturnsEmptyForAggregateExpections()
        {
            //Act
            var sw360CreatorServiceMock = new Mock<ISw360CreatorService>(MockBehavior.Default);

            ComparisonBomData item = new ComparisonBomData()
            {
                Name = "test",
                Version = "1",
                ReleaseID = "89768ae1b0ea9dc061328b8f32792cbd"
            };            
            
            CommonAppSettings appSettings = new CommonAppSettings()
            {
                SW360 = new SW360() { URL = "http://localhost:8081/" },
                Directory = new Common.Directory()
            };
            sw360CreatorServiceMock.Setup(x => x.TriggerFossologyProcess(It.IsAny<string>(), It.IsAny<string>())).ThrowsAsync(new AggregateException());


            //Act
            string value = await ComponentCreator.TriggerFossologyProcess(item, sw360CreatorServiceMock.Object, appSettings);

            // Assert
            Assert.That(string.IsNullOrEmpty(value));

        }

        [Test]
        public async Task CycloneDxBomParser_PassingFilePath_ReturnsSuccess()
        {
            //Arrange

            List<Property> properties = new List<Property>();
            properties.Add(new Property()
            {
                Name = "internal:siemens:clearing:project-type",
                Value = "DEBIAN"
            });

            Bom bom = new Bom();
            bom.Components = new List<Component>()
                {
                    new Component() { Name = "adduser",Version="3.118",Group="",Purl="pkg:deb/debian/adduser@3.118@arch=source",Properties = properties },
                };
                                   
            CommonAppSettings CommonAppSettings = new()
            {
                SW360 = new SW360() { ProjectName = "Test" },
                Directory = new Common.Directory()
                {
                    OutputFolder = @"\Output"
                }
            };
            List<ComparisonBomData> comparisonBomData = new List<ComparisonBomData>();
            comparisonBomData.Add(new ComparisonBomData());
            var sw360Service = new Mock<ISW360Service>();
            var creatorHelper = new Mock<ICreatorHelper>();
            var parser = new Mock<ICycloneDXBomParser>();
            parser.Setup(x => x.ParseCycloneDXBom(It.IsAny<string>())).Returns(bom);
            creatorHelper.Setup(x => x.SetContentsForComparisonBOM(It.IsAny<List<Components>>(), sw360Service.Object)).ReturnsAsync(comparisonBomData);
            var cycloneDXBomParser = new ComponentCreator();

            //Act
            var list = await cycloneDXBomParser.CycloneDxBomParser(CommonAppSettings, sw360Service.Object, parser.Object, creatorHelper.Object);


            //Assert
            Assert.That(list.Count > 0);
        }

        [Test]
        public async Task CycloneDxBomParser_PassingFilePath_ReturnsComponentsExcludingDev()
        {
            //Arrange

            List<Property> properties = new List<Property>();
            properties.Add(new Property()
            {
                Name = "internal:siemens:clearing:project-type",
                Value = "NUGET"
            });
            properties.Add(new Property()
            {
                Name = "internal:siemens:clearing:development",
                Value = "true"
            });



            Bom bom = new Bom();
            bom.Components = new List<Component>()
                {
                    new Component() { Name = "newtonsoft",Version="3.1.18",Group="",Purl="pkg:nuget/newtonsoft@3.1.18",Properties = properties },
                };
            
           
            CommonAppSettings CommonAppSettings = new()
            {
                SW360 = new SW360() { ProjectName = "Test" },
                Directory = new Common.Directory()
                {
                    OutputFolder = @"\Output"
                }
            };


            List<ComparisonBomData> comparisonBomData = new List<ComparisonBomData>();
            comparisonBomData.Add(new ComparisonBomData());
            var sw360Service = new Mock<ISW360Service>();
            var creatorHelper = new Mock<ICreatorHelper>();
            var parser = new Mock<ICycloneDXBomParser>();
            parser.Setup(x => x.ParseCycloneDXBom(It.IsAny<string>())).Returns(bom);
            var cycloneDXBomParser = new ComponentCreator();

            //Act
            var list = await cycloneDXBomParser.CycloneDxBomParser(CommonAppSettings, sw360Service.Object, parser.Object, creatorHelper.Object);


            //Assert
            Assert.That(list == null);
        }
        [Test]
        public async Task CycloneDxBomParser_PassingFilePath_DoesntExcludeDevDependentComponent()
        {
            //Arrange

            List<Property> properties = new List<Property>();
            properties.Add(new Property()
            {
                Name = "internal:siemens:clearing:project-type",
                Value = "NUGET"
            });
            properties.Add(new Property()
            {
                Name = "internal:siemens:clearing:development",
                Value = "true"
            });



            Bom bom = new Bom();
            bom.Components = new List<Component>()
                {
                    new Component() { Name = "newtonsoft",Version="3.1.18",Group="",Purl="pkg:nuget/newtonsoft@3.1.18",Properties = properties }
                };
            
            
            CommonAppSettings commonAppSettings = new CommonAppSettings()
            {
                SW360 = new SW360() { IgnoreDevDependency = false, ProjectName = "Test" },
                Directory = new Common.Directory()
                {
                    OutputFolder = @"\Output"
                }
            };

            List<ComparisonBomData> comparisonBomData = new List<ComparisonBomData>();
            comparisonBomData.Add(new ComparisonBomData());
            var sw360Service = new Mock<ISW360Service>();
            var creatorHelper = new Mock<ICreatorHelper>();
            var parser = new Mock<ICycloneDXBomParser>();
            parser.Setup(x => x.ParseCycloneDXBom(It.IsAny<string>())).Returns(bom);
            creatorHelper.Setup(x => x.SetContentsForComparisonBOM(It.IsAny<List<Components>>(), sw360Service.Object)).ReturnsAsync(comparisonBomData);
            var cycloneDXBomParser = new ComponentCreator();

            //Act
            var list = await cycloneDXBomParser.CycloneDxBomParser(commonAppSettings, sw360Service.Object, parser.Object, creatorHelper.Object);


            //Assert
            Assert.That(list.Count == 1);
        }

        [Test]
        public async Task CycloneDxBomParser_Alpine_Component_Passing_ReturnsSuccess()
        {
            //Arrange

            List<Property> properties = new List<Property>();
            properties.Add(new Property()
            {
                Name = "internal:siemens:clearing:project-type",
                Value = "ALPINE"
            });

            Bom bom = new Bom();
            bom.Components = new List<Component>()
                {
                    new Component() { Name = "apk-tools",Version="2.12.9-r3",Group="",BomRef="pkg:apk/alpine/alpine-keys@2.4-r1?distro=alpine-3.16.2",Purl="pkg:apk/alpine/apk-tools@2.12.9-r3?arch=source",Properties = properties },
                };
            
            CommonAppSettings appSettings = new CommonAppSettings()
            {

                SW360 = new SW360() { ProjectName = "Test" },
                Directory = new LCT.Common.Directory()
                {
                    OutputFolder = @"\Output"
                }
            };

            List<ComparisonBomData> comparisonBomData = new List<ComparisonBomData>();
            comparisonBomData.Add(new ComparisonBomData());
            var sw360Service = new Mock<ISW360Service>();
            var creatorHelper = new Mock<ICreatorHelper>();
            var parser = new Mock<ICycloneDXBomParser>();
            parser.Setup(x => x.ParseCycloneDXBom(It.IsAny<string>())).Returns(bom);
            creatorHelper.Setup(x => x.SetContentsForComparisonBOM(It.IsAny<List<Components>>(), sw360Service.Object)).ReturnsAsync(comparisonBomData);
            var cycloneDXBomParser = new ComponentCreator();

            //Act
            var list = await cycloneDXBomParser.CycloneDxBomParser(appSettings, sw360Service.Object, parser.Object, creatorHelper.Object);


            //Assert
            Assert.That(list.Count > 0);
        }

        [Test]
        public void RemoveDuplicateComponents_RemovesDuplicateComponents()
        {
            // Arrange
            var componentCreator = new ComponentCreator();
            var components = new List<Components>
            {
                new Components { Name = "Component1", Version = "1.0" },
                new Components { Name = "Component2", Version = "2.0" },
                new Components { Name = "Component1", Version = "1.0" },
                new Components { Name = "Component3", Version = "3.0" },
                new Components { Name = "Component2", Version = "2.0" }
            };

            // Act
            var result = componentCreator.RemoveDuplicateComponents(components);

            // Assert
            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Any(c => c.Name == "Component1" && c.Version == "1.0"));
            Assert.IsTrue(result.Any(c => c.Name == "Component2" && c.Version == "2.0"));
            Assert.IsTrue(result.Any(c => c.Name == "Component3" && c.Version == "3.0"));
        }

        [Test]
        public void AddReleaseIdToLink_WhenReleaseIdIsNotNull_AddsReleaseToReleasesFoundInSbom()
        {
            // Arrange
            var item = new ComparisonBomData
            {
                Name = "TestComponent",
                Version = "1.0",
            };
            var releaseIdToLink = "12345";

            var componentCreator = new ComponentCreator();

            // Act
            componentCreator.AddReleaseIdToLink(item, releaseIdToLink);

            // Assert
            Assert.AreEqual(1, componentCreator.ReleasesFoundInCbom.Count);
            Assert.AreEqual(item.Name, componentCreator.ReleasesFoundInCbom[0].Name);
            Assert.AreEqual(item.Version, componentCreator.ReleasesFoundInCbom[0].Version);
            Assert.AreEqual(releaseIdToLink, componentCreator.ReleasesFoundInCbom[0].ReleaseId);
        }

        [Test]
        public void AddReleaseIdToLink_WhenReleaseIdIsNull_ThrowsExceptionAndSetsExitCode()
        {
            // Arrange
            var item = new ComparisonBomData
            {
                Name = "TestComponent",
                Version = "1.0",
            };
            string releaseIdToLink = null;

            var componentCreator = new ComponentCreator();

            // Act & Assert
            componentCreator.AddReleaseIdToLink(item, releaseIdToLink);
            Assert.Pass();
        }

        [Test]
        public void GetCreatedStatus_StatusIsTrue_ReturnsNewlyCreated()
        {
            // Arrange
            bool status = true;

            // Act
            string result = ComponentCreator.GetCreatedStatus(status);

            // Assert
            Assert.AreEqual(Dataconstant.NewlyCreated, result);
        }

        [Test]
        public void GetCreatedStatus_StatusIsFalse_ReturnsNotCreated()
        {
            // Arrange
            bool status = false;

            // Act
            string result = ComponentCreator.GetCreatedStatus(status);

            // Assert
            Assert.AreEqual(Dataconstant.NotCreated, result);
        }

        [Test]
        public void IsReleaseAttachmentExist_Sw360AttachmentsIsNull_ReturnsFalse()
        {
            // Arrange
            ReleasesInfo releasesInfo = new ReleasesInfo
            {
                Embedded = new AttachmentEmbedded
                {
                    Sw360attachments = null
                }
            };

            // Act
            bool result = ComponentCreator.IsReleaseAttachmentExist(releasesInfo);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void IsReleaseAttachmentExist_Sw360AttachmentsIsNotNull_ReturnsTrue()
        {
            // Arrange
            ReleasesInfo releasesInfo = new ReleasesInfo
            {
                Embedded = new AttachmentEmbedded
                {
                    Sw360attachments = new List<Sw360Attachments>
                    {
                        new Sw360Attachments
                        {
                            AttachmentType = "SOURCE"
                        }
                    }
                }
            };

            // Act
            bool result = ComponentCreator.IsReleaseAttachmentExist(releasesInfo);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public async Task GetComponentId_ComponentIdIsNull_ReturnsComponentIdUsingExternalId()
        {
            // Arrange
            var item = new ComparisonBomData
            {
                Name = "TestComponent",
                ComponentExternalId = "TestExternalId"
            };

            var sw360CreatorServiceMock = new Mock<ISw360CreatorService>();
            sw360CreatorServiceMock.Setup(s => s.GetComponentId(item.Name)).ReturnsAsync((string)null);
            sw360CreatorServiceMock.Setup(s => s.GetComponentIdUsingExternalId(item.Name, item.ComponentExternalId)).ReturnsAsync("TestComponentId");


            // Act
            var result = await ComponentCreator.GetComponentId(item, sw360CreatorServiceMock.Object);

            // Assert
            Assert.AreEqual("TestComponentId", result);
            sw360CreatorServiceMock.Verify(s => s.GetComponentId(item.Name), Times.Once);
            sw360CreatorServiceMock.Verify(s => s.GetComponentIdUsingExternalId(item.Name, item.ComponentExternalId), Times.Once);
        }

        [Test]
        public async Task CheckFossologyProcessStatus_ExceptionScenario_ReturnsEmptyUploadId()
        {
            // Arrange
            var link = "https://example.com/fossology/process/12345";
            var sw360CreatorServiceMock = new Mock<ISw360CreatorService>();
            var item = new ComparisonBomData
            {
                ReleaseID = "releaseId"
            };
            sw360CreatorServiceMock
                .Setup(x => x.CheckFossologyProcessStatus(link))
                .ThrowsAsync(new AggregateException("Error in TriggerFossologyProcess"));

            // Act
            var uploadId = await ComponentCreator.CheckFossologyProcessStatus(link, sw360CreatorServiceMock.Object, item);

            // Assert
            Assert.AreEqual(string.Empty, uploadId);
        }
        [Test]
        public async Task CheckFossologyProcessStatus_FossologyResultIsNull_ReturnsEmptyUploadId()
        {
            // Arrange
            var link = "https://example.com/fossology/process/12345";
            var sw360CreatorServiceMock = new Mock<ISw360CreatorService>();
            var item = new ComparisonBomData
            {
                Name = "TestComponent",
                Version = "1.0.0",
                ParentReleaseName = "ParentComponent"
            };

            sw360CreatorServiceMock
                .Setup(x => x.CheckFossologyProcessStatus(link))
                .ReturnsAsync((CheckFossologyProcess)null); // Simulate null response

            // Act
            var uploadId = await ComponentCreator.CheckFossologyProcessStatus(link, sw360CreatorServiceMock.Object, item);

            // Assert
            Assert.AreEqual(string.Empty, uploadId);
            sw360CreatorServiceMock.Verify(x => x.CheckFossologyProcessStatus(link), Times.Once);
        }
        [Test]
        public async Task CheckFossologyProcessStatus_NonExceptionScenario_ReturnsUploadId()
        {
            // Arrange
            var link = "https://example.com/fossology/process/12345";
            var sw360CreatorServiceMock = new Mock<ISw360CreatorService>();
            var item = new ComparisonBomData
            {
                ReleaseID = "releaseId"
            };
            var processStep = new ProcessSteps
            {
                ProcessStepIdInTool = "67890"
            };
            var ProcessStepsList = new List<ProcessSteps>() { processStep };

            var fossResult = new CheckFossologyProcess
            {
                FossologyProcessInfo = new FossologyProcessInfo
                {
                    ExternalTool = "Fossology",
                    ProcessSteps = [.. ProcessStepsList]
                }
            };
            sw360CreatorServiceMock
                .Setup(x => x.CheckFossologyProcessStatus(link))
                .ReturnsAsync(fossResult);

            // Act
            var uploadId = await ComponentCreator.CheckFossologyProcessStatus(link, sw360CreatorServiceMock.Object, item);

            // Assert
            Assert.AreEqual("67890", uploadId);
        }

        [Test]
        public async Task TriggerFossologyProcess_ExceptionScenario_ReturnsEmptyUploadId()
        {
            // Arrange
            var item = new ComparisonBomData
            {
                ReleaseID = "releaseId"
            };

            var sw360CreatorServiceMock = new Mock<ISw360CreatorService>();
            sw360CreatorServiceMock.Setup(x => x.TriggerFossologyProcess(It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new AggregateException());

            // Act
            var uploadId = await ComponentCreator.TriggerFossologyProcess(item, sw360CreatorServiceMock.Object, new CommonAppSettings() { SW360 = new SW360() });

            // Assert
            Assert.AreEqual(string.Empty, uploadId);
        }

        [Test]
        public async Task TriggerFossologyProcess_NonExceptionScenario_ReturnsUploadId()
        {
            // Arrange
            var item = new ComparisonBomData
            {
                ReleaseID = "releaseId"
            };

            ProcessSteps processSteps = new ProcessSteps
            {
                ProcessStepIdInTool = "uploadId"
            };
            var steps = new List<ProcessSteps>() { processSteps };
            var sw360CreatorServiceMock = new Mock<ISw360CreatorService>();
            sw360CreatorServiceMock.Setup(x => x.TriggerFossologyProcess(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new FossTriggerStatus
                {
                    Links = new Links
                    {
                        Self = new Self
                        {
                            Href = "fossologyLink"
                        }
                    }
                });

            sw360CreatorServiceMock.Setup(x => x.CheckFossologyProcessStatus(It.IsAny<string>()))
                .ReturnsAsync(new CheckFossologyProcess
                {
                    FossologyProcessInfo = new FossologyProcessInfo
                    {
                        ExternalTool = "fossologyTool",
                        ProcessSteps = steps.ToArray()
                    }
                });


            // Act
            var uploadId = await ComponentCreator.TriggerFossologyProcess(item, sw360CreatorServiceMock.Object, new CommonAppSettings() { SW360 = new SW360() });

            // Assert
            Assert.AreEqual("uploadId", uploadId);
        }
        [Test]
        public async Task TriggerFossologyProcess_ProvidedReleaseId_ReturnsProcessingStatus()
        {
            // Arrange
            var sw360CreatorServiceMock = new Mock<ISw360CreatorService>(MockBehavior.Default);
            FossTriggerStatus fossTriggerStatus = new FossTriggerStatus()
            {
                Content = new Content() { Message = "triggered successfully" },
                Links = new Links() { Self = new Self() { Href = "test_link" } },
            };
            CheckFossologyProcess checkFossologyProcess = new CheckFossologyProcess()
            {
                Status = "PROCESSING",

            };

            ComparisonBomData item = new ComparisonBomData()
            {
                Name = "test",
                Version = "1",
                ReleaseID = "89768ae1b0ea9dc061328b8f32792cbd"
            };

           
            CommonAppSettings appSettings = new CommonAppSettings()
            {
                SW360 = new SW360() { URL = "http://localhost:8081/" },
                Directory = new Common.Directory()
            };

            sw360CreatorServiceMock.Setup(x => x.TriggerFossologyProcess(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(fossTriggerStatus);
            sw360CreatorServiceMock.Setup(x => x.CheckFossologyProcessStatus(It.IsAny<string>())).ReturnsAsync(checkFossologyProcess);

            // Act
            string uploadId = await ComponentCreator.TriggerFossologyProcess(item, sw360CreatorServiceMock.Object, appSettings);

            // Assert           
            sw360CreatorServiceMock.Verify(x => x.TriggerFossologyProcess(It.IsAny<string>(), It.IsAny<string>()), Times.AtLeastOnce);
        }
        [Test]
        public async Task GetUploadIdWhenReleaseExists_ShouldExitEarly_WhenReleasesInfoIsNull()
        {
            // Arrange
            var item = new ComparisonBomData();
            ReleasesInfo releasesInfo = null; // Simulate null releasesInfo
            var appSettings = new CommonAppSettings();

            // Act
            await ComponentCreator.GetUploadIdWhenReleaseExists(item, releasesInfo, appSettings);

            // Assert
            Assert.IsNull(item.ClearingState, "ClearingState should remain null when releasesInfo is null.");
            Assert.IsNull(item.FossologyLink, "FossologyLink should remain null when releasesInfo is null.");
            Assert.IsNull(item.FossologyUploadId, "FossologyUploadId should remain null when releasesInfo is null.");
        }
        [Test]
        public async Task GetUploadIdWhenReleaseExists_ShouldSetFossologyLinkAndUploadId_WhenAdditionalDataContainsFossologyURL()
        {
            // Arrange
            var item = new ComparisonBomData();
            var releasesInfo = new ReleasesInfo
            {
                ClearingState = "APPROVED",
                AdditionalData = new Dictionary<string, string>
        {
            { ApiConstant.AdditionalDataFossologyURL, "https://fossology.example.com/upload/12345" }
        },
                ExternalToolProcesses = new List<ExternalToolProcess>
        {
            new ExternalToolProcess
            {
                ProcessSteps = new List<ProcessSteps>
                {
                    new ProcessSteps
                    {
                        StepName = "01_upload",
                        ProcessStepIdInTool = "12345"
                    }
                }
            }
        }
            };
            var appSettings = new CommonAppSettings
            {
                SW360 = new SW360
                {
                    Fossology = new Fossology
                    {
                        URL = "https://fossology.example.com"
                    }
                }
            };

            // Act
            await ComponentCreator.GetUploadIdWhenReleaseExists(item, releasesInfo, appSettings);

            // Assert
            Assert.AreEqual("APPROVED", item.ApprovedStatus, "ClearingState should be set from releasesInfo.");
            Assert.AreEqual("https://fossology.example.com/upload/12345", item.FossologyLink, "FossologyLink should be set from AdditionalData.");
            Assert.AreEqual("12345", item.FossologyUploadId, "FossologyUploadId should be set from ProcessStepIdInTool.");
        }
        [Test]
        public async Task ProcessReleaseAlreadyExist_ReleaseAlreadyExists_WithValidReleaseID_ShouldTriggerFossologyUpload()
        {
            // Arrange
            var item = new ComparisonBomData
            {
                ReleaseID = "12345",
                DownloadUrl = "https://example.com/download"
            };
            var releaseCreateStatus = new ReleaseCreateStatus
            {
                ReleaseAlreadyExist = true
            };
            var releasesInfo = new ReleasesInfo
            {
                Name = "TestRelease",
                Embedded = new AttachmentEmbedded
                {
                    Sw360attachments = new List<Sw360Attachments>
                {
                    new Sw360Attachments { AttachmentType = "SOURCE" }
                }
                }
            };

            _mockSw360CreatorService
                .Setup(service => service.GetReleaseInfo(item.ReleaseID))
                .ReturnsAsync(releasesInfo);

            // Act
            await ComponentCreator.ProcessReleaseAlreadyExist(item, _mockSw360CreatorService.Object, _appSettings, releaseCreateStatus);

            // Assert
            Assert.AreEqual("TestRelease", item.ParentReleaseName);
            _mockSw360CreatorService.Verify(service => service.GetReleaseInfo(item.ReleaseID), Times.Once);
        }

        [Test]
        public async Task ProcessReleaseAlreadyExist_ReleaseAlreadyExists_WithNoAttachments_ShouldNotTriggerFossologyUpload()
        {
            // Arrange
            var item = new ComparisonBomData
            {
                ReleaseID = "12345",
                DownloadUrl = "https://example.com/download"
            };
            var releaseCreateStatus = new ReleaseCreateStatus
            {
                ReleaseAlreadyExist = true
            };
            var releasesInfo = new ReleasesInfo
            {
                Name = "TestRelease",
                Embedded = new AttachmentEmbedded
                {
                    Sw360attachments = new List<Sw360Attachments>() // No attachments
                }
            };

            _mockSw360CreatorService
                .Setup(service => service.GetReleaseInfo(item.ReleaseID))
                .ReturnsAsync(releasesInfo);

            // Act
            await ComponentCreator.ProcessReleaseAlreadyExist(item, _mockSw360CreatorService.Object, _appSettings, releaseCreateStatus);

            // Assert
            Assert.AreEqual("TestRelease", item.ParentReleaseName);
            _mockSw360CreatorService.Verify(service => service.GetReleaseInfo(item.ReleaseID), Times.Once);
        }

        [Test]
        public async Task ProcessReleaseAlreadyExist_ReleaseDoesNotExist_WithValidDownloadUrl_ShouldTriggerFossologyUpload()
        {
            // Arrange
            var item = new ComparisonBomData
            {
                ReleaseID = "12345",
                DownloadUrl = "https://example.com/download"
            };
            var releaseCreateStatus = new ReleaseCreateStatus
            {
                ReleaseAlreadyExist = false
            };

            // Act
            await ComponentCreator.ProcessReleaseAlreadyExist(item, _mockSw360CreatorService.Object, _appSettings, releaseCreateStatus);

            // Assert
            _mockSw360CreatorService.Verify(service => service.GetReleaseInfo(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task ProcessReleaseAlreadyExist_ReleaseDoesNotExist_WithInvalidDownloadUrl_ShouldNotTriggerFossologyUpload()
        {
            // Arrange
            var item = new ComparisonBomData
            {
                ReleaseID = "12345",
                DownloadUrl = Dataconstant.DownloadUrlNotFound
            };
            var releaseCreateStatus = new ReleaseCreateStatus
            {
                ReleaseAlreadyExist = false
            };

            // Act
            await ComponentCreator.ProcessReleaseAlreadyExist(item, _mockSw360CreatorService.Object, _appSettings, releaseCreateStatus);

            // Assert
            _mockSw360CreatorService.Verify(service => service.GetReleaseInfo(It.IsAny<string>()), Times.Never);
        }
    }
}

