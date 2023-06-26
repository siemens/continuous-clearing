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

namespace PackageIdentifier.UTest
{
    [TestFixture]
    class PythonParserTests
    {
        readonly PythonProcessor pythonProcessor;
        public PythonParserTests()
        {
            pythonProcessor = new PythonProcessor();
        }
        [Test]
        public void ParsePackageConfig_GivenAMultipleInputFilePath_ReturnsCounts()
        {
            //Arrange
            int expectednoofcomponents = 9;
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string OutFolder = Path.GetDirectoryName(exePath);
            string[] Includes = { "*_Python.cdx.json" };
            CommonAppSettings appSettings = new CommonAppSettings()
            {
                ProjectType = "PYTHON",
                RemoveDevDependency = true,
                Python = new Config() { Include = Includes },
                PackageFilePath = OutFolder + @"\PackageIdentifierUTTestFiles"
            };

            //Act
            Bom listofcomponents = pythonProcessor.ParsePackageFile(appSettings);

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
            string[] Includes = { "CycloneDX_Python.cdx.json" };
            CommonAppSettings appSettings = new CommonAppSettings()
            {
                PackageFilePath = OutFolder + @"\PackageIdentifierUTTestFiles",
                ProjectType = "PYTHON",
                RemoveDevDependency = true,
                Python = new Config() { Include = Includes }
            };

            //Act
            Bom listofcomponents = pythonProcessor.ParsePackageFile(appSettings);

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
            string[] Includes = { "*_Python.cdx.json" };
            CommonAppSettings appSettings = new CommonAppSettings()
            {
                PackageFilePath = OutFolder + @"\PackageIdentifierUTTestFiles",
                ProjectType = "PYTHON",
                RemoveDevDependency = true,
                Python = new Config() { Include = Includes }
            };

            //Act
            pythonProcessor.ParsePackageFile(appSettings);

            //Assert
            Assert.That(duplicateComponents, Is.EqualTo(BomCreator.bomKpiData.DuplicateComponents), "Checks for no of duplicate components");
        }
    }
}

