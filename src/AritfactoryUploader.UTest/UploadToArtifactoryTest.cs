// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models;
using LCT.APICommunications;
using LCT.APICommunications.Model;
using LCT.APICommunications.Model.AQL;
using LCT.ArtifactoryUploader;
using LCT.ArtifactoryUploader.Model;
using LCT.Common;
using LCT.Common.Constants;
using LCT.Common.Model;
using LCT.Services.Interface;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnitTestUtilities;

namespace AritfactoryUploader.UTest
{
    public class UploadToArtifactoryTest
    {
        [Test]
        [TestCase("NPM", "?to=/destination-repo//")]
        [TestCase("NUGET", "source-repo/package-name.1.0.0.nupkg?to=/destination-repo/package-name.1.0.0.nupkg")]
        [TestCase("MAVEN", "source-repo/package-name/1.0.0?to=/destination-repo/package-name/1.0.0")]
        [TestCase("CONAN", "source-repo/?to=/destination-repo/")]
        [TestCase("POETRY", "?to=/destination-repo/")]
        [TestCase("DEBIAN", "source-repo//package-name_1.0.0*?to=/destination-repo//package-name_1.0.0*")]
        public void GetMoveURL_GivenComponentType_ReturnsMoveURL(string type, string extension)
        {
            // Arrange
            var component = new ComponentsToArtifactory
            {
                ComponentType = type,
                JfrogApi = "https://example.com",
                SrcRepoName = "source-repo",
                Name = "package-name",
                PackageName = "package-name",
                Version = "1.0.0",
                DestRepoName = "destination-repo",
                DryRun = false
            };
            var expectedUrl = $"https://example.com/api/move/{extension}";

            // Act
            var result = UploadToArtifactory.GetMoveURL(component);

            // Assert
            Assert.AreEqual(expectedUrl, result);
        }

        [Test]
        public void GetMoveURL_GivenInvalidComponentType_ReturnsEmptyString()
        {
            // Arrange
            var component = new ComponentsToArtifactory
            {
                ComponentType = "INVALID",
                JfrogApi = "https://example.com/api/",
                SrcRepoName = "source-repo",
                Name = "package-name",
                PackageName = "package-name",
                Version = "1.0.0",
                DestRepoName = "destination-repo",
                DryRun = false
            };

            // Act
            var result = UploadToArtifactory.GetMoveURL(component);

            // Assert
            Assert.AreEqual(string.Empty, result);
        }

        [Test]
        public async Task GetSrcRepoDetailsForPyPiOrConanPackages_WhenNpmRepoExists_ReturnsArtifactoryRepoName()
        {
            // Arrange
            var repoNameProperty = new Property
            {
                Name = Dataconstant.Cdx_ArtifactoryRepoName,
                Value = "npm-repo"
            };
            var properties = new List<Property> { repoNameProperty };
            var item = new Component
            {
                Purl = "pkg:npm/example-package",
                Properties = properties,
                Name = "example-package",
                Version = "1.0.0"
            };
            var aqlResultList = new List<AqlResult>
        {
            new AqlResult
            {
                Repo = "npm-repo",
                Path = "path/to/package",
                Name = "example-package-1.0.0",
                Properties = new List<AqlProperty>
                {
                    new AqlProperty { Key = "npm.name", Value = "example-package" },
                    new AqlProperty { Key = "npm.version", Value = "1.0.0" }
                }
            }
        };

            var jFrogServiceMock = new Mock<IJFrogService>();

            jFrogServiceMock.Setup(x => x.GetNpmComponentDataByRepo(It.IsAny<string>())).ReturnsAsync(aqlResultList);

            UploadToArtifactory.JFrogService = jFrogServiceMock.Object;


            // Act
            var result = await UploadToArtifactory.GetSrcRepoDetailsForComponent(item);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("npm-repo", result.Repo);
            Assert.AreEqual("path/to/package", result.Path);
        }

        [Test]
        public void GetCopyURL_GivenInvalidComponentType_ReturnsEmptyString()
        {
            // Arrange
            var component = new ComponentsToArtifactory
            {
                ComponentType = "INVALID",
                JfrogApi = "https://example.com/api/",
                SrcRepoName = "source-repo",
                Name = "package-name",
                Version = "1.0.0",
                PackageName = "package-name",
                DestRepoName = "destination-repo",
                DryRun = false
            };

            // Act
            var actualUrl = UploadToArtifactory.GetCopyURL(component);

            // Assert
            Assert.AreEqual(string.Empty, actualUrl);
        }

