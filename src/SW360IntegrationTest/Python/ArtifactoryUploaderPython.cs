// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Siemens AG">
//   Copyright (C) Siemens AG 2023. All rights reserved. 
// </copyright>
//<license>MIT</license>
// -------------------------------------------------------------------------------------------------------------------- 

using NUnit.Framework;
using System.Net;
using System.Net.Http;
using TestUtilities;

namespace SW360IntegrationTest
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
            string comparisonBOMPath = OutFolder + @"\..\..\TestFiles\IntegrationTestFiles\ArtifactoryUploaderTestData\PythonComparisonBOM.json";

            int result = TestHelper.RunArtifactoryUploaderExe(new string[]{
                TestConstant.BomFilePath, comparisonBOMPath,
                TestConstant.ArtifactoryUser, testParameters.ArtifactoryUploadUser,
                TestConstant.ArtifactoryKey, testParameters.ArtifactoryUploadApiKey,
                TestConstant.JfrogNPMDestRepoName,testParameters.DestinationRepoName,
                TestConstant.JFrogApiURL,testParameters.JfrogApi
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
