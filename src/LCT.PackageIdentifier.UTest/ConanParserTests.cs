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

namespace LCT.PackageIdentifier.UTest
{
    [TestFixture]
    public class ConanParserTests
    {
        [TestCase]
        public void ParseLockFile_GivenAInputFilePath_ReturnsSuccess()
        {
            //Arrange
            int expectedNoOfcomponents = 17;
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string packagefilepath = Path.GetFullPath(Path.Combine(outFolder, "PackageIdentifierUTTestFiles"));

            string[] Includes = { "conan.lock" };

            IFolderAction folderAction = new FolderAction();
            IFileOperations fileOperations = new FileOperations();
            CommonAppSettings appSettings = new CommonAppSettings(folderAction, fileOperations)
            {
                Conan = new Config() { Include = Includes },
                SW360 = new SW360(),
                Directory = new LCT.Common.Directory(folderAction, fileOperations)
                {
                    InputFolder = packagefilepath
                }
            };
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();

            //Act
            Bom listofcomponents = new ConanProcessor(cycloneDXBomParser.Object).ParsePackageFile(appSettings);

            //Assert
            Assert.That(expectedNoOfcomponents, Is.EqualTo(listofcomponents.Components.Count), "Checks for no of components");

        }

        [TestCase]
        public void ParseLockFile_GivenAInputFilePath_ReturnDevDependentComp()
        {            //Arrange
            string IsDev = "true";
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string packagefilepath = Path.GetFullPath(Path.Combine(outFolder, "PackageIdentifierUTTestFiles"));

            string[] Includes = { "conan.lock" };

            IFolderAction folderAction = new FolderAction();
            IFileOperations fileOperations = new FileOperations();
            CommonAppSettings appSettings = new CommonAppSettings(folderAction, fileOperations)
            {
                Conan = new Config() { Include = Includes },
                SW360 = new SW360(),
                Directory = new LCT.Common.Directory(folderAction, fileOperations)
                {
                    InputFolder = packagefilepath
                }
            };
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();            //Act
            Bom listofcomponents = new ConanProcessor(cycloneDXBomParser.Object).ParsePackageFile(appSettings);
            var component = listofcomponents.Components.Find(a => a.Name == "googletest");
            
            // Make sure the component exists
            Assert.That(component, Is.Not.Null, "Component 'googletest' not found in the list of components");
            
            // Make sure the component has properties
            Assert.That(component.Properties, Is.Not.Null, "Properties collection is null for component 'googletest'");
            
            // Check for development property using the internal:siemens:clearing:siemens:filename property that is currently used
            var property = component.Properties.FirstOrDefault(x => x.Name == "internal:siemens:clearing:siemens:filename");
            Assert.That(property, Is.Not.Null, "Property 'internal:siemens:clearing:siemens:filename' not found in component 'googletest'");
            
            var IsDevDependency = property.Value;

            //Assert
            Assert.That(IsDev, Is.EqualTo(IsDevDependency), "Checks if Dev Dependency Component or not");

        }

        [TestCase]
        public void ParseLockFile_GivenAInputFilePathExcludeComponent_ReturnComponentCount()
        {
            //Arrange
            int totalComponentsAfterExclusion = 17;
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string packagefilepath = Path.GetFullPath(Path.Combine(outFolder, "PackageIdentifierUTTestFiles"));

            string[] Includes = { "conan.lock" };
            IFolderAction folderAction = new FolderAction();
            IFileOperations fileOperations = new FileOperations();
            CommonAppSettings appSettings = new CommonAppSettings(folderAction, fileOperations)
            {
                Conan = new Config() { Include = Includes },
                SW360 = new SW360() { ExcludeComponents = ["openldap:2.6.4-shared-ossl3.1", "libcurl:7.87.0-shared-ossl3.1"] },
                Directory = new LCT.Common.Directory(folderAction, fileOperations)
                {
                    InputFolder = packagefilepath
                }
            };
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();

            //Act
            Bom listofcomponents = new ConanProcessor(cycloneDXBomParser.Object).ParsePackageFile(appSettings);

            //Assert
            Assert.That(totalComponentsAfterExclusion, Is.EqualTo(listofcomponents.Components.Count), "Checks if the excluded components have been removed");
        }

