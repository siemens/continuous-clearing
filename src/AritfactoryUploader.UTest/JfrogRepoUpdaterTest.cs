using LCT.APICommunications.Model;
using LCT.ArtifactoryUploader.Model;
using LCT.ArtifactoryUploader;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using LCT.APICommunications.Model.AQL;
using LCT.Services.Interface;
using Moq;

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
            var actualExtension = JfrogRepoUpdater.GetPackageNameExtensionBasedOnComponentType(package);
            // Assert
            Assert.AreEqual(extension, actualExtension);
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
            List<ComponentsToArtifactory> uploadedPackages = JfrogRepoUpdater.GetUploadePackageDetails(displayPackagesInfo);

            // Assert
            Assert.AreEqual(6, uploadedPackages.Count);
        }
    }
}
