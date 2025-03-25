using LCT.APICommunications.Model.AQL;
using LCT.ArtifactoryUploader;
using LCT.Services.Interface;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
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


    }
}
