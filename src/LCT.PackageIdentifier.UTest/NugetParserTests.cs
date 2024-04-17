// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.PackageIdentifier.Model;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using LCT.Common;
using LCT.Common.Model;
using LCT.PackageIdentifier;
using LCT.Services.Interface;
using LCT.PackageIdentifier.Interface;
using Moq;
using LCT.APICommunications.Model.AQL;
using CycloneDX.Models;
using System.Threading.Tasks;
using System.Linq;
using LCT.Common.Constants;
using Markdig.Extensions.Yaml;

namespace PackageIdentifier.UTest
{
    [TestFixture]
    public class NugetParserTests
    {
        [TestCase]
        public void ParsePackageConfig_GivenAInputFilePath_ReturnsSuccess()
        {
            //Arrange
            int expectednoofcomponents = 7;
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string packagefilepath = outFolder + @"\PackageIdentifierUTTestFiles\packages.config";

            CommonAppSettings appSettings = new CommonAppSettings()
            {
                PackageFilePath = outFolder + @"\PackageIdentifierUTTestFiles"
            };

            //Act
            List<NugetPackage> listofcomponents = NugetProcessor.ParsePackageConfig(packagefilepath, appSettings);

            //Assert
            Assert.That(expectednoofcomponents, Is.EqualTo(listofcomponents.Count), "Checks for no of components");

        }
        [TestCase]
        public void InputFileIdentifaction_GivenARootPath_ReturnsSuccess()
        {
            //Arrange
            int fileCount = 2;
            string[] Includes = { "p*.config", "p*.lock.json" };
            Config config = new Config()
            {
                Include = Includes,
                Exclude = null

            };
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string folderfilepath = outFolder + @"\PackageIdentifierUTTestFiles";

            //Act
            List<string> allFoundConfigFiles = FolderScanner.FileScanner(folderfilepath, config);


            //Assert
            Assert.That(fileCount, Is.EqualTo(allFoundConfigFiles.Count), "Checks for total inout files found");

        }
        [TestCase]
        public void InputFileIdentifaction_GivenIncludeFileAsNull_ReturnsArgumentNullException()
        {
            //Arrange
            string[] Includes = null;
            Config config = new Config()
            {
                Include = Includes,
                Exclude = null

            };
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string folderfilepath = outFolder + @"\PackageIdentifierUTTestFiles";

            //Act & Assert
            Assert.Throws(typeof(ArgumentNullException), () => FolderScanner.FileScanner(folderfilepath, config));

        }
        [TestCase]
        public void InputFileIdentifaction_GivenInputFileAsNull_ReturnsArgumentNullException()
        {
            //Arrange
            string[] Includes = { "p*.config", "p*.lock.json" };
            Config config = new Config()
            {
                Include = Includes,
                Exclude = null

            };
            string folderfilepath = "";


            //Act & Assert
            Assert.Throws(typeof(ArgumentException), () => FolderScanner.FileScanner(folderfilepath, config));

        }
        [TestCase]
        public void InputFileIdentifaction_GivenInvalidInputFile_ReturnsArgumentNullException()
        {
            //Arrange
            string[] Includes = { "p*.config", "p*.lock.json" };
            Config config = new Config()
            {
                Include = Includes,
                Exclude = null

            };
            string folderfilepath = @"../PackageIdentifierUTTestFiles";

            //Act & Assert
            Assert.Throws(typeof(DirectoryNotFoundException), () => FolderScanner.FileScanner(folderfilepath, config));

        }
        [TestCase]
        public void IsDevDependent_GivenListOfDevComponents_ReturnsSuccess()
        {
            //Arrange
            List<ReferenceDetails> referenceDetails = new List<ReferenceDetails>()
            {
             new ReferenceDetails() { Library = "SCL.Library", Version = "3.1.2", Private = true } };

            //Act
            bool actual = NugetProcessor.IsDevDependent(referenceDetails, "SCL.Library", "3.1.2");

            //Assert
            Assert.That(true, Is.EqualTo(actual), "Component is dev dependent");
        }

        [TestCase]
        public void Parsecsproj_GivenXMLFile_ReturnsSuccess()
        {
            //Arrange
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string csprojfilepath = outFolder + @"\PackageIdentifierUTTestFiles";
            string[] Excludes = null;
            CommonAppSettings CommonAppSettings = new CommonAppSettings()
            {
                PackageFilePath = csprojfilepath,
                Nuget = new Config() { Exclude = Excludes }
            };
            int devDependent = 0;

            //Act
            List<ReferenceDetails> referenceDetails = NugetProcessor.Parsecsproj(CommonAppSettings);
            foreach (var item in referenceDetails)
            {
                if (item.Private)
                {
                    devDependent++;
                }
            }

            //Assert
            Assert.That(1, Is.EqualTo(devDependent), "Checks for total dev dependent components found");
        }

