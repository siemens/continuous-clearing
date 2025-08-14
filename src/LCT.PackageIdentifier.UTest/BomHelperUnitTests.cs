// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models;
using LCT.APICommunications.Model;
using LCT.APICommunications.Model.AQL;
using LCT.Common;
using LCT.Common.Constants;
using LCT.Common.Interface;
using LCT.Common.Model;
using LCT.PackageIdentifier.Interface;
using LCT.PackageIdentifier.Model;
using LCT.Services.Interface;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
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

            CommonAppSettings appSettings = new CommonAppSettings()
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
            Mock<ISpdxBomParser> spdxBomParser = new Mock<ISpdxBomParser>();

            IParser parser = new DebianProcessor(cycloneDXBomParser.Object, spdxBomParser.Object);
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

            CommonAppSettings appSettings = new CommonAppSettings()
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
            Mock<ISpdxBomParser> spdxBomParser = new Mock<ISpdxBomParser>();
            IParser parser = new NpmProcessor(cycloneDXBomParser.Object, spdxBomParser.Object);
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

            CommonAppSettings appSettings = new CommonAppSettings()
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
            Mock<ISpdxBomParser> spdxBomParser = new Mock<ISpdxBomParser>();
            IParser parser = new NugetProcessor(cycloneDXBomParser.Object, frameworkPackages.Object, compositionBuilder.Object, spdxBomParser.Object);
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

            CommonAppSettings appSettings = new CommonAppSettings()
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
            Mock<ISpdxBomParser> spdxBomParser = new Mock<ISpdxBomParser>();
            IParser parser = new PythonProcessor(cycloneDXBomParser.Object, spdxBomParser.Object);
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

            CommonAppSettings appSettings = new CommonAppSettings()
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
            Mock<ISpdxBomParser> spdxBomParser = new Mock<ISpdxBomParser>();
            IParser parser = new ConanProcessor(cycloneDXBomParser.Object, spdxBomParser.Object);
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


            CommonAppSettings appSettings = new CommonAppSettings()
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
            Mock<ISpdxBomParser> spdxBomParser = new Mock<ISpdxBomParser>();
            IParser parser = new MavenProcessor(cycloneDXBomParser.Object, spdxBomParser.Object);
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
        public void TestGetHashCodeUsingNpmView_InputNameAndVersion_ReturnsHashCode()
        {
            string expectedhashcode = "5f845b1a58ffb6f3ea6103edf0756ac65320b725";
            string name = "@angular/animations";
            string version = "12.0.0";


            string hashcode = BomHelper.GetHashCodeUsingNpmView(name, version);
            Assert.That(expectedhashcode, Is.EqualTo(hashcode));
        }
        [Test]
        public void GetProjectSummaryLink_ReturnsCorrectUrl()
        {
            // Arrange
            string projectId = "test-project-123";
            string sw360Url = "https://sw360.example.com";
            var bomHelper = new BomHelper();

            // Act
            string result = bomHelper.GetProjectSummaryLink(projectId, sw360Url);

            // Assert
            Assert.That(result, Does.Contain(sw360Url));
            Assert.That(result, Does.Contain(projectId));
        }

        [Test]
        public void ParseBomFile_WithSpdxFile_CallsSpdxParser()
        {
            // Arrange
            string tempFile = Path.GetTempFileName();
            string spdxFile = Path.ChangeExtension(tempFile, FileConstant.SPDXFileExtension);
            System.IO.File.WriteAllText(spdxFile, "test content");

            var mockSpdxParser = new Mock<ISpdxBomParser>();
            var mockCycloneDxParser = new Mock<ICycloneDXBomParser>();
            var appSettings = new CommonAppSettings { ProjectType = "NPM" };
            var expectedBom = new Bom
            {
                Components = new List<Component>(),
                Dependencies = new List<Dependency>()
            };

            mockSpdxParser.Setup(x => x.ParseSPDXBom(spdxFile)).Returns(expectedBom);

            try
            {
                // Act
                var listUnsupportedComponents = new Bom()
                {
                    Components = new List<Component>(),
                    Dependencies = new List<Dependency>()
                };
                var result = BomHelper.ParseBomFile(spdxFile, mockSpdxParser.Object, mockCycloneDxParser.Object, appSettings, ref listUnsupportedComponents);

                // Assert
                Assert.AreEqual(expectedBom, result);
                mockSpdxParser.Verify(x => x.ParseSPDXBom(spdxFile), Times.Once);
                mockCycloneDxParser.Verify(x => x.ParseCycloneDXBom(It.IsAny<string>()), Times.Never);
            }
            finally
            {
                // Cleanup
                if (System.IO.File.Exists(spdxFile))
                    System.IO.File.Delete(spdxFile);
                if (System.IO.File.Exists(tempFile))
                    System.IO.File.Delete(tempFile);
            }
        }

        [Test]
        public void ParseBomFile_WithCycloneDxFile_CallsCycloneDxParser()
        {
            // Arrange
            string tempFile = Path.GetTempFileName();
            string cyclonDxFile = Path.ChangeExtension(tempFile, ".json");
            System.IO.File.WriteAllText(cyclonDxFile, "test content");

            var mockSpdxParser = new Mock<ISpdxBomParser>();
            var mockCycloneDxParser = new Mock<ICycloneDXBomParser>();
            var appSettings = new CommonAppSettings { ProjectType = "NPM" };
            var expectedBom = new Bom
            {
                Components = new List<Component>(),
                Dependencies = new List<Dependency>()
            };

            mockCycloneDxParser.Setup(x => x.ParseCycloneDXBom(cyclonDxFile)).Returns(expectedBom);

            try
            {
                // Act
                var listUnsupportedComponents = new Bom()
                {
                    Components = new List<Component>(),
                    Dependencies = new List<Dependency>()
                };
                var result = BomHelper.ParseBomFile(cyclonDxFile, mockSpdxParser.Object, mockCycloneDxParser.Object, appSettings, ref listUnsupportedComponents);

                // Assert
                Assert.AreEqual(expectedBom, result);
                mockCycloneDxParser.Verify(x => x.ParseCycloneDXBom(cyclonDxFile), Times.Once);
                mockSpdxParser.Verify(x => x.ParseSPDXBom(It.IsAny<string>()), Times.Never);
            }
            finally
            {
                // Cleanup
                if (System.IO.File.Exists(cyclonDxFile))
                    System.IO.File.Delete(cyclonDxFile);
                if (System.IO.File.Exists(tempFile))
                    System.IO.File.Delete(tempFile);
            }
        }


        [Test]
        public void ParseBomFile_WithEmptyDirectory_ReturnsEmptyBom()
        {
            // Arrange
            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            System.IO.Directory.CreateDirectory(tempDir);

            var mockSpdxParser = new Mock<ISpdxBomParser>();
            var mockCycloneDxParser = new Mock<ICycloneDXBomParser>();
            var appSettings = new CommonAppSettings { ProjectType = "NPM" };

            // Setup mock to return empty BOM with initialized collections
            var emptyBom = new Bom
            {
                Components = new List<Component>(),
                Dependencies = new List<Dependency>()
            };
            mockCycloneDxParser.Setup(x => x.ParseCycloneDXBom(It.IsAny<string>())).Returns(emptyBom);

            try
            {
                // Act
                var listUnsupportedComponents = new Bom()
                {
                    Components = new List<Component>(),
                    Dependencies = new List<Dependency>()
                };
                var result = BomHelper.ParseBomFile(tempDir, mockSpdxParser.Object, mockCycloneDxParser.Object, appSettings, ref listUnsupportedComponents);

                // Assert
                Assert.IsNotNull(result);
                Assert.IsInstanceOf<Bom>(result);
            }
            finally
            {
                // Cleanup
                if (System.IO.Directory.Exists(tempDir))
                    System.IO.Directory.Delete(tempDir, true);
            }
        }

        [Test]
        public void ParseBomFile_WithDirectoryContainingInvalidSpdxFiles_ReturnsEmptyBom()
        {
            // Arrange
            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            System.IO.Directory.CreateDirectory(tempDir);


            string spdxFile = Path.Combine(tempDir, "invalid.spdx.sbom.json");
            System.IO.File.WriteAllText(spdxFile, "invalid content");

            var mockSpdxParser = new Mock<ISpdxBomParser>();
            var mockCycloneDxParser = new Mock<ICycloneDXBomParser>();
            var appSettings = new CommonAppSettings { ProjectType = "NPM" };

            // Setup to return empty BOM when parsing fails (as the real parser does)
            var emptyBom = new Bom { Components = new List<Component>(), Dependencies = new List<Dependency>() };
            mockSpdxParser.Setup(x => x.ParseSPDXBom(spdxFile)).Returns(emptyBom);

            try
            {
                // Act - test with the actual SPDX file, not the directory
                var listUnsupportedComponents = new Bom()
                {
                    Components = new List<Component>(),
                    Dependencies = new List<Dependency>()
                };
                var result = BomHelper.ParseBomFile(spdxFile, mockSpdxParser.Object, mockCycloneDxParser.Object, appSettings, ref listUnsupportedComponents);

                // Assert
                Assert.IsNotNull(result);
                Assert.IsInstanceOf<Bom>(result);
                Assert.IsNotNull(result.Components);
                Assert.IsNotNull(result.Dependencies);
            }
            finally
            {
                // Cleanup
                if (System.IO.Directory.Exists(tempDir))
                    System.IO.Directory.Delete(tempDir, true);
            }
        }

        [Test]
        public void NamingConventionOfSPDXFile_WithMissingFiles_ContinuesWithoutException()
        {
            // Arrange
            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            System.IO.Directory.CreateDirectory(tempDir);

            string spdxFile = Path.Combine(tempDir, "test.spdx");
            System.IO.File.WriteAllText(spdxFile, "test content");

            var appSettings = new CommonAppSettings()
            {
                Directory = new LCT.Common.Directory()
                {
                    InputFolder = tempDir
                }
            };

            try
            {
                // Act & Assert - should not throw exception
                Assert.DoesNotThrow(() => BomHelper.NamingConventionOfSPDXFile(spdxFile, appSettings));
            }
            finally
            {
                // Cleanup
                if (System.IO.Directory.Exists(tempDir))
                    System.IO.Directory.Delete(tempDir, true);
            }
        }

        [Test]
        public void NamingConventionOfSPDXFile_WithExistingFiles_ValidatesFiles()
        {
            // Arrange
            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            System.IO.Directory.CreateDirectory(tempDir);

            string spdxFile = Path.Combine(tempDir, "test.spdx");
            string pemFile = Path.Combine(tempDir, "test.spdx.pem");
            string sigFile = Path.Combine(tempDir, "test.spdx.sig");

            System.IO.File.WriteAllText(spdxFile, "test content");
            System.IO.File.WriteAllText(pemFile, "test pem content");
            System.IO.File.WriteAllText(sigFile, "test sig content");

            var appSettings = new CommonAppSettings()
            {
                Directory = new LCT.Common.Directory()
                {
                    InputFolder = tempDir
                }
            };

            try
            {
                // Act & Assert - should not throw exception
                Assert.DoesNotThrow(() => BomHelper.NamingConventionOfSPDXFile(spdxFile, appSettings));
            }
            finally
            {
                // Cleanup
                if (System.IO.Directory.Exists(tempDir))
                    System.IO.Directory.Delete(tempDir, true);
            }
        }

        [Test]
        public async Task GetNpmListOfComponentsFromRepo_WithNullRepoList_ReturnsEmptyList()
        {
            // Arrange
            string[] repoList = null;
            var jFrogServiceMock = new Mock<IJFrogService>();
            var bomHelper = new BomHelper();

            // Act
            var result = await bomHelper.GetNpmListOfComponentsFromRepo(repoList, jFrogServiceMock.Object);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsEmpty(result);
        }

        [Test]
        public async Task GetNpmListOfComponentsFromRepo_WithValidRepoList_ReturnsComponents()
        {
            // Arrange
            string[] repoList = new string[] { "npm-repo1", "npm-repo2" };
            var jFrogServiceMock = new Mock<IJFrogService>();
            var bomHelper = new BomHelper();
            var expectedResults = new List<AqlResult>()
            {
                new AqlResult { Name = "npm-component1", Repo = "npm-repo1" },
                new AqlResult { Name = "npm-component2", Repo = "npm-repo2" }
            };

            jFrogServiceMock.Setup(x => x.GetNpmComponentDataByRepo("npm-repo1"))
                           .ReturnsAsync(new List<AqlResult> { expectedResults[0] });
            jFrogServiceMock.Setup(x => x.GetNpmComponentDataByRepo("npm-repo2"))
                           .ReturnsAsync(new List<AqlResult> { expectedResults[1] });

            // Act
            var result = await bomHelper.GetNpmListOfComponentsFromRepo(repoList, jFrogServiceMock.Object);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count);
            Assert.Contains(expectedResults[0], result);
            Assert.Contains(expectedResults[1], result);
        }

        [Test]
        public async Task GetPypiListOfComponentsFromRepo_WithNullRepoList_ReturnsEmptyList()
        {
            // Arrange
            string[] repoList = null;
            var jFrogServiceMock = new Mock<IJFrogService>();
            var bomHelper = new BomHelper();

            // Act
            var result = await bomHelper.GetPypiListOfComponentsFromRepo(repoList, jFrogServiceMock.Object);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsEmpty(result);
        }

        [Test]
        public async Task GetPypiListOfComponentsFromRepo_WithValidRepoList_ReturnsComponents()
        {
            // Arrange
            string[] repoList = new string[] { "pypi-repo1", "pypi-repo2" };
            var jFrogServiceMock = new Mock<IJFrogService>();
            var bomHelper = new BomHelper();
            var expectedResults = new List<AqlResult>()
            {
                new AqlResult { Name = "pypi-component1", Repo = "pypi-repo1" },
                new AqlResult { Name = "pypi-component2", Repo = "pypi-repo2" }
            };

            jFrogServiceMock.Setup(x => x.GetPypiComponentDataByRepo("pypi-repo1"))
                           .ReturnsAsync(new List<AqlResult> { expectedResults[0] });
            jFrogServiceMock.Setup(x => x.GetPypiComponentDataByRepo("pypi-repo2"))
                           .ReturnsAsync(new List<AqlResult> { expectedResults[1] });

            // Act
            var result = await bomHelper.GetPypiListOfComponentsFromRepo(repoList, jFrogServiceMock.Object);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count);
            Assert.Contains(expectedResults[0], result);
            Assert.Contains(expectedResults[1], result);
        }
        [Test]
        public void GetProjectSummaryLink_ReturnsCorrectUrl()
        {
            // Arrange
            string projectId = "test-project-123";
            string sw360Url = "https://sw360.example.com";
            var bomHelper = new BomHelper();

            // Act
            string result = bomHelper.GetProjectSummaryLink(projectId, sw360Url);

            // Assert
            Assert.That(result, Does.Contain(sw360Url));
            Assert.That(result, Does.Contain(projectId));
        }

        [Test]
        public void ParseBomFile_WithSpdxFile_CallsSpdxParser()
        {
            // Arrange
            string tempFile = Path.GetTempFileName();
            string spdxFile = Path.ChangeExtension(tempFile, FileConstant.SPDXFileExtension);
            System.IO.File.WriteAllText(spdxFile, "test content");

            var mockSpdxParser = new Mock<ISpdxBomParser>();
            var mockCycloneDxParser = new Mock<ICycloneDXBomParser>();
            var appSettings = new CommonAppSettings { ProjectType = "NPM" };
            var expectedBom = new Bom
            {
                Components = new List<Component>(),
                Dependencies = new List<Dependency>()
            };

            mockSpdxParser.Setup(x => x.ParseSPDXBom(spdxFile)).Returns(expectedBom);

            try
            {
                // Act
                var listUnsupportedComponents = new Bom()
                {
                    Components = new List<Component>(),
                    Dependencies = new List<Dependency>()
                };
                var result = BomHelper.ParseBomFile(spdxFile, mockSpdxParser.Object, mockCycloneDxParser.Object, appSettings, ref listUnsupportedComponents);

                // Assert
                Assert.AreEqual(expectedBom, result);
                mockSpdxParser.Verify(x => x.ParseSPDXBom(spdxFile), Times.Once);
                mockCycloneDxParser.Verify(x => x.ParseCycloneDXBom(It.IsAny<string>()), Times.Never);
            }
            finally
            {
                // Cleanup
                if (System.IO.File.Exists(spdxFile))
                    System.IO.File.Delete(spdxFile);
                if (System.IO.File.Exists(tempFile))
                    System.IO.File.Delete(tempFile);
            }
        }

        [Test]
        public void ParseBomFile_WithCycloneDxFile_CallsCycloneDxParser()
        {
            // Arrange
            string tempFile = Path.GetTempFileName();
            string cyclonDxFile = Path.ChangeExtension(tempFile, ".json");
            System.IO.File.WriteAllText(cyclonDxFile, "test content");

            var mockSpdxParser = new Mock<ISpdxBomParser>();
            var mockCycloneDxParser = new Mock<ICycloneDXBomParser>();
            var appSettings = new CommonAppSettings { ProjectType = "NPM" };
            var expectedBom = new Bom
            {
                Components = new List<Component>(),
                Dependencies = new List<Dependency>()
            };

            mockCycloneDxParser.Setup(x => x.ParseCycloneDXBom(cyclonDxFile)).Returns(expectedBom);

            try
            {
                // Act
                var listUnsupportedComponents = new Bom()
                {
                    Components = new List<Component>(),
                    Dependencies = new List<Dependency>()
                };
                var result = BomHelper.ParseBomFile(cyclonDxFile, mockSpdxParser.Object, mockCycloneDxParser.Object, appSettings, ref listUnsupportedComponents);

                // Assert
                Assert.AreEqual(expectedBom, result);
                mockCycloneDxParser.Verify(x => x.ParseCycloneDXBom(cyclonDxFile), Times.Once);
                mockSpdxParser.Verify(x => x.ParseSPDXBom(It.IsAny<string>()), Times.Never);
            }
            finally
            {
                // Cleanup
                if (System.IO.File.Exists(cyclonDxFile))
                    System.IO.File.Delete(cyclonDxFile);
                if (System.IO.File.Exists(tempFile))
                    System.IO.File.Delete(tempFile);
            }
        }


        [Test]
        public void ParseBomFile_WithEmptyDirectory_ReturnsEmptyBom()
        {
            // Arrange
            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            System.IO.Directory.CreateDirectory(tempDir);

            var mockSpdxParser = new Mock<ISpdxBomParser>();
            var mockCycloneDxParser = new Mock<ICycloneDXBomParser>();
            var appSettings = new CommonAppSettings { ProjectType = "NPM" };

            // Setup mock to return empty BOM with initialized collections
            var emptyBom = new Bom
            {
                Components = new List<Component>(),
                Dependencies = new List<Dependency>()
            };
            mockCycloneDxParser.Setup(x => x.ParseCycloneDXBom(It.IsAny<string>())).Returns(emptyBom);

            try
            {
                // Act
                var listUnsupportedComponents = new Bom()
                {
                    Components = new List<Component>(),
                    Dependencies = new List<Dependency>()
                };
                var result = BomHelper.ParseBomFile(tempDir, mockSpdxParser.Object, mockCycloneDxParser.Object, appSettings, ref listUnsupportedComponents);

                // Assert
                Assert.IsNotNull(result);
                Assert.IsInstanceOf<Bom>(result);
            }
            finally
            {
                // Cleanup
                if (System.IO.Directory.Exists(tempDir))
                    System.IO.Directory.Delete(tempDir, true);
            }
        }

        [Test]
        public void ParseBomFile_WithDirectoryContainingInvalidSpdxFiles_ReturnsEmptyBom()
        {
            // Arrange
            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            System.IO.Directory.CreateDirectory(tempDir);


            string spdxFile = Path.Combine(tempDir, "invalid.spdx.sbom.json");
            System.IO.File.WriteAllText(spdxFile, "invalid content");

            var mockSpdxParser = new Mock<ISpdxBomParser>();
            var mockCycloneDxParser = new Mock<ICycloneDXBomParser>();
            var appSettings = new CommonAppSettings { ProjectType = "NPM" };

            // Setup to return empty BOM when parsing fails (as the real parser does)
            var emptyBom = new Bom { Components = new List<Component>(), Dependencies = new List<Dependency>() };
            mockSpdxParser.Setup(x => x.ParseSPDXBom(spdxFile)).Returns(emptyBom);

            try
            {
                // Act - test with the actual SPDX file, not the directory
                var listUnsupportedComponents = new Bom()
                {
                    Components = new List<Component>(),
                    Dependencies = new List<Dependency>()
                };
                var result = BomHelper.ParseBomFile(spdxFile, mockSpdxParser.Object, mockCycloneDxParser.Object, appSettings, ref listUnsupportedComponents);

                // Assert
                Assert.IsNotNull(result);
                Assert.IsInstanceOf<Bom>(result);
                Assert.IsNotNull(result.Components);
                Assert.IsNotNull(result.Dependencies);
            }
            finally
            {
                // Cleanup
                if (System.IO.Directory.Exists(tempDir))
                    System.IO.Directory.Delete(tempDir, true);
            }
        }

        [Test]
        public void NamingConventionOfSPDXFile_WithMissingFiles_ContinuesWithoutException()
        {
            // Arrange
            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            System.IO.Directory.CreateDirectory(tempDir);

            string spdxFile = Path.Combine(tempDir, "test.spdx");
            System.IO.File.WriteAllText(spdxFile, "test content");

            var appSettings = new CommonAppSettings()
            {
                Directory = new LCT.Common.Directory()
                {
                    InputFolder = tempDir
                }
            };

            try
            {
                // Act & Assert - should not throw exception
                Assert.DoesNotThrow(() => BomHelper.NamingConventionOfSPDXFile(spdxFile, appSettings));
            }
            finally
            {
                // Cleanup
                if (System.IO.Directory.Exists(tempDir))
                    System.IO.Directory.Delete(tempDir, true);
            }
        }

        [Test]
        public void NamingConventionOfSPDXFile_WithExistingFiles_ValidatesFiles()
        {
            // Arrange
            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            System.IO.Directory.CreateDirectory(tempDir);

            string spdxFile = Path.Combine(tempDir, "test.spdx");
            string pemFile = Path.Combine(tempDir, "test.spdx.pem");
            string sigFile = Path.Combine(tempDir, "test.spdx.sig");

            System.IO.File.WriteAllText(spdxFile, "test content");
            System.IO.File.WriteAllText(pemFile, "test pem content");
            System.IO.File.WriteAllText(sigFile, "test sig content");

            var appSettings = new CommonAppSettings()
            {
                Directory = new LCT.Common.Directory()
                {
                    InputFolder = tempDir
                }
            };

            try
            {
                // Act & Assert - should not throw exception
                Assert.DoesNotThrow(() => BomHelper.NamingConventionOfSPDXFile(spdxFile, appSettings));
            }
            finally
            {
                // Cleanup
                if (System.IO.Directory.Exists(tempDir))
                    System.IO.Directory.Delete(tempDir, true);
            }
        }

        [Test]
        public async Task GetNpmListOfComponentsFromRepo_WithNullRepoList_ReturnsEmptyList()
        {
            // Arrange
            string[] repoList = null;
            var jFrogServiceMock = new Mock<IJFrogService>();
            var bomHelper = new BomHelper();

            // Act
            var result = await bomHelper.GetNpmListOfComponentsFromRepo(repoList, jFrogServiceMock.Object);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsEmpty(result);
        }

        [Test]
        public async Task GetNpmListOfComponentsFromRepo_WithValidRepoList_ReturnsComponents()
        {
            // Arrange
            string[] repoList = new string[] { "npm-repo1", "npm-repo2" };
            var jFrogServiceMock = new Mock<IJFrogService>();
            var bomHelper = new BomHelper();
            var expectedResults = new List<AqlResult>()
            {
                new AqlResult { Name = "npm-component1", Repo = "npm-repo1" },
                new AqlResult { Name = "npm-component2", Repo = "npm-repo2" }
            };

            jFrogServiceMock.Setup(x => x.GetNpmComponentDataByRepo("npm-repo1"))
                           .ReturnsAsync(new List<AqlResult> { expectedResults[0] });
            jFrogServiceMock.Setup(x => x.GetNpmComponentDataByRepo("npm-repo2"))
                           .ReturnsAsync(new List<AqlResult> { expectedResults[1] });

            // Act
            var result = await bomHelper.GetNpmListOfComponentsFromRepo(repoList, jFrogServiceMock.Object);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count);
            Assert.Contains(expectedResults[0], result);
            Assert.Contains(expectedResults[1], result);
        }

        [Test]
        public async Task GetPypiListOfComponentsFromRepo_WithNullRepoList_ReturnsEmptyList()
        {
            // Arrange
            string[] repoList = null;
            var jFrogServiceMock = new Mock<IJFrogService>();
            var bomHelper = new BomHelper();

            // Act
            var result = await bomHelper.GetPypiListOfComponentsFromRepo(repoList, jFrogServiceMock.Object);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsEmpty(result);
        }

        [Test]
        public async Task GetPypiListOfComponentsFromRepo_WithValidRepoList_ReturnsComponents()
        {
            // Arrange
            string[] repoList = new string[] { "pypi-repo1", "pypi-repo2" };
            var jFrogServiceMock = new Mock<IJFrogService>();
            var bomHelper = new BomHelper();
            var expectedResults = new List<AqlResult>()
            {
                new AqlResult { Name = "pypi-component1", Repo = "pypi-repo1" },
                new AqlResult { Name = "pypi-component2", Repo = "pypi-repo2" }
            };

            jFrogServiceMock.Setup(x => x.GetPypiComponentDataByRepo("pypi-repo1"))
                           .ReturnsAsync(new List<AqlResult> { expectedResults[0] });
            jFrogServiceMock.Setup(x => x.GetPypiComponentDataByRepo("pypi-repo2"))
                           .ReturnsAsync(new List<AqlResult> { expectedResults[1] });

            // Act
            var result = await bomHelper.GetPypiListOfComponentsFromRepo(repoList, jFrogServiceMock.Object);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count);
            Assert.Contains(expectedResults[0], result);
            Assert.Contains(expectedResults[1], result);
        }
    }
}
