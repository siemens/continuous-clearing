// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 


using LCT.SW360PackageCreator.Interfaces;
using NUnit.Framework;
using System.Threading.Tasks;

namespace LCT.SW360PackageCreator.UTest
{
    /// <summary>
    /// The test class for UrlHelper
    /// </summary>
    [TestFixture]
    public class UrlHelperTest
    {
        [Test]
        [TestCase("@angular-devkit/architect", "0.901.3")]
        public void GetSourceUrlForNpmPackage_ProvidedPackageDetails_ReturnsValidSourceURL(string componentName, string version)
        {
            // Arrange
            IUrlHelper urlHelper = new UrlHelper();

            // Act

            string sourceUrl = urlHelper.GetSourceUrlForNpmPackage(componentName, version);

            // Assert

            Assert.That(sourceUrl, Is.EqualTo("https://github.com/angular/angular-cli/"));
        }

        [TestCase("CefSharp.Common", "100.0.140")]
        public async Task GetSourceUrlForNugetPackage_ProvidedPackageDetails_ReturnsValidSourceURL(string componentName, string version)
        {
            // Arrange
            IUrlHelper urlHelper = new UrlHelper();

            // Act

            string sourceUrl = await urlHelper.GetSourceUrlForNugetPackage(componentName, version);

            // Assert

            Assert.That(sourceUrl, Is.EqualTo("https://github.com/cefsharp/CefSharp"));
        }

        [TestCase("CefSharp.Common22", "100.0.140")]
        public async Task GetSourceUrlForNugetPackage_ProvidedInvalidPackageDetails_ReturnsEmptyString(string componentName, string version)
        {
            // Arrange
            IUrlHelper urlHelper = new UrlHelper();

            // Act

            string sourceUrl = await urlHelper.GetSourceUrlForNugetPackage(componentName, version);

            // Assert

            Assert.That(string.IsNullOrEmpty(sourceUrl));
        }

        [TestCase("adduser", "3.118")]
        public async Task GetSourceUrlForDebianPackage_ProvidedSourcePackage_ReturnsValidSourceURL(string componentName, string version)
        {
            // Arrange
            IUrlHelper urlHelper = new UrlHelper();

            // Act

            var sourceUrlDetails = await urlHelper.GetSourceUrlForDebianPackage(componentName, version);

            // Assert

            Assert.IsTrue(sourceUrlDetails.SourceUrl.Contains("adduser"));
        }

        [TestCase("gpgv", "2.2.12-1+deb10u1")]
        public async Task GetSourceUrlForDebianPackage_ProvidedBinaryPackage_ReturnsValidSourceURL(string componentName, string version)
        {
            // Arrange
            IUrlHelper urlHelper = new UrlHelper();

            // Act

            var sourceUrlDetails = await urlHelper.GetSourceUrlForDebianPackage(componentName, version);

            // Assert

            Assert.IsTrue(sourceUrlDetails.SourceUrl.Contains("gnupg2"));
        }

        [TestCase("gnutls28", "3.6.7-4+deb10u7")]
        public void GetReleaseExternalId_ProvidedPackagDetails_ReturnsEncodedExternalID(string componentName, string version)
        {

            // Act
            var sourceUrlDetails = UrlHelper.GetReleaseExternalId(componentName, version);

            // Assert
            Assert.That(sourceUrlDetails, Is.EqualTo("pkg:deb/debian/gnutls28@3.6.7-4%2Bdeb10u7?arch=source"));

        }

        [TestCase("cachecontrol", "0.12.11")]
        public async Task GetSourceUrlForPythonPackage_ProvidedPackageDetails_ReturnsValidSourceURL(string componentName, string version)
        {
            // Arrange
            IUrlHelper urlHelper = new UrlHelper();

            // Act
            string sourceUrl = await urlHelper.GetSourceUrlForPythonPackage(componentName, version);

            // Assert
            Assert.That(sourceUrl, Is.EqualTo("https://files.pythonhosted.org/packages/46/9b/34215200b0c2b2229d7be45c1436ca0e8cad3b10de42cfea96983bd70248/CacheControl-0.12.11.tar.gz"));
        }

        [TestCase("cachecontrol22", "0.12.111")]
        public async Task GetSourceUrlForPythonPackage_ProvidedInvalidPackageDetails_ReturnsEmptyString(string componentName, string version)
        {
            // Arrange
            IUrlHelper urlHelper = new UrlHelper();

            // Act
            string sourceUrl = await urlHelper.GetSourceUrlForPythonPackage(componentName, version);

            // Assert
            Assert.That(string.IsNullOrEmpty(sourceUrl));
        }

        [TestCase("apk-tools", "2.12.9-r3", "pkg:apk/alpine/apk-tools@2.12.9-r3?distro=alpine-3.16.2")]
        public async Task GetSourceUrlForAlpinePackage_ProvidedPackageDetails_ReturnsValidSourceURL(string componentName, string version,string bomref)
        {
            // Arrange
            IUrlHelper urlHelper = new UrlHelper();

            // Act
            var sourceUrlDetails = await urlHelper.GetSourceUrlForAlpinePackage(componentName, version,bomref);

            // Assert
            Assert.That(sourceUrlDetails.SourceUrl.Contains("apk-tools"));
        }
    }
}
