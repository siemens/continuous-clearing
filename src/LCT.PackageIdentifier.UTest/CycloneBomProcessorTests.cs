// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models;
using LCT.Common;
using LCT.Common.Constants;
using LCT.PackageIdentifier;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;

namespace PackageIdentifier.UTest
{
    public class CycloneBomProcessorTests
    {

        [Test]
        public void SetMetadataInComparisonBOM_GivenBOMWithEmptyMetadata_FillsInMetadataInfoInBOM()
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
            CommonAppSettings appSettings = new CommonAppSettings()
            {
                CaVersion = "1.2.3"
            };
            //Act
            Bom files = CycloneBomProcessor.SetMetadataInComparisonBOM(bom, appSettings);

            //Assert
            Assert.That(1, Is.EqualTo(files.Metadata.Tools.Count), "Returns bom with metadata ");

        }
        [Test]
        public void SetMetadataInComparisonBOM_GivenBOMWithMetadata_AddsNewMetadataInfoInBOM()
        {
            //Arrange
            Bom bom = new Bom()
            {
                Metadata = new Metadata()
                {
                    Tools = new List<Tool>(){
                    new Tool(){
                        Name = "Existing Data",Version = "1.0.",Vendor = "AG"} }
                },
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

            Tool tools = new Tool()
            {
                Name = "Clearing Automation Tool",
                Version = "1.0.17",
                Vendor = "Siemens AG"
            };
            //Act
            Bom files = CycloneBomProcessor.SetMetadataInComparisonBOM(bom, appSettings);

            //Assert
            Assert.That(tools.Name, Is.EqualTo(files.Metadata.Tools[1].Name), "Returns bom with metadata ");

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
                 new Property(){ Name = Dataconstant.Cdx_ArtifactoryRepoUrl,Value = repo},
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
