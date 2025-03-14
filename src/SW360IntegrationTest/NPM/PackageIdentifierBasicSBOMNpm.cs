using CycloneDX.Models;
using System.IO;
using NUnit.Framework;
using TestUtilities;

namespace SW360IntegrationTest.NPM
{
    [TestFixture, Order(33)]
    public class PackageIdentifierBasicSBOMNpm
    {
        private string CCTLocalBomTestFile { get; set; }
        private string OutFolder { get; set; }
        private static readonly TestParam testParameters = new TestParam();

        [SetUp]
        public void Setup()
        {
            OutFolder = TestHelper.OutFolder;

            CCTLocalBomTestFile = OutFolder + @"..\..\..\src\SW360IntegrationTest\PackageIdentifierTestFiles\Npm\CCTLocalBOMNpmInitial.json";

            if (!Directory.Exists(OutFolder + @"\..\BOMs"))
            {
                Directory.CreateDirectory(OutFolder + @"\..\BOMs");
            }
        }

        [Test, Order(1)]
        public void TestBOMCreatorexe()
        {
            string packagjsonPath = OutFolder + @"\..\..\TestFiles\IntegrationTestFiles\SystemTest1stIterationData\Npm";
            string bomPath = OutFolder + @"\..\BOMs";

            // Test BOM Creator ran with exit code 0
            Assert.AreEqual(0, TestHelper.RunBOMCreatorExe(new string[]{
                TestConstant.PackageFilePath, packagjsonPath,
                TestConstant.BomFolderPath, bomPath,                
                TestConstant.ProjectType, "Npm",
                TestConstant.BasicSBOM, testParameters.BasicSBOMEnable,
                 TestConstant.Mode,""}),
                "Test to run Package Identifier EXE execution");
        }

        [Test, Order(2)]
        public void TestLocalBOMCreation()
        {
            bool fileExist = false;

            // Expected
            ComponentJsonParsor expected = new ComponentJsonParsor();
            expected.Read(CCTLocalBomTestFile);

            // Actual
            string generatedBOM = OutFolder + $"\\..\\BOMs\\CycloneDX_Bom.cdx.json";
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