        [Test]
        [TestCase("NPM", "?to=/destination-repo//")]
        [TestCase("NUGET", "source-repo/package-name.1.0.0.nupkg?to=/destination-repo/package-name.1.0.0.nupkg")]
        [TestCase("MAVEN", "source-repo/package-name/1.0.0?to=/destination-repo/package-name/1.0.0")]
        [TestCase("CONAN", "source-repo/?to=/destination-repo/")]
        [TestCase("POETRY", "?to=/destination-repo/")]
        [TestCase("DEBIAN", "source-repo//package-name_1.0.0*?to=/destination-repo//package-name_1.0.0*")]
        public void GetCopyURL_GivenComponentType_ReturnsCopyURL(string type, string pkgExtension)
        {
            // Arrange
            var component = new ComponentsToArtifactory
            {
                ComponentType = type,
                JfrogApi = "https://example.com",
                SrcRepoName = "source-repo",
                Name = "package-name",
                Version = "1.0.0",
                PackageName = "package-name",
                DestRepoName = "destination-repo",
                DryRun = false
            };
            var expectedUrl = $"https://example.com/api/copy/{pkgExtension}";

            // Act
            var actualUrl = UploadToArtifactory.GetCopyURL(component);

            // Assert
            Assert.AreEqual(expectedUrl, actualUrl);
        }

        [Test]
        public async Task GetComponentsToBeUploadedToArtifactory_GivenFewApprovedComponentList_ReturnsUploadList()
        {
            //Arrange
            List<Component> componentLists = GetComponentList();
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            DisplayPackagesInfo displayPackagesInfo = PackageUploadInformation.GetComponentsToBePackages();

            var jFrogServiceMock = new Mock<IJFrogService>();
            UploadToArtifactory.JFrogService = jFrogServiceMock.Object;

            CommonAppSettings commonAppSettings = new CommonAppSettings();
            commonAppSettings.Jfrog = new Jfrog()
            {
                Token = "wfwfwfwfwegwgweg",
                URL = UTParams.JFrogURL
            };
            commonAppSettings.Npm = new Config()
            {
                Artifactory = new Artifactory()
                {
                    ThirdPartyRepos = new List<ThirdPartyRepo>()
                    {
                        new() { Name = "npm-test" }
                    }
                }
            };
            commonAppSettings.Nuget = new Config()
            {
                Artifactory = new Artifactory()
                {
                    ThirdPartyRepos = new List<ThirdPartyRepo>()
                    {
                        new() { Name = "nuget-test" }
                    }
                }
            };
            commonAppSettings.Directory.LogFolder = outFolder;

            //Act
            List<ComponentsToArtifactory> uploadList = await UploadToArtifactory.GetComponentsToBeUploadedToArtifactory(componentLists, commonAppSettings, displayPackagesInfo);
            // Assert
            Assert.That(3, Is.EqualTo(uploadList.Count), "Checks for 2  no of components to upload");
        }
        [Test]
        public async Task GetComponentsToBeUploadedToArtifactory_GivenNotApprovedComponentList_ReturnsUploadList()
        {
            //Arrange
            List<Component> componentLists = GetComponentList();
            DisplayPackagesInfo displayPackagesInfo = PackageUploadInformation.GetComponentsToBePackages();
            foreach (var component in componentLists)
            {
                component.Properties[1].Value = "NEW_CLEARING";
            }

            CommonAppSettings commonAppSettings = new CommonAppSettings();
            commonAppSettings.Jfrog = new Jfrog()
            {
                Token = "wfwfwfwfwegwgweg",
                URL = UTParams.JFrogURL
            };
            commonAppSettings.Npm = new Config()
            {
                Artifactory = new Artifactory()
                {
                    ThirdPartyRepos = new List<ThirdPartyRepo>()
                    {
                        new() { Name = "npm-test" }
                    }
                }
            };

            //Act
            List<ComponentsToArtifactory> uploadList = await UploadToArtifactory.GetComponentsToBeUploadedToArtifactory(componentLists, commonAppSettings, displayPackagesInfo);

            // Assert
            Assert.That(0, Is.EqualTo(uploadList.Count), "Checks for components to upload to be zero");
        }
        [Test]
        public async Task GetComponentsToBeUploadedToArtifactory_GivenAllApprovedComponentList_ReturnsUploadList()
        {
            //Arrange
            List<Component> componentLists = GetComponentList();
            DisplayPackagesInfo displayPackagesInfo = PackageUploadInformation.GetComponentsToBePackages();

            var jFrogServiceMock = new Mock<IJFrogService>();
            UploadToArtifactory.JFrogService = jFrogServiceMock.Object;

            foreach (var component in componentLists)
            {
                if (component.Name == "@angular/core")
                {
                    component.Properties[1].Value = "APPROVED";
                }

            }
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);

