// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.APICommunications.Interfaces;
using LCT.APICommunications.Model;
using Moq;
using System.Net;
using LCT.ArtifactoryUploader;
using System.Net.Http;
using NUnit.Framework;
using System.Threading.Tasks;
using LCT.APICommunications;
using LCT.Common;
using LCT.Facade.Interfaces;
using LCT.Facade;
using LCT.Services.Interface;
using LCT.Services;
using UnitTestUtilities;
using LCT.ArtifactoryUploader.Model;
using LCT.APICommunications.Model.AQL;

namespace AritfactoryUploader.UTest
{
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
            CommonAppSettings appSettings = new CommonAppSettings()
            {
                JFrogApi = UTParams.JFrogURL
            };
            ArtfactoryUploader.jFrogService = GetJfrogService(appSettings);
            DisplayPackagesInfo displayPackagesInfo = PackageUploadHelper.GetComponentsToBePackages();
            var componentsToArtifactory = new ComponentsToArtifactory
            {
                Name = "html5lib",
                PackageName = "html5lib",
                Version = "1.1",
                ComponentType = "PYTHON",
                JfrogApi = "https://abc.jfrog.io/artifactory",
                SrcRepoName = "org1-pythonhosted-pypi-remote-cache",
                SrcRepoPathWithFullName = "org1-pythonhosted-pypi-remote-cache/6c/dd/a834df6482147d48e225a49515aabc28974ad5a4ca3215c18a882565b028/html5lib-1.1-py2.py3-none-any.whl",
                PypiCompName = "html5lib-1.1-py2.py3-none-any.whl",
                DestRepoName = "pypi-test",
                ApiKey = "",
                Email = "",
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

        [Test]
        public void SetConfigurationValues_InputEmptyCreds_ReturnsVoid()
        {
            //Arrange
            bool returnValue = true;

            //Act
            ArtfactoryUploader.SetConfigurationValues();

            //Assert
            Assert.That(returnValue, Is.True);
        }

        private static IJFrogService GetJfrogService(CommonAppSettings appSettings)
        {
            ArtifactoryCredentials artifactoryUpload = new ArtifactoryCredentials()
            {
                ApiKey = appSettings.ArtifactoryUploadApiKey
            };
            IJfrogAqlApiCommunication jfrogAqlApiCommunication =
                new JfrogAqlApiCommunication(appSettings.JFrogApi, artifactoryUpload, appSettings.TimeOut);
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
            jFrogServiceMock.Setup(x => x.GetPackageInfo(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
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
                ApiKey = "apiKey",
                Email = "test@example.com"
            };
            component.SrcRepoName = "";
            component.DestRepoName = "";
            component.JfrogPackageName = "";
            component.Path = "";
            var timeout = 10000;
            var displayPackagesInfo = new DisplayPackagesInfo();
            var jFrogServiceMock = new Mock<IJFrogService>();
            var jfrogApicommunicationMock = new Mock<IJFrogApiCommunication>();
            jFrogServiceMock.Setup(x => x.GetPackageInfo(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new AqlResult());
            jfrogApicommunicationMock.Setup(x => x.CopyFromRemoteRepo(It.IsAny<ComponentsToArtifactory>()))
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));
            ArtfactoryUploader.jFrogService = jFrogServiceMock.Object;
            ArtfactoryUploader.JFrogApiCommInstance = jfrogApicommunicationMock.Object;
            // Act
            var response = await ArtfactoryUploader.UploadPackageToRepo(component, timeout, displayPackagesInfo);

            // Assert
            jfrogApicommunicationMock.Verify(x => x.CopyFromRemoteRepo(component), Times.Once);
            //Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        [Test]
        public async Task UploadPackageToRepo_WhenPackageTypeIsInternal_CallsMoveFromRepo()
        {
            // Arrange
            var component = new ComponentsToArtifactory
            {
                PackageType = PackageType.Internal,
                ApiKey = "apiKey",
                Email = "test@example.com"
            };
            component.SrcRepoName = "";
            component.DestRepoName = "";
            component.JfrogPackageName = "";
            component.Path = "";
            var timeout = 10000;
            var displayPackagesInfo = new DisplayPackagesInfo();
            var jFrogServiceMock = new Mock<IJFrogService>();
            var jfrogApicommunicationMock = new Mock<IJFrogApiCommunication>();
            jFrogServiceMock.Setup(x => x.GetPackageInfo(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new AqlResult());
            jfrogApicommunicationMock.Setup(x => x.MoveFromRepo(It.IsAny<ComponentsToArtifactory>()))
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));
            ArtfactoryUploader.jFrogService = jFrogServiceMock.Object;
            ArtfactoryUploader.JFrogApiCommInstance = jfrogApicommunicationMock.Object;
            // Act
            var response = await ArtfactoryUploader.UploadPackageToRepo(component, timeout, displayPackagesInfo);

            // Assert
            jfrogApicommunicationMock.Verify(x => x.MoveFromRepo(component), Times.Once);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        [Test]
        public async Task UploadPackageToRepo_WhenPackageTypeIsNotSupported_ReturnsNotFoundResponse()
        {
            // Arrange
            var component = new ComponentsToArtifactory
            {
                PackageType = PackageType.Unknown,
                ApiKey = "apiKey",
                Email = "test@example.com"
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
    }
}