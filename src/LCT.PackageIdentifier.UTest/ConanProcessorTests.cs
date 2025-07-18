// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models;
using LCT.APICommunications.Model.AQL;
using LCT.Common;
using LCT.Common.Constants;
using LCT.Common.Interface;
using LCT.Common.Model;
using LCT.PackageIdentifier.Interface;
using LCT.PackageIdentifier.Model;
using LCT.Services.Interface;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System;

namespace LCT.PackageIdentifier.UTest
{
    [TestFixture]
    public class ConanProcessorTests
    {
        private ConanProcessor _conanProcessor;
        private Mock<IJFrogService> _mockJFrogService;
        private Mock<IBomHelper> _mockBomHelper;
        private Mock<ICycloneDXBomParser> _mockCycloneDxBomParser;
        private Mock<ISpdxBomParser> _mockspdxBomParser;

        [SetUp]
        public void Setup()
        {
            _mockJFrogService = new Mock<IJFrogService>();
            _mockBomHelper = new Mock<IBomHelper>();
            _mockCycloneDxBomParser = new Mock<ICycloneDXBomParser>();
            _mockspdxBomParser = new Mock<ISpdxBomParser>();
            _conanProcessor = new ConanProcessor(_mockCycloneDxBomParser.Object, _mockspdxBomParser.Object);
        }

        [Test]
        public void GetArtifactoryRepoName_Returns_RepoName()
        {
            // Arrange
            var aqlResultList = new List<AqlResult>
            {
                new AqlResult
                {
                    Path = "org/package/1.0.0",
                    Repo = "repo1",
                    Name = "package.tgz"
                },
                new AqlResult
                {
                    Path = "org/package/2.0.0",
                    Repo = "repo2",
                    Name = "package.tgz"
                }
            };
            var component = new Component
            {
                Name = "package",
                Version = "1.0.0"
            };            

            // Act
            var repoName = ConanProcessor.GetArtifactoryRepoName(aqlResultList, component, out string jfrogRepoPath);

            // Assert
            Assert.AreEqual("repo1", repoName);
            Assert.AreEqual("repo1/org/package/1.0.0/package.tgz;", jfrogRepoPath);
        }

        #region UpdateBomKpiData Tests

        [Test]
        public void UpdateBomKpiData_DevDepRepo_IncrementsDevdependencyComponents()
        {
            // Arrange
            BomCreator.bomKpiData.DevdependencyComponents = 0;
            var appSettings = CreateTestAppSettings();
            appSettings.Conan.DevDepRepo = "dev-repo";
            string repoValue = "dev-repo";

            // Act
            var method = typeof(ConanProcessor).GetMethod("UpdateBomKpiData", BindingFlags.NonPublic | BindingFlags.Static);
            method.Invoke(null, new object[] { appSettings, repoValue });

            // Assert
            Assert.AreEqual(1, BomCreator.bomKpiData.DevdependencyComponents);
        }

        [Test]
        public void UpdateBomKpiData_ThirdPartyRepo_IncrementsThirdPartyRepoComponents()
        {
            // Arrange
            BomCreator.bomKpiData.ThirdPartyRepoComponents = 0;
            var appSettings = CreateTestAppSettings();
            appSettings.Conan.Artifactory.ThirdPartyRepos = new List<ThirdPartyRepo>
            {
                new ThirdPartyRepo { Name = "third-party-repo-1", Upload = true },
                new ThirdPartyRepo { Name = "third-party-repo-2", Upload = false }
            };
            string repoValue = "third-party-repo-1";

            // Act
            var method = typeof(ConanProcessor).GetMethod("UpdateBomKpiData", BindingFlags.NonPublic | BindingFlags.Static);
            method.Invoke(null, new object[] { appSettings, repoValue });

            // Assert
            Assert.AreEqual(1, BomCreator.bomKpiData.ThirdPartyRepoComponents);
        }

        [Test]
        public void UpdateBomKpiData_ReleaseRepo_IncrementsReleaseRepoComponents()
        {
            // Arrange
            BomCreator.bomKpiData.ReleaseRepoComponents = 0;
            var appSettings = CreateTestAppSettings();
            appSettings.Conan.ReleaseRepo = "release-repo";
            string repoValue = "release-repo";

            // Act
            var method = typeof(ConanProcessor).GetMethod("UpdateBomKpiData", BindingFlags.NonPublic | BindingFlags.Static);
            method.Invoke(null, new object[] { appSettings, repoValue });

            // Assert
            Assert.AreEqual(1, BomCreator.bomKpiData.ReleaseRepoComponents);
        }

