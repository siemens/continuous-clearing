// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT

// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models;
using NUnit.Framework;
using System.IO;
using TestUtilities;
using File = System.IO.File;


namespace SW360IntegrationTest.Nuget
{
    [TestFixture, Order(43)]
    public class ComplianceCheckIntegrationNuget
    {
        private TestParamNuget testParameters;
        private string CCTComparisonBomTestFile { get; set; }
        private string OutFolder { get; set; }
        [SetUp]
        public void Setup()
        {
            testParameters = new TestParamNuget();
            OutFolder = TestHelper.OutFolder;
            CCTComparisonBomTestFile = Path.GetFullPath(Path.Combine(OutFolder, "..", "..", "src", "SW360IntegrationTest", "PackageCreatorTestFiles", "Nuget", "CCTComplienceCheckComparisonBOMNugetInitial.json"));

            if (!TestHelper.BOMCreated)
            {
                OutFolder = TestHelper.OutFolder;
                string packagejsonPath = Path.GetFullPath(Path.Combine(OutFolder, "..", "..", "TestFiles", "IntegrationTestFiles", "SystemTest1stIterationData", "ComplienceCheckNugetFolder"));
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
                TestConstant.JfrogNugetInternalRepo,"Nuget-test",
                TestConstant.ArtifactoryKey, testParameters.ArtifactoryUploadApiKey,
                TestConstant.TelemetryEnable, testParameters.TelemetryEnable,
                TestConstant.ProjectType,"NUGET",
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
                TestConstant.ProjectType,"NUGET",
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
    }
}