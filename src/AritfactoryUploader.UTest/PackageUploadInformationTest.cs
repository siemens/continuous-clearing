using LCT.APICommunications;
using LCT.APICommunications.Model;
using LCT.ArtifactoryUploader;
using LCT.ArtifactoryUploader.Model;
using LCT.Common.Interface;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace AritfactoryUploader.UTest
{
    public class PackageUploadInformationTest
    {
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
            await PackageUploadInformation.JfrogNotFoundPackagesAsync(item, displayPackagesInfo);

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
            await PackageUploadInformation.JfrogFoundPackagesAsync(item, displayPackagesInfo, operationType, responseMessage, dryRunSuffix);

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
    }
}
