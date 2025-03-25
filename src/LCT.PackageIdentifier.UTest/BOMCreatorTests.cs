// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.APICommunications;
using Newtonsoft.Json;
using NUnit.Framework;
using System.IO;
using System.Xml;

namespace LCT.PackageIdentifier.UTest
{
    [TestFixture]
    public class BOMCreatorTest
    {
        [Test]
        public void WriteCycloneDXBOMToJSONFile_InputCycloneDxFile_ReturnsNoneEmptyArray()
        {
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string CCTComparisonBomTestFile = Path.GetFullPath(Path.Combine(outFolder, "PackageIdentifierUTTestFiles", "CycloneDX_Debian.cdx.json"));

            string json = "";
            if (File.Exists(CCTComparisonBomTestFile))
            {

                json = File.ReadAllText(CCTComparisonBomTestFile);

            }
            dynamic array = JsonConvert.DeserializeObject(json);
            Assert.IsNotNull(array);
            Assert.IsNotEmpty(array);
        }


        [Test]
        public void WriteToConfigurationFile()
        {
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string filepath = Path.GetFullPath(Path.Combine(outFolder, "PackageIdentifierUTTestFiles", "SW360ProjectInfo.xml"));

            string expectedproject_id = "42e86178b3b4fe8b8623788052002a6c";
            string expectedprojectname = "CCT";
            string project_id = "", project_name = "";
            if (File.Exists(filepath))
            {

                XmlDocument doc = new XmlDocument();
                doc.Load(filepath);
                XmlNodeList nodes = doc.DocumentElement.SelectNodes("/ProjectInfo");
                foreach (XmlNode node in nodes)
                {
                    project_id = node.SelectSingleNode("ProjectId").InnerText;
                    project_name = node.SelectSingleNode("ProjectName").InnerText;
                }

                Assert.AreEqual(expectedproject_id, project_id);
                Assert.AreEqual(expectedprojectname, project_name);
            }
        }

        [Test]
        public void GetProjectSummaryLink_ProvidedProjectId_ReturnsSW360Url()
        {
            //Arrange

            BomHelper bomHelper = new BomHelper();
            string url = "http:localhost:8090";
            string expected = $"{url}{ApiConstant.Sw360ProjectUrlApiSuffix}12345";
            //Act
            string actual = bomHelper.GetProjectSummaryLink("12345", url);

            //Assert
            Assert.That(expected, Is.EqualTo(actual), "Checks the project url");

        }
    }
}
