// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT

// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models;
using LCT.APICommunications;
using LCT.APICommunications.Interfaces;
using LCT.APICommunications.Model;
using LCT.PackageIdentifier;
using LCT.PackageIdentifier.Model;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading.Tasks;
using System.Xml;
using LCT.Common;

namespace PackageIdentifier.UTest
{
    public class Tests
    {
        [TestFixture]
        public class BOMCreatorTest
        {
            [Test]
            public void WriteCycloneDXBOMToJSONFile_InputCycloneDxFile_ReturnsNoneEmptyArray()
            {
                string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                string outFolder = Path.GetDirectoryName(exePath);
                string CCTComparisonBomTestFile = outFolder + @"\PackageIdentifierUTTestFiles\Cyclonedx.json";

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
                string filepath = outFolder + @"\PackageIdentifierUTTestFiles\SW360ProjectInfo.xml";

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
            public async Task IdentificationOfInternalComponents_FromExistingCycloneDXBOM_ReturnsListExcludingInternalComponents()
            {
                //Arrange
                BomKpiData bomKpiData = new BomKpiData();
                int expectedCount = 2;
                List<Component> list = new List<Component>()
                {
                    new Component()
                    {
                        Name = "sicharts",
                        Version = "1.0.2",
                        Purl = ""
                    },
                       new Component()
                    {
                        Name = "requirejs",
                        Version = "2.3.6",
                        Purl = ""
                    }

                };
               
                JfrogInfo jfrogInfo = new JfrogInfo() { Checksum = new Checksum() { Sha1 = "f0b0eb4e8061a46704c7142423774686ea79cd0e" } };

                CommonAppSettings CommonAppSettings = new CommonAppSettings();
                HttpResponseMessage httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ObjectContent<JfrogInfo>(jfrogInfo, new JsonMediaTypeFormatter(), "application/some-format")
                };
                Mock<IJFrogApiCommunication> jfrogCommunicationMck = new Mock<IJFrogApiCommunication>();
                jfrogCommunicationMck.Setup(x => x.CheckPackageAvailabilityInRepo(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(httpResponseMessage);
                NpmProcessor parser = new NpmProcessor();
                ComponentIdentification componentIdentification = new ComponentIdentification()
                {
                    comparisonBOMData = list
                };

                //Act
                ComponentIdentification result =await parser.IdentificationOfInternalComponents(componentIdentification, CommonAppSettings);

                //Assert
                Assert.That(expectedCount, Is.EqualTo(result.comparisonBOMData.Count), "Checks no of components in comparisonBOM");

            }
            [Test]
            public async Task IdentificationOfInternalComponents_FromExistingCycloneDXBomData_ReturnsListWithoutExcludingInternalComponents()
            {
                //Arrange
      
                int expectedCount = 1;
                List<Component> list = new List<Component>()
                {
                    new Component()
                    {
                        Name = "sicharts",
                        Version = "1.0.2",
                        Purl = ""
                    }

                };
                JfrogInfo jfrogInfo = new JfrogInfo() { Checksum = new Checksum() { Sha1 = "f0b0eb4e8061a46704c7142423774686ea79cd0e" } };

                CommonAppSettings CommonAppSettings = new CommonAppSettings();
                HttpResponseMessage httpResponseMessage = new HttpResponseMessage(HttpStatusCode.Unauthorized)
                {
                    Content = new ObjectContent<JfrogInfo>(jfrogInfo, new JsonMediaTypeFormatter(), "application/some-format")
                };
                Mock<IJFrogApiCommunication> jfrogCommunicationMck = new Mock<IJFrogApiCommunication>();
                jfrogCommunicationMck.Setup(x => x.CheckPackageAvailabilityInRepo(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(httpResponseMessage);
                NpmProcessor parser = new NpmProcessor();
                ComponentIdentification componentIdentification = new ComponentIdentification()
                {
                    comparisonBOMData = list
                };

                //Act
                ComponentIdentification result =await parser.IdentificationOfInternalComponents(componentIdentification, CommonAppSettings);

                //Assert
                Assert.That(expectedCount, Is.EqualTo(result.comparisonBOMData.Count), "Checks no of components in comparisonBOM");

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
}
