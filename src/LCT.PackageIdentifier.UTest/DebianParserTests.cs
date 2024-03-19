// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models;
using LCT.PackageIdentifier;
using NUnit.Framework;
using System.IO;
using LCT.Common;
using LCT.Common.Model;
using LCT.Common.Constants;
using Moq;

namespace PackageIdentifier.UTest
{
    [TestFixture]
    class DebianParserTests
    {
        [Test]
        public void ParsePackageConfig_GivenAMultipleInputFilePath_ReturnsCounts()
        {
            //Arrange
            int expectednoofcomponents = 8;
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string OutFolder = Path.GetDirectoryName(exePath);
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();

            DebianProcessor DebianProcessor = new DebianProcessor(cycloneDXBomParser.Object);
            string[] Includes = { "*_Debian.cdx.json" };
            CommonAppSettings appSettings = new CommonAppSettings()
            {
                ProjectType = "DEBIAN",
                RemoveDevDependency = true,
                Debian = new Config() { Include = Includes },
                PackageFilePath = OutFolder + @"\PackageIdentifierUTTestFiles"
            };

            //Act
            Bom listofcomponents = DebianProcessor.ParsePackageFile(appSettings);

            //Assert
            Assert.That(expectednoofcomponents, Is.EqualTo(listofcomponents.Components.Count), "Checks for no of components");
        }


        [Test]
        public void ParsePackageConfig_GivenAInputFilePath_ReturnsCounts()
        {
            //Arrange
            int expectednoofcomponents = 4;
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();

            string OutFolder = Path.GetDirectoryName(exePath);
            DebianProcessor DebianProcessor = new DebianProcessor(cycloneDXBomParser.Object);
            string[] Includes = { "CycloneDX_Debian.cdx.json" };
            CommonAppSettings appSettings = new CommonAppSettings()
            {
                PackageFilePath = OutFolder + @"\PackageIdentifierUTTestFiles",
                ProjectType = "DEBIAN",
                RemoveDevDependency = true,
                Debian = new Config() { Include = Includes }
            };

            //Act
            Bom listofcomponents = DebianProcessor.ParsePackageFile(appSettings);

            //Assert
            Assert.That(expectednoofcomponents, Is.EqualTo(listofcomponents.Components.Count), "Checks for no of components");
        }

        [Test]
        public void ParsePackageConfig_GivenMultipleInputFiles_ReturnsCountOfDuplicates()
        {
            //Arrange
            int duplicateComponents = 1;
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string OutFolder = Path.GetDirectoryName(exePath);
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();

            DebianProcessor DebianProcessor = new DebianProcessor(cycloneDXBomParser.Object);
            string[] Includes = { "*_Debian.cdx.json" };

            CommonAppSettings appSettings = new CommonAppSettings()
            {
                PackageFilePath = OutFolder + @"\PackageIdentifierUTTestFiles",
                ProjectType = "DEBIAN",
                RemoveDevDependency = true,
                Debian = new Config() { Include = Includes }
            };

            //Act
            DebianProcessor.ParsePackageFile(appSettings);

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
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();

            DebianProcessor DebianProcessor = new DebianProcessor(cycloneDXBomParser.Object);
            string[] Includes = { "SourceDetails_Cyclonedx.cdx.json" };

            CommonAppSettings appSettings = new CommonAppSettings()
            {
                PackageFilePath = OutFolder + @"\PackageIdentifierUTTestFiles",
                ProjectType = "DEBIAN",
                RemoveDevDependency = true,
                Debian = new Config() { Include = Includes }
            };

            //Act
            Bom listofcomponents = DebianProcessor.ParsePackageFile(appSettings);

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

            DebianProcessor DebianProcessor = new DebianProcessor(cycloneDXBomParser.Object);
            string[] Includes = { "CycloneDX_Debian.cdx.json", "SBOMTemplate_Debian.cdx.json" };
            string packagefilepath = OutFolder + @"\PackageIdentifierUTTestFiles";

            CommonAppSettings appSettings = new CommonAppSettings()
            {
                PackageFilePath = packagefilepath,
                ProjectType = "DEBIAN",
                RemoveDevDependency = true,
                Debian = new Config() { Include = Includes },
                CycloneDxSBomTemplatePath = packagefilepath + "\\SBOMTemplates\\SBOM_DebianCATemplate.cdx.json"
            };

            //Act
            Bom listofcomponents = DebianProcessor.ParsePackageFile(appSettings);

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

            DebianProcessor DebianProcessor = new DebianProcessor(cycloneDXBomParser.Object);
            string[] Includes = { "CycloneDX_Debian.cdx.json", "SBOMTemplate_Debian.cdx.json" };
            string packagefilepath = OutFolder + @"\PackageIdentifierUTTestFiles";

            CommonAppSettings appSettings = new CommonAppSettings()
            {
                PackageFilePath = packagefilepath,
                ProjectType = "DEBIAN",
                RemoveDevDependency = true,
                Debian = new Config() { Include = Includes },
                CycloneDxSBomTemplatePath = packagefilepath + "\\SBOMTemplates\\SBOMTemplate_Debian.cdx.json",
            };

            //Act
            Bom listofcomponents = DebianProcessor.ParsePackageFile(appSettings);
            bool isUpdated = listofcomponents.Components.Exists(x => x.Properties != null && x.Properties.Exists(x => x.Name == Dataconstant.Cdx_IdentifierType && x.Value == Dataconstant.Discovered));

            //Assert
            Assert.IsTrue(isUpdated, "Checks For Updated Property In List ");
        }
    }
}
