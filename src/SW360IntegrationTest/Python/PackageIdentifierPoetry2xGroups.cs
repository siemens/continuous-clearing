// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 
using CycloneDX.Models;
using LCT.Common.Constants;
using NUnit.Framework;
using System.IO;
using System.Linq;
using TestUtilities;

namespace SW360IntegrationTest.Python
{
    [TestFixture, Order(60)]
    public class PackageIdentifierPoetry2XGroups
    {
        private string CCTLocalBomTestFile { get; set; }
        private string OutFolder { get; set; }
        TestParamNuget testParameters;

        [SetUp]
        public void Setup()
        {
            OutFolder = TestHelper.OutFolder;

            CCTLocalBomTestFile = Path.GetFullPath(Path.Combine(OutFolder, "..", "..", "src", "SW360IntegrationTest", "PackageIdentifierTestFiles", "Python", "CCTLocalBOMPoetry2xGroups.json"));

            if (!Directory.Exists(Path.GetFullPath(Path.Combine(OutFolder, "..", "Poetry2xGroupsBOMs"))))
            {
                Directory.CreateDirectory(Path.GetFullPath(Path.Combine(OutFolder, "..", "Poetry2xGroupsBOMs")));
            }
            testParameters = new TestParamNuget();
        }

        [Test, Order(1)]
        public void RunBOMCreatorexe_ProvidedPoetry2xLockFile_ReturnsSuccess()
        {
            string packagejsonPath = Path.GetFullPath(Path.Combine(OutFolder, "..", "..", "TestFiles", "IntegrationTestFiles", "Poetry2xGroupsTest"));
            string bomPath = Path.GetFullPath(Path.Combine(OutFolder, "..", "Poetry2xGroupsBOMs"));


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
                TestConstant.TelemetryEnable, testParameters.TelemetryEnable,
                TestConstant.SBOMSignVerify, testParameters.SBOMSignVerify,
                TestConstant.JfrogPoetryInternalRepo,"Pypi-test",
                TestConstant.ProjectType,"Poetry",
                TestConstant.Mode,""}),
                "Test to run Package Identifier EXE execution for Poetry 2.x groups");
        }

        [Test, Order(2)]
        public void LocalBOMCreation_AfterSuccessfulExeRun_ReturnsSuccess()
        {
            bool fileExist = false;

            // Expected
            ComponentJsonParsor expected = new ComponentJsonParsor();
            expected.Read(CCTLocalBomTestFile);

            // Actual
            string generatedBOM = Path.GetFullPath(Path.Combine(OutFolder, "..", "Poetry2xGroupsBOMs", $"{testParameters.SW360ProjectName}_Bom.cdx.json"));
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
            //Assert
            Assert.IsTrue(fileExist, "Test to verify BOM file is present");
        }

        [Test, Order(3)]
        public void LocalBOMCreation_VerifyPoetry2xGroupsDevDependency_MarkedCorrectly()
        {
            // Actual
            string generatedBOM = Path.GetFullPath(Path.Combine(OutFolder, "..", "Poetry2xGroupsBOMs", $"{testParameters.SW360ProjectName}_Bom.cdx.json"));
            Assert.IsTrue(File.Exists(generatedBOM), "Generated BOM file should exist");

            ComponentJsonParsor actual = new ComponentJsonParsor();
            actual.Read(generatedBOM);

            //Assert - pytest has groups = ["dev"], should be dev dependency
            var pytest = actual.Components.FirstOrDefault(c => c.Name == "pytest" && c.Version == "7.4.0");
            Assert.That(pytest, Is.Not.Null, "pytest should be found in generated BOM");
            var pytestDevProperty = pytest.Properties?.FirstOrDefault(p => p.Name == Dataconstant.Cdx_IsDevelopment);
            Assert.That(pytestDevProperty, Is.Not.Null, "pytest should have IsDevelopment property");
            Assert.AreEqual("true", pytestDevProperty.Value, "pytest with groups=[\"dev\"] should have IsDevelopment=true");
        }

        [Test, Order(4)]
        public void LocalBOMCreation_VerifyPoetry2xGroupsMainDependency_MarkedCorrectly()
        {
            // Actual
            string generatedBOM = Path.GetFullPath(Path.Combine(OutFolder, "..", "Poetry2xGroupsBOMs", $"{testParameters.SW360ProjectName}_Bom.cdx.json"));
            Assert.IsTrue(File.Exists(generatedBOM), "Generated BOM file should exist");

            ComponentJsonParsor actual = new ComponentJsonParsor();
            actual.Read(generatedBOM);

            //Assert - requests has groups = ["main"], should NOT be dev dependency
            var requests = actual.Components.FirstOrDefault(c => c.Name == "requests" && c.Version == "2.31.0");
            Assert.That(requests, Is.Not.Null, "requests should be found in generated BOM");
            var requestsDevProperty = requests.Properties?.FirstOrDefault(p => p.Name == Dataconstant.Cdx_IsDevelopment);
            Assert.That(requestsDevProperty, Is.Not.Null, "requests should have IsDevelopment property");
            Assert.AreEqual("false", requestsDevProperty.Value, "requests with groups=[\"main\"] should have IsDevelopment=false");
        }

        [Test, Order(5)]
        public void LocalBOMCreation_VerifyPoetry2xGroupsMainAndDev_MarkedAsProduction()
        {
            // Actual
            string generatedBOM = Path.GetFullPath(Path.Combine(OutFolder, "..", "Poetry2xGroupsBOMs", $"{testParameters.SW360ProjectName}_Bom.cdx.json"));
            Assert.IsTrue(File.Exists(generatedBOM), "Generated BOM file should exist");

            ComponentJsonParsor actual = new ComponentJsonParsor();
            actual.Read(generatedBOM);

            //Assert - coverage has groups = ["main", "dev"], main takes precedence
            var coverage = actual.Components.FirstOrDefault(c => c.Name == "coverage" && c.Version == "7.3.0");
            Assert.That(coverage, Is.Not.Null, "coverage should be found in generated BOM");
            var coverageDevProperty = coverage.Properties?.FirstOrDefault(p => p.Name == Dataconstant.Cdx_IsDevelopment);
            Assert.That(coverageDevProperty, Is.Not.Null, "coverage should have IsDevelopment property");
            Assert.AreEqual("false", coverageDevProperty.Value, "coverage with groups=[\"main\", \"dev\"] should have IsDevelopment=false (main takes precedence)");
        }

    }
}