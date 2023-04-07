// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT

// --------------------------------------------------------------------------------------------------------------------

using CycloneDX.Models;
using LCT.Common;
using LCT.Common.Model;
using LCT.PackageIdentifier.Model;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;

namespace LCT.PackageIdentifier.UTest
{
    public class MavenParserTests
    {
        [Test]
        public void ParsePackageFile_PackageLockWithDuplicateComponents_ReturnsCountOfDuplicates()
        {
            //Arrange
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string filepath = outFolder + @"\PackageIdentifierUTTestFiles";
            string[] Includes = { "POM.xml" };
            string[] Excludes = { "lol" };

            CommonAppSettings appSettings = new CommonAppSettings()
            {
                PackageFilePath = filepath,
                ProjectType = "MAVEN",
                RemoveDevDependency = true,
                Maven = new Config() { Include = Includes, Exclude = Excludes }
            };

            MavenProcessor MavenProcessor = new MavenProcessor();

            //Act
            Bom bom = MavenProcessor.ParsePackageFile(appSettings);

            //Assert
            Assert.That(bom.Components.Count,Is.EqualTo(3), "Returns the count of components");

        }

        [TestCase]
        public void IsDevDependent_GivenListOfMavenDevComponents_ReturnsNonDevComponents()
        {
            //Arrange
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string filepath = outFolder + @"\PackageIdentifierUTTestFiles";
            string[] Includes = { "POM.xml" };
            string[] Excludes = { "lol" };

            CommonAppSettings appSettings = new CommonAppSettings()
            {
                PackageFilePath = filepath,
                ProjectType = "MAVEN",
                RemoveDevDependency = true,
                Maven = new Config() { Include = Includes, Exclude = Excludes, DevDependentScopeList = new string[] { "test" } }
            };

            MavenProcessor MavenProcessor = new MavenProcessor();

            //Act
            Bom bom = MavenProcessor.ParsePackageFile(appSettings);

            //Assert
            Assert.That(bom.Components.Count,Is.EqualTo(1), "Returns the count of NON Dev Dependency components");
        }
    }
}