        [TestCase]
        public void IsDevDependent_GivenListOfDevComponents_ReturnsSuccess()
        {
            //Arrange
            var conanPackage = new ConanPackage() { Id = "10" };
            var buildNodeIds = new List<string> { "10", "11", "12" };
            var noOfDevDependent = 0;
            //Act
            bool actual = ConanProcessor.IsDevDependency(conanPackage, buildNodeIds, ref noOfDevDependent);

            //Assert
            Assert.That(true, Is.EqualTo(actual), "Component is a dev dependent");
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
            IFolderAction folderAction = new FolderAction();
            IFileOperations fileOperations = new FileOperations();
            CommonAppSettings appSettings = new CommonAppSettings(folderAction, fileOperations)
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

            // Act
            ConanProcessor conanProcessor = new ConanProcessor(cycloneDXBomParser.Object);
            var actual = await conanProcessor.IdentificationOfInternalComponents(componentIdentification, appSettings, mockJfrogService.Object, mockBomHelper.Object);

            // Assert
            Assert.That(actual, Is.Not.Null);
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
            IFolderAction folderAction = new FolderAction();
            IFileOperations fileOperations = new FileOperations();
            CommonAppSettings appSettings = new CommonAppSettings(folderAction, fileOperations)
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

            // Act
            ConanProcessor conanProcessor = new ConanProcessor(cycloneDXBomParser.Object);
            var actual = await conanProcessor.GetJfrogRepoDetailsOfAComponent(
                components, appSettings, mockJfrogService.Object, mockBomHelper.Object);
            var reponameActual = actual.First(x => x.Properties[0].Name == "internal:siemens:clearing:jfrog-repo-name").Properties[0].Value;

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
            IFolderAction folderAction = new FolderAction();
            IFileOperations fileOperations = new FileOperations();
            CommonAppSettings appSettings = new CommonAppSettings(folderAction, fileOperations)
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

            // Act
            ConanProcessor conanProcessor = new ConanProcessor(cycloneDXBomParser.Object);
            var actual = await conanProcessor.GetJfrogRepoDetailsOfAComponent(
                components, appSettings, mockJfrogService.Object, mockBomHelper.Object);

            var reponameActual = actual.First(x => x.Properties[0].Name == "internal:siemens:clearing:jfrog-repo-name").Properties[0].Value;

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

            ConanProcessor conanProcessor = new ConanProcessor(cycloneDXBomParser.Object);
            string[] Includes = { "SBOM_ConanCATemplate.cdx.json" };
            string packagefilepath = Path.GetFullPath(Path.Combine(OutFolder, "PackageIdentifierUTTestFiles"));

            IFolderAction folderAction = new FolderAction();
            IFileOperations fileOperations = new FileOperations();
            CommonAppSettings appSettings = new CommonAppSettings(folderAction, fileOperations)
            {
                ProjectType = "CONAN",
                Conan = new Config() { Include = Includes },
                SW360 = new SW360() { IgnoreDevDependency = true },
                Directory = new LCT.Common.Directory(folderAction, fileOperations)
                {
                    InputFolder = Path.GetFullPath(Path.Combine(OutFolder, "PackageIdentifierUTTestFiles"))
                }
            };

            //Act
            Bom listofcomponents = conanProcessor.ParsePackageFile(appSettings);

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

            var folderAction = new Mock<IFolderAction>();
            var fileOperations = new Mock<IFileOperations>();
            var appSettings = new CommonAppSettings(folderAction.Object, fileOperations.Object)
            {
                Directory = new LCT.Common.Directory(folderAction.Object, fileOperations.Object)
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

            var folderAction = new Mock<IFolderAction>();
            var fileOperations = new Mock<IFileOperations>();
            var appSettings = new CommonAppSettings(folderAction.Object, fileOperations.Object)
            {
                Directory = new LCT.Common.Directory(folderAction.Object, fileOperations.Object)
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