            CommonAppSettings commonAppSettings = new CommonAppSettings();
            commonAppSettings.Jfrog = new Jfrog()
            {
                Token = "wfwfwfwfwegwgweg",
                URL = UTParams.JFrogURL
            };
            commonAppSettings.Npm = new Config()
            {
                Artifactory = new Artifactory()
                {
                    ThirdPartyRepos = new List<ThirdPartyRepo>()
                    {
                        new() { Name = "npm-test" }
                    }
                }
            };
            commonAppSettings.Nuget = new Config()
            {
                Artifactory = new Artifactory()
                {
                    ThirdPartyRepos = new List<ThirdPartyRepo>()
                    {
                        new() { Name = "nuget-test" }
                    }
                }
            };
            commonAppSettings.Directory.LogFolder = outFolder;


            //Act
            List<ComponentsToArtifactory> uploadList = await UploadToArtifactory.GetComponentsToBeUploadedToArtifactory(componentLists, commonAppSettings, displayPackagesInfo);

            // Assert
            Assert.That(4, Is.EqualTo(uploadList.Count), "Checks for 3 no of components to upload");
        }
        [Test]
        public async Task GetSrcRepoDetailsForPyPiOrConanPackages_WhenPypiRepoExists_ReturnsArtifactoryRepoName()
        {
            // Arrange
            Property repoNameProperty = new Property
            {
                Name = Dataconstant.Cdx_ArtifactoryRepoName,
                Value = "Reponame"
            };
            List<Property> properties = new List<Property>() { repoNameProperty };
            var item = new Component
            {
                Purl = "pypi://example-package",
                Properties = properties,
                Name = "pypi component",
                Version = "1.0.0"
            };
            AqlProperty pypiNameProperty = new AqlProperty
            {
                Key = "pypi.normalized.name",
                Value = "pypi component"
            };

            AqlProperty pypiVersionProperty = new AqlProperty
            {
                Key = "pypi.version",
                Value = "1.0.0"
            };
            List<AqlProperty> propertys = new List<AqlProperty> { pypiNameProperty, pypiVersionProperty };
            //GetInternalComponentDataByRepo
            var aqlResultList = new List<AqlResult>
            {
                new AqlResult
                {
                    Repo = "pypi-repo",
                    Path = "path/to/package",
                    Name = "pypi component-1.0.0",
                    Properties=propertys,
                }
            };
            var jFrogServiceMock = new Mock<IJFrogService>();

            jFrogServiceMock.Setup(x => x.GetPypiComponentDataByRepo(It.IsAny<string>())).ReturnsAsync(aqlResultList);


            UploadToArtifactory.JFrogService = jFrogServiceMock.Object;

            // Act
            var result = await UploadToArtifactory.GetSrcRepoDetailsForComponent(item);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("pypi-repo", result.Repo);
            Assert.AreEqual("path/to/package", result.Path);
        }
        public async static Task GetSrcRepoDetailsForPyPiOrConanPackages_WhenConanRepoExists_ReturnsArtifactoryRepoName()
        {
            // Arrange
            Property prop1 = new Property
            {
                Name = Dataconstant.Cdx_ArtifactoryRepoName,
                Value = "Reponame"
            };
            List<Property> properties = new List<Property>() { prop1 };
            var item = new Component
            {
                Purl = "conan://example-package",
                Properties = properties,
                Name = "conancomponent",
                Version = "1.0.0"
            };
            var aqlResultList = new List<AqlResult>
            {
                new AqlResult
                {
                    Repo = "conan-repo",
                    Path = "path/to/conancomponent/1.0.0",
                    Name = "conan component-1.0.0",
                }
            };

            var jFrogServiceMock = new Mock<IJFrogService>();
            jFrogServiceMock.Setup(x => x.GetInternalComponentDataByRepo(It.IsAny<string>())).ReturnsAsync(aqlResultList);
            UploadToArtifactory.JFrogService = jFrogServiceMock.Object;

            // Act
            var result = await UploadToArtifactory.GetSrcRepoDetailsForComponent(item);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("conan-repo", result.Repo);
            Assert.AreEqual("path/to/conancomponent/1.0.0", result.Path);
        }
        [Test]
        public async Task GetSrcRepoDetailsForPyPiOrConanPackages_WhenNoRepoExists_ReturnsNull()
        {
            // Arrange
            var item = new Component
            {
                Purl = "unknown://example-package"
            };
            var jFrogServiceMock = new Mock<IJFrogService>();
            UploadToArtifactory.JFrogService = jFrogServiceMock.Object;

            // Act
            var result = await UploadToArtifactory.GetSrcRepoDetailsForComponent(item);

            // Assert
            Assert.IsNull(result);
        }
        [Test]
        public void GetJfrogRepPath_NpmComponent_ReturnsExpectedPath()
        {
            var component = new ComponentsToArtifactory
            {
                ComponentType = "NPM",
                DestRepoName = "npm-repo",
                Path = "package/path",
                PypiOrNpmCompName = "my-npm-package"
            };

            var result = UploadToArtifactory.GetJfrogRepPath(component);

            Assert.AreEqual("npm-repo/package/path/my-npm-package", result);
        }

