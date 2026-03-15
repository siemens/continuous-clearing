using CycloneDX.Models;
using LCT.APICommunications.Model;
using Newtonsoft.Json;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using TestUtilities;

namespace SW360IntegrationTest.Conan
{
    [TestFixture, Order(27)]
    public class ComponentCreatorInitialConan
    {
        private static readonly TestParamConan testParameters = new TestParamConan();

        [SetUp]
        public void Setup()
        {
            OutFolder = TestHelper.OutFolder;
            CCTComparisonBomTestFile = Path.GetFullPath(Path.Combine(OutFolder, "..", "..", "src", "SW360IntegrationTest", "PackageCreatorTestFiles", "Conan", "CCTComparisonBOMConanInitial.json"));

            if (!TestHelper.BOMCreated)
            {
                OutFolder = TestHelper.OutFolder;
                string packagjsonPath = Path.GetFullPath(Path.Combine(OutFolder, "..", "..", "TestFiles", "IntegrationTestFiles", "SystemTest1stIterationData", "Conan"));
                string bomPath = Path.GetFullPath(Path.Combine(OutFolder, "..", "BOMs"));
                TestHelper.RunBOMCreatorExe(new string[]{
                TestConstant.PackageFilePath, packagjsonPath,
                TestConstant.BomFolderPath, bomPath,
                TestConstant.Sw360Token, testParameters.SW360AuthTokenValue,
                TestConstant.SW360AuthTokenType, testParameters.SW360AuthTokenType,
                TestConstant.SW360URL, testParameters.SW360URL,
                TestConstant.SW360ProjectID, testParameters.SW360ProjectID,
                TestConstant.SW360ProjectName, testParameters.SW360ProjectName,
                TestConstant.JFrogApiURL, testParameters.JfrogApi,
                TestConstant.JfrogConanInternalRepo,"Conan-test",
                TestConstant.ArtifactoryKey, testParameters.ArtifactoryUploadApiKey,
                TestConstant.TelemetryEnable, testParameters.TelemetryEnable,
                TestConstant.SBOMSignVerify, testParameters.SBOMSignVerify,
                TestConstant.ProjectType, "CONAN",
                TestConstant.Mode,""
                });
            }
        }
        [Test, Order(1)]
        public void TestComponentCreatorExe_Conan()
        {
            string bomPath = Path.GetFullPath(Path.Combine(OutFolder, "..", "BOMs"));
            // Assert
            // Check return with warning code 2
            Assert.AreEqual(2, TestHelper.RunComponentCreatorExe(new string[] {
                TestConstant.BomFolderPath,bomPath,
                TestConstant.Sw360Token, testParameters.SW360AuthTokenValue,
                TestConstant.SW360URL, testParameters.SW360URL,
                TestConstant.SW360AuthTokenType, testParameters.SW360AuthTokenType,
                TestConstant.SW360ProjectID, testParameters.SW360ProjectID,
                TestConstant.SW360ProjectName, testParameters.SW360ProjectName,
                TestConstant.FossologyURL, testParameters.FossUrl,
                TestConstant.EnableFossologyTrigger,testParameters.FossologyTrigger,
                TestConstant.TelemetryEnable, testParameters.TelemetryEnable,
                TestConstant.SBOMSignVerify, testParameters.SBOMSignVerify,
                TestConstant.Mode,""
            }),
                "Test to run Package Creator EXE execution");
        }

        [Test, Order(2)]
        public void TestComparisionBOMUpdation_Conan()
        {
            bool filecheck = false;

            // Expected
            ComponentJsonParsor expected = new ComponentJsonParsor();
            expected.Read(CCTComparisonBomTestFile);

            // Actual
            string generatedBOM = Path.GetFullPath(Path.Combine(OutFolder, "..", "BOMs", $"{testParameters.SW360ProjectName}_Bom.cdx.json"));
            if (File.Exists(generatedBOM))
            {

                filecheck = true;
                ComponentJsonParsor actual = new ComponentJsonParsor();
                actual.Read(generatedBOM);

                foreach (var item in expected.Components)
                {

                    foreach (var i in actual.Components)
                    {
                        if ((i.Name == item.Name) && (i.Version == item.Version))
                        {
                            Component component = i;
                            Assert.AreEqual(item.Name, component.Name);
                            Assert.AreEqual(item.Version, component.Version);
                            Assert.AreEqual(item.BomRef, component.BomRef);
                            Assert.AreEqual(item.Purl, component.Purl);
                        }
                    }

                }

            }
            Assert.IsTrue(filecheck, "CycloneDx BOM not exist");
        }

        [Test, Order(3)]
        public async Task TestComponentCreation_Conan()
        {
            //Setting the httpclient
            var httpClient = new HttpClient() { };
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(TestConstant.TestSw360TokenType, TestConstant.TestSw360TokenValue);

            //url formation for retrieving component details
            string url = TestConstant.Sw360ComponentApi + TestConstant.componentNameUrl + "libcurl";
            string responseBody = await httpClient.GetStringAsync(url); //GET request
            var responseData = JsonConvert.DeserializeObject<ComponentsModel>(responseBody);
            //Assert
            Assert.IsTrue(responseData.Embedded.Sw360components.Count == 1);

        }
        [Test, Order(4)]
        public async Task ReleaseCreation__AfterSuccessfulExeRun_ReturnsClearingStateAsNewClearing()
        {
            //Setting the httpclient
            var httpClient = new HttpClient() { };
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(testParameters.SW360AuthTokenType, testParameters.SW360AuthTokenValue);
            string expectedname = "libcurl";
            string expectedversion = "8.18.0";
            string expecteddownloadurl = "https://curl.se/download/curl-8.18.0.tar.xz";
            string expectedexternalid = "pkg:conan/libcurl@8.18.0";
            //url formation for retrieving component details
            string url = TestConstant.Sw360ReleaseApi + TestConstant.componentNameUrl + "libcurl";
            string responseBody = await httpClient.GetStringAsync(url);//GET method         
            var responseData = JsonConvert.DeserializeObject<ReleaseIdOfComponent>(responseBody);
            string urlofreleaseid = responseData.Embedded.Sw360Releases[0].Links.Self.Href;
            string responseForRelease = await httpClient.GetStringAsync(urlofreleaseid);//GET method for fetching the release details
            var responseDataForRelease = JsonConvert.DeserializeObject<Releases>(responseForRelease);

            string name = responseDataForRelease.Name;
            string version = responseDataForRelease.Version;
            string downloadurl = responseDataForRelease.SourceDownloadurl;
            string externalid = responseDataForRelease.ExternalIds.Package_Url;
            string releaseLink = responseDataForRelease.Links.Self.Href;
            string releaseResponseBody = await httpClient.GetStringAsync(releaseLink);//GET method
            var releasesInfo = JsonConvert.DeserializeObject<ReleasesInfo>(releaseResponseBody);

            var releaseAttachments = releasesInfo?.Embedded?.Sw360attachments ?? new List<Sw360Attachments>();
            bool AttachmentFound = releaseAttachments.Any(x => x.AttachmentType.Equals("SOURCE"));

            //Assert
            Assert.IsTrue(AttachmentFound, "Expected a SOURCE attachment to be present in the release.");
            Assert.AreEqual(expectedname, name, "Test Project Name");
            Assert.AreEqual(expectedversion, version, "Test Project  Version");
            Assert.AreEqual(expecteddownloadurl, downloadurl, "Test download Url of Entity Framework");
            Assert.AreEqual(expectedexternalid, externalid, "Test component external id");
        }
        private string CCTComparisonBomTestFile { get; set; }
        private string OutFolder { get; set; }
    }

}
