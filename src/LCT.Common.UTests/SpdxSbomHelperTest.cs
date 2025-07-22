// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models;
using LCT.Common.Constants;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LCT.Common.UTest
{
    public class SpdxSbomHelperTest
    {
        [Test]
        public void AddSpdxSBomFileNameProperty_ValidBomWithComponents_AddsFileNamePropertyToAllComponents()
        {
            // Arrange
            var testBom = new Bom
            {
                Components = new List<Component>
        {
            new Component
            {
                Name = "ComponentWithProperties",
                Properties = new List<Property>
                {
                    new Property { Name = "ExistingProperty", Value = "ExistingValue" }
                }
            },
            new Component
            {
                Name = "ComponentWithNullProperties",
                Properties = null
            },
            new Component
            {
                Name = "ComponentWithEmptyProperties",
                Properties = new List<Property>()
            }
        }
            };
            var testFilePath = Path.Combine("TestDirectory", "TestFile.spdx.json");

            // Act
            CommonHelper.AddSpdxSBomFileNameProperty(ref testBom, testFilePath);

            // Assert - All components should have the SPDX filename property
            Assert.AreEqual(3, testBom.Components.Count);
            foreach (var component in testBom.Components)
            {
                Assert.IsNotNull(component.Properties);
                var spdxProperty = component.Properties.FirstOrDefault(p => p.Name == Dataconstant.Cdx_SpdxFileName);
                Assert.IsNotNull(spdxProperty);
                Assert.AreEqual("TestFile.spdx.json", spdxProperty.Value);
            }

            // Assert - Existing properties are preserved
            var componentWithExisting = testBom.Components.First();
            Assert.AreEqual(3, componentWithExisting.Properties.Count);
            Assert.IsTrue(componentWithExisting.Properties.Any(p => p.Name == "ExistingProperty"));
        }

        [Test]
        public void AddSpdxSBomFileNameProperty_NullAndEmptyScenarios_HandlesGracefully()
        {
            // Test null BOM
            Bom nullBom = null;
            var testFilePath = Path.Combine("TestDirectory", "TestFile.spdx.json");
            Assert.DoesNotThrow(() => CommonHelper.AddSpdxSBomFileNameProperty(ref nullBom, testFilePath));

            // Test BOM with null components
            var bomWithNullComponents = new Bom { Components = null };
            Assert.DoesNotThrow(() => CommonHelper.AddSpdxSBomFileNameProperty(ref bomWithNullComponents, testFilePath));

            // Test empty components list
            var bomWithEmptyComponents = new Bom { Components = new List<Component>() };
            CommonHelper.AddSpdxSBomFileNameProperty(ref bomWithEmptyComponents, testFilePath);
            Assert.AreEqual(0, bomWithEmptyComponents.Components.Count);
        }

        [Test]
        public void AddSpdxSBomFileNameProperty_VariousFilePathFormats_ExtractsCorrectFileName()
        {
            var testCases = new[]
            {
        (Path.Combine("C:", "Complex", "Path", "ComplexFile.spdx.json"), "ComplexFile.spdx.json"),
        ("SimpleFile.json", "SimpleFile.json"),
        (Path.Combine("Folder", "FileWithoutExtension"), "FileWithoutExtension"),
        ("", ""),
        (null, null)
    };

            foreach (var (filePath, expectedFileName) in testCases)
            {
                var testBom = new Bom
                {
                    Components = new List<Component>
            {
                new Component { Name = "TestComponent", Properties = new List<Property>() }
            }
                };

                if (filePath == null)
                {
                    Assert.DoesNotThrow(() => CommonHelper.AddSpdxSBomFileNameProperty(ref testBom, filePath));
                }
                else
                {
                    CommonHelper.AddSpdxSBomFileNameProperty(ref testBom, filePath);
                    var property = testBom.Components.First().Properties.FirstOrDefault(p => p.Name == Dataconstant.Cdx_SpdxFileName);
                    Assert.IsNotNull(property);
                    Assert.AreEqual(expectedFileName, property.Value);
                }
            }
        }

        [Test]
        public void AddSpdxSBomFileNameProperty_BomComponentsReference_PreservesOriginalReference()
        {
            // Arrange
            var testBom = new Bom
            {
                Components = new List<Component>
        {
            new Component { Name = "Component1", Properties = new List<Property>() },
            new Component { Name = "Component2", Properties = new List<Property>() },
            new Component { Name = "Component3", Properties = new List<Property>() }
        }
            };
            var originalComponentsList = testBom.Components;
            var testFilePath = Path.Combine("TestDirectory", "TestFile.spdx.json");

            // Act
            CommonHelper.AddSpdxSBomFileNameProperty(ref testBom, testFilePath);

            // Assert - The BOM should still reference the same components list
            Assert.AreSame(originalComponentsList, testBom.Components);

            // Assert - bomComponentsList assignment works correctly (line coverage)
            Assert.IsNotNull(testBom.Components);
            Assert.AreEqual(3, testBom.Components.Count);
        }

        [Test]
        public void AddSpdxSBomFileNameProperty_PropertyInitialization_WorksForNullProperties()
        {
            // Arrange - Component with null properties
            var bomWithNullProps = new Bom
            {
                Components = new List<Component>
        {
            new Component { Name = "TestComponent", Properties = null }
        }
            };
            var testFilePath = Path.Combine("TestDirectory", "TestFile.spdx.json");

            // Act
            CommonHelper.AddSpdxSBomFileNameProperty(ref bomWithNullProps, testFilePath);

            // Assert - Properties should be initialized and property added
            var component = bomWithNullProps.Components.First();
            Assert.IsNotNull(component.Properties);
            Assert.AreEqual(2, component.Properties.Count);
            Assert.AreEqual(Dataconstant.Cdx_SpdxFileName, component.Properties[0].Name);
            Assert.AreEqual("TestFile.spdx.json", component.Properties[0].Value);
        }

        [Test]
        public void AddSpdxSBomFileNameProperty_FilePathWithSpecialCharacters_HandlesCorrectly()
        {
            // Arrange
            var testBom = new Bom
            {
                Components = new List<Component>
        {
            new Component { Name = "TestComponent", Properties = new List<Property>() }
        }
            };
            var specialCharPath = Path.Combine("Test Folder", "File-Name_With@Special#Characters.spdx.json");

            // Act
            CommonHelper.AddSpdxSBomFileNameProperty(ref testBom, specialCharPath);

            // Assert
            var component = testBom.Components.First();
            var spdxFileNameProperty = component.Properties.FirstOrDefault(p => p.Name == Dataconstant.Cdx_SpdxFileName);
            Assert.IsNotNull(spdxFileNameProperty);
            Assert.AreEqual("File-Name_With@Special#Characters.spdx.json", spdxFileNameProperty.Value);
        }

        [Test]
        public void AddSpdxSBomFileNameProperty_EmptyFilePath_HandlesCorrectly()
        {
            // Arrange
            var testBom = new Bom
            {
                Components = new List<Component>
        {
            new Component { Name = "TestComponent", Properties = new List<Property>() }
        }
            };
            var emptyFilePath = string.Empty;

            // Act
            CommonHelper.AddSpdxSBomFileNameProperty(ref testBom, emptyFilePath);

            // Assert
            var component = testBom.Components.First();
            var spdxFileNameProperty = component.Properties.FirstOrDefault(p => p.Name == Dataconstant.Cdx_SpdxFileName);
            Assert.IsNotNull(spdxFileNameProperty);
            Assert.AreEqual(string.Empty, spdxFileNameProperty.Value);
        }

        [Test]
        public void AddSpdxSBomFileNameProperty_MultipleCallsWithSameFile_AddsPropertyEachTime()
        {
            // Arrange
            var testBom = new Bom
            {
                Components = new List<Component>
        {
            new Component { Name = "TestComponent", Properties = new List<Property>() }
        }
            };
            var testFilePath = Path.Combine("TestDirectory", "TestFile.spdx.json");

            // Act
            CommonHelper.AddSpdxSBomFileNameProperty(ref testBom, testFilePath);
            CommonHelper.AddSpdxSBomFileNameProperty(ref testBom, testFilePath);

            // Assert
            var component = testBom.Components.First();
            var spdxFileNameProperties = component.Properties.Where(p => p.Name == Dataconstant.Cdx_SpdxFileName);
            Assert.AreEqual(1, spdxFileNameProperties.Count());
        }

        [Test]
        public void AddSpdxSBomFileNameProperty_LargeNumberOfComponents_ProcessesAllComponents()
        {
            // Arrange
            var largeComponentList = new List<Component>();
            for (int i = 0; i < 100; i++)
            {
                largeComponentList.Add(new Component
                {
                    Name = $"Component{i}",
                    Version = "1.0.0",
                    Properties = new List<Property>()
                });
            }

            var largeBom = new Bom { Components = largeComponentList };
            var testFilePath = Path.Combine("TestDirectory", "TestFile.spdx.json");

            // Act
            CommonHelper.AddSpdxSBomFileNameProperty(ref largeBom, testFilePath);

            // Assert
            Assert.AreEqual(100, largeBom.Components.Count);
            foreach (var component in largeBom.Components)
            {
                var spdxFileNameProperty = component.Properties.FirstOrDefault(p => p.Name == Dataconstant.Cdx_SpdxFileName);
                Assert.IsNotNull(spdxFileNameProperty);
                Assert.AreEqual("TestFile.spdx.json", spdxFileNameProperty.Value);
            }
        }
    }
}
