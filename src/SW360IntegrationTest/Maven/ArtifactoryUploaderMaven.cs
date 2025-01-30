// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Siemens AG">
//   Copyright (C) Siemens AG 2023. All rights reserved. 
// </copyright>
//<license>MIT</license>
// -------------------------------------------------------------------------------------------------------------------- 
using CycloneDX.Models;
using NUnit.Framework;
using System.IO;
using System.Net;
using System.Net.Http;
using TestUtilities;

namespace SW360IntegrationTest.Maven
{
    [TestFixture, Order(21)]
    public class ArtifactoryUploaderMaven
    {
        private string OutFolder { get; set; }
        private static readonly TestParamMaven testParameters = new TestParamMaven();
        [Test, Order(1)]
        public void TestArtifactoryUploaderexe()
        {
            OutFolder = TestHelper.OutFolder;

            int result = TestHelper.RunArtifactoryUploaderExe(new string[]{
                TestConstant.BomFolderPath, OutFolder + @"\..\..\TestFiles\MavenTestFile\ArtifactoryUploaderTestData",
                TestConstant.SW360ProjectName, testParameters.SW360ProjectName,
                TestConstant.ArtifactoryKey, testParameters.ArtifactoryUploadApiKey,
                TestConstant.JfrogMavenThirdPartyDestRepoName,testParameters.ThirdPartyDestinationRepoName,
                TestConstant.JfrogMavenDevDestRepoName,testParameters.DevDestinationRepoName,
                TestConstant.JfrogMavenInternalDestRepoName,testParameters.InternalDestinationRepoName,
                TestConstant.JFrogApiURL,testParameters.JfrogApi,
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
            string comparisonBOMPath = OutFolder + @"\..\..\TestFiles\MavenTestFile\ArtifactoryUploaderTestData\Test_Bom.cdx.json";
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
                                Assert.AreEqual("org1-bintray-maven-remote-cache", components.Properties[1].Value);


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
            string url = $"{TestConstant.JfrogApi}/org/hamcrest/hamcrest-core/1.3";

            HttpResponseMessage responseBody = httpClient.GetAsync(url).Result;


            // Assert
            Assert.That(HttpStatusCode.NotFound, Is.EqualTo(responseBody.StatusCode), "Returns Failure status code");


        }

    }
}