        [TestCase]
        public void RemoveExcludedComponents_ReturnsUpdatedBom()
        {
            //Arrange
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string csprojfilepath = outFolder + @"\PackageIdentifierUTTestFiles";
            string[] Excludes = null;

            Bom bom = new Bom();
            bom.Components = new List<Component>();
            CommonAppSettings CommonAppSettings = new CommonAppSettings()
            {
                PackageFilePath = csprojfilepath,
                Nuget = new Config() { Exclude = Excludes, ExcludedComponents = new List<string>() }
            };

            //Act
            Bom updatedBom = NugetProcessor.RemoveExcludedComponents(CommonAppSettings, bom);

            //Assert
            Assert.AreEqual(0, updatedBom.Components.Count, "Zero component exculded");

        }

        [Test]
        public async Task IdentificationOfInternalComponents_Nuget_ReturnsComponentData_Successfully()
        {
            // Arrange
            Component component1 = new Component();
            component1.Name = "animations";
            component1.Group = "";
            component1.Description = string.Empty;
            component1.Version = "1.0.0";
            var components = new List<Component>() { component1 };
            ComponentIdentification component = new() { comparisonBOMData = components };
            string[] reooListArr = { "internalrepo1", "internalrepo1" };
            CommonAppSettings appSettings = new() { InternalRepoList = reooListArr };

            AqlResult aqlResult = new()
            {
                Name = "animations-1.0.0.nupkg",
                Path = "@testfolder/-/folder",
                Repo = "internalrepo1"
            };

            List<AqlResult> results = new List<AqlResult>() { aqlResult };
            Mock<IJFrogService> mockJfrogService = new Mock<IJFrogService>();
            Mock<IBomHelper> mockBomHelper = new Mock<IBomHelper>();
            mockBomHelper.Setup(m => m.GetListOfComponentsFromRepo(It.IsAny<string[]>(), It.IsAny<IJFrogService>()))
                .ReturnsAsync(results);
            mockBomHelper.Setup(m => m.GetFullNameOfComponent(It.IsAny<Component>())).Returns("animations");
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();

            // Act
            NugetProcessor nugetProcessor = new NugetProcessor(cycloneDXBomParser.Object);
            var actual = await nugetProcessor.IdentificationOfInternalComponents(
                component, appSettings, mockJfrogService.Object, mockBomHelper.Object);

            // Assert
            Assert.That(actual, Is.Not.Null);
        }

        [Test]
        public async Task IdentificationOfInternalComponents_Nuget_ReturnsComponentData2_Successfully()
        {
            // Arrange
            Component component1 = new Component();
            component1.Name = "animations";
            component1.Group = "";
            component1.Description = string.Empty;
            component1.Version = "1.0.0";
            var components = new List<Component>() { component1 };
            ComponentIdentification component = new() { comparisonBOMData = components };
            string[] reooListArr = { "internalrepo1", "internalrepo2" };
            CommonAppSettings appSettings = new() { InternalRepoList = reooListArr };

            AqlResult aqlResult = new()
            {
                Name = "animations-common_license-1.0.0.nupkg",
                Path = "@testfolder/-/folder",
                Repo = "internalrepo1"
            };

            List<AqlResult> results = new List<AqlResult>() { aqlResult };
            Mock<IJFrogService> mockJfrogService = new Mock<IJFrogService>();
            Mock<IBomHelper> mockBomHelper = new Mock<IBomHelper>();
            mockBomHelper.Setup(m => m.GetListOfComponentsFromRepo(It.IsAny<string[]>(), It.IsAny<IJFrogService>()))
                .ReturnsAsync(results);
            mockBomHelper.Setup(m => m.GetFullNameOfComponent(It.IsAny<Component>())).Returns("animations");

            // Act
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();
            NugetProcessor nugetProcessor = new NugetProcessor(cycloneDXBomParser.Object);
            var actual = await nugetProcessor.IdentificationOfInternalComponents(component, appSettings, mockJfrogService.Object, mockBomHelper.Object);

            // Assert
            Assert.That(actual, Is.Not.Null);
        }

