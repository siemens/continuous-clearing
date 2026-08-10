// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2026 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models;
using Moq;
using NUnit.Framework;
using SIT.Common;
using SIT.Common.Constants;
using SIT.Common.Interface;
using SIT.Common.Model;
using SIT.Scan.Model;
using System.Collections.Generic;
using System.IO;

namespace SIT.Scan.UTest
{

    [TestFixture]
    public class NPMParserTests
    {
        private static Bom ListUnsupportedComponentsForBom = new Bom { Components = new List<Component>(), Dependencies = new List<Dependency>() };
        [Test]
        public void ParsePackageFile_PackageLockWithDuplicateComponents_ReturnsCountOfDuplicates()
        {
            //Arrange
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string filepath = Path.GetFullPath(Path.Combine(outFolder, "SITScanUTTestFiles", "TestDir"));
            string[] Includes = { "p*-lock.json" };
            string[] Excludes = { "node_modules" };

            CommonAppSettings appSettings = new CommonAppSettings()
            {
                ProjectType = "NPM",
                Npm = new Config() { Include = Includes, Exclude = Excludes },
                SW360 = new SW360() { IgnoreDevDependency = true },
                Directory = new SIT.Common.Directory()
                {
                    InputFolder = filepath,
                    OutputFolder = outFolder
                }
            };

            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();
            Mock<ISpdxBomParser> spdxBomParser = new Mock<ISpdxBomParser>();
            NpmProcessor NpmProcessor = new NpmProcessor(cycloneDXBomParser.Object, spdxBomParser.Object);

            //Act
            NpmProcessor.ParsePackageFile(appSettings, ref ListUnsupportedComponentsForBom);

            //Assert
            Assert.That(2974, Is.EqualTo(BomCreator.bomKpiData.DuplicateComponents), "Returns the count of duplicate components");

        }
        [Test]
        public void ParsePackageFile_PackageLockWithangular16_ReturnsCountOfComponents()
        {
            //Arrange
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string filepath = Path.GetFullPath(Path.Combine(outFolder, "SITScanUTTestFiles"));
            string[] Includes = { "p*-lock16.json" };
            string[] Excludes = { "node_modules" };

            CommonAppSettings appSettings = new CommonAppSettings()
            {
                ProjectType = "NPM",
                Npm = new Config() { Include = Includes, Exclude = Excludes },
                SW360 = new SW360() { IgnoreDevDependency = true },
                Directory = new SIT.Common.Directory()
                {
                    InputFolder = filepath,
                    OutputFolder = outFolder
                }
            };
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();
            Mock<ISpdxBomParser> spdxBomParser = new Mock<ISpdxBomParser>();
            NpmProcessor NpmProcessor = new NpmProcessor(cycloneDXBomParser.Object, spdxBomParser.Object);

            //Act
            Bom bom = NpmProcessor.ParsePackageFile(appSettings, ref ListUnsupportedComponentsForBom);

            //Assert
            Assert.That(10, Is.EqualTo(bom.Components.Count), "Returns the count of components");
            Assert.That(6, Is.EqualTo(bom.Dependencies.Count), "Returns the count of dependencies");

        }

        [Test]
        public void ParsePackageFile_PackageLockWithoutDuplicateComponents_ReturnsCountZeroDuplicates()
        {
            //Arrange
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string filepath = Path.GetFullPath(Path.Combine(outFolder, "SITScanUTTestFiles", "TestDir", "DupDir"));
            string[] Includes = { "p*-lock.json" };
            string[] Excludes = { "node_modules" };
            BomKpiData bomKpiData = new BomKpiData();

            CommonAppSettings appSettings = new CommonAppSettings()
            {
                ProjectType = "NPM",
                Npm = new Config() { Include = Includes, Exclude = Excludes },
                SW360 = new SW360() { IgnoreDevDependency = true },
                Directory = new SIT.Common.Directory()
                {
                    InputFolder = filepath,
                    OutputFolder = outFolder
                }

            };

            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();
            Mock<ISpdxBomParser> spdxBomParser = new Mock<ISpdxBomParser>();
            NpmProcessor NpmProcessor = new NpmProcessor(cycloneDXBomParser.Object, spdxBomParser.Object);

            //Act
            NpmProcessor.ParsePackageFile(appSettings, ref ListUnsupportedComponentsForBom);

            //Assert
            Assert.That(0, Is.EqualTo(bomKpiData.DuplicateComponents), "Returns the count of duplicate components as zero");
        }

        [Test]
        public void ParseCycloneDXFile_GivenMultipleInputFiles_ReturnsCounts()
        {
            //Arrange
            int expectednoofcomponents = 5;
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();
            Mock<ISpdxBomParser> spdxBomParser = new Mock<ISpdxBomParser>();
            NpmProcessor npmProcessor = new NpmProcessor(cycloneDXBomParser.Object, spdxBomParser.Object);
            string[] Includes = { "*_NPM.cdx.json" };

            CommonAppSettings appSettings = new CommonAppSettings()
            {
                ProjectType = "NPM",
                Npm = new Config() { Include = Includes },
                SW360 = new SW360() { IgnoreDevDependency = true },
                Directory = new SIT.Common.Directory()
                {
                    InputFolder = Path.GetFullPath(Path.Combine(outFolder, "SITScanUTTestFiles")),
                    OutputFolder = outFolder
                }
            };

            //Act
            Bom listofcomponents = npmProcessor.ParsePackageFile(appSettings, ref ListUnsupportedComponentsForBom);

            //Assert
            Assert.That(expectednoofcomponents, Is.EqualTo(listofcomponents.Components.Count), "Checks for no of components");
        }

