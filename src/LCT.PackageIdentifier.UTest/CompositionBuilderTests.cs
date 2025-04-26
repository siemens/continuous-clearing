// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models;
using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace LCT.PackageIdentifier.UTest
{
    [TestFixture]
    public class CompositionBuilderTests
    {
        private CompositionBuilder _builder;
        private ComponentConfig _config;
        private Bom _bom;

        [SetUp]
        public void Setup()
        {
            _config = new ComponentConfig
            {
                RuntimeName = "Test Runtime",
                RuntimePackage = "test-runtime",
                DefaultVersion = "1.0.0"
            };
            _builder = new CompositionBuilder(_config);
            _bom = new Bom();
        }

        [Test]
        public void AddCompositionsToBom_WithNullBom_ThrowsArgumentNullException()
        {
            // Arrange
            var packages = new Dictionary<string, Dictionary<string, NuGetVersion>>();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                _builder.AddCompositionsToBom(null, packages));
        }

        [Test]
        public void AddCompositionsToBom_WithNullPackages_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                _builder.AddCompositionsToBom(_bom, null));
        }

        [Test]
        public void AddCompositionsToBom_WithValidInput_CreatesCorrectCompositions()
        {
            // Arrange
            var packages = new Dictionary<string, Dictionary<string, NuGetVersion>>
            {
                ["net6.0"] = new Dictionary<string, NuGetVersion>
                {
                    ["PackageA"] = new NuGetVersion("1.0.0"),
                    ["PackageB"] = new NuGetVersion("2.0.0")
                }
            };

            // Act
            _builder.AddCompositionsToBom(_bom, packages);

            // Assert
            Assert.IsNotNull(_bom.Compositions);
            Assert.AreEqual(1, _bom.Compositions.Count);
            var composition = _bom.Compositions.First();
            Assert.AreEqual(Composition.AggregateType.Complete, composition.Aggregate);
            Assert.AreEqual(1, composition.Assemblies.Count);
            Assert.AreEqual(2, composition.Dependencies.Count);
        }

        [Test]
        public void CreateComposition_WithFrameworkVersion_GeneratesCorrectIdentifiers()
        {
            // Arrange
            var frameworkPackages = new Dictionary<string, NuGetVersion>
            {
                ["PackageA"] = new NuGetVersion("1.0.0")
            };
            var framework = new KeyValuePair<string, Dictionary<string, NuGetVersion>>(
                "net6.0", frameworkPackages);

            // Act
            var composition = _builder.GetType()
                .GetMethod("CreateComposition", System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance)
                .Invoke(_builder, new object[] { framework }) as Composition;

            // Assert
            Assert.IsNotNull(composition);
            Assert.IsTrue(composition.Assemblies.First()
                .Contains("pkg:nuget/test-runtime@6.0.0"));
        }

        [Test]
        public void CreateRuntimeComponentIdentifier_WithInvalidFrameworkMoniker_UsesDefaultVersion()
        {
            // Arrange
            var frameworkMoniker = "invalid-moniker";

            // Act
            var identifier = _builder.GetType()
                .GetMethod("CreateRuntimeComponentIdentifier",
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance)
                .Invoke(_builder, new object[] { frameworkMoniker }) as string;

            // Assert
            Assert.IsNotNull(identifier);
            Assert.IsTrue(identifier.Contains("pkg:nuget/test-runtime@1.0.0")); // Default version from config
        }


        [Test]
        [TestCase("net6.0", "6.0.0")]
        [TestCase("net5.0-windows", "5.0.0")]
        [TestCase("net4", "4.0.0")]
        [TestCase("", "1.0.0")] // Default version from config
        public void ExtractFrameworkVersion_ReturnsCorrectVersion(
            string frameworkMoniker, string expectedVersion)
        {
            // Arrange & Act
            var version = _builder.GetType()
                .GetMethod("ExtractFrameworkVersion",
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance)
                .Invoke(_builder, new object[] { frameworkMoniker }) as string;

            // Assert
            Assert.AreEqual(expectedVersion, version);
        }

        [Test]
        public void CreateDependencyIdentifiers_GeneratesCorrectPurls()
        {
            // Arrange
            var dependencies = new Dictionary<string, NuGetVersion>
            {
                ["PackageA"] = new NuGetVersion("1.0.0"),
                ["PackageB"] = new NuGetVersion("2.0.0-beta")
            };

            // Act
            var identifiers = _builder.GetType()
                .GetMethod("CreateDependencyIdentifiers",
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance)
                .Invoke(_builder, new object[] { dependencies }) as List<string>;

            // Assert
            Assert.IsNotNull(identifiers);
            Assert.AreEqual(2, identifiers.Count);
            Assert.IsTrue(identifiers.Any(i => i.Contains("PackageA@1.0.0")));
            Assert.IsTrue(identifiers.Any(i => i.Contains("PackageB@2.0.0-beta")));
        }
    }
}