        [Test]
        public void UpdateBomKpiData_NotFoundInJFrog_IncrementsUnofficialComponents()
        {
            // Arrange
            BomCreator.bomKpiData.UnofficialComponents = 0;
            var appSettings = CreateTestAppSettings();
            string repoValue = Dataconstant.NotFoundInJFrog;

            // Act
            var method = typeof(ConanProcessor).GetMethod("UpdateBomKpiData", BindingFlags.NonPublic | BindingFlags.Static);
            method.Invoke(null, new object[] { appSettings, repoValue });

            // Assert
            Assert.AreEqual(1, BomCreator.bomKpiData.UnofficialComponents);
        }

        [Test]
        public void UpdateBomKpiData_EmptyRepoValue_IncrementsUnofficialComponents()
        {
            // Arrange
            BomCreator.bomKpiData.UnofficialComponents = 0;
            var appSettings = CreateTestAppSettings();
            string repoValue = "";

            // Act
            var method = typeof(ConanProcessor).GetMethod("UpdateBomKpiData", BindingFlags.NonPublic | BindingFlags.Static);
            method.Invoke(null, new object[] { appSettings, repoValue });

            // Assert
            Assert.AreEqual(1, BomCreator.bomKpiData.UnofficialComponents);
        }

        [Test]
        public void UpdateBomKpiData_ThirdPartyReposNull_DoesNotIncrementThirdPartyComponents()
        {
            // Arrange
            BomCreator.bomKpiData.ThirdPartyRepoComponents = 0;
            BomCreator.bomKpiData.UnofficialComponents = 0;
            var appSettings = CreateTestAppSettings();
            appSettings.Conan.Artifactory.ThirdPartyRepos = null;
            string repoValue = "some-repo";

            // Act
            var method = typeof(ConanProcessor).GetMethod("UpdateBomKpiData", BindingFlags.NonPublic | BindingFlags.Static);
            method.Invoke(null, new object[] { appSettings, repoValue });

            // Assert
            Assert.AreEqual(0, BomCreator.bomKpiData.ThirdPartyRepoComponents);
            // When ThirdPartyRepos is null and repo doesn't match other conditions, no counter is incremented
            Assert.AreEqual(0, BomCreator.bomKpiData.UnofficialComponents);
        }

        [Test]
        public void UpdateBomKpiData_DevDepRepoTakesPrecedence_OverThirdPartyRepo()
        {
            // Arrange
            BomCreator.bomKpiData.DevdependencyComponents = 0;
            BomCreator.bomKpiData.ThirdPartyRepoComponents = 0;
            var appSettings = CreateTestAppSettings();
            appSettings.Conan.DevDepRepo = "shared-repo";
            appSettings.Conan.Artifactory.ThirdPartyRepos = new List<ThirdPartyRepo>
            {
                new ThirdPartyRepo { Name = "shared-repo", Upload = true }
            };
            string repoValue = "shared-repo";

            // Act
            var method = typeof(ConanProcessor).GetMethod("UpdateBomKpiData", BindingFlags.NonPublic | BindingFlags.Static);
            method.Invoke(null, new object[] { appSettings, repoValue });

            // Assert
            Assert.AreEqual(1, BomCreator.bomKpiData.DevdependencyComponents);
            Assert.AreEqual(0, BomCreator.bomKpiData.ThirdPartyRepoComponents);
        }

        [Test]
        public void UpdateBomKpiData_ThirdPartyRepoTakesPrecedence_OverReleaseRepo()
        {
            // Arrange
            BomCreator.bomKpiData.ThirdPartyRepoComponents = 0;
            BomCreator.bomKpiData.ReleaseRepoComponents = 0;
            var appSettings = CreateTestAppSettings();
            appSettings.Conan.ReleaseRepo = "shared-repo";
            appSettings.Conan.Artifactory.ThirdPartyRepos = new List<ThirdPartyRepo>
            {
                new ThirdPartyRepo { Name = "shared-repo", Upload = true }
            };
            string repoValue = "shared-repo";

            // Act
            var method = typeof(ConanProcessor).GetMethod("UpdateBomKpiData", BindingFlags.NonPublic | BindingFlags.Static);
            method.Invoke(null, new object[] { appSettings, repoValue });

            // Assert
            Assert.AreEqual(1, BomCreator.bomKpiData.ThirdPartyRepoComponents);
            Assert.AreEqual(0, BomCreator.bomKpiData.ReleaseRepoComponents);
        }

