// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.Common;
using LCT.Common.Interface;
using LCT.Common.Model;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Directory = System.IO.Directory;
using File = System.IO.File;

namespace LCT.PackageIdentifier.UTest
{
    [TestFixture]
    public class ScannerTests
    {
        private Mock<IEnvironmentHelper> _environmentHelperMock;
        [SetUp]
        public void SetUp()
        {
            _environmentHelperMock = new Mock<IEnvironmentHelper>();
        }
        [Test]
        public void FolderScanner_GivenARootPath_ReturnsPackages()
        {
            //Arrange
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string filepath = Path.GetFullPath(Path.Combine(outFolder, "PackageIdentifierUTTestFiles", "TestDir"));
            string[] Includes = { "p*-lock.json" };
            string[] Excludes = { "node_modules" };
            Config config = new Config()
            {
                Include = Includes,
                Exclude = Excludes

            };
            IEnvironmentHelper environmentHelper = new EnvironmentHelper();
            //Act
            List<string> foundfiles = FolderScanner.FileScanner(filepath, config, environmentHelper);

            //Assert
            Assert.That(3, Is.EqualTo(foundfiles.Count), "Returns 3 package-lock.json files");

        }
        [Test]
        public void ValidateInputs_GivenNullConfig_ShouldExitApplication()
        {
            // Arrange
            string rootPath = "valid/path";
            Config config = null;

            _environmentHelperMock.Setup(x => x.CallEnvironmentExit(It.IsAny<int>()));

            // Act & Assert
            Assert.DoesNotThrow(() =>
            {
                typeof(FolderScanner).GetMethod("ValidateInputs", BindingFlags.NonPublic | BindingFlags.Static)
                    .Invoke(null, new object[] { rootPath, config, _environmentHelperMock.Object });
            });

            _environmentHelperMock.Verify(x => x.CallEnvironmentExit(-1), Times.Exactly(2));
        }

        [Test]
        public void ValidateInputs_GivenEmptyRootPath_ShouldExitApplication()
        {
            // Arrange
            string rootPath = "";
            Config config = new Config { Include = new[] { "*.json" }, Exclude = null };

            _environmentHelperMock.Setup(x => x.CallEnvironmentExit(It.IsAny<int>()));

            // Act & Assert
            Assert.DoesNotThrow(() =>
            {
                typeof(FolderScanner).GetMethod("ValidateInputs", BindingFlags.NonPublic | BindingFlags.Static)
                    .Invoke(null, new object[] { rootPath, config, _environmentHelperMock.Object });
            });

            _environmentHelperMock.Verify(x => x.CallEnvironmentExit(-1), Times.Exactly(2));
        }

        [Test]
        public void ValidateInputs_GivenNonExistentRootPath_ShouldExitApplication()
        {
            // Arrange
            string rootPath = "non/existent/path";
            Config config = new Config { Include = new[] { "*.json" }, Exclude = null };

            _environmentHelperMock.Setup(x => x.CallEnvironmentExit(It.IsAny<int>()));

            // Act & Assert
            Assert.DoesNotThrow(() =>
            {
                typeof(FolderScanner).GetMethod("ValidateInputs", BindingFlags.NonPublic | BindingFlags.Static)
                    .Invoke(null, new object[] { rootPath, config, _environmentHelperMock.Object });
            });

            _environmentHelperMock.Verify(x => x.CallEnvironmentExit(-1), Times.Once);
        }
        [Test]
        public void ProcessIncludePattern_GivenValidPattern_ShouldAddFilesToList()
        {
            // Arrange
            string rootPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestFiles"));
            Directory.CreateDirectory(rootPath);
            File.WriteAllText(Path.Combine(rootPath, "test.json"), "{}");

            string includePattern = "*.json";
            Config config = new Config { Include = new[] { includePattern }, Exclude = null };
            var fileOperationsMock = new Mock<IFileOperations>();
            List<string> allFoundConfigFiles = new List<string>();

            // Act & Assert
            Assert.DoesNotThrow(() =>
            {
                typeof(FolderScanner).GetMethod("ProcessIncludePattern", BindingFlags.NonPublic | BindingFlags.Static)
                    .Invoke(null, new object[] { rootPath, includePattern, config, fileOperationsMock.Object, allFoundConfigFiles });
            });

            // Assert
            Assert.AreEqual(1, allFoundConfigFiles.Count);
            Assert.AreEqual(Path.Combine(rootPath, "test.json"), allFoundConfigFiles[0]);

            // Cleanup
            Directory.Delete(rootPath, true);
        }

        [Test]
        public void ProcessIncludePattern_GivenInvalidPattern_ShouldNotAddFilesToList()
        {
            // Arrange
            string rootPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestFiles"));
            Directory.CreateDirectory(rootPath);

            string includePattern = "*.invalid";
            Config config = new Config { Include = new[] { includePattern }, Exclude = null };
            var fileOperationsMock = new Mock<IFileOperations>();
            List<string> allFoundConfigFiles = new List<string>();

            // Act & Assert
            Assert.DoesNotThrow(() =>
            {
                typeof(FolderScanner).GetMethod("ProcessIncludePattern", BindingFlags.NonPublic | BindingFlags.Static)
                    .Invoke(null, new object[] { rootPath, includePattern, config, fileOperationsMock.Object, allFoundConfigFiles });
            });

            // Assert
            Assert.AreEqual(0, allFoundConfigFiles.Count);

            // Cleanup
            Directory.Delete(rootPath, true);
        }       
        
    }
}
