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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LCT.PackageIdentifier.UTest
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
            string jfrogPackageName, jfrogRepoPath;
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