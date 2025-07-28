// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// --------------------------------------------------------------------------------------------------------------------
using LCT.Common;
using LCT.Common.Interface;
using log4net;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace LCT.PackageIdentifier
{
    /// <summary>
    /// Provides methods to verify digital signatures using PEM-encoded certificates or public keys.
    /// Supports RSA, ECDSA, and DSA algorithms.
    /// </summary>
    public static class PemSignatureVerifier
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
            const string certHeader = "-----BEGIN CERTIFICATE-----";
            const string certFooter = "-----END CERTIFICATE-----";
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

                LogCertificateInfo(certificate);

                bool IsValid = ValidateSignedFileFromCertificate(documentPath, signaturePath, certificate);
                return IsValid;

            }
            //IF System.FormatException is thrown, it indicates that the PEM content is not a valid certificate.
            catch (FormatException ex)
            {
                Logger.Debug($"Error loading PEM content: {ex.Message}");
                Logger.Debug("Attempting to load as public key...");
                return ValidateSignedFileFromPublicKey(documentPath, signaturePath, pemFilePath);
            }
            //IF System.Security.Cryptography.CryptographicException is thrown, it indicates that the PEM content is not a valid certificate.
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

                byte[] publicKeyBytes;
                if (IsBase64String(base64Key))
                {
                    publicKeyBytes = Convert.FromBase64String(base64Key);
                }
                else
                {
                    publicKeyBytes = File.ReadAllBytes(publicKeyPath);
                }

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
            catch (IOException ex)
            {
                Logger.Error($"Error reading file during signature validation: {ex.Message}", ex);
                return false;
            }
            catch (UnauthorizedAccessException ex)
            {
                Logger.Error($"Access denied while reading file during signature validation: {ex.Message}", ex);
                return false;
            }
            catch (CryptographicException ex)
            {
                Logger.Error($"Cryptographic error during signature validation: {ex.Message}", ex);
                return false;
            }
            catch (FormatException ex)
            {
                Logger.Error($"Invalid format during signature validation: {ex.Message}", ex);
                return false;
            }
            catch (ArgumentException ex)
            {
                Logger.Error($"Invalid argument during signature validation: {ex.Message}", ex);
                return false;
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
                int headerIndex = pem.IndexOf(headers[i], StringComparison.Ordinal);
                int footerIndex = pem.IndexOf(footers[i], StringComparison.Ordinal);
                if (headerIndex >= 0 && footerIndex > headerIndex)
                {
                    int start = headerIndex + headers[i].Length;
                    int length = footerIndex - start;
                    string base64 = pem.Substring(start, length);
                    // Remove all whitespace (including \r, \n, spaces, tabs)
                    base64 = new string(base64.Where(c => !char.IsWhiteSpace(c)).ToArray());
                    return base64;
                }
            }
            // If no header matched, assume the whole string is base64
            return new string(pem.Where(c => !char.IsWhiteSpace(c)).ToArray());
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
            catch (Exception ex) when (ex is CryptographicException || ex is FormatException)
            {
                Logger.Debug($"ECDSA verification failed: {ex.Message}");
                // If ECDSA verification fails, we can try RSA or DSA
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
                    rsa.ImportSubjectPublicKeyInfo(publicKeyBytes, out _);
                    return rsa.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                }
            }
            catch (Exception ex) when (ex is CryptographicException || ex is FormatException)
            {
                Logger.Debug($"RSA verification failed: {ex.Message}");
                // If RSA verification fails, we can try DSA
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
            catch (Exception ex) when (ex is CryptographicException || ex is FormatException)
            {
                Logger.Debug($"DSA verification failed: {ex.Message}");
                return false;
            }
            return false;
        }

        /// <summary>
        /// Prints certificate information to the console.
        /// </summary>
        /// <param name="certificate">The X509Certificate2 object.</param>
        private static void LogCertificateInfo(X509Certificate2 certificate)
        {
            Logger.Debug("Certificate loaded successfully.");
            Logger.Debug($"Subject: {certificate.Subject}");
            Logger.Debug($"Issuer: {certificate.Issuer}");
            Logger.Debug($"Valid From: {certificate.NotBefore}");
            Logger.Debug($"Valid To: {certificate.NotAfter}");
            Logger.Debug($"Thumbprint: {certificate.Thumbprint}");
            Logger.Debug($"Algorithm: {certificate.SignatureAlgorithm.FriendlyName}");
        }

        /// <summary>
        /// Checks if a string is a valid Base64-encoded string.
        /// </summary>
        /// <param name="s">The string to check.</param>
        private static bool IsBase64String(string s)
        {
            Span<byte> buffer = new Span<byte>(new byte[s.Length]);
            return Convert.TryFromBase64String(s, buffer, out _);
        }
    }
}