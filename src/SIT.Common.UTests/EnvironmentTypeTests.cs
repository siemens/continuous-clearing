// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using NUnit.Framework;
using SIT.Common.Runtime;
using System;

namespace SIT.Common.UTest
{
    [TestFixture]
    internal class EnvironmentTypeTests
    {
        [Test]
        public void EnumValues_ShouldMatchExpectedValues()
        {
            // Arrange
            var expectedValues = new[]
            {
                EnvironmentType.Unknown,
                EnvironmentType.GitLab,
                EnvironmentType.AzurePipeline,
                EnvironmentType.AzureRelease
            };

            // Act
            var actualValues = (EnvironmentType[])Enum.GetValues(typeof(EnvironmentType));

            // Assert
            Assert.That(actualValues.Length, Is.EqualTo(expectedValues.Length));
            for (int i = 0; i < expectedValues.Length; i++)
            {
                Assert.That(actualValues[i], Is.EqualTo(expectedValues[i]));
            }
        }
    }
}
