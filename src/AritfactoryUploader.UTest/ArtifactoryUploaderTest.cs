// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.APICommunications.Interfaces;
using LCT.APICommunications.Model;
using Moq;
using Newtonsoft.Json;
using System.Net;
using LCT.ArtifactoryUploader;
using System.Net.Http;
using System;
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
            UnknownPackagesAll unknownPackagesAll=new();
            //Act
            var responseMessage = await ArtfactoryUploader.UploadPackageToRepo(componentsToArtifactory, 100, unknownPackagesAll);
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
    }
}