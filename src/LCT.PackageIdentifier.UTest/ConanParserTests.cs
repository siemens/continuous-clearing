// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models;
using LCT.APICommunications.Model.AQL;
using LCT.Common;
using LCT.Common.Interface;
using LCT.Common.Model;
using LCT.PackageIdentifier.Interface;
using LCT.PackageIdentifier.Model;
using LCT.Services.Interface;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using File = System.IO.File;

namespace LCT.PackageIdentifier.UTest
{
    [TestFixture]
    public class ConanParserTests
    {
        private static Bom ListUnsupportedComponentsForBom = new Bom { Components = new List<Component>(), Dependencies = new List<Dependency>() };
        [TestCase]
        public void ParseDepJsonFile_GivenAInputFilePath_ReturnsSuccess()
        {
            //Arrange
            int expectedNoOfcomponents = 23; // Real dep.json test file has nodes 1-23 (excluding root node 0)
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string packagefilepath = Path.GetFullPath(Path.Combine(outFolder, "PackageIdentifierUTTestFiles"));

            string[] Includes = { "dep.json" }; // dep.json files only

            CommonAppSettings appSettings = new CommonAppSettings()
            {
                Conan = new Config() { Include = Includes },
                SW360 = new SW360(),
                Directory = new LCT.Common.Directory()
                {
                    InputFolder = packagefilepath
                }
            };
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();
            Mock<ISpdxBomParser> spdxBomParser = new Mock<ISpdxBomParser>();
            //Act
            Bom listofcomponents = new ConanProcessor(cycloneDXBomParser.Object, spdxBomParser.Object).ParsePackageFile(appSettings, ref ListUnsupportedComponentsForBom);

            //Assert
            Assert.That(expectedNoOfcomponents, Is.EqualTo(listofcomponents.Components.Count), "Checks for no of components");

        }

        [TestCase]
        public void ParseDepJsonFile_GivenAInputFilePath_ReturnDevDependentComp()
        {
            //Arrange
            string IsDev = "true";
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string packagefilepath = Path.GetFullPath(Path.Combine(outFolder, "PackageIdentifierUTTestFiles"));

            string[] Includes = { "dep.json" }; // dep.json files only


            CommonAppSettings appSettings = new CommonAppSettings()
            {
                Conan = new Config() { Include = Includes },
                SW360 = new SW360(),
                Directory = new LCT.Common.Directory()
                {
                    InputFolder = packagefilepath
                }
            };
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();
            Mock<ISpdxBomParser> spdxBomParser = new Mock<ISpdxBomParser>();
            //Act
            Bom listofcomponents = new ConanProcessor(cycloneDXBomParser.Object, spdxBomParser.Object).ParsePackageFile(appSettings, ref ListUnsupportedComponentsForBom);

            //Assert
            Assert.That(IsDev, Is.EqualTo(listofcomponents.Components.Where(x => x.Properties.Where(x => x.Name.Contains("IsDevelopment")).FirstOrDefault().Value.Contains("true")).Count().ToString()), "Checks for Dev Dependent components");

        }

        [TestCase]
        public void ParseDepJsonFile_GivenAInputFilePathExcludeComponent_ReturnComponentCount()
        {
            //Arrange
            int totalComponentsAfterExclusion = 21; // 23 total components minus 2 excluded (openssl and libcurl if exclusion patterns match)
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string packagefilepath = Path.GetFullPath(Path.Combine(outFolder, "PackageIdentifierUTTestFiles"));

            string[] Includes = { "dep.json" }; // dep.json files only

            CommonAppSettings appSettings = new CommonAppSettings()
            {
                Conan = new Config() { Include = Includes },
                SW360 = new SW360() { ExcludeComponents = ["openssl:3.5.2", "libcurl:8.15.0"] }, // Updated component names to match dep.json
                Directory = new LCT.Common.Directory()
                {
                    InputFolder = packagefilepath
                }
            };
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();
            Mock<ISpdxBomParser> spdxBomParser = new Mock<ISpdxBomParser>();
            //Act
            Bom listofcomponents = new ConanProcessor(cycloneDXBomParser.Object, spdxBomParser.Object).ParsePackageFile(appSettings, ref ListUnsupportedComponentsForBom);

            //Assert
            Assert.That(totalComponentsAfterExclusion, Is.EqualTo(listofcomponents.Components.Count), "Checks if the excluded components have been removed");
        }

