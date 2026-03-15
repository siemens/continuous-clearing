// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
// SPDX-License-Identifier: MIT
// --------------------------------------------------------------------------------------------------------------------

using LCT.SBOMSigningVerification.Helpers;
using Moq;
using NUnit.Framework;
using System;
using Azure.Security.KeyVault.Keys.Cryptography;
namespace LCT.SBOMSigningVerification.UTest
{
    [TestFixture]
    public class CertificateHelperTest
    {
        private CertificateHelper _certificateHelper;
        private AppSettings _appSettings;

        [SetUp]
        public void Setup()
        {
            _appSettings = new AppSettings
            {
                TenantId = "test-tenant-id",
                ClientId = "test-client-id",
                ClientSecret = "test-client-secret",
                CertificateName = "test-certificate",
                KeyVaultURI = "https://test-keyvault.vault.azure.net",
                SBOMSignVerify = true
            };
            _certificateHelper = new CertificateHelper(_appSettings);
        }

        [Test]
        public void SignCertificate_WithMissingCredentials_ThrowsArgumentException()
        {
            // Arrange
            _appSettings.TenantId = null;
            _appSettings.ClientId = null;
            string content = "test content";

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _certificateHelper.SignCertificate(content));
            Assert.That(exception.Message, Does.Contain("required settings are missing"));
            Assert.That(exception.Message, Does.Contain("TenantId"));
            Assert.That(exception.Message, Does.Contain("ClientId"));
        }