        [Test]
        public async Task IdentificationOfInternalComponents_ReturnsComponentData3_Successfully()
        {
            // Arrange
            Component component1 = new Component
            {
                Name = "animations",
                Group = "common",
                Description = string.Empty,
                Version = "1.0.0"
            };
            var components = new List<Component>() { component1 };
            ComponentIdentification componentIdentification = new() { comparisonBOMData = components };
            string[] reooListArr = { "internalrepo1", "internalrepo2" };
            CommonAppSettings appSettings = new() { InternalRepoList = reooListArr };

            AqlResult aqlResult = new()
            {
                Name = "animations-common-1.0.0.nupkg",
                Path = "@testfolder/-/folder",
                Repo = "internalrepo1"

            };

            List<AqlResult> results = new List<AqlResult>() { aqlResult };
            Mock<IJFrogService> mockJfrogService = new Mock<IJFrogService>();
            Mock<IBomHelper> mockBomHelper = new Mock<IBomHelper>();
            mockBomHelper.Setup(m => m.GetListOfComponentsFromRepo(It.IsAny<string[]>(), It.IsAny<IJFrogService>()))
                .ReturnsAsync(results);
            mockBomHelper.Setup(m => m.GetFullNameOfComponent(It.IsAny<Component>())).Returns("animations/common");
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();

            // Act
            NugetProcessor nugetProcessor = new NugetProcessor(cycloneDXBomParser.Object);
            var actual = await nugetProcessor.IdentificationOfInternalComponents(
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
                Name = "animations",
                Group = "common",
                Description = string.Empty,
                Version = "1.0.0"
            };
            var components = new List<Component>() { component1 };
            string[] reooListArr = { "internalrepo1", "internalrepo2" };
            CommonAppSettings appSettings = new();
            appSettings.Nuget = new Config() { JfrogNugetRepoList = reooListArr };
            AqlResult aqlResult = new()
            {
                Name = "animations-common-1.0.0.nupkg",
                Path = "@testfolder/-/folder",
                Repo = "internalrepo1"
            };

            List<AqlResult> results = new List<AqlResult>() { aqlResult };

            Mock<IJFrogService> mockJfrogService = new Mock<IJFrogService>();
            Mock<IBomHelper> mockBomHelper = new Mock<IBomHelper>();
            mockBomHelper.Setup(m => m.GetListOfComponentsFromRepo(It.IsAny<string[]>(), It.IsAny<IJFrogService>()))
                .ReturnsAsync(results);
            mockBomHelper.Setup(m => m.GetFullNameOfComponent(It.IsAny<Component>())).Returns("animations/common");
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();

            // Act
            NugetProcessor nugetProcessor = new NugetProcessor(cycloneDXBomParser.Object);
            var actual = await nugetProcessor.GetJfrogRepoDetailsOfAComponent(
                components, appSettings, mockJfrogService.Object, mockBomHelper.Object);

            // Assert
            Assert.That(actual, Is.Not.Null);
        }

        [Test]
        public async Task GetJfrogRepoDetailsOfAComponent_Nuget_ReturnsWithData2_SuccessFully()
        {
            // Arrange
            Component component1 = new Component
            {
                Name = "animations",
                Group = "",
                Description = string.Empty,
                Version = "1.0.0"
            };
            var components = new List<Component>() { component1 };
            string[] reooListArr = { "internalrepo1", "internalrepo2" };
            CommonAppSettings appSettings = new();
            appSettings.Nuget = new Config() { JfrogNugetRepoList = reooListArr };
            AqlResult aqlResult = new()
            {
                Name = "animations-common-1.0.0.nupkg",
                Path = "@testfolder/-/folder",
                Repo = "internalrepo1"
            };

            List<AqlResult> results = new List<AqlResult>() { aqlResult };

            Mock<IJFrogService> mockJfrogService = new Mock<IJFrogService>();
            Mock<IBomHelper> mockBomHelper = new Mock<IBomHelper>();
            mockBomHelper.Setup(m => m.GetListOfComponentsFromRepo(It.IsAny<string[]>(), It.IsAny<IJFrogService>()))
                .ReturnsAsync(results);
            mockBomHelper.Setup(m => m.GetFullNameOfComponent(It.IsAny<Component>())).Returns("animations");
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();

            // Act
            NugetProcessor nugetProcessor = new NugetProcessor(cycloneDXBomParser.Object);
            var actual = await nugetProcessor.GetJfrogRepoDetailsOfAComponent(
                components, appSettings, mockJfrogService.Object, mockBomHelper.Object);

            // Assert
            Assert.That(actual, Is.Not.Null);
        }

