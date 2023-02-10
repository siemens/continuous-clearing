// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
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
using UnitTestUtilities;
using LCT.Common.Model;

namespace PackageIdentifier.UTest
{
    [TestFixture]
    public class BomHelperUnitTests
    {
        private readonly Mock<IProcessor> mockIProcessor = new Mock<IProcessor>();

        [TestCase]
        public async Task GetRepoDetails_GivenProjectTypeAsNPM_ReturnsListOFComponents()
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
                ProjectType = "NPM",
                Npm = new Config()
                {
                    JfrogNpmRepoList = new string[] { "here" }
                },
                JFrogApi = UTParams.JFrogURL

            };


            mockIProcessor.Setup(x => x.GetJfrogArtifactoryRepoInfo(It.IsAny<CommonAppSettings>(), It.IsAny<ArtifactoryCredentials>(), It.IsAny<Component>(), It.IsAny<string>())).ReturnsAsync(lstComponentForBOM);


            IParser parser = new NpmProcessor();

            //Act
            var expected = await parser.GetRepoDetails(lstComponentForBOM, appSettings);

            //Assert           
            Assert.AreEqual(expected.Count, lstComponentForBOM.Count);
        }
        [TestCase]
        public async Task GetRepoDetails_GivenProjectTypeAsNUGET_ReturnsListOFComponents()
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
                ProjectType = "NUGET",
                Nuget = new Config()
                {
                    JfrogNugetRepoList = new string[] { "here" }
                },
                JFrogApi = UTParams.JFrogURL
            };



            mockIProcessor.Setup(x => x.GetJfrogArtifactoryRepoInfo(It.IsAny<CommonAppSettings>(), It.IsAny<ArtifactoryCredentials>(), It.IsAny<Component>(), It.IsAny<string>())).ReturnsAsync(lstComponentForBOM);


            IParser parser = new NugetProcessor();

            //Act
            var expected = await parser.GetRepoDetails(lstComponentForBOM, appSettings);

            //Assert           
            Assert.AreEqual(expected.Count, lstComponentForBOM.Count);
        }
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
                    JfrogNpmRepoList = new string[] { "here" }
                },
                JFrogApi = "https://jfrogapi"
            };


            mockIProcessor.Setup(x => x.GetJfrogArtifactoryRepoInfo(It.IsAny<CommonAppSettings>(), It.IsAny<ArtifactoryCredentials>(), It.IsAny<Component>(), It.IsAny<string>())).ReturnsAsync(lstComponentForBOM);


            IParser parser = new DebianProcessor();

            //Act
            var expected = await parser.GetRepoDetails(lstComponentForBOM, appSettings);

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


    }
}
