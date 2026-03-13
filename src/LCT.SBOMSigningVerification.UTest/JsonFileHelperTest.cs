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
using System.Text;
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

        #region Additional Test Cases for Complete Coverage

        [Test]
        public void SignSBOMFile_VerifySignaturePropertyStructure()
        {
            // Arrange
            string originalSbom = "{\"bomFormat\":\"CycloneDX\"}";
            string cleanedSbom = "{\"bomFormat\":\"CycloneDX\"}";
            byte[] signatureBytes = { 1, 2, 3, 4, 5 };

            _appSettings.bomcontent = originalSbom;

            _mockSignatureHelper.Setup(x => x.RemoveSignature(originalSbom))
                .Returns(cleanedSbom);
            _mockCertificateHelper.Setup(x => x.SignCertificate(cleanedSbom))
                .Returns(signatureBytes);

            // Act
            string result = _jsonFileHelper.SignSBOMFile();

            // Assert - Verify signature object structure
            var jsonDoc = JsonDocument.Parse(result);
            Assert.That(jsonDoc.RootElement.TryGetProperty("signature", out var signatureProp), Is.True);
            Assert.That(signatureProp.TryGetProperty("algorithm", out var algorithmProp), Is.True);
            Assert.That(algorithmProp.GetString(), Is.EqualTo("http://www.w3.org/2001/04/xmldsig-more#rsa-sha256"));
            Assert.That(signatureProp.TryGetProperty("value", out var valueProp), Is.True);
        }

        [Test]
        public void SignSBOMFile_WithMultipleProperties_PreservesAllExistingProperties()
        {
            // Arrange
            string originalSbom = @"{
                ""name"": ""example-sbom"",
                ""version"": ""1.0.0"",
                ""license"": ""MIT"",
                ""components"": []
            }";
            string cleanedSbom = originalSbom;
            byte[] signatureBytes = { 1, 2, 3 };

            _appSettings.bomcontent = originalSbom;

            _mockSignatureHelper.Setup(x => x.RemoveSignature(originalSbom))
                .Returns(cleanedSbom);
            _mockCertificateHelper.Setup(x => x.SignCertificate(cleanedSbom))
                .Returns(signatureBytes);

            // Act
            string result = _jsonFileHelper.SignSBOMFile();

            // Assert - Verify all original properties are preserved
            var jsonDoc = JsonDocument.Parse(result);
            Assert.That(jsonDoc.RootElement.TryGetProperty("name", out _), Is.True);
            Assert.That(jsonDoc.RootElement.TryGetProperty("version", out _), Is.True);
            Assert.That(jsonDoc.RootElement.TryGetProperty("license", out _), Is.True);
            Assert.That(jsonDoc.RootElement.TryGetProperty("components", out _), Is.True);
            Assert.That(jsonDoc.RootElement.TryGetProperty("signature", out _), Is.True);
        }

        [Test]
        public void SignSBOMFile_CallsSignCertificateWithCleanedContent()
        {
            // Arrange
            string originalSbom = "{\"data\":\"original\",\"signature\":{\"value\":\"old\"}}";
            string cleanedSbom = "{\"data\":\"original\"}";
            byte[] signatureBytes = { 1, 2, 3 };

            _appSettings.bomcontent = originalSbom;

            _mockSignatureHelper.Setup(x => x.RemoveSignature(originalSbom))
                .Returns(cleanedSbom);
            _mockCertificateHelper.Setup(x => x.SignCertificate(cleanedSbom))
                .Returns(signatureBytes);

            // Act
            _jsonFileHelper.SignSBOMFile();

            // Assert - Verify SignCertificate was called with cleaned content
            _mockCertificateHelper.Verify(x => x.SignCertificate(cleanedSbom), Times.Once);
        }

        [Test]
        public void SignSBOMFile_ConvertsSignatureToBase64()
        {
            // Arrange
            string originalSbom = "{\"test\":\"data\"}";
            string cleanedSbom = "{\"test\":\"data\"}";
            byte[] signatureBytes = Encoding.UTF8.GetBytes("signature-content");
            string expectedBase64 = Convert.ToBase64String(signatureBytes);

            _appSettings.bomcontent = originalSbom;

            _mockSignatureHelper.Setup(x => x.RemoveSignature(originalSbom))
                .Returns(cleanedSbom);
            _mockCertificateHelper.Setup(x => x.SignCertificate(cleanedSbom))
                .Returns(signatureBytes);

            // Act
            string result = _jsonFileHelper.SignSBOMFile();

            // Assert
            Assert.That(result, Does.Contain(expectedBase64));
        }

        [Test]
        public void ReadSBOMFile_CallsVerifySignatureWithCorrectParameters()
        {
            // Arrange
            string sbomContent = "{\"data\":\"test\",\"signature\":{\"value\":\"sig123\"}}";
            string sbomFilePath = Path.Combine(_tempDirectory, "test.json");
            File.WriteAllText(sbomFilePath, sbomContent);

            var signature = new Signature { Value = "sig123" };
            string cleanedSbom = "{\"data\":\"test\"}";

            _mockSignatureHelper.Setup(x => x.ExtractSignature(sbomContent))
                .Returns(signature);
            _mockSignatureHelper.Setup(x => x.RemoveSignature(sbomContent))
                .Returns(cleanedSbom);
            _mockCertificateHelper.Setup(x => x.VerifySignature(cleanedSbom, "sig123"))
                .Returns(true);

            // Act
            _jsonFileHelper.ReadSBOMFile(sbomFilePath, out bool isValid);

            // Assert
            _mockCertificateHelper.Verify(x => x.VerifySignature(cleanedSbom, "sig123"), Times.Once);
        }

        [Test]
        public void ReadSBOMFile_ReadsFileContent()
        {
            // Arrange
            string sbomContent = "{\"bomFormat\":\"CycloneDX\",\"signature\":{\"value\":\"test\"}}";
            string sbomFilePath = Path.Combine(_tempDirectory, "bom.json");
            File.WriteAllText(sbomFilePath, sbomContent);

            var signature = new Signature { Value = "test" };
            string cleanedSbom = "{\"bomFormat\":\"CycloneDX\"}";

            _mockSignatureHelper.Setup(x => x.ExtractSignature(sbomContent))
                .Returns(signature);
            _mockSignatureHelper.Setup(x => x.RemoveSignature(sbomContent))
                .Returns(cleanedSbom);
            _mockCertificateHelper.Setup(x => x.VerifySignature(cleanedSbom, "test"))
                .Returns(true);

            // Act
            _jsonFileHelper.ReadSBOMFile(sbomFilePath, out _);

            // Assert - Verify ExtractSignature was called with the file content
            _mockSignatureHelper.Verify(x => x.ExtractSignature(sbomContent), Times.Once);
        }

        [Test]
        public void SignSBOMFile_WithUnicodeCharacters_HandlesCorrectly()
        {
            // Arrange
            string originalSbom = "{\"name\":\"测试\",\"description\":\"Тест\",\"emoji\":\"🔐\"}";
            string cleanedSbom = originalSbom;
            byte[] signatureBytes = { 0xAA, 0xBB, 0xCC, 0xDD };

            _appSettings.bomcontent = originalSbom;

            _mockSignatureHelper.Setup(x => x.RemoveSignature(originalSbom))
                .Returns(cleanedSbom);
            _mockCertificateHelper.Setup(x => x.SignCertificate(cleanedSbom))
                .Returns(signatureBytes);

            // Act
            string result = _jsonFileHelper.SignSBOMFile();

            // Assert
            Assert.That(result, Is.Not.Null);
            var jsonDoc = JsonDocument.Parse(result);
            Assert.That(jsonDoc.RootElement.TryGetProperty("signature", out _), Is.True);
        }

        [Test]
        public void ReadSBOMFile_WithWhitespaceInFilePath_HandlesCorrectly()
        {
            // Arrange
            string sbomContent = "{\"data\":\"test\",\"signature\":{\"value\":\"sig\"}}";
            string sbomFilePath = Path.Combine(_tempDirectory, "  test file  .json");
            File.WriteAllText(sbomFilePath, sbomContent);

            var signature = new Signature { Value = "sig" };
            string cleanedSbom = "{\"data\":\"test\"}";

            _mockSignatureHelper.Setup(x => x.ExtractSignature(sbomContent))
                .Returns(signature);
            _mockSignatureHelper.Setup(x => x.RemoveSignature(sbomContent))
                .Returns(cleanedSbom);
            _mockCertificateHelper.Setup(x => x.VerifySignature(cleanedSbom, "sig"))
                .Returns(true);

            // Act & Assert
            Assert.DoesNotThrow(() => _jsonFileHelper.ReadSBOMFile(sbomFilePath, out bool isValid));
        }

        [Test]
        public void SignSBOMFile_WithEmptyObject_HandlesCorrectly()
        {
            // Arrange
            string originalSbom = "{}";
            string cleanedSbom = "{}";
            byte[] signatureBytes = { 1, 2, 3 };

            _appSettings.bomcontent = originalSbom;

            _mockSignatureHelper.Setup(x => x.RemoveSignature(originalSbom))
                .Returns(cleanedSbom);
            _mockCertificateHelper.Setup(x => x.SignCertificate(cleanedSbom))
                .Returns(signatureBytes);

            // Act
            string result = _jsonFileHelper.SignSBOMFile();

            // Assert
            Assert.That(result, Is.Not.Null);
            var jsonDoc = JsonDocument.Parse(result);
            Assert.That(jsonDoc.RootElement.TryGetProperty("signature", out _), Is.True);
        }

        [Test]
        public void ReadSBOMFile_WithNestedSignatureObject_ExtractsCorrectly()
        {
            // Arrange
            string sbomContent = "{\"metadata\":{\"version\":\"1.0\"},\"signature\":{\"algorithm\":\"RS256\",\"value\":\"xyz789\"}}";
            string sbomFilePath = Path.Combine(_tempDirectory, "nested.json");
            File.WriteAllText(sbomFilePath, sbomContent);

            var signature = new Signature { Value = "xyz789", Algorithm = "RS256" };
            string cleanedSbom = "{\"metadata\":{\"version\":\"1.0\"}}";

            _mockSignatureHelper.Setup(x => x.ExtractSignature(sbomContent))
                .Returns(signature);
            _mockSignatureHelper.Setup(x => x.RemoveSignature(sbomContent))
                .Returns(cleanedSbom);
            _mockCertificateHelper.Setup(x => x.VerifySignature(cleanedSbom, "xyz789"))
                .Returns(true);

            // Act
            _jsonFileHelper.ReadSBOMFile(sbomFilePath, out bool isValid);

            // Assert
            Assert.That(isValid, Is.True);
            _mockSignatureHelper.Verify(x => x.ExtractSignature(sbomContent), Times.Once);
        }

        [Test]
        public void SignSBOMFile_CallsRemoveSignatureBeforeVerifying()
        {
            // Arrange
            string originalSbom = "{\"data\":\"value\",\"signature\":{\"old\":\"sig\"}}";
            string cleanedSbom = "{\"data\":\"value\"}";
            byte[] signatureBytes = { 5, 6, 7 };

            _appSettings.bomcontent = originalSbom;

            _mockSignatureHelper.Setup(x => x.RemoveSignature(originalSbom))
                .Returns(cleanedSbom);
            _mockCertificateHelper.Setup(x => x.SignCertificate(cleanedSbom))
                .Returns(signatureBytes);

            // Act
            _jsonFileHelper.SignSBOMFile();

            // Assert - Verify RemoveSignature was called first
            _mockSignatureHelper.Verify(x => x.RemoveSignature(originalSbom), Times.Once);
        }

        [Test]
        public void ReadSBOMFile_WithSpecialCharactersInPath_HandlesCorrectly()
        {
            // Arrange
            string sbomContent = "{\"test\":\"data\",\"signature\":{\"value\":\"sig\"}}";
            string sbomFilePath = Path.Combine(_tempDirectory, "test-file_v1.0.json");
            File.WriteAllText(sbomFilePath, sbomContent);

            var signature = new Signature { Value = "sig" };
            string cleanedSbom = "{\"test\":\"data\"}";

            _mockSignatureHelper.Setup(x => x.ExtractSignature(sbomContent))
                .Returns(signature);
            _mockSignatureHelper.Setup(x => x.RemoveSignature(sbomContent))
                .Returns(cleanedSbom);
            _mockCertificateHelper.Setup(x => x.VerifySignature(cleanedSbom, "sig"))
                .Returns(true);

            // Act & Assert
            Assert.DoesNotThrow(() => _jsonFileHelper.ReadSBOMFile(sbomFilePath, out bool isValid));
        }

        [Test]
        public void SignSBOMFile_ProducesValidJsonOutput()
        {
            // Arrange
            string originalSbom = "{\"name\":\"test\"}";
            string cleanedSbom = "{\"name\":\"test\"}";
            byte[] signatureBytes = { 1, 2, 3 };

            _appSettings.bomcontent = originalSbom;

            _mockSignatureHelper.Setup(x => x.RemoveSignature(originalSbom))
                .Returns(cleanedSbom);
            _mockCertificateHelper.Setup(x => x.SignCertificate(cleanedSbom))
                .Returns(signatureBytes);

            // Act
            string result = _jsonFileHelper.SignSBOMFile();

            // Assert - Verify result is valid JSON
            Assert.DoesNotThrow(() => JsonDocument.Parse(result));
        }

        #endregion
    }
}
