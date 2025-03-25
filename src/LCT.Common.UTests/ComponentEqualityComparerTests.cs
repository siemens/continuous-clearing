// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models;
using NUnit.Framework;

namespace LCT.Common.UTest
{
    [TestFixture]
    public class ComponentEqualityComparerTests
    {
        [SetUp]
        public void Setup()
        {
            // Implement
        }
        [Test]
        public void Equals_WhenComponentNameAndVersionAreSame_ReturnsTrue()
        {
            Component componentA = new Component()
            {
                Name = "Test",
                Version = "1"
            };
            Component componentB = new Component()
            {
                Name = "Test",
                Version = "1"
            };


            var componentEqualityComparer = new ComponentEqualityComparer();
            var expected = componentEqualityComparer.Equals(componentA, componentB);
            Assert.True(expected);
        }
        [Test]
        public void Equals_WhenComponentNameAndVersionAreDifferent_ReturnsTrue()
        {
            Component componentA = new Component()
            {
                Name = "Test",
                Version = ""
            };
            Component componentB = new Component()
            {
                Name = "Test",
                Version = "1"
            };


            var componentEqualityComparer = new ComponentEqualityComparer();
            var expected = componentEqualityComparer.Equals(componentA, componentB);
            Assert.IsFalse(expected);
        }

        [Test]
        public void Equals_WhenBothComponentsAreNull_ReturnsTrue()
        {
            Component componentA = null;
            Component componentB = null;

            var componentEqualityComparer = new ComponentEqualityComparer();
            var expected = componentEqualityComparer.Equals(componentA, componentB);
            Assert.True(expected);
        }

        [Test]
        public void Equals_WhenOneComponentIsNull_ReturnsFalse()
        {
            Component componentA = new Component()
            {
                Name = "Test",
                Version = "1"
            };
            Component componentB = null;

            var componentEqualityComparer = new ComponentEqualityComparer();
            var expected = componentEqualityComparer.Equals(componentA, componentB);
            Assert.False(expected);
        }

        [Test]
        public void Equals_WhenComponentsHaveDifferentNames_ReturnsFalse()
        {
            Component componentA = new Component()
            {
                Name = "Test1",
                Version = "1"
            };
            Component componentB = new Component()
            {
                Name = "Test2",
                Version = "1"
            };

            var componentEqualityComparer = new ComponentEqualityComparer();
            var expected = componentEqualityComparer.Equals(componentA, componentB);
            Assert.False(expected);
        }

        [Test]
        public void Equals_WhenComponentsHaveDifferentVersions_ReturnsFalse()
        {
            Component componentA = new Component()
            {
                Name = "Test",
                Version = "1"
            };
            Component componentB = new Component()
            {
                Name = "Test",
                Version = "2"
            };

            var componentEqualityComparer = new ComponentEqualityComparer();
            var expected = componentEqualityComparer.Equals(componentA, componentB);
            Assert.False(expected);
        }

        [Test]
        public void Equals_WhenComponentsHaveDifferentPurls_ReturnsFalse()
        {
            Component componentA = new Component()
            {
                Name = "Test",
                Version = "1",
                Purl = "pkg:test@1"
            };
            Component componentB = new Component()
            {
                Name = "Test",
                Version = "1",
                Purl = "pkg:test@2"
            };

            var componentEqualityComparer = new ComponentEqualityComparer();
            var expected = componentEqualityComparer.Equals(componentA, componentB);
            Assert.False(expected);
        }

        [Test]
        public void GetHashCode_ReturnsSameHashCodeForEqualComponents()
        {
            Component componentA = new Component()
            {
                Name = "Test",
                Version = "1",
                Purl = "pkg:test@1"
            };
            Component componentB = new Component()
            {
                Name = "Test",
                Version = "1",
                Purl = "pkg:test@1"
            };

            var componentEqualityComparer = new ComponentEqualityComparer();
            var hashCodeA = componentEqualityComparer.GetHashCode(componentA);
            var hashCodeB = componentEqualityComparer.GetHashCode(componentB);
            Assert.AreEqual(hashCodeA, hashCodeB);
        }

        [Test]
        public void GetHashCode_ReturnsDifferentHashCodeForDifferentComponents()
        {
            Component componentA = new Component()
            {
                Name = "Test1",
                Version = "1",
                Purl = "pkg:test@1"
            };
            Component componentB = new Component()
            {
                Name = "Test2",
                Version = "2",
                Purl = "pkg:test@2"
            };

            var componentEqualityComparer = new ComponentEqualityComparer();
            var hashCodeA = componentEqualityComparer.GetHashCode(componentA);
            var hashCodeB = componentEqualityComparer.GetHashCode(componentB);
            Assert.AreNotEqual(hashCodeA, hashCodeB);
        }
    }
}
