using CycloneDX.Models;
using System.IO;
using NUnit.Framework;
using TestUtilities;

namespace SW360IntegrationTest.Python
{
    [TestFixture, Order(35)]
    public class PackageIdentifierBasicSBOMPython
    {
        private string CCTLocalBomTestFile { get; set; }
        private string OutFolder { get; set; }
        TestParamNuget testParameters;

        [SetUp]
        public void Setup()
        {
            OutFolder = TestHelper.OutFolder;

            CCTLocalBomTestFile = OutFolder + @"..\..\..\src\SW360IntegrationTest\PackageIdentifierTestFiles\Python\CCTLocalBOMPythonInitial.json";

            if (!Directory.Exists(OutFolder + @"\..\BOMs"))
            {
                Directory.CreateDirectory(OutFolder + @"\..\BOMs");
            }
            testParameters = new TestParamNuget();
        }

        [Test, Order(1)]
        public void RunBOMCreatorexe_ProvidedPackageJsonFilePath_ReturnsSuccess()
        {
            string packagejsonPath = OutFolder + @"\..\..\TestFiles\IntegrationTestFiles\SystemTest1stIterationData\Python";
            string bomPath = OutFolder + @"\..\BOMs";
            string appsettingsFilePath = @"BasicSBOMAppsettingsTest.json";

            // Test BOM Creator ran with exit code 0
            Assert.AreEqual(0, TestHelper.RunBOMCreatorExe(new string[]{
                TestConstant.PackageFilePath, packagejsonPath,
                TestConstant.BomFolderPath, bomPath,
                TestConstant.Appsettings,appsettingsFilePath,
                TestConstant.ProjectType,"Poetry",
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
            string generatedBOM = OutFolder + $"\\..\\BOMs\\ContinuousClearing_Bom.cdx.json";
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
            Assert.IsTrue(fileExist, "Test to BOM file present");
        }
    }
}
