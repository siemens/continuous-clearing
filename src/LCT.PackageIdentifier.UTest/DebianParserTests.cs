// --------------------------------------------------------------------------------------------------------------------
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
using LCT.Services.Interface;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;

namespace LCT.PackageIdentifier.UTest
{
    [TestFixture]
    public class DebianParserTests
    {
        readonly DebianProcessor _debianProcessor;

        private DebianProcessor _mdebianProcessor;
        private Mock<IBomHelper> _mockBomHelper;
        private Mock<IJFrogService> _mockJFrogService;
        private Mock<ICycloneDXBomParser> _mockCycloneDXBomParser;
        private Mock<ISpdxBomParser> _mockSpdxBomParser;

        [SetUp]
        public void Setup()
        {
            _mockBomHelper = new Mock<IBomHelper>();
            _mockJFrogService = new Mock<IJFrogService>();
            _mockCycloneDXBomParser = new Mock<ICycloneDXBomParser>();
            _mockSpdxBomParser = new Mock<ISpdxBomParser>();
            _mdebianProcessor = new DebianProcessor(_mockCycloneDXBomParser.Object, _mockSpdxBomParser.Object);
        }


        [Test]
        public void GetArtifactoryRepoName_WithValidComponent_ReturnsRepoName()
        {
            // Arrange
            var aqlResultList = new List<AqlResult>
            {
                new AqlResult { Name = "component_1.deb", Repo = "repo1" },
                new AqlResult { Name = "component_2.deb", Repo = "repo2" }
            };
            var component = new Component { Name = "component", Version = "1" };
            string jfrogRepoPackageName;
            string jfrogRepoPath;
            _mockBomHelper.Setup(x => x.GetFullNameOfComponent(component)).Returns("component_1.deb");

            // Act
            var repoName = _debianProcessor.GetArtifactoryRepoName(aqlResultList, component, _mockBomHelper.Object, out jfrogRepoPackageName, out jfrogRepoPath);

            // Assert
            Assert.AreEqual("repo1", repoName);
            Assert.AreEqual("component_1.deb", jfrogRepoPackageName);
            Assert.AreEqual("repo1/component_1.deb", jfrogRepoPath);
        }

        [Test]
        public void GetArtifactoryRepoName_WithInvalidComponent_ReturnsNotFoundInRepo()
        {
            // Arrange
            var aqlResultList = new List<AqlResult>
            {
                new AqlResult { Name = "component_1.deb", Repo = "repo1" },
                new AqlResult { Name = "component_2.deb", Repo = "repo2" }
            };
            var component = new Component { Name = "invalid_component", Version = "1.0" };
            string jfrogRepoPackageName;
            string jfrogRepoPath;

            // Act
            var repoName = _debianProcessor.GetArtifactoryRepoName(aqlResultList, component, _mockBomHelper.Object, out jfrogRepoPackageName, out jfrogRepoPath);

            // Assert
            Assert.AreEqual("Not Found in JFrogRepo", repoName);
            Assert.AreEqual("Package name not found in Jfrog", jfrogRepoPackageName);
            Assert.AreEqual("Jfrog repo path not found", jfrogRepoPath);
        }

        [Test]
        public void GetArtifactoryRepoName_WithFullNameVersionMismatch_ReturnsRepoName()
        {
            // Arrange
            var aqlResultList = new List<AqlResult>
            {
                new AqlResult { Name = "full_name_component_1.deb", Repo = "repo1" },
                new AqlResult { Name = "component_2.deb", Repo = "repo2" }
            };
            var component = new Component { Name = "component", Version = "2" };
            string jfrogRepoPackageName;
            string jfrogRepoPath;

            // Act
            var repoName = _debianProcessor.GetArtifactoryRepoName(aqlResultList, component, _mockBomHelper.Object, out jfrogRepoPackageName, out jfrogRepoPath);

            // Assert
            Assert.AreEqual("repo2", repoName);
            Assert.AreEqual("component_2.deb", jfrogRepoPackageName);
            Assert.AreEqual("repo2/component_2.deb", jfrogRepoPath);
        }