        [Test]
        public void GetJfrogRepPath_NugetComponent_ReturnsExpectedPath()
        {
            var component = new ComponentsToArtifactory
            {
                ComponentType = "NUGET",
                DestRepoName = "nuget-repo",
                Name = "MyNuget",
                Version = "1.2.3"
            };

            var result = UploadToArtifactory.GetJfrogRepPath(component);

            Assert.AreEqual($"nuget-repo/MyNuget.1.2.3{ApiConstant.NugetExtension}", result);
        }

        [Test]
        public void GetJfrogRepPath_MavenComponent_ReturnsExpectedPath()
        {
            var component = new ComponentsToArtifactory
            {
                ComponentType = "MAVEN",
                DestRepoName = "maven-repo",
                Name = "my-maven",
                Version = "2.0.0"
            };

            var result = UploadToArtifactory.GetJfrogRepPath(component);

            Assert.AreEqual("maven-repo/my-maven/2.0.0", result);
        }

        [Test]
        public void GetJfrogRepPath_PoetryComponent_ReturnsExpectedPath()
        {
            var component = new ComponentsToArtifactory
            {
                ComponentType = "POETRY",
                DestRepoName = "poetry-repo",
                PypiOrNpmCompName = "my-poetry"
            };

            var result = UploadToArtifactory.GetJfrogRepPath(component);

            Assert.AreEqual("poetry-repo/my-poetry", result);
        }

        [Test]
        public void GetJfrogRepPath_ConanComponent_ReturnsExpectedPath()
        {
            var component = new ComponentsToArtifactory
            {
                ComponentType = "CONAN",
                DestRepoName = "conan-repo",
                Path = "conan/path"
            };

            var result = UploadToArtifactory.GetJfrogRepPath(component);

            Assert.AreEqual("conan-repo/conan/path", result);
        }

        [Test]
        public void GetJfrogRepPath_DebianComponent_ReturnsExpectedPath()
        {
            var component = new ComponentsToArtifactory
            {
                ComponentType = "DEBIAN",
                DestRepoName = "debian-repo",
                Path = "debian/path",
                Name = "my-deb",
                Version = "1.0.0"
            };

            var result = UploadToArtifactory.GetJfrogRepPath(component);

            Assert.AreEqual("debian-repo/debian/path/my-deb_1.0.0*", result);
        }

        [Test]
        public void GetJfrogRepPath_UnknownComponent_ReturnsEmptyString()
        {
            var component = new ComponentsToArtifactory
            {
                ComponentType = "UNKNOWN"
            };

            var result = UploadToArtifactory.GetJfrogRepPath(component);

            Assert.AreEqual(string.Empty, result);
        }
        private static List<Component> GetComponentList()
        {
            List<Component> componentLists = new List<Component>();
            Property propinternal = new Property
            {
                Name = Dataconstant.Cdx_IsInternal,
                Value = "false"
            };
            Property prop = new Property
            {
                Name = Dataconstant.Cdx_ClearingState,
                Value = "APPROVED"
            };
            Component comp1 = new Component
            {
                Name = "@angular/animations",
                Version = "11.2.3",
                Purl = "pkg:npm/%40angular/animations@11.0.4",
                Properties = new List<Property>()
            };
            comp1.Properties.Add(propinternal);
            comp1.Properties.Add(prop);
            componentLists.Add(comp1);

            Property prop1 = new Property
            {
                Name = Dataconstant.Cdx_ClearingState,
                Value = "NEW_CLEARING"
            };
            Component comp2 = new Component
            {
                Name = "@angular/core",
                Version = "11.2.3",
                Purl = "pkg:npm/%40angular/core@11.0.4",
                Properties = new List<Property>()
            };
            comp2.Properties.Add(propinternal);
            comp2.Properties.Add(prop1);
            componentLists.Add(comp2);

            Property prop2 = new Property
            {
                Name = Dataconstant.Cdx_ClearingState,
                Value = "APPROVED"
            };
            Component comp3 = new Component
            {
                Name = "NewtonsoftJson",
                Version = "11.9.3",
                Purl = "pkg:nuget/NewtonsoftJson@11.9.3",
                Properties = new List<Property>()
            };
            comp3.Properties.Add(propinternal);
            comp3.Properties.Add(prop2);
            componentLists.Add(comp3);

            Property prop3 = new Property
            {
                Name = Dataconstant.Cdx_ClearingState,
                Value = "APPROVED"
            };
            Component comp4 = new Component
            {
                Name = "adduser",
                Version = "11.9.3",
                Purl = "pkg:deb/adduser@11.9.3",
                Properties = new List<Property>()
            };
            comp4.Properties.Add(propinternal);
            comp4.Properties.Add(prop3);
            componentLists.Add(comp4);
            return componentLists;
        }
    }
}
