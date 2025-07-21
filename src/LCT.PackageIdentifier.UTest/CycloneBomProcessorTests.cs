// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models;
using LCT.APICommunications.Model;
using LCT.Common;
using LCT.Common.Constants;
using LCT.Common.Model;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using File = System.IO.File;

namespace LCT.PackageIdentifier.UTest
{
    [TestFixture]
    public class CycloneBomProcessorTests
    {

        [Test]
        public void SetMetadataInComparisonBOM_GivenBOMWithEmptyMetadata_FillsInMetadataInfoInBOM()
        {
            //Arrange
            ProjectReleases projectReleases = new ProjectReleases();
            Bom bom = new Bom()
            {
                Metadata = null,
                Components = new List<Component>()
            {
                new Component(){Name="Test",Version="2.2"},
                new Component(){Name="new",Version="4.2"}
            }
            };
            CommonAppSettings appSettings = new CommonAppSettings()
            {
                SW360 = new()

            };
            projectReleases.Version = "1.2.3";
            CatoolInfo caToolInformation = new CatoolInfo() { CatoolVersion = "6.0.0", CatoolRunningLocation = "" };
            //Act
            Bom resultBom = CycloneBomProcessor.SetMetadataInComparisonBOM(bom, appSettings, projectReleases, caToolInformation);

            //Assert
            Assert.IsNotNull(resultBom.Metadata, "Metadata should not be null.");
            Assert.AreEqual(1, resultBom.Metadata.Tools.Components.Count, "Metadata should contain one tool component.");

        }
        [Test]
        public void SetMetadataInComparisonBOM_GivenBOMWithMetadata_AddsNewMetadataInfoInBOM()
        {
            //Arrange
            ProjectReleases projectReleases = new ProjectReleases();
            projectReleases.Version = "1.0";

            Bom bom = new Bom
            {
                Metadata = new Metadata
                {
                    Tools = new ToolChoices
                    {
                        Components = new List<Component>
            {
                new Component
                {
                    Supplier = new OrganizationalEntity
                    {
                        Name = "Siemens AG"
                    },
                    Name = "Clearing Automation Tool",
                    Version = "8.0.0",
                    ExternalReferences = new List<ExternalReference>
                    {
                        new ExternalReference
                        {
                            Url = "",
                            Type = ExternalReference.ExternalReferenceType.Website
                        }
                    }
                }
            }
                    }
                },
                Definitions = new Definitions()
            };

            CommonAppSettings appSettings = new CommonAppSettings()
            {
                SW360 = new() { ProjectName = "Test" }
            };
            projectReleases.Version = "1.2.3";


            Component component = new Component
            {
                Name = appSettings.SW360.ProjectName,
                Version = projectReleases.Version,
                Type = Component.Classification.Application
            };

            CatoolInfo caToolInformation = new CatoolInfo() { CatoolVersion = "6.0.0", CatoolRunningLocation = "" };
            //Act
            Bom files = CycloneBomProcessor.SetMetadataInComparisonBOM(bom, appSettings, projectReleases, caToolInformation);

            //Assert
            Assert.That(bom.Metadata.Tools.Components[0].Name, Is.EqualTo(files.Metadata.Tools.Components[0].Name), "Returns bom with metadata tools");
            Assert.AreEqual(files.Definitions.Standards[0].Name, "Standard BOM");
            Assert.AreEqual(files.Definitions.Standards[0].Version, "3.0.0");
            Assert.That(component.Name, Is.EqualTo(files.Metadata.Component.Name), "Returns bom with metadata component ");
            Assert.That(component.Version, Is.EqualTo(files.Metadata.Component.Version), "Returns bom with metadata component ");
            Assert.That(component.Type, Is.EqualTo(files.Metadata.Component.Type), "Returns bom with metadata component ");
        }
        [Test]
        public void SetProperties_GivenComponent_SetsPropertiesInBOM()
        {
            //Arrange
            List<Component> componentForBOM = new List<Component>();
            string repo = "org1-npmjs-npm-remote";
            List<Property> expectedpropList = new List<Property>()
            {
                new Property(){ Name = Dataconstant.Cdx_ProjectType,Value = "NPM"},
                 new Property(){ Name = Dataconstant.Cdx_ArtifactoryRepoName,Value = repo},
                 new Property(){ Name = Dataconstant.Cdx_IsInternal,Value = "false"},

            };
            List<Component> componentForBOMexpected = new List<Component>()
            {
                    new Component()
            {
                Name = "test",
                Version = "1.2.3",
                Properties  =expectedpropList
            }
            };
            Component component = new Component()
            {
                Name = "test",
                Version = "1.2.3"
            };

            CommonAppSettings appsettings = new CommonAppSettings()
            {
                ProjectType = "NPM"
            };


            //Act
            CycloneBomProcessor.SetProperties(appsettings, component, ref componentForBOM, repo);

            //Assert
            Assert.That(componentForBOMexpected.Count, Is.EqualTo(componentForBOM.Count), "Returns component list with properties ");

        }
        [Test]
        public void ParseCycloneDXBom_GivenBOMFilePath_ReturnsBOM()
        {
            //Arrange
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string BomTestFile = Path.GetFullPath(Path.Combine(outFolder, "PackageIdentifierUTTestFiles", "CycloneDX_Debian.cdx.json"));

            //Act
            CycloneDXBomParser cycloneBomProcessor = new CycloneDXBomParser();
            Bom files = cycloneBomProcessor.ParseCycloneDXBom(BomTestFile);

            //Assert
            Assert.That(4, Is.EqualTo(files.Components.Count), "Returns components in BOM");

        }

