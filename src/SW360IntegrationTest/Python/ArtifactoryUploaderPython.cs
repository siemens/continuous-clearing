// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Siemens AG">
//   Copyright (C) Siemens AG 2023. All rights reserved. 
// </copyright>
//<license>MIT</license>
// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models;
using NUnit.Framework;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using TestUtilities;

namespace SW360IntegrationTest.Python
{
    [TestFixture, Order(25)]
    public class ArtifactoryUploaderPython
    {
        private string OutFolder { get; set; }
        private static readonly TestParam testParameters = new TestParam();
        [Test, Order(1)]
        public void TestArtifactoryUploaderexe()
        {
            OutFolder = TestHelper.OutFolder;
            string comparisonBOMPath = "C:\\Users\\z004tjcm\\Desktop\\CATool\\Output\\SICAMDeviceManager_Bom.cdx.json";

            int result = TestHelper.RunArtifactoryUploaderExe(new string[]{
                TestConstant.BomFilePath, comparisonBOMPath,
                TestConstant.SW360ProjectName, testParameters.SW360ProjectName,
                TestConstant.ArtifactoryUser, testParameters.ArtifactoryUploadUser,
                TestConstant.ArtifactoryKey, testParameters.ArtifactoryUploadApiKey,
                TestConstant.JfrogPythonThirdPartyDestRepoName,testParameters.ThirdPartyDestinationRepoName,
                TestConstant.JfrogPythonDevDestRepoName,testParameters.DevDestinationRepoName,
                TestConstant.JfrogPythonInternalDestRepoName,testParameters.InternalDestinationRepoName,
                TestConstant.JFrogApiURL,testParameters.JfrogApi,
                TestConstant.Release, false.ToString(),
                "--LogFolderPath C:\\Users\\z004tjcm\\Desktop\\CATool\\Logs"
            });

            // Test BOM Creator ran with exit code 0 or 2 (Warning)
            Assert.IsTrue(result == 0 || result == 2,
                "Test to run Artifactory Uploader EXE execution");
        }

        [Test, Order(2)]
        public void ComponentUpload_IsFailure()
        {
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add(TestConstant.JFrog_API_Header, testParameters.ArtifactoryUploadApiKey);
            httpClient.DefaultRequestHeaders.Add(TestConstant.Email, testParameters.ArtifactoryUploadUser);

            // Act
            string url = $"{TestConstant.JfrogApi}/pypi-test/cachy222-0.3.0-py2.py3-none-any.whl";
            HttpResponseMessage responseBody = httpClient.GetAsync(url).Result;

            // Assert
            Assert.That(HttpStatusCode.NotFound, Is.EqualTo(responseBody.StatusCode), "Returns Failure status code");
        }
    }
}