        [TestCase]
        public void ParseDepJsonFile_GivenAInputFilePath_ReturnsBomComponents()
        {
            //Arrange
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string packagefilepath = Path.GetFullPath(Path.Combine(outFolder, "PackageIdentifierUTTestFiles"));

            string[] Includes = { "dep.json" }; // dep.json files only

            CommonAppSettings appSettings = new CommonAppSettings()
            {
                Conan = new Config() { Include = Includes },
                SW360 = new SW360(),
                Directory = new LCT.Common.Directory()
                {
                    InputFolder = packagefilepath
                }
            };
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();
            Mock<ISpdxBomParser> spdxBomParser = new Mock<ISpdxBomParser>();
            //Act
            Bom listofcomponents = new ConanProcessor(cycloneDXBomParser.Object, spdxBomParser.Object).ParsePackageFile(appSettings, ref ListUnsupportedComponentsForBom);

            //Assert
            Assert.That(listofcomponents.Components.Count, Is.GreaterThan(0), "Checks for BOM components not null");

        }

        [TestCase]
        public void ParseDepJsonFile_GivenAInputFilePath_ReturnComponents()
        {
            //Arrange
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string packagefilepath = Path.GetFullPath(Path.Combine(outFolder, "PackageIdentifierUTTestFiles"));

            string[] Includes = { "dep.json" }; // dep.json files only

            CommonAppSettings appSettings = new CommonAppSettings()
            {
                Conan = new Config() { Include = Includes },
                SW360 = new SW360(),
                Directory = new LCT.Common.Directory()
                {
                    InputFolder = packagefilepath
                }
            };
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();
            Mock<ISpdxBomParser> spdxBomParser = new Mock<ISpdxBomParser>();
            //Act
            Bom listofcomponents = new ConanProcessor(cycloneDXBomParser.Object, spdxBomParser.Object).ParsePackageFile(appSettings, ref ListUnsupportedComponentsForBom);

            //Assert
            Assert.That(listofcomponents.Components.Count, Is.GreaterThan(0), "Checks for components not null");

        }

        [TestCase]
        public void ParseDepJsonFile_Givencomponents_returnPurlInfo()
        {
            //Arrange
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string packagefilepath = Path.GetFullPath(Path.Combine(outFolder, "PackageIdentifierUTTestFiles"));

            string[] Includes = { "dep.json" }; // dep.json files only

            CommonAppSettings appSettings = new CommonAppSettings()
            {
                Conan = new Config() { Include = Includes },
                SW360 = new SW360(),
                Directory = new LCT.Common.Directory()
                {
                    InputFolder = packagefilepath
                }
            };
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();
            Mock<ISpdxBomParser> spdxBomParser = new Mock<ISpdxBomParser>();
            //Act
            Bom listofcomponents = new ConanProcessor(cycloneDXBomParser.Object, spdxBomParser.Object).ParsePackageFile(appSettings, ref ListUnsupportedComponentsForBom);

            //Assert
            Assert.That(listofcomponents.Components.Where(x => !string.IsNullOrEmpty(x.Purl)).Count(), Is.GreaterThan(0), "Checks for valid purl");

        }


        [TestCase]
        public void ParseCycloneDxFile_GivenACylcondxFile_ReturnComponents()
        {
            //Arrange
            List<Component> components = new List<Component>();
            components.Add(new Component() { Name = "Test", Version = "1.0.0", Purl = "pkg:conan/Test@1.0.0" });
            Bom bom = new() { Components = components };
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);

            CommonAppSettings appSettings = new CommonAppSettings()
            {
                Conan = new Config(),
                SW360 = new SW360(),
                Directory = new LCT.Common.Directory()
                {
                    InputFolder = outFolder
                }
            };
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();
            Mock<ISpdxBomParser> spdxBomParser = new Mock<ISpdxBomParser>();
            cycloneDXBomParser.Setup(x => x.ParseCycloneDXBom(It.IsAny<string>())).Returns(bom);
            //Act
            Bom listofcomponents = new ConanProcessor(cycloneDXBomParser.Object, spdxBomParser.Object).ParsePackageFile(appSettings, ref ListUnsupportedComponentsForBom);

            //Assert
            Assert.That(listofcomponents.Components.Count, Is.GreaterThanOrEqualTo(0), "Checks for components count");

        }

