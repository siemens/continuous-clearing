using log4net;
using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace LCT.PackageIdentifier
{
    /// <summary>
    /// Provides methods to verify digital signatures using PEM-encoded certificates or public keys.
    /// Supports RSA, ECDSA, and DSA algorithms.
    /// </summary>
    public class PemSignatureVerifier
    {
        static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Validates the signature of a document using a PEM-encoded certificate or public key file.
        /// Detects the certificate or public key format and algorithm.
        /// </summary>
        /// <param name="documentPath">Path to the document file to verify.</param>
        /// <param name="signaturePath">Path to the signature file.</param>
        /// <param name="pemFilePath">Path to the PEM certificate or public key file.</param>
        /// <returns>True if the signature is valid; otherwise, false.</returns>
        public static bool ValidatePem(string documentPath, string signaturePath, string pemFilePath)
        {
            string pemContent = File.ReadAllText(pemFilePath).Trim();
            string certHeader = "-----BEGIN CERTIFICATE-----";
            string certFooter = "-----END CERTIFICATE-----";
            string base64Content;

            int headerIndex = pemContent.IndexOf(certHeader, StringComparison.Ordinal);
            int footerIndex = pemContent.IndexOf(certFooter, StringComparison.Ordinal);

            if (headerIndex >= 0 && footerIndex > headerIndex)
            {
                base64Content = pemContent.Substring(headerIndex + certHeader.Length, footerIndex - headerIndex - certHeader.Length)
                                         .Replace("\r", "").Replace("\n", "").Trim();
            }
            else
            {
                base64Content = pemContent;
            }

            try
            {
                byte[] certBytes = Convert.FromBase64String(base64Content);
                var certificate = new X509Certificate2(certBytes);

                if (certificate.PublicKey == null)
                {
                    Logger.Debug("Certificate does not contain a valid public key.");
                    return false;
                }

                PrintCertificateInfo(certificate);

                return ValidateSignedFileFromCertificate(documentPath, signaturePath, certificate);
            }
            catch (CryptographicException ex)
            {
                Logger.Debug($"Error loading as certificate: {ex.Message}");
                Logger.Debug("Attempting to load as public key...");
                return ValidateSignedFileFromPublicKey(documentPath, signaturePath, pemFilePath);
            }
        }

        /// <summary>
        /// Verifies the signature of a document using the public key from an X509 certificate.
        /// Supports RSA, ECDSA, and DSA algorithms.
        /// </summary>
        /// <param name="documentPath">Path to the document file to verify.</param>
        /// <param name="signaturePath">Path to the signature file.</param>
        /// <param name="certificate">The X509Certificate2 object containing the public key.</param>
        /// <returns>True if the signature is valid; otherwise, false.</returns>
        private static bool ValidateSignedFileFromCertificate(string documentPath, string signaturePath, X509Certificate2 certificate)
        {
            byte[] fileData = File.ReadAllBytes(documentPath);
            byte[] signatureData = File.ReadAllBytes(signaturePath);

            // Try ECDSA
            using (var ecdsa = certificate.GetECDsaPublicKey())
            {
                if (ecdsa != null)
                {
                    return ecdsa.VerifyData(fileData, signatureData, HashAlgorithmName.SHA256);
                }
            }

            // Try RSA
            using (var rsa = certificate.GetRSAPublicKey())
            {
                if (rsa != null)
                {
                    return rsa.VerifyData(fileData, signatureData, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                }
            }

            // Try DSA
            using (var dsa = certificate.GetDSAPublicKey())
            {
                if (dsa != null)
                {
                    if (dsa.VerifyData(fileData, signatureData, HashAlgorithmName.SHA256) ||
                        dsa.VerifyData(fileData, signatureData, HashAlgorithmName.SHA1))
                    {
                        return true;
                    }
                    return false;
                }
            }

            Logger.Debug("Unsupported or missing public key algorithm in certificate.");
            return false;
        }

        /// <summary>
        /// Verifies the signature of a document using a PEM-encoded public key file.
        /// Supports RSA, ECDSA, and DSA algorithms.
        /// </summary>
        /// <param name="documentPath">Path to the document file to verify.</param>
        /// <param name="signaturePath">Path to the signature file.</param>
        /// <param name="publicKeyPath">Path to the PEM public key file.</param>
        /// <returns>True if the signature is valid; otherwise, false.</returns>
        private static bool ValidateSignedFileFromPublicKey(string documentPath, string signaturePath, string publicKeyPath)
        {
            try
            {
                byte[] documentData = File.ReadAllBytes(documentPath);
                byte[] signature = File.ReadAllBytes(signaturePath);
                string pem = File.ReadAllText(publicKeyPath).Trim();
                string base64Key = ExtractBase64FromPem(pem);

                if (string.IsNullOrEmpty(base64Key))
                {
                    Logger.Debug("Unsupported or invalid public key format.");
                    return false;
                }

                byte[] publicKeyBytes = Convert.FromBase64String(base64Key);

                // Try ECDSA
                if (TryVerifyEcdsa(documentData, signature, publicKeyBytes))
                    return true;

                // Try RSA
                if (TryVerifyRsa(documentData, signature, publicKeyBytes))
                    return true;

                // Try DSA
                if (TryVerifyDsa(documentData, signature, publicKeyBytes))
                    return true;

                Logger.Debug("Unsupported or invalid public key format.");
                return false;
            }
            catch (Exception ex)
            {
                throw new Exception($"Verification failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Extracts the Base64-encoded key from a PEM string.
        /// </summary>
        /// <param name="pem">The PEM string.</param>
        /// <returns>The Base64-encoded key, or null if not found.</returns>
        private static string ExtractBase64FromPem(string pem)
        {
            string[] headers = {
                "-----BEGIN PUBLIC KEY-----",
                "-----BEGIN EC PUBLIC KEY-----",
                "-----BEGIN DSA PUBLIC KEY-----"
            };
            string[] footers = {
                "-----END PUBLIC KEY-----",
                "-----END EC PUBLIC KEY-----",
                "-----END DSA PUBLIC KEY-----"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                if (pem.StartsWith(headers[i]))
                {
                    return pem.Replace(headers[i], "")
                              .Replace(footers[i], "")
                              .Replace("\r", "")
                              .Replace("\n", "")
                              .Trim();
                }
            }
            // If no header matched, assume the whole string is base64
            return pem;
        }

        /// <summary>
        /// Attempts to verify the signature using ECDSA.
        /// </summary>
        private static bool TryVerifyEcdsa(byte[] data, byte[] signature, byte[] publicKeyBytes)
        {
            try
            {
                using (var ecdsa = ECDsa.Create())
                {
                    ecdsa.ImportSubjectPublicKeyInfo(publicKeyBytes, out _);
                    return ecdsa.VerifyData(data, signature, HashAlgorithmName.SHA256);
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Attempts to verify the signature using RSA.
        /// </summary>
        private static bool TryVerifyRsa(byte[] data, byte[] signature, byte[] publicKeyBytes)
        {
            try
            {
                using (var rsa = RSA.Create())
                {
                    rsa.ImportRSAPublicKey(publicKeyBytes, out _);
                    return rsa.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Attempts to verify the signature using DSA.
        /// </summary>
        private static bool TryVerifyDsa(byte[] data, byte[] signature, byte[] publicKeyBytes)
        {
            try
            {
                using (var dsa = DSA.Create())
                {
                    dsa.ImportSubjectPublicKeyInfo(publicKeyBytes, out _);
                    if (dsa.VerifyData(data, signature, HashAlgorithmName.SHA256) ||
                        dsa.VerifyData(data, signature, HashAlgorithmName.SHA1))
                        return true;
                }
            }
            catch
            {
                return false;
            }
            return false;
        }

        /// <summary>
        /// Prints certificate information to the console.
        /// </summary>
        /// <param name="certificate">The X509Certificate2 object.</param>
        private static void PrintCertificateInfo(X509Certificate2 certificate)
        {
            Logger.Debug("Certificate loaded successfully.");
            Logger.Debug($"Subject: {certificate.Subject}");
            Logger.Debug($"Issuer: {certificate.Issuer}");
            Logger.Debug($"Valid From: {certificate.NotBefore}");
            Logger.Debug($"Valid To: {certificate.NotAfter}");
            Logger.Debug($"Thumbprint: {certificate.Thumbprint}");
            Logger.Debug($"Algorithm: {certificate.SignatureAlgorithm.FriendlyName}");
        }
    }
}