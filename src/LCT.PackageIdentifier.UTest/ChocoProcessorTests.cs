// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// --------------------------------------------------------------------------------------------------------------------
using CycloneDX.Models;
using LCT.Common;
using LCT.Common.Interface;
using LCT.Common.Model;
using LCT.PackageIdentifier;
using LCT.PackageIdentifier.Model;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LCT.PackageIdentifier.Tests
{
    [TestFixture]
    public class ChocoProcessorTests
    {
        private Mock<ICycloneDXBomParser> _cycloneDXBomParserMock;
        private Mock<ISpdxBomParser> _spdxBomParserMock;
        private ChocoProcessor _chocoProcessor;
        private CommonAppSettings _appSettings;
        private Bom _unsupportedBomList;
        private string _testInputFolder;

        [SetUp]
        public void SetUp()
        {
            _cycloneDXBomParserMock = new Mock<ICycloneDXBomParser>();
            _spdxBomParserMock = new Mock<ISpdxBomParser>();
            _chocoProcessor = new ChocoProcessor(_cycloneDXBomParserMock.Object, _spdxBomParserMock.Object);

            // Setup a valid test input folder
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            _testInputFolder = Path.Combine(outFolder, "PackageIdentifierUTTestFiles");
            if (!System.IO.Directory.Exists(_testInputFolder))
            {
                System.IO.Directory.CreateDirectory(_testInputFolder);
            }

            _appSettings = new CommonAppSettings
            {
                Directory = new LCT.Common.Directory
                {
                    InputFolder = _testInputFolder
                },
                Choco = new LCT.Common.Model.Config
                {
                    Include = new[] { "choco.config" }, // Adjust pattern as needed
                    Exclude = new string[0]
                }
            };
            _unsupportedBomList = new Bom();
        }

        [Test]
        public void ParsePackageFile_ReturnsBomWithComponents_WhenConfigFilesExist()
        {
            // Arrange
            var configFiles = new List<string> { "choco.config", ".choco.config" };
            var nugetPackages1 = new List<NugetPackage> { new NugetPackage() };
            var nugetPackages2 = new List<NugetPackage> { new NugetPackage() };

            // Mock FolderScanner.FileScanner
            FolderScanner.FileScanner = (inputFolder, config) => configFiles;

            // Mock ParsePackageConfig
            NugetProcessor.ParsePackageConfig = (filePath, appSettings) =>
                filePath == "choco.config" ? nugetPackages1 : nugetPackages2;

            // Mock ConvertToCycloneDXModel
            NugetProcessor.ConvertToCycloneDXModel = (components, packages, dependencies) =>
            {
                components.Add(new Component { Name = "Choco" });
            };

            // Act
            var result = _chocoProcessor.ParsePackageFile(_appSettings, ref _unsupportedBomList);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Components);
            Assert.GreaterOrEqual(result.Components.Count, 1);
            Assert.IsTrue(result.Components.Any(c => c.Name == "om-testmodules"));
            Assert.IsNotNull(result.Dependencies);
            Assert.AreEqual(0, result.Dependencies.Count);
        }
    }

    // Static delegates for mocking static methods
    public static class FolderScanner
    {
        public static System.Func<object, object, List<string>> FileScanner { get; internal set; }
    }
    public static class NugetProcessor
    {
        public static Func<string, CommonAppSettings, List<NugetPackage>> ParsePackageConfig;
        public static Action<List<Component>, List<NugetPackage>, List<Dependency>> ConvertToCycloneDXModel;
    }
}



