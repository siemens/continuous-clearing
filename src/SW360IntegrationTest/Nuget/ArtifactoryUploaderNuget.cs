// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT

// -------------------------------------------------------------------------------------------------------------------- 
using CycloneDX.Models;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using TestUtilities;

namespace SW360IntegrationTest.Nuget
{
    [TestFixture, Order(16)]
    public class ArtifactoryUploaderNuget
    {
        private string OutFolder { get; set; }
        private static readonly TestParamNuget testParameters = new TestParamNuget();
        [Test, Order(1)]
        public void TestArtifactoryUploaderexe()
        {
            OutFolder = TestHelper.OutFolder + @"\..\..\TestFiles\IntegrationTestFiles\ArtifactoryUploaderTestData\Nuget";

            // Test BOM Creator ran with exit code 0
            Assert.AreEqual(0, TestHelper.RunArtifactoryUploaderExe(new string[]{
                TestConstant.BomFolderPath, OutFolder,
                TestConstant.SW360ProjectName, testParameters.SW360ProjectName,
                TestConstant.ArtifactoryKey, testParameters.ArtifactoryUploadApiKey,
                TestConstant.JfrogNugetThirdPartyDestRepoName,testParameters.ThirdPartyDestinationRepoName,
                TestConstant.JfrogNugetDevDestRepoName,testParameters.DevDestinationRepoName,
                TestConstant.JfrogNugetInternalDestRepoName,testParameters.InternalDestinationRepoName,
                TestConstant.JFrogApiURL,testParameters.JfrogApi,
                TestConstant.DryRun, false.ToString()
            }),
                "Test to run Artifactory Uploader EXE execution");
        }


        [Test, Order(2)]
        public void ComponentUpload_IsUnsuccessful_AlreadyPresentInDestination()
        {
            OutFolder = TestHelper.OutFolder;
            string comparisonBOMPath = OutFolder + @"\..\..\TestFiles\IntegrationTestFiles\ArtifactoryUploaderTestData\Nuget\Test_Bom.cdx.json";
            if (File.Exists(comparisonBOMPath))
            {
                ComponentJsonParsor expected = new ComponentJsonParsor();
                expected.Read(comparisonBOMPath);

                foreach (var item in expected.Components)
                {
                    foreach (var i in expected.Components)
                    {
                        if ((i.Name == item.Name) && (i.Version == item.Version))
                        {
                            Component components = i;
                            if (components.Properties[3].Name.Contains("ApprovedStatus"))
                            {

                                // Assert
                                Assert.AreEqual("siparty-release-nuget-egll", components.Properties[1].Value);


                            }
                        }
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
            string url = $"{TestConstant.JfrogApi}/BouncyCastle-1.8.1.nupkg";

            HttpResponseMessage responseBody = httpClient.GetAsync(url).Result;


            // Assert
            Assert.That(HttpStatusCode.NotFound, Is.EqualTo(responseBody.StatusCode), "Returns Failure status code");


        }

    }
}
