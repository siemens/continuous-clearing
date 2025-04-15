// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models;
using LCT.APICommunications.Model;
using LCT.APICommunications.Model.AQL;
using LCT.ArtifactoryUploader;
using LCT.Services.Interface;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace AritfactoryUploader.UTest
{
    public class JfrogRepoUpdaterTest
    {
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
            JfrogRepoUpdater.jFrogService = jFrogServiceMock.Object;

            // Act
            var actualAqlResultList = await JfrogRepoUpdater.GetJfrogRepoInfoForAllTypePackages(destRepoNames);


            // Assert
            Assert.That(actualAqlResultList.Count, Is.GreaterThan(2));
        }
        [Test]
        public void GetJfrogRepoPath_GivenAqlResultWithEmptyPath_ReturnsRepoAndName()
        {
            // Arrange
            var aqlResult = new AqlResult
            {
                Repo = "test-repo",
                Name = "test-name",
                Path = ""
            };

            var methodInfo = typeof(JfrogRepoUpdater).GetMethod("GetJfrogRepoPath", BindingFlags.NonPublic | BindingFlags.Static);

            // Act
            var result = (string)methodInfo.Invoke(null, new object[] { aqlResult });

            // Assert
            Assert.AreEqual("test-repo/test-name", result);
        }

        [Test]
        public void GetJfrogRepoPath_GivenAqlResultWithDotPath_ReturnsRepoAndName()
        {
            // Arrange
            var aqlResult = new AqlResult
            {
                Repo = "test-repo",
                Name = "test-name",
                Path = "."
            };

            var methodInfo = typeof(JfrogRepoUpdater).GetMethod("GetJfrogRepoPath", BindingFlags.NonPublic | BindingFlags.Static);

            // Act
            var result = (string)methodInfo.Invoke(null, new object[] { aqlResult });

            // Assert
            Assert.AreEqual("test-repo/test-name", result);
        }

        [Test]
        public void GetJfrogRepoPath_GivenAqlResultWithValidPath_ReturnsRepoPathAndName()
        {
            // Arrange
            var aqlResult = new AqlResult
            {
                Repo = "test-repo",
                Name = "test-name",
                Path = "test-path"
            };

            var methodInfo = typeof(JfrogRepoUpdater).GetMethod("GetJfrogRepoPath", BindingFlags.NonPublic | BindingFlags.Static);

            // Act
            var result = (string)methodInfo.Invoke(null, new object[] { aqlResult });

            // Assert
            Assert.AreEqual("test-repo/test-path/test-name", result);
        }
        [Test]
        public void GetJfrogInfoOfThePackageUploaded_GivenConanPackage_ReturnsMatchingAqlResult()
        {
            // Arrange
            var jfrogPackagesListAql = new List<AqlResult>
            {
                new AqlResult { Path = "package1/1.0.0", Name = "package.conan" },
                new AqlResult { Path = "package2/2.0.0", Name = "package2.conan" }
            };

            var package = new ComponentsToArtifactory
            {
                Name = "package1",
                Version = "1.0.0",
                ComponentType = "CONAN"
            };

            string packageNameExtension = "conan";

            var methodInfo = typeof(JfrogRepoUpdater).GetMethod("GetJfrogInfoOfThePackageUploaded", BindingFlags.NonPublic | BindingFlags.Static);

            // Act
            var result = (AqlResult)methodInfo.Invoke(null, new object[] { jfrogPackagesListAql, package, packageNameExtension });

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("package1/1.0.0", result.Path);
            Assert.AreEqual("package.conan", result.Name);
        }

        [Test]
        public void GetJfrogInfoOfThePackageUploaded_GivenNonConanPackage_ReturnsMatchingAqlResult()
        {
            // Arrange
            var jfrogPackagesListAql = new List<AqlResult>
            {
                new AqlResult { Path = "package1", Name = "1.0.0.jar" },
                new AqlResult { Path = "package2", Name = "2.0.0.jar" }
            };

            var package = new ComponentsToArtifactory
            {
                Name = "package1",
                Version = "1.0.0",
                ComponentType = "MAVEN"
            };

            string packageNameExtension = "jar";

            var methodInfo = typeof(JfrogRepoUpdater).GetMethod("GetJfrogInfoOfThePackageUploaded", BindingFlags.NonPublic | BindingFlags.Static);

            // Act
            var result = (AqlResult)methodInfo.Invoke(null, new object[] { jfrogPackagesListAql, package, packageNameExtension });

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("package1", result.Path);
            Assert.AreEqual("1.0.0.jar", result.Name);
        }

        [Test]
        public void GetJfrogInfoOfThePackageUploaded_GivenNoMatchingPackage_ReturnsNull()
        {
            // Arrange
            var jfrogPackagesListAql = new List<AqlResult>
            {
                new AqlResult { Path = "package1", Name = "1.0.0.jar" },
                new AqlResult { Path = "package2", Name = "2.0.0.jar" }
            };

            var package = new ComponentsToArtifactory
            {
                Name = "package3",
                Version = "3.0.0",
                ComponentType = "MAVEN"
            };

            string packageNameExtension = "jar";

            var methodInfo = typeof(JfrogRepoUpdater).GetMethod("GetJfrogInfoOfThePackageUploaded", BindingFlags.NonPublic | BindingFlags.Static);

            // Act
            var result = (AqlResult)methodInfo.Invoke(null, new object[] { jfrogPackagesListAql, package, packageNameExtension });

            // Assert
            Assert.IsNull(result);
        }
        [Test]
        public void UpdateJfroRepoPathProperty_GivenComponentNotInUploadList_SkipsUpdate()
        {
            // Arrange
            var bom = new Bom
            {
                Components = new List<Component>
        {
            new Component
            {
                Name = "component1",
                Version = "1.0.0",
                Purl = "pkg:generic/component1@1.0.0",
                Properties = null
            },
            new Component
            {
                Name = "component2",
                Version = "2.0.0",
                Purl = "pkg:generic/component2@2.0.0",
                Properties = null
            }
        }
            };

            var uploadedPackages = new List<ComponentsToArtifactory>
    {
        new ComponentsToArtifactory
        {
            Name = "component3", // Does not match any component in BOM
            Version = "3.0.0",
            Purl = "pkg:generic/component3@3.0.0",
            ComponentType = "MAVEN"
        }
    };

            var jfrogPackagesListAql = new List<AqlResult>
    {
        new AqlResult { Repo = "repo1", Path = "path1", Name = "component3-3.0.0.jar" }
    };

            // Act
            var methodInfo = typeof(JfrogRepoUpdater).GetMethod("UpdateJfroRepoPathProperty", BindingFlags.NonPublic | BindingFlags.Static);
            var updatedComponents = (List<Component>)methodInfo.Invoke(null, new object[] { bom, uploadedPackages, jfrogPackagesListAql });

            // Assert
            Assert.IsNotNull(updatedComponents);
            Assert.AreEqual(2, updatedComponents.Count);

            // Verify that no properties were added to the components in BOM
            Assert.IsNull(updatedComponents[0].Properties);
            Assert.IsNull(updatedComponents[1].Properties);
        }

        [Test]
        public void UpdateJfroRepoPathProperty_GivenJfrogDataNotFound_SkipsUpdate()
        {
            // Arrange
            var bom = new Bom
            {
                Components = new List<Component>
        {
            new Component
            {
                Name = "component1",
                Version = "1.0.0",
                Purl = "pkg:generic/component1@1.0.0",
                Properties = null
            }
        }
            };

            var uploadedPackages = new List<ComponentsToArtifactory>
    {
        new ComponentsToArtifactory
        {
            Name = "component1",
            Version = "1.0.0",
            Purl = "pkg:generic/component1@1.0.0",
            ComponentType = "MAVEN"
        }
    };

            var jfrogPackagesListAql = new List<AqlResult>(); // No matching JFrog data

            // Act
            var methodInfo = typeof(JfrogRepoUpdater).GetMethod("UpdateJfroRepoPathProperty", BindingFlags.NonPublic | BindingFlags.Static);
            var updatedComponents = (List<Component>)methodInfo.Invoke(null, new object[] { bom, uploadedPackages, jfrogPackagesListAql });

            // Assert
            Assert.IsNotNull(updatedComponents);
            Assert.AreEqual(1, updatedComponents.Count);

            // Verify that no properties were added to the component
            Assert.IsNull(updatedComponents[0].Properties);
        }

    }
}
