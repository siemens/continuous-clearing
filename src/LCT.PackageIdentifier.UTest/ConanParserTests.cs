﻿// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models;
using LCT.APICommunications.Model.AQL;
using LCT.Common;
using LCT.Common.Model;
using LCT.PackageIdentifier;
using LCT.PackageIdentifier.Interface;
using LCT.PackageIdentifier.Model;
using LCT.Services.Interface;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LCT.PackageIdentifier.UTest
{
    [TestFixture]
    public class ConanParserTests
    {
        [TestCase]
        public void ParseLockFile_GivenAInputFilePath_ReturnsSuccess()
        {
            //Arrange
            int expectedNoOfcomponents = 17;
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string packagefilepath = outFolder + @"\PackageIdentifierUTTestFiles";

            string[] Includes = { "conan.lock" };
            Config config = new Config()
            {
                Include = Includes
            };

            CommonAppSettings appSettings = new CommonAppSettings()
            {
                PackageFilePath = packagefilepath,
                Conan = config
            };
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();

            //Act
            Bom listofcomponents = new ConanProcessor(cycloneDXBomParser.Object).ParsePackageFile(appSettings);

            //Assert
            Assert.That(expectedNoOfcomponents, Is.EqualTo(listofcomponents.Components.Count), "Checks for no of components");

        }

        [TestCase]
        public void ParseLockFile_GivenAInputFilePath_ReturnDevDependentComp()
        {
            //Arrange
            string IsDev = "true";
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string packagefilepath = outFolder + @"\PackageIdentifierUTTestFiles";

            string[] Includes = { "conan.lock" };
            Config config = new Config()
            {
                Include = Includes
            };

            CommonAppSettings appSettings = new CommonAppSettings()
            {
                PackageFilePath = packagefilepath,
                Conan = config
            };
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();

            //Act
            Bom listofcomponents = new ConanProcessor(cycloneDXBomParser.Object).ParsePackageFile(appSettings);
            var IsDevDependency = listofcomponents.Components.Find(a => a.Name == "googletest")
                .Properties.First(x => x.Name == "internal:siemens:clearing:development").Value;

            //Assert
            Assert.That(IsDev, Is.EqualTo(IsDevDependency), "Checks if Dev Dependency Component or not");

        }

        [TestCase]
        public void ParseLockFile_GivenAInputFilePathExcludeComponent_ReturnComponentCount()
        {
            //Arrange
            int totalComponentsAfterExclusion = 15;
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string packagefilepath = outFolder + @"\PackageIdentifierUTTestFiles";

            string[] Includes = { "conan.lock" };
            Config config = new Config()
            {
                Include = Includes,
                ExcludedComponents = new List<string> { "openldap:2.6.4-shared-ossl3.1", "libcurl:7.87.0-shared-ossl3.1" }
            };

            CommonAppSettings appSettings = new CommonAppSettings()
            {
                PackageFilePath = packagefilepath,
                Conan = config
            };
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();

            //Act
            Bom listofcomponents = new ConanProcessor(cycloneDXBomParser.Object).ParsePackageFile(appSettings);

            //Assert
            Assert.That(totalComponentsAfterExclusion, Is.EqualTo(listofcomponents.Components.Count), "Checks if the excluded components have been removed");
        }

        [TestCase]
        public void IsDevDependent_GivenListOfDevComponents_ReturnsSuccess()
        {
            //Arrange
            var conanPackage = new ConanPackage() {Id = "10"};
            var buildNodeIds = new List<string> { "10", "11", "12" };
            var noOfDevDependent = 0;
            //Act
            bool actual = ConanProcessor.IsDevDependency(conanPackage, buildNodeIds, ref noOfDevDependent);

            //Assert
            Assert.That(true, Is.EqualTo(actual), "Component is a dev dependent");
        }

        [Test]
        public async Task IdentificationOfInternalComponents_ReturnsComponentData_Successfully()
        {
            // Arrange
            Component component = new Component()
            { 
                Name = "securitycommunicationmanager",
                Description = string.Empty,
                Version = "2.6.5",
                Purl = "pkg:conan/securitycommunicationmanager@2.6.5"
            };
            
            var components = new List<Component>() { component };
            ComponentIdentification componentIdentification = new() { comparisonBOMData = components };
            string[] repoList = { "internalrepo1", "internalrepo2" };
            CommonAppSettings appSettings = new() { InternalRepoList = repoList };

            AqlResult aqlResult = new()
            {
                Name = "index.json",
                Path = "siemens-energy/securitycommunicationmanager/2.7.1/stable",
                Repo = "internalrepo1"
            };

            List<AqlResult> results = new List<AqlResult>() { aqlResult };
            Mock<IJFrogService> mockJfrogService = new Mock<IJFrogService>();
            Mock<IBomHelper> mockBomHelper = new Mock<IBomHelper>();
            mockBomHelper.Setup(m => m.GetListOfComponentsFromRepo(It.IsAny<string[]>(), It.IsAny<IJFrogService>()))
                .ReturnsAsync(results);
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();

            // Act
            ConanProcessor conanProcessor = new ConanProcessor(cycloneDXBomParser.Object);
            var actual = await conanProcessor.IdentificationOfInternalComponents(componentIdentification, appSettings, mockJfrogService.Object, mockBomHelper.Object);

            // Assert
            Assert.That(actual, Is.Not.Null);
        }

        [Test]
        public async Task GetJfrogRepoDetailsOfAComponent_ReturnsWithData_SuccessFully()
        {
            // Arrange
            Component component = new Component()
            {
                Name = "securitycommunicationmanager",
                Description = string.Empty,
                Version = "2.6.5",
                Purl = "pkg:conan/securitycommunicationmanager@2.6.5"
            };
            var components = new List<Component>() { component };
            string[] repoList = { "internalrepo1", "internalrepo2" };
            CommonAppSettings appSettings = new();
            appSettings.Conan = new LCT.Common.Model.Config() { JfrogConanRepoList = repoList };
            AqlResult aqlResult = new()
            {
                Name = "index.json",
                Path = "siemens-energy/securitycommunicationmanager/2.6.5/stable",
                Repo = "internalrepo1"
            };

            List<AqlResult> results = new List<AqlResult>() { aqlResult };

            Mock<IJFrogService> mockJfrogService = new Mock<IJFrogService>();
            Mock<IBomHelper> mockBomHelper = new Mock<IBomHelper>();
            mockBomHelper.Setup(m => m.GetListOfComponentsFromRepo(It.IsAny<string[]>(), It.IsAny<IJFrogService>()))
                .ReturnsAsync(results);
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();

            // Act
            ConanProcessor conanProcessor = new ConanProcessor(cycloneDXBomParser.Object);
            var actual = await conanProcessor.GetJfrogRepoDetailsOfAComponent(
                components, appSettings, mockJfrogService.Object, mockBomHelper.Object);
            var reponameActual = actual.First(x => x.Properties[0].Name == "internal:siemens:clearing:jfrog-repo-name").Properties[0].Value;

            // Assert
            Assert.That(actual, Is.Not.Null);
            Assert.That(aqlResult.Repo, Is.EqualTo(reponameActual));
        }

        [Test]
        public async Task GetArtifactoryRepoName_Conan_ReturnsNotFound_ReturnsFailure()
        {
            // Arrange
            Component component = new Component()
            {
                Name = "securitycommunicationmanager",
                Description = string.Empty,
                Version = "2.6.5",
                Purl = "pkg:conan/securitycommunicationmanager@2.6.5"
            };
            var components = new List<Component>() { component };
            string[] repoList = { "internalrepo1", "internalrepo2" };
            CommonAppSettings appSettings = new();
            appSettings.Conan = new LCT.Common.Model.Config() { JfrogConanRepoList = repoList };
            AqlResult aqlResult = new()
            {
                Name = "index.json",
                Path = "siemens-energy/securitycommunicationmanager/2.7.1/stable",
                Repo = "internalrepo1"
            };

            List<AqlResult> results = new() { aqlResult };

            Mock<IJFrogService> mockJfrogService = new Mock<IJFrogService>();
            Mock<IBomHelper> mockBomHelper = new Mock<IBomHelper>();
            mockBomHelper.Setup(m => m.GetListOfComponentsFromRepo(It.IsAny<string[]>(), It.IsAny<IJFrogService>()))
                .ReturnsAsync(results);
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();

            // Act
            ConanProcessor conanProcessor = new ConanProcessor(cycloneDXBomParser.Object);
            var actual = await conanProcessor.GetJfrogRepoDetailsOfAComponent(
                components, appSettings, mockJfrogService.Object, mockBomHelper.Object);

            var reponameActual = actual.First(x => x.Properties[0].Name == "internal:siemens:clearing:jfrog-repo-name").Properties[0].Value;

            Assert.That("Not Found in JFrogRepo", Is.EqualTo(reponameActual));
        }

        [Test]
        public void ParsePackageConfig_GivenAInputFilePathAlongWithSBOMTemplate_ReturnTotalComponentsList()
        {
            //Arrange
            int expectednoofcomponents = 0;
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string OutFolder = Path.GetDirectoryName(exePath);
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();

            ConanProcessor conanProcessor = new ConanProcessor(cycloneDXBomParser.Object);
            string[] Includes = { "SBOM_ConanCATemplate.cdx.json" };
            string packagefilepath = OutFolder + @"\PackageIdentifierUTTestFiles";

            CommonAppSettings appSettings = new CommonAppSettings()
            {
                PackageFilePath = packagefilepath,
                ProjectType = "CONAN",
                RemoveDevDependency = true,
                Conan = new Config() { Include = Includes },
                CycloneDxSBomTemplatePath = packagefilepath + "\\SBOMTemplates\\SBOM_ConanCATemplate.cdx.json"
            };

            //Act
            Bom listofcomponents = conanProcessor.ParsePackageFile(appSettings);

            //Assert
            Assert.That(expectednoofcomponents, Is.EqualTo(listofcomponents.Components.Count), "Checks for no of components");
        }
    }
}
