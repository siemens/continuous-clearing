// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
// SPDX-License-Identifier: MIT
// --------------------------------------------------------------------------------------------------------------------

using Azure.Identity;
using Azure.Security.KeyVault.Keys.Cryptography;
using LCT.SBOMSigningVerification.Interface;
using log4net;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace LCT.SBOMSigningVerification.Helpers
{
    public class CertificateHelper : ICertificateHelper
    {
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod()?.DeclaringType ?? typeof(CertificateHelper));
        readonly AppSettings appSettings;
        public CertificateHelper(AppSettings commonAppSettings)
        {
            appSettings = commonAppSettings;
        }
        /// <summary>
        /// Signs the sbom content
        /// </summary>
        /// <param name="bomcontent">args</param>
        /// <returns>signature</returns>
        public byte[] SignCertificate(string content)
        {           
            string tenantId = appSettings.TenantId ?? string.Empty;
            string clientId = appSettings.ClientId ?? string.Empty;
            string clientSecret = appSettings.ClientSecret ?? string.Empty;
            string certificateName = appSettings.CertificateName ?? string.Empty;
            string kvUri = appSettings.KeyVaultURI ?? string.Empty;

            var settings = new Dictionary<string, string>
            {
                { DataConstant.TenantId, tenantId },
                { DataConstant.ClientId, clientId },
                { DataConstant.ClientSecret, clientSecret },
                { DataConstant.CertificateName,certificateName },
                { DataConstant.KeyVaultURI, kvUri }
            };

            var missingSettings = settings
                .Where(s => string.IsNullOrEmpty(s.Value))
                .Select(s => s.Key)
                .ToList();

            // Handle missing settings if any
            if (missingSettings.Count != 0)
            {
                string missingSettingsList = string.Join(", ", missingSettings);

                if (appSettings.SBOMVerify)
                {
                    string errorMsg = $"The following required settings are missing or empty: {missingSettingsList}. Please ensure you have provided all the required arguments.";
                    throw new ArgumentException(errorMsg);
                }                
            }

            try
            {
                var clientSecretCredential = new ClientSecretCredential(tenantId, clientId, clientSecret);
                var cryptoClient = new CryptographyClient(new Uri($"{kvUri}/keys/{certificateName}"), clientSecretCredential);
                byte[] dataToSign = Encoding.UTF8.GetBytes(content);

                var signResult = cryptoClient.SignData(SignatureAlgorithm.RS256, dataToSign);

                if (signResult?.Signature == null || signResult.Signature.Length == 0)
                {
                    string errorMsg = "Azure Key Vault returned null or empty signature.";
                    Logger.Error(errorMsg);
                    throw new InvalidOperationException(errorMsg);
                }

                Logger.Debug("Signature generated successfully.");
                return signResult.Signature;
            }
            catch (Exception ex)
            {
                string errorMsg = $"Failed to sign SBOM content: {ex.Message}";
                Logger.Error(errorMsg, ex);
                throw; // ✅ Re-throw to propagate the error
            }            
        }     

        /// <summary>
        /// Signs the sbom content
        /// </summary>
        /// <param name="bomcontent">bomcontent</param>
        /// <param name="signature">signature</param>
        /// <returns>validation</returns>
        public bool VerifySignature(string content, string signature)
        {
                string tenantId = appSettings.TenantId ?? string.Empty;
                string clientId = appSettings.ClientId ?? string.Empty;
                string clientSecret = appSettings.ClientSecret ?? string.Empty;
                string certificateName = appSettings.CertificateName ?? string.Empty;
                string kvUri = appSettings.KeyVaultURI ?? string.Empty;
                bool isValid = false;

                var settings = new Dictionary<string, string>
            {
                { DataConstant.TenantId, tenantId },
                { DataConstant.ClientId, clientId },
                { DataConstant.ClientSecret, clientSecret },
                { DataConstant.CertificateName,certificateName },
                { DataConstant.KeyVaultURI, kvUri }
            };

                var missingSettings = settings
                        .Where(s => string.IsNullOrEmpty(s.Value))
                        .Select(s => s.Key)
                        .ToList();

                // Handle missing settings if any
                if (missingSettings.Count != 0)
                {
                    string missingSettingsList = string.Join(", ", missingSettings);

                    if (appSettings.SBOMVerify)
                    {
                        string errorMsg = $"The following required settings are missing or empty: {missingSettingsList}. Please ensure you have provided all the required arguments.";
                        throw new ArgumentException(errorMsg);
                    }                   
                }

                try
                {
                    var clientSecretCredential = new ClientSecretCredential(tenantId, clientId, clientSecret);
                    var cryptoClient = new CryptographyClient(new Uri($"{kvUri}/keys/{certificateName}"), clientSecretCredential);
                    byte[] dataToVerify = Encoding.UTF8.GetBytes(content);

                    using (var sha256 = SHA256.Create())
                    {
                        byte[] hash = sha256.ComputeHash(dataToVerify);
                        byte[] sign = Convert.FromBase64String(signature);
                        VerifyResult verifyResult = cryptoClient.Verify(SignatureAlgorithm.RS256, hash, sign);
                        isValid = verifyResult.IsValid;
                    }
                }
                catch (Exception ex)
                {
                    var exNamespace = ex.GetType().Namespace;
                    if (!string.IsNullOrEmpty(exNamespace) && exNamespace.StartsWith("Azure"))
                    {
                        Logger.Error("Azure Exception: {0}", ex);
                        Logger.DebugFormat("StackTrace: {0}", ex.StackTrace);
                    }
                    else
                    {
                        Logger.Debug("Signature obtained successfully.");
                    }

                }

                return isValid;
            }       
    }
}
