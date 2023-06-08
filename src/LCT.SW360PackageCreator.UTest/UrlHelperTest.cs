// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
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
    }
}
