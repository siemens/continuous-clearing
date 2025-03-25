using LCT.APICommunications;
using LCT.APICommunications.Model;
using LCT.ArtifactoryUploader;
using LCT.ArtifactoryUploader.Model;
using LCT.Common.Constants;
using LCT.Common.Interface;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;

namespace AritfactoryUploader.UTest
{
    public class PackageUploadInformationTest
    {
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
            List<ComponentsToArtifactory> uploadedPackages = PackageUploadInformation.GetUploadePackageDetails(displayPackagesInfo);

            // Assert
            Assert.AreEqual(6, uploadedPackages.Count);
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
            PackageUploadInformation.GetNotApprovedDebianPackages(unknownPackages, projectResponse, fileOperationsMock.Object, filepath, filename);

            // Assert
            // Add your assertions here
            Assert.That(unknownPackages.Count, Is.EqualTo(2));
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

            PackageUploadInformation.DisplayErrorForJfrogPackages(JfrogNotFoundPackages);

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

            PackageUploadInformation.DisplayErrorForJfrogFoundPackages(JfrogNotFoundPackages);

            // Assert
            Assert.That(JfrogNotFoundPackages.Count, Is.GreaterThanOrEqualTo(1));
        }

        [Test]
        public void GetNotApprovedDebianPackages_FileExists_ShouldUpdateDebianComponents()
        {
            // Arrange
            var unknownPackages = new List<ComponentsToArtifactory>
            {
                new ComponentsToArtifactory { Name = "Package1", Version = "1.0.0" },
                new ComponentsToArtifactory { Name = "Package2", Version = "2.0.0" }
            };
            var projectResponse = new ProjectResponse();
            var mockFileOperations = new Mock<IFileOperations>();
            var filepath = Path.GetTempPath();
            var filename = Path.Combine(filepath, $"Artifactory_{FileConstant.artifactoryReportNotApproved}");

            var existingProjectResponse = new ProjectResponse
            {
                Debian = new List<JsonComponents>
                {
                    new JsonComponents { Name = "ExistingPackage", Version = "1.0.0" }
                }
            };
            var json = JsonConvert.SerializeObject(existingProjectResponse);
            File.WriteAllText(filename, json);

            // Act
            PackageUploadInformation.GetNotApprovedDebianPackages(unknownPackages, projectResponse, mockFileOperations.Object, filepath, filename);

            // Assert
            mockFileOperations.Verify(m => m.WriteContentToReportNotApprovedFile(It.Is<ProjectResponse>(pr =>
                pr.Debian.Count == 2 &&
                pr.Debian[0].Name == "Package1" &&
                pr.Debian[0].Version == "1.0.0" &&
                pr.Debian[1].Name == "Package2" &&
                pr.Debian[1].Version == "2.0.0"
            ), filepath, FileConstant.artifactoryReportNotApproved, "Artifactory"), Times.Once);

            // Cleanup
            File.Delete(filename);
        }
    }
}
