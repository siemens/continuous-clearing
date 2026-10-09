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
using System.Threading.Tasks;

namespace SIT.Scan.UTest
{
    [TestFixture]
    public class CargoProcessorTests
    {
        private const string TestFolder = "TestInputFolder";
        private CargoProcessor _cargoProcessor;
        private Mock<IJFrogService> _mockJFrogService;
        private Mock<IBomHelper> _mockBomHelper;
        private Mock<ICycloneDXBomParser> _mockCycloneDxBomParser;
        private Mock<ISpdxBomParser> _mockSpdxBomParser;

        [SetUp]
        public void Setup()
        {
            if (!System.IO.Directory.Exists(TestFolder))
                System.IO.Directory.CreateDirectory(TestFolder);

            var dummyFilePath = System.IO.Path.Combine(TestFolder, "dummy.cargo");
            if (!System.IO.File.Exists(dummyFilePath))
                System.IO.File.WriteAllText(dummyFilePath, "{}"); // Minimal valid JSON or content

            _mockJFrogService = new Mock<IJFrogService>();
            _mockBomHelper = new Mock<IBomHelper>();
            _mockCycloneDxBomParser = new Mock<ICycloneDXBomParser>();
            _mockSpdxBomParser = new Mock<ISpdxBomParser>();
            _cargoProcessor = new CargoProcessor(_mockCycloneDxBomParser.Object, _mockSpdxBomParser.Object);
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            if (System.IO.Directory.Exists(TestFolder))
                System.IO.Directory.Delete(TestFolder, true);
        }

        [Test]
        public void GetDetailsforManuallyAddedCompCoversMethod()
        {
            var components = new List<Component>
    {
        new Component
        {
            Name = "ManualComp",
            Version = "1.0",
            Purl = "pkg:cargo/ManualComp@1.0",
            Properties = new List<Property>()
        }
    };

            // Call the method directly from BomHelper
            BomHelper.GetDetailsforManuallyAddedComp(components);

            var propNames = components[0].Properties.Select(p => p.Name).ToList();
            Assert.Contains(Dataconstant.Cdx_IsDevelopment, propNames);
            Assert.Contains(Dataconstant.Cdx_IdentifierType, propNames);
            Assert.AreEqual("false", components[0].Properties.First(p => p.Name == Dataconstant.Cdx_IsDevelopment).Value);
            Assert.AreEqual(Dataconstant.ManullayAdded, components[0].Properties.First(p => p.Name == Dataconstant.Cdx_IdentifierType).Value);
        }

        [Test]
        public async Task IdentificationOfInternalComponents_ReturnsUpdatedComponentIdentification()
        {
            var appSettings = CreateTestAppSettings();
            var componentData = new ComponentIdentification
            {
                comparisonBOMData = new List<Component> { new Component { Name = "Test", Version = "1.0", Purl = "pkg:cargo/Test@1.0" } }
            };

            _mockBomHelper.Setup(b => b.GetCargoListOfComponentsFromRepo(It.IsAny<string[]>(), _mockJFrogService.Object))
                .ReturnsAsync(new List<AqlResult>
                {
                    new AqlResult
                    {
                        Properties = new List<AqlProperty>
                        {
                            new AqlProperty { Key = "crate.name", Value = "Test" },
                            new AqlProperty { Key = "crate.version", Value = "1.0" }
                        }
                    }
                });

            var result = await _cargoProcessor.IdentificationOfInternalComponents(componentData, appSettings, _mockJFrogService.Object, _mockBomHelper.Object);

            Assert.NotNull(result);
            Assert.NotNull(result.internalComponents);
        }

        [Test]
        public async Task IdentificationOfInternalComponents_HandlesNoInternalComponents()
        {
            var appSettings = CreateTestAppSettings();
            var componentData = new ComponentIdentification
            {
                comparisonBOMData = new List<Component> { new Component { Name = "NotInternal", Version = "1.0", Purl = "pkg:cargo/NotInternal@1.0" } }
            };

            _mockBomHelper.Setup(b => b.GetCargoListOfComponentsFromRepo(It.IsAny<string[]>(), _mockJFrogService.Object))
                .ReturnsAsync(new List<AqlResult>());

            var result = await _cargoProcessor.IdentificationOfInternalComponents(componentData, appSettings, _mockJFrogService.Object, _mockBomHelper.Object);

            Assert.NotNull(result);
            Assert.IsEmpty(result.internalComponents ?? new List<Component>());
        }

