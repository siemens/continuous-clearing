// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT

// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using TestUtilities;

namespace SW360IntegrationTest.NPM
{
    [TestFixture, Order(1)]
    public class PackageIdentifierInitialTestMode
    {
        private string CCTLocalBomTestFile { get; set; }
        private string OutFolder { get; set; }
        private static readonly TestParam testParameters = new TestParam();

        [SetUp]
        public void Setup()
        {
            OutFolder = TestHelper.OutFolder;

            CCTLocalBomTestFile = Path.GetFullPath(Path.Combine(OutFolder, "..", "..", "src", "SW360IntegrationTest", "PackageIdentifierTestFiles", "Npm", "CCTLocalBOMNpmInitial.json"));

            if (!Directory.Exists(Path.GetFullPath(Path.Combine(OutFolder, "..", "BOMs"))))
            {
                Directory.CreateDirectory(Path.GetFullPath(Path.Combine(OutFolder, "..", "BOMs")));
            }
        }

        [Test, Order(1)]
        public async Task CreateComponent_AfterSuccessfulExeRun_ReturnsSuccess()
        {
            // Arrange
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(testParameters.SW360AuthTokenType, testParameters.SW360AuthTokenValue);
            string componentName = "samplecomponent";
            string componentVersion = "1.0.0";
            string componentType = "OSS";
            HttpResponseMessage componentCheck = await httpClient.GetAsync(TestConstant.Sw360ReleaseApi);

            // Act

            if (componentCheck != null && componentCheck.StatusCode.Equals(HttpStatusCode.NoContent))
            {
                var componentResponse = await httpClient.PostAsync(TestConstant.Sw360ComponentApi, new StringContent(JsonConvert.SerializeObject(new
                {
                    name = componentName,
                    version = componentVersion,
                    componentType = componentType
                }), Encoding.UTF8, "application/json"));

                if (componentResponse.StatusCode == HttpStatusCode.Created)
                {
                    string componentResponseText = await componentResponse.Content.ReadAsStringAsync();
                    var componentJsonObject = JObject.Parse(componentResponseText);
                    var componentId = componentJsonObject["_links"]["self"]["href"].ToString().Split('/').Last();

                    var releaseResponse = await httpClient.PostAsync(TestConstant.Sw360ReleaseApi, new StringContent(JsonConvert.SerializeObject(new
                    {
                        name = componentName,
                        version = componentVersion,
                        componentType = componentType,
                        componentId = componentId,
                        ClearingState = "NEW_CLEARING",
                    }), Encoding.UTF8, "application/json"));
                }

                // Assert
                Assert.AreEqual(HttpStatusCode.Created, componentResponse.StatusCode);
            }
        }

        [Test, Order(2)]
        public void TestBOMCreatorexe()
        {
            string packagjsonPath = Path.GetFullPath(Path.Combine(OutFolder, "..", "..", "TestFiles", "IntegrationTestFiles", "SystemTest1stIterationData"));
            string bomPath = Path.GetFullPath(Path.Combine(OutFolder, "..", "BOMs"));

            // Test BOM Creator ran with exit code 0
            Assert.AreEqual(0, TestHelper.RunBOMCreatorExe(new string[]{
                TestConstant.PackageFilePath, packagjsonPath,
                TestConstant.BomFolderPath, bomPath,
                TestConstant.Sw360Token, testParameters.SW360AuthTokenValue,
                TestConstant.SW360AuthTokenType, testParameters.SW360AuthTokenType,
                TestConstant.SW360URL, testParameters.SW360URL,
                TestConstant.SW360ProjectID, testParameters.SW360ProjectID,
                TestConstant.SW360ProjectName, testParameters.SW360ProjectName,
                TestConstant.JFrogApiURL, testParameters.JfrogApi,
                TestConstant.ArtifactoryKey, testParameters.ArtifactoryUploadApiKey,
                TestConstant.TelemetryEnable, testParameters.TelemetryEnable,
                TestConstant.ProjectType, "NPM",
                TestConstant.Mode,"test"
            }),
                "Test to run  Package Identifier EXE execution");
        }


        [Test, Order(3)]
        public void TestLocalBOMCreation()
        {
            bool fileExist = false;

            // Expected
            ComponentJsonParsor expected = new ComponentJsonParsor();
            expected.Read(CCTLocalBomTestFile);

            // Actual
            string generatedBOM = Path.GetFullPath(Path.Combine(OutFolder, "..", "BOMs", $"{testParameters.SW360ProjectName}_Bom.cdx.json"));
            if (File.Exists(generatedBOM))
            {
                fileExist = true;

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
                            Assert.AreEqual(item.Purl, component.Purl);
                            Assert.AreEqual(item.BomRef, component.BomRef);
                        }
                    }

                }
            }

            Assert.IsTrue(fileExist, "Test to BOM file present");
        }
    }
}