using LCT.Common.Model;
using LCT.Common;
using LCT.Services.Interface;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using log4net;
using LCT.APICommunications.Model;
namespace LCT.SW360PackageCreator.UTest
{
    public class PackageCreaterTest
    {
        [Test]
        public async Task CreatePackageInSw360_ShouldCallCreatePackageForEachBomItem_WhenTestModeIsFalse()
        {
            // Arrange
            var mockLogger = new Mock<ILog>();
            var mockSw360CreatorService = new Mock<ISw360CreatorService>();
            var mockSW360Service = new Mock<ISW360Service>();           
            var appSettings = new CommonAppSettings { Mode = "production" };
            var parsedBomData = new List<ComparisonBomData>
    {
        new ComparisonBomData { PackageName = "Package1", Version = "1.0", PackageStatus = "Not Available" },
        new ComparisonBomData { PackageName = "Package2", Version = "2.0", PackageStatus = "Not Available" }
    };            
            mockSw360CreatorService
                .Setup(service => service.CreatePackageBasesOFswComaprisonBOM(It.IsAny<ComparisonBomData>()))
                .ReturnsAsync(new PackageCreateStatus { IsCreated = true });
            var packageCreater = new PackageCreater();
            // Act
            await packageCreater.CreatePackageInSw360(appSettings, mockSw360CreatorService.Object, parsedBomData, mockSW360Service.Object);
            // Assert
            mockSw360CreatorService.Verify(service => service.CreatePackageBasesOFswComaprisonBOM(It.IsAny<ComparisonBomData>()), Times.Exactly(parsedBomData.Count));            
            mockSw360CreatorService.VerifyAll();
        }
        [Test]
        public async Task CreatePackageInSw360_ShouldNotCallCreatePackageForBomItems_WhenPackageStatusIsAvailable()
        {
            // Arrange
            var mockLogger = new Mock<ILog>();
            var mockSw360CreatorService = new Mock<ISw360CreatorService>();
            var mockSW360Service = new Mock<ISW360Service>();
            var appSettings = new CommonAppSettings { Mode = "production" };
            var parsedBomData = new List<ComparisonBomData>
    {
        new ComparisonBomData { PackageName = "Package1", Version = "1.0", PackageStatus = "Available" },
        new ComparisonBomData { PackageName = "Package2", Version = "2.0", PackageStatus = "Available" }
    };
            mockSw360CreatorService
                .Setup(service => service.CreatePackageBasesOFswComaprisonBOM(It.IsAny<ComparisonBomData>()))
                .ReturnsAsync(new PackageCreateStatus { IsCreated = true });
            var packageCreater = new PackageCreater();
            // Act
            await packageCreater.CreatePackageInSw360(appSettings, mockSw360CreatorService.Object, parsedBomData, mockSW360Service.Object);
            // Assert            
            mockSw360CreatorService.Verify(service => service.CreatePackageBasesOFswComaprisonBOM(It.IsAny<ComparisonBomData>()), Times.Never());            
        }

    }
}
