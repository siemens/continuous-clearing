// Ignore Spelling: Spdx Bom LCT

using LCT.Common.Interface;
using LCT.Common.Model;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LCT.Common.UTest
{
    [TestFixture]
    public class SpdxBomParserTests
    {
        private ISpdxBomParser _spdxBomParser;
        private string _testDataDirectory;

        [SetUp]
        public void Setup()
        {
            _spdxBomParser = new SpdxBomParser();
            _testDataDirectory = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData");
            System.IO.Directory.CreateDirectory(_testDataDirectory);
        }

        [TearDown]
        public void TearDown()
        {
            if (System.IO.Directory.Exists(_testDataDirectory))
            {
                System.IO.Directory.Delete(_testDataDirectory, true);
            }
        }

        #region ParseSPDXBom Tests

        [Test]
        public void ParseSPDXBom_ValidSpdx23File_ReturnsValidBom()
        {
            // Arrange
            var spdxData = CreateValidSpdxData();
            var filePath = CreateTestFile(spdxData);

            // Act
            var result = _spdxBomParser.ParseSPDXBom(filePath);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Components, Is.Not.Null);
            Assert.That(result.Components.Count, Is.EqualTo(2));
            Assert.That(result.Dependencies, Is.Not.Null);
        }

        [Test]
        public void ParseSPDXBom_FileNotFound_ReturnsEmptyBom()
        {
            // Arrange
            var nonExistentPath = Path.Combine(_testDataDirectory, "nonexistent.json");

            // Act
            var result = _spdxBomParser.ParseSPDXBom(nonExistentPath);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Components, Is.Null.Or.Empty);
            Assert.That(result.Dependencies, Is.Null.Or.Empty);
        }

        [Test]
        public void ParseSPDXBom_InvalidJson_ReturnsEmptyBom()
        {
            // Arrange
            var filePath = Path.Combine(_testDataDirectory, "invalid.json");
            System.IO.File.WriteAllText(filePath, "{ invalid json }");

            // Act
            var result = _spdxBomParser.ParseSPDXBom(filePath);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Components, Is.Null.Or.Empty);
        }

        [Test]
        public void ParseSPDXBom_InvalidSpdxVersion_ReturnsEmptyBom()
        {
            // Arrange
            var spdxData = CreateValidSpdxData();
            spdxData.SpdxVersion = "SPDX-2.2";
            var filePath = CreateTestFile(spdxData);

            // Act
            var result = _spdxBomParser.ParseSPDXBom(filePath);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Components, Is.Null.Or.Empty);
        }

        [Test]
        public void ParseSPDXBom_NullSpdxVersion_ReturnsEmptyBom()
        {
            // Arrange
            var spdxData = CreateValidSpdxData();
            spdxData.SpdxVersion = null;
            var filePath = CreateTestFile(spdxData);

            // Act
            var result = _spdxBomParser.ParseSPDXBom(filePath);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Components, Is.Null.Or.Empty);
        }

        #endregion

        #region Component Processing Tests

        [Test]
        public void ProcessSpdxPackages_ValidPackagesWithPurl_CreatesComponents()
        {
            // Arrange
            var spdxData = CreateValidSpdxData();
            var filePath = CreateTestFile(spdxData);

            // Act
            var result = _spdxBomParser.ParseSPDXBom(filePath);

            // Assert
            Assert.That(result.Components.Count, Is.EqualTo(2));

            var component1 = result.Components.FirstOrDefault(c => c.Name == "TestPackage1");
            Assert.That(component1, Is.Not.Null);
            Assert.That(component1.Version, Is.EqualTo("1.0.0"));
            Assert.That(component1.Purl, Is.EqualTo("pkg:nuget/TestPackage1@1.0.0"));

            var component2 = result.Components.FirstOrDefault(c => c.Name == "TestPackage2");
            Assert.That(component2, Is.Not.Null);
            Assert.That(component2.Version, Is.EqualTo("2.0.0"));
            Assert.That(component2.Purl, Is.EqualTo("pkg:nuget/TestPackage2@2.0.0"));
        }

        [Test]
        public void ProcessSpdxPackages_PackageWithoutExternalRefs_SkipsPackage()
        {
            // Arrange
            var spdxData = new SpdxBomData
            {
                SpdxVersion = "SPDX-2.3",
                Packages = new List<Package>
                {
                    new Package
                    {
                        SPDXID = "SPDXRef-Package-1",
                        Name = "TestPackage1",
                        VersionInfo = "1.0.0",
                        ExternalRefs = null // No external references
                    }
                }
            };
            var filePath = CreateTestFile(spdxData);

            // Act
            var result = _spdxBomParser.ParseSPDXBom(filePath);

            // Assert
            Assert.That(result.Components, Is.Empty);
        }

        [Test]
        public void ProcessSpdxPackages_PackageWithoutPurlRef_SkipsPackage()
        {
            // Arrange
            var spdxData = new SpdxBomData
            {
                SpdxVersion = "SPDX-2.3",
                Packages = new List<Package>
                {
                    new Package
                    {
                        SPDXID = "SPDXRef-Package-1",
                        Name = "TestPackage1",
                        VersionInfo = "1.0.0",
                        ExternalRefs = new List<ExternalRef>
                        {
                            new ExternalRef
                            {
                                ReferenceCategory = "OTHER",
                                ReferenceType = "website",
                                ReferenceLocator = "https://example.com"
                            }
                        }
                    }
                }
            };
            var filePath = CreateTestFile(spdxData);

            // Act
            var result = _spdxBomParser.ParseSPDXBom(filePath);

            // Assert
            Assert.That(result.Components, Is.Empty);
        }

        #endregion

        #region Relationship Processing Tests

        [Test]
        public void ProcessSpdxRelationships_ValidDependencyOfRelationship_CreatesDependencies()
        {
            // Arrange
            var spdxData = CreateSpdxDataWithRelationships();
            var filePath = CreateTestFile(spdxData);

            // Act
            var result = _spdxBomParser.ParseSPDXBom(filePath);

            // Assert
            Assert.That(result.Dependencies, Is.Not.Null);
            Assert.That(result.Dependencies.Count, Is.GreaterThan(0));

            var dependency = result.Dependencies.FirstOrDefault(d => d.Ref == "pkg:nuget/TestPackage2@2.0.0");
            Assert.That(dependency, Is.Not.Null);
            Assert.That(dependency.Dependencies.Count, Is.EqualTo(1));
            Assert.That(dependency.Dependencies[0].Ref, Is.EqualTo("pkg:nuget/TestPackage1@1.0.0"));
        }

        [Test]
        public void ProcessSpdxRelationships_NoRelationships_ReturnsEmptyDependencies()
        {
            // Arrange
            var spdxData = CreateValidSpdxData();
            spdxData.Relationships = null;
            var filePath = CreateTestFile(spdxData);

            // Act
            var result = _spdxBomParser.ParseSPDXBom(filePath);

            // Assert
            Assert.That(result.Dependencies, Is.Empty);
        }

        [Test]
        public void ProcessSpdxRelationships_UnsupportedRelationshipType_SkipsRelationship()
        {
            // Arrange
            var spdxData = CreateValidSpdxData();
            spdxData.Relationships = new List<Relationship>
            {
                new Relationship
                {
                    SpdxElementId = "SPDXRef-Package-1",
                    RelationshipType = "CONTAINS", // Unsupported type
                    RelatedSpdxElement = "SPDXRef-Package-2"
                }
            };
            var filePath = CreateTestFile(spdxData);

            // Act
            var result = _spdxBomParser.ParseSPDXBom(filePath);

            // Assert
            Assert.That(result.Dependencies, Is.Empty);
        }

        [Test]
        public void ProcessSpdxRelationships_DevDependencyOfRelationship_ProcessesCorrectly()
        {
            // Arrange
            var spdxData = CreateValidSpdxData();
            spdxData.Relationships = new List<Relationship>
            {
                new Relationship
                {
                    SpdxElementId = "SPDXRef-Package-1",
                    RelationshipType = "DEV_DEPENDENCY_OF",
                    RelatedSpdxElement = "SPDXRef-Package-2"
                }
            };
            var filePath = CreateTestFile(spdxData);

            // Act
            var result = _spdxBomParser.ParseSPDXBom(filePath);

            // Assert
            var dependency = result.Dependencies.FirstOrDefault(d => d.Ref == "pkg:nuget/TestPackage2@2.0.0");
            Assert.That(dependency, Is.Not.Null);
        }

        [Test]
        public void ProcessSpdxRelationships_RuntimeDependencyOfRelationship_ProcessesCorrectly()
        {
            // Arrange
            var spdxData = CreateValidSpdxData();
            spdxData.Relationships = new List<Relationship>
            {
                new Relationship
                {
                    SpdxElementId = "SPDXRef-Package-1",
                    RelationshipType = "RUNTIME_DEPENDENCY_OF",
                    RelatedSpdxElement = "SPDXRef-Package-2"
                }
            };
            var filePath = CreateTestFile(spdxData);

            // Act
            var result = _spdxBomParser.ParseSPDXBom(filePath);

            // Assert
            var dependency = result.Dependencies.FirstOrDefault(d => d.Ref == "pkg:nuget/TestPackage2@2.0.0");
            Assert.That(dependency, Is.Not.Null);
        }

        #endregion

        #region Manufacturer Cleanup Tests

        [Test]
        public void CleanupComponentManufacturerData_ComponentsWithManufacturer_SetsManufacturerToNull()
        {
            // Arrange
            var spdxData = CreateValidSpdxData();
            var filePath = CreateTestFile(spdxData);

            // Act
            var result = _spdxBomParser.ParseSPDXBom(filePath);

            // Assert
            Assert.That(result.Components.All(c => c.Manufacturer == null), Is.True);
        }

        #endregion

        #region Edge Cases

        [Test]
        public void ParseSPDXBom_EmptyPackagesList_ReturnsEmptyBom()
        {
            // Arrange
            var spdxData = new SpdxBomData
            {
                SpdxVersion = "SPDX-2.3",
                Packages = new List<Package>()
            };
            var filePath = CreateTestFile(spdxData);

            // Act
            var result = _spdxBomParser.ParseSPDXBom(filePath);

            // Assert
            Assert.That(result.Components, Is.Empty);
            Assert.That(result.Dependencies, Is.Empty);
        }

        [Test]
        public void ParseSPDXBom_NullPackages_ReturnsEmptyBom()
        {
            // Arrange
            var spdxData = new SpdxBomData
            {
                SpdxVersion = "SPDX-2.3",
                Packages = null
            };
            var filePath = CreateTestFile(spdxData);

            // Act
            var result = _spdxBomParser.ParseSPDXBom(filePath);

            // Assert
            Assert.That(result.Components, Is.Null.Or.Empty);
        }

        [Test]
        public void ParseSPDXBom_LargeSpdxFile_ProcessesSuccessfully()
        {
            // Arrange
            var spdxData = CreateLargeSpdxData(100);
            var filePath = CreateTestFile(spdxData);

            // Act
            var result = _spdxBomParser.ParseSPDXBom(filePath);

            // Assert
            Assert.That(result.Components.Count, Is.EqualTo(100));
        }

        #endregion

        #region Helper Methods

        private SpdxBomData CreateValidSpdxData()
        {
            return new SpdxBomData
            {
                SpdxVersion = "SPDX-2.3",
                Packages = new List<Package>
                {
                    new Package
                    {
                        SPDXID = "SPDXRef-Package-1",
                        Name = "TestPackage1",
                        VersionInfo = "1.0.0",
                        ExternalRefs = new List<ExternalRef>
                        {
                            new ExternalRef
                            {
                                ReferenceCategory = "PACKAGE-MANAGER",
                                ReferenceType = "purl",
                                ReferenceLocator = "pkg:nuget/TestPackage1@1.0.0"
                            }
                        }
                    },
                    new Package
                    {
                        SPDXID = "SPDXRef-Package-2",
                        Name = "TestPackage2",
                        VersionInfo = "2.0.0",
                        ExternalRefs = new List<ExternalRef>
                        {
                            new ExternalRef
                            {
                                ReferenceCategory = "PACKAGE-MANAGER",
                                ReferenceType = "purl",
                                ReferenceLocator = "pkg:nuget/TestPackage2@2.0.0"
                            }
                        }
                    }
                }
            };
        }

        private SpdxBomData CreateSpdxDataWithRelationships()
        {
            var spdxData = CreateValidSpdxData();
            spdxData.Relationships = new List<Relationship>
            {
                new Relationship
                {
                    SpdxElementId = "SPDXRef-Package-1",
                    RelationshipType = "DEPENDENCY_OF",
                    RelatedSpdxElement = "SPDXRef-Package-2"
                }
            };
            return spdxData;
        }

        private SpdxBomData CreateLargeSpdxData(int packageCount)
        {
            var packages = new List<Package>();
            for (int i = 1; i <= packageCount; i++)
            {
                packages.Add(new Package
                {
                    SPDXID = $"SPDXRef-Package-{i}",
                    Name = $"TestPackage{i}",
                    VersionInfo = $"{i}.0.0",
                    ExternalRefs = new List<ExternalRef>
                    {
                        new ExternalRef
                        {
                            ReferenceCategory = "PACKAGE-MANAGER",
                            ReferenceType = "purl",
                            ReferenceLocator = $"pkg:nuget/TestPackage{i}@{i}.0.0"
                        }
                    }
                });
            }

            return new SpdxBomData
            {
                SpdxVersion = "SPDX-2.3",
                Packages = packages
            };
        }

        private string CreateTestFile(SpdxBomData spdxData)
        {
            var fileName = $"test_{Guid.NewGuid()}.json";
            var filePath = Path.Combine(_testDataDirectory, fileName);
            var json = JsonConvert.SerializeObject(spdxData, Formatting.Indented);
            System.IO.File.WriteAllText(filePath, json);
            return filePath;
        }

        #endregion
    }
}
