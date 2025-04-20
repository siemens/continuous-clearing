// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

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


namespace LCT.SW360PackageCreator.UTest
{
    [TestFixture]
    public class ComponentCreatorTest
    {
        private Mock<ISw360CreatorService> _mockSw360CreatorService;
        private CommonAppSettings _appSettings;
        private ComponentCreator _componentCreator;
        [SetUp]
        public void Setup()
        {
            _mockSw360CreatorService = new Mock<ISw360CreatorService>();
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
        public async Task TriggeringFossologyUploadAndUpdateAdditionalData_FossologyDisabled_ShouldSetNotUploaded()
        {
            // Arrange
            _appSettings.SW360.Fossology.EnableTrigger = false;
            var item = new ComparisonBomData
            {
                Name = "TestComponent",
                Version = "1.0.0",
                ApprovedStatus = "NEW_CLEARING",
                ClearingState = Dataconstant.ScanClearingState
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
                ClearingState = Dataconstant.ScanClearingState,
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
                ClearingState = Dataconstant.ScanClearingState
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
                FossologyUploadId = Dataconstant.NotUploaded,
                Name = "TestComponent",
                Version = "1.0.0"
            };
            string formattedName = "TestComponent";

            // Act
            await ComponentCreator.UpdateFossologyStatus(item, _mockSw360CreatorService.Object, _appSettings, formattedName);

            // Assert
            Assert.AreEqual(Dataconstant.Uploaded, item.FossologyUploadStatus);
            _mockSw360CreatorService.Verify(x => x.UdpateSW360ReleaseContent(It.IsAny<Components>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task UpdateFossologyStatus_FossologyUploadIdExistsButNoLink_ShouldUpdateFossologyLinkAndStatus()
        {
            // Arrange
            var item = new ComparisonBomData
            {
                FossologyLink = null,
                FossologyUploadId = Dataconstant.NotUploaded,
                Name = "TestComponent",
                Version = "1.0.0",
                ReleaseID = "67890"
            };
            string formattedName = "TestComponent";

            // Act
            await ComponentCreator.UpdateFossologyStatus(item, _mockSw360CreatorService.Object, _appSettings, formattedName);

            // Assert
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
            _mockSw360CreatorService.Verify(x => x.UdpateSW360ReleaseContent(It.IsAny<Components>(), It.IsAny<string>()), Times.Never);
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
            var folderAction = new Mock<IFolderAction>();
            var fileOperations = new Mock<IFileOperations>();
            var appSettings = new CommonAppSettings
            {
                SW360 = new SW360
                {
                    URL = "http://sw360.example.com",
                    ProjectID = "projectId",
                    ProjectName = "projectName"
                },
                Directory = new Common.Directory(folderAction.Object, fileOperations.Object)
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
            if (File.Exists(attachmentJson))
            {
                json = File.ReadAllText(attachmentJson);
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
            IFolderAction folderAction = new FolderAction();
            IFileOperations fileOperations = new FileOperations();
            CommonAppSettings appSettings = new CommonAppSettings()
            {
                SW360 = new SW360() { URL = "http://localhost:8081/" },
                Directory = new Common.Directory(folderAction, fileOperations)
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

            IFolderAction folderAction = new FolderAction();
            IFileOperations fileOperations = new FileOperations();
            CommonAppSettings appSettings = new CommonAppSettings()
            {
                SW360 = new SW360() { URL = "http://localhost:8081/" },
                Directory = new Common.Directory(folderAction, fileOperations)
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

            IFolderAction folderAction = new FolderAction();
            IFileOperations fileOperations = new FileOperations();
            CommonAppSettings CommonAppSettings = new()
            {
                SW360 = new SW360() { ProjectName = "Test" },
                Directory = new Common.Directory(folderAction, fileOperations)
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
            IFolderAction folderAction = new FolderAction();
            IFileOperations fileOperations = new FileOperations();
            CommonAppSettings CommonAppSettings = new()
            {
                SW360 = new SW360() { ProjectName = "Test" },
                Directory = new Common.Directory(folderAction, fileOperations)
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
            IFolderAction folderAction = new FolderAction();
            IFileOperations fileOperations = new FileOperations();
            CommonAppSettings commonAppSettings = new CommonAppSettings()
            {
                SW360 = new SW360() { IgnoreDevDependency = false, ProjectName = "Test" },
                Directory = new Common.Directory(folderAction, fileOperations)
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
            IFolderAction folderAction = new FolderAction();
            IFileOperations fileOperations = new FileOperations();
            CommonAppSettings appSettings = new CommonAppSettings(folderAction, fileOperations)
            {

                SW360 = new SW360() { ProjectName = "Test" },
                Directory = new LCT.Common.Directory(folderAction, fileOperations)
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
        public void AddReleaseIdToLink_WhenReleaseIdIsNotNull_AddsReleaseToReleasesFoundInCbom()
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
            sw360CreatorServiceMock
                .Setup(x => x.CheckFossologyProcessStatus(link))
                .ThrowsAsync(new AggregateException("Error in TriggerFossologyProcess"));

            // Act
            var uploadId = await ComponentCreator.CheckFossologyProcessStatus(link, sw360CreatorServiceMock.Object);

            // Assert
            Assert.AreEqual(string.Empty, uploadId);
        }

        [Test]
        public async Task CheckFossologyProcessStatus_NonExceptionScenario_ReturnsUploadId()
        {
            // Arrange
            var link = "https://example.com/fossology/process/12345";
            var sw360CreatorServiceMock = new Mock<ISw360CreatorService>();
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
            var uploadId = await ComponentCreator.CheckFossologyProcessStatus(link, sw360CreatorServiceMock.Object);

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

            IFolderAction folderAction = new FolderAction();
            IFileOperations fileOperations = new FileOperations();
            CommonAppSettings appSettings = new CommonAppSettings()
            {
                SW360 = new SW360() { URL = "http://localhost:8081/" },
                Directory = new Common.Directory(folderAction, fileOperations)
            };

            sw360CreatorServiceMock.Setup(x => x.TriggerFossologyProcess(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(fossTriggerStatus);
            sw360CreatorServiceMock.Setup(x => x.CheckFossologyProcessStatus(It.IsAny<string>())).ReturnsAsync(checkFossologyProcess);

            // Act
            string uploadId = await ComponentCreator.TriggerFossologyProcess(item, sw360CreatorServiceMock.Object, appSettings);

            // Assert           
            sw360CreatorServiceMock.Verify(x => x.TriggerFossologyProcess(It.IsAny<string>(), It.IsAny<string>()), Times.AtLeastOnce);            
        }

    }
}

