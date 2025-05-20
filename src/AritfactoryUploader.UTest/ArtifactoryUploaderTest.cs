// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models.Vulnerabilities;
using LCT.APICommunications;
using LCT.APICommunications.Interfaces;
using LCT.APICommunications.Model;
using LCT.APICommunications.Model.AQL;
using LCT.ArtifactoryUploader;
using LCT.ArtifactoryUploader.Model;
using LCT.Common;
using LCT.Facade;
using LCT.Facade.Interfaces;
using LCT.Services;
using LCT.Services.Interface;
using Moq;
using NUnit.Framework;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using UnitTestUtilities;

namespace AritfactoryUploader.UTest
{
    [TestFixture]
    public class ArtifactoryUploader
    {
        [SetUp]
        public void Setup()
        {
            // Method intentionally left empty.
        }


        [Test]
        public async Task UploadPackageToRepo_InputEmptyCreds_ReturnsPackgeNotFound()
        {
            //Arrange
            CommonAppSettings appSettings = new CommonAppSettings();
            appSettings.Jfrog = new Jfrog()
            {
                URL = UTParams.JFrogURL
            };

            ArtfactoryUploader.jFrogService = GetJfrogService(appSettings);
            DisplayPackagesInfo displayPackagesInfo = PackageUploadInformation.GetComponentsToBePackages();
            var componentsToArtifactory = new ComponentsToArtifactory
            {
                Name = "html5lib",
                PackageName = "html5lib",
                Version = "1.1",
                ComponentType = "PYTHON",
                JfrogApi = "https://abc.jfrog.io/artifactory",
                SrcRepoName = "org1-pythonhosted-pypi-remote-cache",
                SrcRepoPathWithFullName = "org1-pythonhosted-pypi-remote-cache/6c/dd/a834df6482147d48e225a49515aabc28974ad5a4ca3215c18a882565b028/html5lib-1.1-py2.py3-none-any.whl",
                PypiOrNpmCompName = "html5lib-1.1-py2.py3-none-any.whl",
                DestRepoName = "pypi-test",
                Token = "",
                CopyPackageApiUrl = "https://abc.jfrog.io/artifactory/api/copy/org1-pythonhosted-pypi-remote-cache/6c/dd/a834df6482147d48e225a49515aabc28974ad5a4ca3215c18a882565b028/html5lib-1.1-py2.py3-none-any.whl?to=/pypi-test/html5lib-1.1-py2.py3-none-any.whl&dry=1",
                Path = "",
                DryRun = true,
                Purl = "pkg:pypi/html5lib@1.1",
                JfrogPackageName = "html5lib-1.1-py2.py3-none-any.whl"
            };

            //Act
            var responseMessage = await ArtfactoryUploader.UploadPackageToRepo(componentsToArtifactory, 100, displayPackagesInfo);
            Assert.AreEqual(HttpStatusCode.NotFound, responseMessage.StatusCode);
            Assert.AreEqual("Package Not Found", responseMessage.ReasonPhrase);

        }

        private static IJFrogService GetJfrogService(CommonAppSettings appSettings)
        {
            ArtifactoryCredentials artifactoryUpload = new ArtifactoryCredentials()
            {
                Token = appSettings.Jfrog.Token
            };
            IJfrogAqlApiCommunication jfrogAqlApiCommunication =
                new JfrogAqlApiCommunication(appSettings.Jfrog.URL, artifactoryUpload, appSettings.TimeOut);
            IJfrogAqlApiCommunicationFacade jFrogApiCommunicationFacade =
                new JfrogAqlApiCommunicationFacade(jfrogAqlApiCommunication);
            IJFrogService jFrogService = new JFrogService(jFrogApiCommunicationFacade);
            return jFrogService;
        }

        [Test]
        public async Task UploadPackageToRepo_WhenPackageInfoIsNull_ReturnsNotFoundResponse()
        {
            // Arrange
            var component = new ComponentsToArtifactory();
            component.SrcRepoName = "";
            component.DestRepoName = "";
            component.JfrogPackageName = "";
            component.Path = "";
            var timeout = 10000;
            var displayPackagesInfo = new DisplayPackagesInfo();
            var jFrogServiceMock = new Mock<IJFrogService>();
            jFrogServiceMock.Setup(x => x.GetPackageInfo(component))
                .ReturnsAsync((AqlResult)null);
            ArtfactoryUploader.jFrogService = jFrogServiceMock.Object;
            // Act
            var response = await ArtfactoryUploader.UploadPackageToRepo(component, timeout, displayPackagesInfo);

            // Assert
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
            Assert.AreEqual(ApiConstant.PackageNotFound, response.ReasonPhrase);
        }

