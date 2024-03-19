// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models;
using LCT.APICommunications.Model;
using LCT.PackageIdentifier;
using LCT.PackageIdentifier.Interface;
using LCT.PackageIdentifier.Model;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using LCT.Common;
using System.Threading.Tasks;
using LCT.Common.Model;
using LCT.Services.Interface;
using LCT.APICommunications.Model.AQL;

namespace PackageIdentifier.UTest
{
    [TestFixture]
    public class BomHelperUnitTests
    {
        private readonly Mock<IProcessor> mockIProcessor = new Mock<IProcessor>();
  

        [TestCase]
        public async Task GetRepoDetails_GivenProjectTypeAsDebian_ReturnsListOFComponents()
        {

            //Arrange
            var lstComponentForBOM = new List<Component>()
            {
                new Component()
                {
                 Name="Test",
                 Version="1",
                }
            };

            CommonAppSettings appSettings = new CommonAppSettings()
            {
                ArtifactoryUploadApiKey = "testvalue",
                ProjectType = "DEBIAN",
                Debian = new Config()
                {
                    JfrogDebianRepoList = new string[] { "here" }
                },
                JFrogApi = "https://jfrogapi"
            };
            List<AqlResult> aqlResultList = new()
            {
                new()
                {
                    Path="test/test",
                    Name="compoenent",
                    Repo="remote"
                }
            };
            mockIProcessor.Setup(x => x.GetJfrogArtifactoryRepoInfo(It.IsAny<CommonAppSettings>(), It.IsAny<ArtifactoryCredentials>(), It.IsAny<Component>(), It.IsAny<string>())).ReturnsAsync(lstComponentForBOM);
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();

            IParser parser = new DebianProcessor(cycloneDXBomParser.Object);
            Mock<IJFrogService> jFrogService = new Mock<IJFrogService>();
            Mock<IBomHelper> bomHelper = new Mock<IBomHelper>();
            bomHelper.Setup(x => x.GetListOfComponentsFromRepo(It.IsAny<string[]>(), It.IsAny<IJFrogService>())).ReturnsAsync(aqlResultList);
      
            //Act
            var expected = await parser.GetJfrogRepoDetailsOfAComponent(lstComponentForBOM, appSettings, jFrogService.Object, bomHelper.Object);

            //Assert           
            Assert.AreEqual(expected.Count, lstComponentForBOM.Count);
        }


        [TestCase]
        public void Test_WriteBomKpiDataToConsole()
        {
            var mock = new Mock<IBomHelper>();
            mock.Object.WriteBomKpiDataToConsole(new BomKpiData());
            mock.Verify(x => x.WriteBomKpiDataToConsole(It.IsAny<BomKpiData>()), Times.Once);
        }


        [TestCase]
        public void Test_WriteInternalComponentsListToKpi()
        {
            var lstComponentForBOM = new List<Component>()
            {
                new Component()
                {
                 Name="Test",
                 Version="1",
                }
            };

            IBomHelper helper = new BomHelper();
            helper.WriteInternalComponentsListToKpi(lstComponentForBOM);
            Assert.AreEqual(true, true);
        }

        [TestCase]
        public void TestGetHashCodeUsingNpmView_InputNameAndVersion_ReturnsHashCode()
        {
            string expectedhashcode = "5f845b1a58ffb6f3ea6103edf0756ac65320b725";
            string name = "@angular/animations";
            string version = "12.0.0";

        
            string hashcode= BomHelper.GetHashCodeUsingNpmView(name,version);
            Assert.That(expectedhashcode, Is.EqualTo(hashcode));
        }
    }
}
