// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.PackageIdentifier.Model;
using NUnit.Framework;
using System.IO;
using LCT.Common;
using LCT.Common.Model;
using System.Collections.Generic;
using CycloneDX.Models;
using LCT.Common.Constants;
using Moq;

namespace LCT.PackageIdentifier.UTest
{

    [TestFixture]
    public class NPMParserTests
    {
        [Test]
        public void ParsePackageFile_PackageLockWithDuplicateComponents_ReturnsCountOfDuplicates()
        {
            //Arrange
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string filepath = outFolder + @"\PackageIdentifierUTTestFiles\TestDir";
            string[] Includes = { "p*-lock.json" };
            string[] Excludes = { "node_modules" };

            CommonAppSettings appSettings = new CommonAppSettings()
            {
                BomFolderPath = outFolder,
                PackageFilePath = filepath,
                ProjectType = "NPM",
                RemoveDevDependency = true,
                Npm = new Config() { Include = Includes, Exclude = Excludes }
            };

            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();
            NpmProcessor NpmProcessor = new NpmProcessor(cycloneDXBomParser.Object);

            //Act
            NpmProcessor.ParsePackageFile(appSettings);

            //Assert
            Assert.That(2974, Is.EqualTo(BomCreator.bomKpiData.DuplicateComponents), "Returns the count of duplicate components");

        }
        [Test]
        public void ParsePackageFile_PackageLockWithangular16_ReturnsCountOfComponents()
        {
            //Arrange
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string filepath = outFolder + @"\PackageIdentifierUTTestFiles";
            string[] Includes = { "p*-lock16.json" };
            string[] Excludes = { "node_modules" };

            CommonAppSettings appSettings = new CommonAppSettings()
            {
                BomFolderPath = outFolder,
                PackageFilePath = filepath,
                ProjectType = "NPM",
                RemoveDevDependency = true,
                Npm = new Config() { Include = Includes, Exclude = Excludes }
            };
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();
            NpmProcessor NpmProcessor = new NpmProcessor(cycloneDXBomParser.Object);

            //Act
            Bom bom=NpmProcessor.ParsePackageFile(appSettings);

            //Assert
            Assert.That(10, Is.EqualTo(bom.Components.Count), "Returns the count of components");
            Assert.That(6, Is.EqualTo(bom.Dependencies.Count), "Returns the count of dependencies");

        }

        [Test]
        public void ParsePackageFile_PackageLockWithoutDuplicateComponents_ReturnsCountZeroDuplicates()
        {
            //Arrange
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string filepath = outFolder + @"\PackageIdentifierUTTestFiles\TestDir\DupDir";
            string[] Includes = { "p*-lock.json" };
            string[] Excludes = { "node_modules" };
            BomKpiData bomKpiData = new BomKpiData();
            CommonAppSettings appSettings = new CommonAppSettings()
            {
                BomFolderPath= outFolder,
                PackageFilePath = filepath,
                ProjectType = "NPM",
                RemoveDevDependency = true,
                Npm = new Config() { Include = Includes, Exclude = Excludes }


            };

            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();
            NpmProcessor NpmProcessor = new NpmProcessor(cycloneDXBomParser.Object);

            //Act
            NpmProcessor.ParsePackageFile(appSettings);

            //Assert
            Assert.That(0, Is.EqualTo(bomKpiData.DuplicateComponents), "Returns the count of duplicate components as zero");
        }

        [Test]
        public void ParseCycloneDXFile_GivenMultipleInputFiles_ReturnsCounts()
        {
            //Arrange
            int expectednoofcomponents = 5;
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();
            NpmProcessor npmProcessor = new NpmProcessor(cycloneDXBomParser.Object);
            string[] Includes = { "*_NPM.cdx.json" };
            CommonAppSettings appSettings = new CommonAppSettings()
            {
                BomFolderPath = outFolder,
                PackageFilePath = outFolder + @"\PackageIdentifierUTTestFiles",
                ProjectType = "NPM",
                RemoveDevDependency = true,
                Npm = new Config() { Include = Includes }
            };

            //Act
            Bom listofcomponents = npmProcessor.ParsePackageFile(appSettings);

            //Assert
            Assert.That(expectednoofcomponents, Is.EqualTo(listofcomponents.Components.Count), "Checks for no of components");
        }

        [Test]
        public void ParseCycloneDXFile_GivenAInputFilePathAlongWithSBOMTemplate_ReturnTotalComponentsList()
        {
            //Arrange
            int expectednoofcomponents = 3;
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();
            NpmProcessor npmProcessor = new NpmProcessor(cycloneDXBomParser.Object);
            string[] Includes = { "CycloneDX2_NPM.cdx.json", "SBOMTemplate_Npm.cdx.json" };
            string packagefilepath = outFolder + @"\PackageIdentifierUTTestFiles";

            CommonAppSettings appSettings = new CommonAppSettings()
            {
                BomFolderPath = outFolder,
                PackageFilePath = packagefilepath,
                ProjectType = "NPM",
                RemoveDevDependency = true,
                Npm = new Config() { Include = Includes },
                CycloneDxSBomTemplatePath = packagefilepath + "\\SBOMTemplates\\SBOM_NpmCATemplate.cdx.json"
            };

            //Act
            Bom listofcomponents = npmProcessor.ParsePackageFile(appSettings);

            //Assert
            Assert.That(expectednoofcomponents, Is.EqualTo(listofcomponents.Components.Count), "Checks for no of components");
        }

        [Test]
        public void ParseCycloneDXFile_GivenAInputFilePathAlongWithSBOMTemplate_ReturnUpdatedComponents()
        {
            //Arrange
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();
            NpmProcessor npmProcessor = new NpmProcessor(cycloneDXBomParser.Object);
            string[] Includes = { "CycloneDX2_NPM.cdx.json" };
            string packagefilepath = outFolder + @"\PackageIdentifierUTTestFiles";

            CommonAppSettings appSettings = new CommonAppSettings()
            {
                BomFolderPath = outFolder,
                PackageFilePath = packagefilepath,
                ProjectType = "NPM",
                RemoveDevDependency = true,
                Npm = new Config() { Include = Includes },
                CycloneDxSBomTemplatePath = packagefilepath + "\\SBOMTemplates\\SBOMTemplate_Npm.cdx.json"
            };

            //Act
            Bom listofcomponents = npmProcessor.ParsePackageFile(appSettings);

            bool isUpdated = listofcomponents.Components.Exists(x => x.Properties != null && x.Properties.Exists(x => x.Name == Dataconstant.Cdx_IdentifierType && x.Value == Dataconstant.ManullayAdded));

            //Assert
            Assert.IsTrue(isUpdated, "Checks For Updated Property In List ");
        }
    }
}
