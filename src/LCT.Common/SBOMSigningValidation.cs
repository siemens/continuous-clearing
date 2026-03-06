// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
// SPDX-License-Identifier: MIT
// --------------------------------------------------------------------------------------------------------------------

using CycloneDX.Models;
using LCT.Common.Interface;
using LCT.SBOMSigningVerification.Helpers;
using log4net;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Reflection;
using OperationType = LCT.SBOMSigningVerification.Enum.OperationType;
using SignatureHelper = LCT.SBOMSigningVerification.Helpers.SignatureHelper;

namespace LCT.Common
{
    public class SbomSigningValidation : ISbomSigningValidation
    {
        static readonly ILog Logger = LoggerFactory.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Performs SBOM signing or validation based on operation type.
        /// </summary>
        /// <param name="appSettings">Application settings containing signing configuration.</param>
        /// <param name="operationType">Operation type: "sign" or "validate".</param>
        /// <param name="bomFilePath">Path to the BOM file.</param>
        /// <param name="bomContent">Optional BOM content for signing (if not provided, reads from file).</param>
        /// <returns>
        /// For Sign operation: Returns the signed BOM content as string.
        /// For Validate operation: Returns validation result as string ("True" or "False").
        /// </returns>
        public string PerformSbomOperation(CommonAppSettings appSettings, string operationType, string bomFilePath, string bomContent )
        {
            // Create AppSettings object for SBOMSigning tool
            var sbomSigningAppSettings = new SBOMSigningVerification.AppSettings
            {
                BomFilePath = bomFilePath,
                Operation = (operationType.Equals("sign", StringComparison.OrdinalIgnoreCase) ? OperationType.Sign : OperationType.Validate),
                KeyVaultURI = appSettings.SbomSigning.KeyVaultURI,
                CertificateName = appSettings.SbomSigning.CertificateName,
                ClientId = appSettings.SbomSigning.ClientId,
                ClientSecret = appSettings.SbomSigning.ClientSecret,
                TenantId = appSettings.SbomSigning.TenantId,
                SBOMVerify = appSettings.SbomSigning.SBOMVerify,
                bomcontent = bomContent,
                UseLocalCertificate = appSettings.SbomSigning.UseLocalCertificate,
                LocalCertificatePassword = appSettings.SbomSigning.LocalCertificatePassword,
                LocalCertificatePath = appSettings.SbomSigning.LocalCertificatePath
            };

            var certificateHelper = new CertificateHelper(sbomSigningAppSettings);
            var signatureHelper = new SignatureHelper();
            var jsonFileHelper = new JsonFileHelper(sbomSigningAppSettings, certificateHelper, signatureHelper);

            if ((int)sbomSigningAppSettings.Operation == (int)OperationType.Sign)
            {
                string signedBom = jsonFileHelper.SignSBOMFile();
                return signedBom;
            }
            else if ((int)sbomSigningAppSettings.Operation == (int)OperationType.Validate)

            {
                jsonFileHelper.ReadSBOMFile(sbomSigningAppSettings.BomFilePath, out bool isValid);
                return isValid.ToString();
            }

            return string.Empty;
        }

        /// <summary>
        /// Performs SBOM signing operation and returns the signed BOM content.
        /// </summary>
        /// <param name="appSettings">Application settings containing signing configuration.</param>
        /// <param name="operationType">Operation type (should be "sign").</param>
        /// <param name="bomFilePath">Path to the BOM file.</param>
        /// <param name="bomContent">Optional BOM content for signing.</param>
        /// <returns>Signed BOM content as string.</returns>
        public string PerformSbomSigning(CommonAppSettings appSettings, string operationType, string bomFilePath, string bomContent )
        {
            return PerformSbomOperation(appSettings, operationType, bomFilePath, bomContent);
        }

        /// <summary>
        /// Performs SBOM validation operation and returns the validation result.
        /// </summary>
        /// <param name="appSettings">Application settings containing signing configuration.</param>
        /// <param name="operationType">Operation type (should be "validate").</param>
        /// <param name="bomFilePath">Path to the BOM file.</param>
        /// <returns>True if validation succeeds; otherwise false.</returns>
        public bool PerformSbomSigningVerification(CommonAppSettings appSettings, string operationType, string bomFilePath)
        {
            string result = PerformSbomOperation(appSettings, operationType, bomFilePath,null);
            return bool.TryParse(result, out bool isValid) && isValid;
        }
        
        /// <summary>
        /// Validates SBOM signature and handles exit behavior based on IsSignVerifyRequired flag.
        /// This method encapsulates all validation logic including logging, error handling, and exit handling.
        /// </summary>
        /// <param name="appSettings">Application settings</param>
        /// <param name="bomFilePath">Path to BOM file</param>
        /// <param name="environmentHelper">Environment helper for exit handling</param>
        public void SigningVerification(CommonAppSettings appSettings, string bomFilePath, IEnvironmentHelper environmentHelper)
        {
            try
            {
                bool validationResult = PerformSbomSigningVerification(appSettings, "Validate", bomFilePath);

                if (validationResult)
                {
                    Logger.Logger.Log(null, log4net.Core.Level.Notice,
                        "SBOM Verified successfully.", null);
                }
                else
                {
                    Logger.Error("SBOM signature verification failed.");
                    environmentHelper.CallEnvironmentExit(-1);
                    return;
                }
            }
            catch (InvalidOperationException ex)
            {
                string errorMsg = $"SBOM Verification failed: {ex.Message}";
                Logger.Error(errorMsg, ex);
                environmentHelper.CallEnvironmentExit(-1);
            }
            catch (ArgumentException ex)
            {
                string errorMsg = $"SBOM Verification failed: {ex.Message}";
                Logger.Error(errorMsg, ex);
                environmentHelper.CallEnvironmentExit(-1);
            }
            catch (Exception ex)
            {
                string errorMsg = $"SBOM Verification failed: {ex.Message}";
                Logger.Error(errorMsg, ex);
                environmentHelper.CallEnvironmentExit(-1);
            }
        }
    }
}