        public DebianParserTests()
        {
            List<Component> components = new List<Component>();
            components.Add(new Component() { Name = "adduser", Version = "3.118", Purl = "pkg:deb/debian/adduser@3.118?arch=all\u0026distro=debian-10" });
            components.Add(new Component() { Name = "base-files", Version = "10.3+deb10u10", Purl = "pkg:deb/debian/base-files@10.3+deb10u10?arch=amd64\u0026distro=debian-10" });
            Bom bom = new() { Components = components };

            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();
            cycloneDXBomParser.Setup(x => x.ParseCycloneDXBom(It.IsAny<string>())).Returns(bom);
            Mock<ISpdxBomParser> spdxBomParser = new Mock<ISpdxBomParser>();

            _debianProcessor = new DebianProcessor(cycloneDXBomParser.Object, spdxBomParser.Object);
        }

        [Test]
        public void ParsePackageConfig_GivenAMultipleInputFilePath_ReturnsCounts()
        {
            //Arrange
            int expectednoofcomponents = 2;
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string OutFolder = Path.GetDirectoryName(exePath);
            string[] Includes = { "*_Debian.cdx.json" };
            IFolderAction folderAction = new FolderAction();
            IFileOperations fileOperations = new FileOperations();
            CommonAppSettings appSettings = new CommonAppSettings(folderAction, fileOperations)
            {
                ProjectType = "DEBIAN",
                Debian = new Config() { Include = Includes },
                SW360 = new SW360() { IgnoreDevDependency = true },
                Directory = new LCT.Common.Directory(folderAction, fileOperations)
                {
                    InputFolder = Path.GetFullPath(Path.Combine(OutFolder, "PackageIdentifierUTTestFiles"))
                }
            };

            //Act
            Bom listofcomponents = _debianProcessor.ParsePackageFile(appSettings);

            //Assert
            Assert.That(expectednoofcomponents,
                Is.EqualTo(listofcomponents.Components.Count), "Checks for no of components");
        }


        [Test]
        public void ParsePackageConfig_GivenAInputFilePath_ReturnsCounts()
        {
            //Arrange
            int expectednoofcomponents = 2;
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;

            string OutFolder = Path.GetDirectoryName(exePath);
            string[] Includes = { "CycloneDX_Debian.cdx.json" };

            IFolderAction folderAction = new FolderAction();
            IFileOperations fileOperations = new FileOperations();
            CommonAppSettings appSettings = new CommonAppSettings(folderAction, fileOperations)
            {
                ProjectType = "DEBIAN",
                Debian = new Config() { Include = Includes },
                SW360 = new SW360() { IgnoreDevDependency = true },
                Directory = new LCT.Common.Directory(folderAction, fileOperations)
                {
                    InputFolder = Path.GetFullPath(Path.Combine(OutFolder, "PackageIdentifierUTTestFiles"))
                }
            };

            //Act
            Bom listofcomponents = _debianProcessor.ParsePackageFile(appSettings);

            //Assert
            Assert.That(expectednoofcomponents, Is.EqualTo(listofcomponents.Components.Count), "Checks for no of components");
        }

        [Test]
        public void ParsePackageConfig_GivenMultipleInputFiles_ReturnsCountOfDuplicates()
        {
            //Arrange
            int duplicateComponents = 2;
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string OutFolder = Path.GetDirectoryName(exePath);
            string[] Includes = { "*_Debian.cdx.json" };

            IFolderAction folderAction = new FolderAction();
            IFileOperations fileOperations = new FileOperations();
            CommonAppSettings appSettings = new CommonAppSettings(folderAction, fileOperations)
            {
                ProjectType = "DEBIAN",
                Debian = new Config() { Include = Includes },
                SW360 = new SW360() { IgnoreDevDependency = true },
                Directory = new LCT.Common.Directory(folderAction, fileOperations)
                {
                    InputFolder = Path.GetFullPath(Path.Combine(OutFolder, "PackageIdentifierUTTestFiles"))
                }
            };

            //Act
            _debianProcessor.ParsePackageFile(appSettings);

            //Assert
            Assert.That(duplicateComponents, Is.EqualTo(BomCreator.bomKpiData.DuplicateComponents), "Checks for no of duplicate components");
        }

