// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Siemens AG">
//   Copyright (C) Siemens AG 2023. All rights reserved. 
// </copyright>
//<license>MIT</license>
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

namespace SW360IntegrationTest.Maven
{
    [TestFixture, Order(20)]
    public class ComponentCreatorInitialMaven
    {
        private TestParamNuget testParameters;

        [SetUp]
        public void Setup()
        {
            testParameters = new TestParamNuget();
            OutFolder = TestHelper.OutFolder;
            CCTComparisonBomTestFile = Path.GetFullPath(Path.Combine(OutFolder, "..", "..", "src", "SW360IntegrationTest", "PackageCreatorTestFiles", "Maven", "CCTComparisonBOMMavenUpdated.json"));

            if (!TestHelper.BOMCreated)
            {
                OutFolder = TestHelper.OutFolder;
                string packagejsonPath = Path.GetFullPath(Path.Combine(OutFolder, "..", "..", "TestFiles", "IntegrationTestFiles", "SystemTest1stIterationData", "Maven"));
                string bomPath = Path.GetFullPath(Path.Combine(OutFolder, "..", "BOMs"));
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
                TestConstant.JfrogMavenInternalRepo,"Maven-test",
                TestConstant.TelemetryEnable, testParameters.TelemetryEnable,
                TestConstant.ProjectType,"MAVEN",
                TestConstant.Mode,""});
            }
        }

        [Test, Order(1)]
        public void ComponentCreatorExe_ProvidedBOMFilePath_ReturnsSuccess()
        {
            string bomPath = Path.GetFullPath(Path.Combine(OutFolder, "..", "BOMs"));
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
                TestConstant.Mode,""}),
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
                            Assert.AreEqual(item.Properties.Count, component.Properties.Count);

                        }
                    }

                }
            }
            Assert.IsTrue(filecheck, "CycloneDx BOM not exist");
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
            string expectedname = "joda-time";
            //url formation for retrieving component details
            string url = TestConstant.Sw360ComponentApi + TestConstant.componentNameUrl + "joda-time";
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
            string expectedname = "joda-time";
            string expectedversion = "2.9.2";
            string expectedclearingState = "NEW_CLEARING";
            string expecteddownloadurl = "https://repo.maven.apache.org/maven2/joda-time/joda-time/2.9.2/joda-time-2.9.2-sources.jar";
            string expectedexternalid = "pkg:maven/joda-time/joda-time@2.9.2";
            //url formation for retrieving component details
            string url = TestConstant.Sw360ReleaseApi + TestConstant.componentNameUrl + "joda-time";
            string responseBody = await httpClient.GetStringAsync(url);//GET method         
            var responseData = JsonConvert.DeserializeObject<ReleaseIdOfComponent>(responseBody);
            string urlofreleaseid = responseData.Embedded.Sw360Releases[0].Links.Self.Href;
            string responseForRelease = await httpClient.GetStringAsync(urlofreleaseid);//GET method for fetching the release details
            var responseDataForRelease = JsonConvert.DeserializeObject<Releases>(responseForRelease);

            string name = responseDataForRelease.Name;
            string version = responseDataForRelease.Version;
            string downloadurl = responseDataForRelease.SourceDownloadurl;
            string clearingState = responseDataForRelease.ClearingState;
            string externalid = responseDataForRelease.ExternalIds.Package_Url;

            //Assert
            Assert.AreEqual(expectedname, name, "Test Project Name");
            Assert.AreEqual(expectedversion, version, "Test Project  Version");
            Assert.AreEqual(expecteddownloadurl, downloadurl, "Test download Url of Entity Framework");
            Assert.AreEqual(expectedclearingState, clearingState, "Test component clearing state");
            Assert.AreEqual(expectedexternalid, externalid, "Test component external id");
        }

        private string CCTComparisonBomTestFile { get; set; }
        private string OutFolder { get; set; }
    }
}