using LCT.APICommunications.Model;
using log4net;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security;
using System.Text;
using System.Threading.Tasks;

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

            // Act
            var result = _attachmentHelper.AttachComponentSourceToSW360(attachReport);

            // Assert
            Assert.AreEqual("https://api.mock.com/release/123/attachments", result);
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
            Assert.IsTrue(File.Exists(jsonFilePath));

            // Validate JSON content
            var fileContent = File.ReadAllText(jsonFilePath);
            Assert.IsTrue(fileContent.Contains("Source"));
            Assert.IsTrue(fileContent.Contains("Test comment"));

            // Cleanup
            File.Delete(jsonFilePath);
        }
    }
}
