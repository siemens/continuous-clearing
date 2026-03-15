// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2026 Siemens AG
//
//  SPDX-License-Identifier: MIT
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
                ValidateAzureKeyVaultSettings();

                try
                {
                    var clientSecretCredential = new ClientSecretCredential(
                        appSettings.TenantId,
                        appSettings.ClientId,
                        appSettings.ClientSecret);

                    var cryptoClient = new CryptographyClient(
                        new Uri($"{appSettings.KeyVaultURI}/keys/{appSettings.CertificateName}"),
                        clientSecretCredential);

                    byte[] dataToSign = Encoding.UTF8.GetBytes(content);
                    byte[] hash = SHA256.HashData(dataToSign);
                    var signResult = cryptoClient.Sign(SignatureAlgorithm.RS256, hash);

                    if (signResult?.Signature == null || signResult.Signature.Length == 0)
                    {
                        const string errorMsg = "Azure Key Vault returned null or empty signature.";
                        Logger.Error(errorMsg);
                        throw new InvalidOperationException(errorMsg);
                    }

                    return signResult.Signature;
                }
                catch (Exception ex)
                {
                    string contextMsg = $"Error occurred while validating the content for certificate '{appSettings.CertificateName}' in Key Vault '{appSettings.KeyVaultURI}': {ex.Message}";
                    Logger.Error(contextMsg, ex);
                    throw new InvalidOperationException(contextMsg, ex);
                }

            }
        
        /// <summary>
        /// Verifying signature
        /// </summary>
        /// <param name="content"></param>
        /// <param name="signature"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public bool VerifySignature(string content, string signature)
        {
                ValidateAzureKeyVaultSettings();

                try
                {
                    var clientSecretCredential = new ClientSecretCredential(
                        appSettings.TenantId,
                        appSettings.ClientId,
                        appSettings.ClientSecret);

                    var cryptoClient = new CryptographyClient(
                        new Uri($"{appSettings.KeyVaultURI}/keys/{appSettings.CertificateName}"),
                        clientSecretCredential);

                    byte[] dataToVerify = Encoding.UTF8.GetBytes(content);
                    byte[] hash = SHA256.HashData(dataToVerify);
                    byte[] sign = Convert.FromBase64String(signature);

                    VerifyResult verifyResult = cryptoClient.Verify(SignatureAlgorithm.RS256, hash, sign);

                    return verifyResult.IsValid;
                }
                catch (Exception ex)
                {
                    string contextMsg = $"Error occurred while validating the content for certificate '{appSettings.CertificateName}' in Key Vault '{appSettings.KeyVaultURI}': {ex.Message}";
                    Logger.Error(contextMsg, ex);
                    throw new InvalidOperationException(contextMsg, ex);
                }

            }


        /// <summary>
        /// Validates that all required Azure Key Vault settings are present and not empty.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when one or more required settings are missing or empty.</exception>
        private void ValidateAzureKeyVaultSettings()
        {
            var settings = new Dictionary<string, string>
            {
                { DataConstant.TenantId, appSettings.TenantId ?? string.Empty },
                { DataConstant.ClientId, appSettings.ClientId ?? string.Empty },
                { DataConstant.ClientSecret, appSettings.ClientSecret ?? string.Empty },
                { DataConstant.CertificateName, appSettings.CertificateName ?? string.Empty },
                { DataConstant.KeyVaultURI, appSettings.KeyVaultURI ?? string.Empty }
            };

            var missingSettings = settings
                .Where(s => string.IsNullOrEmpty(s.Value))
                .Select(s => s.Key)
                .ToList();

            if (missingSettings.Count != 0)
            {
                string missingSettingsList = string.Join(", ", missingSettings);
                string errorMsg = $"The following required settings are missing or empty: {missingSettingsList}. Please ensure you have provided all the required arguments.";
                throw new ArgumentException(errorMsg);
            }
        }
    }

}
