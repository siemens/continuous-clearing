// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using NUnit.Framework;
using System;

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
    }
}
