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
            int expectedNoOfcomponents = 11; // Based on Test_Bom.cdx.json showing 11 components
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string packagefilepath = Path.GetFullPath(Path.Combine(outFolder, "PackageIdentifierUTTestFiles"));

            string[] Includes = { "*.dep.json" }; // Use pattern matching for dep.json files

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
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string packagefilepath = Path.GetFullPath(Path.Combine(outFolder, "PackageIdentifierUTTestFiles"));

            string[] Includes = { "*.dep.json" };

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
            
            // Based on Test_Bom.cdx.json, 8 components have development = "true"
            var devComponents = listofcomponents.Components.Where(x => 
                x.Properties.Any(p => p.Name == "internal:siemens:clearing:development" && p.Value == "true")).Count();

            //Assert
            Assert.That(devComponents, Is.EqualTo(8), "Checks for Dev Dependent components count");
        }

        [TestCase]
        public void ParseDepJsonFile_GivenAInputFilePath_ReturnsBomComponents()
        {
            //Arrange
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string packagefilepath = Path.GetFullPath(Path.Combine(outFolder, "PackageIdentifierUTTestFiles"));

            string[] Includes = { "*.dep.json" };

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

            string[] Includes = { "*.dep.json" };

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

            string[] Includes = { "*.dep.json" };

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
        public void IsDevDependency_GivenBuildContextComponent_ReturnsTrue()
        {
            //Arrange
            var conanPackage = new ConanPackage() { Context = "build" }; // Conan 2.0 uses context="build" for dev dependencies
            var noOfDevDependent = 0;
            //Act
            bool actual = ConanProcessor.IsDevDependency(conanPackage, ref noOfDevDependent);

            //Assert
            Assert.That(true, Is.EqualTo(actual), "Component with context=build is a dev dependent");
            Assert.That(1, Is.EqualTo(noOfDevDependent), "Dev dependent count incremented");
        }

        [TestCase]
        public void IsDevDependency_GivenHostContextComponent_ReturnsFalse()
        {
            //Arrange
            var conanPackage = new ConanPackage() { Context = "host", Libs = true }; // Production component
            var noOfDevDependent = 0;
            //Act
            bool actual = ConanProcessor.IsDevDependency(conanPackage, ref noOfDevDependent);

            //Assert
            Assert.That(false, Is.EqualTo(actual), "Component with context=host is not a dev dependent");
            Assert.That(0, Is.EqualTo(noOfDevDependent), "Dev dependent count not incremented");
        }

        

        [Test]
        public async Task IdentificationOfInternalComponents_ReturnsComponentData_Successfully()
        {
            // Arrange
            Component component = new Component()
            {
                Name = "securitycommunicationmanager",
                Description = string.Empty,
                Version = "2.6.5",
                Purl = "pkg:conan/securitycommunicationmanager@2.6.5"
            };

            var components = new List<Component>() { component };
            ComponentIdentification componentIdentification = new() { comparisonBOMData = components };
            string[] repoList = { "internalrepo1", "internalrepo2" };

            CommonAppSettings appSettings = new CommonAppSettings()
            {
                SW360 = new SW360(),
                Conan = new Config
                {
                    Artifactory = new Artifactory
                    {
                        InternalRepos = repoList
                    }
                }
            };

            AqlResult aqlResult = new()
            {
                Name = "index.json",
                Path = "siemens-energy/securitycommunicationmanager/2.7.1/stable",
                Repo = "internalrepo1"
            };

            List<AqlResult> results = new List<AqlResult>() { aqlResult };
            Mock<IJFrogService> mockJfrogService = new Mock<IJFrogService>();
            Mock<IBomHelper> mockBomHelper = new Mock<IBomHelper>();
            mockBomHelper.Setup(m => m.GetListOfComponentsFromRepo(It.IsAny<string[]>(), It.IsAny<IJFrogService>()))
                .ReturnsAsync(results);
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();
            Mock<ISpdxBomParser> spdxBomParser = new Mock<ISpdxBomParser>();
            // Act
            ConanProcessor conanProcessor = new ConanProcessor(cycloneDXBomParser.Object, spdxBomParser.Object);
            var actual = await conanProcessor.IdentificationOfInternalComponents(componentIdentification, appSettings, mockJfrogService.Object, mockBomHelper.Object);

            // Assert
            Assert.That(actual, Is.Not.Null);
        }

        [Test]
        public async Task IdentificationOfInternalComponents_ReturnsComponentData_Failure()
        {
            // Arrange
            Component component = new Component()
            {
                Name = "Test",
                Description = string.Empty,
                Version = "1.0.0",
                Purl = "pkg:conan/Test@1.0.0"
            };
            var components = new List<Component>() { component };
            ComponentIdentification componentIdentification = new() { comparisonBOMData = components };
            string[] repoList = { "internalrepo1", "internalrepo2" };

            CommonAppSettings appSettings = new CommonAppSettings()
            {
                SW360 = new SW360(),
                Conan = new Config
                {
                    Artifactory = new Artifactory
                    {
                        InternalRepos = repoList
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

            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();
            Mock<ISpdxBomParser> spdxBomParser = new Mock<ISpdxBomParser>();
            // Act
            ConanProcessor conanProcessor = new ConanProcessor(cycloneDXBomParser.Object, spdxBomParser.Object);
            var actual = await conanProcessor.IdentificationOfInternalComponents(
                componentIdentification, appSettings, mockJfrogService.Object, mockBomHelper.Object);

            // Assert
            Assert.That(actual, Is.Not.Null);
            Assert.That(actual.comparisonBOMData, Is.Not.Null);
        }

        [Test]
        public async Task GetJfrogRepoDetailsOfAComponent_ReturnsWithData_SuccessFully()
        {
            // Arrange
            Component component = new Component()
            {
                Name = "securitycommunicationmanager",
                Description = string.Empty,
                Version = "2.6.5",
                Purl = "pkg:conan/securitycommunicationmanager@2.6.5"
            };
            var components = new List<Component>() { component };
            string[] repoList = { "internalrepo1", "internalrepo2" };

            CommonAppSettings appSettings = new CommonAppSettings()
            {
                ProjectType = "Conan",
                SW360 = new SW360(),
                Conan = new Config
                {
                    Artifactory = new Artifactory
                    {
                        RemoteRepos = repoList
                    }
                }
            };
            AqlResult aqlResult = new()
            {
                Name = "index.json",
                Path = "siemens-energy/securitycommunicationmanager/2.6.5/stable",
                Repo = "internalrepo1"
            };

            List<AqlResult> results = new List<AqlResult>() { aqlResult };

            Mock<IJFrogService> mockJfrogService = new Mock<IJFrogService>();
            Mock<IBomHelper> mockBomHelper = new Mock<IBomHelper>();
            mockBomHelper.Setup(m => m.GetListOfComponentsFromRepo(It.IsAny<string[]>(), It.IsAny<IJFrogService>()))
                .ReturnsAsync(results);
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();
            Mock<ISpdxBomParser> spdxBomParser = new Mock<ISpdxBomParser>();
            // Act
            ConanProcessor conanProcessor = new ConanProcessor(cycloneDXBomParser.Object, spdxBomParser.Object);
            var actual = await conanProcessor.GetJfrogRepoDetailsOfAComponent(
                components, appSettings, mockJfrogService.Object, mockBomHelper.Object);
            var reponameActual = actual.First(x => x.Properties.Any(p => p.Name == "internal:siemens:clearing:jfrog-repo-name")).Properties.First(p => p.Name == "internal:siemens:clearing:jfrog-repo-name").Value;

            // Assert
            Assert.That(actual, Is.Not.Null);
            Assert.That(aqlResult.Repo, Is.EqualTo(reponameActual));
        }

        [Test]
        public async Task GetArtifactoryRepoName_Conan_ReturnsNotFound_ReturnsFailure()
        {
            // Arrange
            Component component = new Component()
            {
                Name = "securitycommunicationmanager",
                Description = string.Empty,
                Version = "2.6.5",
                Purl = "pkg:conan/securitycommunicationmanager@2.6.5"
            };
            var components = new List<Component>() { component };
            string[] repoList = { "internalrepo1", "internalrepo2" };

            CommonAppSettings appSettings = new CommonAppSettings()
            {
                ProjectType = "Conan",
                SW360 = new SW360(),
                Conan = new Config
                {
                    Artifactory = new Artifactory
                    {
                        RemoteRepos = repoList
                    }
                }
            };
            AqlResult aqlResult = new()
            {
                Name = "index.json",
                Path = "siemens-energy/securitycommunicationmanager/2.7.1/stable",
                Repo = "internalrepo1"
            };

            List<AqlResult> results = new() { aqlResult };

            Mock<IJFrogService> mockJfrogService = new Mock<IJFrogService>();
            Mock<IBomHelper> mockBomHelper = new Mock<IBomHelper>();
            mockBomHelper.Setup(m => m.GetListOfComponentsFromRepo(It.IsAny<string[]>(), It.IsAny<IJFrogService>()))
                .ReturnsAsync(results);
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();
            Mock<ISpdxBomParser> spdxBomParser = new Mock<ISpdxBomParser>();
            // Act
            ConanProcessor conanProcessor = new ConanProcessor(cycloneDXBomParser.Object, spdxBomParser.Object);
            var actual = await conanProcessor.GetJfrogRepoDetailsOfAComponent(
                components, appSettings, mockJfrogService.Object, mockBomHelper.Object);

            var reponameActual = actual.First(x => x.Properties.Any(p => p.Name == "internal:siemens:clearing:jfrog-repo-name")).Properties.First(p => p.Name == "internal:siemens:clearing:jfrog-repo-name").Value;

            Assert.That("Not Found in JFrogRepo", Is.EqualTo(reponameActual));
        }

        [Test]
        public void ParsePackageConfig_GivenAInputFilePathAlongWithSBOMTemplate_ReturnTotalComponentsList()
        {
            //Arrange
            int expectednoofcomponents = 0;
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string OutFolder = Path.GetDirectoryName(exePath);
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();
            Mock<ISpdxBomParser> spdxBomParser = new Mock<ISpdxBomParser>();
            ConanProcessor conanProcessor = new ConanProcessor(cycloneDXBomParser.Object, spdxBomParser.Object);
            string[] Includes = { "SBOM_ConanCATemplate.cdx.json" };
            string packagefilepath = Path.GetFullPath(Path.Combine(OutFolder, "PackageIdentifierUTTestFiles"));

            CommonAppSettings appSettings = new CommonAppSettings()
            {
                ProjectType = "CONAN",
                Conan = new Config() { Include = Includes },
                SW360 = new SW360() { IgnoreDevDependency = true },
                Directory = new LCT.Common.Directory()
                {
                    InputFolder = Path.GetFullPath(Path.Combine(OutFolder, "PackageIdentifierUTTestFiles"))
                }
            };

            //Act
            Bom listofcomponents = conanProcessor.ParsePackageFile(appSettings, ref ListUnsupportedComponentsForBom);

            //Assert
            Assert.That(expectednoofcomponents, Is.EqualTo(listofcomponents.Components.Count), "Checks for no of components");
        }

        [Test]
        public void CreateFileForMultipleVersions_GivenComponentsWithMultipleVersions_CreatesFileSuccessfully()
        {
            // Arrange
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string OutFolder = Path.GetDirectoryName(exePath);

            var componentsWithMultipleVersions = new List<Component>
            {
                new Component { Name = "ComponentA", Version = "1.0.0", Description = "DescriptionA" },
                new Component { Name = "ComponentA", Version = "2.0.0", Description = "DescriptionA" },
                new Component { Name = "ComponentB", Version = "1.0.0", Description = "DescriptionB" },
                new Component { Name = "ComponentB", Version = "2.0.0", Description = "DescriptionB" }
            };

            var appSettings = new CommonAppSettings()
            {
                Directory = new LCT.Common.Directory()
                {
                    OutputFolder = Path.GetFullPath(Path.Combine(OutFolder, "PackageIdentifierUTTestFiles"))
                }
            };

            // Act
            ConanProcessor.CreateFileForMultipleVersions(componentsWithMultipleVersions, appSettings);

            // Assert
            string filePath = Path.GetFullPath(Path.Combine(OutFolder, "PackageIdentifierUTTestFiles", "ContinuousClearing_Multipleversions.json"));
            Assert.IsTrue(File.Exists(filePath), "The file was not created.");
        }

        [Test]
        public void CreateFileForMultipleVersions_FileAlreadyExists_UpdatesFileSuccessfully()
        {
            // Arrange
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string outputFolder = Path.GetFullPath(Path.Combine(outFolder, "PackageIdentifierUTTestFiles"));
            string filePath = Path.GetFullPath(Path.Combine(outFolder, "PackageIdentifierUTTestFiles", "ContinuousClearing_Multipleversions.json"));

            // Create an initial file with some content
            var initialContent = new MultipleVersions
            {
                Conan = new List<MultipleVersionValues>
                {
                    new MultipleVersionValues { ComponentName = "InitialComponent", ComponentVersion = "1.0.0", PackageFoundIn = "InitialDescription" }
                }
            };
            File.WriteAllText(filePath, JsonConvert.SerializeObject(initialContent));

            var componentsWithMultipleVersions = new List<Component>
            {
                new Component { Name = "ComponentA", Version = "1.0.0", Description = "DescriptionA" },
                new Component { Name = "ComponentA", Version = "2.0.0", Description = "DescriptionA" },
                new Component { Name = "ComponentB", Version = "1.0.0", Description = "DescriptionB" },
                new Component { Name = "ComponentB", Version = "2.0.0", Description = "DescriptionB" }
            };

            var appSettings = new CommonAppSettings()
            {
                Directory = new LCT.Common.Directory()
                {
                    OutputFolder = outputFolder
                }
            };

            // Act
            ConanProcessor.CreateFileForMultipleVersions(componentsWithMultipleVersions, appSettings);

            // Assert
            Assert.IsTrue(File.Exists(filePath), "The file was not created.");
        }
    }
}
