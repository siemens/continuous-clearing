// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// --------------------------------------------------------------------------------------------------------------------
using NUnit.Framework;
using Newtonsoft.Json.Linq;
using LCT.PackageIdentifier;

namespace LCT.PackageIdentifier.Tests
{
    [TestFixture]
    public class LicenseInfoExtractorTests
    {
        private LicenseInfoExtractor _extractor;

        [SetUp]
        public void Setup()
        {
            _extractor = new LicenseInfoExtractor();
        }

        [Test]
        public void ExtractLicense_NpmLicenseField_ReturnsLicenseId()
        {
            var metadata = JObject.Parse("{ 'license': 'Apache-2.0' }");
            var result = _extractor.ExtractLicense("npm", metadata);
            Assert.AreEqual("Apache-2.0", result);
        }

        [Test]
        public void ExtractLicense_NpmLicensesArray_ReturnsCommaSeparatedLicenses()
        {
            var metadata = JObject.Parse("{ 'licenses': ['MIT', 'BSD'] }");
            var result = _extractor.ExtractLicense("npm", metadata);
            Assert.AreEqual("MIT, BSD", result);
        }

        [Test]
        public void ExtractLicense_NugetLicenseUrl_ReturnsLicenseUrl()
        {
            var metadata = JObject.Parse("{ 'LicenseUrl': 'http://example.com/license' }");
            var result = _extractor.ExtractLicense("nuget", metadata);
            Assert.AreEqual("http://example.com/license", result);
        }

        [Test]
        public void ExtractLicense_NugetLicenseExpression_ReturnsLicenseExpression()
        {
            var metadata = JObject.Parse("{ 'LicenseExpression': 'MIT' }");
            var result = _extractor.ExtractLicense("nuget", metadata);
            Assert.AreEqual("MIT", result);
        }

        [Test]
        public void ExtractLicense_UnknownType_ReturnsNotFound()
        {
            var metadata = JObject.Parse("{ 'license': 'Apache-2.0' }");
            var result = _extractor.ExtractLicense("unknown", metadata);
            Assert.AreEqual("Not Found", result);
        }

        [Test]
        public void ExtractLicense_NullMetadata_ReturnsNotFound()
        {
            var result = _extractor.ExtractLicense("npm", null);
            Assert.AreEqual("Not Found", result);
        }
    }
}
