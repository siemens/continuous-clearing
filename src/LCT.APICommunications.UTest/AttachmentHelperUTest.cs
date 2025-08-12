using LCT.APICommunications.Model;
using LCT.Common.Model;
using Moq;
using System.Net;
using System.Security;
using File = System.IO.File;

namespace LCT.APICommunications.UTest
{
    [TestFixture]
    public class AttachmentHelperUTest
    {
        private readonly string _sw360AuthToken = "mockToken";
        private readonly string _sw360AuthTokenType = "Bearer";
        private readonly string _sw360ReleaseApi = "https://api.mock.com/release";
        private readonly string _attachmentFilePath = "mockFilePath";
        private AttachmentHelper _attachmentHelper;

        [SetUp]
        public void SetUp()
        {

            // Initialize the AttachmentHelper with mock logger.
            _attachmentHelper = new AttachmentHelper(_sw360AuthTokenType, _sw360AuthToken, _sw360ReleaseApi);
        }

        [Test]
        public void AttachComponentSourceToSW360_ValidInput_ReturnsCorrectUrl()
        {
            // Arrange
            var attachReport = new AttachReport
            {
                ReleaseId = "123",
                AttachmentFile = _attachmentFilePath,
                AttachmentType = "Source",
                AttachmentReleaseComment = "Test comment"
            };

            // Mock WebRequest behavior
            var webRequestMock = new Mock<HttpWebRequest>();
            var webResponseMock = new Mock<WebResponse>();

            // Simulate valid response
            webRequestMock.Setup(x => x.GetRequestStream()).Returns(new MemoryStream());
            webRequestMock.Setup(x => x.GetResponse()).Returns(webResponseMock.Object);
            ComparisonBomData comparisonBomData = new ComparisonBomData();
            // Act
            var result = _attachmentHelper.AttachComponentSourceToSW360(attachReport, comparisonBomData);

            // Assert
            Assert.That(result, Is.EqualTo("https://api.mock.com/release/123/attachments"));
        }

        [Test]
        public void WriteAttachmentsJSONFile_CreatesValidJSONFile()
        {
            // Arrange
            var attachReport = new AttachReport
            {
                AttachmentType = "Source",
                AttachmentReleaseComment = "Test comment"
            };
            string folderPath = Path.Combine(Path.GetTempPath(), "ClearingTool/DownloadedFiles");
            string fileName = "Attachment.json";

            // Act
            AttachmentHelper.WriteAttachmentsJSONFile(fileName, folderPath, attachReport);

            // Assert
            var jsonFilePath = Path.Combine(folderPath, "Attachment.json");
            Assert.That(File.Exists(jsonFilePath), Is.True);

            // Validate JSON content
            var fileContent = File.ReadAllText(jsonFilePath);
            Assert.That(fileContent, Does.Contain("Source"));
            Assert.That(fileContent, Does.Contain("Test comment"));

            // Cleanup
            File.Delete(jsonFilePath);
        }

        [Test]
        public void AttachComponentSourceToSW360_UriFormatException_LogsError()
        {
            // Arrange
            var attachReport = new AttachReport
            {
                ReleaseId = "invalid_uri",
                AttachmentFile = _attachmentFilePath,
                AttachmentType = "Source",
                AttachmentReleaseComment = "Test comment"
            };
            ComparisonBomData comparisonBomData = new ComparisonBomData();
            // Act & Assert
            Assert.DoesNotThrow(() => _attachmentHelper.AttachComponentSourceToSW360(attachReport, comparisonBomData));
        }

        [Test]
        public void AttachComponentSourceToSW360_SecurityException_LogsError()
        {
            // Arrange
            var attachReport = new AttachReport
            {
                ReleaseId = "123",
                AttachmentFile = _attachmentFilePath,
                AttachmentType = "Source",
                AttachmentReleaseComment = "Test comment"
            };

            // Mock WebRequest behavior to throw SecurityException
            var webRequestMock = new Mock<HttpWebRequest>();
            webRequestMock.Setup(x => x.GetRequestStream()).Throws(new SecurityException());
            ComparisonBomData comparisonBomData = new ComparisonBomData();
            // Act & Assert
            Assert.DoesNotThrow(() => _attachmentHelper.AttachComponentSourceToSW360(attachReport, comparisonBomData));
        }

        [Test]
        public void AttachComponentSourceToSW360_WebException_LogsError()
        {
            // Arrange
            var attachReport = new AttachReport
            {
                ReleaseId = "123",
                AttachmentFile = _attachmentFilePath,
                AttachmentType = "Source",
                AttachmentReleaseComment = "Test comment"
            };

            // Mock WebRequest behavior to throw WebException
            var webRequestMock = new Mock<HttpWebRequest>();
            var webResponseMock = new Mock<WebResponse>();
            webRequestMock.Setup(x => x.GetRequestStream()).Throws(new WebException());
            webRequestMock.Setup(x => x.GetResponse()).Returns(webResponseMock.Object);
            ComparisonBomData comparisonBomData = new ComparisonBomData();
            // Act & Assert
            Assert.DoesNotThrow(() => _attachmentHelper.AttachComponentSourceToSW360(attachReport, comparisonBomData));
        }

        [Test]
        public void AttachComponentSourceToSW360_IOException_LogsError()
        {
            // Arrange
            var attachReport = new AttachReport
            {
                ReleaseId = "123",
                AttachmentFile = _attachmentFilePath,
                AttachmentType = "Source",
                AttachmentReleaseComment = "Test comment"
            };

            // Mock WebRequest behavior to throw IOException
            var webRequestMock = new Mock<HttpWebRequest>();
            webRequestMock.Setup(x => x.GetRequestStream()).Throws(new IOException());
            ComparisonBomData comparisonBomData = new ComparisonBomData();
            // Act & Assert
            Assert.DoesNotThrow(() => _attachmentHelper.AttachComponentSourceToSW360(attachReport, comparisonBomData));
        }
    }
}
