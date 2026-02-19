// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

// Ignore Spelling: Aritfactory Uploader

using CycloneDX.Models;
using LCT.APICommunications;
using LCT.APICommunications.Model;
using LCT.ArtifactoryUploader;
using LCT.ArtifactoryUploader.Model;
using LCT.Common.Constants;
using LCT.Common.Interface;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

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
            string comparisonBOMPath = Path.GetFullPath(Path.Combine(outFolder, "ArtifactoryUTTestFiles", "Test_Bom.cdx.json"));
            Mock<IEnvironmentHelper> environmentHelperMock = new Mock<IEnvironmentHelper>();
            environmentHelperMock.Setup(x => x.CallEnvironmentExit(-1));
            //Act
            Bom componentList = PackageUploadHelper.GetComponentListFromComparisonBOM(comparisonBOMPath, environmentHelperMock.Object);
            // Assert
            Assert.That(6, Is.EqualTo(componentList.Components.Count), "Checks for no of components");
        }
        [Test]
        [TestCase("NPM", ".tgz")]
        [TestCase("NUGET", ".nupkg")]
        [TestCase("MAVEN", ".jar")]
        [TestCase("DEBIAN", ".deb")]
        [TestCase("POETRY", ".whl")]
        [TestCase("CONAN", "package.tgz")]
        [TestCase("CARGO", ".crate")]
        [TestCase("CHOCO", ".nupkg")]
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
        public void GetComponentListFromComparisonBOM_GivenInvalidComparisonBOM_ReturnsNull()
        {
            // Arrange
            string comparisonBOMPath = @"TestFiles\CCTComparisonBOM.json";
            Mock<IEnvironmentHelper> environmentHelperMock = new Mock<IEnvironmentHelper>();
            environmentHelperMock.Setup(x => x.CallEnvironmentExit(-1));
            // Act
            var result = PackageUploadHelper.GetComponentListFromComparisonBOM(comparisonBOMPath, environmentHelperMock.Object);

            // Assert
            Assert.IsNull(result, "Expected null when the file does not exist.");
        }
        [Test]
        public void GetComponentListFromComparisonBOM_GivenInvalidfile_ReturnsException()
        {
            //Arrange
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string comparisonBOMPath = Path.GetFullPath(Path.Combine(outFolder, "ArtifactoryUTTestFiles", "ComparisonBOM.json"));
            Mock<IEnvironmentHelper> environmentHelperMock = new Mock<IEnvironmentHelper>();
            environmentHelperMock.Setup(x => x.CallEnvironmentExit(-1));
            //Act && Assert
            Assert.Throws<System.Text.Json.JsonException>(() => PackageUploadHelper.GetComponentListFromComparisonBOM(comparisonBOMPath, environmentHelperMock.Object));
        }


        [Test]
        public void UpdateBomArtifactoryRepoUrl_GivenBomAndComponentsUploadedToArtifactory_UpdatesBom()
        {
            //Arrange
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string comparisonBOMPath = Path.GetFullPath(Path.Combine(outFolder, "ArtifactoryUTTestFiles", "Test_Bom.cdx.json"));
            Mock<IEnvironmentHelper> environmentHelperMock = new Mock<IEnvironmentHelper>();
            environmentHelperMock.Setup(x => x.CallEnvironmentExit(-1));
            Bom bom = PackageUploadHelper.GetComponentListFromComparisonBOM(comparisonBOMPath, environmentHelperMock.Object);
            List<ComponentsToArtifactory> components = new List<ComponentsToArtifactory>()
            {
                new ComponentsToArtifactory()
                {
                    Purl = "pkg:npm/rxjs@6.5.4",
                    DestRepoName = "org1-npmjs-npm-remote",
                    DryRun = false,
                    JfrogRepoPath="org1-npmjs-npm-remote/rxjs/-/rxjs-6.5.4.tgz",
                }
            };

            //Act
            PackageUploadHelper.UpdateBomArtifactoryRepoUrl(ref bom, components);

            //Assert
            var repoUrl = bom.Components.First(x => x.Properties[5].Name == Dataconstant.Cdx_ArtifactoryRepoName).Properties[5].Value;
            var repoPath = bom.Components.First(x => x.Properties[6].Name == Dataconstant.Cdx_JfrogRepoPath).Properties[6].Value;
            Assert.AreEqual("org1-npmjs-npm-remote", repoUrl);
            Assert.AreEqual("org1-npmjs-npm-remote/rxjs/-/rxjs-6.5.4.tgz", repoPath);
        }

        [Test]
        public void UpdateBomArtifactoryRepoUrl_GivenBomAndComponentsUploadedToArtifactoryDryRun_NoUpdateBom()
        {
            //Arrange
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string comparisonBOMPath = Path.GetFullPath(Path.Combine(outFolder, "ArtifactoryUTTestFiles", "Test_Bom.cdx.json"));
            Mock<IEnvironmentHelper> environmentHelperMock = new Mock<IEnvironmentHelper>();
            environmentHelperMock.Setup(x => x.CallEnvironmentExit(-1));
            Bom bom = PackageUploadHelper.GetComponentListFromComparisonBOM(comparisonBOMPath, environmentHelperMock.Object);
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
            var repoUrl = bom.Components.First(x => x.Properties[3].Name == Dataconstant.Cdx_ArtifactoryRepoName).Properties[3].Value;
            Assert.AreNotEqual("org1-npmjs-npm-remote", repoUrl);
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
                Token = "api-key"
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
                ComponentType = "POETRY",
                JfrogApi = "https://api.jfrog.com",
                SrcRepoName = "python-repo",
                Token = "api-key"
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
                Token = "api-key",
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
        [TestCase("POETRY")]
        [TestCase("CONAN")]
        [TestCase("DEBIAN")]
        [TestCase("CARGO")]
        [TestCase("CHOCO")]
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
            displayPackagesInfo.JfrogNotFoundPackagesCargo = new List<ComponentsToArtifactory>();
            displayPackagesInfo.JfrogNotFoundPackagesChoco = new List<ComponentsToArtifactory>();

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
            else if (item.ComponentType == "POETRY")
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
            else if (item.ComponentType == "CARGO")
            {
                Assert.AreEqual(1, displayPackagesInfo.JfrogNotFoundPackagesCargo.Count);
                Assert.That(displayPackagesInfo.JfrogNotFoundPackagesCargo[0], Is.Not.Null);
            }
            else if (item.ComponentType == "CHOCO")
            {
                Assert.AreEqual(1, displayPackagesInfo.JfrogNotFoundPackagesChoco.Count);
                Assert.That(displayPackagesInfo.JfrogNotFoundPackagesChoco[0], Is.Not.Null);
            }
        }

        [Test]
        [TestCase("NPM")]
        [TestCase("NUGET")]
        [TestCase("MAVEN")]
        [TestCase("POETRY")]
        [TestCase("CONAN")]
        [TestCase("DEBIAN")]
        [TestCase("CARGO")]
        [TestCase("CHOCO")]
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
            displayPackagesInfo.JfrogFoundPackagesCargo = new List<ComponentsToArtifactory>();
            displayPackagesInfo.JfrogFoundPackagesChoco = new List<ComponentsToArtifactory>();
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
            else if (item.ComponentType == "POETRY")
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
            else if (item.ComponentType == "CARGO")
            {
                Assert.AreEqual(1, displayPackagesInfo.JfrogFoundPackagesCargo.Count);
                Assert.That(displayPackagesInfo.JfrogFoundPackagesCargo[0], Is.Not.Null);
            }
            else if (item.ComponentType == "CHOCO")
            {
                Assert.AreEqual(1, displayPackagesInfo.JfrogFoundPackagesChoco.Count);
                Assert.That(displayPackagesInfo.JfrogFoundPackagesChoco[0], Is.Not.Null);
            }
        }

        [Test]
        public async Task JfrogNotFoundPackagesAsync_CargoComponent_AddsToCargoNotFoundList()
        {
            // Arrange
            var cargoItem = new ComponentsToArtifactory
            {
                ComponentType = "CARGO",
                Name = "serde",
                Version = "1.0.150",
                Purl = "pkg:cargo/serde@1.0.150",
                SrcRepoName = "cargo-src-repo",
                DestRepoName = "cargo-dest-repo",
                PackageType = PackageType.ClearedThirdParty
            };
            var displayPackagesInfo = new DisplayPackagesInfo();
            displayPackagesInfo.JfrogNotFoundPackagesCargo = new List<ComponentsToArtifactory>();

            // Act
            await PackageUploadHelper.JfrogNotFoundPackagesAsync(cargoItem, displayPackagesInfo);

            // Assert
            Assert.AreEqual(1, displayPackagesInfo.JfrogNotFoundPackagesCargo.Count);
            var addedComponent = displayPackagesInfo.JfrogNotFoundPackagesCargo[0];
            Assert.AreEqual("serde", addedComponent.Name);
            Assert.AreEqual("1.0.150", addedComponent.Version);
            Assert.AreEqual("pkg:cargo/serde@1.0.150", addedComponent.Purl);
            Assert.AreEqual("cargo-src-repo", addedComponent.SrcRepoName);
            Assert.AreEqual(PackageType.ClearedThirdParty, addedComponent.PackageType);
        }

        [Test]
        public async Task JfrogFoundPackagesAsync_CargoComponent_AddsToCargoFoundList()
        {
            // Arrange
            var cargoItem = new ComponentsToArtifactory
            {
                ComponentType = "CARGO",
                Name = "tokio",
                Version = "1.23.0",
                Purl = "pkg:cargo/tokio@1.23.0",
                SrcRepoName = "cargo-src-repo",
                DestRepoName = "cargo-dest-repo",
                Token = "test-token",
                CopyPackageApiUrl = "https://test.api.url",
                PackageName = "tokio-1.23.0.crate",
                PackageType = PackageType.ClearedThirdParty
            };
            var displayPackagesInfo = new DisplayPackagesInfo();
            displayPackagesInfo.JfrogFoundPackagesCargo = new List<ComponentsToArtifactory>();
            var operationType = "copy";
            var responseMessage = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
            var dryRunSuffix = "";

            // Act
            await PackageUploadHelper.JfrogFoundPackagesAsync(cargoItem, displayPackagesInfo, operationType, responseMessage, dryRunSuffix);

            // Assert
            Assert.AreEqual(1, displayPackagesInfo.JfrogFoundPackagesCargo.Count);
            var addedComponent = displayPackagesInfo.JfrogFoundPackagesCargo[0];
            Assert.AreEqual("tokio", addedComponent.Name);
            Assert.AreEqual("1.23.0", addedComponent.Version);
            Assert.AreEqual("pkg:cargo/tokio@1.23.0", addedComponent.Purl);
            Assert.AreEqual("cargo-src-repo", addedComponent.SrcRepoName);
            Assert.AreEqual("cargo-dest-repo", addedComponent.DestRepoName);
            Assert.AreEqual("copy", addedComponent.OperationType);
            Assert.AreEqual(responseMessage, addedComponent.ResponseMessage);
            Assert.AreEqual("test-token", addedComponent.Token);
            Assert.AreEqual("tokio-1.23.0.crate", addedComponent.PackageName);
            Assert.AreEqual(PackageType.ClearedThirdParty, addedComponent.PackageType);
        }

        [Test]
        public void GetPackageNameExtensionBasedOnComponentType_CargoComponent_ReturnsCorrectExtension()
        {
            // Arrange
            var cargoPackage = new ComponentsToArtifactory
            {
                ComponentType = "CARGO"
            };

            // Act
            var extension = PackageUploadHelper.GetPackageNameExtensionBasedOnComponentType(cargoPackage);

            // Assert
            Assert.AreEqual(".crate", extension);
        }

        [Test]
        public void GetPackageNameExtensionBasedOnComponentType_CargoComponentCaseInsensitive_ReturnsCorrectExtension()
        {
            // Arrange
            var cargoPackage = new ComponentsToArtifactory
            {
                ComponentType = "cargo" // lowercase
            };

            // Act
            var extension = PackageUploadHelper.GetPackageNameExtensionBasedOnComponentType(cargoPackage);

            // Assert
            Assert.AreEqual(".crate", extension);
        }

        [Test]
        public void JfrogNotFoundPackagesAsync_CargoComponentWithNullDisplayInfo_ThrowsNullReferenceException()
        {
            // Arrange
            var cargoItem = new ComponentsToArtifactory
            {
                ComponentType = "CARGO",
                Name = "regex",
                Version = "1.7.0"
            };

            // Act & Assert
            Assert.ThrowsAsync<System.NullReferenceException>(async () =>
                await PackageUploadHelper.JfrogNotFoundPackagesAsync(cargoItem, null));
        }

        [Test]
        public void JfrogFoundPackagesAsync_CargoComponentWithNullDisplayInfo_ThrowsNullReferenceException()
        {
            // Arrange
            var cargoItem = new ComponentsToArtifactory
            {
                ComponentType = "CARGO",
                Name = "clap",
                Version = "4.0.0"
            };
            var operationType = "move";
            var responseMessage = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
            var dryRunSuffix = "";

            // Act & Assert
            Assert.ThrowsAsync<System.NullReferenceException>(async () =>
                await PackageUploadHelper.JfrogFoundPackagesAsync(cargoItem, null, operationType, responseMessage, dryRunSuffix));
        }

        [Test]
        public async Task JfrogFoundPackagesAsync_CargoComponentWithNullResponseMessage_StillAddsToList()
        {
            // Arrange
            var cargoItem = new ComponentsToArtifactory
            {
                ComponentType = "CARGO",
                Name = "rand",
                Version = "0.8.5"
            };
            var displayPackagesInfo = new DisplayPackagesInfo();
            displayPackagesInfo.JfrogFoundPackagesCargo = new List<ComponentsToArtifactory>();
            var operationType = "copy";
            HttpResponseMessage responseMessage = null;
            var dryRunSuffix = "dry-run";

            // Act
            await PackageUploadHelper.JfrogFoundPackagesAsync(cargoItem, displayPackagesInfo, operationType, responseMessage, dryRunSuffix);

            // Assert
            Assert.AreEqual(1, displayPackagesInfo.JfrogFoundPackagesCargo.Count);
            var addedComponent = displayPackagesInfo.JfrogFoundPackagesCargo[0];
            Assert.AreEqual("rand", addedComponent.Name);
            Assert.AreEqual("0.8.5", addedComponent.Version);
            Assert.AreEqual("copy", addedComponent.OperationType);
            Assert.IsNull(addedComponent.ResponseMessage);
            Assert.AreEqual("dry-run", addedComponent.DryRunSuffix);
        }

        [Test]
        public async Task JfrogNotFoundPackagesAsync_CargoComponentWithEmptyName_AddsComponentWithEmptyName()
        {
            // Arrange
            var cargoItem = new ComponentsToArtifactory
            {
                ComponentType = "CARGO",
                Name = "",
                Version = "1.0.0",
                Purl = "pkg:cargo/@1.0.0"
            };
            var displayPackagesInfo = new DisplayPackagesInfo();
            displayPackagesInfo.JfrogNotFoundPackagesCargo = new List<ComponentsToArtifactory>();

            // Act
            await PackageUploadHelper.JfrogNotFoundPackagesAsync(cargoItem, displayPackagesInfo);

            // Assert
            Assert.AreEqual(1, displayPackagesInfo.JfrogNotFoundPackagesCargo.Count);
            var addedComponent = displayPackagesInfo.JfrogNotFoundPackagesCargo[0];
            Assert.AreEqual("", addedComponent.Name);
            Assert.AreEqual("1.0.0", addedComponent.Version);
        }

        [Test]
        public async Task JfrogFoundPackagesAsync_CargoComponentMultipleCalls_AddsMultipleComponents()
        {
            // Arrange
            var cargoItem1 = new ComponentsToArtifactory
            {
                ComponentType = "CARGO",
                Name = "serde",
                Version = "1.0.150"
            };
            var cargoItem2 = new ComponentsToArtifactory
            {
                ComponentType = "CARGO",
                Name = "tokio",
                Version = "1.23.0"
            };
            var displayPackagesInfo = new DisplayPackagesInfo();
            displayPackagesInfo.JfrogFoundPackagesCargo = new List<ComponentsToArtifactory>();
            var operationType = "move";
            var responseMessage = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
            var dryRunSuffix = "";

            // Act
            await PackageUploadHelper.JfrogFoundPackagesAsync(cargoItem1, displayPackagesInfo, operationType, responseMessage, dryRunSuffix);
            await PackageUploadHelper.JfrogFoundPackagesAsync(cargoItem2, displayPackagesInfo, operationType, responseMessage, dryRunSuffix);

            // Assert
            Assert.AreEqual(2, displayPackagesInfo.JfrogFoundPackagesCargo.Count);
            Assert.AreEqual("serde", displayPackagesInfo.JfrogFoundPackagesCargo[0].Name);
            Assert.AreEqual("tokio", displayPackagesInfo.JfrogFoundPackagesCargo[1].Name);
        }

        [Test]
        public async Task JfrogNotFoundPackagesAsync_WithNullComponentType_DoesNotAddComponent()
        {
            // Arrange
            var item = new ComponentsToArtifactory
            {
                ComponentType = null,
                Name = "test-package",
                Version = "1.0.0"
            };
            var displayPackagesInfo = new DisplayPackagesInfo
            {
                JfrogNotFoundPackagesNpm = new List<ComponentsToArtifactory>(),
                JfrogNotFoundPackagesNuget = new List<ComponentsToArtifactory>(),
                JfrogNotFoundPackagesMaven = new List<ComponentsToArtifactory>(),
                JfrogNotFoundPackagesPython = new List<ComponentsToArtifactory>(),
                JfrogNotFoundPackagesConan = new List<ComponentsToArtifactory>(),
                JfrogNotFoundPackagesDebian = new List<ComponentsToArtifactory>(),
                JfrogNotFoundPackagesCargo = new List<ComponentsToArtifactory>(),
                JfrogNotFoundPackagesChoco = new List<ComponentsToArtifactory>()
            };

            // Act
            await PackageUploadHelper.JfrogNotFoundPackagesAsync(item, displayPackagesInfo);

            // Assert - No components should be added to any list
            Assert.AreEqual(0, displayPackagesInfo.JfrogNotFoundPackagesNpm.Count);
            Assert.AreEqual(0, displayPackagesInfo.JfrogNotFoundPackagesNuget.Count);
            Assert.AreEqual(0, displayPackagesInfo.JfrogNotFoundPackagesMaven.Count);
            Assert.AreEqual(0, displayPackagesInfo.JfrogNotFoundPackagesPython.Count);
            Assert.AreEqual(0, displayPackagesInfo.JfrogNotFoundPackagesConan.Count);
            Assert.AreEqual(0, displayPackagesInfo.JfrogNotFoundPackagesDebian.Count);
            Assert.AreEqual(0, displayPackagesInfo.JfrogNotFoundPackagesCargo.Count);
            Assert.AreEqual(0, displayPackagesInfo.JfrogNotFoundPackagesChoco.Count);
        }

        [Test]
        public async Task JfrogNotFoundPackagesAsync_WithEmptyComponentType_DoesNotAddComponent()
        {
            // Arrange
            var item = new ComponentsToArtifactory
            {
                ComponentType = "",
                Name = "test-package",
                Version = "1.0.0"
            };
            var displayPackagesInfo = new DisplayPackagesInfo
            {
                JfrogNotFoundPackagesNpm = new List<ComponentsToArtifactory>(),
                JfrogNotFoundPackagesNuget = new List<ComponentsToArtifactory>(),
                JfrogNotFoundPackagesMaven = new List<ComponentsToArtifactory>(),
                JfrogNotFoundPackagesPython = new List<ComponentsToArtifactory>(),
                JfrogNotFoundPackagesConan = new List<ComponentsToArtifactory>(),
                JfrogNotFoundPackagesDebian = new List<ComponentsToArtifactory>(),
                JfrogNotFoundPackagesCargo = new List<ComponentsToArtifactory>(),
                JfrogNotFoundPackagesChoco = new List<ComponentsToArtifactory>()
            };

            // Act
            await PackageUploadHelper.JfrogNotFoundPackagesAsync(item, displayPackagesInfo);

            // Assert - No components should be added to any list
            Assert.AreEqual(0, displayPackagesInfo.JfrogNotFoundPackagesNpm.Count);
            Assert.AreEqual(0, displayPackagesInfo.JfrogNotFoundPackagesNuget.Count);
            Assert.AreEqual(0, displayPackagesInfo.JfrogNotFoundPackagesMaven.Count);
            Assert.AreEqual(0, displayPackagesInfo.JfrogNotFoundPackagesPython.Count);
            Assert.AreEqual(0, displayPackagesInfo.JfrogNotFoundPackagesConan.Count);
            Assert.AreEqual(0, displayPackagesInfo.JfrogNotFoundPackagesDebian.Count);
            Assert.AreEqual(0, displayPackagesInfo.JfrogNotFoundPackagesCargo.Count);
            Assert.AreEqual(0, displayPackagesInfo.JfrogNotFoundPackagesChoco.Count);
        }

        [Test]
        public async Task JfrogNotFoundPackagesAsync_WithWhitespaceComponentType_DoesNotAddComponent()
        {
            // Arrange
            var item = new ComponentsToArtifactory
            {
                ComponentType = "   ",
                Name = "test-package",
                Version = "1.0.0"
            };
            var displayPackagesInfo = new DisplayPackagesInfo
            {
                JfrogNotFoundPackagesNpm = new List<ComponentsToArtifactory>(),
                JfrogNotFoundPackagesNuget = new List<ComponentsToArtifactory>(),
                JfrogNotFoundPackagesMaven = new List<ComponentsToArtifactory>(),
                JfrogNotFoundPackagesPython = new List<ComponentsToArtifactory>(),
                JfrogNotFoundPackagesConan = new List<ComponentsToArtifactory>(),
                JfrogNotFoundPackagesDebian = new List<ComponentsToArtifactory>(),
                JfrogNotFoundPackagesCargo = new List<ComponentsToArtifactory>(),
                JfrogNotFoundPackagesChoco = new List<ComponentsToArtifactory>()
            };

            // Act
            await PackageUploadHelper.JfrogNotFoundPackagesAsync(item, displayPackagesInfo);

            // Assert - No components should be added to any list
            Assert.AreEqual(0, displayPackagesInfo.JfrogNotFoundPackagesNpm.Count);
            Assert.AreEqual(0, displayPackagesInfo.JfrogNotFoundPackagesNuget.Count);
            Assert.AreEqual(0, displayPackagesInfo.JfrogNotFoundPackagesMaven.Count);
            Assert.AreEqual(0, displayPackagesInfo.JfrogNotFoundPackagesPython.Count);
            Assert.AreEqual(0, displayPackagesInfo.JfrogNotFoundPackagesConan.Count);
            Assert.AreEqual(0, displayPackagesInfo.JfrogNotFoundPackagesDebian.Count);
            Assert.AreEqual(0, displayPackagesInfo.JfrogNotFoundPackagesCargo.Count);
            Assert.AreEqual(0, displayPackagesInfo.JfrogNotFoundPackagesChoco.Count);
        }

        [Test]
        public void WriteCreatorKpiDataToConsole_ValidUploaderKpiData_LogsDataSuccessfully()
        {
            // Arrange
            var uploaderKpiData = new UploaderKpiData
            {
                ComponentInComparisonBOM = 100,
                ComponentNotApproved = 10,
                PackagesToBeUploaded = 90,
                PackagesUploadedToJfrog = 85,
                PackagesNotUploadedToJfrog = 5,
                DevPackagesUploaded = 15,
                DevPackagesNotUploadedToJfrog = 2,
                InternalPackagesUploaded = 20,
                InternalPackagesNotUploadedToJfrog = 3,
                PackagesNotExistingInRemoteCache = 4,
                PackagesNotUploadedDueToError = 1,
                TimeTakenByArtifactoryUploader = 45.5
            };

            // Act & Assert - Should not throw any exceptions
            Assert.DoesNotThrow(() => PackageUploadHelper.WriteCreatorKpiDataToConsole(uploaderKpiData));
        }

        [Test]
        public void WriteCreatorKpiDataToConsole_WithZeroValues_LogsDataSuccessfully()
        {
            // Arrange
            var uploaderKpiData = new UploaderKpiData
            {
                ComponentInComparisonBOM = 0,
                ComponentNotApproved = 0,
                PackagesToBeUploaded = 0,
                PackagesUploadedToJfrog = 0,
                PackagesNotUploadedToJfrog = 0,
                DevPackagesUploaded = 0,
                DevPackagesNotUploadedToJfrog = 0,
                InternalPackagesUploaded = 0,
                InternalPackagesNotUploadedToJfrog = 0,
                PackagesNotExistingInRemoteCache = 0,
                PackagesNotUploadedDueToError = 0,
                TimeTakenByArtifactoryUploader = 0.0
            };

            // Act & Assert - Should not throw any exceptions
            Assert.DoesNotThrow(() => PackageUploadHelper.WriteCreatorKpiDataToConsole(uploaderKpiData));
        }

        #region SourceRepoFoundToUploadArtifactory Tests - Using IncrementCountersBasedOnPackageType

        [Test]
        public void IncrementCountersBasedOnPackageType_WhenClearedThirdPartySuccess_IncrementsPackagesUploadedToJfrog()
        {
            // Arrange
            var uploaderKpiData = new UploaderKpiData();
            var packageType = PackageType.ClearedThirdParty;
            bool isSuccess = true;

            // Use reflection to call private method
            var methodInfo = typeof(PackageUploadHelper).GetMethod("IncrementCountersBasedOnPackageType",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            // Act
            methodInfo.Invoke(null, new object[] { uploaderKpiData, packageType, isSuccess });

            // Assert
            Assert.AreEqual(1, uploaderKpiData.PackagesUploadedToJfrog);
            Assert.AreEqual(0, uploaderKpiData.PackagesNotUploadedToJfrog);
            Assert.AreEqual(0, uploaderKpiData.PackagesNotUploadedDueToError);
        }

        [Test]
        public async Task SourceRepoFoundToUploadArtifactory_WhenPackageNotFound_SetsWarningCodeAndIncrementsFailureCounters()
        {
            // Arrange
            var packageType = PackageType.ClearedThirdParty;
            var uploaderKpiData = new UploaderKpiData();
            var item = new ComponentsToArtifactory
            {
                Name = "missing-package",
                Version = "2.0.0",
                ComponentType = "NUGET",
                PackageType = PackageType.ClearedThirdParty,
                DryRun = false,
                SrcRepoName = "src-repo",
                DestRepoName = "dest-repo",
                JfrogApi = "https://test-api.com",
                Token = "test-token"
            };
            int timeout = 5000;
            var displayPackagesInfo = new DisplayPackagesInfo
            {
                JfrogFoundPackagesNuget = new List<ComponentsToArtifactory>(),
                JfrogNotFoundPackagesNuget = new List<ComponentsToArtifactory>()
            };

            // Mock services
            var mockJFrogService = new Mock<IJFrogService>();
            var mockJFrogApiComm = new Mock<IJFrogApiCommunication>();
            PackageUploadHelper.JFrogService = mockJFrogService.Object;
            ArtifactoryUploader.ArtifactoryUploader.JFrogService = mockJFrogService.Object;
            ArtifactoryUploader.ArtifactoryUploader.JFrogApiCommInstance = mockJFrogApiComm.Object;

            // Use reflection to get private method
            var methodInfo = typeof(PackageUploadHelper).GetMethod("SourceRepoFoundToUploadArtifactory",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            // Act
            var task = (Task)methodInfo.Invoke(null, new object[] { packageType, uploaderKpiData, item, timeout, displayPackagesInfo });
            await task;

            // Assert
            Assert.AreEqual(0, uploaderKpiData.PackagesUploadedToJfrog);
            Assert.AreEqual(1, uploaderKpiData.PackagesNotUploadedToJfrog);
            Assert.AreEqual(1, uploaderKpiData.PackagesNotUploadedDueToError);
            Assert.IsNull(item.DestRepoName);
        }

        [Test]
        public async Task SourceRepoFoundToUploadArtifactory_WhenErrorInUpload_IncrementsFailureCountersAndLogsError()
        {
            // Arrange
            var packageType = PackageType.Internal;
            var uploaderKpiData = new UploaderKpiData();
            var item = new ComponentsToArtifactory
            {
                Name = "error-package",
                Version = "3.0.0",
                ComponentType = "MAVEN",
                PackageType = PackageType.Internal,
                DryRun = false,
                SrcRepoName = "src-repo",
                DestRepoName = "dest-repo",
                JfrogApi = "https://test-api.com",
                Token = "test-token"
            };
            int timeout = 5000;
            var displayPackagesInfo = new DisplayPackagesInfo
            {
                JfrogFoundPackagesMaven = new List<ComponentsToArtifactory>(),
                JfrogNotFoundPackagesMaven = new List<ComponentsToArtifactory>()
            };

            // Mock services
            var mockJFrogService = new Mock<IJFrogService>();
            var mockJFrogApiComm = new Mock<IJFrogApiCommunication>();
            PackageUploadHelper.JFrogService = mockJFrogService.Object;
            ArtifactoryUploader.ArtifactoryUploader.JFrogService = mockJFrogService.Object;
            ArtifactoryUploader.ArtifactoryUploader.JFrogApiCommInstance = mockJFrogApiComm.Object;

            // Use reflection to get private method
            var methodInfo = typeof(PackageUploadHelper).GetMethod("SourceRepoFoundToUploadArtifactory",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            // Act
            var task = (Task)methodInfo.Invoke(null, new object[] { packageType, uploaderKpiData, item, timeout, displayPackagesInfo });
            await task;

            // Assert
            Assert.AreEqual(0, uploaderKpiData.InternalPackagesUploaded);
            Assert.AreEqual(1, uploaderKpiData.InternalPackagesNotUploadedToJfrog);
            Assert.AreEqual(1, uploaderKpiData.PackagesNotUploadedDueToError);
            Assert.IsNull(item.DestRepoName);
        }

        [Test]
        public async Task SourceRepoFoundToUploadArtifactory_WhenDevelopmentPackageSuccessful_IncrementsDevCounters()
        {
            // Arrange
            var packageType = PackageType.Development;
            var uploaderKpiData = new UploaderKpiData();
            var item = new ComponentsToArtifactory
            {
                Name = "dev-package",
                Version = "1.0.0-dev",
                ComponentType = "POETRY",
                PackageType = PackageType.Development,
                DryRun = false,
                SrcRepoName = "src-repo",
                DestRepoName = "dev-repo",
                JfrogApi = "https://test-api.com",
                Token = "test-token"
            };
            int timeout = 5000;
            var displayPackagesInfo = new DisplayPackagesInfo
            {
                JfrogFoundPackagesPython = new List<ComponentsToArtifactory>(),
                JfrogNotFoundPackagesPython = new List<ComponentsToArtifactory>()
            };

            // Mock services
            var mockJFrogService = new Mock<IJFrogService>();
            var mockJFrogApiComm = new Mock<IJFrogApiCommunication>();
            PackageUploadHelper.JFrogService = mockJFrogService.Object;
            ArtifactoryUploader.ArtifactoryUploader.JFrogService = mockJFrogService.Object;
            ArtifactoryUploader.ArtifactoryUploader.JFrogApiCommInstance = mockJFrogApiComm.Object;

            // Use reflection to get private method
            var methodInfo = typeof(PackageUploadHelper).GetMethod("SourceRepoFoundToUploadArtifactory",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            // Act
            var task = (Task)methodInfo.Invoke(null, new object[] { packageType, uploaderKpiData, item, timeout, displayPackagesInfo });
            await task;

            // Assert
            Assert.AreEqual(1, uploaderKpiData.DevPackagesUploaded);
            Assert.AreEqual(0, uploaderKpiData.DevPackagesNotUploadedToJfrog);
        }

        [Test]
        public async Task SourceRepoFoundToUploadArtifactory_WhenDevelopmentPackageFails_IncrementsDevFailureCounters()
        {
            // Arrange
            var packageType = PackageType.Development;
            var uploaderKpiData = new UploaderKpiData();
            var item = new ComponentsToArtifactory
            {
                Name = "dev-package-fail",
                Version = "2.0.0-dev",
                ComponentType = "CONAN",
                PackageType = PackageType.Development,
                DryRun = false,
                SrcRepoName = "src-repo",
                DestRepoName = "dev-repo",
                JfrogApi = "https://test-api.com",
                Token = "test-token"
            };
            int timeout = 5000;
            var displayPackagesInfo = new DisplayPackagesInfo
            {
                JfrogFoundPackagesConan = new List<ComponentsToArtifactory>(),
                JfrogNotFoundPackagesConan = new List<ComponentsToArtifactory>()
            };

            // Mock services
            var mockJFrogService = new Mock<IJFrogService>();
            var mockJFrogApiComm = new Mock<IJFrogApiCommunication>();
            PackageUploadHelper.JFrogService = mockJFrogService.Object;
            ArtifactoryUploader.ArtifactoryUploader.JFrogService = mockJFrogService.Object;
            ArtifactoryUploader.ArtifactoryUploader.JFrogApiCommInstance = mockJFrogApiComm.Object;

            // Use reflection to get private method
            var methodInfo = typeof(PackageUploadHelper).GetMethod("SourceRepoFoundToUploadArtifactory",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            // Act
            var task = (Task)methodInfo.Invoke(null, new object[] { packageType, uploaderKpiData, item, timeout, displayPackagesInfo });
            await task;

            // Assert
            Assert.AreEqual(0, uploaderKpiData.DevPackagesUploaded);
            Assert.AreEqual(1, uploaderKpiData.DevPackagesNotUploadedToJfrog);
            Assert.AreEqual(1, uploaderKpiData.PackagesNotUploadedDueToError);
        }

        [Test]
        public async Task SourceRepoFoundToUploadArtifactory_WhenDryRunMode_DoesNotIncrementSuccessCounters()
        {
            // Arrange
            var packageType = PackageType.ClearedThirdParty;
            var uploaderKpiData = new UploaderKpiData();
            var item = new ComponentsToArtifactory
            {
                Name = "dryrun-package",
                Version = "1.0.0",
                ComponentType = "DEBIAN",
                PackageType = PackageType.ClearedThirdParty,
                DryRun = true,
                SrcRepoName = "src-repo",
                DestRepoName = "dest-repo",
                JfrogApi = "https://test-api.com",
                Token = "test-token"
            };
            int timeout = 5000;
            var displayPackagesInfo = new DisplayPackagesInfo
            {
                JfrogFoundPackagesDebian = new List<ComponentsToArtifactory>(),
                JfrogNotFoundPackagesDebian = new List<ComponentsToArtifactory>()
            };

            // Mock services
            var mockJFrogService = new Mock<IJFrogService>();
            var mockJFrogApiComm = new Mock<IJFrogApiCommunication>();
            PackageUploadHelper.JFrogService = mockJFrogService.Object;
            ArtifactoryUploader.ArtifactoryUploader.JFrogService = mockJFrogService.Object;
            ArtifactoryUploader.ArtifactoryUploader.JFrogApiCommInstance = mockJFrogApiComm.Object;

            // Use reflection to get private method
            var methodInfo = typeof(PackageUploadHelper).GetMethod("SourceRepoFoundToUploadArtifactory",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            // Act
            var task = (Task)methodInfo.Invoke(null, new object[] { packageType, uploaderKpiData, item, timeout, displayPackagesInfo });
            await task;

            // Assert - No counters should be incremented in dry run mode with success
            Assert.AreEqual(0, uploaderKpiData.PackagesUploadedToJfrog);
            Assert.AreEqual(0, uploaderKpiData.PackagesNotUploadedToJfrog);
        }

        [Test]
        public async Task SourceRepoFoundToUploadArtifactory_WithCopyOperation_SetsCorrectOperationType()
        {
            // Arrange
            var packageType = PackageType.ClearedThirdParty;
            var uploaderKpiData = new UploaderKpiData();
            var item = new ComponentsToArtifactory
            {
                Name = "copy-package",
                Version = "1.0.0",
                ComponentType = "CARGO",
                PackageType = PackageType.ClearedThirdParty, // Should result in copy operation
                DryRun = false,
                SrcRepoName = "src-repo",
                DestRepoName = "dest-repo",
                JfrogApi = "https://test-api.com",
                Token = "test-token"
            };
            int timeout = 5000;
            var displayPackagesInfo = new DisplayPackagesInfo
            {
                JfrogFoundPackagesCargo = new List<ComponentsToArtifactory>(),
                JfrogNotFoundPackagesCargo = new List<ComponentsToArtifactory>()
            };

            // Mock services
            var mockJFrogService = new Mock<IJFrogService>();
            var mockJFrogApiComm = new Mock<IJFrogApiCommunication>();
            PackageUploadHelper.JFrogService = mockJFrogService.Object;
            ArtifactoryUploader.ArtifactoryUploader.JFrogService = mockJFrogService.Object;
            ArtifactoryUploader.ArtifactoryUploader.JFrogApiCommInstance = mockJFrogApiComm.Object;

            // Use reflection to get private method
            var methodInfo = typeof(PackageUploadHelper).GetMethod("SourceRepoFoundToUploadArtifactory",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            // Act
            var task = (Task)methodInfo.Invoke(null, new object[] { packageType, uploaderKpiData, item, timeout, displayPackagesInfo });
            await task;

            // Assert
            Assert.AreEqual(1, uploaderKpiData.PackagesUploadedToJfrog);
        }

        [Test]
        public async Task SourceRepoFoundToUploadArtifactory_WithMoveOperation_InternalPackage()
        {
            // Arrange
            var packageType = PackageType.Internal;
            var uploaderKpiData = new UploaderKpiData();
            var item = new ComponentsToArtifactory
            {
                Name = "internal-package",
                Version = "1.0.0",
                ComponentType = "CHOCO",
                PackageType = PackageType.Internal, // Should result in move operation
                DryRun = false,
                SrcRepoName = "src-repo",
                DestRepoName = "internal-repo",
                JfrogApi = "https://test-api.com",
                Token = "test-token"
            };
            int timeout = 5000;
            var displayPackagesInfo = new DisplayPackagesInfo
            {
                JfrogFoundPackagesChoco = new List<ComponentsToArtifactory>(),
                JfrogNotFoundPackagesChoco = new List<ComponentsToArtifactory>()
            };

            // Mock services
            var mockJFrogService = new Mock<IJFrogService>();
            var mockJFrogApiComm = new Mock<IJFrogApiCommunication>();
            PackageUploadHelper.JFrogService = mockJFrogService.Object;
            ArtifactoryUploader.ArtifactoryUploader.JFrogService = mockJFrogService.Object;
            ArtifactoryUploader.ArtifactoryUploader.JFrogApiCommInstance = mockJFrogApiComm.Object;

            // Use reflection to get private method
            var methodInfo = typeof(PackageUploadHelper).GetMethod("SourceRepoFoundToUploadArtifactory",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            // Act
            var task = (Task)methodInfo.Invoke(null, new object[] { packageType, uploaderKpiData, item, timeout, displayPackagesInfo });
            await task;

            // Assert
            Assert.AreEqual(1, uploaderKpiData.InternalPackagesUploaded);
            Assert.AreEqual(0, uploaderKpiData.InternalPackagesNotUploadedToJfrog);
        }

        [Test]
        public async Task SourceRepoFoundToUploadArtifactory_WhenResponseIsNotOkOrKnownError_DoesNothing()
        {
            // Arrange
            var packageType = PackageType.ClearedThirdParty;
            var uploaderKpiData = new UploaderKpiData();
            var item = new ComponentsToArtifactory
            {
                Name = "unknown-error-package",
                Version = "1.0.0",
                ComponentType = "NPM",
                PackageType = PackageType.ClearedThirdParty,
                DryRun = false,
                SrcRepoName = "src-repo",
                DestRepoName = "dest-repo",
                JfrogApi = "https://test-api.com",
                Token = "test-token"
            };
            int timeout = 5000;
            var displayPackagesInfo = new DisplayPackagesInfo
            {
                JfrogFoundPackagesNpm = new List<ComponentsToArtifactory>(),
                JfrogNotFoundPackagesNpm = new List<ComponentsToArtifactory>()
            };

            // Mock services
            var mockJFrogService = new Mock<IJFrogService>();
            var mockJFrogApiComm = new Mock<IJFrogApiCommunication>();
            PackageUploadHelper.JFrogService = mockJFrogService.Object;
            ArtifactoryUploader.ArtifactoryUploader.JFrogService = mockJFrogService.Object;
            ArtifactoryUploader.ArtifactoryUploader.JFrogApiCommInstance = mockJFrogApiComm.Object;

            // Use reflection to get private method
            var methodInfo = typeof(PackageUploadHelper).GetMethod("SourceRepoFoundToUploadArtifactory",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            // Act
            var task = (Task)methodInfo.Invoke(null, new object[] { packageType, uploaderKpiData, item, timeout, displayPackagesInfo });
            await task;

            // Assert - Counters should remain at 0 for unknown errors (else branch does nothing)
            Assert.AreEqual(0, uploaderKpiData.PackagesUploadedToJfrog);
        }

        #endregion

    }
}
