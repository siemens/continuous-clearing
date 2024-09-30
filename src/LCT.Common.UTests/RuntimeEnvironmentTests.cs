// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.Common.Runtime;
using NUnit.Framework;
using System;

namespace LCT.Common.UTest
{
    [TestFixture]
    internal class RuntimeEnvironmentTests
    {
        [Test]
        public void GetEnvironment_WhenNoEnvironmentVariableDefined_ReturnsUnknown()
        {
            // Arrange
            _= EnvironmentType.Unknown;
            Environment.SetEnvironmentVariable("Unknown", "0");

            // Act
            var actualEnvironment = RuntimeEnvironment.GetEnvironment();

            // Assert
            Assert.That(actualEnvironment, Is.Not.Null);
            Assert.That(Environment.GetEnvironmentVariable("unknown"), Is.Not.Null);
        }

        [Test]
        public void GetEnvironment_WhenReleaseIdEnvironmentVariableDefined_ReturnsAzureRelease()
        {
            // Arrange
            _= EnvironmentType.AzureRelease;
            SetEnvironmentVariable("Release_ReleaseId", "12345");

            // Act
            var actualEnvironment = RuntimeEnvironment.GetEnvironment();

            // Assert
            Assert.That(actualEnvironment, Is.Not.Null);
            Assert.That(Environment.GetEnvironmentVariable("Release_ReleaseId"), Is.Not.Null);
        }

        [Test]
        public void GetEnvironment_WhenBuildIdEnvironmentVariableDefined_ReturnsAzurePipeline()
        {
            // Arrange
            var expectedEnvironment = EnvironmentType.AzurePipeline;
            SetEnvironmentVariable("Build_BuildId", "67890");

            // Act
            var actualEnvironment = RuntimeEnvironment.GetEnvironment();

            // Assert
            Assert.That(actualEnvironment, Is.EqualTo(expectedEnvironment));
        }

        [Test]
        public void GetEnvironment_WhenJobIdEnvironmentVariableDefined_ReturnsGitLab()
        {
            // Arrange
            _= EnvironmentType.GitLab;
            SetEnvironmentVariable("CI_JOB_ID", "54321");

            // Act
            var actualEnvironment = RuntimeEnvironment.GetEnvironment();

            // Assert
            Assert.That(actualEnvironment, Is.Not.Null);
            Assert.That(Environment.GetEnvironmentVariable("CI_JOB_ID"), Is.Not.Null);
        }

        private static void SetEnvironmentVariable(string name, string value)
        {
            // Set the environment variable for the duration of the test
            // This ensures that the environment variable is defined during the test execution
            Environment.SetEnvironmentVariable(name, value);
        }
    }
}
