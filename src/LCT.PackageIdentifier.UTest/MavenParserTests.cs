// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// --------------------------------------------------------------------------------------------------------------------

using CycloneDX.Models;
using LCT.Common;
using LCT.Common.Model;
using LCT.PackageIdentifier.Model;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using LCT.APICommunications.Model.AQL;
using LCT.PackageIdentifier.Interface;
using LCT.Services.Interface;
using Moq;
using System.Threading.Tasks;
using LCT.Common.Constants;

namespace LCT.PackageIdentifier.UTest
{
    public class MavenParserTests
    {
        [Test]
        public void ParsePackageFile_PackageLockWithDuplicateComponents_ReturnsCountOfDuplicates()
        {
            //Arrange
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string filepath = outFolder + @"\PackageIdentifierUTTestFiles";
            string[] Includes = { "*_Maven.cdx.json" };
            string[] Excludes = { "lol" };

            CommonAppSettings appSettings = new CommonAppSettings()
            {
                PackageFilePath = filepath,
                ProjectType = "MAVEN",
                RemoveDevDependency = true,
                Maven = new Config() { Include = Includes, Exclude = Excludes }
            };

            MavenProcessor MavenProcessor = new MavenProcessor();

            //Act
            Bom bom = MavenProcessor.ParsePackageFile(appSettings);

            //Assert
            Assert.That(bom.Components.Count, Is.EqualTo(2), "Returns the count of components");
            Assert.That(bom.Dependencies.Count, Is.EqualTo(4), "Returns the count of dependencies");

        }

        [Test]
        public async Task IdentificationOfInternalComponents_ReturnsComponentData_Successfully()
        {
            // Arrange
            Component component1 = new Component();
            component1.Name = "junit";
            component1.Group = "";
            component1.Description = string.Empty;
            component1.Version = "1.0.0";
            var components = new List<Component>() { component1 };
            ComponentIdentification component = new() { comparisonBOMData = components };
            string[] reooListArr = { "internalrepo1", "internalrepo2" };
            CommonAppSettings appSettings = new() { InternalRepoList = reooListArr };

            AqlResult aqlResult = new()
            {
                Name = "animations-1.0.0.tgz",
                Path = "@testfolder/-/folder",
                Repo = "internalrepo1"
            };

            List<AqlResult> results = new List<AqlResult>() { aqlResult };
            Mock<IJFrogService> mockJfrogService = new Mock<IJFrogService>();
            Mock<IBomHelper> mockBomHelper = new Mock<IBomHelper>();
            mockBomHelper.Setup(m => m.GetListOfComponentsFromRepo(It.IsAny<string[]>(), It.IsAny<IJFrogService>()))
                .ReturnsAsync(results);
            mockBomHelper.Setup(m => m.GetFullNameOfComponent(It.IsAny<Component>())).Returns("junit");

            // Act
            MavenProcessor mavenProcessor = new MavenProcessor();
            var actual = await mavenProcessor.IdentificationOfInternalComponents(component, appSettings, mockJfrogService.Object, mockBomHelper.Object);

            // Assert
            Assert.That(actual, Is.Not.Null);
        }

        [Test]
        public async Task IdentificationOfInternalComponents_ReturnsComponentData2_Successfully()
        {
            // Arrange
            Component component1 = new Component();
            component1.Name = "junit";
            component1.Group = "";
            component1.Description = string.Empty;
            component1.Version = "1.0.0";
            var components = new List<Component>() { component1 };
            ComponentIdentification component = new() { comparisonBOMData = components };
            string[] reooListArr = { "internalrepo1", "internalrepo2" };
            CommonAppSettings appSettings = new() { InternalRepoList = reooListArr };

            AqlResult aqlResult = new()
            {
                Name = "animations-common_license-1.0.0.tgz",
                Path = "@testfolder/-/folder",
                Repo = "internalrepo1"
            };

            List<AqlResult> results = new List<AqlResult>() { aqlResult };
            Mock<IJFrogService> mockJfrogService = new Mock<IJFrogService>();
            Mock<IBomHelper> mockBomHelper = new Mock<IBomHelper>();
            mockBomHelper.Setup(m => m.GetListOfComponentsFromRepo(It.IsAny<string[]>(), It.IsAny<IJFrogService>()))
                .ReturnsAsync(results);
            mockBomHelper.Setup(m => m.GetFullNameOfComponent(It.IsAny<Component>())).Returns("junit");

            // Act
            MavenProcessor mavenProcessor = new MavenProcessor();
            var actual = await mavenProcessor.IdentificationOfInternalComponents(component, appSettings, mockJfrogService.Object, mockBomHelper.Object);

            // Assert
            Assert.That(actual, Is.Not.Null);
        }

