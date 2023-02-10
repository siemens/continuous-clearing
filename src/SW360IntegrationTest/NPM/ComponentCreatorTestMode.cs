// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT

// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models;
using LCT.APICommunications.Model;
using Newtonsoft.Json;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using TestUtilities;

namespace SW360IntegrationTest
{
    [TestFixture, Order(2)]
    class ComponentCreatorTestMode
    {
        private static readonly TestParam testParameters = new TestParam();

        [SetUp]
        public void Setup()
        {
            OutFolder = TestHelper.OutFolder;
            CCTComparisonBomTestFile = OutFolder + @"..\..\..\src\SW360IntegrationTest\PackageCreatorTestFiles\Npm\CCTComparisonBOMNpmInitial.json";

            if (!TestHelper.BOMCreated)
            {
                OutFolder = TestHelper.OutFolder;
                string packagjsonPath = OutFolder + @"\..\..\TestFiles\IntegrationTestFiles\SystemTest1stIterationData";
                string bomPath = OutFolder + @"\..\BOMs";
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
                TestConstant.ProjectType, "NPM",
                TestConstant.Mode,"test"
                });
            }
        }

        [Test, Order(1)]
        public void TestComponentCreatorExe_TestMode()
        {
            string bomPath = OutFolder + $"\\..\\BOMs\\{testParameters.SW360ProjectName}_Bom.cdx.json";
            // Assert
            // Check exit is normal
            Assert.AreEqual(0, TestHelper.RunComponentCreatorExe(new string[] {
                TestConstant.BomFilePath,bomPath,
                TestConstant.Sw360Token, testParameters.SW360AuthTokenValue,
                TestConstant.SW360URL, testParameters.SW360URL,
                TestConstant.SW360AuthTokenType, testParameters.SW360AuthTokenType,
                TestConstant.SW360ProjectID, testParameters.SW360ProjectID,
                TestConstant.SW360ProjectName, testParameters.SW360ProjectName,
                TestConstant.Mode,"test"
            }),
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
                        }
                    }

                }

            }
            Assert.IsTrue(filecheck, "CycloneDx BOM not exist");
        }




        [Test, Order(3)]
        public async Task TestComponentCreation_TestMode()
        {
            //Setting the httpclient
            var httpClient = new HttpClient() { };
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(TestConstant.TestSw360TokenType, TestConstant.TestSw360TokenValue);

            //url formation for retrieving component details
            string url = TestConstant.Sw360ComponentApi + TestConstant.componentNameUrl + "rxjs";
            string responseBody = await httpClient.GetStringAsync(url); //GET request
            var responseData = JsonConvert.DeserializeObject<ComponentsModel>(responseBody);
            //Assert
            Assert.IsTrue(responseData.Embedded.Sw360components.Count == 0);

        }

        private string CCTComparisonBomTestFile { get; set; }
        private string OutFolder { get; set; }
    }
}