        [Test]
        public async Task GetArtifactoryRepoName_Nuget_ReturnsRepoName_SuccessFully()
        {
            // Arrange
            Component component1 = new()
            {
                Name = "animations-common",
                Group = "",
                Description = string.Empty,
                Version = "1.0.0"
            };
            var components = new List<Component>() { component1 };
            string[] reooListArr = { "internalrepo1", "internalrepo2" };
            CommonAppSettings appSettings = new();
            appSettings.Nuget = new Config() { JfrogNugetRepoList = reooListArr };
            AqlResult aqlResult = new()
            {
                Name = "animations-common-1.0.0.nupkg",
                Path = "@testfolder/-/folder",
                Repo = "internalrepo1"
            };

            List<AqlResult> results = new List<AqlResult>() { aqlResult };

            Mock<IJFrogService> mockJfrogService = new Mock<IJFrogService>();
            Mock<IBomHelper> mockBomHelper = new Mock<IBomHelper>();
            mockBomHelper.Setup(m => m.GetListOfComponentsFromRepo(It.IsAny<string[]>(), It.IsAny<IJFrogService>()))
                .ReturnsAsync(results);
            mockBomHelper.Setup(m => m.GetFullNameOfComponent(It.IsAny<Component>())).Returns("animations");
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();

            // Act
            NugetProcessor nugetProcessor = new NugetProcessor(cycloneDXBomParser.Object);
            var actual = await nugetProcessor.GetJfrogRepoDetailsOfAComponent(
                components, appSettings, mockJfrogService.Object, mockBomHelper.Object);

            var reponameActual = actual.First(x => x.Properties[0].Name == "internal:siemens:clearing:repo-name").Properties[0].Value;

            Assert.That(reponameActual, Is.EqualTo(aqlResult.Repo));
        }
        [Test]
        public async Task GetArtifactoryRepoName_Nuget_ReturnsRepoName_ReturnsFailure()
        {
            // Arrange
            Component component1 = new()
            {
                Name = "animations-common",
                Group = "",
                Description = string.Empty,
                Version = "1.0.0"
            };
            var components = new List<Component>() { component1 };
            string[] reooListArr = { "internalrepo1", "internalrepo2" };
            CommonAppSettings appSettings = new();
            appSettings.Nuget = new Config() { JfrogNugetRepoList = reooListArr };
            AqlResult aqlResult = new()
            {
                Name = "animation-test-1.0.0.nupkg",
                Path = "@testfolder/-/folder",
                Repo = "internalrepo1"
            };

            List<AqlResult> results = new() { aqlResult };

            Mock<IJFrogService> mockJfrogService = new Mock<IJFrogService>();
            Mock<IBomHelper> mockBomHelper = new Mock<IBomHelper>();
            mockBomHelper.Setup(m => m.GetListOfComponentsFromRepo(It.IsAny<string[]>(), It.IsAny<IJFrogService>()))
                .ReturnsAsync(results);
            mockBomHelper.Setup(m => m.GetFullNameOfComponent(It.IsAny<Component>())).Returns("animations");
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();

            // Act
            NugetProcessor nugetProcessor = new NugetProcessor(cycloneDXBomParser.Object);
            var actual = await nugetProcessor.GetJfrogRepoDetailsOfAComponent(
                components, appSettings, mockJfrogService.Object, mockBomHelper.Object);

            var reponameActual = actual.First(x => x.Properties[0].Name == "internal:siemens:clearing:repo-name").Properties[0].Value;

            Assert.That("Not Found in JFrogRepo", Is.EqualTo(reponameActual));
        }
        [Test]
        public async Task GetArtifactoryRepoName_Nuget_ReturnsRepoName_ReturnsSuccess()
        {
            // Arrange
            Component component1 = new()
            {
                Name = "animations-common",
                Group = "",
                Description = string.Empty,
                Version = "1.0.0"
            };
            var components = new List<Component>() { component1 };
            string[] reooListArr = { "internalrepo1", "internalrepo2" };
            CommonAppSettings appSettings = new();
            appSettings.Nuget = new Config() { JfrogNugetRepoList = reooListArr };
            AqlResult aqlResult = new()
            {
                Name = "animations-common.1.0.0.nupkg",
                Path = "@testfolder/-/folder",
                Repo = "internalrepo1"
            };

            List<AqlResult> results = new() { aqlResult };

            Mock<IJFrogService> mockJfrogService = new Mock<IJFrogService>();
            Mock<IBomHelper> mockBomHelper = new Mock<IBomHelper>();
            mockBomHelper.Setup(m => m.GetListOfComponentsFromRepo(It.IsAny<string[]>(), It.IsAny<IJFrogService>()))
                .ReturnsAsync(results);
            mockBomHelper.Setup(m => m.GetFullNameOfComponent(It.IsAny<Component>())).Returns("animations");
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();

            // Act
            NugetProcessor nugetProcessor = new NugetProcessor(cycloneDXBomParser.Object);
            var actual = await nugetProcessor.GetJfrogRepoDetailsOfAComponent(
                components, appSettings, mockJfrogService.Object, mockBomHelper.Object);

            var reponameActual = actual.First(x => x.Properties[0].Name == "internal:siemens:clearing:repo-name").Properties[0].Value;


            Assert.That("internalrepo1", Is.EqualTo(reponameActual));
        }
        [Test]
        public async Task GetArtifactoryRepoName_Nuget_ReturnsNotFound_ReturnsFailure()
        {
            // Arrange
            Component component1 = new()
            {
                Name = "animations-common",
                Group = "",
                Description = string.Empty,
                Version = "1.0.0"
            };
            var components = new List<Component>() { component1 };
            string[] reooListArr = { "internalrepo1", "internalrepo2" };
            CommonAppSettings appSettings = new();
            appSettings.Nuget = new Config() { JfrogNugetRepoList = reooListArr };
            AqlResult aqlResult = new()
            {
                Name = "animation-common.1.0.0.nupkg",
                Path = "@testfolder/-/folder",
                Repo = "internalrepo1"
            };

            List<AqlResult> results = new() { aqlResult };

            Mock<IJFrogService> mockJfrogService = new Mock<IJFrogService>();
            Mock<IBomHelper> mockBomHelper = new Mock<IBomHelper>();
            mockBomHelper.Setup(m => m.GetListOfComponentsFromRepo(It.IsAny<string[]>(), It.IsAny<IJFrogService>()))
                .ReturnsAsync(results);
            mockBomHelper.Setup(m => m.GetFullNameOfComponent(It.IsAny<Component>())).Returns("animations");
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();

            // Act
            NugetProcessor nugetProcessor = new NugetProcessor(cycloneDXBomParser.Object);
            var actual = await nugetProcessor.GetJfrogRepoDetailsOfAComponent(
                components, appSettings, mockJfrogService.Object, mockBomHelper.Object);

            var reponameActual = actual.First(x => x.Properties[0].Name == "internal:siemens:clearing:repo-name").Properties[0].Value;

            Assert.That("Not Found in JFrogRepo", Is.EqualTo(reponameActual));
        }

