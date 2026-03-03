using CycloneDX.Models;
using LCT.Common.Interface;
using log4net;
using log4net.Repository.Hierarchy;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SBOMSigning;
using SBOMSigning.Enum;
using SBOMSigning.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using OperationType = SBOMSigning.Enum.OperationType;
using SignatureHelper = SBOMSigning.Helpers.SignatureHelper;

namespace LCT.Common
{
    public class SBOMSigningValidation
    {
        static readonly ILog Logger = LoggerFactory.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Signing sbom file
        /// </summary>
        /// <param name="appSettings"></param>
        /// <param name="operationtype"></param>
        /// <param name="bomfilepath"></param>
        /// <returns></returns>
        public static bool PerformSbomSigning(CommonAppSettings appSettings,string operationtype, string bomfilepath)
        {            
            // Create AppSettings object for SBOMSigning tool
            var sbomSigningAppSettings = new SBOMSigning.AppSettings
            {
                BomFilePath = bomfilepath,
                Operation = (global::SBOMSigning.Enum.OperationType)(operationtype.Equals("sign", StringComparison.OrdinalIgnoreCase) ? OperationType.Sign : OperationType.Validate),
                KeyVaultURI = appSettings.SbomSigning.KeyVaultURI,
                CertificateName = appSettings.SbomSigning.CertificateName,
                ClientId = appSettings.SbomSigning.ClientId,
                ClientSecret = appSettings.SbomSigning.ClientSecret,
                TenantId = appSettings.SbomSigning.TenantId,               
                IsSignVerifyRequired=appSettings.SbomSigning.IsSignVerifyRequired
            };
            var certificateHelper = new CertificateHelper(sbomSigningAppSettings);
            var signatureHelper = new SignatureHelper();
            var jsonFileHelper = new JsonFileHelper(sbomSigningAppSettings, certificateHelper, signatureHelper);
            bool isValid = true;

            if ((int)sbomSigningAppSettings.Operation == (int)OperationType.Sign)
            {
                jsonFileHelper.SignSBOMFile();
            }
            else if ((int)sbomSigningAppSettings.Operation == (int)OperationType.Validate)
            {
                jsonFileHelper.ReadSBOMFile(sbomSigningAppSettings.BomFilePath, out isValid);               
                return isValid;
            }
            return true;
        }


        /// <summary>
        /// Removes existing signature from the SBOM file before re-signing.
        /// This ensures clean signing without conflicts from previous signatures.
        /// </summary>
        /// <param name="bomFilePath">Path to the BOM file.</param>
        public static void RemoveExistingSignature(string bomFilePath)
        {
            try
            {
                if (!File.Exists(bomFilePath))
                {
                    Logger.Debug($"RemoveExistingSignature(): BOM file not found: {bomFilePath}");
                    return;
                }

                Logger.Debug($"RemoveExistingSignature(): Removing existing signature from BOM file: {bomFilePath}");

                string jsonContent = File.ReadAllText(bomFilePath);

                // Deserialize the BOM using CycloneDX's own deserializer to handle version enums correctly
                Bom bom;
                try
                {
                    bom = CycloneDX.Json.Serializer.Deserialize(jsonContent);
                }
                catch (JsonSerializationException)
                {
                    // Fallback to Newtonsoft.Json if CycloneDX deserializer fails
                    Logger.Debug("RemoveExistingSignature(): CycloneDX deserializer failed, trying Newtonsoft.Json.");
                    bom = JsonConvert.DeserializeObject<Bom>(jsonContent);
                }

                if (bom?.Signature != null)
                {
                    Logger.Debug("RemoveExistingSignature(): Existing signature found, removing it.");
                    bom.Signature = null;

                    // Serialize back without the signature using CycloneDX serializer
                    string cleanJson = CycloneDX.Json.Serializer.Serialize(bom);
                    File.WriteAllText(bomFilePath, cleanJson);

                    Logger.Debug("RemoveExistingSignature(): Successfully removed existing signature.");
                }
                else
                {
                    Logger.Debug("RemoveExistingSignature(): No existing signature found in BOM file.");
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"RemoveExistingSignature(): Failed to remove existing signature: {ex.Message}");
                Logger.Debug("RemoveExistingSignature(): Continuing with signing process despite signature removal failure.");
                // Don't throw - allow signing to proceed even if signature removal fails
            }
        }

        /// <summary>
        /// Validates SBOM signature and handles exit behavior based on IsSignVerifyRequired flag.
        /// This method encapsulates all validation logic including logging, error handling, and exit handling.
        /// </summary>
        /// <param name="appSettings">Application settings</param>
        /// <param name="bomFilePath">Path to BOM file</param>
        /// <param name="environmentHelper">Environment helper for exit handling</param>
        public static void SigningVerification(CommonAppSettings appSettings, string bomFilePath, IEnvironmentHelper environmentHelper)
        {
            try
            {
                Logger.Logger.Log(null, log4net.Core.Level.Notice, "Validating SBOM signature...", null);

                bool validationResult = PerformSbomSigning(appSettings, "Validate", bomFilePath);

                if (validationResult)
                {
                    // Validation succeeded - continue
                    Logger.Logger.Log(null, log4net.Core.Level.Notice,
                        "SBOM signature validation completed successfully.", null);
                }
                else
                {
                    // Validation failed
                    if (appSettings.SbomSigning.IsSignVerifyRequired)
                    {
                        // IsSignVerifyRequired is true - validation failed and we must stop
                        Logger.Logger.Log(null, log4net.Core.Level.Error,
                            "SBOM signature validation failed. Stopping execution due to IsSignVerifyRequired being true.", null);
                        environmentHelper.CallEnvironmentExit(-1);
                    }
                    else
                    {
                        // IsSignVerifyRequired is false - validation failed but we continue with warning
                        Logger.Logger.Log(null, log4net.Core.Level.Warn,
                            "SBOM signature validation failed, but continuing execution (IsSignVerifyRequired is false).", null);
                    }
                }
            }
            catch (InvalidOperationException ex)
            {
                // Handle specific InvalidOperationException
                Logger.Error($"SBOM signature verification failed: {ex.Message}");
                Logger.Logger.Log(null, log4net.Core.Level.Error, 
                    "Stopping execution due to signature validation failure.", null);
                environmentHelper.CallEnvironmentExit(-1);
            }
            catch (Exception ex)
            {
                // Handle any other unexpected exceptions
                Logger.Error($"SBOM signature verification failed: {ex.Message}");
                Logger.Logger.Log(null, log4net.Core.Level.Error, 
                    "Stopping execution due to validation error.", null);
                environmentHelper.CallEnvironmentExit(-1);
            }
        }
    }
}
