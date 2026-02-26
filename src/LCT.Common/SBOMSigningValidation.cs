using CycloneDX.Models;
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

        public static bool PerformSbomSigning(CommonAppSettings appSettings,string operationtype, string bomfilepath)
        {            
            // Create AppSettings object for SBOMSigning tool
            var sbomSigningAppSettings = new SBOMSigning.AppSettings
            {
                BomFilePath = bomfilepath,
                //Operation = OperationType.Sign,
                Operation = (global::SBOMSigning.Enum.OperationType)(operationtype.Equals("sign", StringComparison.OrdinalIgnoreCase) ? OperationType.Sign : OperationType.Validate),
                KeyVaultURI = appSettings.SbomSigning.KeyVaultURI,
                CertificateName = appSettings.SbomSigning.CertificateName,
                ClientId = appSettings.SbomSigning.ClientId,
                ClientSecret = appSettings.SbomSigning.ClientSecret,
                TenantId = appSettings.SbomSigning.TenantId,
                OutputFolderPath = appSettings.SbomSigning.OutputFolderPath,
                LogFolderPath = appSettings.SbomSigning.LogFolderPath ?? appSettings.Directory.LogFolder,
                UseLocalCertificate = appSettings.SbomSigning.UseLocalCertificate,
                LocalCertificatePassword = appSettings.SbomSigning.LocalCertificatePassword,
                LocalCertificatePath = appSettings.SbomSigning.LocalCertificatePath
            };
            var certificateHelper = new CertificateHelper(sbomSigningAppSettings);
            var signatureHelper = new SignatureHelper();
            var jsonFileHelper = new JsonFileHelper(sbomSigningAppSettings, certificateHelper, signatureHelper);
            bool isValid = true;
            // This calls the same SignSBOMFile() method from the SBOMSigning project
            if ((int)sbomSigningAppSettings.Operation == (int)OperationType.Sign)
            {
                jsonFileHelper.SignSBOMFile();
            }
            else if ((int)sbomSigningAppSettings.Operation == (int)OperationType.Validate)
            {
                jsonFileHelper.ReadSBOMFile(sbomSigningAppSettings.BomFilePath, out isValid);
                if (!isValid)
                {
                    // Check VerifySignature flag to determine behavior
                    bool verifySignature = appSettings.SbomSigning?.IsSignVerifyRequired ?? true;

                    if (verifySignature)
                    {
                        // Verification is enforced - throw exception to stop the process
                        throw new InvalidOperationException($"SBOM signature verification failed for file: {bomfilepath}. Set 'VerifySignature' to false in SBOM signing configuration to continue despite validation failures.");
                    }
                    else
                    {
                        // Verification is not enforced - return false but don't throw
                        return false;
                    }
                }

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

    }
}
