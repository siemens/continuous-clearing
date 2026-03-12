// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
// SPDX-License-Identifier: MIT
// --------------------------------------------------------------------------------------------------------------------

using LCT.SBOMSigningVerification.Helpers;
using NUnit.Framework;
using System;

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
    }
}