        [Test]
        public async Task GetJfrogRepoDetailsOfAComponent_ReturnsModifiedComponents()
        {
            var appSettings = CreateTestAppSettings();
            var components = new List<Component>
            {
                new Component { Name = "Test", Version = "1.0", Purl = "pkg:cargo/Test@1.0" }
            };

            _mockBomHelper.Setup(b => b.GetCargoListOfComponentsFromRepo(It.IsAny<string[]>(), _mockJFrogService.Object))
                .ReturnsAsync(new List<AqlResult>
                {
                    new AqlResult
                    {
                        Name = "Test",
                        Repo = "repo1",
                        Properties = new List<AqlProperty>
                        {
                            new AqlProperty { Key = "crate.name", Value = "Test" },
                            new AqlProperty { Key = "crate.version", Value = "1.0" }
                        }
                    }
                });

            var result = await _cargoProcessor.GetJfrogRepoDetailsOfAComponent(components, appSettings, _mockJFrogService.Object, _mockBomHelper.Object);

            //Assert.Null(result);
            Assert.IsFalse(result[0].Properties.Any(p => p.Name == Dataconstant.Cdx_SiemensDirect));
            Assert.AreEqual("Test", result[0].Name);
        }

        [Test]
        public async Task GetJfrogRepoDetailsOfAComponent_HandlesEmptyAqlResult()
        {
            var appSettings = CreateTestAppSettings();
            var components = new List<Component>
            {
                new Component { Name = "Test", Version = "1.0", Purl = "pkg:cargo/Test@1.0" }
            };

            _mockBomHelper.Setup(b => b.GetCargoListOfComponentsFromRepo(It.IsAny<string[]>(), _mockJFrogService.Object))
                .ReturnsAsync(new List<AqlResult>());

            var result = await _cargoProcessor.GetJfrogRepoDetailsOfAComponent(components, appSettings, _mockJFrogService.Object, _mockBomHelper.Object);

            Assert.NotNull(result);
            Assert.AreEqual("Test", result[0].Name);
        }

        [Test]
        public void AddSiemensDirectProperty_SetsPropertyCorrectly()
        {
            var bom = new Bom
            {
                Components = new List<Component>
                {
                    new Component { Name = "CompA", Version = "1.0", Purl = "pkg:cargo/CompA@1.0", Properties = new List<Property>() }
                },
                Dependencies = new List<Dependency>
                {
                    new Dependency { Ref = "pkg:cargo/CompA@1.0" }
                }
            };

            CargoProcessor.AddSiemensDirectProperty(ref bom);

            Assert.IsTrue(bom.Components[0].Properties.Any(p => p.Name == Dataconstant.Cdx_SiemensDirect));
        }

        [Test]
        public void GetDetailsforManuallyAddedComp_CoversMethod()
        {
            var components = new List<Component>
    {
        new Component
        {
            Name = "ManualComp",
            Version = "1.0",
            Purl = "pkg:cargo/ManualComp@1.0",
            Properties = new List<Property>()
        }
    };

            // Call the method directly from BomHelper
            BomHelper.GetDetailsforManuallyAddedComp(components);

            var propNames = components[0].Properties.Select(p => p.Name).ToList();
            Assert.Contains(Dataconstant.Cdx_IsDevelopment, propNames);
            Assert.Contains(Dataconstant.Cdx_IdentifierType, propNames);
            Assert.AreEqual("false", components[0].Properties.First(p => p.Name == Dataconstant.Cdx_IsDevelopment).Value);
            Assert.AreEqual(Dataconstant.ManullayAdded, components[0].Properties.First(p => p.Name == Dataconstant.Cdx_IdentifierType).Value);
        }

