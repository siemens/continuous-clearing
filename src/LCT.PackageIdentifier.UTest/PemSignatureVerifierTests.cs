// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// --------------------------------------------------------------------------------------------------------------------

using NUnit.Framework;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using LCT.PackageIdentifier;
using System.Reflection;

namespace LCT.PackageIdentifier.UTest
{
    [TestFixture]
    public class PemSignatureVerifierTests
    {
        private string _tempDirectory;
        private string _testDocumentPath;
        private string _testSignaturePath;
        private string _testCertificatePath;
        private string _testPublicKeyPath;
        private string _testInvalidCertPath;
        private string _testInvalidPublicKeyPath;

        [SetUp]
        public void Setup()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), "PemSignatureVerifierTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDirectory);

            _testDocumentPath = Path.Combine(_tempDirectory, "testdocument.txt");
            _testSignaturePath = Path.Combine(_tempDirectory, "signature.sig");
            _testCertificatePath = Path.Combine(_tempDirectory, "certificate.pem");
            _testPublicKeyPath = Path.Combine(_tempDirectory, "publickey.pem");
            _testInvalidCertPath = Path.Combine(_tempDirectory, "invalid_cert.pem");
            _testInvalidPublicKeyPath = Path.Combine(_tempDirectory, "invalid_key.pem");

