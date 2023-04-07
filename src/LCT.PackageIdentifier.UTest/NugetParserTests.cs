﻿// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT

// -------------------------------------------------------------------------------------------------------------------- 


using LCT.PackageIdentifier.Model;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using LCT.Common;
using LCT.Common.Model;
using System.Security;
using LCT.PackageIdentifier;

namespace PackageIdentifier.UTest
{
    [TestFixture]
    public class NugetParserTests
    {

        [TestCase]
        public void ParsePackageConfig_GivenAInputFilePath_ReturnsSuccess()
        {
            //Arrange
            int expectednoofcomponents = 7;
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string packagefilepath = outFolder + @"\PackageIdentifierUTTestFiles\packages.config";


            CommonAppSettings appSettings = new CommonAppSettings()
            {
                PackageFilePath = outFolder + @"\PackageIdentifierUTTestFiles"
            };

            //Act
            List<NugetPackage> listofcomponents = NugetProcessor.ParsePackageConfig(packagefilepath, appSettings);

            //Assert
            Assert.That(expectednoofcomponents, Is.EqualTo(listofcomponents.Count), "Checks for no of components");

        }

        [TestCase]
        public void ParsePackageLockJson_GivenAInputFilePath_ReturnsSuccess()
        {
            //Arrange
            int expectednoofcomponents = 152;
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string packagefilepath = outFolder + @"\PackageIdentifierUTTestFiles\packages.lock.json";
            string csprojPath = outFolder + @"\PackageIdentifierUTTestFiles";


            CommonAppSettings appSettings = new CommonAppSettings()
            {
                PackageFilePath = csprojPath
            };

            //Act
            List<NugetPackage> listofcomponents = NugetProcessor.ParsePackageLock(packagefilepath, appSettings);

            //Assert
            Assert.That(expectednoofcomponents, Is.EqualTo(listofcomponents.Count), "Checks for no of components");

        }
        [TestCase]
        public void InputFileIdentifaction_GivenARootPath_ReturnsSuccess()
        {
            //Arrange
            int fileCount = 2;
            string[] Includes = { "p*.config", "p*.lock.json" };
            Config config = new Config()
            {
                Include = Includes,
                Exclude = null

            };
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string folderfilepath = outFolder + @"\PackageIdentifierUTTestFiles";

            //Act
            List<string> allFoundConfigFiles = FolderScanner.FileScanner(folderfilepath, config);


            //Assert
            Assert.That(fileCount, Is.EqualTo(allFoundConfigFiles.Count), "Checks for total inout files found");

        }
        [TestCase]
        public void InputFileIdentifaction_GivenIncludeFileAsNull_ReturnsArgumentNullException()
        {
            //Arrange
            string[] Includes = null;
            Config config = new Config()
            {
                Include = Includes,
                Exclude = null

            };
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string folderfilepath = outFolder + @"\PackageIdentifierUTTestFiles";

            //Act & Assert
            Assert.Throws(typeof(ArgumentNullException), () => FolderScanner.FileScanner(folderfilepath, config));

        }
        [TestCase]
        public void InputFileIdentifaction_GivenInputFileAsNull_ReturnsArgumentNullException()
        {
            //Arrange
            string[] Includes = { "p*.config", "p*.lock.json" };
            Config config = new Config()
            {
                Include = Includes,
                Exclude = null

            };
            string folderfilepath = "";


            //Act & Assert
            Assert.Throws(typeof(ArgumentException), () => FolderScanner.FileScanner(folderfilepath, config));

        }
        [TestCase]
        public void InputFileIdentifaction_GivenInvalidInputFile_ReturnsArgumentNullException()
        {
            //Arrange
            string[] Includes = { "p*.config", "p*.lock.json" };
            Config config = new Config()
            {
                Include = Includes,
                Exclude = null

            };
            string folderfilepath = @"../PackageIdentifierUTTestFiles";

            //Act & Assert
            Assert.Throws(typeof(DirectoryNotFoundException), () => FolderScanner.FileScanner(folderfilepath, config));

        }
        [TestCase]
        public void IsDevDependent_GivenListOfDevComponents_ReturnsSuccess()
        {
            //Arrange
            List<ReferenceDetails> referenceDetails = new List<ReferenceDetails>()
            {
             new ReferenceDetails() { Library = "SCL.Library", Version = "3.1.2", Private = true } };

            //Act
            bool actual = NugetProcessor.IsDevDependent(referenceDetails, "SCL.Library", "3.1.2");

            //Assert
            Assert.That(true, Is.EqualTo(actual), "Component is dev dependent");
        }

        [TestCase]
        public void Parsecsproj_GivenXMLFile_ReturnsSuccess()
        {
            //Arrange
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string csprojfilepath = outFolder + @"\PackageIdentifierUTTestFiles";
            string[] Excludes = null;
            CommonAppSettings CommonAppSettings = new CommonAppSettings()
            {
                PackageFilePath = csprojfilepath,
                Nuget = new Config() { Exclude = Excludes }
            };
            int devDependent = 0;

            //Act
            List<ReferenceDetails> referenceDetails = NugetProcessor.Parsecsproj(CommonAppSettings);
            foreach (var item in referenceDetails)
            {
                if (item.Private)
                {
                    devDependent++;
                }
            }

            //Assert
            Assert.That(1, Is.EqualTo(devDependent), "Checks for total dev dependent components found");
        }

        [TestCase]
        public void RemoveExcludedComponents_ReturnsUpdatedBom()
        {
            //Arrange
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string csprojfilepath = outFolder + @"\PackageIdentifierUTTestFiles";
            string[] Excludes = null;

            CycloneDX.Models.Bom bom = new CycloneDX.Models.Bom();
            bom.Components = new List<CycloneDX.Models.Component>();
            CommonAppSettings CommonAppSettings = new CommonAppSettings()
            {
                PackageFilePath = csprojfilepath,
                Nuget = new Config() { Exclude = Excludes ,ExcludedComponents =new List<string>()}
            };

            //Act
            CycloneDX.Models.Bom updatedBom = NugetProcessor.RemoveExcludedComponents(CommonAppSettings, bom);

            //Assert
            Assert.AreEqual(0, updatedBom.Components.Count, "Zero component exculded");

        }
    }
}