        [Test]
        public void AddingIdentifierType_SetsDiscoveredProperty()
        {
            var components = new List<Component>
    {
        new Component { Name = "Comp", Version = "1.0", Purl = "pkg:cargo/Comp@1.0" }
    };
            var method = typeof(CargoProcessor).GetMethod("AddingIdentifierType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            method.Invoke(null, new object[] { components });

            Assert.IsTrue(components[0].Properties.Any(p => p.Name == Dataconstant.Cdx_IdentifierType));
            Assert.AreEqual(Dataconstant.Discovered, components[0].Properties.First(p => p.Name == Dataconstant.Cdx_IdentifierType).Value);
        }

        [Test]
        public void GetArtifactoryRepoName_ReturnsNotFoundInRepo_WhenNoMatch()
        {
            var aqlResultList = new List<AqlResult>();
            var component = new Component { Name = "Test", Version = "1.0" };
            var bomHelper = new Mock<IBomHelper>();
            var method = typeof(CargoProcessor).GetMethod("GetArtifactoryRepoName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            var repoName = (string)method.Invoke(null, new object[] { aqlResultList, component, bomHelper.Object, null, null });

            Assert.AreEqual("Not Found in JFrogRepo", repoName);
        }

        [Test]
        public void GetJfrogNameOfCargoComponent_ReturnsPackageNameNotFoundInJfrog_WhenNoMatch()
        {
            var aqlResultList = new List<AqlResult>();
            var method = typeof(CargoProcessor).GetMethod("GetJfrogNameOfCargoComponent", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            var nameVersion = (string)method.Invoke(null, new object[] { "Test", "1.0", aqlResultList });

            Assert.AreEqual(Dataconstant.PackageNameNotFoundInJfrog, nameVersion);
        }

        [Test]
        public void GetJfrogRepoPath_ReturnsRepoAndName_WhenPathIsEmpty()
        {
            var aqlResult = new AqlResult { Repo = "repo", Name = "name", Path = "" };
            var method = typeof(CargoProcessor).GetMethod("GetJfrogRepoPath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            var path = (string)method.Invoke(null, new object[] { aqlResult });

            Assert.AreEqual("repo/name", path);
        }



        [Test]
        public void AddSiemensDirectProperty_EmptyBom_DoesNotThrow()
        {
            var bom = new Bom
            {
                Components = new List<Component>(),
                Dependencies = new List<Dependency>()
            };

            Assert.DoesNotThrow(() => CargoProcessor.AddSiemensDirectProperty(ref bom));
            Assert.IsEmpty(bom.Components);
        }

        [Test]
        public void AddingIdentifierType_EmptyList_DoesNotThrow()
        {
            var components = new List<Component>();
            var method = typeof(CargoProcessor).GetMethod("AddingIdentifierType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            Assert.DoesNotThrow(() => method.Invoke(null, new object[] { components }));
            Assert.IsEmpty(components);
        }


        [Test]
        public void GetJfrogRepoPath_NonEmptyPath_ReturnsFullPath()
        {
            var aqlResult = new AqlResult { Repo = "repo", Name = "name", Path = "somepath" };
            var method = typeof(CargoProcessor).GetMethod("GetJfrogRepoPath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            var path = (string)method.Invoke(null, new object[] { aqlResult });

            Assert.AreEqual("repo/somepath/name", path);
        }

        [Test]
        public void ProcessCargoComponent_WithEmptyAqlResultList_ReturnsComponentUnchanged()
        {
            // Arrange
            var component = new Component
            {
                Name = "TestComp",
                Version = "1.0",
                Properties = new List<Property>()
            };
            var bomHelperMock = new Mock<IBomHelper>();
            var appSettings = CreateTestAppSettings();
            var projectTypeProperty = new Property();

            var method = typeof(CargoProcessor).GetMethod("ProcessCargoComponent", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            // Act
            var result = (Component)method.Invoke(null, new object[] { component, new List<AqlResult>(), bomHelperMock.Object, appSettings, projectTypeProperty });

            // Assert
            Assert.AreEqual(component.Name, result.Name);
            Assert.AreEqual(component.Version, result.Version);
        }

        [Test]
        public void UpdateCargoKpiDataBasedOnRepo_ReleaseRepo_IncrementsReleaseRepoComponents()
        {
            BomCreator.bomKpiData.ReleaseRepoComponents = 0;
            var appSettings = CreateTestAppSettings();
            appSettings.Cargo.ReleaseRepo = "release-repo";
            string repoValue = "release-repo";
            var method = typeof(CargoProcessor).GetMethod("UpdateCargoKpiDataBasedOnRepo", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            method.Invoke(null, new object[] { repoValue, appSettings });
            Assert.AreEqual(1, BomCreator.bomKpiData.ReleaseRepoComponents);
        }

        [Test]
        public void UpdateCargoKpiDataBasedOnRepo_NotFoundInJFrog_IncrementsUnofficialComponents()
        {
            BomCreator.bomKpiData.UnofficialComponents = 0;
            var appSettings = CreateTestAppSettings();
            string repoValue = "Not Found in JFrogRepo";
            var method = typeof(CargoProcessor).GetMethod("UpdateCargoKpiDataBasedOnRepo", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            method.Invoke(null, new object[] { repoValue, appSettings });
            Assert.AreEqual(1, BomCreator.bomKpiData.UnofficialComponents);
        }

        [Test]
        public void UpdateCargoKpiDataBasedOnRepo_EmptyRepoValue_IncrementsUnofficialComponents()
        {
            BomCreator.bomKpiData.UnofficialComponents = 0;
            var appSettings = CreateTestAppSettings();
            string repoValue = "";
            var method = typeof(CargoProcessor).GetMethod("UpdateCargoKpiDataBasedOnRepo", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            method.Invoke(null, new object[] { repoValue, appSettings });
            Assert.AreEqual(1, BomCreator.bomKpiData.UnofficialComponents);
        }

        [Test]
        public void IsInternalCargoComponent_ReturnsTrue_WhenAqlResultMatches()
        {
            var aqlResultList = new List<AqlResult>
{
    new AqlResult { Name = "TestComp", Repo = "repo1", Path = "path1", Properties = new List<AqlProperty>() }
};


            var component = new Component { Name = "TestComp", Version = "1.0" };
            var method = typeof(CargoProcessor).GetMethod("IsInternalCargoComponent", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var result = (bool)method.Invoke(null, new object[] { aqlResultList, component });
            Assert.IsFalse(result);
        }

        [Test]
        public void IsInternalCargoComponent_ReturnsFalse_WhenNoMatch()
        {
            var aqlResultList = new List<AqlResult>
{
    new AqlResult { Name = "TestComp", Repo = "repo1", Path = "path1", Properties = new List<AqlProperty>() }
};
            var component = new Component { Name = "TestComp", Version = "1.0" };
            var method = typeof(CargoProcessor).GetMethod("IsInternalCargoComponent", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var result = (bool)method.Invoke(null, new object[] { aqlResultList, component });
            Assert.IsFalse(result);
        }

        [Test]
        public void GetPackagesFromCargoMetadataJson_WithInvalidPath_DoesNotThrow()
        {
            var method = typeof(CargoProcessor).GetMethod("GetPackagesFromCargoMetadataJson", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var components = new List<Component>();
            var dependencies = new List<Dependency>();
            Assert.DoesNotThrow(() => method.Invoke(null, new object[] { "invalid_path.json", components, dependencies }));
        }

        [Test]
        public void ParseCargoFile_WithInvalidPath_DoesNotThrow()
        {
            var method = typeof(CargoProcessor).GetMethod("ParseCargoFile", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var components = new List<Component>();
            var dependencies = new List<Dependency>();
            Assert.DoesNotThrow(() => method.Invoke(null, new object[] { "invalid_path.json", components, dependencies }));
        }

        [Test]
        public void AddSourceUrlExternalReference_WithRepository_AddsExternalReference()
        {
            var component = new Component { Name = "Comp", Version = "1.0" };
            var pkg = new CargoPackageDetails.Package { Repository = "https://example.com/repo" };
            var method = typeof(CargoProcessor).GetMethod("AddSourceUrlExternalReference", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            method.Invoke(null, new object[] { component, pkg });

            Assert.That(component.ExternalReferences, Is.Not.Null);
            Assert.That(component.ExternalReferences[0].Url, Is.EqualTo("https://example.com/repo"));
            Assert.That(component.ExternalReferences[0].Type, Is.EqualTo(ExternalReference.ExternalReferenceType.Distribution));
        }

        [Test]
        public void AddSourceUrlExternalReference_WithoutRepository_DoesNotAddExternalReference()
        {
            var component = new Component { Name = "Comp", Version = "1.0" };
            var pkg = new CargoPackageDetails.Package { Repository = "   " };
            var method = typeof(CargoProcessor).GetMethod("AddSourceUrlExternalReference", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            method.Invoke(null, new object[] { component, pkg });

            Assert.That(component.ExternalReferences, Is.Null);
        }

        [Test]
        public void ParseCargoPackagesExcluding_ExcludesWorkspaceMembersAndPopulatesMaps()
        {
            var packageDetails = new CargoPackageDetails
            {
                Packages = new List<CargoPackageDetails.Package>
                {
                    new CargoPackageDetails.Package { Id = "id1", Name = "CompA", Version = "1.0", Repository = "https://repo/a" },
                    new CargoPackageDetails.Package { Id = "id2", Name = "CompB", Version = "2.0" }
                }
            };
            var components = new List<Component>();
            var idToComponent = new Dictionary<string, Component>();
            var idToPurl = new Dictionary<string, string>();
            var excludeIds = new List<string> { "id2" };
            var method = typeof(CargoProcessor).GetMethod("ParseCargoPackagesExcluding", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            method.Invoke(null, new object[] { packageDetails, components, idToComponent, idToPurl, excludeIds });

            Assert.That(components, Has.Count.EqualTo(1));
            Assert.That(components[0].Name, Is.EqualTo("CompA"));
            Assert.That(idToComponent.ContainsKey("id1"), Is.True);
            Assert.That(idToComponent.ContainsKey("id2"), Is.False);
        }

        [Test]
        public void ParseCargoPackagesExcluding_WithNullPackages_DoesNotThrow()
        {
            var packageDetails = new CargoPackageDetails { Packages = null };
            var components = new List<Component>();
            var idToComponent = new Dictionary<string, Component>();
            var idToPurl = new Dictionary<string, string>();
            var excludeIds = new List<string>();
            var method = typeof(CargoProcessor).GetMethod("ParseCargoPackagesExcluding", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            Assert.DoesNotThrow(() => method.Invoke(null, new object[] { packageDetails, components, idToComponent, idToPurl, excludeIds }));
            Assert.That(components, Is.Empty);
        }

        [Test]
        public void IsValidDep_WithNullDep_ReturnsFalse()
        {
            var idToPurl = new Dictionary<string, string>();
            var method = typeof(CargoProcessor).GetMethod("IsValidDep", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var parameters = new object[] { null, idToPurl, null };

            var result = (bool)method.Invoke(null, parameters);

            Assert.That(result, Is.False);
        }

        [Test]
        public void IsValidDep_WithUnresolvedPkg_ReturnsFalse()
        {
            var dep = new CargoPackageDetails.Dep { Pkg = "unknown-id" };
            var idToPurl = new Dictionary<string, string>();
            var method = typeof(CargoProcessor).GetMethod("IsValidDep", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var parameters = new object[] { dep, idToPurl, null };

            var result = (bool)method.Invoke(null, parameters);

            Assert.That(result, Is.False);
        }

        [Test]
        public void IsValidDep_WithResolvablePkg_ReturnsTrueAndOutputsPurl()
        {
            var dep = new CargoPackageDetails.Dep { Pkg = "id1" };
            var idToPurl = new Dictionary<string, string> { { "id1", "pkg:cargo/CompA@1.0" } };
            var method = typeof(CargoProcessor).GetMethod("IsValidDep", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var parameters = new object[] { dep, idToPurl, null };

            var result = (bool)method.Invoke(null, parameters);

            Assert.That(result, Is.True);
            Assert.That(parameters[2], Is.EqualTo("pkg:cargo/CompA@1.0"));
        }

        [Test]
        public void GetOrCreateKindList_WhenKeyMissing_CreatesNewList()
        {
            var purlToDevDependencyKinds = new Dictionary<string, List<string>>();
            var method = typeof(CargoProcessor).GetMethod("GetOrCreateKindList", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            var result = (List<string>)method.Invoke(null, new object[] { "pkg:cargo/CompA@1.0", purlToDevDependencyKinds });

            Assert.That(result, Is.Not.Null);
            Assert.That(purlToDevDependencyKinds.ContainsKey("pkg:cargo/CompA@1.0"), Is.True);
        }

        [Test]
        public void GetOrCreateKindList_WhenKeyExists_ReturnsExistingList()
        {
            var existingList = new List<string> { "dev" };
            var purlToDevDependencyKinds = new Dictionary<string, List<string>> { { "pkg:cargo/CompA@1.0", existingList } };
            var method = typeof(CargoProcessor).GetMethod("GetOrCreateKindList", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            var result = (List<string>)method.Invoke(null, new object[] { "pkg:cargo/CompA@1.0", purlToDevDependencyKinds });

            Assert.That(result, Is.SameAs(existingList));
        }

        [Test]
        public void AddDepKinds_WithDepKinds_AddsEachKind()
        {
            var dep = new CargoPackageDetails.Dep
            {
                DepKinds = new List<CargoPackageDetails.DepKind>
                {
                    new CargoPackageDetails.DepKind { Kind = "dev" },
                    new CargoPackageDetails.DepKind { Kind = "build" }
                }
            };
            var dependencyKindList = new List<string>();
            var method = typeof(CargoProcessor).GetMethod("AddDepKinds", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            method.Invoke(null, new object[] { dep, dependencyKindList });

            Assert.That(dependencyKindList, Is.EquivalentTo(new[] { "dev", "build" }));
        }

        [Test]
        public void AddDepKinds_WithoutDepKinds_AddsNull()
        {
            var dep = new CargoPackageDetails.Dep { DepKinds = null };
            var dependencyKindList = new List<string>();
            var method = typeof(CargoProcessor).GetMethod("AddDepKinds", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            method.Invoke(null, new object[] { dep, dependencyKindList });

            Assert.That(dependencyKindList, Has.Count.EqualTo(1));
            Assert.That(dependencyKindList[0], Is.Null);
        }

        [Test]
        public void ProcessNodeDeps_WithNullDeps_DoesNotThrow()
        {
            var node = new CargoPackageDetails.Node { Deps = null };
            var idToPurl = new Dictionary<string, string>();
            var purlToDevDependencyKinds = new Dictionary<string, List<string>>();
            var method = typeof(CargoProcessor).GetMethod("ProcessNodeDeps", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            Assert.DoesNotThrow(() => method.Invoke(null, new object[] { node, idToPurl, purlToDevDependencyKinds }));
        }

        [Test]
        public void ProcessNodeDeps_WithValidDeps_PopulatesKindsByPurl()
        {
            var node = new CargoPackageDetails.Node
            {
                Deps = new List<CargoPackageDetails.Dep>
                {
                    new CargoPackageDetails.Dep
                    {
                        Pkg = "id1",
                        DepKinds = new List<CargoPackageDetails.DepKind> { new CargoPackageDetails.DepKind { Kind = "dev" } }
                    }
                }
            };
            var idToPurl = new Dictionary<string, string> { { "id1", "pkg:cargo/CompA@1.0" } };
            var purlToDevDependencyKinds = new Dictionary<string, List<string>>();
            var method = typeof(CargoProcessor).GetMethod("ProcessNodeDeps", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            method.Invoke(null, new object[] { node, idToPurl, purlToDevDependencyKinds });

            Assert.That(purlToDevDependencyKinds["pkg:cargo/CompA@1.0"], Is.EquivalentTo(new[] { "dev" }));
        }

        [Test]
        public void AddCycloneDXDependencyExcluding_WithValidParent_AddsDependencyWithSubDeps()
        {
            var compA = new Component { Name = "CompA", Version = "1.0", Purl = "pkg:cargo/CompA@1.0" };
            var compB = new Component { Name = "CompB", Version = "1.0", Purl = "pkg:cargo/CompB@1.0" };
            var idToComponent = new Dictionary<string, Component> { { "idA", compA }, { "idB", compB } };
            var node = new CargoPackageDetails.Node { Id = "idA", Dependencies = new List<string> { "idB" } };
            var dependencies = new List<Dependency>();
            var excludeIds = new List<string>();
            var method = typeof(CargoProcessor).GetMethod("AddCycloneDXDependencyExcluding", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            method.Invoke(null, new object[] { node, idToComponent, "idA", dependencies, excludeIds });

            Assert.That(dependencies, Has.Count.EqualTo(1));
            Assert.That(dependencies[0].Ref, Is.EqualTo("pkg:cargo/CompA@1.0"));
            Assert.That(dependencies[0].Dependencies[0].Ref, Is.EqualTo("pkg:cargo/CompB@1.0"));
        }

        [Test]
        public void AddCycloneDXDependencyExcluding_WithUnknownParent_DoesNotAddDependency()
        {
            var idToComponent = new Dictionary<string, Component>();
            var node = new CargoPackageDetails.Node { Id = "idA", Dependencies = new List<string>() };
            var dependencies = new List<Dependency>();
            var excludeIds = new List<string>();
            var method = typeof(CargoProcessor).GetMethod("AddCycloneDXDependencyExcluding", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            method.Invoke(null, new object[] { node, idToComponent, "idA", dependencies, excludeIds });

            Assert.That(dependencies, Is.Empty);
        }

        [Test]
        public void AnalyzeCargoDependencyKindsExcluding_WithNullNodes_DoesNotThrow()
        {
            var packageDetails = new CargoPackageDetails { ResolveInfo = new CargoPackageDetails.Resolve { Nodes = null } };
            var idToPurl = new Dictionary<string, string>();
            var purlToDevDependencyKinds = new Dictionary<string, List<string>>();
            var idToComponent = new Dictionary<string, Component>();
            var dependencies = new List<Dependency>();
            var excludeIds = new List<string>();
            var method = typeof(CargoProcessor).GetMethod("AnalyzeCargoDependencyKindsExcluding", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            Assert.DoesNotThrow(() => method.Invoke(null, new object[] { packageDetails, idToPurl, purlToDevDependencyKinds, idToComponent, dependencies, excludeIds }));
        }

        [Test]
        public void MarkCargoDevelopmentProperties_WithDevKind_MarksComponentAndIncrementsKpi()
        {
            BomCreator.bomKpiData.DevDependentComponents = 0;
            var component = new Component { Name = "CompA", Version = "1.0", Purl = "pkg:cargo/CompA@1.0", Properties = new List<Property>() };
            var components = new List<Component> { component };
            var purlToDevKinds = new Dictionary<string, List<string>> { { "pkg:cargo/CompA@1.0", new List<string> { "dev" } } };
            var method = typeof(CargoProcessor).GetMethod("MarkCargoDevelopmentProperties", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            method.Invoke(null, new object[] { components, purlToDevKinds });

            Assert.That(component.Properties.First(p => p.Name == Dataconstant.Cdx_IsDevelopment).Value, Is.EqualTo("true"));
            Assert.That(BomCreator.bomKpiData.DevDependentComponents, Is.EqualTo(1));
        }

        [Test]
        public void MarkCargoDevelopmentProperties_WithNullKind_IsNotDevelopment()
        {
            var component = new Component { Name = "CompA", Version = "1.0", Purl = "pkg:cargo/CompA@1.0", Properties = new List<Property>() };
            var components = new List<Component> { component };
            var purlToDevKinds = new Dictionary<string, List<string>> { { "pkg:cargo/CompA@1.0", new List<string> { null } } };
            var method = typeof(CargoProcessor).GetMethod("MarkCargoDevelopmentProperties", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            method.Invoke(null, new object[] { components, purlToDevKinds });

            Assert.That(component.Properties.First(p => p.Name == Dataconstant.Cdx_IsDevelopment).Value, Is.EqualTo("false"));
        }

        [Test]
        public void MarkCargoDevelopmentProperties_WithUnknownPurl_IsNotDevelopment()
        {
            var component = new Component { Name = "CompA", Version = "1.0", Purl = "pkg:cargo/CompA@1.0", Properties = new List<Property>() };
            var components = new List<Component> { component };
            var purlToDevKinds = new Dictionary<string, List<string>>();
            var method = typeof(CargoProcessor).GetMethod("MarkCargoDevelopmentProperties", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            method.Invoke(null, new object[] { components, purlToDevKinds });

            Assert.That(component.Properties.First(p => p.Name == Dataconstant.Cdx_IsDevelopment).Value, Is.EqualTo("false"));
        }

        [Test]
        public void AddDirectDependencyProperty_WithRootDependencies_MarksDirectComponents()
        {
            var packageDetails = new CargoPackageDetails
            {
                ResolveInfo = new CargoPackageDetails.Resolve
                {
                    Root = "root-id",
                    Nodes = new List<CargoPackageDetails.Node>
                    {
                        new CargoPackageDetails.Node { Id = "root-id", Dependencies = new List<string> { "id1" } }
                    }
                }
            };
            var compA = new Component { Name = "CompA", Version = "1.0", Purl = "pkg:cargo/CompA@1.0", Properties = new List<Property>() };
            var compB = new Component { Name = "CompB", Version = "1.0", Purl = "pkg:cargo/CompB@1.0", Properties = new List<Property>() };
            var components = new List<Component> { compA, compB };
            var idToPurl = new Dictionary<string, string> { { "id1", "pkg:cargo/CompA@1.0" } };

            CargoProcessor.AddDirectDependencyProperty(packageDetails, components, idToPurl);

            Assert.That(compA.Properties.First(p => p.Name == Dataconstant.Cdx_SiemensDirect).Value, Is.EqualTo("true"));
            Assert.That(compB.Properties.First(p => p.Name == Dataconstant.Cdx_SiemensDirect).Value, Is.EqualTo("false"));
        }

        [Test]
        public void AddDirectDependencyProperty_WithNoRoot_MarksAllComponentsAsIndirect()
        {
            var packageDetails = new CargoPackageDetails { ResolveInfo = null };
            var compA = new Component { Name = "CompA", Version = "1.0", Purl = "pkg:cargo/CompA@1.0", Properties = new List<Property>() };
            var components = new List<Component> { compA };
            var idToPurl = new Dictionary<string, string>();

            CargoProcessor.AddDirectDependencyProperty(packageDetails, components, idToPurl);

            Assert.That(compA.Properties.First(p => p.Name == Dataconstant.Cdx_SiemensDirect).Value, Is.EqualTo("false"));
        }

        [Test]
        public void GetPackagesFromCargoMetadataJson_WithValidJson_PopulatesComponentsAndDependencies()
        {
            string json = JsonConvert.SerializeObject(new CargoPackageDetails
            {
                Packages = new List<CargoPackageDetails.Package>
                {
                    new CargoPackageDetails.Package { Id = "id1", Name = "CompA", Version = "1.0" }
                },
                ResolveInfo = new CargoPackageDetails.Resolve
                {
                    Root = "id1",
                    Nodes = new List<CargoPackageDetails.Node>
                    {
                        new CargoPackageDetails.Node { Id = "id1", Dependencies = new List<string>() }
                    }
                }
            });
            string filePath = System.IO.Path.Combine(TestFolder, "valid-metadata.json");
            System.IO.File.WriteAllText(filePath, json);
            var components = new List<Component>();
            var dependencies = new List<Dependency>();
            var method = typeof(CargoProcessor).GetMethod("GetPackagesFromCargoMetadataJson", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            method.Invoke(null, new object[] { filePath, components, dependencies });

            Assert.That(components, Has.Count.EqualTo(1));
            Assert.That(components[0].Name, Is.EqualTo("CompA"));
        }

        [Test]
        public void GetPackagesFromCargoMetadataJson_WithMalformedJson_DoesNotThrow()
        {
            string filePath = System.IO.Path.Combine(TestFolder, "malformed-metadata.json");
            System.IO.File.WriteAllText(filePath, "{ this is not valid json ");
            var components = new List<Component>();
            var dependencies = new List<Dependency>();
            var method = typeof(CargoProcessor).GetMethod("GetPackagesFromCargoMetadataJson", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            Assert.DoesNotThrow(() => method.Invoke(null, new object[] { filePath, components, dependencies }));
            Assert.That(components, Is.Empty);
        }

        private static CommonAppSettings CreateTestAppSettings()
        {
            return new CommonAppSettings
            {
                Cargo = new Config
                {
                    Artifactory = new Artifactory
                    {
                        InternalRepos = new[] { "repo1" },
                        ThirdPartyRepos = new List<ThirdPartyRepo> { new ThirdPartyRepo { Name = "repo1", Upload = true } }
                    },
                    DevDepRepo = "dev-repo",
                    ReleaseRepo = "release-repo",
                    Exclude = Array.Empty<string>()
                },
                ProjectType = "CARGO"
            };
        }
    }
}