// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models;
using LCT.APICommunications.Model;
using LCT.Common;
using LCT.Common.Constants;
using LCT.Common.Model;
using LCT.PackageIdentifier;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;

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
                CaVersion = "1.2.3"
            };
            CatoolInfo caToolInformation = new CatoolInfo() { CatoolVersion = "6.0.0", CatoolRunningLocation="" };
            //Act
            Bom files = CycloneBomProcessor.SetMetadataInComparisonBOM(bom, appSettings, projectReleases, caToolInformation);

            //Assert
            Assert.That(2, Is.EqualTo(files.Metadata.Tools.Count), "Returns bom with metadata ");

        }
        [Test]
        public void SetMetadataInComparisonBOM_GivenBOMWithMetadata_AddsNewMetadataInfoInBOM()
        {
            //Arrange
            ProjectReleases projectReleases = new ProjectReleases();            
            projectReleases.Version= "1.0";
            
            Bom bom = new Bom()
            {
                Metadata = new Metadata()
                {
                    Tools = new List<Tool>() {
                        new Tool() {
                            Name = "Existing Data", Version = "1.0.", Vendor = "AG" } },
                    Component = new Component()
                },
                Components = new List<Component>()
            {
                new Component(){Name="Test",Version="2.2"},
                new Component(){Name="new",Version="4.2"}
            }
            };
            CommonAppSettings appSettings = new CommonAppSettings()
            {
                CaVersion = "1.2.3",
                SW360ProjectName = "Test",
            };

            Tool tools = new Tool()
            {
                Name = "Clearing Automation Tool",
                Version = "1.0.17",
                Vendor = "Siemens AG"
            };
            Tool SiemensSBOM = new Tool
            {
                Name = "Siemens SBOM",
                Version = "2.0.0",
                Vendor = "Siemens AG",                
            };
            Component component = new Component
            {
                Name = appSettings.SW360ProjectName,
                Version = projectReleases.Version,
                Type = Component.Classification.Application
            };

            CatoolInfo caToolInformation = new CatoolInfo() { CatoolVersion = "6.0.0", CatoolRunningLocation = "" };
            //Act
            Bom files = CycloneBomProcessor.SetMetadataInComparisonBOM(bom, appSettings, projectReleases, caToolInformation);

            //Assert
            Assert.That(tools.Name, Is.EqualTo(files.Metadata.Tools[0].Name), "Returns bom with metadata tools");
            Assert.That(SiemensSBOM.Name, Is.EqualTo(files.Metadata.Tools[1].Name), "Returns bom with metadata tools");
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
            string BomTestFile = outFolder + @"\PackageIdentifierUTTestFiles\CycloneDX_Debian.cdx.json";

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
            string BomTestFile = outFolder + @"\PackageIdentifierUTTestFiles\Cyclonedx1.json";

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
            string sourcePath = $"{Path.GetTempPath()}\\";
            File.WriteAllText(sourcePath + "output.json", "{[}");

            //Act
            CycloneDXBomParser cycloneBomProcessor = new CycloneDXBomParser();
            Bom files = cycloneBomProcessor.ParseCycloneDXBom(sourcePath + "/output.json");

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
            Assert.That(0,Is.EqualTo(files.Components.Count), "Returns Zero components in BOM");

        }
    }
}