        [Test]
        public void ParseCycloneDXBom_GivenInvlidBOMFilePath_ReturnsZeroComponents()
        {
            //Arrange
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string BomTestFile = Path.GetFullPath(Path.Combine(outFolder, "PackageIdentifierUTTestFiles", "Cyclonedx1.json"));

            //Act
            CycloneDXBomParser cycloneBomProcessor = new CycloneDXBomParser();
            Bom files = cycloneBomProcessor.ParseCycloneDXBom(BomTestFile);

            //Assert
            Assert.IsNull(files.Components, "Returns Zero components in BOM");

        }

        [Test]
        public void ParseCycloneDXBom_GivenInCorrectJsonFile_ReturnsZeroComponents()
        {
            //Arrange
            string sourcePath = $"{Path.GetTempPath()}";
            File.WriteAllText(sourcePath + "output.json", "{[}");

            //Act
            CycloneDXBomParser cycloneBomProcessor = new CycloneDXBomParser();
            Bom files = cycloneBomProcessor.ParseCycloneDXBom(Path.GetFullPath(Path.Combine(sourcePath, "output.json")));

            //Assert
            Assert.IsNull(files.Components, "Returns Zero components in BOM");

        }

        [Test]
        public void ExtractSBOMDetailsFromTemplate_GivenBomTemplateWithComponents_RetrunsBom()
        {
            //Arrange
            Bom bom = new Bom()
            {
                Metadata = null,
                Components = new List<Component>()
            {
                new Component(){Name="Test",Version="2.2"},
                new Component(){Name="new",Version="4.2"}
            }
            };

            //Act

            Bom files = CycloneDXBomParser.ExtractSBOMDetailsFromTemplate(bom);

            //Assert
            Assert.IsNotNull(files.Components, "Returns Non Zero components in BOM");

        }
        [Test]
        public void ExtractSBOMDetailsFromTemplate_GivenBomTemplateWithoutComponents_RetrunsBom()
        {
            //Arrange
            Bom bom = new Bom()
            {
                Metadata = null,
                Components = null


            };

            //Act

            Bom files = CycloneDXBomParser.ExtractSBOMDetailsFromTemplate(bom);

            //Assert
            Assert.That(0, Is.EqualTo(files.Components.Count), "Returns Zero components in BOM");

        }

        [Test]
        public void SetProperties_ComponentWithUnsupportedPackageType_AddsOnlyDirectAndDevelopmentProperties()
        {
            // Arrange
            List<Component> componentForBOM = new List<Component>();
            Component component = new Component()
            {
                Name = "unsupported-component",
                Version = "1.0.0",
                Publisher = Dataconstant.UnsupportedPackageType
            };

            CommonAppSettings appSettings = new CommonAppSettings()
            {
                ProjectType = "NPM"
            };

            string repo = "test-repo";

            // Act
            CycloneBomProcessor.SetProperties(appSettings, component, ref componentForBOM, repo);

            // Assert
            Assert.That(componentForBOM.Count, Is.EqualTo(1), "Should add one component to the BOM");
            
            Component resultComponent = componentForBOM[0];
            Assert.That(resultComponent.Properties.Count, Is.EqualTo(2), "Should have exactly 2 properties for unsupported package type");
            
            // Verify only isDirect and isDevelopment properties are added
            bool hasDirectProperty = false;
            bool hasDevelopmentProperty = false;
            
            foreach (var property in resultComponent.Properties)
            {
                if (property.Name == Dataconstant.Cdx_SiemensDirect && property.Value == "true")
                    hasDirectProperty = true;
                else if (property.Name == Dataconstant.Cdx_IsDevelopment && property.Value == "false")
                    hasDevelopmentProperty = true;
            }
            
            Assert.IsTrue(hasDirectProperty, "Should have siemens:direct property set to true");
            Assert.IsTrue(hasDevelopmentProperty, "Should have development property set to false");
            
            // Verify that other properties are NOT added
            foreach (var property in resultComponent.Properties)
            {
                Assert.AreNotEqual(Dataconstant.Cdx_ProjectType, property.Name, "Should not have project type property");
                Assert.AreNotEqual(Dataconstant.Cdx_ArtifactoryRepoName, property.Name, "Should not have artifactory repo property");
                Assert.AreNotEqual(Dataconstant.Cdx_IsInternal, property.Name, "Should not have internal property");
                Assert.AreNotEqual(Dataconstant.Cdx_Siemensfilename, property.Name, "Should not have filename property");
                Assert.AreNotEqual(Dataconstant.Cdx_JfrogRepoPath, property.Name, "Should not have jfrog repo path property");
            }
            
            Assert.IsNull(resultComponent.Description, "Description should be set to null");
        }

