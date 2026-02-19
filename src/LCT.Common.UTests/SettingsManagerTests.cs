// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using NUnit.Framework;
using System;
using System.IO;

namespace LCT.Common.UTest
{
    [TestFixture]
    internal class SettingsManagerTests
    {
        private SettingsManager _settingsManager;
        public SettingsManagerTests()
        {
            _settingsManager = new SettingsManager();
        }

        [SetUp]
        public void Setup()
        {
            _settingsManager = new SettingsManager();
        }

        [Test]
        public void ReadConfiguration_WhenArgsIsNull_ShouldThrowInvalidDataException()
        {
            // Arrange
            string[] args = null;
            string jsonSettingsFileName = "settings.json";
            EnvironmentHelper helper = new EnvironmentHelper();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _settingsManager.ReadConfiguration<object>(args, jsonSettingsFileName, helper));
        }

        [Test]
        public void IsAzureDevOpsDebugEnabled_WhenSystemDebugIsTrue_ShouldReturnTrue()
        {
            // Arrange
            Environment.SetEnvironmentVariable("System.Debug", "true");

            // Act
            bool result = SettingsManager.IsAzureDevOpsDebugEnabled();

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void IsAzureDevOpsDebugEnabled_WhenSystemDebugIsFalse_ShouldReturnFalse()
        {
            // Arrange
            Environment.SetEnvironmentVariable("System.Debug", "false");

            // Act
            bool result = SettingsManager.IsAzureDevOpsDebugEnabled();

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void IsAzureDevOpsDebugEnabled_WhenSystemDebugIsNotSet_ShouldReturnFalse()
        {
            // Arrange
            Environment.SetEnvironmentVariable("System.Debug", null);

            // Act
            bool result = SettingsManager.IsAzureDevOpsDebugEnabled();

            // Assert
            Assert.That(result, Is.False);
        }
        [Test]
        public void DisplayHelp_WhenFileExists_ShouldOutputFileContent()
        {
            // Arrange
            string testFilePath = "CLIUsageNpkg.txt";
            string expectedContent = "This is a test content for CLI usage.";
            File.WriteAllText(testFilePath, expectedContent);

            using StringWriter consoleOutput = new StringWriter();
            Console.SetOut(consoleOutput);

            // Act
            SettingsManager.DisplayHelp();

            // Assert
            string actualOutput = consoleOutput.ToString().Trim();
            Assert.That(actualOutput, Is.EqualTo(expectedContent));

            // Cleanup
            File.Delete(testFilePath);
        }

        [Test]
        public void DisplayHelp_WhenFileDoesNotExist_ShouldThrowFileNotFoundException()
        {
            // Arrange
            string testFilePath = "CLIUsageNpkg.txt";
            if (File.Exists(testFilePath))
            {
                File.Delete(testFilePath);
            }

            // Act & Assert
            Assert.Throws<FileNotFoundException>(() => SettingsManager.DisplayHelp());
        }

        [Test]
        public void ReadConfiguration_WhenInvalidJsonFormat_ShouldReturnDefault()
        {
            // Arrange
            string invalidJsonFile = "invalid-settings.json";
            File.WriteAllText(invalidJsonFile, "{invalid json content}");
            
            string[] args = new string[] { $"--settingsfilepath={invalidJsonFile}" };
            EnvironmentHelper helper = new EnvironmentHelper();

            // Act
            var result = _settingsManager.ReadConfiguration<CommonAppSettings>(args, invalidJsonFile, helper);

            // Assert
            Assert.That(result, Is.Null);

            // Cleanup
            if (File.Exists(invalidJsonFile))
            {
                File.Delete(invalidJsonFile);
            }
        }

        [Test]
        public void ReadConfiguration_WhenFormatExceptionOccurs_ShouldReturnDefault()
        {
            // Arrange
            string invalidFormatFile = "format-error-settings.json";
            File.WriteAllText(invalidFormatFile, "{\"key\": \"value with missing quote}");
            
            string[] args = new string[] { $"--settingsfilepath={invalidFormatFile}" };
            EnvironmentHelper helper = new EnvironmentHelper();

            // Act
            var result = _settingsManager.ReadConfiguration<CommonAppSettings>(args, invalidFormatFile, helper);

            // Assert
            Assert.That(result, Is.Null);

            // Cleanup
            if (File.Exists(invalidFormatFile))
            {
                File.Delete(invalidFormatFile);
            }
        }

        [Test]
        public void CheckRequiredArgsToRun_WhenIdentifierWithAllRequiredParameters_ShouldNotThrow()
        {
            // Arrange
            var appSettings = new CommonAppSettings
            {
                ProjectType = "NPM",
                Directory = new Directory
                {
                    InputFolder = "input",
                    OutputFolder = "output"
                }
            };

            // Act & Assert
            Assert.DoesNotThrow(() => _settingsManager.CheckRequiredArgsToRun(appSettings, "Identifier"));
        }

        [Test]
        public void CheckRequiredArgsToRun_WhenCreatorWithAllRequiredParameters_ShouldNotThrow()
        {
            // Arrange
            var appSettings = new CommonAppSettings
            {
                SW360 = new SW360
                {
                    ProjectID = "project123",
                    Token = "token123",
                    URL = "https://sw360.test"
                },
                Directory = new Directory
                {
                    OutputFolder = "output"
                }
            };

            // Act & Assert
            Assert.DoesNotThrow(() => _settingsManager.CheckRequiredArgsToRun(appSettings, "Creator"));
        }

        [Test]
        public void CheckRequiredArgsToRun_WhenUploaderWithAllRequiredParameters_ShouldNotThrow()
        {
            // Arrange
            var appSettings = new CommonAppSettings
            {
                Jfrog = new Jfrog
                {
                    URL = "https://jfrog.test",
                    Token = "token123"
                },
                Directory = new Directory
                {
                    OutputFolder = "output"
                }
            };

            // Act & Assert
            Assert.DoesNotThrow(() => _settingsManager.CheckRequiredArgsToRun(appSettings, "Uploader"));
        }
    }
}
