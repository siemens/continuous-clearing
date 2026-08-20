// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2026 Siemens AG
//
//  SPDX-License-Identifier: MIT
// --------------------------------------------------------------------------------------------------------------------

using NUnit.Framework;

namespace SIT.Common.UTest
{
    [TestFixture]
    public class RegexHelperTests
    {
        [TestCase("1.2.3", ExpectedResult = true)]
        [TestCase("1.2.3+build.1", ExpectedResult = true)]
        [TestCase("1.2.3-alpha", ExpectedResult = false)]
        [TestCase("1.2.3-alpha.1", ExpectedResult = false)]
        [TestCase("1.2.3-hf1.0", ExpectedResult = true)]
        [TestCase("1.2.3-HF2.1", ExpectedResult = true)]
        [TestCase("1.2.3-sp3.0", ExpectedResult = true)]
        [TestCase("1.2.3-SP3.0", ExpectedResult = true)]
        [TestCase("", ExpectedResult = false)]
        [TestCase(null, ExpectedResult = false)]
        public bool IsReleaseVersion_VariousInputs_ReturnsExpected(string version)
        {
            return RegexHelper.IsReleaseVersion(version);
        }
    }
}
