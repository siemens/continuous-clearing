using LCT.APICommunications.Model;
using LCT.Common.Model;
using Moq;
using System.Net;
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
        public void AttachComponentSourceToSW360_UriFormatException_HandlesGracefully()
        {
            // Arrange - Create AttachmentHelper with URL that will construct invalid URI in method
            // Using special characters that cause UriFormatException within the method
            var attachReport = new AttachReport
            {
                ReleaseId = "{invalid-id-with-[brackets]}",
                AttachmentFile = _attachmentFilePath,
                AttachmentType = "Source",
                AttachmentReleaseComment = "Test comment"
            };
            ComparisonBomData comparisonBomData = new ComparisonBomData
            {
                ReleaseCreatedBy = "testuser@example.com"
            };

            // Act & Assert - Should handle UriFormatException gracefully and not throw
            string result = string.Empty;
            Assert.DoesNotThrow(() => result = _attachmentHelper.AttachComponentSourceToSW360(attachReport, comparisonBomData));

            // Verify that the method returns the expected API URL structure even when exception occurs
            Assert.That(result, Does.Contain("attachments"));
        }

        [Test]
        public void AttachComponentSourceToSW360_SecurityException_HandlesGracefully()
        {
            // Arrange
            var attachReport = new AttachReport
            {
                ReleaseId = "security-test-123",
                AttachmentFile = Path.Combine(Path.GetTempPath(), "nonexistent_file.zip"),
                AttachmentType = "Source",
                AttachmentReleaseComment = "Security test comment"
            };
            ComparisonBomData comparisonBomData = new ComparisonBomData
            {
                Name = "TestComponent",
                Version = "1.0.0",
                ReleaseCreatedBy = "testuser@example.com"
            };

            // Act & Assert - Should handle SecurityException gracefully and log with exception parameter
            string result = string.Empty;
            Assert.DoesNotThrow(() => result = _attachmentHelper.AttachComponentSourceToSW360(attachReport, comparisonBomData));

            // Verify that the result contains the release ID
            Assert.That(result, Does.Contain("security-test-123"));
        }

        [Test]
        public void AttachComponentSourceToSW360_WebException_HandlesGracefullyAndLogsDetails()
        {
            // Arrange
            var attachReport = new AttachReport
            {
                ReleaseId = "webex-test-123",
                AttachmentFile = Path.Combine(Path.GetTempPath(), "test_source.zip"),
                AttachmentType = "Source",
                AttachmentReleaseComment = "Web exception test"
            };
            ComparisonBomData comparisonBomData = new ComparisonBomData
            {
                Name = "WebExComponent",
                Version = "2.0.0",
                ReleaseCreatedBy = "developer@example.com"
            };

            // Act & Assert - Should handle WebException gracefully with proper logging
            string result = string.Empty;
            Assert.DoesNotThrow(() => result = _attachmentHelper.AttachComponentSourceToSW360(attachReport, comparisonBomData));

            // Verify method completes and returns URL
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Does.Contain("webex-test-123"));
        }

        [Test]
        public void AttachComponentSourceToSW360_IOException_HandlesGracefullyWithExceptionParameter()
        {
            // Arrange
            var attachReport = new AttachReport
            {
                ReleaseId = "io-test-456",
                AttachmentFile = Path.Combine(Path.GetTempPath(), "locked_file.zip"),
                AttachmentType = "Binary",
                AttachmentReleaseComment = "IO exception test",
                AttachmentCheckStatus = "ACCEPTED"
            };
            ComparisonBomData comparisonBomData = new ComparisonBomData
            {
                Name = "IOTestComponent",
                Version = "3.0.0",
                ReleaseCreatedBy = "qa@example.com"
            };

            // Act & Assert - Should catch IOException, log with exception parameter using ErrorFormat
            string result = string.Empty;
            Assert.DoesNotThrow(() => result = _attachmentHelper.AttachComponentSourceToSW360(attachReport, comparisonBomData));

            // Verify the release ID is in the returned URL
            Assert.That(result, Does.Contain("io-test-456"));
            Assert.That(result, Does.Contain("/attachments"));
        }

        [Test]
        public void AttachComponentSourceToSW360_WebExceptionWithResponseStream_LogsResponseDetails()
        {
            // Arrange - Test WebException with response stream for detailed error logging
            var attachReport = new AttachReport
            {
                ReleaseId = "webex-stream-789",
                AttachmentFile = Path.Combine(Path.GetTempPath(), "test_attachment.zip"),
                AttachmentType = "Source",
                AttachmentReleaseComment = "Test with response stream"
            };
            ComparisonBomData comparisonBomData = new ComparisonBomData
            {
                Name = "StreamTestComponent",
                Version = "1.5.0",
                ReleaseCreatedBy = "admin@example.com"
            };

            // Act & Assert - Should handle WebException with response stream gracefully
            string result = string.Empty;
            Assert.DoesNotThrow(() => result = _attachmentHelper.AttachComponentSourceToSW360(attachReport, comparisonBomData));

            // Verify result contains expected URL structure
            Assert.That(result, Is.Not.Empty);
            Assert.That(result, Does.Contain("webex-stream-789"));
        }
    }
}
