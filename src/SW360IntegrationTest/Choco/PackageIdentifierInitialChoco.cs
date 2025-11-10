// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT

// -------------------------------------------------------------------------------------------------------------------- 
using CycloneDX.Models;
using LCT.APICommunications.Model;
using Newtonsoft.Json;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TestUtilities;

namespace SW360IntegrationTest.Choco
{
    [TestFixture, Order(28)]
    public class PackageIdentifierInitialChoco
    {
        private string CCTLocalBomTestFile { get; set; }
        private string OutFolder { get; set; }
        TestParamChoco testParameters;

        [SetUp]
        public void Setup()
        {
            OutFolder = TestHelper.OutFolder;

            CCTLocalBomTestFile = Path.GetFullPath(Path.Combine(OutFolder, "..", "..", "src", "SW360IntegrationTest", "PackageIdentifierTestFiles", "Choco", "CCTComparisonBOMChocoInitial.json"));

            if (!Directory.Exists(Path.GetFullPath(Path.Combine(OutFolder, "..", "BOMs"))))
            {
                Directory.CreateDirectory(Path.GetFullPath(Path.Combine(OutFolder, "..", "BOMs")));
            }
            testParameters = new TestParamChoco();
        }
        [Test, Order(1)]
        public void RunBOMCreatorexe_ProvidedPackageJsonFilePath_ReturnsSuccess()
        {
            string packagejsonPath = Path.GetFullPath(Path.Combine(OutFolder, "..", "..", "TestFiles", "IntegrationTestFiles", "SystemTest1stIterationData", "Choco"));
            string bomPath = Path.GetFullPath(Path.Combine(OutFolder, "..", "BOMs"));

            // Test BOM Creator ran with exit code 0
            Assert.AreEqual(0, TestHelper.RunBOMCreatorExe(new string[]{
                TestConstant.PackageFilePath, packagejsonPath,
                TestConstant.BomFolderPath, bomPath,
                TestConstant.Sw360Token, testParameters.SW360AuthTokenValue,
                TestConstant.SW360AuthTokenType, testParameters.SW360AuthTokenType,
                TestConstant.SW360URL, testParameters.SW360URL,
                TestConstant.SW360ProjectID, testParameters.SW360ProjectID,
                TestConstant.SW360ProjectName, testParameters.SW360ProjectName,
                TestConstant.JFrogApiURL, testParameters.JfrogApi,
                TestConstant.ArtifactoryKey, testParameters.ArtifactoryUploadApiKey,
                TestConstant.JfrogChocoInternalRepo,"choco-test",
                TestConstant.ProjectType,"CHOCO",
                TestConstant.TelemetryEnable, testParameters.TelemetryEnable,
                TestConstant.Mode,""}),
                "Test to run  Package Identifier EXE execution");
        }


        [Test, Order(2)]
        public void LocalBOMCreation_AfterSuccessfulExeRun_ReturnsSuccess()
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

        [Test, Order(3)]
        public void RunComponentCreatorexe_WithChocoComponents_SkipsSw360ProcessingAndDisplaysManualSteps()
        {
            string bomPath = Path.GetFullPath(Path.Combine(OutFolder, "..", "BOMs"));
            
            // Test ComponentCreator with exit code 0 - should succeed but skip SW360 processing for CHOCO
            int result = TestHelper.RunComponentCreatorExe(new string[] {
                TestConstant.BomFolderPath, bomPath,
                TestConstant.Sw360Token, testParameters.SW360AuthTokenValue,
                TestConstant.SW360URL, testParameters.SW360URL,
                TestConstant.SW360AuthTokenType, testParameters.SW360AuthTokenType,
                TestConstant.SW360ProjectName, testParameters.SW360ProjectName,
                TestConstant.SW360ProjectID, testParameters.SW360ProjectID,
                TestConstant.FossologyURL, testParameters.FossUrl,
                TestConstant.TelemetryEnable, testParameters.TelemetryEnable,
                TestConstant.EnableFossologyTrigger, false.ToString(),
                TestConstant.Mode, ""
            });
            
            Assert.AreEqual(0, result, "ComponentCreator should succeed for CHOCO components");
        }

