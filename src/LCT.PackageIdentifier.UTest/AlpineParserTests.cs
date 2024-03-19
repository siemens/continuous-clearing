// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models;
using LCT.Common.Model;
using LCT.Common;
using NUnit.Framework;
using System.IO;
using LCT.Common.Constants;
using Moq;

namespace LCT.PackageIdentifier.UTest
{
    [TestFixture]
    class AlpineParserTests
    {
        [Test]
        public void ParsePackageConfig_GivenAMultipleInputFilePath_ReturnsCounts()
        {
            //Arrange
            int expectednoofcomponents = 4;
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string OutFolder = Path.GetDirectoryName(exePath);
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();

            AlpineProcessor alpineProcessor = new AlpineProcessor(cycloneDXBomParser.Object);
            string[] Includes = { "*_Alpine.cdx.json" };
            CommonAppSettings appSettings = new CommonAppSettings()
            {
                ProjectType = "ALPINE",
                RemoveDevDependency = true,
                Alpine = new Config() { Include = Includes },
                PackageFilePath = OutFolder + @"\PackageIdentifierUTTestFiles"
            };

            //Act
            Bom listofcomponents = alpineProcessor.ParsePackageFile(appSettings);

            //Assert
            Assert.That(expectednoofcomponents, Is.EqualTo(listofcomponents.Components.Count), "Checks for no of components");
        }


        [Test]
        public void ParsePackageConfig_GivenAInputFilePath_ReturnsCounts()
        {
            //Arrange
            int expectednoofcomponents = 4;
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string OutFolder = Path.GetDirectoryName(exePath);
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();

            AlpineProcessor alpineProcessor = new AlpineProcessor(cycloneDXBomParser.Object);
            string[] Includes = { "CycloneDX_Alpine.cdx.json" };
            CommonAppSettings appSettings = new CommonAppSettings()
            {
                PackageFilePath = OutFolder + @"\PackageIdentifierUTTestFiles",
                ProjectType = "ALPINE",
                RemoveDevDependency = true,
                Alpine = new Config() { Include = Includes }
            };

            //Act
            Bom listofcomponents = alpineProcessor.ParsePackageFile(appSettings);

            //Assert
            Assert.That(expectednoofcomponents, Is.EqualTo(listofcomponents.Components.Count), "Checks for no of components");
        }

        [Test]
        public void ParsePackageConfig_GivenMultipleInputFiles_ReturnsCountOfDuplicates()
        {
            //Arrange
            int duplicateComponents = 4;
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string OutFolder = Path.GetDirectoryName(exePath);
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();

            AlpineProcessor alpineProcessor = new AlpineProcessor(cycloneDXBomParser.Object);
            string[] Includes = { "*_Alpine.cdx.json" };

            CommonAppSettings appSettings = new CommonAppSettings()
            {
                PackageFilePath = OutFolder + @"\PackageIdentifierUTTestFiles",
                ProjectType = "ALPINE",
                RemoveDevDependency = true,
                Alpine = new Config() { Include = Includes }
            };

            //Act
            alpineProcessor.ParsePackageFile(appSettings);

            //Assert
            Assert.That(duplicateComponents, Is.EqualTo(BomCreator.bomKpiData.DuplicateComponents), "Checks for no of duplicate components");
        }

        [Test]
        public void ParsePackageConfig_GivenAInputFilePath_ReturnsSourceDetails()
        {
            //Arrange
            string sourceName = "alpine-baselayout" + "_" + "3.4.3-r1";
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string OutFolder = Path.GetDirectoryName(exePath);
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();

            AlpineProcessor alpineProcessor = new AlpineProcessor(cycloneDXBomParser.Object);
            string[] Includes = { "AlpineSourceDetails_Cyclonedx.cdx.json" };

            CommonAppSettings appSettings = new CommonAppSettings()
            {
                PackageFilePath = OutFolder + @"\PackageIdentifierUTTestFiles",
                ProjectType = "ALPINE",
                RemoveDevDependency = true,
                Alpine = new Config() { Include = Includes }
            };

            //Act
            Bom listofcomponents = alpineProcessor.ParsePackageFile(appSettings);

            //Assert
            Assert.AreEqual(sourceName, listofcomponents.Components[0].Name + "_" + listofcomponents.Components[0].Version, "Checks component name and version");
        }

        [Test]
        public void ParsePackageConfig_GivenAInputFilePathAlongWithSBOMTemplate_ReturnTotalComponentsList()
        {
            //Arrange
            int expectednoofcomponents = 5;
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string OutFolder = Path.GetDirectoryName(exePath);
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();

            AlpineProcessor alpineProcessor = new AlpineProcessor(cycloneDXBomParser.Object);
            string[] Includes = { "CycloneDX_Alpine.cdx.json", "SBOMTemplate_Alpine.cdx.json" };
            string packagefilepath = OutFolder + @"\PackageIdentifierUTTestFiles";

            CommonAppSettings appSettings = new CommonAppSettings()
            {
                PackageFilePath = packagefilepath,
                ProjectType = "ALPINE",
                RemoveDevDependency = true,
                Alpine = new Config() { Include = Includes },
                CycloneDxSBomTemplatePath = packagefilepath + "\\SBOMTemplates\\SBOM_AlpineCATemplate.cdx.json"
            };

            //Act
            Bom listofcomponents = alpineProcessor.ParsePackageFile(appSettings);

            //Assert
            Assert.That(expectednoofcomponents, Is.EqualTo(listofcomponents.Components.Count), "Checks for no of components");
        }

        [Test]
        public void ParsePackageConfig_GivenAInputFilePathAlongWithSBOMTemplate_ReturnUpdatedComponents()
        {
            //Arrange
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string OutFolder = Path.GetDirectoryName(exePath);
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();

            AlpineProcessor alpineProcessor = new AlpineProcessor(cycloneDXBomParser.Object);
            string[] Includes = { "CycloneDX_Alpine.cdx.json", "SBOMTemplate_Alpine.cdx.json" };
            string packagefilepath = OutFolder + @"\PackageIdentifierUTTestFiles";

            CommonAppSettings appSettings = new CommonAppSettings()
            {
                PackageFilePath = packagefilepath,
                ProjectType = "ALPINE",
                RemoveDevDependency = true,
                Alpine = new Config() { Include = Includes },
                CycloneDxSBomTemplatePath = packagefilepath + "\\SBOMTemplates\\SBOMTemplate_Alpine.cdx.json",
            };

            //Act
            Bom listofcomponents = alpineProcessor.ParsePackageFile(appSettings);
            bool isUpdated = listofcomponents.Components.Exists(x => x.Properties != null && x.Properties.Exists(x => x.Name == Dataconstant.Cdx_IdentifierType && x.Value == Dataconstant.Discovered));

            //Assert
            Assert.IsTrue(isUpdated, "Checks For Updated Property In List ");
        }
    }
}