        [Test]
        public async Task UploadPackageToRepo_WhenPackageTypeIsClearedThirdPartyOrDevelopment_CallsCopyFromRemoteRepo()
        {
            // Arrange
            var component = new ComponentsToArtifactory
            {
                PackageType = PackageType.ClearedThirdParty,
                Token = "apiKey"
            };
            component.SrcRepoName = "";
            component.DestRepoName = "";
            component.JfrogPackageName = "";
            component.Path = "";
            var timeout = 10000;
            var displayPackagesInfo = new DisplayPackagesInfo();
            var jFrogServiceMock = new Mock<IJFrogService>();
            var jfrogApicommunicationMock = new Mock<IJFrogApiCommunication>();
            jFrogServiceMock.Setup(x => x.GetPackageInfo(component))
                .ReturnsAsync(new AqlResult());
            jfrogApicommunicationMock.Setup(x => x.CopyFromRemoteRepo(It.IsAny<ComponentsToArtifactory>(), It.IsAny<string>()))
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));
            ArtfactoryUploader.jFrogService = jFrogServiceMock.Object;
            ArtfactoryUploader.JFrogApiCommInstance = jfrogApicommunicationMock.Object;
            // Act
            var response = await ArtfactoryUploader.UploadPackageToRepo(component, timeout, displayPackagesInfo);

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        [Test]
        public async Task UploadPackageToRepo_WhenPackageTypeIsInternal_CallsMoveFromRepo()
        {
            // Arrange
            var component = new ComponentsToArtifactory
            {
                PackageType = PackageType.Internal,
                Token = "apiKey"
            };
            component.SrcRepoName = "";
            component.DestRepoName = "";
            component.JfrogPackageName = "";
            component.Path = "";
            var timeout = 10000;
            var displayPackagesInfo = new DisplayPackagesInfo();
            var jFrogServiceMock = new Mock<IJFrogService>();
            var jfrogApicommunicationMock = new Mock<IJFrogApiCommunication>();
            jFrogServiceMock.Setup(x => x.GetPackageInfo(component))
                .ReturnsAsync(new AqlResult());
            jfrogApicommunicationMock.Setup(x => x.MoveFromRepo(It.IsAny<ComponentsToArtifactory>(), It.IsAny<string>()))
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));
            ArtfactoryUploader.jFrogService = jFrogServiceMock.Object;
            ArtfactoryUploader.JFrogApiCommInstance = jfrogApicommunicationMock.Object;
            // Act
            var response = await ArtfactoryUploader.UploadPackageToRepo(component, timeout, displayPackagesInfo);

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        [Test]
        public async Task UploadPackageToRepo_WhenPackageTypeIsNotSupported_ReturnsNotFoundResponse()
        {
            // Arrange
            var component = new ComponentsToArtifactory
            {
                PackageType = PackageType.Unknown,
                Token = "apiKey"
            };
            component.SrcRepoName = "";
            component.DestRepoName = "";
            component.JfrogPackageName = "";
            component.Path = "";
            var timeout = 10000;
            var displayPackagesInfo = new DisplayPackagesInfo();
            var jFrogServiceMock = new Mock<IJFrogService>();
            ArtfactoryUploader.jFrogService = jFrogServiceMock.Object;
            // Act
            var response = await ArtfactoryUploader.UploadPackageToRepo(component, timeout, displayPackagesInfo);

            // Assert
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        }
        [Test]
        public async Task UploadPackageToRepo_WhenHttpRequestExceptionOccurs_ReturnsErrorResponse()
        {
            // Arrange
            var component = new ComponentsToArtifactory
            {
                PackageType = PackageType.Unknown,
                Token = "apiKey"
            };
            component.SrcRepoName = "";
            component.DestRepoName = "";
            component.JfrogPackageName = "";
            component.Path = "";
            var timeout = 10000;
            var displayPackagesInfo = new DisplayPackagesInfo();
            var jFrogServiceMock = new Mock<IJFrogService>();
            var jfrogApicommunicationMock = new Mock<IJFrogApiCommunication>();
            jFrogServiceMock.Setup(x => x.GetPackageInfo(component))
                .ThrowsAsync(new HttpRequestException());
            ArtfactoryUploader.jFrogService = jFrogServiceMock.Object;
            ArtfactoryUploader.JFrogApiCommInstance = jfrogApicommunicationMock.Object;

            // Act
            var response = await ArtfactoryUploader.UploadPackageToRepo(component, timeout, displayPackagesInfo);

            // Assert
            Assert.AreEqual(ApiConstant.ErrorInUpload, response.ReasonPhrase);
        }

        [Test]
        public async Task UploadPackageToRepo_WhenInvalidOperationExceptionOccurs_ReturnsErrorResponse()
        {
            // Arrange
            var component = new ComponentsToArtifactory
            {
                PackageType = PackageType.Unknown,
                Token = "apiKey"
            };
            component.SrcRepoName = "";
            component.DestRepoName = "";
            component.JfrogPackageName = "";
            component.Path = "";
            var timeout = 10000;
            var displayPackagesInfo = new DisplayPackagesInfo();
            var jFrogServiceMock = new Mock<IJFrogService>();
            var jfrogApicommunicationMock = new Mock<IJFrogApiCommunication>();
            jFrogServiceMock.Setup(x => x.GetPackageInfo(component))
                .ThrowsAsync(new InvalidOperationException());
            ArtfactoryUploader.jFrogService = jFrogServiceMock.Object;
            ArtfactoryUploader.JFrogApiCommInstance = jfrogApicommunicationMock.Object;

            // Act
            var response = await ArtfactoryUploader.UploadPackageToRepo(component, timeout, displayPackagesInfo);

            // Assert
            Assert.AreEqual(ApiConstant.ErrorInUpload, response.ReasonPhrase);
        }
    }
}