        [Test]
        public void UpdateBomKpiData_MultipleThirdPartyRepos_MatchesCorrectRepo()
        {
            // Arrange
            BomCreator.bomKpiData.ThirdPartyRepoComponents = 0;
            var appSettings = CreateTestAppSettings();
            appSettings.Conan.Artifactory.ThirdPartyRepos = new List<ThirdPartyRepo>
            {
                new ThirdPartyRepo { Name = "repo-1", Upload = true },
                new ThirdPartyRepo { Name = "repo-2", Upload = false },
                new ThirdPartyRepo { Name = "repo-3", Upload = true }
            };
            string repoValue = "repo-2";

            // Act
            var method = typeof(ConanProcessor).GetMethod("UpdateBomKpiData", BindingFlags.NonPublic | BindingFlags.Static);
            method.Invoke(null, new object[] { appSettings, repoValue });

            // Assert
            Assert.AreEqual(1, BomCreator.bomKpiData.ThirdPartyRepoComponents);
        }

        [Test]
        public void UpdateBomKpiData_CaseSensitive_RepoNameMatching()
        {
            // Arrange
            BomCreator.bomKpiData.ThirdPartyRepoComponents = 0;
            BomCreator.bomKpiData.UnofficialComponents = 0;
            var appSettings = CreateTestAppSettings();
            appSettings.Conan.Artifactory.ThirdPartyRepos = new List<ThirdPartyRepo>
            {
                new ThirdPartyRepo { Name = "Repo-Name", Upload = true }
            };
            string repoValue = "repo-name"; // Different case

            // Act
            var method = typeof(ConanProcessor).GetMethod("UpdateBomKpiData", BindingFlags.NonPublic | BindingFlags.Static);
            method.Invoke(null, new object[] { appSettings, repoValue });

            // Assert
            // Case-sensitive mismatch should not increment any counter (none of the conditions are met)
            Assert.AreEqual(0, BomCreator.bomKpiData.ThirdPartyRepoComponents);
            Assert.AreEqual(0, BomCreator.bomKpiData.UnofficialComponents);
        }

        [Test]
        public void UpdateBomKpiData_NullRepoValue_IncrementsUnofficialComponents()
        {
            // Arrange
            BomCreator.bomKpiData.UnofficialComponents = 0;
            var appSettings = CreateTestAppSettings();
            string repoValue = null;

            // Act
            var method = typeof(ConanProcessor).GetMethod("UpdateBomKpiData", BindingFlags.NonPublic | BindingFlags.Static);
            method.Invoke(null, new object[] { appSettings, repoValue });

            // Assert
            Assert.AreEqual(1, BomCreator.bomKpiData.UnofficialComponents);
        }

        [Test]
        public void UpdateBomKpiData_UnmatchedRepoValue_DoesNotIncrementAnyCounter()
        {
            // Arrange
            BomCreator.bomKpiData.DevdependencyComponents = 0;
            BomCreator.bomKpiData.ThirdPartyRepoComponents = 0;
            BomCreator.bomKpiData.ReleaseRepoComponents = 0;
            BomCreator.bomKpiData.UnofficialComponents = 0;
            var appSettings = CreateTestAppSettings();
            appSettings.Conan.DevDepRepo = "dev-repo";
            appSettings.Conan.ReleaseRepo = "release-repo";
            appSettings.Conan.Artifactory.ThirdPartyRepos = new List<ThirdPartyRepo>
            {
                new ThirdPartyRepo { Name = "third-party-repo", Upload = true }
            };
            string repoValue = "some-random-repo"; // Doesn't match any configured repo

            // Act
            var method = typeof(ConanProcessor).GetMethod("UpdateBomKpiData", BindingFlags.NonPublic | BindingFlags.Static);
            method.Invoke(null, new object[] { appSettings, repoValue });

            // Assert
            // None of the counters should be incremented for unmatched repo names
            Assert.AreEqual(0, BomCreator.bomKpiData.DevdependencyComponents);
            Assert.AreEqual(0, BomCreator.bomKpiData.ThirdPartyRepoComponents);
            Assert.AreEqual(0, BomCreator.bomKpiData.ReleaseRepoComponents);
            Assert.AreEqual(0, BomCreator.bomKpiData.UnofficialComponents);
        }

