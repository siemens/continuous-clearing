using CycloneDX.Models;
using NUnit.Framework;
using System.IO;
using System.Net;
using System.Net.Http;
using TestUtilities;

namespace SW360IntegrationTest
{
    [TestFixture, Order(25),Ignore("Out of Scope")]
    public class ArtifactoryUploaderConan
    {
        private string OutFolder { get; set; }
        private static readonly TestParamConan testParameters = new TestParamConan();
        [Test, Order(1)]
        public void TestArtifactoryUploaderexe()
        {
            OutFolder = TestHelper.OutFolder;
            string comparisonBOMPath = OutFolder + @"\..\..\TestFiles\IntegrationTestFiles\ArtifactoryUploaderTestData\NPMComparisonBOM.json";

            // Test BOM Creator ran with exit code 0
            Assert.AreEqual(0, TestHelper.RunArtifactoryUploaderExe(new string[]{
                TestConstant.BomFilePath, comparisonBOMPath,
                TestConstant.ArtifactoryUser, testParameters.ArtifactoryUploadUser,
                TestConstant.ArtifactoryKey, testParameters.ArtifactoryUploadApiKey,
                TestConstant.JfrogNPMDestRepoName,testParameters.DestinationRepoName,
                TestConstant.JFrogApiURL,testParameters.JfrogApi
            }),
                "Test to run Artifactory Uploader EXE execution");
        }

        [Test, Order(2)]
        public void ComponentUpload_IsUnsuccessful_AlreadyPresentInDestination()
        {
            OutFolder = TestHelper.OutFolder;
            string comparisonBOMPath = OutFolder + @"\..\..\TestFiles\IntegrationTestFiles\ArtifactoryUploaderTestData\NPMComparisonBOM.json";
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