        [Test]
        public async Task IdentificationOfInternalComponents_ReturnsComponentData3_Successfully()
        {
            // Arrange
            Component component1 = new Component
            {
                Name = "junit",
                Group = "junit",
                Description = string.Empty,
                Version = "1.0.0"
            };
            var components = new List<Component>() { component1 };
            ComponentIdentification componentIdentification = new() { comparisonBOMData = components };
            string[] reooListArr = {"internalrepo1", "internalrepo2" };
            CommonAppSettings appSettings = new() { InternalRepoList = reooListArr };

            AqlResult aqlResult = new()
            {
                Name = "animations-common-1.0.0.tgz",
                Path = "@testfolder/-/folder",
                Repo = "internalrepo1"
            };

            List<AqlResult> results = new List<AqlResult>() { aqlResult };
            Mock<IJFrogService> mockJfrogService = new Mock<IJFrogService>();
            Mock<IBomHelper> mockBomHelper = new Mock<IBomHelper>();
            mockBomHelper.Setup(m => m.GetListOfComponentsFromRepo(It.IsAny<string[]>(), It.IsAny<IJFrogService>()))
                .ReturnsAsync(results);
            mockBomHelper.Setup(m => m.GetFullNameOfComponent(It.IsAny<Component>())).Returns("junit/junit");

            // Act
            MavenProcessor mavenProcessor = new MavenProcessor();
            var actual = await mavenProcessor.IdentificationOfInternalComponents(
                componentIdentification, appSettings, mockJfrogService.Object, mockBomHelper.Object);

            // Assert
            Assert.That(actual, Is.Not.Null);
        }

        [Test]
        public async Task GetJfrogRepoDetailsOfAComponent_ReturnsWithData_SuccessFully()
        {
            // Arrange
            Component component1 = new Component
            {
                Name = "junit",
                Group = "junit",
                Description = string.Empty,
                Version = "1.0.0"
            };
            var components = new List<Component>() { component1 };
            string[] reooListArr = { "internalrepo1", "internalrepo2" };
            CommonAppSettings appSettings = new();
            appSettings.Maven = new Config() { JfrogMavenRepoList = reooListArr };
            AqlResult aqlResult = new()
            {
                Name = "animations-common-1.0.0.tgz",
                Path = "@testfolder/-/folder",
                Repo = "internalrepo1"
            };

            List<AqlResult> results = new List<AqlResult>() { aqlResult };

            Mock<IJFrogService> mockJfrogService = new Mock<IJFrogService>();
            Mock<IBomHelper> mockBomHelper = new Mock<IBomHelper>();
            mockBomHelper.Setup(m => m.GetListOfComponentsFromRepo(It.IsAny<string[]>(), It.IsAny<IJFrogService>()))
                .ReturnsAsync(results);
            mockBomHelper.Setup(m => m.GetFullNameOfComponent(It.IsAny<Component>())).Returns("junit/junit");

            // Act
            MavenProcessor mavenProcessor = new MavenProcessor();
            var actual = await mavenProcessor.GetJfrogRepoDetailsOfAComponent(
                components, appSettings, mockJfrogService.Object, mockBomHelper.Object);

            // Assert
            Assert.That(actual, Is.Not.Null);
        }

        [Test]
        public async Task GetJfrogRepoDetailsOfAComponent_ReturnsWithData2_SuccessFully()
        {
            // Arrange
            Component component1 = new Component
            {
                Name = "junit",
                Group = "",
                Description = string.Empty,
                Version = "1.0.0"
            };
            var components = new List<Component>() { component1 };
            string[] reooListArr = { "internalrepo1", "internalrepo2" };
            CommonAppSettings appSettings = new();
            appSettings.Maven = new Config() { JfrogMavenRepoList = reooListArr };
            AqlResult aqlResult = new()
            {
                Name = "animations-common-1.0.0.tgz",
                Path = "@testfolder/-/folder",
                Repo = "internalrepo1"
            };

            List<AqlResult> results = new List<AqlResult>() { aqlResult };

            Mock<IJFrogService> mockJfrogService = new Mock<IJFrogService>();
            Mock<IBomHelper> mockBomHelper = new Mock<IBomHelper>();
            mockBomHelper.Setup(m => m.GetListOfComponentsFromRepo(It.IsAny<string[]>(), It.IsAny<IJFrogService>()))
                .ReturnsAsync(results);
            mockBomHelper.Setup(m => m.GetFullNameOfComponent(It.IsAny<Component>())).Returns("junit");

            // Act
            MavenProcessor mavenProcessor = new MavenProcessor();
            var actual = await mavenProcessor.GetJfrogRepoDetailsOfAComponent(
                components, appSettings, mockJfrogService.Object, mockBomHelper.Object);

            // Assert
            Assert.That(actual, Is.Not.Null);
        }

