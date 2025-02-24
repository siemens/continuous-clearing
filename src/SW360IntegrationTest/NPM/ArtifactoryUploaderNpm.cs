// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT

// -------------------------------------------------------------------------------------------------------------------- 
using CycloneDX.Models;
using NUnit.Framework;
using System.IO;
using System.Net;
using System.Net.Http;
using TestUtilities;

namespace SW360IntegrationTest.NPM
{
    [TestFixture, Order(12)]
    public class ArtifactoryUploaderNpm
    {
        private string OutFolder { get; set; }
        private static readonly TestParam testParameters = new TestParam();
        [Test, Order(1)]
        public void TestArtifactoryUploaderexe()
        {
            OutFolder = TestHelper.OutFolder;

            int result = TestHelper.RunArtifactoryUploaderExe(new string[]{
                TestConstant.BomFolderPath, OutFolder + @"\..\..\TestFiles\IntegrationTestFiles\ArtifactoryUploaderTestData\Npm",
                TestConstant.SW360ProjectName, testParameters.SW360ProjectName,
                TestConstant.ArtifactoryKey, testParameters.ArtifactoryUploadApiKey,
                TestConstant.JfrogNpmThirdPartyDestRepoName,testParameters.ThirdPartyDestinationRepoName,
                TestConstant.JfrogNpmDevDestRepoName,testParameters.DevDestinationRepoName,
                TestConstant.JfrogNpmInternalDestRepoName,testParameters.InternalDestinationRepoName,
                TestConstant.JFrogApiURL,testParameters.JfrogApi,
                TestConstant.TelemetryEnable, testParameters.TelemetryEnable,
                TestConstant.DryRun, false.ToString()
            });

            // Test BOM Creator ran with exit code 0 or 2 (Warning)
            Assert.IsTrue(result == 0 || result == 2,
                "Test to run Artifactory Uploader EXE execution");
        }

        [Test, Order(2)]
        public void ComponentUpload_IsUnsuccessful_AlreadyPresentInDestination()
        {
            OutFolder = TestHelper.OutFolder;
            string comparisonBOMPath = OutFolder + @"\..\..\TestFiles\IntegrationTestFiles\ArtifactoryUploaderTestData\Npm\Test_Bom.cdx.json";
            if (File.Exists(comparisonBOMPath))
            {
                ComponentJsonParsor expected = new ComponentJsonParsor();
                expected.Read(comparisonBOMPath);

                foreach (var item in expected.Components)
                {
                    Component components = item;
                    if (components.Properties[3].Name.Contains("ApprovedStatus"))
                    {
                        // Assert
                        Assert.AreEqual("siparty-release-npm-egll", components.Properties[1].Value);
                    }
                }
            }
        }



        [Test, Order(3)]
        public void ComponentUpload_IsFailure()
        {
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add(TestConstant.JFrog_API_Header, testParameters.ArtifactoryUploadApiKey);
            httpClient.DefaultRequestHeaders.Add(TestConstant.Email, testParameters.ArtifactoryUploadUser);

            // Act
            string url = $"{TestConstant.JfrogApi}/@angular/core/-/core-9.1.3.tgz";
            HttpResponseMessage responseBody = httpClient.GetAsync(url).Result;

            // Assert
            Assert.That(HttpStatusCode.NotFound, Is.EqualTo(responseBody.StatusCode), "Returns Failure status code");
        }

    }
}
