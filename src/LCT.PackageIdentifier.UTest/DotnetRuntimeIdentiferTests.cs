// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using NUnit.Framework;
using LCT.PackageIdentifier.Model;
using LCT.Common;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Locator;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace LCT.PackageIdentifier.UTest
{
    [TestFixture]
    public class DotnetRuntimeIdentiferTests
    {
        private DotnetRuntimeIdentifer _identifier;
        private CommonAppSettings _appSettings;
        private string _testDir;
        private string _assetsFilePath;
        private string _csprojFilePath;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            if (!MSBuildLocator.IsRegistered)
            {
                MSBuildLocator.RegisterDefaults();
            }
        }

        [SetUp]
        public void Setup()
        {
            _identifier = new DotnetRuntimeIdentifer();
            _testDir = Path.Combine(TestContext.CurrentContext.TestDirectory, "PackageIdentifierUTTestFiles");
            _appSettings = new CommonAppSettings
            {
                Directory = new LCT.Common.Directory()
                {
                    InputFolder = _testDir
                }
            };
            _assetsFilePath = Path.Combine(_testDir, "project.assets.json");
            _csprojFilePath = Path.Combine(_testDir, "Nuget.csproj");
        }

        [Test]
        public void Register_RegistersMSBuildLocatorWithoutError()
        {
            Assert.DoesNotThrow(() => _identifier.Register());
        }

        [Test]
        public void LoadProject_ReturnsProjectInstance()
        {
            var globalProps = new Dictionary<string, string> { { "Configuration", "Release" } };
            using var collection = new ProjectCollection();
            var project = DotnetRuntimeIdentifer.LoadProject(_csprojFilePath, globalProps, collection);
            Assert.IsNotNull(project);
            Assert.AreEqual(_csprojFilePath, project.FullPath);
        }

        [Test]
        public void IdentifyRuntime_ReturnsError_WhenNoAssetsFilesFound()
        {
            // Create an empty directory for the test
            var emptyDir = Path.Combine(_testDir, "EmptyDir");
            System.IO.Directory.CreateDirectory(emptyDir);

            var appSettings = new CommonAppSettings
            {
                Directory = new LCT.Common.Directory()
                {
                    InputFolder = emptyDir
                }
            };

            var result = _identifier.IdentifyRuntime(appSettings);
            Assert.IsFalse(string.IsNullOrEmpty(result.ErrorMessage));
            Assert.That(result.ErrorMessage, Does.Contain("No project.assets.json files found"));
        }

        [Test]
        public void IdentifyRuntime_ReturnsError_WhenProjectContainsInvalidMSBuildContent()
        {
            // Create a directory with valid assets file but invalid csproj
            string invalidBuildDir = Path.Combine(_testDir, "InvalidBuild");
            System.IO.Directory.CreateDirectory(invalidBuildDir);
            string assetsFile = Path.Combine(invalidBuildDir, "project.assets.json");
            string invalidCsproj = Path.Combine(invalidBuildDir, "Invalid.csproj");

            // Copy the real assets file but modify it to point to our invalid project
            string json = File.ReadAllText(_assetsFilePath);
            json = json.Replace(
                    "\"projectPath\": \"\"",
                    $"\"projectPath\": \"{invalidCsproj.Replace("\\", "\\\\")}\"");

            File.WriteAllText(assetsFile, json);

            // Create a csproj file with invalid MSBuild syntax
            File.WriteAllText(invalidCsproj,
                "<Project Sdk=\"Microsoft.NET.Sdk\">\n" +
                "  <PropertyGroup>\n" +
                "    <TargetFramework>net8.0</TargetFramework>\n" +
                "    <InvalidProperty><<>>ThisIsInvalid</InvalidProperty>\n" + // Invalid XML syntax
                "  </PropertyGroup>\n" +
                "</Project>");

            var testAppSettings = new CommonAppSettings
            {
                Directory = new LCT.Common.Directory()
                {
                    InputFolder = invalidBuildDir
                }
            };

            var result = _identifier.IdentifyRuntime(testAppSettings);

            Assert.IsFalse(string.IsNullOrEmpty(result.ErrorMessage));
            Assert.That(result.ErrorMessage + result.ErrorDetails, Does.Contain("Error"));

            // Clean up
            System.IO.Directory.Delete(invalidBuildDir, true);
        }

        [Test]
        public void IdentifyRuntime_ReturnsValidRuntimeInfo_WhenAssetsFilePresent()
        {
            // Create a copy of the original assets file
            string tempDir = Path.Combine(_testDir, "Temp");
            System.IO.Directory.CreateDirectory(tempDir);
            string modifiedAssetsFile = Path.Combine(tempDir, "project.assets.json");
            File.Copy(_assetsFilePath, modifiedAssetsFile, true);

            try
            {
                // Modify the project path in the JSON file
                // We need to use a valid NuGet-parsable format
                string json = File.ReadAllText(modifiedAssetsFile);

                // Replace the project path in the JSON
                // Since we can't directly modify LockFile.PackageSpec.RestoreMetadata.ProjectPath after reading,
                // we need to update the JSON before NuGet reads it
                json = json.Replace(
                        "\"projectPath\": \"\"",
                        $"\"projectPath\": \"{_csprojFilePath.Replace("\\", "\\\\")}\"");

                File.WriteAllText(modifiedAssetsFile, json);

                // Ensure the referenced csproj file exists
                if (!File.Exists(_csprojFilePath))
                {
                    // Create a minimal csproj file for testing
                    string csprojDir = Path.GetDirectoryName(_csprojFilePath);
                    System.IO.Directory.CreateDirectory(csprojDir);
                    File.WriteAllText(_csprojFilePath,
                        "<Project Sdk=\"Microsoft.NET.Sdk\">\n" +
                        "  <PropertyGroup>\n" +
                        "    <TargetFramework>net8.0</TargetFramework>\n" +
                        "  </PropertyGroup>\n" +
                        "</Project>");
                }

                // Use a settings object that points to our modified directory
                var testAppSettings = new CommonAppSettings
                {
                    Directory = new LCT.Common.Directory()
                    {
                        InputFolder = tempDir
                    }
                };

                // Run the actual test
                var result = _identifier.IdentifyRuntime(testAppSettings);

                // Verify the results
                Assert.IsTrue(string.IsNullOrEmpty(result.ErrorMessage),
                    $"Unexpected error: {result.ErrorMessage} {result.ErrorDetails}");
                Assert.IsNotNull(result.ProjectName);
                Assert.AreEqual(Path.GetFileNameWithoutExtension(_csprojFilePath), result.ProjectName);
                Assert.AreEqual(_csprojFilePath, result.ProjectPath);
            }
            finally
            {
                // Clean up
                System.IO.Directory.Delete(tempDir, true);
            }
        }

        [Test]
        public void FindAssetFiles_ReturnsFiles_WhenAssetsFileExists()
        {
            var method = typeof(DotnetRuntimeIdentifer).GetMethod("FindAssetFiles", BindingFlags.NonPublic | BindingFlags.Static);
            var info = new RuntimeInfo();
            var files = (string[])method.Invoke(null, new object[] { _appSettings, info });
            Assert.IsNotEmpty(files);
            Assert.IsTrue(files[0].EndsWith("project.assets.json"));
        }

        [Test]
        public void IsExcluded_ReturnsTrue_WhenPatternMatches()
        {
            var method = typeof(DotnetRuntimeIdentifer).GetMethod("IsExcluded", BindingFlags.NonPublic | BindingFlags.Static);
            string filePath = Path.Combine(_testDir, "project.assets.json");
            string[] patterns = { "project.assets.json" };
            bool result = (bool)method.Invoke(null, new object[] { filePath, patterns });
            Assert.IsTrue(result);
        }

        [Test]
        public void IsExcluded_ReturnsFalse_WhenNoPatternMatches()
        {
            var method = typeof(DotnetRuntimeIdentifer).GetMethod("IsExcluded", BindingFlags.NonPublic | BindingFlags.Static);
            string filePath = Path.Combine(_testDir, "other.json");
            string[] patterns = { "project.assets.json" };
            bool result = (bool)method.Invoke(null, new object[] { filePath, patterns });
            Assert.IsFalse(result);
        }

        [Test]
        public void GetProjectFilePathFromAssestJson_ReturnsNull_WhenFileInvalid()
        {
            var method = typeof(DotnetRuntimeIdentifer).GetMethod("GetProjectFilePathFromAssestJson", BindingFlags.NonPublic | BindingFlags.Static);
            string filePath = Path.Combine(_testDir, "invalid.assets.json");
            File.WriteAllText(filePath, "{}\n");
            string result = (string)method.Invoke(null, new object[] { filePath });
            Assert.IsNull(result);
            File.Delete(filePath);
        }
    }
}