        [Test]
        public void ParsePackageConfig_GivenAInputFilePath_ReturnsSourceDetails()
        {
            //Arrange
            string sourceName = "adduser" + "_" + "3.118";
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string OutFolder = Path.GetDirectoryName(exePath);
            string[] Includes = { "SourceDetails_Cyclonedx.cdx.json" };

            IFolderAction folderAction = new FolderAction();
            IFileOperations fileOperations = new FileOperations();
            CommonAppSettings appSettings = new CommonAppSettings(folderAction, fileOperations)
            {
                ProjectType = "DEBIAN",
                Debian = new Config() { Include = Includes },
                SW360 = new SW360() { IgnoreDevDependency = true },
                Directory = new LCT.Common.Directory(folderAction, fileOperations)
                {
                    InputFolder = Path.GetFullPath(Path.Combine(OutFolder, "PackageIdentifierUTTestFiles"))
                }
            };

            //Act
            Bom listofcomponents = _debianProcessor.ParsePackageFile(appSettings);

            //Assert
            Assert.AreEqual(sourceName, listofcomponents.Components[0].Name + "_" + listofcomponents.Components[0].Version, "Checks component name and version");
        }

        [Test]
        public void ParsePackageConfig_GivenAInputFilePathAlongWithSBOMTemplate_ReturnTotalComponentsList()
        {
            //Arrange
            int expectednoofcomponents = 2;
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string OutFolder = Path.GetDirectoryName(exePath);
            string[] Includes = { "CycloneDX_Debian.cdx.json", "SBOMTemplate_Debian.cdx.json", "SBOM_DebianCATemplate.cdx.json" };
            string packagefilepath = Path.GetFullPath(Path.Combine(OutFolder, "PackageIdentifierUTTestFiles"));

            IFolderAction folderAction = new FolderAction();
            IFileOperations fileOperations = new FileOperations();
            CommonAppSettings appSettings = new CommonAppSettings(folderAction, fileOperations)
            {
                ProjectType = "DEBIAN",
                Debian = new Config() { Include = Includes },
                SW360 = new SW360() { IgnoreDevDependency = true },
                Directory = new LCT.Common.Directory(folderAction, fileOperations)
                {
                    InputFolder = packagefilepath,

                }
            };

            //Act
            Bom listofcomponents = _debianProcessor.ParsePackageFile(appSettings);

            //Assert
            Assert.That(expectednoofcomponents, Is.EqualTo(listofcomponents.Components.Count), "Checks for no of components");
        }

        [Test]
        public void ParsePackageConfig_GivenAInputFilePathAlongWithSBOMTemplate_ReturnUpdatedComponents()
        {
            //Arrange
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string OutFolder = Path.GetDirectoryName(exePath);
            string[] Includes = { "CycloneDX_Debian.cdx.json", "SBOMTemplate_Debian.cdx.json" };
            string packagefilepath = Path.GetFullPath(Path.Combine(OutFolder, "PackageIdentifierUTTestFiles"));

            IFolderAction folderAction = new FolderAction();
            IFileOperations fileOperations = new FileOperations();
            CommonAppSettings appSettings = new CommonAppSettings(folderAction, fileOperations)
            {
                ProjectType = "DEBIAN",
                Debian = new Config() { Include = Includes },
                SW360 = new SW360() { IgnoreDevDependency = true },
                Directory = new LCT.Common.Directory(folderAction, fileOperations)
                {
                    InputFolder = packagefilepath,

                }
            };

            //Act
            Bom listofcomponents = _debianProcessor.ParsePackageFile(appSettings);
            bool isUpdated = listofcomponents.Components.Exists(x => x.Properties != null && x.Properties.Exists(x => x.Name == Dataconstant.Cdx_IdentifierType && x.Value == Dataconstant.Discovered));

            //Assert
            Assert.IsTrue(isUpdated, "Checks For Updated Property In List ");
        }
    }
}
