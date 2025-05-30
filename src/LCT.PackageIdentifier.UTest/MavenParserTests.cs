﻿// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// --------------------------------------------------------------------------------------------------------------------

using CycloneDX.Models;
using LCT.APICommunications.Model.AQL;
using LCT.Common;
using LCT.Common.Constants;
using LCT.Common.Interface;
using LCT.Common.Model;
using LCT.PackageIdentifier.Interface;
using LCT.PackageIdentifier.Model;
using LCT.Services.Interface;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace LCT.PackageIdentifier.UTest
{
    [TestFixture]
    public class MavenParserTests
    {
        private MavenProcessor _mavenProcessor;

        [SetUp]
        public void Setup()
        {
            _mavenProcessor = new MavenProcessor(Mock.Of<ICycloneDXBomParser>());
        }

        [Test]
        public void AddSiemensDirectProperty_ShouldAddProperty_WhenMavenDirectDependencyExists()
        {
            // Arrange
            var bom = new Bom
            {
                Components = new List<Component>
                {
                    new Component
                    {
                        Name = "Component1",
                        Version = "1.0.0"
                    },
                    new Component
                    {
                        Name = "Component2",
                        Version = "2.0.0"
                    }
                },
                Dependencies = new List<Dependency>
                {
                    new Dependency
                    {
                        Ref = "Component1:1.0.0"
                    },
                    new Dependency
                    {
                        Ref = "Component2:2.0.0"
                    }
                }
            };

            // Act
            _mavenProcessor.AddSiemensDirectProperty(ref bom);

            // Assert
            Assert.AreEqual("true", bom.Components[0].Properties[0].Value);
            Assert.AreEqual("true", bom.Components[1].Properties[0].Value);
        }

        [Test]
        public void AddSiemensDirectProperty_ShouldNotAddProperty_WhenMavenDirectDependencyDoesNotExist()
        {
            // Arrange
            var bom = new Bom
            {
                Components = new List<Component>
                {
                    new Component
                    {
                        Name = "Component1",
                        Version = "1.0.0"
                    },
                    new Component
                    {
                        Name = "Component2",
                        Version = "2.0.0"
                    }
                },
                Dependencies = new List<Dependency>
                {
                    new Dependency
                    {
                        Ref = "Component3:3.0.0"
                    },
                    new Dependency
                    {
                        Ref = "Component4:4.0.0"
                    }
                }
            };

            // Act
            _mavenProcessor.AddSiemensDirectProperty(ref bom);

            // Assert
            Assert.AreEqual("false", bom.Components[0].Properties[0].Value);
            Assert.AreEqual("false", bom.Components[1].Properties[0].Value);
        }

        [Test]
        public void ParsePackageFile_PackageLockWithDuplicateComponents_ReturnsCountOfDuplicates()
        {
            //Arrange
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string filepath = Path.GetFullPath(Path.Combine(outFolder, "PackageIdentifierUTTestFiles"));
            string[] Includes = { "*_Maven.cdx.json" };
            string[] Excludes = { "lol" };

            IFolderAction folderAction = new FolderAction();
            IFileOperations fileOperations = new FileOperations();
            CommonAppSettings appSettings = new CommonAppSettings(folderAction, fileOperations)
            {
                ProjectType = "MAVEN",
                Maven = new Config() { Include = Includes, Exclude = Excludes },
                SW360 = new SW360() { IgnoreDevDependency = true },
                Directory = new LCT.Common.Directory(folderAction, fileOperations)
                {
                    InputFolder = filepath
                }
            };
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();
            MavenProcessor MavenProcessor = new MavenProcessor(cycloneDXBomParser.Object);

            //Act
            Bom bom = MavenProcessor.ParsePackageFile(appSettings);

            //Assert
            Assert.That(bom.Components.Count, Is.EqualTo(1), "Returns the count of components");
            Assert.That(bom.Dependencies.Count, Is.EqualTo(1), "Returns the count of dependencies");

        }

        [Test]
        public async Task IdentificationOfInternalComponents_ReturnsComponentData_Successfully()
        {
            // Arrange
            Component component1 = new Component();
            component1.Name = "junit";
            component1.Group = "";
            component1.Description = string.Empty;
            component1.Version = "1.0.0";
            var components = new List<Component>() { component1 };
            ComponentIdentification component = new() { comparisonBOMData = components };
            string[] reooListArr = { "internalrepo1", "internalrepo2" };
            IFolderAction folderAction = new FolderAction();
            IFileOperations fileOperations = new FileOperations();
            CommonAppSettings appSettings = new CommonAppSettings(folderAction, fileOperations)
            {
                SW360 = new SW360(),
                Maven = new Config
                {
                    Artifactory = new Artifactory
                    {
                        InternalRepos = reooListArr
                    }
                }
            };

            AqlResult aqlResult = new()
            {
                Name = "animations-1.0.0.tgz",
                Path = "@testfolder/-/folder",
                Repo = "internalrepo1"
            };

            List<AqlResult> results = new List<AqlResult>() { aqlResult };
            Mock<IJFrogService> mockJfrogService = new Mock<IJFrogService>();
            Mock<IBomHelper> mockBomHelper = new Mock<IBomHelper>();
            mockBomHelper.Setup(m => m.GetListOfComponentsFromRepo(It.IsAny<string[]>(), It.IsAny<IJFrogService>()))
                .ReturnsAsync(results);
            mockBomHelper.Setup(m => m.GetFullNameOfComponent(It.IsAny<Component>())).Returns("junit");
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();

            // Act
            MavenProcessor mavenProcessor = new MavenProcessor(cycloneDXBomParser.Object);
            var actual = await mavenProcessor.IdentificationOfInternalComponents(component, appSettings, mockJfrogService.Object, mockBomHelper.Object);

            // Assert
            Assert.That(actual, Is.Not.Null);
        }

        [Test]
        public async Task IdentificationOfInternalComponents_ReturnsComponentData2_Successfully()
        {
            // Arrange
            Component component1 = new Component();
            component1.Name = "junit";
            component1.Group = "";
            component1.Description = string.Empty;
            component1.Version = "1.0.0";
            var components = new List<Component>() { component1 };
            ComponentIdentification component = new() { comparisonBOMData = components };
            string[] reooListArr = { "internalrepo1", "internalrepo2" };
            IFolderAction folderAction = new FolderAction();
            IFileOperations fileOperations = new FileOperations();
            CommonAppSettings appSettings = new CommonAppSettings(folderAction, fileOperations)
            {
                SW360 = new SW360(),
                Maven = new Config
                {
                    Artifactory = new Artifactory
                    {
                        InternalRepos = reooListArr
                    }
                }
            };

            AqlResult aqlResult = new()
            {
                Name = "animations-common_license-1.0.0.tgz",
                Path = "@testfolder/-/folder",
                Repo = "internalrepo1"
            };

            List<AqlResult> results = new List<AqlResult>() { aqlResult };
            Mock<IJFrogService> mockJfrogService = new Mock<IJFrogService>();
            Mock<IBomHelper> mockBomHelper = new Mock<IBomHelper>();
            mockBomHelper.Setup(m => m.GetListOfComponentsFromRepo(It.IsAny<string[]>(), It.IsAny<IJFrogService>()))
                .ReturnsAsync(results);
            mockBomHelper.Setup(m => m.GetFullNameOfComponent(It.IsAny<Component>())).Returns("junit");
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();

            // Act
            MavenProcessor mavenProcessor = new MavenProcessor(cycloneDXBomParser.Object);
            var actual = await mavenProcessor.IdentificationOfInternalComponents(component, appSettings, mockJfrogService.Object, mockBomHelper.Object);

            // Assert
            Assert.That(actual, Is.Not.Null);
        }

        [Test]
        public async Task IdentificationOfInternalComponents_ReturnsComponentData3_Successfully()
        {
            // Arrange
            Component component1 = new Component
            {
                Name = "junit",
                Group = "junit",
                Description = string.Empty,
                Version = "1.0.0"
            };
            var components = new List<Component>() { component1 };
            ComponentIdentification componentIdentification = new() { comparisonBOMData = components };
            string[] reooListArr = { "internalrepo1", "internalrepo2" };
            IFolderAction folderAction = new FolderAction();
            IFileOperations fileOperations = new FileOperations();
            CommonAppSettings appSettings = new CommonAppSettings(folderAction, fileOperations)
            {
                SW360 = new SW360(),
                Maven = new Config
                {
                    Artifactory = new Artifactory
                    {
                        InternalRepos = reooListArr
                    }
                }
            };

            AqlResult aqlResult = new()
            {
                Name = "animations-common-1.0.0.tgz",
                Path = "@testfolder/-/folder",
                Repo = "internalrepo1"
            };

            List<AqlResult> results = new List<AqlResult>() { aqlResult };
            Mock<IJFrogService> mockJfrogService = new Mock<IJFrogService>();
            Mock<IBomHelper> mockBomHelper = new Mock<IBomHelper>();
            mockBomHelper.Setup(m => m.GetListOfComponentsFromRepo(It.IsAny<string[]>(), It.IsAny<IJFrogService>()))
                .ReturnsAsync(results);
            mockBomHelper.Setup(m => m.GetFullNameOfComponent(It.IsAny<Component>())).Returns("junit/junit");
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();

            // Act
            MavenProcessor mavenProcessor = new MavenProcessor(cycloneDXBomParser.Object);
            var actual = await mavenProcessor.IdentificationOfInternalComponents(
                componentIdentification, appSettings, mockJfrogService.Object, mockBomHelper.Object);

            // Assert
            Assert.That(actual, Is.Not.Null);
        }

        [Test]
        public async Task GetJfrogRepoDetailsOfAComponent_ReturnsWithData_SuccessFully()
        {
            // Arrange
            Component component1 = new Component
            {
                Name = "junit",
                Group = "junit",
                Description = string.Empty,
                Version = "1.0.0"
            };
            var components = new List<Component>() { component1 };
            string[] reooListArr = { "internalrepo1", "internalrepo2" };
            IFolderAction folderAction = new FolderAction();
            IFileOperations fileOperations = new FileOperations();
            CommonAppSettings appSettings = new CommonAppSettings(folderAction, fileOperations)
            {
                ProjectType = "Maven",
                SW360 = new SW360(),
                Maven = new Config
                {
                    Artifactory = new Artifactory
                    {
                        RemoteRepos = reooListArr
                    }
                }
            };
            AqlResult aqlResult = new()
            {
                Name = "animations-common-1.0.0.tgz",
                Path = "@testfolder/-/folder",
                Repo = "internalrepo1"
            };

            List<AqlResult> results = new List<AqlResult>() { aqlResult };

            Mock<IJFrogService> mockJfrogService = new Mock<IJFrogService>();
            Mock<IBomHelper> mockBomHelper = new Mock<IBomHelper>();
            mockBomHelper.Setup(m => m.GetListOfComponentsFromRepo(It.IsAny<string[]>(), It.IsAny<IJFrogService>()))
                .ReturnsAsync(results);
            mockBomHelper.Setup(m => m.GetFullNameOfComponent(It.IsAny<Component>())).Returns("junit/junit");
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();

            // Act
            MavenProcessor mavenProcessor = new MavenProcessor(cycloneDXBomParser.Object);
            var actual = await mavenProcessor.GetJfrogRepoDetailsOfAComponent(
                components, appSettings, mockJfrogService.Object, mockBomHelper.Object);

            // Assert
            Assert.That(actual, Is.Not.Null);
        }

        [Test]
        public async Task GetJfrogRepoDetailsOfAComponent_ReturnsWithData2_SuccessFully()
        {
            // Arrange
            Component component1 = new Component
            {
                Name = "junit",
                Group = "",
                Description = string.Empty,
                Version = "1.0.0"
            };
            var components = new List<Component>() { component1 };
            string[] reooListArr = { "internalrepo1", "internalrepo2" };
            IFolderAction folderAction = new FolderAction();
            IFileOperations fileOperations = new FileOperations();
            CommonAppSettings appSettings = new CommonAppSettings(folderAction, fileOperations)
            {
                ProjectType = "Maven",
                SW360 = new SW360(),
                Maven = new Config
                {
                    Artifactory = new Artifactory
                    {
                        RemoteRepos = reooListArr
                    }
                }
            };
            AqlResult aqlResult = new()
            {
                Name = "animations-common-1.0.0.tgz",
                Path = "@testfolder/-/folder",
                Repo = "internalrepo1"
            };

            List<AqlResult> results = new List<AqlResult>() { aqlResult };

            Mock<IJFrogService> mockJfrogService = new Mock<IJFrogService>();
            Mock<IBomHelper> mockBomHelper = new Mock<IBomHelper>();
            mockBomHelper.Setup(m => m.GetListOfComponentsFromRepo(It.IsAny<string[]>(), It.IsAny<IJFrogService>()))
                .ReturnsAsync(results);
            mockBomHelper.Setup(m => m.GetFullNameOfComponent(It.IsAny<Component>())).Returns("junit");
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();

            // Act
            MavenProcessor mavenProcessor = new MavenProcessor(cycloneDXBomParser.Object);
            var actual = await mavenProcessor.GetJfrogRepoDetailsOfAComponent(
                components, appSettings, mockJfrogService.Object, mockBomHelper.Object);

            // Assert
            Assert.That(actual, Is.Not.Null);
        }

        [Test]
        public void DevDependencyIdentificationLogic_ReturnsCountOfDevDependentcomponents_SuccessFully()
        {
            //Arrange
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string filepath = Path.GetFullPath(Path.Combine(outFolder, "PackageIdentifierUTTestFiles", "MavenDevDependency", "WithDev"));
            string[] Includes = { "*.cdx.json" };
            string[] Excludes = { "lol" };

            IFolderAction folderAction = new FolderAction();
            IFileOperations fileOperations = new FileOperations();
            CommonAppSettings appSettings = new CommonAppSettings(folderAction, fileOperations)
            {
                ProjectType = "MAVEN",
                Maven = new Config() { Include = Includes, Exclude = Excludes },
                SW360 = new SW360() { IgnoreDevDependency = true },
                Directory = new LCT.Common.Directory(folderAction, fileOperations)
                {
                    InputFolder = filepath
                }
            };
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();
            MavenProcessor MavenProcessor = new MavenProcessor(cycloneDXBomParser.Object);

            //Act
            MavenProcessor.ParsePackageFile(appSettings);

            //Assert
            Assert.That(BomCreator.bomKpiData.DevDependentComponents, Is.EqualTo(9), "Returns the count of components");

        }
        [Test]
        public void DevDependencyIdentificationLogic_ReturnsCountOfComponents_WithoutDevdependency()
        {
            //Arrange
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string filepath = Path.GetFullPath(Path.Combine(outFolder, "PackageIdentifierUTTestFiles", "MavenDevDependency", "WithOneInputFile"));
            string[] Includes = { "*.cdx.json" };
            string[] Excludes = { "lol" };

            IFolderAction folderAction = new FolderAction();
            IFileOperations fileOperations = new FileOperations();
            CommonAppSettings appSettings = new CommonAppSettings(folderAction, fileOperations)
            {
                ProjectType = "MAVEN",
                Maven = new Config() { Include = Includes, Exclude = Excludes },
                SW360 = new SW360() { IgnoreDevDependency = true },
                Directory = new LCT.Common.Directory(folderAction, fileOperations)
                {
                    InputFolder = filepath
                }
            };
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();
            MavenProcessor MavenProcessor = new MavenProcessor(cycloneDXBomParser.Object);

            //Act
            Bom bom = MavenProcessor.ParsePackageFile(appSettings);

            //Assert
            Assert.That(BomCreator.bomKpiData.DevDependentComponents, Is.EqualTo(3), "Returns the count of components");

        }

        [Test]
        public void ParsePackageFile_GivenAInputFilePathAlongWithSBOMTemplate_ReturnTotalComponentsList()
        {
            //Arrange
            int expectednoofcomponents = 1;
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string filepath = Path.GetFullPath(Path.Combine(outFolder, "PackageIdentifierUTTestFiles"));
            string[] Includes = { "CycloneDX_Maven.cdx.json", "SBOMTemplate_Maven.cdx.json", "SBOM_MavenCATemplate.cdx.json" };
            string[] Excludes = { "lol" };

            IFolderAction folderAction = new FolderAction();
            IFileOperations fileOperations = new FileOperations();
            CommonAppSettings appSettings = new CommonAppSettings(folderAction, fileOperations)
            {
                ProjectType = "MAVEN",
                Maven = new Config() { Include = Includes, Exclude = Excludes },
                SW360 = new SW360() { IgnoreDevDependency = true },
                Directory = new LCT.Common.Directory(folderAction, fileOperations)
                {
                    InputFolder = filepath,

                }
            };
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();
            MavenProcessor MavenProcessor = new MavenProcessor(cycloneDXBomParser.Object);

            //Act
            Bom bom = MavenProcessor.ParsePackageFile(appSettings);

            //Assert
            Assert.That(expectednoofcomponents, Is.EqualTo(bom.Components.Count), "Checks for no of components");

        }

        [Test]
        public void ParsePackageFile_GivenAInputFilePathAlongWithSBOMTemplate_ReturnUpdatedComponents()
        {
            //Arrange
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string filepath = Path.GetFullPath(Path.Combine(outFolder, "PackageIdentifierUTTestFiles"));
            string[] Includes = { "CycloneDX_Maven.cdx.json" };
            string[] Excludes = { "lol" };

            IFolderAction folderAction = new FolderAction();
            IFileOperations fileOperations = new FileOperations();
            CommonAppSettings appSettings = new CommonAppSettings(folderAction, fileOperations)
            {
                ProjectType = "MAVEN",
                Maven = new Config() { Include = Includes, Exclude = Excludes },
                SW360 = new SW360() { IgnoreDevDependency = true },
                Directory = new LCT.Common.Directory(folderAction, fileOperations)
                {
                    InputFolder = filepath,

                }
            };
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();
            MavenProcessor MavenProcessor = new MavenProcessor(cycloneDXBomParser.Object);

            //Act
            Bom bom = MavenProcessor.ParsePackageFile(appSettings);

            bool isUpdated = bom.Components.Exists(x => x.Properties != null && x.Properties.Exists(x => x.Name == Dataconstant.Cdx_IdentifierType && x.Value == Dataconstant.Discovered));

            //Assert
            Assert.IsTrue(isUpdated, "Checks For Updated Property In List ");

        }

    }
}