        [TestCase]
        public void ParseProjectAssetFile_GivenAInputFilePath_ReturnsSuccess()
        {
            //Arrange
            int expectednoofcomponents = 2;
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string packagefilepath = outFolder + @"\PackageIdentifierUTTestFiles";

            string[] Includes = { "project.assets.json" };
            Config config = new Config()
            {
                Include = Includes
            };

            CommonAppSettings appSettings = new CommonAppSettings()
            {
                PackageFilePath = packagefilepath,
                Nuget = config
            };
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();

            //Act
            Bom listofcomponents = new NugetProcessor(cycloneDXBomParser.Object).ParsePackageFile(appSettings);

            //Assert
            Assert.That(expectednoofcomponents, Is.EqualTo(listofcomponents.Components.Count), "Checks for no of components");

        }

        [TestCase]
        public void ParseProjectAssetFile_GivenAInputFilePath_ReturnDevDependentComp()
        {
            //Arrange
            string IsDev = "true";
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string packagefilepath = outFolder + @"\PackageIdentifierUTTestFiles";

            string[] Includes = { "project.assets.json" };
            Config config = new Config()
            {
                Include = Includes
            };

            CommonAppSettings appSettings = new CommonAppSettings()
            {
                PackageFilePath = packagefilepath,
                Nuget = config
            };
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();


            //Act
            Bom listofcomponents = new NugetProcessor(cycloneDXBomParser.Object).ParsePackageFile(appSettings);
            var IsDevDependency = 
                listofcomponents.Components.Find(a => a.Name == "SonarAnalyzer.CSharp")
                .Properties[0].Value;

            //Assert
            Assert.That(IsDev, Is.EqualTo(IsDevDependency), "Checks if Dev Dependency Component or not");

        }
    }
}
