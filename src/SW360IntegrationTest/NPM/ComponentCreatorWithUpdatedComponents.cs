﻿// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
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

namespace SW360IntegrationTest.NPM
{
    [TestFixture, Order(8)]
    public class ComponentCreatorWithUpdatedComponents
    {
        private string CCTComparisonBomTestFile { get; set; }
        private string OutFolder { get; set; }
        private static readonly TestParam testParameters = new TestParam();


        [SetUp]
        public void Setup()
        {
            OutFolder = TestHelper.OutFolder;
            CCTComparisonBomTestFile = Path.GetFullPath(Path.Combine(OutFolder, "..", "..", "src", "SW360IntegrationTest", "PackageCreatorTestFiles", "Npm", "CCTComparisonBOMNpmUpdated.json"));

            if (!TestHelper.BOMCreated)
            {
                OutFolder = TestHelper.OutFolder;
                string packagjsonPath = Path.GetFullPath(Path.Combine(OutFolder, "..", "..", "TestFiles", "IntegrationTestFiles", "SystemTest2ndIterationData"));
                string bomPath = Path.GetFullPath(Path.Combine(OutFolder, "..", "Set2BOMs"));
                TestHelper.RunBOMCreatorExe(new string[]{
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
                TestConstant.Mode,"" });
            }
        }

        [Test, Order(1)]
        public void TestComponentCreatorExe()
        {
            string bomPath = Path.GetFullPath(Path.Combine(OutFolder, "..", "Set2BOMs"));
            // Assert
            // Check exit is normal
            Assert.AreEqual(0, TestHelper.RunComponentCreatorExe(new string[] {
                TestConstant.BomFolderPath,bomPath,
                TestConstant.Sw360Token, testParameters.SW360AuthTokenValue,
                TestConstant.SW360URL, testParameters.SW360URL,
                TestConstant.SW360AuthTokenType, testParameters.SW360AuthTokenType,
                TestConstant.SW360ProjectID, testParameters.SW360ProjectID,
                TestConstant.SW360ProjectName, testParameters.SW360ProjectName,
                TestConstant.FossologyURL, testParameters.FossUrl,
                TestConstant.EnableFossologyTrigger,testParameters.FossologyTrigger,
                TestConstant.TelemetryEnable, testParameters.TelemetryEnable,
                TestConstant.Mode,"" }),
                "Test to run Package Creator EXE execution");
        }

        [Test, Order(2)]
        public void TestComparisionBOMUpdation()
        {
            bool filecheck = false;

            // Expected
            ComponentJsonParsor expected = new ComponentJsonParsor();
            expected.Read(CCTComparisonBomTestFile);

            // Actual
            string generatedBOM = Path.GetFullPath(Path.Combine(OutFolder, "..", "Set2BOMs", $"{testParameters.SW360ProjectName}_Bom.cdx.json"));
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
            Assert.IsTrue(filecheck, "CycloneDx BOM not exist");
        }

        [Test, Order(3)]
        public async Task TestComponentCreation()
        {
            //Setting the httpclient
            var httpClient = new HttpClient() { };
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(TestConstant.TestSw360TokenType, TestConstant.TestSw360TokenValue);

            string expectedcomponentType = "OSS";
            string expectedname = "rxjs";
            //url formation for retrieving component details
            string url = TestConstant.Sw360ComponentApi + TestConstant.componentNameUrl + "rxjs";
            string responseBody = await httpClient.GetStringAsync(url); //GET request
            var responseData = JsonConvert.DeserializeObject<ComponentsModel>(responseBody);
            string name = responseData.Embedded.Sw360components[0].Name;
            string href = responseData.Embedded.Sw360components[0].Links.Self.Href;
            string hrefresponse = await httpClient.GetStringAsync(href);//GET request 
            var hrefData = JsonConvert.DeserializeObject<CreateComponent>(hrefresponse);
            string componentType = hrefData.ComponentType;

            //Assert
            Assert.AreEqual(expectedname, name);
            Assert.AreEqual(expectedcomponentType, componentType);


        }
        [Test, Order(4)]
        public async Task TestReleaseCreation_ClearingStateAsNewClearing()
        {
            //Setting the httpclient
            var httpClient = new HttpClient() { };
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(TestConstant.TestSw360TokenType, TestConstant.TestSw360TokenValue);
            //string expectedname = "zone.js";
            //string expectedversion = "0.10.3";
            //string expecteddownloadurl = "https://github.com/angular/angular.git";
            //string expectedexternalid = "pkg:npm/zone.js@0.10.3";
            string expectedclearingState = "NEW_CLEARING";
            //url formation for retrieving component details
            string url = TestConstant.Sw360ReleaseApi + TestConstant.componentNameUrl + "rxjs";
            string responseBody = await httpClient.GetStringAsync(url);//GET method         
            var responseData = JsonConvert.DeserializeObject<ReleaseIdOfComponent>(responseBody);
            string urlofreleaseid = responseData.Embedded.Sw360Releases[0].Links.Self.Href;
            string responseForRelease = await httpClient.GetStringAsync(urlofreleaseid);//GET method for fetching the release details
            var responseDataForRelease = JsonConvert.DeserializeObject<Releases>(responseForRelease);

            //string name = responseDataForRelease.Name;
            //string version = responseDataForRelease.Version;
            //string downloadurl = responseDataForRelease.SourceDownloadurl;
            //string externalid = responseDataForRelease.ExternalIds.Package_Url;
            string clearingState = responseDataForRelease.ClearingState;

            //Assert
            //Assert.AreEqual(expectedname, name, "Test Project Name");
            //Assert.AreEqual(expectedversion, version, "Test Project  Version");
            //Assert.AreEqual(expecteddownloadurl, downloadurl, "Test download Url of axios");
            //Assert.AreEqual(expectedexternalid, externalid, "Test component external id");
            Assert.AreEqual(expectedclearingState, clearingState);
        }

        [TearDown]
        public void TearDown()
        {
            // implement here
        }
    }
}