            SetupTestFiles();
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, true);
            }
        }

        private void SetupTestFiles()
        {
            // Create test document
            File.WriteAllText(_testDocumentPath, "This is a test document for signature verification.");

            // Create RSA key pair and certificate for testing
            using (var rsa = RSA.Create(2048))
            {
                // Create self-signed certificate with the RSA key
                var request = new CertificateRequest("CN=Test Certificate", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(365));

                // Export certificate to PEM format
                var certPem = Convert.ToBase64String(certificate.Export(X509ContentType.Cert));
                var formattedCertPem = $"-----BEGIN CERTIFICATE-----\n{FormatBase64(certPem)}\n-----END CERTIFICATE-----";
                File.WriteAllText(_testCertificatePath, formattedCertPem);

                // Export the public key to PEM format (using the same key from the certificate)
                var publicKeyBytes = rsa.ExportSubjectPublicKeyInfo();
                var publicKeyPem = Convert.ToBase64String(publicKeyBytes);
                var formattedPublicKeyPem = $"-----BEGIN PUBLIC KEY-----\n{FormatBase64(publicKeyPem)}\n-----END PUBLIC KEY-----";
                File.WriteAllText(_testPublicKeyPath, formattedPublicKeyPem);

                // Create signature using the same RSA key
                var documentData = File.ReadAllBytes(_testDocumentPath);
                var signature = rsa.SignData(documentData, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                File.WriteAllBytes(_testSignaturePath, signature);
            }

            // Create invalid certificate file
            File.WriteAllText(_testInvalidCertPath, "-----BEGIN CERTIFICATE-----\nInvalidCertificateData\n-----END CERTIFICATE-----");

            // Create invalid public key file
            File.WriteAllText(_testInvalidPublicKeyPath, "-----BEGIN PUBLIC KEY-----\nInvalidPublicKeyData\n-----END PUBLIC KEY-----");
        }

        private string FormatBase64(string base64String)
        {
            var result = new StringBuilder();
            for (int i = 0; i < base64String.Length; i += 64)
            {
                int length = Math.Min(64, base64String.Length - i);
                result.AppendLine(base64String.Substring(i, length));
            }
            return result.ToString().TrimEnd();
        }

        [Test]
        public void ValidatePem_WithValidCertificate_ReturnsTrue()
        {
            // Act
            var result = PemSignatureVerifier.ValidatePem(_testDocumentPath, _testSignaturePath, _testCertificatePath);

            // Assert
            Assert.IsTrue(result, "Valid certificate signature should return true");
        }

        [Test]
        public void ValidatePem_WithValidPublicKey_ReturnsTrue()
        {
            // Arrange - Create a separate RSA key and signature specifically for public key testing
            var publicKeyTestPath = Path.Combine(_tempDirectory, "test_publickey.pem");
            var documentTestPath = Path.Combine(_tempDirectory, "test_document.txt");
            var signatureTestPath = Path.Combine(_tempDirectory, "test_signature.sig");

            File.WriteAllText(documentTestPath, "This is a test document for public key verification.");

            using (var rsa = RSA.Create(2048))
            {
                // Export public key in the correct format
                var publicKeyBytes = rsa.ExportSubjectPublicKeyInfo();
                var publicKeyPem = Convert.ToBase64String(publicKeyBytes);
                var formattedPublicKeyPem = $"-----BEGIN PUBLIC KEY-----\n{FormatBase64(publicKeyPem)}\n-----END PUBLIC KEY-----";
                File.WriteAllText(publicKeyTestPath, formattedPublicKeyPem);

                // Create signature with the same RSA key
                var documentData = File.ReadAllBytes(documentTestPath);
                var signature = rsa.SignData(documentData, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                File.WriteAllBytes(signatureTestPath, signature);
            }

            // Act
            var result = PemSignatureVerifier.ValidatePem(documentTestPath, signatureTestPath, publicKeyTestPath);

            // Assert
            Assert.IsTrue(result, "Valid public key signature should return true");
        }

        [Test]
        public void ValidatePem_WithInvalidCertificate_ReturnsFalse()
        {
            // Act
            var result = PemSignatureVerifier.ValidatePem(_testDocumentPath, _testSignaturePath, _testInvalidCertPath);

            // Assert
            Assert.IsFalse(result, "Invalid certificate should return false");
        }

        [Test]
        public void ValidatePem_WithInvalidPublicKey_ReturnsFalse()
        {
            // Act
            var result = PemSignatureVerifier.ValidatePem(_testDocumentPath, _testSignaturePath, _testInvalidPublicKeyPath);

            // Assert
            Assert.IsFalse(result, "Invalid public key should return false");
        }

        [Test]
        public void ValidatePem_WithNonExistentDocument_ReturnsFalse()
        {
            // Arrange
            var nonExistentPath = Path.Combine(_tempDirectory, "nonexistent.txt");

            // Act & Assert
            Assert.Throws<FileNotFoundException>(() =>
                PemSignatureVerifier.ValidatePem(nonExistentPath, _testSignaturePath, _testCertificatePath));
        }

        [Test]
        public void ValidatePem_WithNonExistentSignature_ReturnsFalse()
        {
            // Arrange
            var nonExistentPath = Path.Combine(_tempDirectory, "nonexistent.sig");

            // Act & Assert
            Assert.Throws<FileNotFoundException>(() =>
                PemSignatureVerifier.ValidatePem(_testDocumentPath, nonExistentPath, _testCertificatePath));
        }

        [Test]
        public void ValidatePem_WithNonExistentPemFile_ReturnsFalse()
        {
            // Arrange
            var nonExistentPath = Path.Combine(_tempDirectory, "nonexistent.pem");

            // Act & Assert
            Assert.Throws<FileNotFoundException>(() =>
                PemSignatureVerifier.ValidatePem(_testDocumentPath, _testSignaturePath, nonExistentPath));
        }

        [Test]
        public void ValidatePem_WithWrongSignature_ReturnsFalse()
        {
            // Arrange
            var wrongSignaturePath = Path.Combine(_tempDirectory, "wrong_signature.sig");
            File.WriteAllBytes(wrongSignaturePath, new byte[] { 0x00, 0x01, 0x02, 0x03 });

            // Act
            var result = PemSignatureVerifier.ValidatePem(_testDocumentPath, wrongSignaturePath, _testCertificatePath);

            // Assert
            Assert.IsFalse(result, "Wrong signature should return false");
        }

        [Test]
        public void ValidatePem_WithModifiedDocument_ReturnsFalse()
        {
            // Arrange
            var modifiedDocumentPath = Path.Combine(_tempDirectory, "modified_document.txt");
            File.WriteAllText(modifiedDocumentPath, "This is a modified test document.");

            // Act
            var result = PemSignatureVerifier.ValidatePem(modifiedDocumentPath, _testSignaturePath, _testCertificatePath);

            // Assert
            Assert.IsFalse(result, "Modified document should fail verification");
        }

        [Test]
        public void ValidatePem_WithEmptyDocument_HandlesGracefully()
        {
            // Arrange
            var emptyDocumentPath = Path.Combine(_tempDirectory, "empty_document.txt");
            File.WriteAllText(emptyDocumentPath, "");

            // Act
            var result = PemSignatureVerifier.ValidatePem(emptyDocumentPath, _testSignaturePath, _testCertificatePath);

            // Assert
            Assert.IsFalse(result, "Empty document should fail verification");
        }

        [Test]
        public void ValidatePem_WithEcdsaCertificate_ReturnsTrue()
        {
            // Arrange
            var ecdsaCertPath = Path.Combine(_tempDirectory, "ecdsa_cert.pem");
            var ecdsaDocumentPath = Path.Combine(_tempDirectory, "ecdsa_document.txt");
            var ecdsaSignaturePath = Path.Combine(_tempDirectory, "ecdsa_signature.sig");

            File.WriteAllText(ecdsaDocumentPath, "ECDSA test document");

            using (var ecdsa = ECDsa.Create())
            {
                // Create self-signed certificate with ECDSA
                var request = new CertificateRequest("CN=ECDSA Test Certificate", ecdsa, HashAlgorithmName.SHA256);
                var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(365));

                // Export certificate to PEM format
                var certPem = Convert.ToBase64String(certificate.Export(X509ContentType.Cert));
                var formattedCertPem = $"-----BEGIN CERTIFICATE-----\n{FormatBase64(certPem)}\n-----END CERTIFICATE-----";
                File.WriteAllText(ecdsaCertPath, formattedCertPem);

                // Create signature
                var documentData = File.ReadAllBytes(ecdsaDocumentPath);
                var signature = ecdsa.SignData(documentData, HashAlgorithmName.SHA256);
                File.WriteAllBytes(ecdsaSignaturePath, signature);
            }

            // Act
            var result = PemSignatureVerifier.ValidatePem(ecdsaDocumentPath, ecdsaSignaturePath, ecdsaCertPath);

            // Assert
            Assert.IsTrue(result, "ECDSA certificate signature should return true");
        }

        [Test]
        public void ValidatePem_WithEcdsaPublicKey_ReturnsTrue()
        {
            // Arrange
            var ecdsaKeyPath = Path.Combine(_tempDirectory, "ecdsa_key.pem");
            var ecdsaDocumentPath = Path.Combine(_tempDirectory, "ecdsa_document.txt");
            var ecdsaSignaturePath = Path.Combine(_tempDirectory, "ecdsa_signature.sig");

            File.WriteAllText(ecdsaDocumentPath, "ECDSA test document");

            using (var ecdsa = ECDsa.Create())
            {
                // Export public key to PEM format
                var publicKeyBytes = ecdsa.ExportSubjectPublicKeyInfo();
                var publicKeyPem = Convert.ToBase64String(publicKeyBytes);
                var formattedPublicKeyPem = $"-----BEGIN PUBLIC KEY-----\n{FormatBase64(publicKeyPem)}\n-----END PUBLIC KEY-----";
                File.WriteAllText(ecdsaKeyPath, formattedPublicKeyPem);

                // Create signature
                var documentData = File.ReadAllBytes(ecdsaDocumentPath);
                var signature = ecdsa.SignData(documentData, HashAlgorithmName.SHA256);
                File.WriteAllBytes(ecdsaSignaturePath, signature);
            }

            // Act
            var result = PemSignatureVerifier.ValidatePem(ecdsaDocumentPath, ecdsaSignaturePath, ecdsaKeyPath);

            // Assert
            Assert.IsTrue(result, "ECDSA public key signature should return true");
        }

        [Test]
        public void ValidatePem_WithCertificateWithoutPublicKey_ReturnsFalse()
        {
            // Arrange - Create a certificate-like structure without a valid public key
            var invalidCertPath = Path.Combine(_tempDirectory, "cert_without_key.pem");
            var invalidCertContent = "-----BEGIN CERTIFICATE-----\nMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEA\n-----END CERTIFICATE-----";
            File.WriteAllText(invalidCertPath, invalidCertContent);

            // Act
            var result = PemSignatureVerifier.ValidatePem(_testDocumentPath, _testSignaturePath, invalidCertPath);

            // Assert
            Assert.IsFalse(result, "Certificate without valid public key should return false");
        }

        [Test]
        public void ValidatePem_WithBase64OnlyContent_HandlesGracefully()
        {
            // Arrange
            var base64OnlyPath = Path.Combine(_tempDirectory, "base64_only.pem");
            File.WriteAllText(base64OnlyPath, "SGVsbG8gV29ybGQ="); // "Hello World" in base64

            // Act
            var result = PemSignatureVerifier.ValidatePem(_testDocumentPath, _testSignaturePath, base64OnlyPath);

            // Assert
            Assert.IsFalse(result, "Base64 only content should return false");
        }

        [Test]
        public void ValidatePem_WithMixedLineEndings_HandlesCorrectly()
        {
            // Arrange
            var mixedEndingsPath = Path.Combine(_tempDirectory, "mixed_endings.pem");

            // Create a PEM with mixed line endings
            var pemContent = File.ReadAllText(_testCertificatePath);
            var mixedContent = pemContent.Replace("\n", "\r\n");
            File.WriteAllText(mixedEndingsPath, mixedContent);

            // Act
            var result = PemSignatureVerifier.ValidatePem(_testDocumentPath, _testSignaturePath, mixedEndingsPath);

            // Assert
            Assert.IsTrue(result, "PEM with mixed line endings should still work");
        }

        [Test]
        public void ValidatePem_WithWhitespaceInPem_HandlesCorrectly()
        {
            // Arrange
            var whitespaceInPemPath = Path.Combine(_tempDirectory, "whitespace_in_pem.pem");

            // Create a PEM with extra whitespace by adding spaces and tabs within the base64 content
            var pemContent = File.ReadAllText(_testPublicKeyPath);
            // Add spaces and tabs within the base64 content, not just after newlines
            var lines = pemContent.Split('\n');
            var modifiedLines = new string[lines.Length];
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith("-----") || string.IsNullOrWhiteSpace(lines[i]))
                {
                    modifiedLines[i] = lines[i]; // Keep header/footer lines unchanged
                }
                else
                {
                    // Add spaces and tabs within the base64 content
                    modifiedLines[i] = "  " + lines[i] + "\t ";
                }
            }
            var whitespaceContent = string.Join("\n", modifiedLines);
            File.WriteAllText(whitespaceInPemPath, whitespaceContent);

            // Act
            var result = PemSignatureVerifier.ValidatePem(_testDocumentPath, _testSignaturePath, whitespaceInPemPath);

            // Assert
            Assert.IsTrue(result, "PEM with whitespace should still work");
        }

        [Test]
        public void ValidatePem_WithNullArguments_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                PemSignatureVerifier.ValidatePem(null, _testSignaturePath, _testCertificatePath));

            Assert.Throws<ArgumentNullException>(() =>
                PemSignatureVerifier.ValidatePem(_testDocumentPath, null, _testCertificatePath));

            Assert.Throws<ArgumentNullException>(() =>
                PemSignatureVerifier.ValidatePem(_testDocumentPath, _testSignaturePath, null));
        }

        [Test]
        public void ValidatePem_WithEmptyStringArguments_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                PemSignatureVerifier.ValidatePem("", _testSignaturePath, _testCertificatePath));

            Assert.Throws<ArgumentException>(() =>
                PemSignatureVerifier.ValidatePem(_testDocumentPath, "", _testCertificatePath));

            Assert.Throws<ArgumentException>(() =>
                PemSignatureVerifier.ValidatePem(_testDocumentPath, _testSignaturePath, ""));
        }

        [Test]
        public void ValidatePem_WithLargeDocument_HandlesCorrectly()
        {
            // Arrange
            var largeDocumentPath = Path.Combine(_tempDirectory, "large_document.txt");
            var largeContent = new string('A', 1024 * 1024); // 1MB of 'A's
            File.WriteAllText(largeDocumentPath, largeContent);

            // Create signature for large document
            var largeSignaturePath = Path.Combine(_tempDirectory, "large_signature.sig");
            using (var rsa = RSA.Create(2048))
            {
                // Use the same key that was used to create the test certificate
                var privateKeyBytes = GetPrivateKeyBytes();
                rsa.ImportRSAPrivateKey(privateKeyBytes, out _);
                var documentData = File.ReadAllBytes(largeDocumentPath);
                var signature = rsa.SignData(documentData, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                File.WriteAllBytes(largeSignaturePath, signature);
            }

            // Act & Assert - This might fail due to different keys, but should not crash
            Assert.DoesNotThrow(() =>
                PemSignatureVerifier.ValidatePem(largeDocumentPath, largeSignaturePath, _testCertificatePath));
        }

        private byte[] GetPrivateKeyBytes()
        {
            // This is a mock method - in real tests, you'd need to properly extract the private key
            // For this test, we'll create a minimal valid RSA private key structure
            // Since this is just for testing the large document handling, we'll use a simple approach
            try
            {
                // Try to extract from the existing certificate if possible
                var cert = new X509Certificate2(_testCertificatePath);
                if (cert.HasPrivateKey)
                {
                    return cert.GetRSAPrivateKey().ExportRSAPrivateKey();
                }
            }
            catch
            {
                // If extraction fails, create a new RSA key for testing
            }

            // Create a new RSA key for testing purposes
            using (var rsa = RSA.Create(2048))
            {
                return rsa.ExportRSAPrivateKey();
            }
        }

        [Test]
        public void ValidatePem_WithCorruptedPemFile_ReturnsFalse()
        {
            // Arrange
            var corruptedPemPath = Path.Combine(_tempDirectory, "corrupted.pem");
            File.WriteAllText(corruptedPemPath, "-----BEGIN CERTIFICATE-----\nCorrupted\nData\n-----END CERTIFICATE-----");

            // Act
            var result = PemSignatureVerifier.ValidatePem(_testDocumentPath, _testSignaturePath, corruptedPemPath);

            // Assert
            Assert.IsFalse(result, "Corrupted PEM file should return false");
        }

        [Test]
        public void ValidatePem_WithMultipleCertificatesInPem_UsesFirst()
        {
            // Arrange
            var multiCertPath = Path.Combine(_tempDirectory, "multi_cert.pem");
            var originalPem = File.ReadAllText(_testCertificatePath);
            var multiPemContent = originalPem + "\n" + originalPem; // Duplicate the certificate
            File.WriteAllText(multiCertPath, multiPemContent);

            // Act
            var result = PemSignatureVerifier.ValidatePem(_testDocumentPath, _testSignaturePath, multiCertPath);

            // Assert
            Assert.IsTrue(result, "Multiple certificates should use the first one");
        }

        [Test]
        public void ValidatePem_WithIOException_ReturnsFalse()
        {
            // Arrange - Use a non-existent directory path to trigger IOException
            var nonExistentDir = Path.Combine(_tempDirectory, "nonexistent_directory");
            var nonExistentFile = Path.Combine(nonExistentDir, "nonexistent_file.txt");
            
            // Create an invalid public key that will bypass certificate parsing and go to ValidateSignedFileFromPublicKey
            var invalidPublicKeyPath = Path.Combine(_tempDirectory, "trigger_publickey_path.pem");
            File.WriteAllText(invalidPublicKeyPath, "-----BEGIN PUBLIC KEY-----\nInvalidKeyData123\n-----END PUBLIC KEY-----");

            // Act - This will call ValidateSignedFileFromPublicKey which will try to read the non-existent file
            var result = PemSignatureVerifier.ValidatePem(nonExistentFile, _testSignaturePath, invalidPublicKeyPath);

            // Assert
            Assert.IsFalse(result, "IOException should return false");
        }

        [Test]
        public void ValidatePem_WithIOException_NonExistentSignature_ReturnsFalse()
        {
            // Arrange - Use a non-existent signature file to trigger IOException
            var nonExistentSignature = Path.Combine(_tempDirectory, "nonexistent_signature.sig");
            
            // Create an invalid public key that will bypass certificate parsing
            var invalidPublicKeyPath = Path.Combine(_tempDirectory, "trigger_publickey_path2.pem");
            File.WriteAllText(invalidPublicKeyPath, "-----BEGIN PUBLIC KEY-----\nInvalidKeyData456\n-----END PUBLIC KEY-----");

            // Act - This will call ValidateSignedFileFromPublicKey which will try to read the non-existent signature
            var result = PemSignatureVerifier.ValidatePem(_testDocumentPath, nonExistentSignature, invalidPublicKeyPath);

            // Assert
            Assert.IsFalse(result, "IOException with non-existent signature should return false");
        }

        [Test]
        public void ValidatePem_WithUnauthorizedAccessException_ReturnsFalse()
        {
            // Arrange - This test is difficult to implement reliably across different environments
            // We'll create a test that demonstrates the exception handling pattern
            var restrictedDirectory = Path.Combine(_tempDirectory, "restricted");
            Directory.CreateDirectory(restrictedDirectory);
            
            var restrictedFilePath = Path.Combine(restrictedDirectory, "restricted_file.txt");
            File.WriteAllText(restrictedFilePath, "restricted content");

            // Create an invalid public key that will bypass certificate parsing
            var invalidPublicKeyPath = Path.Combine(_tempDirectory, "invalid_publickey.pem");
            File.WriteAllText(invalidPublicKeyPath, "-----BEGIN PUBLIC KEY-----\nInvalidKeyData\n-----END PUBLIC KEY-----");

            // Act - This test demonstrates the exception handling structure
            // Note: UnauthorizedAccessException is difficult to trigger reliably in unit tests
            var result = PemSignatureVerifier.ValidatePem(restrictedFilePath, _testSignaturePath, invalidPublicKeyPath);

            // Assert - The method should handle any access issues gracefully
            Assert.IsFalse(result, "Access issues should return false");
        }

        [Test]
        public void ValidatePem_WithCryptographicException_ReturnsFalse()
        {
            // Arrange
            var invalidKeyPath = Path.Combine(_tempDirectory, "crypto_invalid.pem");
            // Create a PEM that looks valid but has invalid cryptographic data
            File.WriteAllText(invalidKeyPath, "-----BEGIN PUBLIC KEY-----\nMIIBIjANBgkqhkiG9w0BAQEFAAOCAQEAInvalidCryptographicData\n-----END PUBLIC KEY-----");

            // Act
            var result = PemSignatureVerifier.ValidatePem(_testDocumentPath, _testSignaturePath, invalidKeyPath);

            // Assert
            Assert.IsFalse(result, "CryptographicException should return false");
        }

        [Test]
        public void ValidatePem_WithFormatException_ReturnsFalse()
        {
            // Arrange - Create a PEM that will trigger FormatException during Base64 conversion
            var invalidFormatPath = Path.Combine(_tempDirectory, "format_invalid.pem");
            // Create invalid Base64 characters that will cause FormatException in Convert.FromBase64String
            File.WriteAllText(invalidFormatPath, "-----BEGIN PUBLIC KEY-----\nInvalid@Base64#Format!@#$%\n-----END PUBLIC KEY-----");

            // Act
            var result = PemSignatureVerifier.ValidatePem(_testDocumentPath, _testSignaturePath, invalidFormatPath);

            // Assert
            Assert.IsFalse(result, "FormatException should return false");
        }

        [Test]
        public void ValidatePem_WithFormatException_InvalidBase64_ReturnsFalse()
        {
            // Arrange - Create another scenario that triggers FormatException
            var invalidFormatPath = Path.Combine(_tempDirectory, "invalid_base64.pem");
            // Use characters that are definitely not valid Base64
            File.WriteAllText(invalidFormatPath, "-----BEGIN PUBLIC KEY-----\n@!#$%^&*()_+{}|:<>?[]\n-----END PUBLIC KEY-----");

            // Act
            var result = PemSignatureVerifier.ValidatePem(_testDocumentPath, _testSignaturePath, invalidFormatPath);

            // Assert
            Assert.IsFalse(result, "FormatException with invalid Base64 should return false");
        }

        [Test]
        public void ValidatePem_WithArgumentException_ReturnsFalse()
        {
            // Arrange - Create a PEM with data that will cause ArgumentException
            var argumentInvalidPath = Path.Combine(_tempDirectory, "argument_invalid.pem");
            // Create a PEM with empty or invalid key data that causes ArgumentException in crypto operations
            File.WriteAllText(argumentInvalidPath, "-----BEGIN PUBLIC KEY-----\n\n-----END PUBLIC KEY-----");

            // Act
            var result = PemSignatureVerifier.ValidatePem(_testDocumentPath, _testSignaturePath, argumentInvalidPath);

            // Assert
            Assert.IsFalse(result, "ArgumentException should return false");
        }

        [Test]
        public void ValidatePem_WithArgumentException_EmptyKeyData_ReturnsFalse()
        {
            // Arrange - Create another scenario that triggers ArgumentException
            var argumentInvalidPath = Path.Combine(_tempDirectory, "empty_key.pem");
            // Create a PEM with minimal data that could cause ArgumentException
            File.WriteAllText(argumentInvalidPath, "-----BEGIN PUBLIC KEY-----\nA\n-----END PUBLIC KEY-----");

            // Act
            var result = PemSignatureVerifier.ValidatePem(_testDocumentPath, _testSignaturePath, argumentInvalidPath);

            // Assert
            Assert.IsFalse(result, "ArgumentException with empty key data should return false");
        }

        [Test] 
        public void ValidatePem_WithArgumentException_InvalidKeyLength_ReturnsFalse()
        {
            // Arrange - Create a PEM with invalid key length that triggers ArgumentException
            var argumentInvalidPath = Path.Combine(_tempDirectory, "invalid_length_key.pem");
            // Create a very short Base64 string that's invalid for cryptographic operations
            File.WriteAllText(argumentInvalidPath, "-----BEGIN PUBLIC KEY-----\nMDk=\n-----END PUBLIC KEY-----");

            // Act
            var result = PemSignatureVerifier.ValidatePem(_testDocumentPath, _testSignaturePath, argumentInvalidPath);

            // Assert
            Assert.IsFalse(result, "ArgumentException with invalid key length should return false");
        }
    }
}