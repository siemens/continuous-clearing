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

            var method = typeof(CargoProcessor).GetMethod("GetDetailsforManuallyAddedComp", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.NotNull(method, "Could not find GetDetailsforManuallyAddedComp method via reflection.");

            method.Invoke(null, new object[] { components });

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