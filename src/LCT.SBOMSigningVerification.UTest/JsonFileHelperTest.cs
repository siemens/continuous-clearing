// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
// SPDX-License-Identifier: MIT
// --------------------------------------------------------------------------------------------------------------------

using LCT.SBOMSigningVerification.Helpers;
using LCT.SBOMSigningVerification.Interface;
using LCT.SBOMSigningVerification.Model;
using Moq;
using NUnit.Framework;
using System;
using System.IO;
using System.Text.Json;

namespace LCT.SBOMSigningVerification.UTest
{
    [TestFixture]
    public class JsonFileHelperTest
    {
        private JsonFileHelper _jsonFileHelper;
        private Mock<ICertificateHelper> _mockCertificateHelper;
        private Mock<ISignatureHelper> _mockSignatureHelper;
        private AppSettings _appSettings;
        private string _tempDirectory;

        [SetUp]
        public void Setup()
        {
            _mockCertificateHelper = new Mock<ICertificateHelper>();
            _mockSignatureHelper = new Mock<ISignatureHelper>();
            
            _appSettings = new AppSettings
            {
                bomcontent = "{\"name\":\"test-sbom\",\"version\":\"1.0.0\"}",
                SBOMSignVerify = true
            };

            _jsonFileHelper = new JsonFileHelper(_appSettings, _mockCertificateHelper.Object, _mockSignatureHelper.Object);
            
            // Create temp directory for file operations
            _tempDirectory = Path.Combine(Path.GetTempPath(), "JsonFileHelperTest", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDirectory);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, true);
            }
        }

        [Test]
        public void SignSBOMFile_WithExistingSignature_ThrowsInvalidOperationException()
        {
            // Arrange
            string originalSbom = "{\"name\":\"test-sbom\",\"signature\":{\"value\":\"existing\"}}";
            string cleanedSbomWithSignature = "{\"name\":\"test-sbom\",\"signature\":{\"value\":\"existing\"}}"; // Simulate that signature wasn't removed properly
            
            _appSettings.bomcontent = originalSbom;
            
            _mockSignatureHelper.Setup(x => x.RemoveSignature(originalSbom))
                .Returns(cleanedSbomWithSignature); // This simulates a scenario where signature removal failed

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => _jsonFileHelper.SignSBOMFile());
            Assert.That(exception.Message, Does.Contain("File already contains a signature"));
        }

        [Test]
        public void SignSBOMFile_WithoutExistingSignature_ReturnsSignedSBOM()
        {
            // Arrange
            string originalSbom = "{\"name\":\"test-sbom\",\"version\":\"1.0.0\"}";
            string cleanedSbom = "{\"name\":\"test-sbom\",\"version\":\"1.0.0\"}";
            byte[] signatureBytes = Convert.FromBase64String("dGVzdC1zaWduYXR1cmU=");
            
            _appSettings.bomcontent = originalSbom;
            
            _mockSignatureHelper.Setup(x => x.RemoveSignature(originalSbom))
                .Returns(cleanedSbom);
            _mockCertificateHelper.Setup(x => x.SignCertificate(cleanedSbom))
                .Returns(signatureBytes);

            // Act
            string result = _jsonFileHelper.SignSBOMFile();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Does.Contain("signature"));
            Assert.That(result, Does.Contain("dGVzdC1zaWduYXR1cmU="));
        }

        [Test]
        public void SignSBOMFile_WithNullSignature_ThrowsArgumentNullException()
        {
            // Arrange
            string originalSbom = "{\"name\":\"test-sbom\",\"version\":\"1.0.0\"}";
            string cleanedSbom = "{\"name\":\"test-sbom\",\"version\":\"1.0.0\"}";
            
            _appSettings.bomcontent = originalSbom;
            
            _mockSignatureHelper.Setup(x => x.RemoveSignature(originalSbom))
                .Returns(cleanedSbom);
            _mockCertificateHelper.Setup(x => x.SignCertificate(cleanedSbom))
                .Returns((byte[])null);

            // Act & Assert
            // Convert.ToBase64String(null) throws ArgumentNullException
            Assert.Throws<ArgumentNullException>(() => _jsonFileHelper.SignSBOMFile());
        }

        [Test]
        public void SignSBOMFile_WithEmptySignature_ReturnsSignedSBOM()
        {
            // Arrange
            string originalSbom = "{\"name\":\"test-sbom\",\"version\":\"1.0.0\"}";
            string cleanedSbom = "{\"name\":\"test-sbom\",\"version\":\"1.0.0\"}";
            
            _appSettings.bomcontent = originalSbom;
            
            _mockSignatureHelper.Setup(x => x.RemoveSignature(originalSbom))
                .Returns(cleanedSbom);
            _mockCertificateHelper.Setup(x => x.SignCertificate(cleanedSbom))
                .Returns(Array.Empty<byte>());

            // Act
            string result = _jsonFileHelper.SignSBOMFile();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Does.Contain("signature"));
            // Empty byte array converts to empty string in base64
            Assert.That(result, Does.Contain("\"value\": \"\""));
        }

        [Test]
        public void ReadSBOMFile_WithValidFile_ValidatesSignatureSuccessfully()
        {
            // Arrange
            string sbomContent = "{\"name\":\"test-sbom\",\"signature\":{\"value\":\"dGVzdC1zaWduYXR1cmU=\"}}";
            string sbomFilePath = Path.Combine(_tempDirectory, "test-sbom.json");
            File.WriteAllText(sbomFilePath, sbomContent);

            var signature = new Signature { Value = "dGVzdC1zaWduYXR1cmU=" };
            string cleanedSbom = "{\"name\":\"test-sbom\"}";

            _mockSignatureHelper.Setup(x => x.ExtractSignature(sbomContent))
                .Returns(signature);
            _mockSignatureHelper.Setup(x => x.RemoveSignature(sbomContent))
                .Returns(cleanedSbom);
            _mockCertificateHelper.Setup(x => x.VerifySignature(cleanedSbom, signature.Value))
                .Returns(true);

            // Act
            _jsonFileHelper.ReadSBOMFile(sbomFilePath, out bool isValid);

            // Assert
            Assert.That(isValid, Is.True);
        }

        [Test]
        public void ReadSBOMFile_WithInvalidSignature_ReturnsFalse()
        {
            // Arrange
            string sbomContent = "{\"name\":\"test-sbom\",\"signature\":{\"value\":\"invalid-signature\"}}";
            string sbomFilePath = Path.Combine(_tempDirectory, "test-sbom.json");
            File.WriteAllText(sbomFilePath, sbomContent);

            var signature = new Signature { Value = "invalid-signature" };
            string cleanedSbom = "{\"name\":\"test-sbom\"}";

            _mockSignatureHelper.Setup(x => x.ExtractSignature(sbomContent))
                .Returns(signature);
            _mockSignatureHelper.Setup(x => x.RemoveSignature(sbomContent))
                .Returns(cleanedSbom);
            _mockCertificateHelper.Setup(x => x.VerifySignature(cleanedSbom, signature.Value))
                .Returns(false);

            // Act
            _jsonFileHelper.ReadSBOMFile(sbomFilePath, out bool isValid);

            // Assert
            Assert.That(isValid, Is.False);
        }

        [Test]
        public void ReadSBOMFile_WithNoSignature_ThrowsArgumentException()
        {
            // Arrange
            string sbomContent = "{\"name\":\"test-sbom\"}";
            string sbomFilePath = Path.Combine(_tempDirectory, "test-sbom.json");
            File.WriteAllText(sbomFilePath, sbomContent);

            _mockSignatureHelper.Setup(x => x.ExtractSignature(sbomContent))
                .Returns((Signature)null);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _jsonFileHelper.ReadSBOMFile(sbomFilePath, out bool isValid));
        }

        [Test]
        public void ReadSBOMFile_WithEmptySignatureValue_ThrowsArgumentException()
        {
            // Arrange
            string sbomContent = "{\"name\":\"test-sbom\",\"signature\":{\"value\":\"\"}}";
            string sbomFilePath = Path.Combine(_tempDirectory, "test-sbom.json");
            File.WriteAllText(sbomFilePath, sbomContent);

            var signature = new Signature { Value = "" };

            _mockSignatureHelper.Setup(x => x.ExtractSignature(sbomContent))
                .Returns(signature);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _jsonFileHelper.ReadSBOMFile(sbomFilePath, out bool isValid));
        }

        [Test]
        public void ReadSBOMFile_WithNullSignatureValue_ThrowsArgumentException()
        {
            // Arrange
            string sbomContent = "{\"name\":\"test-sbom\",\"signature\":{\"algorithm\":\"RS256\"}}";
            string sbomFilePath = Path.Combine(_tempDirectory, "test-sbom.json");
            File.WriteAllText(sbomFilePath, sbomContent);

            var signature = new Signature { Value = null, Algorithm = "RS256" };

            _mockSignatureHelper.Setup(x => x.ExtractSignature(sbomContent))
                .Returns(signature);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _jsonFileHelper.ReadSBOMFile(sbomFilePath, out bool isValid));
        }

        [Test]
        public void ReadSBOMFile_WithNullFilePath_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _jsonFileHelper.ReadSBOMFile(null, out bool isValid));
        }

        [Test]
        public void ReadSBOMFile_WithEmptyFilePath_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _jsonFileHelper.ReadSBOMFile("", out bool isValid));
        }

        [Test]
        public void ReadSBOMFile_WithNonExistentFile_ThrowsFileNotFoundException()
        {
            // Arrange
            string nonExistentPath = Path.Combine(_tempDirectory, "nonexistent.json");

            // Act & Assert
            Assert.Throws<FileNotFoundException>(() => _jsonFileHelper.ReadSBOMFile(nonExistentPath, out bool isValid));
        }

        [Test]
        public void Constructor_WithNullAppSettings_DoesNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => new JsonFileHelper(null, _mockCertificateHelper.Object, _mockSignatureHelper.Object));
        }

        [Test]
        public void Constructor_WithNullCertificateHelper_DoesNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => new JsonFileHelper(_appSettings, null, _mockSignatureHelper.Object));
        }

        [Test]
        public void Constructor_WithNullSignatureHelper_DoesNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => new JsonFileHelper(_appSettings, _mockCertificateHelper.Object, null));
        }

        [Test]
        public void SignSBOMFile_WithNullBomContent_ThrowsInvalidOperationException()
        {
            // Arrange
            _appSettings.bomcontent = null;

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => _jsonFileHelper.SignSBOMFile());
        }

        [Test]
        public void SignSBOMFile_WithInvalidJson_ThrowsJsonException()
        {
            // Arrange
            _appSettings.bomcontent = "invalid json";
            
            // SignatureHelper.RemoveSignature will throw JsonException for invalid JSON
            _mockSignatureHelper.Setup(x => x.RemoveSignature("invalid json"))
                .Throws<JsonException>();

            // Act & Assert
            Assert.Throws<JsonException>(() => _jsonFileHelper.SignSBOMFile());
        }

        [Test]
        public void ReadSBOMFile_WithInvalidJsonFile_ThrowsJsonException()
        {
            // Arrange
            string invalidJsonContent = "invalid json";
            string sbomFilePath = Path.Combine(_tempDirectory, "invalid.json");
            File.WriteAllText(sbomFilePath, invalidJsonContent);

            // The SignatureHelper.ExtractSignature will throw when parsing invalid JSON
            _mockSignatureHelper.Setup(x => x.ExtractSignature(invalidJsonContent))
                .Throws<JsonException>();

            // Act & Assert
            Assert.Throws<JsonException>(() => _jsonFileHelper.ReadSBOMFile(sbomFilePath, out bool isValid));
        }

        [Test]
        public void SignSBOMFile_WithComplexJsonStructure_HandlesCorrectly()
        {
            // Arrange
            string complexSbom = @"{
                ""name"": ""test-sbom"",
                ""version"": ""1.0.0"",
                ""components"": [
                    {
                        ""name"": ""component1"",
                        ""version"": ""1.0""
                    }
                ],
                ""metadata"": {
                    ""timestamp"": ""2024-01-01T00:00:00Z""
                }
            }";
            
            byte[] signatureBytes = Convert.FromBase64String("Y29tcGxleC1zaWduYXR1cmU=");
            
            _appSettings.bomcontent = complexSbom;
            
            _mockSignatureHelper.Setup(x => x.RemoveSignature(It.IsAny<string>()))
                .Returns(complexSbom);
            _mockCertificateHelper.Setup(x => x.SignCertificate(It.IsAny<string>()))
                .Returns(signatureBytes);

            // Act
            string result = _jsonFileHelper.SignSBOMFile();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Does.Contain("signature"));
            Assert.That(result, Does.Contain("Y29tcGxleC1zaWduYXR1cmU="));
            
            // Verify the result is valid JSON
            Assert.DoesNotThrow(() => JsonDocument.Parse(result));
        }

        [Test]
        public void ReadSBOMFile_WithLargeFile_HandlesCorrectly()
        {
            // Arrange
            string largeContent = "{\"name\":\"test-sbom\",\"data\":\"" + new string('A', 10000) + "\",\"signature\":{\"value\":\"dGVzdC1zaWduYXR1cmU=\"}}";
            string sbomFilePath = Path.Combine(_tempDirectory, "large-sbom.json");
            File.WriteAllText(sbomFilePath, largeContent);

            var signature = new Signature { Value = "dGVzdC1zaWduYXR1cmU=" };
            string cleanedSbom = "{\"name\":\"test-sbom\",\"data\":\"" + new string('A', 10000) + "\"}";

            _mockSignatureHelper.Setup(x => x.ExtractSignature(largeContent))
                .Returns(signature);
            _mockSignatureHelper.Setup(x => x.RemoveSignature(largeContent))
                .Returns(cleanedSbom);
            _mockCertificateHelper.Setup(x => x.VerifySignature(cleanedSbom, signature.Value))
                .Returns(true);

            // Act
            _jsonFileHelper.ReadSBOMFile(sbomFilePath, out bool isValid);

            // Assert
            Assert.That(isValid, Is.True);
        }
    }
}
