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

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _settingsManager.ReadConfiguration<object>(args, jsonSettingsFileName));
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
    }
}