        [Test]
        public void DevDependencyIdentificationLogic_ReturnsCountOfDevDependentcomponents_SuccessFully()
        {
            //Arrange
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string filepath = outFolder + @"\PackageIdentifierUTTestFiles\MavenDevDependency\WithDev";
            string[] Includes = { "*.cdx.json" };
            string[] Excludes = { "lol" };

            CommonAppSettings appSettings = new CommonAppSettings()
            {
                PackageFilePath = filepath,
                ProjectType = "MAVEN",
                RemoveDevDependency = true,
                Maven = new Config() { Include = Includes, Exclude = Excludes }
            };

            MavenProcessor MavenProcessor = new MavenProcessor();

            //Act
            MavenProcessor.ParsePackageFile(appSettings);

            //Assert
            Assert.That(BomCreator.bomKpiData.DevDependentComponents, Is.EqualTo(6), "Returns the count of components");

        }
        [Test]
        public void DevDependencyIdentificationLogic_ReturnsCountOfComponents_WithoutDevdependency()
        {
            //Arrange
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string filepath = outFolder + @"\PackageIdentifierUTTestFiles\MavenDevDependency\WithOneInputFile";
            string[] Includes = { "*.cdx.json" };
            string[] Excludes = { "lol" };

            CommonAppSettings appSettings = new CommonAppSettings()
            {
                PackageFilePath = filepath,
                ProjectType = "MAVEN",
                RemoveDevDependency = true,
                Maven = new Config() { Include = Includes, Exclude = Excludes }
            };

            MavenProcessor MavenProcessor = new MavenProcessor();

            //Act
            Bom bom = MavenProcessor.ParsePackageFile(appSettings);

            //Assert
            Assert.That(BomCreator.bomKpiData.DevDependentComponents, Is.EqualTo(0), "Returns the count of components");

        }

        [Test]
        public void ParsePackageFile_GivenAInputFilePathAlongWithSBOMTemplate_ReturnTotalComponentsList()
        {
            //Arrange
            int expectednoofcomponents = 1;
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string filepath = outFolder + @"\PackageIdentifierUTTestFiles";
            string[] Includes = { "CycloneDX_Maven.cdx.json", "SBOMTemplate_Maven.cdx.json" };
            string[] Excludes = { "lol" };

            CommonAppSettings appSettings = new CommonAppSettings()
            {
                PackageFilePath = filepath,
                ProjectType = "MAVEN",
                RemoveDevDependency = true,
                Maven = new Config() { Include = Includes, Exclude = Excludes },
                CycloneDxSBomTemplatePath = filepath + "\\SBOMTemplates\\SBOMTemplate_Maven.cdx.json"
            };

            MavenProcessor MavenProcessor = new MavenProcessor();

            //Act
            Bom bom = MavenProcessor.ParsePackageFile(appSettings);

            //Assert
            Assert.That(expectednoofcomponents, Is.EqualTo(bom.Components.Count), "Checks for no of components");

        }

        [Test]
        public void ParsePackageFile_GivenAInputFilePathAlongWithSBOMTemplate_ReturnUpdatedComponents()
        {
            //Arrange
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string filepath = outFolder + @"\PackageIdentifierUTTestFiles";
            string[] Includes = { "CycloneDX_Maven.cdx.json" };
            string[] Excludes = { "lol" };

            CommonAppSettings appSettings = new CommonAppSettings()
            {
                PackageFilePath = filepath,
                ProjectType = "MAVEN",
                RemoveDevDependency = true,
                Maven = new Config() { Include = Includes, Exclude = Excludes },
                CycloneDxSBomTemplatePath = filepath + "\\SBOMTemplates\\SBOMTemplate_Maven.cdx.json"
            };

            MavenProcessor MavenProcessor = new MavenProcessor();

            //Act
            Bom bom = MavenProcessor.ParsePackageFile(appSettings);

            bool isUpdated = bom.Components.Exists(x => x.Properties != null && x.Properties.Exists(x => x.Name == Dataconstant.Cdx_IdentifierType && x.Value == Dataconstant.Discovered));

            //Assert
            Assert.IsTrue(isUpdated, "Checks For Updated Property In List ");

        }

    }
}