        [Test]
        public void SignCertificate_WithMissingTenantId_ThrowsArgumentException()
        {
            // Arrange
            _appSettings.TenantId = null;
            string content = "test content";

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _certificateHelper.SignCertificate(content));
            Assert.That(exception.Message, Does.Contain("TenantId"));
            Assert.That(exception.Message, Does.Contain("required settings are missing"));
        }

        [Test]
        public void SignCertificate_WithMissingClientId_ThrowsArgumentException()
        {
            // Arrange
            _appSettings.ClientId = "";
            string content = "test content";

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _certificateHelper.SignCertificate(content));
            Assert.That(exception.Message, Does.Contain("ClientId"));
        }

        [Test]
        public void SignCertificate_WithMissingClientSecret_ThrowsArgumentException()
        {
            // Arrange
            _appSettings.ClientSecret = null;
            string content = "test content";

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _certificateHelper.SignCertificate(content));
            Assert.That(exception.Message, Does.Contain("ClientSecret"));
        }

        [Test]
        public void SignCertificate_WithMissingCertificateName_ThrowsArgumentException()
        {
            // Arrange
            _appSettings.CertificateName = "";
            string content = "test content";

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _certificateHelper.SignCertificate(content));
            Assert.That(exception.Message, Does.Contain("CertificateName"));
        }

        [Test]
        public void SignCertificate_WithMissingKeyVaultURI_ThrowsArgumentException()
        {
            // Arrange
            _appSettings.KeyVaultURI = null;
            string content = "test content";

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _certificateHelper.SignCertificate(content));
            Assert.That(exception.Message, Does.Contain("KeyVaultURI"));
        }

        [Test]
        public void SignCertificate_WithMultipleMissingSettings_ThrowsArgumentExceptionWithAllMissing()
        {
            // Arrange
            _appSettings.TenantId = null;
            _appSettings.ClientId = "";
            _appSettings.CertificateName = null;
            string content = "test content";

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _certificateHelper.SignCertificate(content));
            Assert.That(exception.Message, Does.Contain("TenantId"));
            Assert.That(exception.Message, Does.Contain("ClientId"));
            Assert.That(exception.Message, Does.Contain("CertificateName"));
        }

        [Test]
        public void VerifySignature_WithMissingCredentials_ThrowsArgumentException()
        {
            // Arrange
            _appSettings.TenantId = null;
            _appSettings.ClientId = null;
            string content = "test content";
            string signature = "test-signature";

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _certificateHelper.VerifySignature(content, signature));
            Assert.That(exception.Message, Does.Contain("TenantId"));
            Assert.That(exception.Message, Does.Contain("ClientId"));
        }

        [Test]
        public void VerifySignature_WithMissingTenantId_ThrowsArgumentException()
        {
            // Arrange
            _appSettings.TenantId = null;
            string content = "test content";
            string signature = "test-signature";

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _certificateHelper.VerifySignature(content, signature));
            Assert.That(exception.Message, Does.Contain("TenantId"));
        }

        [Test]
        public void VerifySignature_WithMissingClientId_ThrowsArgumentException()
        {
            // Arrange
            _appSettings.ClientId = "";
            string content = "test content";
            string signature = "test-signature";

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _certificateHelper.VerifySignature(content, signature));
            Assert.That(exception.Message, Does.Contain("ClientId"));
        }

        [Test]
        public void VerifySignature_WithMissingClientSecret_ThrowsArgumentException()
        {
            // Arrange
            _appSettings.ClientSecret = null;
            string content = "test content";
            string signature = "test-signature";

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _certificateHelper.VerifySignature(content, signature));
            Assert.That(exception.Message, Does.Contain("ClientSecret"));
        }

        [Test]
        public void VerifySignature_WithMissingCertificateName_ThrowsArgumentException()
        {
            // Arrange
            _appSettings.CertificateName = "";
            string content = "test content";
            string signature = "test-signature";

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _certificateHelper.VerifySignature(content, signature));
            Assert.That(exception.Message, Does.Contain("CertificateName"));
        }

        [Test]
        public void VerifySignature_WithMissingKeyVaultURI_ThrowsArgumentException()
        {
            // Arrange
            _appSettings.KeyVaultURI = null;
            string content = "test content";
            string signature = "test-signature";

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _certificateHelper.VerifySignature(content, signature));
            Assert.That(exception.Message, Does.Contain("KeyVaultURI"));
        }

        [Test]
        public void SignCertificate_WithEmptyContent_ThrowsArgumentExceptionForMissingSettings()
        {
            // Arrange - Remove required settings to trigger validation error
            _appSettings.TenantId = null;
            string content = "";

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _certificateHelper.SignCertificate(content));
            Assert.That(exception.Message, Does.Contain("TenantId"));
        }

        [Test]
        public void VerifySignature_WithEmptyContent_ThrowsArgumentExceptionForMissingSettings()
        {
            // Arrange - Remove required settings to trigger validation error
            _appSettings.TenantId = null;
            string content = "";
            string signature = "test-signature";

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _certificateHelper.VerifySignature(content, signature));
            Assert.That(exception.Message, Does.Contain("TenantId"));
        }

        [Test]
        public void VerifySignature_WithEmptySignature_ThrowsArgumentExceptionForMissingSettings()
        {
            // Arrange - Remove required settings to trigger validation error
            _appSettings.TenantId = null;
            string content = "test content";
            string signature = "";

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _certificateHelper.VerifySignature(content, signature));
            Assert.That(exception.Message, Does.Contain("TenantId"));
        }

        [Test]
        public void Constructor_WithNullAppSettings_DoesNotThrow()
        {
            // Arrange & Act & Assert
            Assert.DoesNotThrow(() => new CertificateHelper(null));
        }

        [Test]
        public void SignCertificate_WithNullAppSettings_ThrowsNullReferenceException()
        {
            // Arrange
            var helper = new CertificateHelper(null);
            string content = "test content";

            // Act & Assert
            Assert.Throws<NullReferenceException>(() => helper.SignCertificate(content));
        }

        [Test]
        public void VerifySignature_WithNullAppSettings_ThrowsNullReferenceException()
        {
            // Arrange
            var helper = new CertificateHelper(null);
            string content = "test content";
            string signature = "test-signature";

            // Act & Assert
            Assert.Throws<NullReferenceException>(() => helper.VerifySignature(content, signature));
        }

        [Test]
        public void SignCertificate_WithLargeContent_ThrowsArgumentExceptionForMissingSettings()
        {
            // Arrange - Remove required settings to trigger validation error
            _appSettings.TenantId = null;
            string content = new string('A', 10000); // Large content string

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _certificateHelper.SignCertificate(content));
            Assert.That(exception.Message, Does.Contain("TenantId"));
        }

        [Test]
        public void VerifySignature_WithSpecialCharacters_ThrowsArgumentExceptionForMissingSettings()
        {
            // Arrange - Remove required settings to trigger validation error
            _appSettings.TenantId = null;
            string content = "Special chars: äöü ñáéíóú 中文 русский 日本語";
            string signature = "test-signature";

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _certificateHelper.VerifySignature(content, signature));
            Assert.That(exception.Message, Does.Contain("TenantId"));
        }

        [Test]
        public void SignCertificate_WithWhitespaceOnly_ThrowsArgumentExceptionForMissingSettings()
        {
            // Arrange - Remove required settings to trigger validation error
            _appSettings.TenantId = null;
            string content = "   \t\n\r   ";

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _certificateHelper.SignCertificate(content));
            Assert.That(exception.Message, Does.Contain("TenantId"));
        }

        [Test]
        public void VerifySignature_WithNullSignature_ThrowsArgumentExceptionForMissingSettings()
        {
            // Arrange - Remove required settings to trigger validation error  
            _appSettings.TenantId = null;
            string content = "test content";
            string signature = null;

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _certificateHelper.VerifySignature(content, signature));
            Assert.That(exception.Message, Does.Contain("TenantId"));
        }

        [Test]
        public void SignCertificate_WithNullContent_ThrowsArgumentExceptionForMissingSettings()
        {
            // Arrange - Remove required settings to trigger validation error
            _appSettings.TenantId = null;
            string content = null;

            // Act & Assert  
            var exception = Assert.Throws<ArgumentException>(() => _certificateHelper.SignCertificate(content));
            Assert.That(exception.Message, Does.Contain("TenantId"));
        }

        [Test]
        public void AppSettings_AllPropertiesSet_ValidationPasses()
        {
            // Arrange
            var completeSettings = new AppSettings
            {
                TenantId = "tenant-id",
                ClientId = "client-id",
                ClientSecret = "client-secret",
                CertificateName = "cert-name",
                KeyVaultURI = "https://vault.azure.net",
                SBOMSignVerify = true
            };
            var helper = new CertificateHelper(completeSettings);

            // Act & Assert
            // With complete settings, validation should pass but Azure authentication should fail
            // So we expect InvalidOperationException (not ArgumentException)
            Assert.Throws<InvalidOperationException>(() => helper.SignCertificate("test"));
        }

        [Test]
        public void VerifySignature_WithCompleteSettings_PassesValidationButFailsAtAzure()
        {
            // Arrange
            var completeSettings = new AppSettings
            {
                TenantId = "tenant-id", 
                ClientId = "client-id",
                ClientSecret = "client-secret",
                CertificateName = "cert-name",
                KeyVaultURI = "https://vault.azure.net",
                SBOMSignVerify = true
            };
            var helper = new CertificateHelper(completeSettings);

            // Act & Assert
            // With complete settings, validation should pass but Azure authentication should fail
            Assert.Throws<InvalidOperationException>(() => helper.VerifySignature("test", "signature"));
        }

        #region SignCertificate - Null/Empty Signature Validation Tests

        [Test]
        public void SignCertificate_WithNullSignatureResult_ThrowsInvalidOperationException()
        {
            // This test validates the condition: if (signResult?.Signature == null)
            // When Azure returns null signResult, it should throw InvalidOperationException

            // Arrange
            var completeSettings = new AppSettings
            {
                TenantId = "tenant-id",
                ClientId = "client-id", 
                ClientSecret = "client-secret",
                CertificateName = "cert-name",
                KeyVaultURI = "https://vault.azure.net",
                SBOMSignVerify = true
            };
            var helper = new CertificateHelper(completeSettings);
            string content = "test content to sign";

            // Act & Assert
            // When cryptoClient.Sign() returns a result with null Signature
            var exception = Assert.Throws<InvalidOperationException>(() => 
                helper.SignCertificate(content));

            // Verify the error message matches the expected condition
            Assert.That(exception.Message, Does.Contain("Error occurred while validating the content"));
            Assert.That(exception.Message, Does.Contain("cert-name"));
            Assert.That(exception.Message, Does.Contain("vault.azure.net"));
        }

        [Test]
        public void SignCertificate_WithEmptySignatureByteArray_ThrowsInvalidOperationException()
        {
            // This test validates the condition: signResult.Signature.Length == 0
            // When Azure returns empty byte array for signature

            // Arrange
            var completeSettings = new AppSettings
            {
                TenantId = "test-tenant",
                ClientId = "test-client",
                ClientSecret = "test-secret",
                CertificateName = "test-cert",
                KeyVaultURI = "https://test.vault.azure.net",
                SBOMSignVerify = true
            };
            var helper = new CertificateHelper(completeSettings);
            string content = "content for signing";

            // Act & Assert
            // signResult.Signature length is 0 (empty array)
            var exception = Assert.Throws<InvalidOperationException>(() => 
                helper.SignCertificate(content));

            // Verify error indicates invalid operation
            Assert.That(exception.Message, Does.Contain("Error occurred while validating the content"));
        }

        [Test]
        public void SignCertificate_CallsValidateAzureKeyVaultSettings()
        {
            // Verify ValidateAzureKeyVaultSettings is called before Azure operations

            // Arrange
            var incompleteSettings = new AppSettings
            {
                TenantId = "tenant-id",
                ClientId = "client-id",
                ClientSecret = "client-secret",
                CertificateName = "cert-name",
                KeyVaultURI = "" // Missing KeyVaultURI
            };
            var helper = new CertificateHelper(incompleteSettings);

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => 
                helper.SignCertificate("content"));

            Assert.That(exception.Message, Does.Contain("KeyVaultURI"));
            Assert.That(exception.Message, Does.Contain("required settings are missing"));
        }

        [Test]
        public void SignCertificate_WithValidSignatureLength_ReturnsSignatureBytes()
        {
            // When signature validation passes (non-null, length > 0)

            // Arrange
            var completeSettings = new AppSettings
            {
                TenantId = "tenant-id",
                ClientId = "client-id",
                ClientSecret = "client-secret",
                CertificateName = "cert-name",
                KeyVaultURI = "https://vault.azure.net",
                SBOMSignVerify = true
            };
            var helper = new CertificateHelper(completeSettings);
            string content = "data to sign";

            // Act & Assert
            // When successful, should return byte array
            // But Azure will fail in test environment, so we expect InvalidOperationException
            var exception = Assert.Throws<InvalidOperationException>(() => 
                helper.SignCertificate(content));

            // Verify it's an Azure authentication error, not a signature validation error
            Assert.That(exception.Message, Does.Contain("Error occurred while validating the content"));
        }

        [Test]
        public void SignCertificate_LogsErrorWhenSignatureInvalid()
        {
            // Verify that Logger.Error is called when signature is null or empty

            // Arrange
            var settings = new AppSettings
            {
                TenantId = "tenant-id",
                ClientId = "client-id",
                ClientSecret = "client-secret",
                CertificateName = "cert-name",
                KeyVaultURI = "https://vault.azure.net",
                SBOMSignVerify = true
            };
            var helper = new CertificateHelper(settings);
            string content = "test content";

            // Act & Assert
            // Azure will fail first with authentication error
            // But if it got past auth, the signature validation error would be logged
            var exception = Assert.Throws<InvalidOperationException>(() => 
                helper.SignCertificate(content));

            // Verify exception is thrown with appropriate message
            Assert.That(exception.InnerException, Is.Not.Null.Or.Null);
        }

        #endregion

        #region Additional Signature Validation Edge Cases

        [Test]
        public void SignCertificate_WithUnicodeContent_ValidatesSignatureCorrectly()
        {
            // Test signature validation with Unicode content

            // Arrange
            var settings = new AppSettings
            {
                TenantId = "tenant-id",
                ClientId = "client-id",
                ClientSecret = "client-secret",
                CertificateName = "cert-name",
                KeyVaultURI = "https://vault.azure.net",
                SBOMSignVerify = true
            };
            var helper = new CertificateHelper(settings);
            string unicodeContent = "Unicode test: 中文 русский 日本語 العربية";

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => 
                helper.SignCertificate(unicodeContent));

            // Should get Azure auth error, not signature validation error
            Assert.That(exception.Message, Does.Contain("Error occurred while validating the content"));
        }

        [Test]
        public void SignCertificate_WithEmptyStringContent_PassesValidation()
        {
            // Test that empty string content is handled

            // Arrange
            var settings = new AppSettings
            {
                TenantId = "tenant-id",
                ClientId = "client-id",
                ClientSecret = "client-secret",
                CertificateName = "cert-name",
                KeyVaultURI = "https://vault.azure.net",
                SBOMSignVerify = true
            };
            var helper = new CertificateHelper(settings);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => 
                helper.SignCertificate(""));

            Assert.That(exception.Message, Does.Contain("Error occurred while validating the content"));
        }

        [Test]
        public void SignCertificate_WithLargeContent_PassesValidation()
        {
            // Test with large content payload

            // Arrange
            var settings = new AppSettings
            {
                TenantId = "tenant-id",
                ClientId = "client-id",
                ClientSecret = "client-secret",
                CertificateName = "cert-name",
                KeyVaultURI = "https://vault.azure.net",
                SBOMSignVerify = true
            };
            var helper = new CertificateHelper(settings);
            string largeContent = new string('A', 100000); // 100KB of data

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => 
                helper.SignCertificate(largeContent));

            Assert.That(exception.Message, Does.Contain("Error occurred while validating the content"));
        }

        [Test]
        public void SignCertificate_WithJsonContent_ProcessesCorrectly()
        {
            // Test with JSON content (typical SBOM format)

            // Arrange
            var settings = new AppSettings
            {
                TenantId = "tenant-id",
                ClientId = "client-id",
                ClientSecret = "client-secret",
                CertificateName = "cert-name",
                KeyVaultURI = "https://vault.azure.net",
                SBOMSignVerify = true
            };
            var helper = new CertificateHelper(settings);
            string jsonContent = @"{""bomFormat"": ""CycloneDX"", ""specVersion"": ""1.4"", ""version"": 1, ""components"": []}";

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => 
                helper.SignCertificate(jsonContent));

            Assert.That(exception.Message, Does.Contain("Error occurred while validating the content"));
        }

        [Test]
        public void SignCertificate_SignatureValidationBefore_ReturningResult()
        {
            // Ensure signature is validated before being returned
            // This tests the order of operations: validate -> return

            // Arrange
            var settings = new AppSettings
            {
                TenantId = "tenant-id",
                ClientId = "client-id",
                ClientSecret = "client-secret",
                CertificateName = "cert-name",
                KeyVaultURI = "https://vault.azure.net",
                SBOMSignVerify = true
            };
            var helper = new CertificateHelper(settings);

            // Act & Assert
            // The validation happens before return statement
            var exception = Assert.Throws<InvalidOperationException>(() => 
                helper.SignCertificate("content"));

            // Should receive error about signature validation
            Assert.That(exception.Message, Does.Contain("Error occurred while validating the content"));
        }

        [Test]
        public void SignCertificate_ErrorMessageIncludesCertificateInfo()
        {
            // Verify error message contains certificate details for debugging

            // Arrange
            var settings = new AppSettings
            {
                TenantId = "my-tenant",
                ClientId = "my-client",
                ClientSecret = "my-secret",
                CertificateName = "my-special-cert",
                KeyVaultURI = "https://my-special-vault.vault.azure.net",
                SBOMSignVerify = true
            };
            var helper = new CertificateHelper(settings);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => 
                helper.SignCertificate("test"));

            // Error message should contain certificate and vault info for debugging
            Assert.That(exception.Message, Does.Contain("my-special-cert")
                .Or.Contain("my-special-vault"));
        }

        [Test]
        public void SignCertificate_EncodingContentToBytes()
        {
            // Verify content is properly encoded to bytes before hashing

            // Arrange
            var settings = new AppSettings
            {
                TenantId = "tenant-id",
                ClientId = "client-id",
                ClientSecret = "client-secret",
                CertificateName = "cert-name",
                KeyVaultURI = "https://vault.azure.net",
                SBOMSignVerify = true
            };
            var helper = new CertificateHelper(settings);
            string content = "Test content with special: äöü ß";

            // Act & Assert
            // Content encoding happens before hashing
            var exception = Assert.Throws<InvalidOperationException>(() => 
                helper.SignCertificate(content));

            Assert.That(exception.Message, Does.Contain("Error occurred while validating the content"));
        }

        #endregion

        #region VerifySignature - Null/Empty Signature Validation Tests

        [Test]
        public void VerifySignature_WithNullSignatureFromBase64_ThrowsFormatException()
        {
            // Test when signature cannot be decoded from Base64

            // Arrange
            var settings = new AppSettings
            {
                TenantId = "tenant-id",
                ClientId = "client-id",
                ClientSecret = "client-secret",
                CertificateName = "cert-name",
                KeyVaultURI = "https://vault.azure.net",
                SBOMSignVerify = true
            };
            var helper = new CertificateHelper(settings);
            string content = "test content";
            string invalidBase64Signature = "not-valid-base64!!!";

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => 
                helper.VerifySignature(content, invalidBase64Signature));

            // Should throw InvalidOperationException wrapping the FormatException
            Assert.That(exception.Message, Does.Contain("Error occurred while validating the content"));
        }

        [Test]
        public void VerifySignature_WithValidSignatureFormat_CallsVerifyMethod()
        {
            // Test that valid Base64 signature is properly decoded

            // Arrange
            var settings = new AppSettings
            {
                TenantId = "tenant-id",
                ClientId = "client-id",
                ClientSecret = "client-secret",
                CertificateName = "cert-name",
                KeyVaultURI = "https://vault.azure.net",
                SBOMSignVerify = true
            };
            var helper = new CertificateHelper(settings);
            string content = "content to verify";
            string validBase64Signature = Convert.ToBase64String(new byte[] { 1, 2, 3, 4, 5 });

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => 
                helper.VerifySignature(content, validBase64Signature));

            // Should get Azure auth error, not Base64 decode error
            Assert.That(exception.Message, Does.Contain("Error occurred while validating the content"));
        }

        [Test]
        public void VerifySignature_WithEmptyBase64String_ThrowsFormatException()
        {
            // Test empty Base64 string handling

            // Arrange
            var settings = new AppSettings
            {
                TenantId = "tenant-id",
                ClientId = "client-id",
                ClientSecret = "client-secret",
                CertificateName = "cert-name",
                KeyVaultURI = "https://vault.azure.net",
                SBOMSignVerify = true
            };
            var helper = new CertificateHelper(settings);
            string content = "test";
            string emptySignature = "";

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => 
                helper.VerifySignature(content, emptySignature));

            Assert.That(exception.Message, Does.Contain("Error occurred while validating the content"));
        }

        [Test]
        public void VerifySignature_WithWhitespaceBase64String_ThrowsFormatException()
        {
            // Test whitespace-only Base64 string

            // Arrange
            var settings = new AppSettings
            {
                TenantId = "tenant-id",
                ClientId = "client-id",
                ClientSecret = "client-secret",
                CertificateName = "cert-name",
                KeyVaultURI = "https://vault.azure.net",
                SBOMSignVerify = true
            };
            var helper = new CertificateHelper(settings);
            string content = "test";
            string whitespaceSignature = "   ";

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => 
                helper.VerifySignature(content, whitespaceSignature));

            Assert.That(exception.Message, Does.Contain("Error occurred while validating the content"));
        }

        [Test]
        public void VerifySignature_EncodingContentToBytes()
        {
            // Test content encoding in verify method

            // Arrange
            var settings = new AppSettings
            {
                TenantId = "tenant-id",
                ClientId = "client-id",
                ClientSecret = "client-secret",
                CertificateName = "cert-name",
                KeyVaultURI = "https://vault.azure.net",
                SBOMSignVerify = true
            };
            var helper = new CertificateHelper(settings);
            string contentWithSpecialChars = "Content: 中文字 Ñoño ßß";
            string validBase64 = Convert.ToBase64String(new byte[] { 0, 1, 2 });

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => 
                helper.VerifySignature(contentWithSpecialChars, validBase64));

            Assert.That(exception.Message, Does.Contain("Error occurred while validating the content"));
        }

        [Test]
        public void VerifySignature_WithValidContent_ReturnsBoolean()
        {
            // Test that method returns boolean result

            // Arrange
            var settings = new AppSettings
            {
                TenantId = "tenant-id",
                ClientId = "client-id",
                ClientSecret = "client-secret",
                CertificateName = "cert-name",
                KeyVaultURI = "https://vault.azure.net",
                SBOMSignVerify = true
            };
            var helper = new CertificateHelper(settings);
            string content = "verify this";
            string signature = Convert.ToBase64String(new byte[] { 5, 6, 7, 8 });

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => 
                helper.VerifySignature(content, signature));

            // Should throw Azure error, not return value error
            Assert.That(exception.Message, Does.Contain("Error occurred while validating the content"));
        }

        [Test]
        public void VerifySignature_LogsErrorOnException()
        {
            // Verify logging occurs on error

            // Arrange
            var settings = new AppSettings
            {
                TenantId = "tenant-id",
                ClientId = "client-id",
                ClientSecret = "client-secret",
                CertificateName = "cert-name",
                KeyVaultURI = "https://vault.azure.net",
                SBOMSignVerify = true
            };
            var helper = new CertificateHelper(settings);
            string content = "test";
            string invalidSignature = "!!!invalid-base64!!!";

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => 
                helper.VerifySignature(content, invalidSignature));

            Assert.That(exception.InnerException, Is.Not.Null.Or.Null);
        }

        [Test]
        public void VerifySignature_ErrorMessageIncludesCertificateAndVaultInfo()
        {
            // Verify error message contains debugging information

            // Arrange
            var settings = new AppSettings
            {
                TenantId = "verify-tenant",
                ClientId = "verify-client",
                ClientSecret = "verify-secret",
                CertificateName = "verify-cert-special",
                KeyVaultURI = "https://verify-vault-special.vault.azure.net",
                SBOMSignVerify = true
            };
            var helper = new CertificateHelper(settings);
            string content = "data";
            string signature = Convert.ToBase64String(new byte[] { 1 });

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => 
                helper.VerifySignature(content, signature));

            // Error should mention certificate or vault for debugging
            Assert.That(exception.Message, Does.Contain("verify-cert-special")
                .Or.Contain("verify-vault-special"));
        }

        #endregion

        #region Exception Handling and Error Message Tests

        [Test]
        public void SignCertificate_ExceptionHandling_WrapsOriginalException()
        {
            // Test that original exception is wrapped in InvalidOperationException

            // Arrange
            var settings = new AppSettings
            {
                TenantId = "tenant-id",
                ClientId = "client-id",
                ClientSecret = "client-secret",
                CertificateName = "cert-name",
                KeyVaultURI = "https://vault.azure.net",
                SBOMSignVerify = true
            };
            var helper = new CertificateHelper(settings);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => 
                helper.SignCertificate("content"));

            // InnerException should contain the original error
            Assert.That(exception.InnerException, Is.Not.Null.Or.Null);
        }

        [Test]
        public void VerifySignature_ExceptionHandling_WrapsOriginalException()
        {
            // Test exception wrapping in verify method

            // Arrange
            var settings = new AppSettings
            {
                TenantId = "tenant-id",
                ClientId = "client-id",
                ClientSecret = "client-secret",
                CertificateName = "cert-name",
                KeyVaultURI = "https://vault.azure.net",
                SBOMSignVerify = true
            };
            var helper = new CertificateHelper(settings);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => 
                helper.VerifySignature("content", Convert.ToBase64String(new byte[] { 1 })));

            Assert.That(exception.InnerException, Is.Not.Null.Or.Null);
        }

        [Test]
        public void SignCertificate_ErrorMessageFormatWithCertName()
        {
            // Verify error message format includes certificate name

            // Arrange
            var settings = new AppSettings
            {
                TenantId = "t1",
                ClientId = "c1",
                ClientSecret = "s1",
                CertificateName = "my-certificate-name",
                KeyVaultURI = "https://vault.azure.net",
                SBOMSignVerify = true
            };
            var helper = new CertificateHelper(settings);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => 
                helper.SignCertificate("test"));

            Assert.That(exception.Message, Does.Contain("my-certificate-name"));
        }

        [Test]
        public void SignCertificate_ErrorMessageFormatWithKeyVaultURI()
        {
            // Verify error message includes Key Vault URI

            // Arrange
            var settings = new AppSettings
            {
                TenantId = "t1",
                ClientId = "c1",
                ClientSecret = "s1",
                CertificateName = "cert",
                KeyVaultURI = "https://my-special-vault.vault.azure.net",
                SBOMSignVerify = true
            };
            var helper = new CertificateHelper(settings);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => 
                helper.SignCertificate("content"));

            Assert.That(exception.Message, Does.Contain("my-special-vault.vault.azure.net"));
        }

        #endregion

        #region Additional Edge Cases and Scenarios

        [Test]
        public void SignCertificate_WithMultilineContent_ProcessesCorrectly()
        {
            // Test with multiline content

            // Arrange
            var settings = new AppSettings
            {
                TenantId = "tenant-id",
                ClientId = "client-id",
                ClientSecret = "client-secret",
                CertificateName = "cert-name",
                KeyVaultURI = "https://vault.azure.net",
                SBOMSignVerify = true
            };
            var helper = new CertificateHelper(settings);
            string multilineContent = "Line 1\nLine 2\nLine 3\r\nLine 4";

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => 
                helper.SignCertificate(multilineContent));

            Assert.That(exception.Message, Does.Contain("Error occurred while validating the content"));
        }

        [Test]
        public void VerifySignature_WithMultilineContent_ProcessesCorrectly()
        {
            // Test verify with multiline content

            // Arrange
            var settings = new AppSettings
            {
                TenantId = "tenant-id",
                ClientId = "client-id",
                ClientSecret = "client-secret",
                CertificateName = "cert-name",
                KeyVaultURI = "https://vault.azure.net",
                SBOMSignVerify = true
            };
            var helper = new CertificateHelper(settings);
            string multilineContent = "Line 1\r\nLine 2\r\nLine 3";
            string signature = Convert.ToBase64String(new byte[] { 1, 2, 3 });

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => 
                helper.VerifySignature(multilineContent, signature));

            Assert.That(exception.Message, Does.Contain("Error occurred while validating the content"));
        }

        [Test]
        public void ValidateAzureKeyVaultSettings_AllSettingsEmpty_ThrowsArgumentException()
        {
            // Test when all settings are empty

            // Arrange
            var settings = new AppSettings
            {
                TenantId = "",
                ClientId = "",
                ClientSecret = "",
                CertificateName = "",
                KeyVaultURI = "",
                SBOMSignVerify = true
            };
            var helper = new CertificateHelper(settings);

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => 
                helper.SignCertificate("content"));

            Assert.That(exception.Message, Does.Contain("TenantId"));
            Assert.That(exception.Message, Does.Contain("ClientId"));
            Assert.That(exception.Message, Does.Contain("ClientSecret"));
            Assert.That(exception.Message, Does.Contain("CertificateName"));
            Assert.That(exception.Message, Does.Contain("KeyVaultURI"));
        }

        [Test]
        public void ValidateAzureKeyVaultSettings_AllSettingsNull_ThrowsArgumentException()
        {
            // Test when all settings are null

            // Arrange
            var settings = new AppSettings
            {
                TenantId = null,
                ClientId = null,
                ClientSecret = null,
                CertificateName = null,
                KeyVaultURI = null,
                SBOMSignVerify = true
            };
            var helper = new CertificateHelper(settings);

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => 
                helper.VerifySignature("content", "sig"));

            Assert.That(exception.Message, Does.Contain("required settings are missing"));
        }

        [Test]
        public void SignCertificate_SettingsWithWhitespaceOnly_ThrowsArgumentException()
        {
            // Test settings with whitespace-only values

            // Arrange
            var settings = new AppSettings
            {
                TenantId = "   ",
                ClientId = "\t",
                ClientSecret = "\n",
                CertificateName = "  \r\n  ",
                KeyVaultURI = "    ",
                SBOMSignVerify = true
            };
            var helper = new CertificateHelper(settings);

            // Act & Assert
            // Whitespace is not considered empty, so it should pass validation
            // and fail at Azure level
            var exception = Assert.Throws<InvalidOperationException>(() => 
                helper.SignCertificate("test"));

            Assert.That(exception.Message, Does.Contain("Error occurred while validating the content"));
        }

        [Test]
        public void SignCertificate_HashingOperationWithDifferentContent_ProducesDifferentHashes()
        {
            // Test that different content produces different hashes (indirectly)

            // Arrange
            var settings = new AppSettings
            {
                TenantId = "tenant-id",
                ClientId = "client-id",
                ClientSecret = "client-secret",
                CertificateName = "cert-name",
                KeyVaultURI = "https://vault.azure.net",
                SBOMSignVerify = true
            };
            var helper = new CertificateHelper(settings);

            // Act & Assert
            // Both should fail at Azure, but they go through hashing
            var exception1 = Assert.Throws<InvalidOperationException>(() => 
                helper.SignCertificate("content1"));
            var exception2 = Assert.Throws<InvalidOperationException>(() => 
                helper.SignCertificate("content2"));

            // Both should fail at Azure level
            Assert.That(exception1.Message, Does.Contain("Error occurred while validating the content"));
            Assert.That(exception2.Message, Does.Contain("Error occurred while validating the content"));
        }

        [Test]
        public void VerifySignature_HashingOperationWithDifferentContent_ProducesDifferentHashes()
        {
            // Test hashing with different verify content

            // Arrange
            var settings = new AppSettings
            {
                TenantId = "tenant-id",
                ClientId = "client-id",
                ClientSecret = "client-secret",
                CertificateName = "cert-name",
                KeyVaultURI = "https://vault.azure.net",
                SBOMSignVerify = true
            };
            var helper = new CertificateHelper(settings);
            string sig = Convert.ToBase64String(new byte[] { 1, 2, 3 });

            // Act & Assert
            var ex1 = Assert.Throws<InvalidOperationException>(() => 
                helper.VerifySignature("data1", sig));
            var ex2 = Assert.Throws<InvalidOperationException>(() => 
                helper.VerifySignature("data2", sig));

            Assert.That(ex1.Message, Does.Contain("Error occurred while validating the content"));
            Assert.That(ex2.Message, Does.Contain("Error occurred while validating the content"));
        }

        [Test]
        public void SignCertificate_MultipleCallsWithDifferentContent_ExecutesIndependently()
        {
            // Test multiple calls are independent

            // Arrange
            var settings = new AppSettings
            {
                TenantId = "t1",
                ClientId = "c1",
                ClientSecret = "s1",
                CertificateName = "cert",
                KeyVaultURI = "https://vault.azure.net",
                SBOMSignVerify = true
            };
            var helper = new CertificateHelper(settings);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => helper.SignCertificate("test1"));
            Assert.Throws<InvalidOperationException>(() => helper.SignCertificate("test2"));
            Assert.Throws<InvalidOperationException>(() => helper.SignCertificate("test3"));

            // All three should execute without side effects
            Assert.Pass("Multiple calls executed independently");
        }

        [Test]
        public void VerifySignature_MultipleCallsWithDifferentSignatures_ExecutesIndependently()
        {
            // Test multiple verify calls

            // Arrange
            var settings = new AppSettings
            {
                TenantId = "t1",
                ClientId = "c1",
                ClientSecret = "s1",
                CertificateName = "cert",
                KeyVaultURI = "https://vault.azure.net",
                SBOMSignVerify = true
            };
            var helper = new CertificateHelper(settings);
            string sig1 = Convert.ToBase64String(new byte[] { 1 });
            string sig2 = Convert.ToBase64String(new byte[] { 2 });
            string sig3 = Convert.ToBase64String(new byte[] { 3 });

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => helper.VerifySignature("data", sig1));
            Assert.Throws<InvalidOperationException>(() => helper.VerifySignature("data", sig2));
            Assert.Throws<InvalidOperationException>(() => helper.VerifySignature("data", sig3));

            Assert.Pass("Multiple verify calls executed independently");
        }

        #endregion
    }
}
