// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
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
    }
}
