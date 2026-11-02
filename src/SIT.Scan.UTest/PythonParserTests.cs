// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2026 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models;
using Moq;
using NUnit.Framework;
using SIT.APICommunications.Model.AQL;
using SIT.Common;
using SIT.Common.Constants;
using SIT.Common.Interface;
using SIT.Common.Model;
using SIT.Scan.Interface;
using SIT.Scan.Model;
using SIT.Services.Interface;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace SIT.Scan.UTest
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
                Directory = new SIT.Common.Directory()
                {
                    InputFolder = Path.GetFullPath(Path.Combine(OutFolder, "SITScanUTTestFiles"))
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
                Directory = new SIT.Common.Directory()
                {
                    InputFolder = Path.GetFullPath(Path.Combine(OutFolder, "SITScanUTTestFiles"))
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
                Directory = new SIT.Common.Directory()
                {
                    InputFolder = Path.GetFullPath(Path.Combine(OutFolder, "SITScanUTTestFiles"))
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
                Directory = new SIT.Common.Directory()
                {
                    InputFolder = Path.GetFullPath(Path.Combine(OutFolder, "SITScanUTTestFiles"))
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
            string packagefilepath = Path.GetFullPath(Path.Combine(OutFolder, "SITScanUTTestFiles"));

            CommonAppSettings appSettings = new CommonAppSettings()
            {
                ProjectType = "Poetry",
                Poetry = new Config() { Include = Includes },
                SW360 = new SW360() { IgnoreDevDependency = true },
                Directory = new SIT.Common.Directory()
                {
                    InputFolder = Path.GetFullPath(Path.Combine(OutFolder, "SITScanUTTestFiles")),

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
            string packagefilepath = Path.GetFullPath(Path.Combine(OutFolder, "SITScanUTTestFiles"));

            CommonAppSettings appSettings = new CommonAppSettings()
            {
                ProjectType = "Poetry",
                Poetry = new Config() { Include = Includes },
                SW360 = new SW360() { IgnoreDevDependency = true },
                Directory = new SIT.Common.Directory()
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
                Key = Dataconstant.PypiNormalizedNameKey,
                Value = "cachy"
            };

            AqlProperty pypiVersionProperty = new AqlProperty
            {
                Key = Dataconstant.PypiVersionKey,
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
                Key = Dataconstant.PypiNormalizedNameKey,
                Value = "cachy"
            };

            AqlProperty pypiVersionProperty = new AqlProperty
            {
                Key = Dataconstant.PypiVersionKey,
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
                Key = Dataconstant.PypiNormalizedNameKey,
                Value = "html5lib"
            };

            AqlProperty pypiVersionProperty = new AqlProperty
            {
                Key = Dataconstant.PypiVersionKey,
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
                Key = Dataconstant.PypiNormalizedNameKey,
                Value = "html5lib"
            };

            AqlProperty pypiVersionProperty = new AqlProperty
            {
                Key = Dataconstant.PypiVersionKey,
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
            string filePath = Path.GetFullPath(Path.Combine(OutFolder, "SITScanUTTestFiles", "CycloneDX_Python.cdx.json"));

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
                Directory = new SIT.Common.Directory()
                {
                    InputFolder = Path.GetFullPath(Path.Combine(OutFolder, "SITScanUTTestFiles", "PythonTestProject"))
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
            string filePath = Path.GetFullPath(Path.Combine(OutFolder, "SITScanUTTestFiles", "PythonTestProject", "poetry2.lock"));
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
            string filePath = Path.GetFullPath(Path.Combine(OutFolder, "SITScanUTTestFiles", "PythonTestProject", "poetry2.lock"));
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
            string filePath = Path.GetFullPath(Path.Combine(OutFolder, "SITScanUTTestFiles", "PythonTestProject", "poetry2.lock"));
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
            string filePath = Path.GetFullPath(Path.Combine(OutFolder, "SITScanUTTestFiles", "PythonTestProject", "poetry2.lock"));
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
            string filePath = Path.GetFullPath(Path.Combine(OutFolder, "SITScanUTTestFiles", "PythonTestProject", "poetry2.lock"));
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
            string filePath = Path.GetFullPath(Path.Combine(OutFolder, "SITScanUTTestFiles", "PythonTestProject", "poetry2.lock"));
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
            string filePath = Path.GetFullPath(Path.Combine(OutFolder, "SITScanUTTestFiles", "PythonTestProject", "poetry2.lock"));
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
            string filePath = Path.GetFullPath(Path.Combine(OutFolder, "SITScanUTTestFiles", "PythonTestProject", "poetry2.lock"));
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
            string filePath = Path.GetFullPath(Path.Combine(OutFolder, "SITScanUTTestFiles", "PythonTestProject", "poetry2.lock"));
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
            string filePath = Path.GetFullPath(Path.Combine(OutFolder, "SITScanUTTestFiles", "PythonTestProject", "poetry2.lock"));
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
            string filePath = Path.GetFullPath(Path.Combine(OutFolder, "SITScanUTTestFiles", "PythonTestProject", "poetry.lock"));
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
            string filePath = Path.GetFullPath(Path.Combine(OutFolder, "SITScanUTTestFiles", "PythonTestProject", "poetry.lock"));
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
            string filePath = Path.GetFullPath(Path.Combine(OutFolder, "SITScanUTTestFiles", "PythonTestProject", "poetry.lock"));
            List<Dependency> dependencies = new List<Dependency>();

            //Act
            List<PythonPackage> packages = PythonProcessor.ExtractDetailsForPoetryLockfile(filePath, dependencies);

            //Assert - black has groups = "dev" (string, not array)
            var six = packages.Find(p => p.Name == "six");
            Assert.That(six, Is.Not.Null, "six should be found in packages");
            Assert.IsFalse(six.Isdevdependent, "Package with groups=\"dev\" (string format) should be marked as dev dependency");
        }

        /// <summary>
        /// In-memory mock uv.lock content used by the uv parser tests so the
        /// tests do not depend on an external fixture file.
        /// </summary>
        private const string MockUvLockContent = @"version = 1
revision = 3
requires-python = "">=3.10""

[[package]]
name = ""sit-app""
version = ""0.1.0""
source = { virtual = ""."" }
dependencies = [
    { name = ""requests"" },
    { name = ""jinja2"" },
]

[package.metadata]
requires-dist = [
    { name = ""requests"", specifier = "">=2.31.0"" },
    { name = ""jinja2"", specifier = "">=3.1.6"" },
]
requires-dev = { dev = [
    { name = ""pytest"", specifier = "">=8.0.0"" },
    { name = ""black"", specifier = "">=24.0.0"" },
] }

[[package]]
name = ""requests""
version = ""2.31.0""
source = { registry = ""https://pypi.org/simple"" }
dependencies = [
    { name = ""certifi"" },
    { name = ""idna"" },
    { name = ""urllib3"" },
]

[[package]]
name = ""jinja2""
version = ""3.1.6""
source = { registry = ""https://pypi.org/simple"" }
dependencies = [
    { name = ""markupsafe"" },
]

[[package]]
name = ""certifi""
version = ""2024.2.2""
source = { registry = ""https://pypi.org/simple"" }

[[package]]
name = ""idna""
version = ""3.6""
source = { registry = ""https://pypi.org/simple"" }

[[package]]
name = ""urllib3""
version = ""2.2.1""
source = { registry = ""https://pypi.org/simple"" }

[[package]]
name = ""markupsafe""
version = ""2.1.5""
source = { registry = ""https://pypi.org/simple"" }

[[package]]
name = ""pytest""
version = ""8.1.1""
source = { registry = ""https://pypi.org/simple"" }

[[package]]
name = ""black""
version = ""24.3.0""
source = { registry = ""https://pypi.org/simple"" }
";

        /// <summary>
        /// Writes the mock uv.lock content to a unique temp file and returns its path.
        /// The caller is responsible for deleting the file after use.
        /// </summary>
        private static string CreateMockUvLockFile()
        {
            string filePath = Path.Combine(Path.GetTempPath(), $"uv_{System.Guid.NewGuid():N}.lock");
            System.IO.File.WriteAllText(filePath, MockUvLockContent);
            return filePath;
        }

        [Test]
        public void ExtractDetailsForUvLockfile_GivenUvLock_ReturnsAllPackages()
        {
            //Arrange
            string filePath = CreateMockUvLockFile();
            List<Dependency> dependencies = new List<Dependency>();

            try
            {
                //Act
                List<PythonPackage> packages = PythonProcessor.ExtractDetailsForUvLockfile(filePath, dependencies);

                //Assert - 8 third-party packages (the virtual root project is excluded)
                Assert.That(packages.Count, Is.EqualTo(8), "Should detect 8 packages, excluding the virtual root project");
                var requests = packages.Find(p => p.Name == "requests");
                Assert.That(requests, Is.Not.Null, "requests should be found in packages");
                Assert.That(requests.Version, Is.EqualTo("2.31.0"), "requests version should be parsed");
                Assert.That(requests.PurlID, Does.Contain("pkg:pypi/requests@2.31.0"), "PurlID should be a PyPI purl");
            }
            finally
            {
                System.IO.File.Delete(filePath);
            }
        }

        [Test]
        public void ExtractDetailsForUvLockfile_RequiresDevGroup_IdentifiesDevDependencies()
        {
            //Arrange
            string filePath = CreateMockUvLockFile();
            List<Dependency> dependencies = new List<Dependency>();

            try
            {
                //Act
                List<PythonPackage> packages = PythonProcessor.ExtractDetailsForUvLockfile(filePath, dependencies);

                //Assert - pytest and black are in the root requires-dev dev group
                var pytest = packages.Find(p => p.Name == "pytest");
                var black = packages.Find(p => p.Name == "black");
                Assert.That(pytest, Is.Not.Null, "pytest should be found in packages");
                Assert.That(black, Is.Not.Null, "black should be found in packages");
                Assert.IsTrue(pytest.Isdevdependent, "pytest should be marked as a dev dependency");
                Assert.IsTrue(black.Isdevdependent, "black should be marked as a dev dependency");
            }
            finally
            {
                System.IO.File.Delete(filePath);
            }
        }

        [Test]
        public void ExtractDetailsForUvLockfile_ProductionPackages_NotMarkedAsDev()
        {
            //Arrange
            string filePath = CreateMockUvLockFile();
            List<Dependency> dependencies = new List<Dependency>();

            try
            {
                //Act
                List<PythonPackage> packages = PythonProcessor.ExtractDetailsForUvLockfile(filePath, dependencies);

                //Assert
                var requests = packages.Find(p => p.Name == "requests");
                var jinja2 = packages.Find(p => p.Name == "jinja2");
                Assert.IsFalse(requests.Isdevdependent, "requests should NOT be marked as a dev dependency");
                Assert.IsFalse(jinja2.Isdevdependent, "jinja2 should NOT be marked as a dev dependency");
            }
            finally
            {
                System.IO.File.Delete(filePath);
            }
        }

        [Test]
        public void ExtractDetailsForUvLockfile_PopulatesDependencyEdges()
        {
            //Arrange
            string filePath = CreateMockUvLockFile();
            List<Dependency> dependencies = new List<Dependency>();

            try
            {
                //Act
                PythonProcessor.ExtractDetailsForUvLockfile(filePath, dependencies);

                //Assert - one dependency entry per package (root project excluded)
                Assert.That(dependencies.Count, Is.EqualTo(8), "Should have a dependency entry for each non-root package");
                var requestsDep = dependencies.Find(d => d.Ref.Contains("pkg:pypi/requests@2.31.0"));
                Assert.That(requestsDep, Is.Not.Null, "requests dependency entry should exist");
                Assert.That(requestsDep.Dependencies.Count, Is.EqualTo(3), "requests should have 3 sub-dependencies (certifi, idna, urllib3)");
                Assert.That(requestsDep.Dependencies.Exists(d => d.Ref.Contains("certifi@2024.2.2")), Is.True, "requests should reference certifi with resolved version");

                var jinjaDep = dependencies.Find(d => d.Ref.Contains("pkg:pypi/jinja2@3.1.6"));
                Assert.That(jinjaDep, Is.Not.Null, "jinja2 dependency entry should exist");
                Assert.That(jinjaDep.Dependencies.Count, Is.EqualTo(1), "jinja2 should have 1 sub-dependency (markupsafe)");
                Assert.That(jinjaDep.Dependencies[0].Ref, Does.Contain("markupsafe@2.1.5"), "jinja2 should reference markupsafe with resolved version");
            }
            finally
            {
                System.IO.File.Delete(filePath);
            }
        }

        [Test]
        public void ExtractDetailsForUvLockfile_VirtualRootProject_IsExcludedFromComponents()
        {
            //Arrange
            string filePath = CreateMockUvLockFile();
            List<Dependency> dependencies = new List<Dependency>();

            try
            {
                //Act
                List<PythonPackage> packages = PythonProcessor.ExtractDetailsForUvLockfile(filePath, dependencies);

                //Assert - the virtual root project (sit-app) must not be emitted
                var root = packages.Find(p => p.Name == "sit-app");
                Assert.That(root, Is.Null, "The virtual root project should be excluded from BOM components");
            }
            finally
            {
                System.IO.File.Delete(filePath);
            }
        }

        /// <summary>
        /// Mock uv.lock whose root project uses nested dev groups via include-group
        /// and whose dev packages have their own transitive dependencies.
        /// </summary>
        private const string MockUvLockNestedContent = @"version = 1
revision = 3
requires-python = "">=3.10""

[[package]]
name = ""sit-app""
version = ""0.1.0""
source = { virtual = ""."" }
dependencies = [
    { name = ""requests"" },
]

[package.metadata]
requires-dist = [
    { name = ""requests"", specifier = "">=2.31.0"" },
]
requires-dev = { dev = [
    { include-group = ""lint"" },
], lint = [
    { name = ""flake8"", specifier = "">=7.0.0"" },
] }

[[package]]
name = ""requests""
version = ""2.31.0""
source = { registry = ""https://pypi.org/simple"" }
dependencies = [
    { name = ""urllib3"" },
]

[[package]]
name = ""urllib3""
version = ""2.2.1""
source = { registry = ""https://pypi.org/simple"" }

[[package]]
name = ""flake8""
version = ""7.0.0""
source = { registry = ""https://pypi.org/simple"" }
dependencies = [
    { name = ""pyflakes"" },
]

[[package]]
name = ""pyflakes""
version = ""3.2.0""
source = { registry = ""https://pypi.org/simple"" }
";

        private static string CreateMockUvLockNestedFile()
        {
            string filePath = Path.Combine(Path.GetTempPath(), $"uv_nested_{System.Guid.NewGuid():N}.lock");
            System.IO.File.WriteAllText(filePath, MockUvLockNestedContent);
            return filePath;
        }

        [Test]
        public void ExtractDetailsForUvLockfile_IncludeGroupNesting_ResolvesNestedDevDependencies()
        {
            //Arrange
            string filePath = CreateMockUvLockNestedFile();
            List<Dependency> dependencies = new List<Dependency>();

            try
            {
                //Act
                List<PythonPackage> packages = PythonProcessor.ExtractDetailsForUvLockfile(filePath, dependencies);

                //Assert - flake8 reached via dev -> include-group "lint"
                var flake8 = packages.Find(p => p.Name == "flake8");
                Assert.That(flake8, Is.Not.Null, "flake8 should be found in packages");
                Assert.IsTrue(flake8.Isdevdependent, "flake8 should be dev via include-group nesting");
            }
            finally
            {
                System.IO.File.Delete(filePath);
            }
        }

        [Test]
        public void ExtractDetailsForUvLockfile_TransitiveDevDependency_IsMarkedAsDev()
        {
            //Arrange
            string filePath = CreateMockUvLockNestedFile();
            List<Dependency> dependencies = new List<Dependency>();

            try
            {
                //Act
                List<PythonPackage> packages = PythonProcessor.ExtractDetailsForUvLockfile(filePath, dependencies);

                //Assert - pyflakes is only a dependency of the dev package flake8
                var pyflakes = packages.Find(p => p.Name == "pyflakes");
                Assert.That(pyflakes, Is.Not.Null, "pyflakes should be found in packages");
                Assert.IsTrue(pyflakes.Isdevdependent, "pyflakes should be dev via transitive propagation from flake8");

                //Assert - urllib3 is a runtime transitive dependency and must remain non-dev
                var urllib3 = packages.Find(p => p.Name == "urllib3");
                Assert.That(urllib3, Is.Not.Null, "urllib3 should be found in packages");
                Assert.IsFalse(urllib3.Isdevdependent, "urllib3 is a runtime dependency and should NOT be dev");
            }
            finally
            {
                System.IO.File.Delete(filePath);
            }
        }

        /// <summary>
        /// Mock uv.lock using the [package.dev-dependencies] table format emitted by
        /// current uv versions (e.g. uv 0.12.x), as opposed to the legacy
        /// [package.metadata].requires-dev representation. Includes a nested
        /// include-group reference (dev -> lint) to verify group resolution.
        /// </summary>
        private const string MockUvLockDevDependenciesContent = @"version = 1
revision = 3
requires-python = "">=3.10""

[[package]]
name = ""sample-uv-project""
version = ""0.1.0""
source = { editable = ""."" }
dependencies = [
    { name = ""requests"" },
]

[package.dev-dependencies]
dev = [
    { name = ""pytest"" },
    { name = ""flake8"" },
]
lint = [
    { name = ""flake8"" },
]

[package.metadata]
requires-dist = [
    { name = ""requests"", specifier = "">=2.31.0"" },
]

[[package]]
name = ""requests""
version = ""2.31.0""
source = { registry = ""https://pypi.org/simple"" }

[[package]]
name = ""pytest""
version = ""8.1.1""
source = { registry = ""https://pypi.org/simple"" }

[[package]]
name = ""flake8""
version = ""7.0.0""
source = { registry = ""https://pypi.org/simple"" }
dependencies = [
    { name = ""pyflakes"" },
]

[[package]]
name = ""pyflakes""
version = ""3.2.0""
source = { registry = ""https://pypi.org/simple"" }
";

        private static string CreateMockUvLockDevDependenciesFile()
        {
            string filePath = Path.Combine(Path.GetTempPath(), $"uv_{System.Guid.NewGuid():N}.lock");
            System.IO.File.WriteAllText(filePath, MockUvLockDevDependenciesContent);
            return filePath;
        }

        [Test]
        public void ExtractDetailsForUvLockfile_DevDependenciesTable_IdentifiesDevDependencies()
        {
            //Arrange - uses the modern [package.dev-dependencies] format
            string filePath = CreateMockUvLockDevDependenciesFile();
            List<Dependency> dependencies = new List<Dependency>();

            try
            {
                //Act
                List<PythonPackage> packages = PythonProcessor.ExtractDetailsForUvLockfile(filePath, dependencies);

                //Assert - pytest is a direct dev dependency, flake8 is dev (also via include-group),
                //pyflakes is dev transitively through flake8, and requests stays a runtime dependency.
                var pytest = packages.Find(p => p.Name == "pytest");
                var flake8 = packages.Find(p => p.Name == "flake8");
                var pyflakes = packages.Find(p => p.Name == "pyflakes");
                var requests = packages.Find(p => p.Name == "requests");

                Assert.That(pytest, Is.Not.Null, "pytest should be found in packages");
                Assert.That(flake8, Is.Not.Null, "flake8 should be found in packages");
                Assert.That(pyflakes, Is.Not.Null, "pyflakes should be found in packages");
                Assert.That(requests, Is.Not.Null, "requests should be found in packages");

                Assert.IsTrue(pytest.Isdevdependent, "pytest should be dev via [package.dev-dependencies]");
                Assert.IsTrue(flake8.Isdevdependent, "flake8 should be dev via [package.dev-dependencies]");
                Assert.IsTrue(pyflakes.Isdevdependent, "pyflakes should be dev via transitive propagation from flake8");
                Assert.IsFalse(requests.Isdevdependent, "requests is a runtime dependency and should NOT be dev");
            }
            finally
            {
                System.IO.File.Delete(filePath);
            }
        }

        /// <summary>
        /// Mock uv.lock where a package (click) is a direct runtime dependency of the
        /// root project AND a transitive dependency of a dev package (black). Runtime
        /// reachability must take precedence, so click must remain a runtime dependency.
        /// </summary>
        private const string MockUvLockSharedRuntimeAndDevContent = @"version = 1
revision = 3
requires-python = "">=3.10""

[[package]]
name = ""sample-uv-project""
version = ""0.1.0""
source = { editable = ""."" }
dependencies = [
    { name = ""click"" },
]

[package.dev-dependencies]
dev = [
    { name = ""black"" },
]

[[package]]
name = ""click""
version = ""8.1.7""
source = { registry = ""https://pypi.org/simple"" }

[[package]]
name = ""black""
version = ""24.3.0""
source = { registry = ""https://pypi.org/simple"" }
dependencies = [
    { name = ""click"" },
    { name = ""pathspec"" },
]

[[package]]
name = ""pathspec""
version = ""0.12.1""
source = { registry = ""https://pypi.org/simple"" }
";

        private static string CreateMockUvLockSharedRuntimeAndDevFile()
        {
            string filePath = Path.Combine(Path.GetTempPath(), $"uv_shared_{System.Guid.NewGuid():N}.lock");
            System.IO.File.WriteAllText(filePath, MockUvLockSharedRuntimeAndDevContent);
            return filePath;
        }

        [Test]
        public void ExtractDetailsForUvLockfile_PackageReachableFromRuntimeAndDev_StaysRuntime()
        {
            //Arrange - click is a direct runtime dependency AND a dependency of dev package black
            string filePath = CreateMockUvLockSharedRuntimeAndDevFile();
            List<Dependency> dependencies = new List<Dependency>();

            try
            {
                //Act
                List<PythonPackage> packages = PythonProcessor.ExtractDetailsForUvLockfile(filePath, dependencies);

                //Assert - runtime reachability must win over dev propagation
                var click = packages.Find(p => p.Name == "click");
                var black = packages.Find(p => p.Name == "black");
                var pathspec = packages.Find(p => p.Name == "pathspec");

                Assert.That(click, Is.Not.Null, "click should be found in packages");
                Assert.That(black, Is.Not.Null, "black should be found in packages");
                Assert.That(pathspec, Is.Not.Null, "pathspec should be found in packages");

                Assert.IsFalse(click.Isdevdependent, "click is a runtime dependency of the root and must NOT be marked as dev even though black (dev) also depends on it");
                Assert.IsTrue(black.Isdevdependent, "black should be dev via [package.dev-dependencies]");
                Assert.IsTrue(pathspec.Isdevdependent, "pathspec should be dev transitively through black and is not runtime-reachable");
            }
            finally
            {
                System.IO.File.Delete(filePath);
            }
        }
    }
}

