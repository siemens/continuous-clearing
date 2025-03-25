// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 


using LCT.Common;
using LCT.SW360PackageCreator.Interfaces;
using Moq;
using Moq.Protected;
using NUnit.Framework;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace LCT.SW360PackageCreator.UTest
{
    /// <summary>
    /// The test class for UrlHelper
    /// </summary>
    [TestFixture]
    public class UrlHelperTest
    {
        private Mock<HttpMessageHandler> _mockHandler;
        private HttpClient _httpClient;
        private UrlHelper _urlHelper;
        private string _pkgFilePath;
        private string _sourceData;
        private string _localPathForSourceRepo = @"C:\TestRepo";
        private string _pkgName = "test-package";
        [SetUp]
        public void SetUp()
        {
            _mockHandler = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_mockHandler.Object);
            _urlHelper = UrlHelper.Instance;
            _pkgFilePath = "path_to_pkg_file";
            _sourceData = "https://example.com/$pkgname/$pkgver/$_commit/$_tzcodever";
        }

        [Test]
        public async Task GetSourceUrlForConanPackage_ReturnsEmptyString_When404ErrorOccurs()
        {
            // Arrange
            var componentName = "test-component";
            var componentVersion = "1.0.0";
            var downLoadUrl = $"{CommonAppSettings.SourceURLConan}" + componentName + "/all/conandata.yml";

            // Mock the HttpClient response to return a 404 status code
            var mockResponse = new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("Not Found")
            };

            _mockHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Get && req.RequestUri.ToString() == downLoadUrl),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(mockResponse)
                .Verifiable();

            // Act
            var result = await _urlHelper.GetSourceUrlForConanPackage(componentName, componentVersion);

            // Assert
            Assert.AreEqual(string.Empty, result, "The result should be an empty string when a 404 error occurs.");
        }
        [Test]
        public void GetSourceFromAPKBUILD_HandlesUnauthorizedAccessException_WhenExceptionIsThrown()
        {
            // Arrange
            string localPathforSourceRepo = @"C:\Repo";
            string name = "example-package";
            string expectedSource = string.Empty;

            // Mocking Directory.Exists and File.Exists
            var mockDirectory = new Mock<IDirectoryWrapper>();
            var mockFile = new Mock<IFileWrapper>();

            mockDirectory.Setup(d => d.Exists(It.IsAny<string>())).Returns(true);
            mockFile.Setup(f => f.Exists(It.IsAny<string>())).Returns(true);
            mockFile.Setup(f => f.ReadAllText(It.IsAny<string>())).Throws(new UnauthorizedAccessException("Access denied"));

            // Act
            var result = UrlHelper.GetSourceFromAPKBUILD(localPathforSourceRepo, name);

            // Assert
            Assert.AreEqual(expectedSource, result); // In case of UnauthorizedAccessException, method should return empty string
        }

        /// <summary>
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
        public async Task GetSourceUrlForAlpinePackage_ProvidedPackageDetails_ReturnsValidSourceURL(string componentName, string version, string bomref)
        {
            // Arrange
            IUrlHelper urlHelper = new UrlHelper();

            // Act
            var sourceUrlDetails = await urlHelper.GetSourceUrlForAlpinePackage(componentName, version, bomref);

            // Assert
            Assert.That(sourceUrlDetails.SourceUrl.Contains("apk-tools"));
        }
        [Test]
        public void GetSourceFromAPKBUILD_WithValidData_ReturnsCorrectSource()
        {
            // Arrange
            string expectedSource = "https://example.com/source.tar.gz";
            string directoryPath = Path.Combine(_localPathForSourceRepo, "aports", "main", _pkgName);
            string filePath = Path.Combine(directoryPath, "APKBUILD");
            string fileContent = $"# Fake APKBUILD file\nsource=\"{expectedSource}\"";

            System.IO.Directory.CreateDirectory(directoryPath);
            File.WriteAllText(filePath, fileContent);

            // Act
            string result = UrlHelper.GetSourceFromAPKBUILD(_localPathForSourceRepo, _pkgName);

            // Cleanup
            File.Delete(filePath);
            System.IO.Directory.Delete(directoryPath, true);

            // Assert
            Assert.AreEqual(expectedSource, result);
        }

        [Test]
        public void GetSourceUrlForAlpine_WithAllCorrectData_ShouldReturnCorrectUrl()
        {
            string expectedUrl = "https://example.com/package-name/1.2.3/abc123/456";
            File.WriteAllLines(_pkgFilePath, new string[] {
                "pkgver=1.2.3",
                "pkgname=package-name",
                "_commit=abc123",
                "_tzcodever=456"
            });

            var result = UrlHelper.GetSourceUrlForAlpine(_pkgFilePath, _sourceData);

            Assert.AreEqual(expectedUrl, result);
        }

        [Test]
        public void GetSourceUrlForAlpine_MissingDataLines_ShouldReturnIncompleteUrl()
        {
            string expectedUrl = "https://example.com/package-name//abc123/";
            File.WriteAllLines(_pkgFilePath, new string[] {
                "pkgname=package-name",
                "_commit=abc123"
            });

            var result = UrlHelper.GetSourceUrlForAlpine(_pkgFilePath, _sourceData);

            Assert.AreEqual(expectedUrl, result);
        }

        [Test]
        public void GetSourceUrlForAlpine_MalformedData_ShouldHandleGracefully()
        {
            string expectedUrl = "https://example.com/package-name/1.2.3/abc123/";
            File.WriteAllLines(_pkgFilePath, new string[] {
                "pkgver=1.2.3",
                "pkgname=package-name",
                "_commit=abc123",
                "_tzcodever="
            });

            var result = UrlHelper.GetSourceUrlForAlpine(_pkgFilePath, _sourceData);

            Assert.AreEqual(expectedUrl, result);
        }

        [TearDown]
        public void CleanUp()
        {
            if (File.Exists(_pkgFilePath))
                File.Delete(_pkgFilePath);
        }
        public interface IDirectoryWrapper
        {
            bool Exists(string path);
        }

        public interface IFileWrapper
        {
            bool Exists(string path);
            string ReadAllText(string path);
        }


    }
}
