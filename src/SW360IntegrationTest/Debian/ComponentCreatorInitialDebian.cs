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

namespace SW360IntegrationTest.Debian
{
    [TestFixture, Order(18)]
    class ComponentCreatorInitialDebian
    {
        private TestParamDebian testParameters;

        [SetUp]
        public void Setup()
        {
            testParameters = new TestParamDebian();
            OutFolder = TestHelper.OutFolder;
            CCTComparisonBomTestFile = OutFolder + @"..\..\..\src\SW360IntegrationTest\PackageCreatorTestFiles\Debian\CCTComparisonBOMDebianInitial.json";

            if (!TestHelper.BOMCreated)
            {
                OutFolder = TestHelper.OutFolder;
                string packagejsonPath = OutFolder + @"\..\..\TestFiles\IntegrationTestFiles\SystemTest1stIterationData\Debian";
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
                TestConstant.JfrogDebianInternalRepo,"Debian-test",
                TestConstant.ProjectType,"DEBIAN",
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
                TestConstant.ProjectType,"DEBIAN",
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
            string expectedname = "adduser";
            //url formation for retrieving component details
            string url = TestConstant.Sw360ComponentApi + TestConstant.componentNameUrl + "adduser";
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
            string expectedname = "adduser";
            string expectedversion = "3.118.debian";
            string expecteddownloadurl = "https://snapshot.debian.org/archive/debian/20180915T211528Z/pool/main/a/adduser/adduser_3.118.tar.xz";
            string expectedexternalid = "pkg:deb/debian/adduser@3.118?arch=source";
            //url formation for retrieving component details
            string url = TestConstant.Sw360ReleaseApi + TestConstant.componentNameUrl + "adduser";
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

        private string CCTComparisonBomTestFile { get; set; }
        private string OutFolder { get; set; }
    }
}