        #endregion

        #region IdentificationOfInternalComponents Tests

        [Test]
        public async Task IdentificationOfInternalComponents_ReturnsComponentData_Successfully()
        {
            // Arrange
            var component1 = new Component
            {
                Name = "boost",
                Version = "1.75.0",
                Purl = "pkg:conan/boost@1.75.0"
            };
            var components = new List<Component> { component1 };
            var componentData = new ComponentIdentification { comparisonBOMData = components };
            var internalRepos = new[] { "internal-repo-1", "internal-repo-2" };

            var appSettings = new CommonAppSettings
            {
                Conan = new Config
                {
                    Artifactory = new Artifactory
                    {
                        InternalRepos = internalRepos
                    }
                }
            };

            var aqlResult = new AqlResult
            {
                Name = "boost-1.75.0.tgz",
                Path = "boost/1.75.0/stable/package",
                Repo = "internal-repo-1"
            };

            var aqlResults = new List<AqlResult> { aqlResult };
            _mockBomHelper.Setup(m => m.GetListOfComponentsFromRepo(It.IsAny<string[]>(), It.IsAny<IJFrogService>()))
                .ReturnsAsync(aqlResults);

            // Act
            var result = await _conanProcessor.IdentificationOfInternalComponents(componentData, appSettings, _mockJFrogService.Object, _mockBomHelper.Object);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.comparisonBOMData, Is.Not.Null);
            Assert.That(result.internalComponents, Is.Not.Null);
            _mockBomHelper.Verify(m => m.GetListOfComponentsFromRepo(internalRepos, _mockJFrogService.Object), Times.Once);
        }

