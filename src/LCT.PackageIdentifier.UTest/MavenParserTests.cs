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
using System.Reflection;
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
            _mavenProcessor = new MavenProcessor(Mock.Of<ICycloneDXBomParser>(), Mock.Of<ISpdxBomParser>());
            
            // Reset KPI data before each test to ensure clean state
            BomCreator.bomKpiData.DevdependencyComponents = 0;
            BomCreator.bomKpiData.ThirdPartyRepoComponents = 0;
            BomCreator.bomKpiData.ReleaseRepoComponents = 0;
            BomCreator.bomKpiData.UnofficialComponents = 0;
            BomCreator.bomKpiData.ComponentsExcludedSW360 = 0;
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
            MavenProcessor.AddSiemensDirectProperty(ref bom);

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
            MavenProcessor.AddSiemensDirectProperty(ref bom);

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

            
            CommonAppSettings appSettings = new CommonAppSettings()
            {
                ProjectType = "MAVEN",
                Maven = new Config() { Include = Includes, Exclude = Excludes },
                SW360 = new SW360() { IgnoreDevDependency = true },
                Directory = new LCT.Common.Directory()
                {
                    InputFolder = filepath
                }
            };
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();
            Mock<ISpdxBomParser> spdxBomParser = new Mock<ISpdxBomParser>();
            MavenProcessor MavenProcessor = new MavenProcessor(cycloneDXBomParser.Object, spdxBomParser.Object);

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
           
            CommonAppSettings appSettings = new CommonAppSettings()
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
            Mock<ISpdxBomParser> spdxBomParser = new Mock<ISpdxBomParser>();
            // Act
            MavenProcessor mavenProcessor = new MavenProcessor(cycloneDXBomParser.Object, spdxBomParser.Object);
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
            
            CommonAppSettings appSettings = new CommonAppSettings()
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
            Mock<ISpdxBomParser> spdxBomParser = new Mock<ISpdxBomParser>();
            // Act
            MavenProcessor mavenProcessor = new MavenProcessor(cycloneDXBomParser.Object, spdxBomParser.Object);
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
            
            CommonAppSettings appSettings = new CommonAppSettings()
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
            Mock<ISpdxBomParser> spdxBomParser = new Mock<ISpdxBomParser>();
            // Act
            MavenProcessor mavenProcessor = new MavenProcessor(cycloneDXBomParser.Object, spdxBomParser.Object);
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
            
            CommonAppSettings appSettings = new CommonAppSettings()
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
            Mock<ISpdxBomParser> spdxBomParser = new Mock<ISpdxBomParser>();
            // Act
            MavenProcessor mavenProcessor = new MavenProcessor(cycloneDXBomParser.Object, spdxBomParser.Object);
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
            
            CommonAppSettings appSettings = new CommonAppSettings()
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
            Mock<ISpdxBomParser> spdxBomParser = new Mock<ISpdxBomParser>();
            // Act
            MavenProcessor mavenProcessor = new MavenProcessor(cycloneDXBomParser.Object, spdxBomParser.Object);
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
            
            CommonAppSettings appSettings = new CommonAppSettings()
            {
                ProjectType = "MAVEN",
                Maven = new Config() { Include = Includes, Exclude = Excludes },
                SW360 = new SW360() { IgnoreDevDependency = true },
                Directory = new LCT.Common.Directory()
                {
                    InputFolder = filepath
                }
            };
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();
            Mock<ISpdxBomParser> spdxBomParser = new Mock<ISpdxBomParser>();
            MavenProcessor MavenProcessor = new MavenProcessor(cycloneDXBomParser.Object, spdxBomParser.Object);

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

            CommonAppSettings appSettings = new CommonAppSettings()
            {
                ProjectType = "MAVEN",
                Maven = new Config() { Include = Includes, Exclude = Excludes },
                SW360 = new SW360() { IgnoreDevDependency = true },
                Directory = new LCT.Common.Directory()
                {
                    InputFolder = filepath
                }
            };
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();
            Mock<ISpdxBomParser> spdxBomParser = new Mock<ISpdxBomParser>();
            MavenProcessor MavenProcessor = new MavenProcessor(cycloneDXBomParser.Object, spdxBomParser.Object);

            //Act
            MavenProcessor.ParsePackageFile(appSettings);

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
                        
            CommonAppSettings appSettings = new CommonAppSettings()
            {
                ProjectType = "MAVEN",
                Maven = new Config() { Include = Includes, Exclude = Excludes },
                SW360 = new SW360() { IgnoreDevDependency = true },
                Directory = new LCT.Common.Directory()
                {
                    InputFolder = filepath,

                }
            };
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();
            Mock<ISpdxBomParser> spdxBomParser = new Mock<ISpdxBomParser>();
            MavenProcessor MavenProcessor = new MavenProcessor(cycloneDXBomParser.Object, spdxBomParser.Object);

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

            CommonAppSettings appSettings = new CommonAppSettings()
            {
                ProjectType = "MAVEN",
                Maven = new Config() { Include = Includes, Exclude = Excludes },
                SW360 = new SW360() { IgnoreDevDependency = true },
                Directory = new LCT.Common.Directory()
                {
                    InputFolder = filepath,
                }
            };
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();
            Mock<ISpdxBomParser> spdxBomParser = new Mock<ISpdxBomParser>();
            MavenProcessor MavenProcessor = new MavenProcessor(cycloneDXBomParser.Object, spdxBomParser.Object);

            //Act
            Bom bom = MavenProcessor.ParsePackageFile(appSettings);

            bool isUpdated = bom.Components.Exists(x => x.Properties != null && x.Properties.Exists(x => x.Name == Dataconstant.Cdx_IdentifierType && x.Value == Dataconstant.Discovered));

            //Assert
            Assert.IsTrue(isUpdated, "Checks For Updated Property In List ");

        }

        [Test]
        public void ParsePackageFile_WithExcludedComponents_RemovesExcludedComponentsAndUpdatesBomKpi()
        {
            // Arrange
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string filepath = Path.GetFullPath(Path.Combine(outFolder, "PackageIdentifierUTTestFiles"));
            string[] Includes = { "CycloneDX_Maven.cdx.json" };
            string[] Excludes = { "lol" };

            // Reset BOM KPI data
            BomCreator.bomKpiData.ComponentsExcludedSW360 = 0;

            CommonAppSettings appSettings = new CommonAppSettings()
            {
                ProjectType = "MAVEN",
                Maven = new Config() { Include = Includes, Exclude = Excludes },
                SW360 = new SW360() 
                { 
                    IgnoreDevDependency = true,
                    // Use the actual component from the test data: joda-time/joda-time:2.9.2
                    ExcludeComponents = new List<string> { "joda-time:2.9.2" }
                },
                Directory = new LCT.Common.Directory()
                {
                    InputFolder = filepath,
                }
            };
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();
            Mock<ISpdxBomParser> spdxBomParser = new Mock<ISpdxBomParser>();
            MavenProcessor mavenProcessor = new MavenProcessor(cycloneDXBomParser.Object, spdxBomParser.Object);

            // Act
            Bom bom = mavenProcessor.ParsePackageFile(appSettings);

            // Assert
            Assert.IsNotNull(bom, "BOM should not be null");
            Assert.IsNotNull(bom.Components, "Components should not be null");
            Assert.IsTrue(bom.Components.Count > 0, "BOM should have components");
            
            // Verify that the excluded component has the exclude property set
            // The component should still be in the BOM but marked as excluded
            var excludedComponent = bom.Components.Find(c => 
                c.Name == "joda-time" && 
                c.Version == "2.9.2");
            
            Assert.IsNotNull(excludedComponent, "joda-time component should exist in the BOM");
            
            // Component should have the exclude property
            bool hasExcludeProperty = excludedComponent.Properties?.Exists(p => 
                p.Name == Dataconstant.Cdx_ExcludeComponent && p.Value == "true") ?? false;
            Assert.IsTrue(hasExcludeProperty, "Excluded component should have exclude property set to true");

            // Verify that BOM KPI data was updated with excluded components count
            Assert.That(BomCreator.bomKpiData.ComponentsExcludedSW360, Is.EqualTo(1), 
                "ComponentsExcludedSW360 should be exactly 1 when one component is excluded");
        }

        [Test]
        public void ParsePackageFile_WithNullExcludedComponents_DoesNotCallRemoveExcludedComponents()
        {
            // Arrange
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string filepath = Path.GetFullPath(Path.Combine(outFolder, "PackageIdentifierUTTestFiles"));
            string[] Includes = { "CycloneDX_Maven.cdx.json" };
            string[] Excludes = { "lol" };

            // Reset BOM KPI data
            int initialExcludedCount = BomCreator.bomKpiData.ComponentsExcludedSW360;

            CommonAppSettings appSettings = new CommonAppSettings()
            {
                ProjectType = "MAVEN",
                Maven = new Config() { Include = Includes, Exclude = Excludes },
                SW360 = new SW360() 
                { 
                    IgnoreDevDependency = true,
                    ExcludeComponents = null // Null excluded components
                },
                Directory = new LCT.Common.Directory()
                {
                    InputFolder = filepath,
                }
            };
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();
            Mock<ISpdxBomParser> spdxBomParser = new Mock<ISpdxBomParser>();
            MavenProcessor mavenProcessor = new MavenProcessor(cycloneDXBomParser.Object, spdxBomParser.Object);

            // Act
            Bom bom = mavenProcessor.ParsePackageFile(appSettings);

            // Assert
            Assert.IsNotNull(bom, "BOM should not be null");
            Assert.IsNotNull(bom.Components, "Components should not be null");
            
            // Verify that excluded components logic was not executed
            Assert.That(BomCreator.bomKpiData.ComponentsExcludedSW360, Is.EqualTo(initialExcludedCount), 
                "ComponentsExcludedSW360 should not be incremented when ExcludeComponents is null");
        }

        [Test]
        public void ParsePackageFile_WithEmptyExcludedComponents_DoesNotCallRemoveExcludedComponents()
        {
            // Arrange
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string filepath = Path.GetFullPath(Path.Combine(outFolder, "PackageIdentifierUTTestFiles"));
            string[] Includes = { "CycloneDX_Maven.cdx.json" };
            string[] Excludes = { "lol" };

            // Reset BOM KPI data
            int initialExcludedCount = BomCreator.bomKpiData.ComponentsExcludedSW360;

            CommonAppSettings appSettings = new CommonAppSettings()
            {
                ProjectType = "MAVEN",
                Maven = new Config() { Include = Includes, Exclude = Excludes },
                SW360 = new SW360() 
                { 
                    IgnoreDevDependency = true,
                    ExcludeComponents = new List<string>() // Empty list
                },
                Directory = new LCT.Common.Directory()
                {
                    InputFolder = filepath,
                }
            };
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();
            Mock<ISpdxBomParser> spdxBomParser = new Mock<ISpdxBomParser>();
            MavenProcessor mavenProcessor = new MavenProcessor(cycloneDXBomParser.Object, spdxBomParser.Object);

            // Act
            Bom bom = mavenProcessor.ParsePackageFile(appSettings);

            // Assert
            Assert.IsNotNull(bom, "BOM should not be null");
            Assert.IsNotNull(bom.Components, "Components should not be null");
            
            // Since the exclude list is empty but not null, the method will be called but no components will be excluded
            Assert.That(BomCreator.bomKpiData.ComponentsExcludedSW360, Is.EqualTo(initialExcludedCount), 
                "ComponentsExcludedSW360 should not be incremented when ExcludeComponents is empty");
        }

        #region UpdateKpiDataBasedOnRepo Tests

        [Test]
        public void UpdateKpiDataBasedOnRepo_DevDepRepo_IncrementsDevdependencyComponents()
        {
            // Arrange
            BomCreator.bomKpiData.DevdependencyComponents = 0;
            var appSettings = CreateTestAppSettings();
            appSettings.Maven.DevDepRepo = "dev-repo";
            string repoValue = "dev-repo";

            // Act
            var method = typeof(MavenProcessor).GetMethod("UpdateKpiDataBasedOnRepo", BindingFlags.NonPublic | BindingFlags.Static);
            method.Invoke(null, new object[] { repoValue, appSettings });

            // Assert
            Assert.AreEqual(1, BomCreator.bomKpiData.DevdependencyComponents);
        }

        [Test]
        public void UpdateKpiDataBasedOnRepo_ThirdPartyRepo_IncrementsThirdPartyRepoComponents()
        {
            // Arrange
            BomCreator.bomKpiData.ThirdPartyRepoComponents = 0;
            var appSettings = CreateTestAppSettings();
            appSettings.Maven.Artifactory.ThirdPartyRepos = new List<ThirdPartyRepo>
            {
                new ThirdPartyRepo { Name = "third-party-repo-1", Upload = true },
                new ThirdPartyRepo { Name = "third-party-repo-2", Upload = false }
            };
            string repoValue = "third-party-repo-1";

            // Act
            var method = typeof(MavenProcessor).GetMethod("UpdateKpiDataBasedOnRepo", BindingFlags.NonPublic | BindingFlags.Static);
            method.Invoke(null, new object[] { repoValue, appSettings });

            // Assert
            Assert.AreEqual(1, BomCreator.bomKpiData.ThirdPartyRepoComponents);
        }

        [Test]
        public void UpdateKpiDataBasedOnRepo_ReleaseRepo_IncrementsReleaseRepoComponents()
        {
            // Arrange
            BomCreator.bomKpiData.ReleaseRepoComponents = 0;
            var appSettings = CreateTestAppSettings();
            appSettings.Maven.ReleaseRepo = "release-repo";
            string repoValue = "release-repo";

            // Act
            var method = typeof(MavenProcessor).GetMethod("UpdateKpiDataBasedOnRepo", BindingFlags.NonPublic | BindingFlags.Static);
            method.Invoke(null, new object[] { repoValue, appSettings });

            // Assert
            Assert.AreEqual(1, BomCreator.bomKpiData.ReleaseRepoComponents);
        }

        [Test]
        public void UpdateKpiDataBasedOnRepo_NotFoundInJFrog_IncrementsUnofficialComponents()
        {
            // Arrange
            BomCreator.bomKpiData.UnofficialComponents = 0;
            var appSettings = CreateTestAppSettings();
            string repoValue = Dataconstant.NotFoundInJFrog;

            // Act
            var method = typeof(MavenProcessor).GetMethod("UpdateKpiDataBasedOnRepo", BindingFlags.NonPublic | BindingFlags.Static);
            method.Invoke(null, new object[] { repoValue, appSettings });

            // Assert
            Assert.AreEqual(1, BomCreator.bomKpiData.UnofficialComponents);
        }

        [Test]
        public void UpdateKpiDataBasedOnRepo_EmptyRepoValue_IncrementsUnofficialComponents()
        {
            // Arrange
            BomCreator.bomKpiData.UnofficialComponents = 0;
            var appSettings = CreateTestAppSettings();
            string repoValue = "";

            // Act
            var method = typeof(MavenProcessor).GetMethod("UpdateKpiDataBasedOnRepo", BindingFlags.NonPublic | BindingFlags.Static);
            method.Invoke(null, new object[] { repoValue, appSettings });

            // Assert
            Assert.AreEqual(1, BomCreator.bomKpiData.UnofficialComponents);
        }

        [Test]
        public void UpdateKpiDataBasedOnRepo_ThirdPartyReposNull_DoesNotIncrementThirdPartyComponents()
        {
            // Arrange
            BomCreator.bomKpiData.ThirdPartyRepoComponents = 0;
            BomCreator.bomKpiData.UnofficialComponents = 0;
            var appSettings = CreateTestAppSettings();
            appSettings.Maven.Artifactory.ThirdPartyRepos = null;
            string repoValue = "some-repo";

            // Act
            var method = typeof(MavenProcessor).GetMethod("UpdateKpiDataBasedOnRepo", BindingFlags.NonPublic | BindingFlags.Static);
            method.Invoke(null, new object[] { repoValue, appSettings });

            // Assert
            Assert.AreEqual(0, BomCreator.bomKpiData.ThirdPartyRepoComponents);
        }        

        [Test]
        public void UpdateKpiDataBasedOnRepo_DevDepRepoTakesPrecedence_OverThirdPartyRepo()
        {
            // Arrange
            BomCreator.bomKpiData.DevdependencyComponents = 0;
            BomCreator.bomKpiData.ThirdPartyRepoComponents = 0;
            var appSettings = CreateTestAppSettings();
            appSettings.Maven.DevDepRepo = "shared-repo";
            appSettings.Maven.Artifactory.ThirdPartyRepos = new List<ThirdPartyRepo>
            {
                new ThirdPartyRepo { Name = "shared-repo", Upload = true }
            };
            string repoValue = "shared-repo";

            // Act
            var method = typeof(MavenProcessor).GetMethod("UpdateKpiDataBasedOnRepo", BindingFlags.NonPublic | BindingFlags.Static);
            method.Invoke(null, new object[] { repoValue, appSettings });

            // Assert
            Assert.AreEqual(1, BomCreator.bomKpiData.DevdependencyComponents);
            Assert.AreEqual(0, BomCreator.bomKpiData.ThirdPartyRepoComponents);
        }

        [Test]
        public void UpdateKpiDataBasedOnRepo_ThirdPartyRepoTakesPrecedence_OverReleaseRepo()
        {
            // Arrange
            BomCreator.bomKpiData.ThirdPartyRepoComponents = 0;
            BomCreator.bomKpiData.ReleaseRepoComponents = 0;
            var appSettings = CreateTestAppSettings();
            appSettings.Maven.ReleaseRepo = "shared-repo";
            appSettings.Maven.Artifactory.ThirdPartyRepos = new List<ThirdPartyRepo>
            {
                new ThirdPartyRepo { Name = "shared-repo", Upload = true }
            };
            string repoValue = "shared-repo";

            // Act
            var method = typeof(MavenProcessor).GetMethod("UpdateKpiDataBasedOnRepo", BindingFlags.NonPublic | BindingFlags.Static);
            method.Invoke(null, new object[] { repoValue, appSettings });

            // Assert
            Assert.AreEqual(1, BomCreator.bomKpiData.ThirdPartyRepoComponents);
            Assert.AreEqual(0, BomCreator.bomKpiData.ReleaseRepoComponents);
        }

        [Test]
        public void UpdateKpiDataBasedOnRepo_MultipleThirdPartyRepos_MatchesCorrectRepo()
        {
            // Arrange
            BomCreator.bomKpiData.ThirdPartyRepoComponents = 0;
            var appSettings = CreateTestAppSettings();
            appSettings.Maven.Artifactory.ThirdPartyRepos = new List<ThirdPartyRepo>
            {
                new ThirdPartyRepo { Name = "repo-1", Upload = true },
                new ThirdPartyRepo { Name = "repo-2", Upload = false },
                new ThirdPartyRepo { Name = "repo-3", Upload = true }
            };
            string repoValue = "repo-2";

            // Act
            var method = typeof(MavenProcessor).GetMethod("UpdateKpiDataBasedOnRepo", BindingFlags.NonPublic | BindingFlags.Static);
            method.Invoke(null, new object[] { repoValue, appSettings });

            // Assert
            Assert.AreEqual(1, BomCreator.bomKpiData.ThirdPartyRepoComponents);
        }

        [Test]
        public void UpdateKpiDataBasedOnRepo_CaseSensitive_RepoNameMatching()
        {
            // Arrange
            BomCreator.bomKpiData.ThirdPartyRepoComponents = 0;
            BomCreator.bomKpiData.UnofficialComponents = 0;
            var appSettings = CreateTestAppSettings();
            appSettings.Maven.Artifactory.ThirdPartyRepos = new List<ThirdPartyRepo>
            {
                new ThirdPartyRepo { Name = "Repo-Name", Upload = true }
            };
            string repoValue = "Not Found in JFrogRepo"; // Different case

            // Act
            var method = typeof(MavenProcessor).GetMethod("UpdateKpiDataBasedOnRepo", BindingFlags.NonPublic | BindingFlags.Static);
            method.Invoke(null, new object[] { repoValue, appSettings });

            // Assert
            Assert.AreEqual(0, BomCreator.bomKpiData.ThirdPartyRepoComponents);
            Assert.AreEqual(1, BomCreator.bomKpiData.UnofficialComponents);
        }

        private CommonAppSettings CreateTestAppSettings()
        {
            return new CommonAppSettings
            {
                Maven = new Config
                {
                    DevDepRepo = "default-dev-repo",
                    ReleaseRepo = "default-release-repo",
                    Artifactory = new Artifactory
                    {
                        ThirdPartyRepos = new List<ThirdPartyRepo>()
                    }
                }
            };
        }

        #endregion

    }
}