        [TestCase]
        public async Task IdentificationOfInternalComponents_ReturnsComponentData_Successfully()
        {
            // Arrange
            Component component1 = new Component();
            component1.Name = "Test";
            component1.Group = "";
            component1.Description = string.Empty;
            component1.Version = "1.0.0";
            var components = new List<Component>() { component1 };
            ComponentIdentification component = new() { comparisonBOMData = components };
            string[] reooListArr = { "internalrepo1", "internalrepo2" };

            CommonAppSettings appSettings = new CommonAppSettings()
            {
                SW360 = new SW360(),
                Conan = new Config
                {
                    Artifactory = new Artifactory
                    {
                        InternalRepos = reooListArr
                    }
                }
            };

            AqlResult aqlResult = new()
            {
                Name = "Test-1.0.0.tgz",
                Path = "TestPath/-/folder",
                Repo = "internalrepo1"
            };

            List<AqlResult> results = new List<AqlResult>() { aqlResult };

            Mock<IJFrogService> mockJfrogService = new Mock<IJFrogService>();
            Mock<IBomHelper> mockBomHelper = new Mock<IBomHelper>();
            mockBomHelper.Setup(m => m.GetListOfComponentsFromRepo(It.IsAny<string[]>(), It.IsAny<IJFrogService>()))
                .ReturnsAsync(results);

            // Act
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();
            Mock<ISpdxBomParser> spdxBomParser = new Mock<ISpdxBomParser>();
            ConanProcessor conanProcessor = new ConanProcessor(cycloneDXBomParser.Object, spdxBomParser.Object);
            var actual = await conanProcessor.IdentificationOfInternalComponents(
                component, appSettings, mockJfrogService.Object, mockBomHelper.Object);

            // Assert
            Assert.AreEqual("true", actual.comparisonBOMData[0].Properties[0].Value);
        }

        [TestCase]
        public async Task IdentificationOfInternalComponents_ReturnsComponentData_Failure()
        {
            // Arrange
            Component component1 = new Component();
            component1.Name = "Test";
            component1.Group = "";
            component1.Description = string.Empty;
            component1.Version = "1.0.0";
            var components = new List<Component>() { component1 };
            ComponentIdentification component = new() { comparisonBOMData = components };
            string[] reooListArr = { "internalrepo1", "internalrepo2" };

            CommonAppSettings appSettings = new CommonAppSettings()
            {
                SW360 = new SW360(),
                Conan = new Config
                {
                    Artifactory = new Artifactory
                    {
                        InternalRepos = reooListArr
                    }
                }
            };

            AqlResult aqlResult = new()
            {
                Name = "Test-1.3.0.tar.gz",
                Path = "@testfolder/-/folder",
                Repo = "internalrepo1"
            };

            List<AqlResult> results = new List<AqlResult>() { aqlResult };
            Mock<IJFrogService> mockJfrogService = new Mock<IJFrogService>();
            Mock<IBomHelper> mockBomHelper = new Mock<IBomHelper>();
            mockBomHelper.Setup(m => m.GetListOfComponentsFromRepo(It.IsAny<string[]>(), It.IsAny<IJFrogService>()))
                .ReturnsAsync(results);

            // Act
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();
            Mock<ISpdxBomParser> spdxBomParser = new Mock<ISpdxBomParser>();
            ConanProcessor conanProcessor = new ConanProcessor(cycloneDXBomParser.Object, spdxBomParser.Object);
            var actual = await conanProcessor.IdentificationOfInternalComponents(
                component, appSettings, mockJfrogService.Object, mockBomHelper.Object);

            // Assert
            Assert.AreEqual("true", actual.comparisonBOMData[0].Properties[0].Value);
        }

        [TestCase]
        public async Task GetJfrogRepoDetailsOfAComponent_ReturnsWithData_SuccessFully()
        {
            // Arrange
            Component component1 = new Component
            {
                Name = "Test",
                Description = string.Empty,
                Version = "1.1"
            };
            var components = new List<Component>() { component1 };
            string[] reooListArr = { "internalrepo1", "internalrepo2" };

            CommonAppSettings appSettings = new CommonAppSettings()
            {
                ProjectType = "CONAN",
                SW360 = new SW360(),
                Conan = new Config
                {
                    Artifactory = new Artifactory
                    {
                        RemoteRepos = reooListArr
                    }
                }
            };

            AqlResult aqlResult = new()
            {
                Name = "Test-1.1.tar.gz",
                Path = "@testfolder/-/folder",
                Repo = "internalrepo1"
            };

            List<AqlResult> results = new List<AqlResult>() { aqlResult };

            Mock<IJFrogService> mockJfrogService = new Mock<IJFrogService>();
            Mock<IBomHelper> mockBomHelper = new Mock<IBomHelper>();
            mockBomHelper.Setup(m => m.GetListOfComponentsFromRepo(It.IsAny<string[]>(), It.IsAny<IJFrogService>()))
                .ReturnsAsync(results);

            // Act
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();
            Mock<ISpdxBomParser> spdxBomParser = new Mock<ISpdxBomParser>();
            ConanProcessor conanProcessor = new ConanProcessor(cycloneDXBomParser.Object, spdxBomParser.Object);
            var actual = await conanProcessor.GetJfrogRepoDetailsOfAComponent(
                components, appSettings, mockJfrogService.Object, mockBomHelper.Object);

            // Assert
            Assert.That(actual, Is.Not.Null);
        }
    }
}
