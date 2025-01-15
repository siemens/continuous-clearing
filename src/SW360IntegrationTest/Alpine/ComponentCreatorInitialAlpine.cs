// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT

// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models;
using LCT.APICommunications.Model;
using Newtonsoft.Json;
using NUnit.Framework;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using TestUtilities;

namespace SW360IntegrationTest.Alpine
{
    [TestFixture, Order(26)]
    class ComponentCreatorInitialAlpine
    {
        private string CCTComparisonBomTestFile { get; set; }
        private string OutFolder { get; set; }
        private TestParamAlpine testParameters;

        [SetUp]
        public void Setup()
        {
            testParameters = new TestParamAlpine();
            OutFolder = TestHelper.OutFolder;
            CCTComparisonBomTestFile = OutFolder + @"..\..\..\src\SW360IntegrationTest\PackageCreatorTestFiles\Alpine\CCTComparisonBOMAlpineInitial.json";

            if (!TestHelper.BOMCreated)
            {
                OutFolder = TestHelper.OutFolder;
                string packagejsonPath = OutFolder + @"\..\..\TestFiles\IntegrationTestFiles\SystemTest1stIterationData\Alpine";
                string bomPath = OutFolder + @"\..\BOMs";
                TestHelper.RunBOMCreatorExe(new string[]{
                TestConstant.PackageFilePath, packagejsonPath,
                TestConstant.BomFolderPath, bomPath,
                TestConstant.Sw360Token, testParameters.SW360AuthTokenValue,
                TestConstant.SW360AuthTokenType, testParameters.SW360AuthTokenType,
                TestConstant.SW360URL, testParameters.SW360URL,
                TestConstant.SW360ProjectID, testParameters.SW360ProjectID,
                TestConstant.SW360ProjectName, testParameters.SW360ProjectName,
                TestConstant.JFrogApiURL, testParameters.JfrogApi,
                TestConstant.ArtifactoryKey, testParameters.ArtifactoryUploadApiKey,
                TestConstant.ProjectType,"ALPINE",
                TestConstant.Mode,""});
            }
        }

        [Test, Order(1)]
        public void ComponentCreatorExe_ProvidedBOMFilePath_ReturnsSuccess()
        {
            string bomPath = OutFolder + $"\\..\\BOMs\\{testParameters.SW360ProjectName}_Bom.cdx.json";
            // Assert
            // Check exit is normal
            Assert.AreEqual(0, TestHelper.RunComponentCreatorExe(new string[] {
                TestConstant.PackageFilePath,bomPath,
                TestConstant.Sw360Token, testParameters.SW360AuthTokenValue,
                TestConstant.SW360URL, testParameters.SW360URL,
                TestConstant.SW360AuthTokenType, testParameters.SW360AuthTokenType,
                TestConstant.SW360ProjectID, testParameters.SW360ProjectID,
                TestConstant.SW360ProjectName, testParameters.SW360ProjectName,
                TestConstant.ProjectType,"ALPINE",
                TestConstant.Mode,""}),
            "Test to run component creator EXE execution");
        }
        [Test, Order(2)]
        public void TestComparisionBOMUpdation()
        {
            bool filecheck = false;

            // Expected
            ComponentJsonParsor expected = new ComponentJsonParsor();
            expected.Read(CCTComparisonBomTestFile);

            // Actual
            string generatedBOM = OutFolder + $"\\..\\BOMs\\{testParameters.SW360ProjectName}_Bom.cdx.json";
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
                            Assert.AreEqual(item.Properties.Count, component.Properties.Count);
                        }
                    }
                }
            }
            Assert.IsTrue(filecheck, "Comparison BOM not exist");
        }
        [Test, Order(3)]
        public async Task ComponentCreation_AfterSuccessfulExeRun_ReturnsSuccess()
        {
            //Setting the httpclient
            var httpClient = new HttpClient() { };
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(testParameters.SW360AuthTokenType, testParameters.SW360AuthTokenValue);
            string expectedcomponentType = "OSS";
            string expectedname = "apk-tools";
            //url formation for retrieving component details
            string url = TestConstant.Sw360ComponentApi + TestConstant.componentNameUrl + "apk-tools";
            string responseBody = await httpClient.GetStringAsync(url); //GET request
            var responseData = JsonConvert.DeserializeObject<ComponentsModel>(responseBody);
            string name = responseData.Embedded.Sw360components[0].Name;
            string href = responseData.Embedded.Sw360components[0].Links.Self.Href;
            string hrefresponse = await httpClient.GetStringAsync(href);//GET request 
            var hrefData = JsonConvert.DeserializeObject<CreateComponent>(hrefresponse);
            string componentType = hrefData.ComponentType;

            //Assert
            Assert.AreEqual(expectedname, name, "Test if the component name is correct");
            Assert.AreEqual(expectedcomponentType, componentType, "Test if the component version is correct");
        }

        [Test, Order(4)]
        public async Task ReleaseCreation__AfterSuccessfulExeRun_ReturnsClearingStateAsNewClearing()
        {
            //Setting the httpclient
            var httpClient = new HttpClient() { };
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(testParameters.SW360AuthTokenType, testParameters.SW360AuthTokenValue);
            string expectedname = "apk-tools";
            string expectedversion = "2.12.9-r3";
            string expecteddownloadurl = "https://gitlab.alpinelinux.org/alpine/apk-tools/-/archive/v2.12.9/apk-tools-v2.12.9.tar.gz";
            string expectedexternalid = "pkg:apk/alpine/apk-tools@2.12.9-r3?arch=source";
            //url formation for retrieving component details
            string url = TestConstant.Sw360ReleaseApi + TestConstant.componentNameUrl + "apk-tools";
            string responseBody = await httpClient.GetStringAsync(url);//GET method         
            var responseData = JsonConvert.DeserializeObject<ReleaseIdOfComponent>(responseBody);
            string urlofreleaseid = responseData.Embedded.Sw360Releases[0].Links.Self.Href;
            string responseForRelease = await httpClient.GetStringAsync(urlofreleaseid);//GET method for fetching the release details
            var responseDataForRelease = JsonConvert.DeserializeObject<Releases>(responseForRelease);

            string name = responseDataForRelease.Name;
            string version = responseDataForRelease.Version;
            string downloadurl = responseDataForRelease.SourceDownloadurl;
            string externalid = responseDataForRelease.ExternalIds.Package_Url;

            //Assert
            Assert.AreEqual(expectedname, name, "Test Project Name");
            Assert.AreEqual(expectedversion, version, "Test Project  Version");
            Assert.AreEqual(expecteddownloadurl, downloadurl, "Test download Url of Entity Framework");
            Assert.AreEqual(expectedexternalid, externalid, "Test component external id");
        }



    }
}