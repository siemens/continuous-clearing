// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Siemens AG">
//   Copyright (C) Siemens AG 2023. All rights reserved. 
// </copyright>
//<license>MIT</license>
// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models;
using NUnit.Framework;
using System.IO;
using TestUtilities;

namespace SW360IntegrationTest
{
    [TestFixture, Order(19)]
    public class PackageIdentifierInitialMaven
    {
        private string CCTLocalBomTestFile { get; set; }
        private string OutFolder { get; set; }
        TestParamNuget testParameters;

        [SetUp]
        public void Setup()
        {
            OutFolder = TestHelper.OutFolder;

            CCTLocalBomTestFile = OutFolder + @"..\..\..\src\SW360IntegrationTest\PackageIdentifierTestFiles\Maven\CCTLocalBOMMavenInitial.json";

            if (!Directory.Exists(OutFolder + @"\..\BOMs"))
            {
                Directory.CreateDirectory(OutFolder + @"\..\BOMs");
            }
            testParameters = new TestParamNuget();
        }

        [Test, Order(1)]
        public void RunBOMCreatorexe_ProvidedPackageJsonFilePath_ReturnsSuccess()
        {
            string packagejsonPath = OutFolder + @"\..\..\TestFiles\IntegrationTestFiles\SystemTest1stIterationData\Maven";
            string bomPath = OutFolder + @"\..\BOMs";

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
                TestConstant.ProjectType,"MAVEN",
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
            string generatedBOM = OutFolder + $"\\..\\BOMs\\{testParameters.SW360ProjectName}_Bom.cdx.json";
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