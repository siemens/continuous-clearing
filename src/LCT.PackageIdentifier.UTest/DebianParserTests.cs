// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT

// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models;
using LCT.PackageIdentifier;
using NUnit.Framework;
using System.IO;
using LCT.Common;
using LCT.Common.Model;
using LCT.APICommunications.Model;
using System.Threading.Tasks;

namespace PackageIdentifier.UTest
{
    [TestFixture]
    class DebianParserTests
    {
        [Test]
        public void ParsePackageConfig_GivenAInputFilePath_ReturnsCounts()
        {
            //Arrange
            int expectednoofcomponents = 9;
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string OutFolder = Path.GetDirectoryName(exePath);
            DebianProcessor DebianProcessor = new DebianProcessor();
            string[] Includes = { "*.json" };
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
        public void ParsePackageConfig_GivenAInputFilePath_ReturnsCountsAsZero()
        {
            //Arrange
            int expectednoofcomponents = 0;
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string OutFolder = Path.GetDirectoryName(exePath);
            DebianProcessor DebianProcessor = new DebianProcessor();
            string[] Includes = { "Cyclonedx1.json" };
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
        public void ParsePackageConfig_GivenMultipleInputFiles_ReturnsCounts()
        {
            //Arrange
            int expectednoofcomponents = 8;
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string OutFolder = Path.GetDirectoryName(exePath);
            DebianProcessor DebianProcessor = new DebianProcessor();
            string[] Includes = { "Cyclone*.json" };
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
            DebianProcessor DebianProcessor = new DebianProcessor();
            string[] Includes = { "Cyclone*.json" };
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
            DebianProcessor DebianProcessor = new DebianProcessor();
            string[] Includes = { "SourceDetails_Cyclonedx.json" };
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
            Assert.AreEqual(sourceName, listofcomponents.Components[0].Name + "_" + listofcomponents.Components[0].Version, "Checks componet name and version");
        }

        [Test]
        public async Task CheckInternalComponentsInJfrogArtifactory_GivenRepoDeatils_ReturnInternalList()
        {
            //Arrange
            CommonAppSettings appSettings = new CommonAppSettings()
            {

            };
            ArtifactoryCredentials artifactoryUpload = new ArtifactoryCredentials();
            Component component = new Component();
            string repo = string.Empty;
            DebianProcessor DebianProcessor = new DebianProcessor();

            //Act
            var components = await DebianProcessor.CheckInternalComponentsInJfrogArtifactory(appSettings, artifactoryUpload, component, repo);

            //Assert
            Assert.AreEqual(1, components.Count, "Internal Component found");
        }

        [Test]
        public void ParsePackageConfig_GivenXMLInputFilePath_ReturnsNoComponents()
        {
            //Arrange
            string filePath = $"{Path.GetTempPath()}\\OutFiles";
            Directory.CreateDirectory(filePath);
            File.WriteAllText(filePath + "\\output.xml", "<components><component></component></components>");

            DebianProcessor DebianProcessor = new DebianProcessor();
            string[] Includes = { "output.xml" };
            CommonAppSettings appSettings = new CommonAppSettings()
            {
                PackageFilePath = filePath,
                ProjectType = "DEBIAN",
                RemoveDevDependency = true,
                Debian = new Config() { Include = Includes }
            };

            //Act
            Bom listofcomponents = DebianProcessor.ParsePackageFile(appSettings);

            //Assert
            Assert.AreEqual(0, listofcomponents.Components.Count, "Return Zero Components");
        }

    }
}
