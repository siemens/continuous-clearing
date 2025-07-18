﻿// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models;
using LCT.APICommunications.Model;
using LCT.APICommunications.Model.AQL;
using LCT.Common;
using LCT.Common.Interface;
using LCT.Common.Model;
using LCT.PackageIdentifier.Interface;
using LCT.PackageIdentifier.Model;
using LCT.Services.Interface;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LCT.PackageIdentifier.UTest
{
    [TestFixture]
    public class BomHelperUnitTests
    {
        private readonly Mock<IProcessor> mockIProcessor = new Mock<IProcessor>();
        private BomHelper _bomHelper;

        [SetUp]
        public void Setup()
        {
            _bomHelper = new BomHelper();
        }

        [Test]
        public void GetFullNameOfComponent_WithGroup_ReturnsFullName()
        {
            // Arrange
            var component = new Component
            {
                Group = "com.example",
                Name = "my-component"
            };

            // Act
            var fullName = _bomHelper.GetFullNameOfComponent(component);

            // Assert
            Assert.AreEqual("com.example/my-component", fullName);
        }

        [Test]
        public void GetFullNameOfComponent_WithoutGroup_ReturnsName()
        {
            // Arrange
            var component = new Component
            {
                Name = "my-component"
            };

            // Act
            var fullName = _bomHelper.GetFullNameOfComponent(component);

            // Assert
            Assert.AreEqual("my-component", fullName);
        }

        [Test]
        public async Task GetListOfComponentsFromRepo_WhenRepoListIsNull_ReturnsEmptyList()
        {
            // Arrange
            string[] repoList = null;
            var jFrogServiceMock = new Mock<IJFrogService>();
            var bomHelper = new BomHelper();

            // Act
            var result = await bomHelper.GetListOfComponentsFromRepo(repoList, jFrogServiceMock.Object);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsEmpty(result);
        }

        [Test]
        public async Task GetListOfComponentsFromRepo_WhenRepoListIsEmpty_ReturnsEmptyList()
        {
            // Arrange
            string[] repoList = new string[0];
            var jFrogServiceMock = new Mock<IJFrogService>();
            var bomHelper = new BomHelper();

            // Act
            var result = await bomHelper.GetListOfComponentsFromRepo(repoList, jFrogServiceMock.Object);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsEmpty(result);
        }

        [Test]
        public async Task GetListOfComponentsFromRepo_WhenJFrogServiceReturnsNull_ReturnsEmptyList()
        {
            // Arrange
            string[] repoList = new string[] { "repo1", "repo2" };
            var jFrogServiceMock = new Mock<IJFrogService>();
            jFrogServiceMock.Setup(x => x.GetInternalComponentDataByRepo(It.IsAny<string>())).ReturnsAsync((List<AqlResult>)null);
            var bomHelper = new BomHelper();

            // Act
            var result = await bomHelper.GetListOfComponentsFromRepo(repoList, jFrogServiceMock.Object);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsEmpty(result);
        }

        [Test]
        public async Task GetListOfComponentsFromRepo_WhenJFrogServiceReturnsEmptyList_ReturnsEmptyList()
        {
            // Arrange
            string[] repoList = new string[] { "repo1", "repo2" };
            var jFrogServiceMock = new Mock<IJFrogService>();
            jFrogServiceMock.Setup(x => x.GetInternalComponentDataByRepo(It.IsAny<string>())).ReturnsAsync(new List<AqlResult>());
            var bomHelper = new BomHelper();

            // Act
            var result = await bomHelper.GetListOfComponentsFromRepo(repoList, jFrogServiceMock.Object);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsEmpty(result);
        }

        [Test]
        public async Task GetListOfComponentsFromRepo_WhenJFrogServiceReturnsNonEmptyList_ReturnsCombinedList()
        {
            // Arrange
            string[] repoList = new string[] { "repo1", "repo2" };
            var jFrogServiceMock = new Mock<IJFrogService>();
            var aqlResultList1 = new List<AqlResult>()
            {
                new AqlResult { Name = "Component1", Path="path/value1", Repo="repo1"},
                new AqlResult { Name = "Component2", Path="path/value2", Repo="repo1"}
            };
            var aqlResultList2 = new List<AqlResult>()
            {
                new AqlResult { Name = "Component3", Path="path/value3", Repo="repo2" },
                new AqlResult { Name = "Component4", Path = "path/value4", Repo = "repo2" }
            };
            jFrogServiceMock.Setup(x => x.GetInternalComponentDataByRepo("repo1")).ReturnsAsync(aqlResultList1);
            jFrogServiceMock.Setup(x => x.GetInternalComponentDataByRepo("repo2")).ReturnsAsync(aqlResultList2);
            var bomHelper = new BomHelper();

            // Act
            var result = await bomHelper.GetListOfComponentsFromRepo(repoList, jFrogServiceMock.Object);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(4, result.Count);
            Assert.Contains(aqlResultList1[0], result);
            Assert.Contains(aqlResultList1[1], result);
            Assert.Contains(aqlResultList2[0], result);
            Assert.Contains(aqlResultList2[1], result);
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

            IFolderAction folderAction = new FolderAction();
            IFileOperations fileOperations = new FileOperations();
            CommonAppSettings appSettings = new CommonAppSettings(folderAction, fileOperations)
            {
                ProjectType = "DEBIAN",
                Debian = new Config
                {
                    Artifactory = new Artifactory
                    {
                        RemoteRepos = new string[] { "here" }
                    }
                },
                Jfrog = new Jfrog
                {
                    Token = "testvalue",
                    URL = "https://jfrogapi"
                }
            };
            List<AqlResult> aqlResultList = new()
            {
                new()
                {
                    Path="test/test",
                    Name="Test-1.debian",
                    Repo="remote",
                    MD5="7654345676543",
                    SHA256="65434567",
                    SHA1="765434567654"
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
        public async Task GetRepoDetails_GivenProjectTypeAsNpm_ReturnsListOFComponents()
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

            IFolderAction folderAction = new FolderAction();
            IFileOperations fileOperations = new FileOperations();
            CommonAppSettings appSettings = new CommonAppSettings(folderAction, fileOperations)
            {
                ProjectType = "NPM",
                Npm = new Config
                {
                    Artifactory = new Artifactory
                    {
                        RemoteRepos = new string[] { "here" }
                    }
                },
                Jfrog = new Jfrog
                {
                    Token = "testvalue",
                    URL = "https://jfrogapi"
                }
            };
            AqlProperty npmNameProperty = new AqlProperty
            {
                Key = "npm.name",
                Value = "Test"
            };

            AqlProperty npmVersionProperty = new AqlProperty
            {
                Key = "npm.version",
                Value = "1"
            };
            List<AqlProperty> npmAqlProperties = new List<AqlProperty> { npmNameProperty, npmVersionProperty };
            List<AqlResult> aqlResultList = new()
            {
                new()
                {
                    Path="test/test",
                    Name="Test-1.tgz",
                    Repo="remote",
                    Properties=npmAqlProperties,
                    MD5="7654345676543",
                    SHA256="65434567",
                    SHA1="765434567654"
                }
            };
            mockIProcessor.Setup(x => x.GetJfrogArtifactoryRepoInfo(It.IsAny<CommonAppSettings>(), It.IsAny<ArtifactoryCredentials>(), It.IsAny<Component>(), It.IsAny<string>())).ReturnsAsync(lstComponentForBOM);
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();

            IParser parser = new NpmProcessor(cycloneDXBomParser.Object);
            Mock<IJFrogService> jFrogService = new Mock<IJFrogService>();
            Mock<IBomHelper> bomHelper = new Mock<IBomHelper>();
            bomHelper.Setup(x => x.GetNpmListOfComponentsFromRepo(It.IsAny<string[]>(), It.IsAny<IJFrogService>())).ReturnsAsync(aqlResultList);

            //Act
            var expected = await parser.GetJfrogRepoDetailsOfAComponent(lstComponentForBOM, appSettings, jFrogService.Object, bomHelper.Object);

            //Assert           
            Assert.AreEqual(expected.Count, lstComponentForBOM.Count);
        }
        [TestCase]
        public async Task GetRepoDetails_GivenProjectTypeAsNuget_ReturnsListOFComponents()
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

            IFolderAction folderAction = new FolderAction();
            IFileOperations fileOperations = new FileOperations();
            CommonAppSettings appSettings = new CommonAppSettings(folderAction, fileOperations)
            {
                ProjectType = "Nuget",
                Nuget = new Config
                {
                    Artifactory = new Artifactory
                    {
                        RemoteRepos = new string[] { "here" }
                    }
                },
                Jfrog = new Jfrog
                {
                    Token = "testvalue",
                    URL = "https://jfrogapi"
                }
            };
            List<AqlResult> aqlResultList = new()
            {
                new()
                {
                    Path="test/test",
                    Name="Test.1.nupkg",
                    Repo="remote",
                    MD5="7654345676543",
                    SHA256="65434567",
                    SHA1="765434567654"
                }
            };
            mockIProcessor.Setup(x => x.GetJfrogArtifactoryRepoInfo(It.IsAny<CommonAppSettings>(), It.IsAny<ArtifactoryCredentials>(), It.IsAny<Component>(), It.IsAny<string>())).ReturnsAsync(lstComponentForBOM);
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();
            Mock<IFrameworkPackages> frameworkPackages = new Mock<IFrameworkPackages>();
            Mock<ICompositionBuilder> compositionBuilder = new Mock<ICompositionBuilder>();

            IParser parser = new NugetProcessor(cycloneDXBomParser.Object, frameworkPackages.Object, compositionBuilder.Object);
            //IParser parser = new NugetProcessor(cycloneDXBomParser.Object);
            Mock<IJFrogService> jFrogService = new Mock<IJFrogService>();
            Mock<IBomHelper> bomHelper = new Mock<IBomHelper>();
            bomHelper.Setup(x => x.GetListOfComponentsFromRepo(It.IsAny<string[]>(), It.IsAny<IJFrogService>())).ReturnsAsync(aqlResultList);

            //Act
            var expected = await parser.GetJfrogRepoDetailsOfAComponent(lstComponentForBOM, appSettings, jFrogService.Object, bomHelper.Object);

            //Assert           
            Assert.AreEqual(expected.Count, lstComponentForBOM.Count);
        }
        [TestCase]
        public async Task GetRepoDetails_GivenProjectTypeAsPython_ReturnsListOFComponents()
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

            IFolderAction folderAction = new FolderAction();
            IFileOperations fileOperations = new FileOperations();
            CommonAppSettings appSettings = new CommonAppSettings(folderAction, fileOperations)
            {
                ProjectType = "POETRY",
                Poetry = new Config
                {
                    Artifactory = new Artifactory
                    {
                        RemoteRepos = new string[] { "here" }
                    }
                },
                Jfrog = new Jfrog
                {
                    Token = "testvalue",
                    URL = "https://jfrogapi"
                }
            };
            AqlProperty pypiNameProperty = new AqlProperty
            {
                Key = "pypi.normalized.name",
                Value = "Test"
            };

            AqlProperty pypiVersionProperty = new AqlProperty
            {
                Key = "pypi.version",
                Value = "1"
            };
            List<AqlProperty> propertys = new List<AqlProperty> { pypiNameProperty, pypiVersionProperty };
            List<AqlResult> aqlResultList = new()
            {
                new()
                {
                    Path="test/test",
                    Name="Test-1.whl",
                    Repo="remote",
                    Properties=propertys,
                    MD5="7654345676543",
                    SHA256="65434567",
                    SHA1="765434567654"

                }
            };
            mockIProcessor.Setup(x => x.GetJfrogArtifactoryRepoInfo(It.IsAny<CommonAppSettings>(), It.IsAny<ArtifactoryCredentials>(), It.IsAny<Component>(), It.IsAny<string>())).ReturnsAsync(lstComponentForBOM);
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();

            IParser parser = new PythonProcessor(cycloneDXBomParser.Object);
            Mock<IJFrogService> jFrogService = new Mock<IJFrogService>();
            Mock<IBomHelper> bomHelper = new Mock<IBomHelper>();
            bomHelper.Setup(x => x.GetPypiListOfComponentsFromRepo(It.IsAny<string[]>(), It.IsAny<IJFrogService>())).ReturnsAsync(aqlResultList);

            //Act
            var expected = await parser.GetJfrogRepoDetailsOfAComponent(lstComponentForBOM, appSettings, jFrogService.Object, bomHelper.Object);

            //Assert           
            Assert.AreEqual(expected.Count, lstComponentForBOM.Count);
        }
        [TestCase]
        public async Task GetRepoDetails_GivenProjectTypeAsConan_ReturnsListOFComponents()
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

            IFolderAction folderAction = new FolderAction();
            IFileOperations fileOperations = new FileOperations();
            CommonAppSettings appSettings = new CommonAppSettings(folderAction, fileOperations)
            {
                ProjectType = "Conan",
                Conan = new Config
                {
                    Artifactory = new Artifactory
                    {
                        RemoteRepos = new string[] { "here" }
                    }
                },
                Jfrog = new Jfrog
                {
                    Token = "testvalue",
                    URL = "https://jfrogapi"
                }
            };
            List<AqlResult> aqlResultList = new()
            {
                new()
                {
                    Path="test/test",
                    Name="Test-1",
                    Repo="remote",
                    MD5="7654345676543",
                    SHA256="65434567",
                    SHA1="765434567654"
                }
            };
            mockIProcessor.Setup(x => x.GetJfrogArtifactoryRepoInfo(It.IsAny<CommonAppSettings>(), It.IsAny<ArtifactoryCredentials>(), It.IsAny<Component>(), It.IsAny<string>())).ReturnsAsync(lstComponentForBOM);
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();

            IParser parser = new ConanProcessor(cycloneDXBomParser.Object);
            Mock<IJFrogService> jFrogService = new Mock<IJFrogService>();
            Mock<IBomHelper> bomHelper = new Mock<IBomHelper>();
            bomHelper.Setup(x => x.GetListOfComponentsFromRepo(It.IsAny<string[]>(), It.IsAny<IJFrogService>())).ReturnsAsync(aqlResultList);

            //Act
            var expected = await parser.GetJfrogRepoDetailsOfAComponent(lstComponentForBOM, appSettings, jFrogService.Object, bomHelper.Object);

            //Assert           
            Assert.AreEqual(expected.Count, lstComponentForBOM.Count);
        }
        [TestCase]
        public async Task GetRepoDetails_GivenProjectTypeAsMaven_ReturnsListOFComponents()
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

            IFolderAction folderAction = new FolderAction();
            IFileOperations fileOperations = new FileOperations();
            CommonAppSettings appSettings = new CommonAppSettings(folderAction, fileOperations)
            {
                ProjectType = "MAVEN",
                Maven = new Config
                {
                    Artifactory = new Artifactory
                    {
                        RemoteRepos = new string[] { "here" }
                    }
                },
                Jfrog = new Jfrog
                {
                    Token = "testvalue",
                    URL = "https://jfrogapi"
                }
            };
            List<AqlResult> aqlResultList = new()
            {
                new()
                {
                    Path="test/test",
                    Name="Test-1-sources.jar",
                    Repo="remote",
                    MD5="7654345676543",
                    SHA256="65434567",
                    SHA1="765434567654"
                }
            };
            mockIProcessor.Setup(x => x.GetJfrogArtifactoryRepoInfo(It.IsAny<CommonAppSettings>(), It.IsAny<ArtifactoryCredentials>(), It.IsAny<Component>(), It.IsAny<string>())).ReturnsAsync(lstComponentForBOM);
            Mock<ICycloneDXBomParser> cycloneDXBomParser = new Mock<ICycloneDXBomParser>();

            IParser parser = new MavenProcessor(cycloneDXBomParser.Object);
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


            string hashcode = BomHelper.GetHashCodeUsingNpmView(name, version);
            Assert.That(expectedhashcode, Is.EqualTo(hashcode));
        }
    }
}
