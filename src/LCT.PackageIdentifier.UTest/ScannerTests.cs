// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT

// -------------------------------------------------------------------------------------------------------------------- 

using LCT.PackageIdentifier;
using LCT.PackageIdentifier.Model;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using LCT.Common.Model;

namespace PackageIdentifier.UTest
{
    [TestFixture]
    public class ScannerTests
    {

        [Test]
        public void FolderScanner_GivenARootPath_ReturnsPackages()
        {
            //Arrange
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string filepath = outFolder + @"\PackageIdentifierUTTestFiles\TestDir";
            string[] Includes = { "p*-lock.json" };
            string[] Excludes = { "node_modules" };
            Config config = new Config()
            {
                Include = Includes,
                Exclude = Excludes

            };

            //Act
            List<string> foundfiles = FolderScanner.FileScanner(filepath, config);

            //Assert
            Assert.That(3, Is.EqualTo(foundfiles.Count), "Returns 3 package-lock.json files");

        }
        [Test]
        public void FolderScanner_GivenRootPathAsEmpty_ReturnsArgumentException()
        {
            //Arrange

            string filepath = "";
            string[] Includes = { "p*-lock.json" };
            string[] Excludes = { "node_modules" };
            Config config = new Config()
            {
                Include = Includes,
                Exclude = Excludes

            };

            //Assert
            Assert.Throws(typeof(ArgumentException), () => FolderScanner.FileScanner(filepath, config));

        }
        [Test]
        public void FolderScanner_GivenWrongRootPath_ReturnsDirectoryNotFoundException()
        {
            //Arrange

            string filepath = "test/empty";
            string[] Includes = { "p*-lock.json" };
            string[] Excludes = { "node_modules" };
            Config config = new Config()
            {
                Include = Includes,
                Exclude = Excludes

            };

            //Assert
            Assert.Throws(typeof(DirectoryNotFoundException), () => FolderScanner.FileScanner(filepath, config));

        }
        [Test]
        public void FolderScanner_GivenConfigAsNull_ReturnsNullException()
        {
            //Arrange
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string filepath = outFolder + @"\PackageIdentifierUTTestFiles\TestDir";
            Config config = new Config()
            {
                Include = null,
                Exclude = null

            };

            //Assert
            Assert.Throws(typeof(ArgumentNullException), () => FolderScanner.FileScanner(filepath, config));

        }


    }
}
