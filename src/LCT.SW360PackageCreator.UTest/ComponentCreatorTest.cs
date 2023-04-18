// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT

// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models;
using LCT.APICommunications.Model;
using LCT.APICommunications.Model.Foss;
using LCT.Common;
using LCT.Common.Model;
using LCT.Facade.Interfaces;
using LCT.Services;
using LCT.Services.Interface;
using LCT.SW360PackageCreator;
using LCT.SW360PackageCreator.Interfaces;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using UnitTestUtilities;


namespace NUnitTestProject1
{
    public class ComponentCreatorTest
    {
        [SetUp]
        public void Setup()
        {
            // implement here
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
            string attachmentJson = outFolder + @"..\..\..\src\LCT.SW360PackageCreator.UTest\ComponentCreatorUTFiles\Attachment.json";

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
                Links = new Links() { Self = new Self() { Href = "test_link" } }
            };
            ComparisonBomData item = new ComparisonBomData()
            {
                Name = "test",
                Version = "1",
                ReleaseID = "89768ae1b0ea9dc061328b8f32792cbd"

            };
            CommonAppSettings appSettings = new CommonAppSettings()
            {
                SW360URL = "http://localhost:8081/"
            };
            sw360CreatorServiceMock.Setup(x => x.TriggerFossologyProcess(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(fossTriggerStatus);
           

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
                SW360URL = "http://localhost:8081/"
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

            properties.Add(new Property()
            {
                Name = "internal:siemens:clearing:development",
                Value = "true"
            });

            Bom bom = new Bom();
            bom.Components = new List<Component>()
                {
                    new Component() { Name = "adduser",Version="3.118",Group="",Purl="pkg:deb/debian/adduser@3.118@arch=source",Properties = properties,Cpe="true" },
                };

            CommonAppSettings CommonAppSettings = new CommonAppSettings();
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

    }
}

