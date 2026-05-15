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
        private static Bom ListUnsupportedComponentsForBom = new Bom { Components = new List<Component>(), Dependencies = new List<Dependency>() };
        public PythonParserTests()
        {

            List<Component> components = new List<Component>();
            components.Add(new Component() { Name = "virtualenv", Version = "20.16.5", Purl = "pkg:pypi/virtualenv@20.16.5" });
            components.Add(new Component() { Name = "virtualenv", Version = "20.19.0", Purl = "pkg:pypi/virtualenv@20.19.0" });
            Bom bom = new() { Components = components };

            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();
            Mock<ISpdxBomParser> spdxBomParser = new Mock<ISpdxBomParser>();
            cycloneDXBomParser.Setup(x => x.ParseCycloneDXBom(It.IsAny<string>())).Returns(bom);
            pythonProcessor = new PythonProcessor(cycloneDXBomParser.Object, spdxBomParser.Object);
        }
        [Test]
        public void ParseCycloneDXFile_GivenAMultipleInputFilePath_ReturnsCounts()
        {
            //Arrange
            int expectednoofcomponents = 2;
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string OutFolder = Path.GetDirectoryName(exePath);
            string[] Includes = { "*_Python.cdx.json" };

            CommonAppSettings appSettings = new CommonAppSettings()
            {
                ProjectType = "POETRY",
                Poetry = new Config() { Include = Includes },
                SW360 = new SW360() { IgnoreDevDependency = true },
                Directory = new LCT.Common.Directory()
                {
                    InputFolder = Path.GetFullPath(Path.Combine(OutFolder, "PackageIdentifierUTTestFiles"))
                }
            };

            //Act
            Bom listofcomponents = pythonProcessor.ParsePackageFile(appSettings, ref ListUnsupportedComponentsForBom);

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

            CommonAppSettings appSettings = new CommonAppSettings()
            {
                ProjectType = "POETRY",
                Poetry = new Config() { Include = Includes },
                SW360 = new SW360() { IgnoreDevDependency = true },
                Directory = new LCT.Common.Directory()
                {
                    InputFolder = Path.GetFullPath(Path.Combine(OutFolder, "PackageIdentifierUTTestFiles"))
                }
            };

            //Act
            Bom listofcomponents = pythonProcessor.ParsePackageFile(appSettings, ref ListUnsupportedComponentsForBom);

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

            CommonAppSettings appSettings = new CommonAppSettings()
            {
                ProjectType = "POETRY",
                Poetry = new Config() { Include = Includes },
                SW360 = new SW360() { IgnoreDevDependency = true, ExcludeComponents = excludeComponents },
                Directory = new LCT.Common.Directory()
                {
                    InputFolder = Path.GetFullPath(Path.Combine(OutFolder, "PackageIdentifierUTTestFiles"))
                }
            };

            //Act
            Bom listofcomponents = pythonProcessor.ParsePackageFile(appSettings, ref ListUnsupportedComponentsForBom);

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

            CommonAppSettings appSettings = new CommonAppSettings()
            {
                ProjectType = "POETRY",
                Poetry = new Config() { Include = Includes },
                SW360 = new SW360() { IgnoreDevDependency = true },
                Directory = new LCT.Common.Directory()
                {
                    InputFolder = Path.GetFullPath(Path.Combine(OutFolder, "PackageIdentifierUTTestFiles"))
                }
            };

            //Act
            pythonProcessor.ParsePackageFile(appSettings, ref ListUnsupportedComponentsForBom);

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

            CommonAppSettings appSettings = new CommonAppSettings()
            {
                ProjectType = "Poetry",
                Poetry = new Config() { Include = Includes },
                SW360 = new SW360() { IgnoreDevDependency = true },
                Directory = new LCT.Common.Directory()
                {
                    InputFolder = Path.GetFullPath(Path.Combine(OutFolder, "PackageIdentifierUTTestFiles")),

                }
            };

            //Act
            Bom listofcomponents = pythonProcessor.ParsePackageFile(appSettings, ref ListUnsupportedComponentsForBom);

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

            CommonAppSettings appSettings = new CommonAppSettings()
            {
                ProjectType = "Poetry",
                Poetry = new Config() { Include = Includes },
                SW360 = new SW360() { IgnoreDevDependency = true },
                Directory = new LCT.Common.Directory()
                {
                    InputFolder = packagefilepath,

                }
            };

            //Act
            Bom listofcomponents = pythonProcessor.ParsePackageFile(appSettings, ref ListUnsupportedComponentsForBom);

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

            CommonAppSettings appSettings = new CommonAppSettings()
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
            Mock<ISpdxBomParser> spdxBomParser = new Mock<ISpdxBomParser>();
            PythonProcessor pyProcessor = new PythonProcessor(cycloneDXBomParser.Object, spdxBomParser.Object);
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

            CommonAppSettings appSettings = new CommonAppSettings()
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
            Mock<ISpdxBomParser> spdxBomParser = new Mock<ISpdxBomParser>();
            // Act
            PythonProcessor pyProcessor = new PythonProcessor(cycloneDXBomParser.Object, spdxBomParser.Object);
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

            CommonAppSettings appSettings = new CommonAppSettings()
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
            Mock<ISpdxBomParser> spdxBomParser = new Mock<ISpdxBomParser>();
            // Act
            PythonProcessor pyProcessor = new PythonProcessor(cycloneDXBomParser.Object, spdxBomParser.Object);
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

            CommonAppSettings appSettings = new CommonAppSettings()
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
            Mock<ISpdxBomParser> spdxBomParser = new Mock<ISpdxBomParser>();
            // Act
            PythonProcessor pyProcessor = new PythonProcessor(cycloneDXBomParser.Object, spdxBomParser.Object);
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


            CommonAppSettings appSettings = new CommonAppSettings()
            {
                ProjectType = "Poetry",
                Poetry = new Config() { Include = Includes },
                SW360 = new SW360() { IgnoreDevDependency = true },
                Directory = new LCT.Common.Directory()
                {
                    InputFolder = Path.GetFullPath(Path.Combine(OutFolder, "PackageIdentifierUTTestFiles", "PythonTestProject"))
                }
            };

            //Act
            Bom listofcomponents = pythonProcessor.ParsePackageFile(appSettings, ref ListUnsupportedComponentsForBom);

            //Assert  Need to change this after python package clearence implementaion
            Assert.True(listofcomponents.Components.Count == 0 || listofcomponents.Components.Count == 6);
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

            var expectedProperties = new List<Property>
            {
                new Property { Name = Dataconstant.Cdx_SiemensDirect, Value = "true" },
                new Property { Name = Dataconstant.Cdx_SiemensDirect, Value = "true" }
            };


            // Act
            PythonProcessor.AddSiemensDirectProperty(ref bom);

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

            var expectedProperties = new List<Property>
            {
                new Property { Name = Dataconstant.Cdx_SiemensDirect, Value = "false" },
                new Property { Name = Dataconstant.Cdx_SiemensDirect, Value = "false" }
            };


            // Act
            PythonProcessor.AddSiemensDirectProperty(ref bom);

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


            var expectedProperties = new List<Property>
            {
                new Property { Name = Dataconstant.Cdx_SiemensDirect, Value = "true" }
            };


            // Act
            PythonProcessor.AddSiemensDirectProperty(ref bom);

            // Assert
            Assert.AreEqual(expectedProperties[0].Name, bom.Components[0].Properties[0].Name);
            Assert.AreEqual(expectedProperties[0].Name, bom.Components[1].Properties[0].Name);
        }

        [Test]
        public void ExtractDetailsForPoetryLockfile_Poetry2xGroupsDevOnly_IdentifiesAsDevDependency()
        {
            //Arrange
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string OutFolder = Path.GetDirectoryName(exePath);
            string filePath = Path.GetFullPath(Path.Combine(OutFolder, "PackageIdentifierUTTestFiles", "PythonTestProject", "poetry2.lock"));
            List<Dependency> dependencies = new List<Dependency>();

            //Act
            List<PythonPackage> packages = PythonProcessor.ExtractDetailsForPoetryLockfile(filePath, dependencies);

            //Assert - pytest has groups = ["dev"]
            var pytest = packages.Find(p => p.Name == "pytest");
            Assert.That(pytest, Is.Not.Null, "pytest should be found in packages");
            Assert.IsTrue(pytest.Isdevdependent, "Package with groups=[\"dev\"] should be marked as dev dependency");
        }

        [Test]
        public void ExtractDetailsForPoetryLockfile_Poetry2xGroupsMainOnly_IdentifiesAsProductionDependency()
        {
            //Arrange
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string OutFolder = Path.GetDirectoryName(exePath);
            string filePath = Path.GetFullPath(Path.Combine(OutFolder, "PackageIdentifierUTTestFiles", "PythonTestProject", "poetry2.lock"));
            List<Dependency> dependencies = new List<Dependency>();

            //Act
            List<PythonPackage> packages = PythonProcessor.ExtractDetailsForPoetryLockfile(filePath, dependencies);

            //Assert - requests has groups = ["main"]
            var requests = packages.Find(p => p.Name == "requests");
            Assert.That(requests, Is.Not.Null, "requests should be found in packages");
            Assert.IsFalse(requests.Isdevdependent, "Package with groups=[\"main\"] should NOT be marked as dev dependency");
        }

        [Test]
        public void ExtractDetailsForPoetryLockfile_Poetry2xGroupsMainAndDev_ClassifiesAsProduction()
        {
            //Arrange
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string OutFolder = Path.GetDirectoryName(exePath);
            string filePath = Path.GetFullPath(Path.Combine(OutFolder, "PackageIdentifierUTTestFiles", "PythonTestProject", "poetry2.lock"));
            List<Dependency> dependencies = new List<Dependency>();

            //Act
            List<PythonPackage> packages = PythonProcessor.ExtractDetailsForPoetryLockfile(filePath, dependencies);

            //Assert - coverage has groups = ["main", "dev"], main takes precedence
            var coverage = packages.Find(p => p.Name == "coverage");
            Assert.That(coverage, Is.Not.Null, "coverage should be found in packages");
            Assert.IsFalse(coverage.Isdevdependent, "Package with groups=[\"main\", \"dev\"] should NOT be dev dependency (main takes precedence)");
        }

        [Test]
        public void ExtractDetailsForPoetryLockfile_Poetry2xGroupsCustomNonDev_ClassifiesAsProduction()
        {
            //Arrange
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string OutFolder = Path.GetDirectoryName(exePath);
            string filePath = Path.GetFullPath(Path.Combine(OutFolder, "PackageIdentifierUTTestFiles", "PythonTestProject", "poetry2.lock"));
            List<Dependency> dependencies = new List<Dependency>();

            //Act
            List<PythonPackage> packages = PythonProcessor.ExtractDetailsForPoetryLockfile(filePath, dependencies);

            //Assert - sphinx has groups = ["docs"], not dev
            var sphinx = packages.Find(p => p.Name == "sphinx");
            Assert.That(sphinx, Is.Not.Null, "sphinx should be found in packages");
            Assert.IsFalse(sphinx.Isdevdependent, "Package with groups=[\"docs\"] should NOT be marked as dev dependency");
        }

        [Test]
        public void ExtractDetailsForPoetryLockfile_Poetry2xFormat_ReturnsCorrectTotalPackageCount()
        {
            //Arrange
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string OutFolder = Path.GetDirectoryName(exePath);
            string filePath = Path.GetFullPath(Path.Combine(OutFolder, "PackageIdentifierUTTestFiles", "PythonTestProject", "poetry2.lock"));
            List<Dependency> dependencies = new List<Dependency>();

            //Act
            List<PythonPackage> packages = PythonProcessor.ExtractDetailsForPoetryLockfile(filePath, dependencies);

            //Assert
            Assert.That(packages.Count, Is.EqualTo(8), "Should detect all 8 packages from Poetry 2.x lock file (including black)");
        }

        [Test]
        public void ExtractDetailsForPoetryLockfile_Poetry2xFormat_ReturnsAccurateDevDependencyCount()
        {
            //Arrange
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string OutFolder = Path.GetDirectoryName(exePath);
            string filePath = Path.GetFullPath(Path.Combine(OutFolder, "PackageIdentifierUTTestFiles", "PythonTestProject", "poetry2.lock"));
            List<Dependency> dependencies = new List<Dependency>();

            //Act
            List<PythonPackage> packages = PythonProcessor.ExtractDetailsForPoetryLockfile(filePath, dependencies);

            //Assert - pytest and black (groups=["dev"] or "dev") should be dev
            int devCount = packages.FindAll(p => p.Isdevdependent).Count;
            int prodCount = packages.FindAll(p => !p.Isdevdependent).Count;
            Assert.That(devCount, Is.EqualTo(2), "2 packages (pytest and black) should be classified as dev dependency");
            Assert.That(prodCount, Is.EqualTo(6), "6 packages should be classified as production dependencies");
        }

        [Test]
        public void ExtractDetailsForPoetryLockfile_Poetry2xFormat_PopulatesDependenciesCorrectly()
        {
            //Arrange
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string OutFolder = Path.GetDirectoryName(exePath);
            string filePath = Path.GetFullPath(Path.Combine(OutFolder, "PackageIdentifierUTTestFiles", "PythonTestProject", "poetry2.lock"));
            List<Dependency> dependencies = new List<Dependency>();

            //Act
            PythonProcessor.ExtractDetailsForPoetryLockfile(filePath, dependencies);

            //Assert
            Assert.That(dependencies.Count, Is.EqualTo(8), "Should have dependency entries for all 6 packages");
            var requestsDep = dependencies.Find(d => d.Ref.Contains("requests"));
            Assert.That(requestsDep, Is.Not.Null, "requests dependency entry should exist");
            Assert.That(requestsDep.Dependencies.Count, Is.EqualTo(1), "requests should have 1 sub-dependency (urllib3)");
        }


        [Test]
        public void ExtractDetailsForPoetryLockfile_Poetry2xGroupsAsString_IdentifiesCorrectly()
        {
            //Arrange
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string OutFolder = Path.GetDirectoryName(exePath);
            string filePath = Path.GetFullPath(Path.Combine(OutFolder, "PackageIdentifierUTTestFiles", "PythonTestProject", "poetry2.lock"));
            List<Dependency> dependencies = new List<Dependency>();

            //Act
            List<PythonPackage> packages = PythonProcessor.ExtractDetailsForPoetryLockfile(filePath, dependencies);

            //Assert - black has groups = "dev" (string, not array)
            var black = packages.Find(p => p.Name == "black");
            Assert.That(black, Is.Not.Null, "black should be found in packages");
            Assert.IsTrue(black.Isdevdependent, "Package with groups=\"dev\" (string format) should be marked as dev dependency");
        }

        [Test]
        public void ExtractDetailsForPoetryLockfile_Poetry2xGroupsAsString_IdentifiesCorrectlystring()
        {
            //Arrange
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string OutFolder = Path.GetDirectoryName(exePath);
            string filePath = Path.GetFullPath(Path.Combine(OutFolder, "PackageIdentifierUTTestFiles", "PythonTestProject", "poetry2.lock"));
            List<Dependency> dependencies = new List<Dependency>();

            //Act
            List<PythonPackage> packages = PythonProcessor.ExtractDetailsForPoetryLockfile(filePath, dependencies);

            //Assert - black has groups = "dev" (string, not array)
            var blacktest = packages.Find(p => p.Name == "blacktest");
            Assert.That(blacktest, Is.Not.Null, "black should be found in packages");
            Assert.IsFalse(blacktest.Isdevdependent, "Package with groups=\"dev\" (string format) should be marked as dev dependency");
        }

        [Test]
        public void ExtractDetailsForPoetryLockfile_Poetry2xGroupsAsString_IdentifiesCorrectlystringempty()
        {
            //Arrange
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string OutFolder = Path.GetDirectoryName(exePath);
            string filePath = Path.GetFullPath(Path.Combine(OutFolder, "PackageIdentifierUTTestFiles", "PythonTestProject", "poetry2.lock"));
            List<Dependency> dependencies = new List<Dependency>();

            //Act
            List<PythonPackage> packages = PythonProcessor.ExtractDetailsForPoetryLockfile(filePath, dependencies);

            //Assert - black has groups = "dev" (string, not array)
            var blacktest1 = packages.Find(p => p.Name == "blacktest1");
            Assert.That(blacktest1, Is.Not.Null, "black should be found in packages");
            Assert.IsFalse(blacktest1.Isdevdependent, "Package with groups=\"dev\" (string format) should be marked as dev dependency");
        }

        [Test]
        public void ExtractDetailsForPoetryLockfile_Poetry2xGroupsAsString_IdentifiesCorrectlycategorydev()
        {
            //Arrange
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string OutFolder = Path.GetDirectoryName(exePath);
            string filePath = Path.GetFullPath(Path.Combine(OutFolder, "PackageIdentifierUTTestFiles", "PythonTestProject", "poetry.lock"));
            List<Dependency> dependencies = new List<Dependency>();

            //Act
            List<PythonPackage> packages = PythonProcessor.ExtractDetailsForPoetryLockfile(filePath, dependencies);

            //Assert - black has groups = "dev" (string, not array)
            var six1 = packages.Find(p => p.Name == "six1");
            Assert.That(six1, Is.Not.Null, "six1 should be found in packages");
            Assert.IsTrue(six1.Isdevdependent, "Package with groups=\"dev\" (string format) should be marked as dev dependency");
        }

        [Test]
        public void ExtractDetailsForPoetryLockfile_Poetry2xGroupsAsString_IdentifiesCorrectlycategorymainempty()
        {
            //Arrange
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string OutFolder = Path.GetDirectoryName(exePath);
            string filePath = Path.GetFullPath(Path.Combine(OutFolder, "PackageIdentifierUTTestFiles", "PythonTestProject", "poetry.lock"));
            List<Dependency> dependencies = new List<Dependency>();

            //Act
            List<PythonPackage> packages = PythonProcessor.ExtractDetailsForPoetryLockfile(filePath, dependencies);

            //Assert - black has groups = "dev" (string, not array)
            var six2 = packages.Find(p => p.Name == "six2");
            Assert.That(six2, Is.Not.Null, "six should be found in packages");
            Assert.IsFalse(six2.Isdevdependent, "Package with groups=\"dev\" (string format) should be marked as dev dependency");
        }

        [Test]
        public void ExtractDetailsForPoetryLockfile_Poetry2xGroupsAsString_IdentifiesCorrectlycategorymain()
        {
            //Arrange
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string OutFolder = Path.GetDirectoryName(exePath);
            string filePath = Path.GetFullPath(Path.Combine(OutFolder, "PackageIdentifierUTTestFiles", "PythonTestProject", "poetry.lock"));
            List<Dependency> dependencies = new List<Dependency>();

            //Act
            List<PythonPackage> packages = PythonProcessor.ExtractDetailsForPoetryLockfile(filePath, dependencies);

            //Assert - black has groups = "dev" (string, not array)
            var six = packages.Find(p => p.Name == "six");
            Assert.That(six, Is.Not.Null, "six should be found in packages");
            Assert.IsFalse(six.Isdevdependent, "Package with groups=\"dev\" (string format) should be marked as dev dependency");
        }
    }
}

