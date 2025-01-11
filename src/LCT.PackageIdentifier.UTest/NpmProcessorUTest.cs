// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models;
using LCT.APICommunications.Model.AQL;
using LCT.Common;
using LCT.Common.Constants;
using LCT.Common.Interface;
using LCT.Common.Model;
using LCT.PackageIdentifier.Interface;
using LCT.PackageIdentifier.Model;
using LCT.Services.Interface;
using Moq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LCT.PackageIdentifier.UTest
{
    [TestFixture]
    internal class NpmProcessorUTest
    {
        [Test]
        public void GetJfrogArtifactoryRepoDetials_RepoPathFound_ReturnsAqlResultWithRepoPath()
        {
            // Arrange
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();
            AqlProperty npmNameProperty = new AqlProperty
            {
                Key = "npm.name",
                Value = "component"
            };

            AqlProperty npmVersionProperty = new AqlProperty
            {
                Key = "npm.version",
                Value = "1.0.0"
            };
            AqlProperty npmNamePropert = new AqlProperty
            {
                Key = "npm.name",
                Value = "component"
            };

            AqlProperty npmVersionPropert = new AqlProperty
            {
                Key = "npm.version",
                Value = "2.0.0"
            };
            List<AqlProperty> propertys = new List<AqlProperty> { npmNameProperty, npmVersionProperty };
            List<AqlProperty> property = new List<AqlProperty> { npmNamePropert, npmVersionPropert };
            var aqlResultList = new List<AqlResult>
            {
                new AqlResult { Name = "component-1.0.0.tgz", Repo = "repo1", Path="path/to",Properties=propertys },
                new AqlResult { Name = "component-2.0.0.tgz", Repo = "repo2", Path="path/to",Properties = property }
            };
            var bomHelperMock = new Mock<IBomHelper>();
            var component = new Component { Name = "component", Version = "1.0.0" };
            bomHelperMock.Setup(b => b.GetFullNameOfComponent(component)).Returns("component");
            var expectedRepoPath = "repo1/path/to/component-1.0.0.tgz";

            var npmProcessor = new NpmProcessor(cycloneDXBomParser.Object);

            // Act
            var result = npmProcessor.GetJfrogArtifactoryRepoDetials(aqlResultList, component, bomHelperMock.Object, out string repoPath);

            // Assert
            Assert.NotNull(result);
            Assert.AreEqual("repo1", result.Repo);
            Assert.AreEqual(expectedRepoPath, repoPath);
        }

        [Test]
        public void GetJfrogArtifactoryRepoDetials_RepoPathNotFound_ReturnsAqlResultWithNotFoundRepo()
        {
            // Arrange
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();
            AqlProperty npmNameProperty = new AqlProperty
            {
                Key = "npm.name",
                Value = "component"
            };

            AqlProperty npmVersionProperty = new AqlProperty
            {
                Key = "npm.version",
                Value = "1.0.0"
            };
            AqlProperty npmNamePropert = new AqlProperty
            {
                Key = "npm.name",
                Value = "component"
            };

            AqlProperty npmVersionPropert = new AqlProperty
            {
                Key = "npm.version",
                Value = "2.0.0"
            };
            List<AqlProperty> propertys = new List<AqlProperty> { npmNameProperty, npmVersionProperty };
            List<AqlProperty> property = new List<AqlProperty> { npmNamePropert, npmVersionPropert };
            var aqlResultList = new List<AqlResult>
            {
                new AqlResult { Name = "component-1.0.0.tgz", Repo = "repo1",Properties=propertys },
                new AqlResult { Name = "component-2.0.0.tgz", Repo = "repo2",Properties=property }
            };
            var component = new Component { Name = "component", Version = "3.0.0" };
            var bomHelperMock = new Mock<IBomHelper>();
            bomHelperMock.Setup(b => b.GetFullNameOfComponent(component)).Returns("component");

            var npmProcessor = new NpmProcessor(cycloneDXBomParser.Object);

            // Act
            var result = npmProcessor.GetJfrogArtifactoryRepoDetials(aqlResultList, component, bomHelperMock.Object, out string repoPath);

            // Assert
            Assert.NotNull(result);
            Assert.AreEqual("Not Found in JFrogRepo", result.Repo);
            Assert.AreEqual(Dataconstant.JfrogRepoPathNotFound, repoPath);
        }

        [Test]
        public async Task IdentificationOfInternalComponents_ReturnsComponentData_Successfully()
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
            IFolderAction folderAction = new FolderAction();
            IFileOperations fileOperations = new FileOperations();
            CommonAppSettings appSettings = new CommonAppSettings(folderAction, fileOperations)
            {
                SW360 = new SW360(),
                Npm = new Config
                {
                    Artifactory = new Artifactory
                    {
                        InternalRepos = reooListArr
                    }
                }
            };
            AqlProperty npmNameProperty = new AqlProperty
            {
                Key = "npm.name",
                Value = "animations"
            };

            AqlProperty npmVersionProperty = new AqlProperty
            {
                Key = "npm.version",
                Value = "1.0.0"
            };
            List<AqlProperty> propertys = new List<AqlProperty> { npmNameProperty, npmVersionProperty };
            AqlResult aqlResult = new()
            {
                Name = "animations-1.0.0.tgz",
                Path = "@testfolder/-/folder",
                Repo = "internalrepo1",
                Properties = propertys
            };

            List<AqlResult> results = new List<AqlResult>() { aqlResult };
            Mock<IJFrogService> mockJfrogService = new Mock<IJFrogService>();
            Mock<IBomHelper> mockBomHelper = new Mock<IBomHelper>();
            mockBomHelper.Setup(m => m.GetNpmListOfComponentsFromRepo(It.IsAny<string[]>(), It.IsAny<IJFrogService>()))
                .ReturnsAsync(results);
            mockBomHelper.Setup(m => m.GetFullNameOfComponent(It.IsAny<Component>())).Returns("animations");
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();

            // Act
            NpmProcessor npmProcessor = new NpmProcessor(cycloneDXBomParser.Object);
            var actual = await npmProcessor.IdentificationOfInternalComponents(component, appSettings, mockJfrogService.Object, mockBomHelper.Object);

            // Assert
            Assert.That(actual, Is.Not.Null);
        }

        [Test]
        public async Task IdentificationOfInternalComponents_ReturnsComponentData2_Successfully()
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
            IFolderAction folderAction = new FolderAction();
            IFileOperations fileOperations = new FileOperations();
            CommonAppSettings appSettings = new CommonAppSettings(folderAction, fileOperations)
            {
                SW360 = new SW360(),
                Npm = new Config
                {
                    Artifactory = new Artifactory
                    {
                        InternalRepos = reooListArr
                    }
                }
            };
            AqlProperty npmNameProperty = new AqlProperty
            {
                Key = "npm.name",
                Value = "animations"
            };

            AqlProperty npmVersionProperty = new AqlProperty
            {
                Key = "npm.version",
                Value = "1.0.0"
            };
            List<AqlProperty> propertys = new List<AqlProperty> { npmNameProperty, npmVersionProperty };
            AqlResult aqlResult = new()
            {
                Name = "animations-common_license-1.0.0.tgz",
                Path = "@testfolder/-/folder",
                Repo = "internalrepo1",
                Properties = propertys
            };

            List<AqlResult> results = new List<AqlResult>() { aqlResult };
            Mock<IJFrogService> mockJfrogService = new Mock<IJFrogService>();
            Mock<IBomHelper> mockBomHelper = new Mock<IBomHelper>();
            mockBomHelper.Setup(m => m.GetNpmListOfComponentsFromRepo(It.IsAny<string[]>(), It.IsAny<IJFrogService>()))
                .ReturnsAsync(results);
            mockBomHelper.Setup(m => m.GetFullNameOfComponent(It.IsAny<Component>())).Returns("animations");
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();

            // Act
            NpmProcessor npmProcessor = new NpmProcessor(cycloneDXBomParser.Object);
            var actual = await npmProcessor.IdentificationOfInternalComponents(component, appSettings, mockJfrogService.Object, mockBomHelper.Object);

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
            string[] reooListArr = { "internalrepo1", "internalrepo1" };
            IFolderAction folderAction = new FolderAction();
            IFileOperations fileOperations = new FileOperations();
            CommonAppSettings appSettings = new CommonAppSettings(folderAction, fileOperations)
            {
                SW360 = new SW360(),
                Npm = new Config
                {
                    Artifactory = new Artifactory
                    {
                        InternalRepos = reooListArr
                    }
                }
            };
            AqlProperty npmNameProperty = new AqlProperty
            {
                Key = "npm.name",
                Value = "animations"
            };

            AqlProperty npmVersionProperty = new AqlProperty
            {
                Key = "npm.version",
                Value = "1.0.0"
            };
            List<AqlProperty> propertys = new List<AqlProperty> { npmNameProperty, npmVersionProperty };
            AqlResult aqlResult = new()
            {
                Name = "animations-common-1.0.0.tgz",
                Path = "@testfolder/-/folder",
                Repo = "internalrepo1",
                Properties = propertys
            };

            List<AqlResult> results = new List<AqlResult>() { aqlResult };
            Mock<IJFrogService> mockJfrogService = new Mock<IJFrogService>();
            Mock<IBomHelper> mockBomHelper = new Mock<IBomHelper>();
            mockBomHelper.Setup(m => m.GetNpmListOfComponentsFromRepo(It.IsAny<string[]>(), It.IsAny<IJFrogService>()))
                .ReturnsAsync(results);
            mockBomHelper.Setup(m => m.GetFullNameOfComponent(It.IsAny<Component>())).Returns("animations/common");
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();

            // Act
            NpmProcessor npmProcessor = new NpmProcessor(cycloneDXBomParser.Object);
            var actual = await npmProcessor.IdentificationOfInternalComponents(
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
            string[] reooListArr = { "internalrepo1", "internalrepo1" };
            IFolderAction folderAction = new FolderAction();
            IFileOperations fileOperations = new FileOperations();
            CommonAppSettings appSettings = new CommonAppSettings(folderAction, fileOperations)
            {
                SW360 = new SW360(),
                Npm = new Config
                {
                    Artifactory = new Artifactory
                    {
                        RemoteRepos = reooListArr
                    }
                }
            };
            AqlProperty npmNameProperty = new AqlProperty
            {
                Key = "npm.name",
                Value = "animations"
            };

            AqlProperty npmVersionProperty = new AqlProperty
            {
                Key = "npm.version",
                Value = "1.0.0"
            };
            List<AqlProperty> propertys = new List<AqlProperty> { npmNameProperty, npmVersionProperty };
            AqlResult aqlResult = new()
            {
                Name = "animations-common-1.0.0.tgz",
                Path = "@testfolder/-/folder",
                Repo = "internalrepo1",
                Properties = propertys
            };

            List<AqlResult> results = new List<AqlResult>() { aqlResult };

            Mock<IJFrogService> mockJfrogService = new Mock<IJFrogService>();
            Mock<IBomHelper> mockBomHelper = new Mock<IBomHelper>();
            mockBomHelper.Setup(m => m.GetNpmListOfComponentsFromRepo(It.IsAny<string[]>(), It.IsAny<IJFrogService>()))
                .ReturnsAsync(results);
            mockBomHelper.Setup(m => m.GetFullNameOfComponent(It.IsAny<Component>())).Returns("animations/common");
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();

            // Act
            NpmProcessor npmProcessor = new NpmProcessor(cycloneDXBomParser.Object);
            var actual = await npmProcessor.GetJfrogRepoDetailsOfAComponent(
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
                Name = "animations",
                Group = "",
                Description = string.Empty,
                Version = "1.0.0"
            };
            var components = new List<Component>() { component1 };
            string[] reooListArr = { "internalrepo1", "internalrepo2" };
            IFolderAction folderAction = new FolderAction();
            IFileOperations fileOperations = new FileOperations();
            CommonAppSettings appSettings = new CommonAppSettings(folderAction, fileOperations)
            {
                SW360 = new SW360(),
                Npm = new Config
                {
                    Artifactory = new Artifactory
                    {
                        RemoteRepos = reooListArr
                    }
                }
            };
            AqlProperty npmNameProperty = new AqlProperty
            {
                Key = "npm.name",
                Value = "animations"
            };

            AqlProperty npmVersionProperty = new AqlProperty
            {
                Key = "npm.version",
                Value = "1.0.0"
            };
            List<AqlProperty> propertys = new List<AqlProperty> { npmNameProperty, npmVersionProperty };
            AqlResult aqlResult = new()
            {
                Name = "animations-common-1.0.0.tgz",
                Path = "@testfolder/-/folder",
                Repo = "internalrepo1",
                Properties = propertys
            };

            List<AqlResult> results = new List<AqlResult>() { aqlResult };

            Mock<IJFrogService> mockJfrogService = new Mock<IJFrogService>();
            Mock<IBomHelper> mockBomHelper = new Mock<IBomHelper>();
            mockBomHelper.Setup(m => m.GetNpmListOfComponentsFromRepo(It.IsAny<string[]>(), It.IsAny<IJFrogService>()))
                .ReturnsAsync(results);
            mockBomHelper.Setup(m => m.GetFullNameOfComponent(It.IsAny<Component>())).Returns("animations");
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();

            // Act
            NpmProcessor npmProcessor = new NpmProcessor(cycloneDXBomParser.Object);
            var actual = await npmProcessor.GetJfrogRepoDetailsOfAComponent(
                components, appSettings, mockJfrogService.Object, mockBomHelper.Object);

            // Assert
            Assert.That(actual, Is.Not.Null);
        }

        [Test]
        public void GetdependencyDetailsOfAComponent_ReturnsListOfDependency_SuccessFully()
        {
            // Arrange
            Component component = new Component
            {
                Name = "animations",
                Group = "",
                Description = string.Empty,
                Version = "1.0.0",
                Purl = "pkg:npm/animations@1.0.0",
                Author = "{ testcomponent:1.2.3 , subdepenendency:3.4.1 }"

            };
            List<Component> componentsForBOM = new List<Component>();
            componentsForBOM.Add(component);
            List<Dependency> dependencyList = new();
            List<Dependency> expectedDependencyList = new()
            {
                new()
                {
                      Ref="pkg:npm/animations@1.0.0",
                      Dependencies=new()
                      {
                         new()
                            {
                            Ref="pkg:npm/testcomponent@1.2.3"

                            },
                         new()
                            {
                             Ref="pkg:npm/subdepenendency@3.4.1"
                             }
                       }
                 }
            };

            //Act

            NpmProcessor.GetdependencyDetails(componentsForBOM, dependencyList);

            //Assert
            Assert.That(expectedDependencyList.Count, Is.EqualTo(dependencyList.Count));


        }


        [Test]
        public void GetIsDirect_ShouldReturnTrue_WhenDirectDependencyExists()
        {
            // Arrange
            var directDependencies = new List<JToken> { "directDependency1", "directDependency2" };
            var prop = new JProperty("node_modules/directDependency1");

            // Act
            var result = NpmProcessor.GetIsDirect(directDependencies, prop);

            // Assert
            Assert.AreEqual("true", result);
        }

        [Test]
        public void GetIsDirect_ShouldReturnFalse_WhenDirectDependencyDoesNotExist()
        {
            // Arrange
            var directDependencies = new List<JToken> { "directDependency1", "directDependency2" };
            var prop = new JProperty("node_modules/indirectDependency");

            // Act
            var result = NpmProcessor.GetIsDirect(directDependencies, prop);

            // Assert
            Assert.AreEqual("false", result);
        }

        [Test]
        public void GetIsDirect_ShouldReturnFalse_WhenDirectDependenciesIsEmpty()
        {
            // Arrange
            var directDependencies = new List<JToken>();
            var prop = new JProperty("node_modules/directDependency");

            // Act
            var result = NpmProcessor.GetIsDirect(directDependencies, prop);

            // Assert
            Assert.AreEqual("false", result);
        }
    }
}
