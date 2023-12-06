using CycloneDX.Models;
using NUnit.Framework;
using System.IO;
using System.Net;
using System.Net.Http;
using TestUtilities;

namespace SW360IntegrationTest.Conan
{
    [TestFixture, Order(28)]
    public class ArtifactoryUploaderConan
    {
        private string OutFolder { get; set; }
        private static readonly TestParamConan testParameters = new TestParamConan();
        [Test, Order(1)]
        public void TestArtifactoryUploaderexe()
        {
            OutFolder = TestHelper.OutFolder;
            string comparisonBOMPath = OutFolder + @"\..\..\TestFiles\IntegrationTestFiles\ArtifactoryUploaderTestData\ConanComparisonBOM.json";

            // Test BOM Creator ran with exit code 0
            Assert.AreEqual(0, TestHelper.RunArtifactoryUploaderExe(new string[]{
                TestConstant.BomFilePath, comparisonBOMPath,
                TestConstant.SW360ProjectName, testParameters.SW360ProjectName,
                TestConstant.ArtifactoryUser, testParameters.ArtifactoryUploadUser,
                TestConstant.ArtifactoryKey, testParameters.ArtifactoryUploadApiKey,
                TestConstant.JfrogConanThirdPartyDestRepoName,testParameters.ThirdPartyDestinationRepoName,
                TestConstant.JfrogConanDevDestRepoName,testParameters.DevDestinationRepoName,
                TestConstant.JfrogConanInternalDestRepoName,testParameters.InternalDestinationRepoName,
                TestConstant.JFrogApiURL,testParameters.JfrogApi,
                TestConstant.Release, false.ToString(),
                "--LogFolderPath C:\\Users\\z004tjcm\\Desktop\\CATool\\Logs"
            }),
                "Test to run Artifactory Uploader EXE execution");
        }

        [Test, Order(2)]
        public void ComponentUpload_IsUnsuccessful_AlreadyPresentInDestination_Conan()
        {
            OutFolder = TestHelper.OutFolder;
            string comparisonBOMPath = OutFolder + @"\..\..\TestFiles\IntegrationTestFiles\ArtifactoryUploaderTestData\ConanComparisonBOM.json";
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
        public void ComponentUpload_IsFailure_Conan()
        {
            HttpClient httpClient = new HttpClient();

            httpClient.DefaultRequestHeaders.Add(TestConstant.JFrog_API_Header, testParameters.ArtifactoryUploadApiKey);
            httpClient.DefaultRequestHeaders.Add(TestConstant.Email, testParameters.ArtifactoryUploadUser);

            // Act
            string url = $"{TestConstant.JfrogApi}/@conan-9.1.3.tgz";

            HttpResponseMessage responseBody = httpClient.GetAsync(url).Result;


            // Assert
            Assert.That(HttpStatusCode.NotFound, Is.EqualTo(responseBody.StatusCode), "Returns Failure status code");


        }

    }
}
