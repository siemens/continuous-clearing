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
            var repoUrl = bom.Components.First(x => x.Properties[3].Name == Dataconstant.Cdx_ArtifactoryRepoName).Properties[3].Value;
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
        }

        [Test]
        [TestCase("NPM")]
        [TestCase("NUGET")]
        [TestCase("MAVEN")]
        [TestCase("POETRY")]
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
        }


    }
}
