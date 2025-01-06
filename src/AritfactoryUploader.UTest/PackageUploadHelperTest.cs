// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.Common;
using CycloneDX.Models;
using LCT.APICommunications.Model;
using LCT.ArtifactoryUploader;
using LCT.Common.Constants;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using UnitTestUtilities;
using System.Threading.Tasks;
using System.Linq;
using LCT.ArtifactoryUploader.Model;
using LCT.APICommunications;
using System.Net.Http;
using System.Net;
using LCT.APICommunications.Model.AQL;
using LCT.Services.Interface;
using Moq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using LCT.Common.Interface;

namespace AritfactoryUploader.UTest
{
    [TestFixture]
    public class PackageUploadHelperTest
    {

        [Test]
        public void GetComponentListFromComparisonBOM_GivenComparisonBOM_ReturnsComponentList()
        {
            //Arrange
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string comparisonBOMPath = outFolder + @"\ArtifactoryUTTestFiles\CyclonedxBom.json";
            //Act
            Bom componentList = PackageUploadHelper.GetComponentListFromComparisonBOM(comparisonBOMPath);
            // Assert
            Assert.That(6, Is.EqualTo(componentList.Components.Count), "Checks for no of components");
        }

        [Test]
        public void GetComponentListFromComparisonBOM_GivenInvalidComparisonBOM_ReturnsException()
        {
            //Arrange
            string comparisonBOMPath = @"TestFiles\CCTComparisonBOM.json";

            //Act && Assert
            Assert.Throws<FileNotFoundException>(() => PackageUploadHelper.GetComponentListFromComparisonBOM(comparisonBOMPath));
        }
        [Test]
        public void GetComponentListFromComparisonBOM_GivenInvalidfile_ReturnsException()
        {
            //Arrange
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string comparisonBOMPath = outFolder + @"\ArtifactoryUTTestFiles\ComparisonBOM.json";

            //Act && Assert
            Assert.Throws<System.Text.Json.JsonException>(() => PackageUploadHelper.GetComponentListFromComparisonBOM(comparisonBOMPath));
        }


        [Test]
        public async Task GetComponentsToBeUploadedToArtifactory_GivenFewApprovedComponentList_ReturnsUploadList()
        {
            //Arrange
            List<Component> componentLists = GetComponentList();
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            DisplayPackagesInfo displayPackagesInfo = PackageUploadHelper.GetComponentsToBePackages();
            CommonAppSettings appSettings = new CommonAppSettings()
            {
                ArtifactoryUploadApiKey = "wfwfwfwfwegwgweg",
                ArtifactoryUploadUser = "user@account.com",
                Npm = new LCT.Common.Model.Config { JfrogThirdPartyDestRepoName = "npm-test" },
                Nuget = new LCT.Common.Model.Config { JfrogThirdPartyDestRepoName = "nuget-test" },
                JfrogNpmSrcRepo = "remote-cache",
                JFrogApi = UTParams.JFrogURL,
                LogFolderPath = outFolder
            };

            //Act
            List<ComponentsToArtifactory> uploadList = await PackageUploadHelper.GetComponentsToBeUploadedToArtifactory(componentLists, appSettings, displayPackagesInfo);
            // Assert
            Assert.That(3, Is.EqualTo(uploadList.Count), "Checks for 2  no of components to upload");
        }


        [Test]
        public async Task GetComponentsToBeUploadedToArtifactory_GivenAllApprovedComponentList_ReturnsUploadList()
        {
            //Arrange
            List<Component> componentLists = GetComponentList();
            DisplayPackagesInfo displayPackagesInfo = PackageUploadHelper.GetComponentsToBePackages();
            foreach (var component in componentLists)
            {
                if (component.Name == "@angular/core")
                {
                    component.Properties[1].Value = "APPROVED";
                }

            }
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            CommonAppSettings appSettings = new CommonAppSettings()
            {
                ArtifactoryUploadApiKey = "wfwfwfwfwegwgweg",
                ArtifactoryUploadUser = "user@account.com",
                Npm = new LCT.Common.Model.Config { JfrogThirdPartyDestRepoName = "npm-test" },
                Nuget = new LCT.Common.Model.Config { JfrogThirdPartyDestRepoName = "nuget-test" },
                JFrogApi = UTParams.JFrogURL,
                LogFolderPath = outFolder
            };

            //Act
            List<ComponentsToArtifactory> uploadList = await PackageUploadHelper.GetComponentsToBeUploadedToArtifactory(componentLists, appSettings, displayPackagesInfo);

            // Assert
            Assert.That(4, Is.EqualTo(uploadList.Count), "Checks for 3 no of components to upload");
        }

