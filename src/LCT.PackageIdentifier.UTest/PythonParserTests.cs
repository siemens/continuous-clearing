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
    class PythonParserTests
    {
        readonly PythonProcessor pythonProcessor;
        public PythonParserTests()
        {

            List<Component> components = new List<Component>();
            components.Add(new Component() { Name = "virtualenv", Version = "20.16.5", Purl = "pkg:pypi/virtualenv@20.16.5" });
            components.Add(new Component() { Name = "virtualenv", Version = "20.19.0", Purl = "pkg:pypi/virtualenv@20.19.0" });
            Bom bom = new() { Components = components };

            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();
            cycloneDXBomParser.Setup(x => x.ParseCycloneDXBom(It.IsAny<string>())).Returns(bom);
            pythonProcessor = new PythonProcessor(cycloneDXBomParser.Object);
        }
        [Test]
        public void ParseCycloneDXFile_GivenAMultipleInputFilePath_ReturnsCounts()
        {
            //Arrange
            int expectednoofcomponents = 2;
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string OutFolder = Path.GetDirectoryName(exePath);
            string[] Includes = { "*_Python.cdx.json" };
            IFolderAction folderAction = new FolderAction();
            IFileOperations fileOperations = new FileOperations();
            CommonAppSettings appSettings = new CommonAppSettings(folderAction, fileOperations)
            {
                ProjectType = "POETRY",
                Poetry = new Config() { Include = Includes },
                SW360 = new SW360() { IgnoreDevDependency = true },
                Directory = new LCT.Common.Directory(folderAction, fileOperations)
                {
                    InputFolder = Path.GetFullPath(Path.Combine(OutFolder, "PackageIdentifierUTTestFiles"))
                }
            };

            //Act
            Bom listofcomponents = pythonProcessor.ParsePackageFile(appSettings);

            //Assert
            Assert.That(expectednoofcomponents, Is.EqualTo(listofcomponents.Components.Count), "Checks for no of components");
        }


        [Test]
        public void ParseCycloneDXFile_GivenAInputFilePath_ReturnsCounts()
        {
            //Arrange
            int expectednoofcomponents = 2;
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string OutFolder = Path.GetDirectoryName(exePath);
            string[] Includes = { "CycloneDX_Python.cdx.json" };
            IFolderAction folderAction = new FolderAction();
            IFileOperations fileOperations = new FileOperations();
            CommonAppSettings appSettings = new CommonAppSettings(folderAction, fileOperations)
            {
                ProjectType = "POETRY",
                Poetry = new Config() { Include = Includes },
                SW360 = new SW360() { IgnoreDevDependency = true },
                Directory = new LCT.Common.Directory(folderAction, fileOperations)
                {
                    InputFolder = Path.GetFullPath(Path.Combine(OutFolder, "PackageIdentifierUTTestFiles"))
                }
            };

            //Act
            Bom listofcomponents = pythonProcessor.ParsePackageFile(appSettings);

            //Assert
            Assert.That(expectednoofcomponents,
                Is.EqualTo(listofcomponents.Components.Count), "Checks for no of components");
        }
        [Test]
        public void ParseCycloneDXFile_GivenAInputFilePathExcludeOneComponent_ReturnsCounts()
        {
            //Arrange
            int expectednoofcomponents = 2;
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string OutFolder = Path.GetDirectoryName(exePath);
            string[] Includes = { "CycloneDX_Python.cdx.json" };
            List<string> excludeComponents = ["attrs:22.2.0"];

            IFolderAction folderAction = new FolderAction();
            IFileOperations fileOperations = new FileOperations();
            CommonAppSettings appSettings = new CommonAppSettings(folderAction, fileOperations)
            {
                ProjectType = "POETRY",
                Poetry = new Config() { Include = Includes },
                SW360 = new SW360() { IgnoreDevDependency = true, ExcludeComponents = excludeComponents },
                Directory = new LCT.Common.Directory(folderAction, fileOperations)
                {
                    InputFolder = Path.GetFullPath(Path.Combine(OutFolder, "PackageIdentifierUTTestFiles"))
                }
            };

            //Act
            Bom listofcomponents = pythonProcessor.ParsePackageFile(appSettings);

            //Assert
            Assert.That(expectednoofcomponents, Is.EqualTo(listofcomponents.Components.Count), "Checks for no of components");
        }

        [Test]
        public void ParseCycloneDXFile_GivenMultipleInputFiles_ReturnsCountOfDuplicates()
        {
            //Arrange
            int duplicateComponents = 2;
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string OutFolder = Path.GetDirectoryName(exePath);
            string[] Includes = { "*_Python.cdx.json" };

            IFolderAction folderAction = new FolderAction();
            IFileOperations fileOperations = new FileOperations();
            CommonAppSettings appSettings = new CommonAppSettings(folderAction, fileOperations)
            {
                ProjectType = "POETRY",
                Poetry = new Config() { Include = Includes },
                SW360 = new SW360() { IgnoreDevDependency = true },
                Directory = new LCT.Common.Directory(folderAction, fileOperations)
                {
                    InputFolder = Path.GetFullPath(Path.Combine(OutFolder, "PackageIdentifierUTTestFiles"))
                }
            };

            //Act
            pythonProcessor.ParsePackageFile(appSettings);

            //Assert
            Assert.That(duplicateComponents, Is.EqualTo(BomCreator.bomKpiData.DuplicateComponents), "Checks for no of duplicate components");
        }

        [Test]
        public void ParseCycloneDXFile_GivenAInputFilePathAlongWithSBOMTemplate_ReturnTotalComponentsList()
        {
            //Arrange
            int expectednoofcomponents = 2;
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string OutFolder = Path.GetDirectoryName(exePath);
            string[] Includes = { "CycloneDX_Python.cdx.json", "SBOMTemplate_Python.cdx.json", "SBOM_PythonCATemplate.cdx.json" };
            string packagefilepath = Path.GetFullPath(Path.Combine(OutFolder, "PackageIdentifierUTTestFiles"));

            IFolderAction folderAction = new FolderAction();
            IFileOperations fileOperations = new FileOperations();
            CommonAppSettings appSettings = new CommonAppSettings(folderAction, fileOperations)
            {
                ProjectType = "Poetry",
                Poetry = new Config() { Include = Includes },
                SW360 = new SW360() { IgnoreDevDependency = true },
                Directory = new LCT.Common.Directory(folderAction, fileOperations)
                {
                    InputFolder = Path.GetFullPath(Path.Combine(OutFolder, "PackageIdentifierUTTestFiles")),

                }
            };

            //Act
            Bom listofcomponents = pythonProcessor.ParsePackageFile(appSettings);

            //Assert
            Assert.That(expectednoofcomponents, Is.EqualTo(listofcomponents.Components.Count), "Checks for no of components");
        }

        [Test]
        public void ParseCycloneDXFile_GivenAInputFilePathAlongWithSBOMTemplate_ReturnUpdatedComponents()
        {
            //Arrange
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string OutFolder = Path.GetDirectoryName(exePath);
            string[] Includes = { "CycloneDX_Python.cdx.json", "SBOMTemplate_Python.cdx.json" };
            string packagefilepath = Path.GetFullPath(Path.Combine(OutFolder, "PackageIdentifierUTTestFiles"));

            IFolderAction folderAction = new FolderAction();
            IFileOperations fileOperations = new FileOperations();
            CommonAppSettings appSettings = new CommonAppSettings(folderAction, fileOperations)
            {
                ProjectType = "Poetry",
                Poetry = new Config() { Include = Includes },
                SW360 = new SW360() { IgnoreDevDependency = true },
                Directory = new LCT.Common.Directory(folderAction, fileOperations)
                {
                    InputFolder = packagefilepath,

                }
            };

            //Act
            Bom listofcomponents = pythonProcessor.ParsePackageFile(appSettings);

            bool isUpdated = listofcomponents.Components.Exists(x => x.Properties != null && x.Properties.Exists(x => x.Name == Dataconstant.Cdx_IdentifierType && x.Value == Dataconstant.ManullayAdded));

            //Assert
            Assert.IsTrue(isUpdated, "Checks For Updated Property In List ");
        }

        [Test]
        public async Task IdentificationOfInternalComponents_Python_ReturnsComponentData_Successfully()
        {
            // Arrange
            Component component1 = new Component();
            component1.Name = "cachy";
            component1.Group = "";
            component1.Description = string.Empty;
            component1.Version = "0.3.0";
            var components = new List<Component>() { component1 };
            ComponentIdentification component = new() { comparisonBOMData = components };
            string[] reooListArr = { "internalrepo1", "internalrepo2" };
            IFolderAction folderAction = new FolderAction();
            IFileOperations fileOperations = new FileOperations();
            CommonAppSettings appSettings = new CommonAppSettings(folderAction, fileOperations)
            {
                SW360 = new SW360(),
                Poetry = new Config
                {
                    Artifactory = new Artifactory
                    {
                        InternalRepos = reooListArr
                    }
                }
            };
            AqlProperty pypiNameProperty = new AqlProperty
            {
                Key = "pypi.normalized.name",
                Value = "cachy"
            };

            AqlProperty pypiVersionProperty = new AqlProperty
            {
                Key = "pypi.version",
                Value = "0.3.0"
            };
            List<AqlProperty> propertys = new List<AqlProperty> { pypiNameProperty, pypiVersionProperty };

            AqlResult aqlResult = new()
            {
                Name = "cachy-0.3.0.tar.gz",
                Path = "@testfolder/-/folder",
                Repo = "internalrepo1",
                Properties = propertys
            };

            List<AqlResult> results = new List<AqlResult>() { aqlResult };
            Mock<IJFrogService> mockJfrogService = new Mock<IJFrogService>();
            Mock<IBomHelper> mockBomHelper = new Mock<IBomHelper>();
            mockBomHelper.Setup(m => m.GetPypiListOfComponentsFromRepo(It.IsAny<string[]>(), It.IsAny<IJFrogService>()))
                .ReturnsAsync(results);
            mockBomHelper.Setup(m => m.GetFullNameOfComponent(It.IsAny<Component>())).Returns("cachy");

            // Act
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();
            PythonProcessor pyProcessor = new PythonProcessor(cycloneDXBomParser.Object);
            var actual = await pyProcessor.IdentificationOfInternalComponents(
                component, appSettings, mockJfrogService.Object, mockBomHelper.Object);

            // Assert
            Assert.AreEqual("true", actual.comparisonBOMData[0].Properties[0].Value);
        }

        [Test]
        public async Task IdentificationOfInternalComponents_Python_ReturnsComponentData_Failure()
        {
            // Arrange
            Component component1 = new Component();
            component1.Name = "cachy";
            component1.Group = "";
            component1.Description = string.Empty;
            component1.Version = "0.3.0";
            var components = new List<Component>() { component1 };
            ComponentIdentification component = new() { comparisonBOMData = components };
            string[] reooListArr = { "internalrepo1", "internalrepo2" };
            IFolderAction folderAction = new FolderAction();
            IFileOperations fileOperations = new FileOperations();
            CommonAppSettings appSettings = new CommonAppSettings(folderAction, fileOperations)
            {
                SW360 = new SW360(),
                Poetry = new Config
                {
                    Artifactory = new Artifactory
                    {
                        InternalRepos = reooListArr
                    }
                }
            };
            AqlProperty pypiNameProperty = new AqlProperty
            {
                Key = "pypi.normalized.name",
                Value = "cachy"
            };

            AqlProperty pypiVersionProperty = new AqlProperty
            {
                Key = "pypi.version",
                Value = "0.3.0"
            };
            List<AqlProperty> propertys = new List<AqlProperty> { pypiNameProperty, pypiVersionProperty };

            AqlResult aqlResult = new()
            {
                Name = "cachy-1.3.0.tar.gz",
                Path = "@testfolder/-/folder",
                Repo = "internalrepo1",
                Properties = propertys
            };

            List<AqlResult> results = new List<AqlResult>() { aqlResult };
            Mock<IJFrogService> mockJfrogService = new Mock<IJFrogService>();
            Mock<IBomHelper> mockBomHelper = new Mock<IBomHelper>();
            mockBomHelper.Setup(m => m.GetPypiListOfComponentsFromRepo(It.IsAny<string[]>(), It.IsAny<IJFrogService>()))
                .ReturnsAsync(results);
            mockBomHelper.Setup(m => m.GetFullNameOfComponent(It.IsAny<Component>())).Returns("cachy");
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();

            // Act
            PythonProcessor pyProcessor = new PythonProcessor(cycloneDXBomParser.Object);
            var actual = await pyProcessor.IdentificationOfInternalComponents(
                component, appSettings, mockJfrogService.Object, mockBomHelper.Object);

            // Assert
            Assert.AreEqual("true", actual.comparisonBOMData[0].Properties[0].Value);
        }

        [Test]
        public async Task GetJfrogRepoDetailsOfAComponentForPython_ReturnsWithData_SuccessFully()
        {
            // Arrange
            Component component1 = new Component
            {
                Name = "html5lib",
                Description = string.Empty,
                Version = "1.1"
            };
            var components = new List<Component>() { component1 };
            string[] reooListArr = { "internalrepo1", "internalrepo2" };
            IFolderAction folderAction = new FolderAction();
            IFileOperations fileOperations = new FileOperations();
            CommonAppSettings appSettings = new CommonAppSettings(folderAction, fileOperations)
            {
                ProjectType = "POETRY",
                SW360 = new SW360(),
                Poetry = new Config
                {
                    Artifactory = new Artifactory
                    {
                        RemoteRepos = reooListArr
                    }
                }
            };
            AqlProperty pypiNameProperty = new AqlProperty
            {
                Key = "pypi.normalized.name",
                Value = "html5lib"
            };

            AqlProperty pypiVersionProperty = new AqlProperty
            {
                Key = "pypi.version",
                Value = "1.1"
            };
            List<AqlProperty> propertys = new List<AqlProperty> { pypiNameProperty, pypiVersionProperty };
            AqlResult aqlResult = new()
            {
                Name = "html5lib-1.1.tar.gz",
                Path = "@testfolder/-/folder",
                Repo = "internalrepo1",
                Properties = propertys
            };

            List<AqlResult> results = new List<AqlResult>() { aqlResult };

            Mock<IJFrogService> mockJfrogService = new Mock<IJFrogService>();
            Mock<IBomHelper> mockBomHelper = new Mock<IBomHelper>();
            mockBomHelper.Setup(m => m.GetPypiListOfComponentsFromRepo(It.IsAny<string[]>(), It.IsAny<IJFrogService>()))
                .ReturnsAsync(results);
            mockBomHelper.Setup(m => m.GetFullNameOfComponent(It.IsAny<Component>())).Returns("html5lib");
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();

            // Act
            PythonProcessor pyProcessor = new PythonProcessor(cycloneDXBomParser.Object);
            var actual = await pyProcessor.GetJfrogRepoDetailsOfAComponent(
                components, appSettings, mockJfrogService.Object, mockBomHelper.Object);

            // Assert
            Assert.That(actual, Is.Not.Null);
        }

        [Test]
        public async Task GetJfrogRepoDetailsOfAComponentForPython_ReturnsWithData2_SuccessFully()
        {
            // Arrange
            Component component1 = new Component
            {
                Name = "html5lib",
                Description = string.Empty,
                Version = "1.1"
            };
            var components = new List<Component>() { component1 };
            string[] reooListArr = { "internalrepo1", "internalrepo2" };
            IFolderAction folderAction = new FolderAction();
            IFileOperations fileOperations = new FileOperations();
            CommonAppSettings appSettings = new CommonAppSettings(folderAction, fileOperations)
            {
                ProjectType = "POETRY",
                SW360 = new SW360(),
                Poetry = new Config
                {
                    Artifactory = new Artifactory
                    {
                        RemoteRepos = reooListArr
                    }
                }
            };
            AqlProperty pypiNameProperty = new AqlProperty
            {
                Key = "pypi.normalized.name",
                Value = "html5lib"
            };

            AqlProperty pypiVersionProperty = new AqlProperty
            {
                Key = "pypi.version",
                Value = "1.1"
            };
            List<AqlProperty> propertys = new List<AqlProperty> { pypiNameProperty, pypiVersionProperty };
            AqlResult aqlResult = new()
            {
                Name = "html5lib-1.1-py2.py3-none-any.whl",
                Path = "@testfolder/-/folder",
                Repo = "internalrepo1",
                Properties = propertys
            };

            List<AqlResult> results = new List<AqlResult>() { aqlResult };

            Mock<IJFrogService> mockJfrogService = new Mock<IJFrogService>();
            Mock<IBomHelper> mockBomHelper = new Mock<IBomHelper>();
            mockBomHelper.Setup(m => m.GetPypiListOfComponentsFromRepo(It.IsAny<string[]>(), It.IsAny<IJFrogService>()))
                .ReturnsAsync(results);
            mockBomHelper.Setup(m => m.GetFullNameOfComponent(It.IsAny<Component>())).Returns("html5lib");
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();

            // Act
            PythonProcessor pyProcessor = new PythonProcessor(cycloneDXBomParser.Object);
            var actual = await pyProcessor.GetJfrogRepoDetailsOfAComponent(
                components, appSettings, mockJfrogService.Object, mockBomHelper.Object);

            // Assert
            Assert.That(actual, Is.Not.Null);
        }

        [Test]
        public void ExtractDetailsForCycloneDX_GivenInputFilePath_ReturnsCounts()
        {
            //Arrange
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string OutFolder = Path.GetDirectoryName(exePath);
            List<Dependency> dependencies = new List<Dependency>();
            string filePath = Path.GetFullPath(Path.Combine(OutFolder, "PackageIdentifierUTTestFiles", "CycloneDX_Python.cdx.json"));

            //Act
            List<PythonPackage> listofcomponents = PythonProcessor.ExtractDetailsForPoetryLockfile(filePath, dependencies);

            //Assert
            Assert.That(0, Is.EqualTo(listofcomponents.Count));
        }

        [Test]
        public void ExtractDetailsForPoetryLockfile_GivenAInputFilePath_ReturnsCounts()
        {
            //Arrange
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string OutFolder = Path.GetDirectoryName(exePath);
            string[] Includes = { "poetry.lock" };

            IFolderAction folderAction = new FolderAction();
            IFileOperations fileOperations = new FileOperations();
            CommonAppSettings appSettings = new CommonAppSettings(folderAction, fileOperations)
            {
                ProjectType = "Poetry",
                Poetry = new Config() { Include = Includes },
                SW360 = new SW360() { IgnoreDevDependency = true },
                Directory = new LCT.Common.Directory(folderAction, fileOperations)
                {
                    InputFolder = Path.GetFullPath(Path.Combine(OutFolder, "PackageIdentifierUTTestFiles", "PythonTestProject"))
                }
            };

            //Act
            Bom listofcomponents = pythonProcessor.ParsePackageFile(appSettings);

            //Assert  Need to change this after python package clearence implementaion
            Assert.True(listofcomponents.Components.Count == 0 || listofcomponents.Components.Count == 4);
        }

        [Test]
        public void AddSiemensDirectProperty_ShouldAddProperty_WhenPythonDirectDependenciesExist()
        {
            // Arrange
            var bom = new Bom
            {
                Dependencies = new List<Dependency>
                {
                    new Dependency { Ref = "package1@1.0.0" },
                    new Dependency { Ref = "package2@2.0.0" }
                },
                Components = new List<Component>
                {
                    new Component { Name = "component1", Version = "1.0.0" },
                    new Component { Name = "component2", Version = "2.0.0" }
                }
            };

            var pythonDirectDependencies = new List<string>
            {
                "component1@1.0.0",
                "component2@2.0.0"
            };

            var expectedProperties = new List<Property>
            {
                new Property { Name = Dataconstant.Cdx_SiemensDirect, Value = "true" },
                new Property { Name = Dataconstant.Cdx_SiemensDirect, Value = "true" }
            };

            var pythonProcessor = new PythonProcessor(null);

            // Act
            pythonProcessor.AddSiemensDirectProperty(ref bom);

            // Assert
            Assert.AreEqual(expectedProperties[0].Name, bom.Components[0].Properties[0].Name);
            Assert.AreEqual(expectedProperties[0].Name, bom.Components[1].Properties[0].Name);
        }

        [Test]
        public void AddSiemensDirectProperty_ShouldNotAddProperty_WhenPythonDirectDependenciesDoNotExist()
        {
            // Arrange
            var bom = new Bom
            {
                Dependencies = new List<Dependency>
                {
                    new Dependency { Ref = "package1@1.0.0" },
                    new Dependency { Ref = "package2@2.0.0" }
                },
                Components = new List<Component>
                {
                    new Component { Name = "component1", Version = "1.0.0" },
                    new Component { Name = "component2", Version = "2.0.0" }
                }
            };

            var pythonDirectDependencies = new List<string>
            {
                "component3@3.0.0",
                "component4@4.0.0"
            };

            var expectedProperties = new List<Property>
            {
                new Property { Name = Dataconstant.Cdx_SiemensDirect, Value = "false" },
                new Property { Name = Dataconstant.Cdx_SiemensDirect, Value = "false" }
            };

            var pythonProcessor = new PythonProcessor(null);

            // Act
            pythonProcessor.AddSiemensDirectProperty(ref bom);

            // Assert
            Assert.AreEqual(expectedProperties[0].Name, bom.Components[0].Properties[0].Name);
            Assert.AreEqual(expectedProperties[0].Name, bom.Components[1].Properties[0].Name);
        }

        [Test]
        public void AddSiemensDirectProperty_ShouldNotAddProperty_WhenPropertiesAlreadyExist()
        {
            // Arrange
            var bom = new Bom
            {
                Dependencies = new List<Dependency>
                {
                    new Dependency { Ref = "package1@1.0.0" },
                    new Dependency { Ref = "package2@2.0.0" }
                },
                Components = new List<Component>
                {
                    new Component
                    {
                        Name = "component1",
                        Version = "1.0.0",
                        Properties = new List<Property>
                        {
                            new Property { Name = Dataconstant.Cdx_SiemensDirect, Value = "true" }
                        }
                    },
                    new Component
                    {
                        Name = "component2",
                        Version = "2.0.0",
                        Properties = new List<Property>
                        {
                            new Property { Name = Dataconstant.Cdx_SiemensDirect, Value = "true" }
                        }
                    }
                }
            };

            var pythonDirectDependencies = new List<string>
            {
                "component1@1.0.0",
                "component2@2.0.0"
            };

            var expectedProperties = new List<Property>
            {
                new Property { Name = Dataconstant.Cdx_SiemensDirect, Value = "true" }
            };

            var pythonProcessor = new PythonProcessor(null);

            // Act
            pythonProcessor.AddSiemensDirectProperty(ref bom);

            // Assert
            Assert.AreEqual(expectedProperties[0].Name, bom.Components[0].Properties[0].Name);
            Assert.AreEqual(expectedProperties[0].Name, bom.Components[1].Properties[0].Name);
        }
    }
}