        [Test]
        public async Task IdentificationOfInternalComponents_WithNoInternalComponents_ReturnsEmptyInternalList()
        {
            // Arrange
            var component1 = new Component
            {
                Name = "external-lib",
                Version = "1.0.0",
                Purl = "pkg:conan/external-lib@1.0.0"
            };
            var components = new List<Component> { component1 };
            var componentData = new ComponentIdentification { comparisonBOMData = components };
            var internalRepos = new[] { "internal-repo-1" };

            var appSettings = new CommonAppSettings
            {
                Conan = new Config
                {
                    Artifactory = new Artifactory
                    {
                        InternalRepos = internalRepos
                    }
                }
            };

            var aqlResults = new List<AqlResult>(); // Empty list - no internal components
            _mockBomHelper.Setup(m => m.GetListOfComponentsFromRepo(It.IsAny<string[]>(), It.IsAny<IJFrogService>()))
                .ReturnsAsync(aqlResults);

            // Act
            var result = await _conanProcessor.IdentificationOfInternalComponents(componentData, appSettings, _mockJFrogService.Object, _mockBomHelper.Object);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.comparisonBOMData, Is.Not.Empty);
            Assert.That(result.internalComponents, Is.Empty);
        }

        [Test]
        public async Task GetJfrogRepoDetailsOfAComponent_ReturnsUpdatedComponents_Successfully()
        {
            // Arrange
            var component1 = new Component
            {
                Name = "boost",
                Version = "1.75.0",
                Purl = "pkg:conan/boost@1.75.0"
            };
            var components = new List<Component> { component1 };

            var appSettings = new CommonAppSettings
            {
                ProjectType = "CONAN",
                Conan = new Config
                {
                    Artifactory = new Artifactory
                    {
                        ThirdPartyRepos = new List<ThirdPartyRepo>
                        {
                            new ThirdPartyRepo { Name = "repo1", Upload = true }
                        }
                    }
                }
            };

            var aqlResult = new AqlResult
            {
                Name = "package.tgz",
                Path = "boost/1.75.0",
                Repo = "repo1",
                MD5 = "md5hash",
                SHA1 = "sha1hash",
                SHA256 = "sha256hash"
            };

            var aqlResults = new List<AqlResult> { aqlResult };
            _mockBomHelper.Setup(m => m.GetListOfComponentsFromRepo(It.IsAny<string[]>(), It.IsAny<IJFrogService>()))
                .ReturnsAsync(aqlResults);

            // Act
            var result = await _conanProcessor.GetJfrogRepoDetailsOfAComponent(components, appSettings, _mockJFrogService.Object, _mockBomHelper.Object);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].Properties, Is.Not.Null);
            Assert.That(result[0].Properties.Count, Is.GreaterThan(0));
        }

        [Test]
        public async Task GetJfrogRepoDetailsOfAComponent_WithUnsupportedComponent_ReturnsUnmodifiedComponent()
        {
            // Arrange
            var component1 = new Component
            {
                Name = "unsupported-component",
                Version = "1.0.0",
                Publisher = Dataconstant.UnsupportedPackageType
            };
            var components = new List<Component> { component1 };

            var appSettings = new CommonAppSettings
            {
                ProjectType = "CONAN",
                Conan = new Config()
            };

            var aqlResults = new List<AqlResult>();
            _mockBomHelper.Setup(m => m.GetListOfComponentsFromRepo(It.IsAny<string[]>(), It.IsAny<IJFrogService>()))
                .ReturnsAsync(aqlResults);

            // Act
            var result = await _conanProcessor.GetJfrogRepoDetailsOfAComponent(components, appSettings, _mockJFrogService.Object, _mockBomHelper.Object);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].Publisher, Is.EqualTo(Dataconstant.UnsupportedPackageType));
        }

        #endregion

        #region CreateFileForMultipleVersions Tests

        [Test]
        public void CreateFileForMultipleVersions_ExistingFile_UpdatesExistingFileWithConanComponents()
        {
            // Arrange
            var tempDir = Path.GetTempPath();
            var testOutputFolder = Path.Combine(tempDir, "test_output_" + Guid.NewGuid().ToString("N")[..8]);
            System.IO.Directory.CreateDirectory(testOutputFolder);

            try
            {
                var appSettings = new CommonAppSettings
                {
                    Directory = new LCT.Common.Directory
                    {
                        OutputFolder = testOutputFolder
                    },
                    SW360 = new SW360
                    {
                        ProjectName = "test-project"
                    }
                };

                var componentsWithMultipleVersions = new List<Component>
                {
                    new Component
                    {
                        Name = "boost",
                        Version = "1.75.0",
                        Description = "test-description-1"
                    },
                    new Component
                    {
                        Name = "openssl",
                        Version = "1.1.1",
                        Description = "test-description-2"
                    }
                };

                // Create existing file with some existing data
                var existingData = new MultipleVersions
                {
                    Npm = new List<MultipleVersionValues>
                    {
                        new MultipleVersionValues
                        {
                            ComponentName = "existing-npm-component",
                            ComponentVersion = "1.0.0",
                            PackageFoundIn = "existing-npm-description"
                        }
                    },
                    Nuget = new List<MultipleVersionValues>(),
                    Conan = new List<MultipleVersionValues>()
                };

                var defaultProjectName = "test-project";
                var existingFilePath = Path.Combine(testOutputFolder, $"{defaultProjectName}_{FileConstant.multipleversionsFileName}");
                var existingJsonContent = JsonConvert.SerializeObject(existingData, Formatting.Indented);
                System.IO.File.WriteAllText(existingFilePath, existingJsonContent);

                // Act
                ConanProcessor.CreateFileForMultipleVersions(componentsWithMultipleVersions, appSettings);

                // Assert
                Assert.IsTrue(System.IO.File.Exists(existingFilePath));

                var updatedJsonContent = System.IO.File.ReadAllText(existingFilePath);
                var updatedData = JsonConvert.DeserializeObject<MultipleVersions>(updatedJsonContent);

                Assert.IsNotNull(updatedData);
                Assert.IsNotNull(updatedData.Conan);
                Assert.AreEqual(2, updatedData.Conan.Count);

                // Verify the existing NPM component is preserved
                Assert.IsNotNull(updatedData.Npm);
                Assert.AreEqual(1, updatedData.Npm.Count);
                Assert.AreEqual("existing-npm-component", updatedData.Npm[0].ComponentName);

                // Verify the new Conan components are added
                var boostComponent = updatedData.Conan.FirstOrDefault(c => c.ComponentName == "boost");
                Assert.IsNotNull(boostComponent);
                Assert.AreEqual("1.75.0", boostComponent.ComponentVersion);
                Assert.AreEqual("test-description-1", boostComponent.PackageFoundIn);

                var opensslComponent = updatedData.Conan.FirstOrDefault(c => c.ComponentName == "openssl");
                Assert.IsNotNull(opensslComponent);
                Assert.AreEqual("1.1.1", opensslComponent.ComponentVersion);
                Assert.AreEqual("test-description-2", opensslComponent.PackageFoundIn);
            }
            finally
            {
                // Cleanup
                if (System.IO.Directory.Exists(testOutputFolder))
                {
                    System.IO.Directory.Delete(testOutputFolder, true);
                }
            }
        }

        [Test]
        public void CreateFileForMultipleVersions_ExistingFileWithEmptyConanSection_ReplacesConanComponents()
        {
            // Arrange
            var tempDir = Path.GetTempPath();
            var testOutputFolder = Path.Combine(tempDir, "test_output_" + Guid.NewGuid().ToString("N")[..8]);
            System.IO.Directory.CreateDirectory(testOutputFolder);

            try
            {
                var appSettings = new CommonAppSettings
                {
                    Directory = new LCT.Common.Directory
                    {
                        OutputFolder = testOutputFolder
                    },
                    SW360 = new SW360
                    {
                        ProjectName = "test-project"
                    }
                };

                var componentsWithMultipleVersions = new List<Component>
                {
                    new Component
                    {
                        Name = "zlib",
                        Version = "1.2.11",
                        Description = "compression-library"
                    }
                };

                // Create existing file with existing Conan data that should be replaced
                var existingData = new MultipleVersions
                {
                    Npm = new List<MultipleVersionValues>(),
                    Nuget = new List<MultipleVersionValues>(),
                    Conan = new List<MultipleVersionValues>
                    {
                        new MultipleVersionValues
                        {
                            ComponentName = "old-conan-component",
                            ComponentVersion = "0.1.0",
                            PackageFoundIn = "old-description"
                        }
                    }
                };

                var defaultProjectName = "test-project";
                var existingFilePath = Path.Combine(testOutputFolder, $"{defaultProjectName}_{FileConstant.multipleversionsFileName}");
                var existingJsonContent = JsonConvert.SerializeObject(existingData, Formatting.Indented);
                System.IO.File.WriteAllText(existingFilePath, existingJsonContent);

                // Act
                ConanProcessor.CreateFileForMultipleVersions(componentsWithMultipleVersions, appSettings);

                // Assert
                Assert.IsTrue(System.IO.File.Exists(existingFilePath));

                var updatedJsonContent = System.IO.File.ReadAllText(existingFilePath);
                var updatedData = JsonConvert.DeserializeObject<MultipleVersions>(updatedJsonContent);

                Assert.IsNotNull(updatedData);
                Assert.IsNotNull(updatedData.Conan);
                Assert.AreEqual(1, updatedData.Conan.Count);

                // Verify the old Conan component is replaced with the new one
                var zlibComponent = updatedData.Conan.FirstOrDefault(c => c.ComponentName == "zlib");
                Assert.IsNotNull(zlibComponent);
                Assert.AreEqual("1.2.11", zlibComponent.ComponentVersion);
                Assert.AreEqual("compression-library", zlibComponent.PackageFoundIn);

                // Verify old component is not present
                var oldComponent = updatedData.Conan.FirstOrDefault(c => c.ComponentName == "old-conan-component");
                Assert.IsNull(oldComponent);
            }
            finally
            {
                // Cleanup
                if (System.IO.Directory.Exists(testOutputFolder))
                {
                    System.IO.Directory.Delete(testOutputFolder, true);
                }
            }
        }

        #endregion

        private CommonAppSettings CreateTestAppSettings()
        {
            return new CommonAppSettings
            {
                Conan = new Config
                {
                    DevDepRepo = "default-dev-repo",
                    ReleaseRepo = "default-release-repo",
                    Artifactory = new Artifactory
                    {
                        ThirdPartyRepos = new List<ThirdPartyRepo>()
                    }
                }
            };
        }
    }
}
