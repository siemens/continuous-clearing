// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2026 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using SIT.APICommunications.Model.AQL;
using SIT.Common;
using SIT.Common.Constants;
using SIT.Common.Interface;
using SIT.Common.Model;
using SIT.Scan.Interface;
using SIT.Scan.Model;
using SIT.Services.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace SIT.Scan.UTest
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

        #endregion

        #region ParseDepJson / GetPackagesForBom / GetDependencyDetails Tests

        [Test]
        public void ParseDepJson_WithValidJson_ReturnsComponentsAndDependencies()
        {
            var depJson = new ConanDepJson
            {
                Graph = new ConanGraph
                {
                    Nodes = new Dictionary<string, ConanPackage>
                    {
                        ["0"] = new ConanPackage
                        {
                            Name = "", // root consumer node, skipped
                            Dependencies = new Dictionary<string, ConanDependency>
                            {
                                ["1"] = new ConanDependency { Direct = true }
                            }
                        },
                        ["1"] = new ConanPackage
                        {
                            Name = "boost",
                            Version = "1.75.0",
                            Context = "host",
                            Dependencies = new Dictionary<string, ConanDependency>
                            {
                                ["2"] = new ConanDependency { Direct = false }
                            }
                        },
                        ["2"] = new ConanPackage
                        {
                            Name = "zlib",
                            Version = "1.2.11",
                            Context = "build"
                        }
                    }
                }
            };
            string json = JsonConvert.SerializeObject(depJson);
            string filePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"conan-depjson-{Guid.NewGuid()}.json");
            System.IO.File.WriteAllText(filePath, json);
            try
            {
                var dependencies = new List<Dependency>();
                var method = typeof(ConanProcessor).GetMethod("ParseDepJson", BindingFlags.NonPublic | BindingFlags.Static);

                var result = (List<Component>)method.Invoke(null, new object[] { filePath, dependencies });

                Assert.That(result, Has.Count.EqualTo(2));
                var boost = result.First(c => c.Name == "boost");
                Assert.That(boost.Properties.First(p => p.Name == Dataconstant.Cdx_SiemensDirect).Value, Is.EqualTo("true"));
                var zlib = result.First(c => c.Name == "zlib");
                Assert.That(zlib.Properties.First(p => p.Name == Dataconstant.Cdx_IsDevelopment).Value, Is.EqualTo("true"));
                Assert.That(dependencies, Has.Count.GreaterThan(0));
            }
            finally
            {
                System.IO.File.Delete(filePath);
            }
        }

        [Test]
        public void ParseDepJson_WithNullNodes_ReturnsEmptyList()
        {
            var depJson = new ConanDepJson { Graph = new ConanGraph { Nodes = null } };
            string json = JsonConvert.SerializeObject(depJson);
            string filePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"conan-depjson-nullnodes-{Guid.NewGuid()}.json");
            System.IO.File.WriteAllText(filePath, json);
            try
            {
                var dependencies = new List<Dependency>();
                var method = typeof(ConanProcessor).GetMethod("ParseDepJson", BindingFlags.NonPublic | BindingFlags.Static);

                var result = (List<Component>)method.Invoke(null, new object[] { filePath, dependencies });

                Assert.That(result, Is.Empty);
            }
            finally
            {
                System.IO.File.Delete(filePath);
            }
        }

        [Test]
        public void ParseDepJson_WithMalformedJson_ReturnsEmptyListAndDoesNotThrow()
        {
            string filePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"conan-depjson-malformed-{Guid.NewGuid()}.json");
            System.IO.File.WriteAllText(filePath, "{ not valid json ");
            try
            {
                var dependencies = new List<Dependency>();
                var method = typeof(ConanProcessor).GetMethod("ParseDepJson", BindingFlags.NonPublic | BindingFlags.Static);

                List<Component> result = null;
                Assert.DoesNotThrow(() => result = (List<Component>)method.Invoke(null, new object[] { filePath, dependencies }));
                Assert.That(result, Is.Empty);
            }
            finally
            {
                System.IO.File.Delete(filePath);
            }
        }

        [Test]
        public void IsDevDependency_WithBuildContext_ReturnsTrueAndIncrementsCounter()
        {
            var package = new ConanPackage { Context = "build" };
            int noOfDevDependent = 0;

            bool result = ConanProcessor.IsDevDependency(package, ref noOfDevDependent);

            Assert.That(result, Is.True);
            Assert.That(noOfDevDependent, Is.EqualTo(1));
        }

        [Test]
        public void IsDevDependency_WithHostContext_ReturnsFalse()
        {
            var package = new ConanPackage { Context = "host" };
            int noOfDevDependent = 0;

            bool result = ConanProcessor.IsDevDependency(package, ref noOfDevDependent);

            Assert.That(result, Is.False);
            Assert.That(noOfDevDependent, Is.EqualTo(0));
        }

        [Test]
        public void GetDependencyDetails_WithDependencies_PopulatesDependencyList()
        {
            var compA = new Component { Name = "boost", Version = "1.75.0", Purl = "pkg:conan/boost@1.75.0" };
            var componentsForBOM = new List<Component> { compA };
            var nodePackages = new List<KeyValuePair<string, ConanPackage>>
            {
                new("1", new ConanPackage
                {
                    Name = "boost",
                    Version = "1.75.0",
                    Dependencies = new Dictionary<string, ConanDependency> { ["2"] = new ConanDependency() }
                }),
                new("2", new ConanPackage { Name = "zlib", Version = "1.2.11" })
            };
            var dependencies = new List<Dependency>();
            var method = typeof(ConanProcessor).GetMethod("GetDependencyDetails", BindingFlags.NonPublic | BindingFlags.Static);

            method.Invoke(null, new object[] { componentsForBOM, nodePackages, dependencies });

            Assert.That(dependencies, Has.Count.EqualTo(1));
            Assert.That(dependencies[0].Ref, Is.EqualTo("pkg:conan/boost@1.75.0"));
            Assert.That(dependencies[0].Dependencies[0].Ref, Does.Contain("zlib"));
        }

        [Test]
        public void GetDependencyDetails_WithNoDependencies_LeavesDependencyListEmpty()
        {
            var compA = new Component { Name = "boost", Version = "1.75.0", Purl = "pkg:conan/boost@1.75.0" };
            var componentsForBOM = new List<Component> { compA };
            var nodePackages = new List<KeyValuePair<string, ConanPackage>>
            {
                new("1", new ConanPackage { Name = "boost", Version = "1.75.0", Dependencies = new Dictionary<string, ConanDependency>() })
            };
            var dependencies = new List<Dependency>();
            var method = typeof(ConanProcessor).GetMethod("GetDependencyDetails", BindingFlags.NonPublic | BindingFlags.Static);

            method.Invoke(null, new object[] { componentsForBOM, nodePackages, dependencies });

            Assert.That(dependencies, Is.Empty);
        }

        #endregion

        #region AddingIdentifierType / IsInternalConanComponent Tests

        [Test]
        public void AddingIdentifierType_WithPackageFileSource_SetsDiscoveredProperty()
        {
            var components = new List<Component> { new Component { Name = "boost", Version = "1.75.0", Properties = new List<Property>() } };
            var method = typeof(ConanProcessor).GetMethod("AddingIdentifierType", BindingFlags.NonPublic | BindingFlags.Static);

            method.Invoke(null, new object[] { components, "PackageFile" });

            Assert.That(components[0].Properties.First(p => p.Name == Dataconstant.Cdx_IdentifierType).Value, Is.EqualTo(Dataconstant.Discovered));
        }

        [Test]
        public void AddingIdentifierType_WithOtherSource_SetsManuallyAddedProperty()
        {
            var components = new List<Component> { new Component { Name = "boost", Version = "1.75.0", Properties = new List<Property>() } };
            var method = typeof(ConanProcessor).GetMethod("AddingIdentifierType", BindingFlags.NonPublic | BindingFlags.Static);

            method.Invoke(null, new object[] { components, "Manual" });

            Assert.That(components[0].Properties.First(p => p.Name == Dataconstant.Cdx_IdentifierType).Value, Is.EqualTo(Dataconstant.ManullayAdded));
            Assert.That(components[0].Properties.First(p => p.Name == Dataconstant.Cdx_IsDevelopment).Value, Is.EqualTo("false"));
        }

        [Test]
        public void IsInternalConanComponent_WithMatch_ReturnsTrue()
        {
            var aqlResultList = new List<AqlResult> { new AqlResult { Path = "boost/1.75.0/stable" } };
            var component = new Component { Name = "boost", Version = "1.75.0" };
            var method = typeof(ConanProcessor).GetMethod("IsInternalConanComponent", BindingFlags.NonPublic | BindingFlags.Static);

            var result = (bool)method.Invoke(null, new object[] { aqlResultList, component });

            Assert.That(result, Is.True);
        }

        [Test]
        public void IsInternalConanComponent_WithNoMatch_ReturnsFalse()
        {
            var aqlResultList = new List<AqlResult> { new AqlResult { Path = "zlib/1.2.11/stable" } };
            var component = new Component { Name = "boost", Version = "1.75.0" };
            var method = typeof(ConanProcessor).GetMethod("IsInternalConanComponent", BindingFlags.NonPublic | BindingFlags.Static);

            var result = (bool)method.Invoke(null, new object[] { aqlResultList, component });

            Assert.That(result, Is.False);
        }

        #endregion

        #region UpdateComponentDetails / GetComponentHashes Tests

        [Test]
        public void UpdateComponentDetails_WithMatchingHashes_SetsComponentHashes()
        {
            var component = new Component { Name = "boost", Version = "1.75.0", Purl = "pkg:conan/boost@1.75.0" };
            var appSettings = CreateTestAppSettings();
            var projectType = new Property { Name = Dataconstant.Cdx_ProjectType, Value = "CONAN" };
            var aqlResultList = new List<AqlResult>
            {
                new AqlResult { Path = "boost/1.75.0", Repo = "repo1", Name = "boost.tgz", MD5 = "md5", SHA1 = "sha1", SHA256 = "sha256" }
            };
            var method = typeof(ConanProcessor).GetMethod("UpdateComponentDetails", BindingFlags.NonPublic | BindingFlags.Static);

            var result = (Component)method.Invoke(null, new object[] { component, aqlResultList, appSettings, projectType });

            Assert.That(result.Hashes, Is.Not.Null);
            Assert.That(result.Hashes, Has.Count.EqualTo(3));
            Assert.That(result.Description, Is.Null);
        }

        [Test]
        public void UpdateComponentDetails_WithNoMatchingHashes_LeavesHashesNull()
        {
            var component = new Component { Name = "boost", Version = "1.75.0", Purl = "pkg:conan/boost@1.75.0" };
            var appSettings = CreateTestAppSettings();
            var projectType = new Property { Name = Dataconstant.Cdx_ProjectType, Value = "CONAN" };
            var aqlResultList = new List<AqlResult>();
            var method = typeof(ConanProcessor).GetMethod("UpdateComponentDetails", BindingFlags.NonPublic | BindingFlags.Static);

            var result = (Component)method.Invoke(null, new object[] { component, aqlResultList, appSettings, projectType });

            Assert.That(result.Hashes, Is.Null);
        }

        [Test]
        public void GetComponentHashes_ReturnsThreeHashAlgorithms()
        {
            var aqlResult = new AqlResult { MD5 = "md5val", SHA1 = "sha1val", SHA256 = "sha256val" };
            var method = typeof(ConanProcessor).GetMethod("GetComponentHashes", BindingFlags.NonPublic | BindingFlags.Static);

            var result = (List<Hash>)method.Invoke(null, new object[] { aqlResult });

            Assert.That(result, Has.Count.EqualTo(3));
            Assert.That(result.Any(h => h.Alg == Hash.HashAlgorithm.MD5 && h.Content == "md5val"), Is.True);
            Assert.That(result.Any(h => h.Alg == Hash.HashAlgorithm.SHA_1 && h.Content == "sha1val"), Is.True);
            Assert.That(result.Any(h => h.Alg == Hash.HashAlgorithm.SHA_256 && h.Content == "sha256val"), Is.True);
        }

        #endregion

        #region CreateFileForMultipleVersions Tests

        [Test]
        public void CreateFileForMultipleVersions_WhenFileDoesNotExist_CreatesNewFile()
        {
            string outputFolder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"conan-multiversion-{Guid.NewGuid()}");
            System.IO.Directory.CreateDirectory(outputFolder);
            try
            {
                var appSettings = CreateTestAppSettings();
                appSettings.Directory = new Directory { OutputFolder = outputFolder };
                appSettings.SW360 = new SW360 { ProjectName = "TestProject" };
                var components = new List<Component>
                {
                    new Component { Name = "boost", Version = "1.75.0", Description = "" },
                    new Component { Name = "boost", Version = "1.76.0", Description = "" }
                };

                Assert.DoesNotThrow(() => ConanProcessor.CreateFileForMultipleVersions(components, appSettings));

                string expectedFilePath = System.IO.Path.Combine(outputFolder, $"{appSettings.SW360.ProjectName}_{FileConstant.multipleversionsFileName}");
                Assert.That(System.IO.File.Exists(expectedFilePath), Is.True);
            }
            finally
            {
                System.IO.Directory.Delete(outputFolder, true);
            }
        }

        [Test]
        public void CreateFileForMultipleVersions_WhenFileAlreadyExists_UpdatesExistingFile()
        {
            string outputFolder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"conan-multiversion-existing-{Guid.NewGuid()}");
            System.IO.Directory.CreateDirectory(outputFolder);
            try
            {
                var appSettings = CreateTestAppSettings();
                appSettings.Directory = new Directory { OutputFolder = outputFolder };
                appSettings.SW360 = new SW360 { ProjectName = "TestProject" };
                var components = new List<Component>
                {
                    new Component { Name = "boost", Version = "1.75.0", Description = "" }
                };
                ConanProcessor.CreateFileForMultipleVersions(components, appSettings);

                var componentsSecondRun = new List<Component>
                {
                    new Component { Name = "zlib", Version = "1.2.11", Description = "" }
                };

                Assert.DoesNotThrow(() => ConanProcessor.CreateFileForMultipleVersions(componentsSecondRun, appSettings));
            }
            finally
            {
                System.IO.Directory.Delete(outputFolder, true);
            }
        }

        #endregion

        private static CommonAppSettings CreateTestAppSettings()
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