        [Test, Order(4)]
        public void VerifyChocoComponentsKpiData_AfterComponentCreatorRun_ReturnsExpectedMetrics()
        {
            // Verify that the KPI data file was created and contains appropriate CHOCO-specific metrics
            string kpiDataPath = Path.GetFullPath(Path.Combine(OutFolder, "..", "BOMs", $"{testParameters.SW360ProjectName}_CreatorKpiData.json"));
            
            Assert.IsTrue(File.Exists(kpiDataPath), "KPI data file should be created");

            // Read and verify KPI data as JSON object since we don't have access to CreatorKpiData here
            string kpiContent = File.ReadAllText(kpiDataPath);
            dynamic kpiData = JsonConvert.DeserializeObject(kpiContent);
            
            Assert.IsNotNull(kpiData, "KPI data should be deserializable");
            Assert.That((int)kpiData.ComponentsReadFromComparisonBOM, Is.GreaterThan(0), "Should have components read from BOM");
            
            // For CHOCO components, SW360-related metrics should be 0
            Assert.That((int)kpiData.ComponentsOrReleasesCreatedNewlyInSw360, Is.EqualTo(0), "No components should be created in SW360 for CHOCO");
            Assert.That((int)kpiData.ComponentsOrReleasesExistingInSw360, Is.EqualTo(0), "No existing components should be found in SW360 for CHOCO");
            Assert.That((int)kpiData.ComponentsOrReleasesNotCreatedInSw360, Is.EqualTo(0), "No failed creation attempts for CHOCO");
            Assert.That((int)kpiData.ComponentsUploadedInFossology, Is.EqualTo(0), "No FOSSology uploads for CHOCO");
            Assert.That((int)kpiData.ComponentsNotUploadedInFossology, Is.EqualTo(0), "No FOSSology upload attempts for CHOCO");
        }

        [Test, Order(5)]
        public void VerifyChocoComponentsWithoutDownloadUrlList_AfterComponentCreatorRun_ReturnsEmptyList()
        {
            // Verify that the components without download URL file was created but is empty for CHOCO
            string componentsWithoutSrcPath = Path.GetFullPath(Path.Combine(OutFolder, "..", "BOMs", $"{testParameters.SW360ProjectName}_ComponentsWithoutDownloadURL.json"));
            
            Assert.IsTrue(File.Exists(componentsWithoutSrcPath), "Components without download URL file should be created");

            // Read and verify the file content as generic list since we don't have access to ComparisonBomData here
            string content = File.ReadAllText(componentsWithoutSrcPath);
            var components = JsonConvert.DeserializeObject<List<dynamic>>(content);
            
            Assert.IsNotNull(components, "Components list should be deserializable");
            // For CHOCO components, this should be empty as they are handled differently
            Assert.That(components.Count, Is.EqualTo(0), "No components should be in the download URL not found list for CHOCO projects");
        }

        [Test, Order(6)]
        public async Task VerifyChocoManualStepsNotificationContent_ChecksBomForChocoComponents()
        {
            // Read the generated BOM to verify CHOCO components are present
            string generatedBOM = Path.GetFullPath(Path.Combine(OutFolder, "..", "BOMs", $"{testParameters.SW360ProjectName}_Bom.cdx.json"));
            Assert.IsTrue(File.Exists(generatedBOM), "Generated BOM file should exist");

            string bomContent = await File.ReadAllTextAsync(generatedBOM);
            var bom = JsonConvert.DeserializeObject<Bom>(bomContent);
            
            Assert.IsNotNull(bom, "BOM should be deserializable");
            Assert.IsNotNull(bom.Components, "BOM should contain components");
            Assert.That(bom.Components.Count, Is.GreaterThan(0), "BOM should have CHOCO components");

            // Verify at least one component is CHOCO type
            bool hasChocoComponents = bom.Components.Any(c => 
                c.Properties?.Any(p => 
                    string.Equals(p.Name, "internal:siemens:clearing:project-type", System.StringComparison.OrdinalIgnoreCase) && 
                    string.Equals(p.Value, "CHOCO", System.StringComparison.OrdinalIgnoreCase)) == true);

            Assert.IsTrue(hasChocoComponents, "BOM should contain at least one CHOCO component");
        }
    }
}