        [Test]
        public void ParseCycloneDXFile_GivenAInputFilePathAlongWithSBOMTemplate_ReturnTotalComponentsList()
        {
            //Arrange
            int expectednoofcomponents = 3;
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();
            Mock<ISpdxBomParser> spdxBomParser = new Mock<ISpdxBomParser>();
            NpmProcessor npmProcessor = new NpmProcessor(cycloneDXBomParser.Object, spdxBomParser.Object);
            string[] Includes = { "CycloneDX2_NPM.cdx.json", "SBOMTemplate_Npm.cdx.json", "SBOM_NpmCATemplate.cdx.json" };
            string packagefilepath = Path.GetFullPath(Path.Combine(outFolder, "SITScanUTTestFiles"));

            CommonAppSettings appSettings = new CommonAppSettings()
            {
                ProjectType = "NPM",
                Npm = new Config() { Include = Includes },
                SW360 = new SW360() { IgnoreDevDependency = true },
                Directory = new SIT.Common.Directory()
                {
                    InputFolder = packagefilepath,
                    OutputFolder = outFolder,

                }
            };

            //Act
            Bom listofcomponents = npmProcessor.ParsePackageFile(appSettings, ref ListUnsupportedComponentsForBom);

            //Assert
            Assert.That(expectednoofcomponents, Is.EqualTo(listofcomponents.Components.Count), "Checks for no of components");
        }

        [Test]
        public void ParseCycloneDXFile_GivenAInputFilePathAlongWithSBOMTemplate_ReturnUpdatedComponents()
        {
            //Arrange
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();
            Mock<ISpdxBomParser> spdxBomParser = new Mock<ISpdxBomParser>();
            NpmProcessor npmProcessor = new NpmProcessor(cycloneDXBomParser.Object, spdxBomParser.Object);
            string[] Includes = { "CycloneDX2_NPM.cdx.json" };
            string packagefilepath = Path.GetFullPath(Path.Combine(outFolder, "SITScanUTTestFiles"));

            CommonAppSettings appSettings = new CommonAppSettings()
            {
                ProjectType = "NPM",
                Npm = new Config() { Include = Includes },
                SW360 = new SW360() { IgnoreDevDependency = true },
                Directory = new SIT.Common.Directory()
                {
                    InputFolder = packagefilepath,
                    OutputFolder = outFolder,

                }
            };

            //Act
            Bom listofcomponents = npmProcessor.ParsePackageFile(appSettings, ref ListUnsupportedComponentsForBom);

            bool isUpdated = listofcomponents.Components.Exists(x => x.Properties != null && x.Properties.Exists(x => x.Name == Dataconstant.Cdx_IdentifierType && x.Value == Dataconstant.ManullayAdded));

            //Assert
            Assert.IsTrue(isUpdated, "Checks For Updated Property In List ");
        }

        [Test]
        public void ParsePackageLockJson_PackagesWithDevAndOptionalFlags_AddsOptionalDevDependencyOnlyWhenBothTrue()
        {
            // Arrange - build a v3-format package-lock.json in memory covering all flag combinations.
            string tempDir = Path.Combine(Path.GetTempPath(), "SITScanNpm_OptDev_" + System.Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(tempDir);
            string lockPath = Path.Combine(tempDir, "package-lock.json");
            string json = @"{
              ""name"": ""optdev-fixture"",
              ""version"": ""1.0.0"",
              ""lockfileVersion"": 3,
              ""requires"": true,
              ""packages"": {
                """": { ""name"": ""optdev-fixture"", ""version"": ""1.0.0"" },
                ""node_modules/pkg-plain"":       { ""version"": ""1.0.0"" },
                ""node_modules/pkg-dev"":         { ""version"": ""1.0.0"", ""dev"": true },
                ""node_modules/pkg-optional"":    { ""version"": ""1.0.0"", ""optional"": true },
                ""node_modules/pkg-devoptional"": { ""version"": ""1.0.0"", ""devOptional"": true },
                ""node_modules/pkg-dev-and-opt"": { ""version"": ""1.0.0"", ""dev"": true, ""optional"": true }
              }
            }";
            System.IO.File.WriteAllText(lockPath, json);

            CommonAppSettings appSettings = new CommonAppSettings
            {
                ProjectType = "NPM",
                SW360 = new SW360 { IgnoreDevDependency = false }
            };

            try
            {
                // Act
                List<Component> components = NpmProcessor.ParsePackageLockJson(lockPath, appSettings);

                // Assert - property present ONLY on pkg-dev-and-opt.
                Assert.That(HasOptionalDevProperty(components, "pkg-dev-and-opt"), Is.True,
                    "pkg-dev-and-opt has dev+optional; must have optional-dev-dependency=true");

                Assert.That(HasOptionalDevProperty(components, "pkg-plain"), Is.False,
                    "pkg-plain has no flags; must not have optional-dev-dependency");
                Assert.That(HasOptionalDevProperty(components, "pkg-dev"), Is.False,
                    "pkg-dev is dev-only; must not have optional-dev-dependency");
                Assert.That(HasOptionalDevProperty(components, "pkg-optional"), Is.False,
                    "pkg-optional is optional-only; must not have optional-dev-dependency");
                Assert.That(HasOptionalDevProperty(components, "pkg-devoptional"), Is.False,
                    "pkg-devoptional alone (without dev+optional) must not have optional-dev-dependency");
            }
            finally
            {
                try { System.IO.Directory.Delete(tempDir, true); } catch { /* best-effort cleanup */ }
            }
        }

        private static bool HasOptionalDevProperty(List<Component> components, string name)
        {
            Component comp = components.Find(c => c.Name == name);
            if (comp?.Properties == null)
            {
                return false;
            }
            return comp.Properties.Exists(p =>
                p.Name == Dataconstant.Cdx_OptionalDevDependency && p.Value == "true");
        }
    }
}