        [Test]
        public async Task GetComponentsToBeUploadedToArtifactory_GivenNotApprovedComponentList_ReturnsUploadList()
        {
            //Arrange
            List<Component> componentLists = GetComponentList();
            DisplayPackagesInfo displayPackagesInfo = PackageUploadHelper.GetComponentsToBePackages();
            foreach (var component in componentLists)
            {
                component.Properties[1].Value = "NEW_CLEARING";
            }

            CommonAppSettings appSettings = new CommonAppSettings()
            {
                ArtifactoryUploadApiKey = "wfwfwfwfwegwgweg",
                ArtifactoryUploadUser = "user@account.com",
                Npm = new LCT.Common.Model.Config
                {
                    JfrogThirdPartyDestRepoName = "nuget-test",
                },
                JFrogApi = UTParams.JFrogURL
            };

            //Act
            List<ComponentsToArtifactory> uploadList = await PackageUploadHelper.GetComponentsToBeUploadedToArtifactory(componentLists, appSettings, displayPackagesInfo);

            // Assert
            Assert.That(0, Is.EqualTo(uploadList.Count), "Checks for components to upload to be zero");
        }

        [Test]
        public void UpdateBomArtifactoryRepoUrl_GivenBomAndComponentsUploadedToArtifactory_UpdatesBom()
        {
            //Arrange
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string comparisonBOMPath = outFolder + @"\ArtifactoryUTTestFiles\CyclonedxBom.json";
            Bom bom = PackageUploadHelper.GetComponentListFromComparisonBOM(comparisonBOMPath);
            List<ComponentsToArtifactory> components = new List<ComponentsToArtifactory>()
            {
                new ComponentsToArtifactory()
                {
                    Purl = "pkg:npm/rxjs@6.5.4",
                    DestRepoName = "org1-npmjs-npm-remote",
                    DryRun = false,
                }
            };

            //Act
            PackageUploadHelper.UpdateBomArtifactoryRepoUrl(ref bom, components);

            //Assert
            var repoUrl = bom.Components.First(x => x.Properties[3].Name == "internal:siemens:clearing:jfrog-repo-name").Properties[3].Value;
            Assert.AreEqual("org1-npmjs-npm-remote", repoUrl);
        }

        [Test]
        public void UpdateBomArtifactoryRepoUrl_GivenBomAndComponentsUploadedToArtifactoryDryRun_NoUpdateBom()
        {
            //Arrange
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string comparisonBOMPath = outFolder + @"\ArtifactoryUTTestFiles\CyclonedxBom.json";
            Bom bom = PackageUploadHelper.GetComponentListFromComparisonBOM(comparisonBOMPath);
            List<ComponentsToArtifactory> components = new List<ComponentsToArtifactory>()
            {
                new ComponentsToArtifactory()
                {
                    Purl = "pkg:npm/rxjs@6.5.4",
                    DestRepoName = "org1-npmjs-npm-remote",
                }
            };

            //Act
            PackageUploadHelper.UpdateBomArtifactoryRepoUrl(ref bom, components);

            //Assert
            var repoUrl = bom.Components.First(x => x.Properties[3].Name == "internal:siemens:clearing:jfrog-repo-name").Properties[3].Value;
            Assert.AreNotEqual("org1-npmjs-npm-remote", repoUrl);
        }

        [Test]
        public void DisplayErrorForJfrogPackages_GivenJfrogNotFoundPackages_ResultsSucess()
        {
            // Arrange
            ComponentsToArtifactory componentsToArtifactory = new ComponentsToArtifactory();
            componentsToArtifactory.Name = "Test";
            componentsToArtifactory.Version = "0.12.3";
            List<ComponentsToArtifactory> JfrogNotFoundPackages = new() { componentsToArtifactory };
            // Act

            PackageUploadHelper.DisplayErrorForJfrogPackages(JfrogNotFoundPackages);

            // Assert
            Assert.That(JfrogNotFoundPackages.Count, Is.GreaterThanOrEqualTo(1));
        }

