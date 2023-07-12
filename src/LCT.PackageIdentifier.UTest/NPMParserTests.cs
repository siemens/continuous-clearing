// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
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
                PackageFilePath = filepath,
                ProjectType = "NPM",
                RemoveDevDependency = true,
                Npm = new Config() { Include = Includes, Exclude = Excludes }
            };

            NpmProcessor NpmProcessor = new NpmProcessor();

            //Act
            NpmProcessor.ParsePackageFile(appSettings);

            //Assert
            Assert.That(2974, Is.EqualTo(BomCreator.bomKpiData.DuplicateComponents), "Returns the count of duplicate components");

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
                PackageFilePath = filepath,
                ProjectType = "NPM",
                RemoveDevDependency = true,
                Npm = new Config() { Include = Includes, Exclude = Excludes }


            };
            NpmProcessor NpmProcessor = new NpmProcessor();

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
            string OutFolder = Path.GetDirectoryName(exePath);
            NpmProcessor npmProcessor = new NpmProcessor();
            string[] Includes = { "*_NPM.cdx.json" };
            CommonAppSettings appSettings = new CommonAppSettings()
            {
                PackageFilePath = OutFolder + @"\PackageIdentifierUTTestFiles",
                ProjectType = "NPM",
                RemoveDevDependency = true,
                Npm = new Config() { Include = Includes }
            };

            //Act
            Bom listofcomponents = npmProcessor.ParsePackageFile(appSettings);

            //Assert
            Assert.That(expectednoofcomponents, Is.EqualTo(listofcomponents.Components.Count), "Checks for no of components");
        }
    }
}