        [Test]
        public void SetProperties_ComponentWithSupportedPackageType_AddsAllProperties()
        {
            // Arrange
            List<Component> componentForBOM = new List<Component>();
            Component component = new Component()
            {
                Name = "supported-component",
                Version = "2.0.0",
                Publisher = "npm" // Not unsupported package type
            };

            CommonAppSettings appSettings = new CommonAppSettings()
            {
                ProjectType = "NPM"
            };

            string repo = "test-repo";

            // Act
            CycloneBomProcessor.SetProperties(appSettings, component, ref componentForBOM, repo);

            // Assert
            Assert.That(componentForBOM.Count, Is.EqualTo(1), "Should add one component to the BOM");
            
            Component resultComponent = componentForBOM[0];
            Assert.That(resultComponent.Properties.Count, Is.EqualTo(7), "Should have exactly 7 properties for supported package type");
            
            // Verify all expected properties are added
            var propertyDict = new Dictionary<string, string>();
            foreach (var property in resultComponent.Properties)
            {
                propertyDict[property.Name] = property.Value;
            }
            
            Assert.IsTrue(propertyDict.ContainsKey(Dataconstant.Cdx_IsInternal), "Should have internal property");
            Assert.AreEqual("false", propertyDict[Dataconstant.Cdx_IsInternal], "Internal property should be false");
            
            Assert.IsTrue(propertyDict.ContainsKey(Dataconstant.Cdx_ArtifactoryRepoName), "Should have artifactory repo property");
            Assert.AreEqual(repo, propertyDict[Dataconstant.Cdx_ArtifactoryRepoName], "Artifactory repo should match input");
            
            Assert.IsTrue(propertyDict.ContainsKey(Dataconstant.Cdx_ProjectType), "Should have project type property");
            Assert.AreEqual("NPM", propertyDict[Dataconstant.Cdx_ProjectType], "Project type should match app settings");
            
            Assert.IsTrue(propertyDict.ContainsKey(Dataconstant.Cdx_IsDevelopment), "Should have development property");
            Assert.AreEqual("false", propertyDict[Dataconstant.Cdx_IsDevelopment], "Development property should be false");
            
            Assert.IsTrue(propertyDict.ContainsKey(Dataconstant.Cdx_SiemensDirect), "Should have siemens direct property");
            Assert.AreEqual("true", propertyDict[Dataconstant.Cdx_SiemensDirect], "Siemens direct property should be true");
            
            Assert.IsTrue(propertyDict.ContainsKey(Dataconstant.Cdx_Siemensfilename), "Should have filename property");
            Assert.AreEqual(Dataconstant.PackageNameNotFoundInJfrog, propertyDict[Dataconstant.Cdx_Siemensfilename], "Filename should be default value");
            
            Assert.IsTrue(propertyDict.ContainsKey(Dataconstant.Cdx_JfrogRepoPath), "Should have jfrog repo path property");
            Assert.AreEqual(Dataconstant.JfrogRepoPathNotFound, propertyDict[Dataconstant.Cdx_JfrogRepoPath], "JFrog repo path should be default value");
            
            Assert.IsNull(resultComponent.Description, "Description should be set to null");
        }

        [Test]
        public void SetProperties_ComponentWithNullPublisher_AddsAllProperties()
        {
            // Arrange
            List<Component> componentForBOM = new List<Component>();
            Component component = new Component()
            {
                Name = "null-publisher-component",
                Version = "3.0.0",
                Publisher = null // Null publisher should go to else branch
            };

            CommonAppSettings appSettings = new CommonAppSettings()
            {
                ProjectType = "MAVEN"
            };

            string repo = "maven-repo";

            // Act
            CycloneBomProcessor.SetProperties(appSettings, component, ref componentForBOM, repo);

            // Assert
            Assert.That(componentForBOM.Count, Is.EqualTo(1), "Should add one component to the BOM");
            
            Component resultComponent = componentForBOM[0];
            Assert.That(resultComponent.Properties.Count, Is.EqualTo(7), "Should have exactly 7 properties for non-unsupported package type");
            
            // Verify project type matches app settings
            var projectTypeProperty = resultComponent.Properties.Find(p => p.Name == Dataconstant.Cdx_ProjectType);
            Assert.IsNotNull(projectTypeProperty, "Should have project type property");
            Assert.AreEqual("MAVEN", projectTypeProperty.Value, "Project type should match app settings");
            
            Assert.IsNull(resultComponent.Description, "Description should be set to null");
        }
    }
}
