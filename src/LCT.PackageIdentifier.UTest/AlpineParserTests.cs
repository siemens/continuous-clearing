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
using System.Collections.Generic;

namespace LCT.PackageIdentifier.UTest
{
    [TestFixture]
    public class AlpineParserTests
    {
        private readonly AlpineProcessor _alpineProcessor;

        public AlpineParserTests()
        {
            List<Component> components = new List<Component>();
            components.Add(new Component() { Name = "apk-tools", Version = "2.12.9-r3", Purl = "pkg:apk/alpine/apk-tools@2.12.9-r3?arch=x86_64&distro=alpine-3.16.2" });
            components.Add(new Component() { Name = "busybox", Version = "1.35.0-r17", Purl = "pkg:apk/alpine/busybox@1.35.0-r17?arch=x86_64&distro=alpine-3.16.2" });
            Bom bom = new() { Components = components };

            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();
            cycloneDXBomParser.Setup(x => x.ParseCycloneDXBom(It.IsAny<string>())).Returns(bom);

            _alpineProcessor = new AlpineProcessor(cycloneDXBomParser.Object);
        }

        [Test]
        public void ParsePackageConfig_GivenAMultipleInputFilePath_ReturnsCounts()
        {
            //Arrange
            int expectednoofcomponents = 2;
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string OutFolder = Path.GetDirectoryName(exePath);
                        
            string[] Includes = { "*_Alpine.cdx.json" };
            CommonAppSettings appSettings = new CommonAppSettings()
            {
                ProjectType = "ALPINE",
                RemoveDevDependency = true,
                Alpine = new Config() { Include = Includes },
                PackageFilePath = OutFolder + @"\PackageIdentifierUTTestFiles"
            };

            //Act
            Bom listofcomponents = _alpineProcessor.ParsePackageFile(appSettings);

            //Assert
            Assert.That(expectednoofcomponents, Is.EqualTo(listofcomponents.Components.Count),
                "Checks for no of components");
        }


        [Test]
        public void ParsePackageConfig_GivenAInputFilePath_ReturnsCounts()
        {
            //Arrange
            int expectednoofcomponents = 2;
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string OutFolder = Path.GetDirectoryName(exePath);
            string[] Includes = { "CycloneDX_Alpine.cdx.json" };
            CommonAppSettings appSettings = new CommonAppSettings()
            {
                PackageFilePath = OutFolder + @"\PackageIdentifierUTTestFiles",
                ProjectType = "ALPINE",
                RemoveDevDependency = true,
                Alpine = new Config() { Include = Includes }
            };

            //Act
            Bom listofcomponents = _alpineProcessor.ParsePackageFile(appSettings);

            //Assert
            Assert.That(expectednoofcomponents, Is.EqualTo(listofcomponents.Components.Count), 
                "Checks for no of components");
        }

        [Test]
        public void ParsePackageConfig_GivenMultipleInputFiles_ReturnsCountOfDuplicates()
        {
            //Arrange
            int duplicateComponents = 2;
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string OutFolder = Path.GetDirectoryName(exePath);
  
            string[] Includes = { "*_Alpine.cdx.json" };

            CommonAppSettings appSettings = new CommonAppSettings()
            {
                PackageFilePath = OutFolder + @"\PackageIdentifierUTTestFiles",
                ProjectType = "ALPINE",
                RemoveDevDependency = true,
                Alpine = new Config() { Include = Includes }
            };

            //Act
            _alpineProcessor.ParsePackageFile(appSettings);

            //Assert
            Assert.That(duplicateComponents, Is.EqualTo(BomCreator.bomKpiData.DuplicateComponents),
                "Checks for no of duplicate components");
        }

        [Test]
        public void ParsePackageConfig_GivenAInputFilePath_ReturnsSourceDetails()
        {
            //Arrange
            string sourceName =@"apk-tools_2.12.9-r3";
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string OutFolder = Path.GetDirectoryName(exePath);
            string[] Includes = { "AlpineSourceDetails_Cyclonedx.cdx.json" };

            CommonAppSettings appSettings = new CommonAppSettings()
            {
                PackageFilePath = OutFolder + @"\PackageIdentifierUTTestFiles",
                ProjectType = "ALPINE",
                RemoveDevDependency = true,
                Alpine = new Config() { Include = Includes }
            };

            //Act
            Bom listofcomponents = _alpineProcessor.ParsePackageFile(appSettings);

            //Assert
            Assert.AreEqual(sourceName, listofcomponents.Components[0].Name + "_" +
                listofcomponents.Components[0].Version, "Checks component name and version");
        }

        [Test]
        public void ParsePackageConfig_GivenAInputFilePathAlongWithSBOMTemplate_ReturnTotalComponentsList()
        {
            //Arrange
            int expectednoofcomponents = 2;
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string OutFolder = Path.GetDirectoryName(exePath);
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
            Bom listofcomponents = _alpineProcessor.ParsePackageFile(appSettings);

            //Assert
            Assert.That(expectednoofcomponents, Is.EqualTo(listofcomponents.Components.Count),
                "Checks for no of components");
        }

        [Test]
        public void ParsePackageConfig_GivenAInputFilePathAlongWithSBOMTemplate_ReturnUpdatedComponents()
        {
            //Arrange
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string OutFolder = Path.GetDirectoryName(exePath);
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
            Bom listofcomponents = _alpineProcessor.ParsePackageFile(appSettings);
            bool isUpdated = listofcomponents.Components.Exists(x => x.Properties != null
            && x.Properties.Exists(x => x.Name == Dataconstant.Cdx_IdentifierType 
            && x.Value == Dataconstant.Discovered));

            //Assert
            Assert.IsTrue(isUpdated, "Checks For Updated Property In List ");
        }
    }
}