        [Test]
        public void DisplayErrorForJfrogFoundPackages_GivenJfrogNotFoundPackages_ResultsSucess()
        {
            // Arrange
            ComponentsToArtifactory componentsToArtifactory = new ComponentsToArtifactory();
            componentsToArtifactory.Name = "Test";
            componentsToArtifactory.Version = "0.12.3";
            componentsToArtifactory.ResponseMessage = new System.Net.Http.HttpResponseMessage()
            { ReasonPhrase = ApiConstant.ErrorInUpload };

            ComponentsToArtifactory componentsToArtifactory2 = new ComponentsToArtifactory();
            componentsToArtifactory2.Name = "Test2";
            componentsToArtifactory2.Version = "0.12.32";
            componentsToArtifactory2.ResponseMessage = new System.Net.Http.HttpResponseMessage()
            { ReasonPhrase = ApiConstant.PackageNotFound };

            ComponentsToArtifactory componentsToArtifactory3 = new ComponentsToArtifactory();
            componentsToArtifactory3.Name = "Test3";
            componentsToArtifactory3.Version = "0.12.33";
            componentsToArtifactory3.ResponseMessage = new System.Net.Http.HttpResponseMessage()
            { ReasonPhrase = "error" };

            List<ComponentsToArtifactory> JfrogNotFoundPackages = new() {
                componentsToArtifactory, componentsToArtifactory2, componentsToArtifactory3 };
            // Act

            PackageUploadHelper.DisplayErrorForJfrogFoundPackages(JfrogNotFoundPackages);

            // Assert
            Assert.That(JfrogNotFoundPackages.Count, Is.GreaterThanOrEqualTo(1));
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

        [Test]
        public void GetUploadPackageDetails_CoversAllScenarios()
        {
            // Arrange
            DisplayPackagesInfo displayPackagesInfo = new DisplayPackagesInfo()
            {
                JfrogFoundPackagesConan = new List<ComponentsToArtifactory>()
                {
                    new ComponentsToArtifactory()
                    {
                        ResponseMessage = new HttpResponseMessage()
                        {
                            StatusCode = HttpStatusCode.OK
                        }
                    }
                },
                JfrogFoundPackagesMaven = new List<ComponentsToArtifactory>()
                {
                    new ComponentsToArtifactory()
                    {
                        ResponseMessage = new HttpResponseMessage()
                        {
                            StatusCode = HttpStatusCode.OK
                        }
                    }
                },
                JfrogFoundPackagesNpm = new List<ComponentsToArtifactory>()
                {
                    new ComponentsToArtifactory()
                    {
                        ResponseMessage = new HttpResponseMessage()
                        {
                            StatusCode = HttpStatusCode.OK
                        }
                    }
                },
                JfrogFoundPackagesNuget = new List<ComponentsToArtifactory>()
                {
                    new ComponentsToArtifactory()
                    {
                        ResponseMessage = new HttpResponseMessage()
                        {
                            StatusCode = HttpStatusCode.OK
                        }
                    }
                },
                JfrogFoundPackagesPython = new List<ComponentsToArtifactory>()
                {
                    new ComponentsToArtifactory()
                    {
                        ResponseMessage = new HttpResponseMessage()
                        {
                            StatusCode = HttpStatusCode.OK
                        }
                    }
                },
                JfrogFoundPackagesDebian = new List<ComponentsToArtifactory>()
                {
                    new ComponentsToArtifactory()
                    {
                        ResponseMessage = new HttpResponseMessage()
                        {
                            StatusCode = HttpStatusCode.OK
                        }
                    }
                }
            };

            // Act
            List<ComponentsToArtifactory> uploadedPackages = PackageUploadHelper.GetUploadePackageDetails(displayPackagesInfo);

            // Assert
            Assert.AreEqual(6, uploadedPackages.Count);
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
            
            PackageUploadHelper.jFrogService = jFrogServiceMock.Object;

            // Act
            var result = await PackageUploadHelper.GetSrcRepoDetailsForComponent(item);

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
            PackageUploadHelper.jFrogService = jFrogServiceMock.Object;

            // Act
            var result = await PackageUploadHelper.GetSrcRepoDetailsForComponent(item);

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
            PackageUploadHelper.jFrogService = jFrogServiceMock.Object;

            // Act
            var result = await PackageUploadHelper.GetSrcRepoDetailsForComponent(item);

            // Assert
            Assert.IsNull(result);
        }

        [Test]
        [TestCase("NPM", ".tgz")]
        [TestCase("NUGET", ".nupkg")]
        [TestCase("MAVEN", ".jar")]
        [TestCase("DEBIAN", ".deb")]
        [TestCase("PYTHON", ".whl")]
        [TestCase("CONAN", "package.tgz")]
        public void GetPkgeNameExtensionBasedOnComponentType_GivenType_ReturnsPkgNameExtension(string type, string extension)
        {
            // Arrange
            var package = new ComponentsToArtifactory();
            package.ComponentType = type;
            // Act
            var actualExtension = PackageUploadHelper.GetPackageNameExtensionBasedOnComponentType(package);
            // Assert
            Assert.AreEqual(extension, actualExtension);
        }

        [Test]
        public void GetJfrogApiCommInstance_GivenComponent_ReturnsJfrogApiCommunicationInstance()
        {
            // Arrange
            var component = new ComponentsToArtifactory
            {
                ComponentType = "MAVEN",
                JfrogApi = "https://api.jfrog.com",
                SrcRepoName = "maven-repo",
                ApiKey = "api-key",
                Email = "test@example.com"
            };
            var timeout = 5000;

            // Act
            var result = PackageUploadHelper.GetJfrogApiCommInstance(component, timeout);

            // Assert
            Assert.IsInstanceOf<MavenJfrogApiCommunication>(result);
        }

        [Test]
        public void GetJfrogApiCommInstance_GivenComponentWithPythonType_ReturnsJfrogApiCommunicationInstance()
        {
            // Arrange
            var component = new ComponentsToArtifactory
            {
                ComponentType = "PYTHON",
                JfrogApi = "https://api.jfrog.com",
                SrcRepoName = "python-repo",
                ApiKey = "api-key",
                Email = "test@example.com"
            };
            var timeout = 5000;

            // Act
            var result = PackageUploadHelper.GetJfrogApiCommInstance(component, timeout);

            // Assert
            Assert.IsInstanceOf<PythonJfrogApiCommunication>(result);
        }

        [Test]
        public void GetJfrogApiCommInstance_GivenComponentWithUnknownType_ReturnsJfrogApiCommunicationInstance()
        {
            // Arrange
            var component = new ComponentsToArtifactory
            {
                ComponentType = "UNKNOWN",
                JfrogApi = "https://api.jfrog.com",
                SrcRepoName = "unknown-repo",
                ApiKey = "api-key",
                Email = "test@example.com"
            };
            var timeout = 5000;

            // Act
            var result = PackageUploadHelper.GetJfrogApiCommInstance(component, timeout);

            // Assert
            Assert.IsInstanceOf<NpmJfrogApiCommunication>(result);
        }

        [Test]
        [TestCase("NPM")]
        [TestCase("NUGET")]
        [TestCase("MAVEN")]
        [TestCase("PYTHON")]
        [TestCase("CONAN")]
        [TestCase("DEBIAN")]
        public async Task JfrogNotFoundPackagesAsync_CoversAllScenarios(string compType)
        {
            // Arrange
            var item = new ComponentsToArtifactory();
            item.ComponentType = compType;
            var displayPackagesInfo = new DisplayPackagesInfo();
            displayPackagesInfo.JfrogNotFoundPackagesNpm = new List<ComponentsToArtifactory>();
            displayPackagesInfo.JfrogNotFoundPackagesNuget = new List<ComponentsToArtifactory>();
            displayPackagesInfo.JfrogNotFoundPackagesMaven = new List<ComponentsToArtifactory>();
            displayPackagesInfo.JfrogNotFoundPackagesPython = new List<ComponentsToArtifactory>();
            displayPackagesInfo.JfrogNotFoundPackagesConan = new List<ComponentsToArtifactory>();
            displayPackagesInfo.JfrogNotFoundPackagesDebian = new List<ComponentsToArtifactory>();

            // Act
            await PackageUploadHelper.JfrogNotFoundPackagesAsync(item, displayPackagesInfo);

            // Assert
            if (item.ComponentType == "NPM")
            {
                Assert.AreEqual(1, displayPackagesInfo.JfrogNotFoundPackagesNpm.Count);
                Assert.That(displayPackagesInfo.JfrogNotFoundPackagesNpm[0], Is.Not.Null);
            }
            else if (item.ComponentType == "NUGET")
            {
                Assert.AreEqual(1, displayPackagesInfo.JfrogNotFoundPackagesNuget.Count);
                Assert.That(displayPackagesInfo.JfrogNotFoundPackagesNuget[0], Is.Not.Null);
            }
            else if (item.ComponentType == "MAVEN")
            {
                Assert.AreEqual(1, displayPackagesInfo.JfrogNotFoundPackagesMaven.Count);
                Assert.That(displayPackagesInfo.JfrogNotFoundPackagesMaven[0], Is.Not.Null);
            }
            else if (item.ComponentType == "PYTHON")
            {
                Assert.AreEqual(1, displayPackagesInfo.JfrogNotFoundPackagesPython.Count);
                Assert.That(displayPackagesInfo.JfrogNotFoundPackagesPython[0], Is.Not.Null);
            }
            else if (item.ComponentType == "CONAN")
            {
                Assert.AreEqual(1, displayPackagesInfo.JfrogNotFoundPackagesConan.Count);
                Assert.That(displayPackagesInfo.JfrogNotFoundPackagesConan[0], Is.Not.Null);
            }
            else if (item.ComponentType == "DEBIAN")
            {
                Assert.AreEqual(1, displayPackagesInfo.JfrogNotFoundPackagesDebian.Count);
                Assert.That(displayPackagesInfo.JfrogNotFoundPackagesDebian[0], Is.Not.Null);
            }
        }

        [Test]
        [TestCase("NPM")]
        [TestCase("NUGET")]
        [TestCase("MAVEN")]
        [TestCase("PYTHON")]
        [TestCase("CONAN")]
        [TestCase("DEBIAN")]
        public async Task JfrogFoundPackagesAsync_CoversAllScenarios(string compType)
        {
            // Arrange
            var item = new ComponentsToArtifactory();
            item.ComponentType = compType;
            var displayPackagesInfo = new DisplayPackagesInfo();
            displayPackagesInfo.JfrogFoundPackagesNpm = new List<ComponentsToArtifactory>();
            displayPackagesInfo.JfrogFoundPackagesNuget = new List<ComponentsToArtifactory>();
            displayPackagesInfo.JfrogFoundPackagesMaven = new List<ComponentsToArtifactory>();
            displayPackagesInfo.JfrogFoundPackagesPython = new List<ComponentsToArtifactory>();
            displayPackagesInfo.JfrogFoundPackagesConan = new List<ComponentsToArtifactory>();
            displayPackagesInfo.JfrogFoundPackagesDebian = new List<ComponentsToArtifactory>();
            var operationType = "operationType";
            var responseMessage = new HttpResponseMessage();
            var dryRunSuffix = "dryRunSuffix";

            // Act
            await PackageUploadHelper.JfrogFoundPackagesAsync(item, displayPackagesInfo, operationType, responseMessage, dryRunSuffix);

            // Assert
            if (item.ComponentType == "NPM")
            {
                Assert.AreEqual(1, displayPackagesInfo.JfrogFoundPackagesNpm.Count);
                Assert.That(displayPackagesInfo.JfrogFoundPackagesNpm[0], Is.Not.Null);
            }
            else if (item.ComponentType == "NUGET")
            {
                Assert.AreEqual(1, displayPackagesInfo.JfrogFoundPackagesNuget.Count);
                Assert.That(displayPackagesInfo.JfrogFoundPackagesNuget[0], Is.Not.Null);
            }
            else if (item.ComponentType == "MAVEN")
            {
                Assert.AreEqual(1, displayPackagesInfo.JfrogFoundPackagesMaven.Count);
                Assert.That(displayPackagesInfo.JfrogFoundPackagesMaven[0], Is.Not.Null);
            }
            else if (item.ComponentType == "PYTHON")
            {
                Assert.AreEqual(1, displayPackagesInfo.JfrogFoundPackagesPython.Count);
                Assert.That(displayPackagesInfo.JfrogFoundPackagesPython[0], Is.Not.Null);
            }
            else if (item.ComponentType == "CONAN")
            {
                Assert.AreEqual(1, displayPackagesInfo.JfrogFoundPackagesConan.Count);
                Assert.That(displayPackagesInfo.JfrogFoundPackagesConan[0], Is.Not.Null);
            }
            else if (item.ComponentType == "DEBIAN")
            {
                Assert.AreEqual(1, displayPackagesInfo.JfrogFoundPackagesDebian.Count);
                Assert.That(displayPackagesInfo.JfrogFoundPackagesDebian[0], Is.Not.Null);
            }
        }

        [Test]
        public void GetNotApprovedDebianPackages_CoversAllScenarios()
        {
            // Arrange
            var unknownPackages = new List<ComponentsToArtifactory>
            {
                new ComponentsToArtifactory { Name = "Package1", Version = "1.0.0" },
                new ComponentsToArtifactory { Name = "Package2", Version = "2.0.0" }
            };
            var projectResponse = new ProjectResponse();
            var fileOperationsMock = new Mock<IFileOperations>();
            var filepath = "..";
            var filename = "\\testFileName.json";

            // Act
            PackageUploadHelper.GetNotApprovedDebianPackages(unknownPackages, projectResponse, fileOperationsMock.Object, filepath, filename);

            // Assert
            // Add your assertions here
            Assert.That(unknownPackages.Count, Is.EqualTo(2));
        }

        [Test]
        public async Task GetJfrogRepoInfoForAllTypePackages_GivenDestRepoNames_ReturnsAqlResultList()
        {
            // Arrange
            var destRepoNames = new List<string> { "repo1", "repo2", "repo3" };
            var expectedAqlResultList = new List<AqlResult>
            {
                new AqlResult { Name = "result1" },
                new AqlResult { Name = "result2" },
                new AqlResult { Name = "result3" }
            };

            var jFrogServiceMock = new Mock<IJFrogService>();
            jFrogServiceMock.Setup(service => service.GetInternalComponentDataByRepo(It.IsAny<string>()))
                            .ReturnsAsync(expectedAqlResultList);
            PackageUploadHelper.jFrogService = jFrogServiceMock.Object;

            // Act
            var actualAqlResultList = await PackageUploadHelper.GetJfrogRepoInfoForAllTypePackages(destRepoNames);


            // Assert
            Assert.That(actualAqlResultList.Count, Is.GreaterThan(2));
        }

        [Test]
        [TestCase("NPM", "?to=/destination-repo//")]
        [TestCase("NUGET", "source-repo/package-name.1.0.0.nupkg?to=/destination-repo/package-name.1.0.0.nupkg")]
        [TestCase("MAVEN", "source-repo/package-name/1.0.0?to=/destination-repo/package-name/1.0.0")]
        [TestCase("CONAN", "source-repo/?to=/destination-repo/")]
        [TestCase("PYTHON", "?to=/destination-repo/")]
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
            var actualUrl = PackageUploadHelper.GetCopyURL(component);

            // Assert
            Assert.AreEqual(expectedUrl, actualUrl);
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
            var actualUrl = PackageUploadHelper.GetCopyURL(component);

            // Assert
            Assert.AreEqual(string.Empty, actualUrl);
        }


        [Test]
        [TestCase("NPM", "?to=/destination-repo//")]
        [TestCase("NUGET", "source-repo/package-name.1.0.0.nupkg?to=/destination-repo/package-name.1.0.0.nupkg")]
        [TestCase("MAVEN", "source-repo/package-name/1.0.0?to=/destination-repo/package-name/1.0.0")]
        [TestCase("CONAN", "source-repo/?to=/destination-repo/")]
        [TestCase("PYTHON", "?to=/destination-repo/")]
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
            var result = PackageUploadHelper.GetMoveURL(component);

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
            var result = PackageUploadHelper.GetMoveURL(component);

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

            PackageUploadHelper.jFrogService = jFrogServiceMock.Object;


            // Act
            var result = await PackageUploadHelper.GetSrcRepoDetailsForComponent(item);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("npm-repo", result.Repo);
            Assert.AreEqual("path/to/package", result.Path);
        }
    }
}
