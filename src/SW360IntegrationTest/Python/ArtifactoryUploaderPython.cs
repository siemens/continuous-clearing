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
            OutFolder = Path.GetFullPath(Path.Combine(TestHelper.OutFolder, "..", "..", "TestFiles", "IntegrationTestFiles", "ArtifactoryUploaderTestData", "Poetry"));

            int result = TestHelper.RunArtifactoryUploaderExe(new string[]{
                TestConstant.BomFolderPath, OutFolder,
                TestConstant.SW360ProjectName, testParameters.SW360ProjectName,
                TestConstant.ArtifactoryKey, testParameters.ArtifactoryUploadApiKey,
                TestConstant.JfrogPythonThirdPartyDestRepoName,testParameters.ThirdPartyDestinationRepoName,
                TestConstant.JfrogPythonDevDestRepoName,testParameters.DevDestinationRepoName,
                TestConstant.JfrogPythonInternalDestRepoName,testParameters.InternalDestinationRepoName,
                TestConstant.TelemetryEnable, testParameters.TelemetryEnable,
                TestConstant.JFrogApiURL,testParameters.JfrogApi,
                TestConstant.DryRun, false.ToString()
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
