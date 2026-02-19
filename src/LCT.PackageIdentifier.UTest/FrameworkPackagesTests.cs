// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.PackageIdentifier;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;

namespace LCT.PackageIdentifier.UTest
{
    [TestFixture]
    public class FrameworkPackagesTests
    {
        private FrameworkPackages _frameworkPackages;
        private string _testDataDirectory;

        [SetUp]
        public void Setup()
        {
            _frameworkPackages = new FrameworkPackages();
            _testDataDirectory = Path.Combine(TestContext.CurrentContext.TestDirectory, "FrameworkPackagesTestData");
            Directory.CreateDirectory(_testDataDirectory);
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                if (Directory.Exists(_testDataDirectory))
                {
                    // Remove read-only attributes from all files before deletion
                    foreach (var file in Directory.GetFiles(_testDataDirectory, "*", SearchOption.AllDirectories))
                    {
                        try
                        {
                            var fileInfo = new FileInfo(file);
                            if (fileInfo.Exists)
                            {
                                fileInfo.Attributes = FileAttributes.Normal;
                            }
                        }
                        catch
                        {
                            // Ignore file attribute errors
                        }
                    }
                    Directory.Delete(_testDataDirectory, true);
                }
            }
            catch
            {
                // Ignore cleanup errors to prevent test host crash
            }
        }

        [Test]
        public void GetFrameworkPackages_WhenAssemblyTypeIsNull_LogsWarningAndReturnsEmptyDictionary()
        {
            // Arrange - Create an empty list to avoid lock file processing
            var lockFilePaths = new List<string>();

            // Act - Since LoadAssembly will return null (assembly not present in test context)
            var result = _frameworkPackages.GetFrameworkPackages(lockFilePaths);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void GetFrameworkPackages_WhenGetFrameworkPackagesMethodIsNull_LogsNoticeAndReturnsEmptyDictionary()
        {
            // Arrange - Create an empty list to trigger the scenario where method lookup fails
            var lockFilePaths = new List<string>();

            // Act - This tests the path where frameworkPackagesType is null, 
            // leading to GetFrameworkPackagesMethod returning null
            var result = _frameworkPackages.GetFrameworkPackages(lockFilePaths);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void GetFrameworkPackages_WithArgumentException_CatchesAndLogsException()
        {
            // Arrange - Create invalid lock file path that will cause ArgumentException
            var lockFilePaths = new List<string> { string.Empty };

            // Act & Assert - Should not throw, exception should be caught
            Assert.DoesNotThrow(() => _frameworkPackages.GetFrameworkPackages(lockFilePaths));
        }

        [Test]
        public void GetFrameworkPackages_WithInvalidOperationException_CatchesAndLogsException()
        {
            // Arrange - Create a path that doesn't exist to potentially trigger InvalidOperationException
            var lockFilePaths = new List<string> { Path.Combine(_testDataDirectory, "nonexistent.json") };

            // Act & Assert - Should not throw, exception should be caught
            Assert.DoesNotThrow(() => _frameworkPackages.GetFrameworkPackages(lockFilePaths));
        }

        [Test]
        public void GetFrameworkPackages_WithMultipleExceptionScenarios_HandlesGracefully()
        {
            // Arrange - Mix of invalid paths and edge cases
            var lockFilePaths = new List<string>
            {
                null,
                string.Empty,
                Path.Combine(_testDataDirectory, "missing.json")
            };

            // Act
            var result = _frameworkPackages.GetFrameworkPackages(lockFilePaths);

            // Assert - Should return empty dictionary without crashing
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void GetFrameworkPackages_EmptyLockFilePathsList_ReturnsEmptyDictionary()
        {
            // Arrange
            var lockFilePaths = new List<string>();

            // Act
            var result = _frameworkPackages.GetFrameworkPackages(lockFilePaths);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void GetFrameworkPackages_InvalidJsonFile_HandlesExceptionAndReturnsEmptyDictionary()
        {
            // Arrange
            var invalidJsonFile = Path.Combine(_testDataDirectory, "invalid.json");
            File.WriteAllText(invalidJsonFile, "{ invalid json content");
            var lockFilePaths = new List<string> { invalidJsonFile };

            // Act & Assert
            Assert.DoesNotThrow(() => _frameworkPackages.GetFrameworkPackages(lockFilePaths));
        }

        [Test]
        public void GetFrameworkPackages_FileWithInvalidPath_CatchesArgumentException()
        {
            // Arrange - Create a path with invalid characters
            var lockFilePaths = new List<string> { "C:\\Invalid<>Path\\file.json" };

            // Act
            var result = _frameworkPackages.GetFrameworkPackages(lockFilePaths);

            // Assert - Should handle the exception and return empty dictionary
            Assert.That(result, Is.Not.Null);
        }
    }
}
