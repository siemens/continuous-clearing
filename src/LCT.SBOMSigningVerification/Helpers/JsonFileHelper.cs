// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
// SPDX-License-Identifier: MIT
// --------------------------------------------------------------------------------------------------------------------

using LCT.SBOMSigningVerification.Interface;
using LCT.SBOMSigningVerification.Model;
using log4net;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace LCT.SBOMSigningVerification.Helpers
{
    public class JsonFileHelper : IJsonFileHelper
    {
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod()?.DeclaringType);

        readonly AppSettings appSettings;
        private readonly ICertificateHelper certificateHelper;
        private readonly ISignatureHelper signatureHelper;
        public JsonFileHelper(AppSettings commonAppSettings, ICertificateHelper certificateHelper, ISignatureHelper signatureHelper)
        {
            appSettings = commonAppSettings;
            this.certificateHelper = certificateHelper;
            this.signatureHelper = signatureHelper;
        }
        /// <summary>
        /// Signs the sbom file
        /// </summary>
        public string SignSBOMFile()
        {
            var originalSbom = appSettings.bomcontent;

            #pragma warning disable CS8604 // Possible null reference argument.
            string bomContent = signatureHelper.RemoveSignature(originalSbom);
            #pragma warning restore CS8604 // Possible null reference argument.
            if (IsPropertyPresent(bomContent, DataConstant.Signature))
            {
                string warningMessage = "SBOM signing failed: File already contains a signature.";
                if (appSettings.IsSignVerifyRequired)
                {
                    Logger.Error("SBOM signing failed: File already contains a signature. IsSignVerifyRequired is set to true.");
                    throw new InvalidOperationException(warningMessage);
                }
                else
                {
                    Logger.Warn("Skipping signing as SBOM already contains a signature. Continuing as IsSignVerifyRequired is set to false.");
                    return bomContent;
                }
            }

            var signatureInBytes = certificateHelper.SignCertificate(bomContent);

            if (signatureInBytes == null || signatureInBytes.Length == 0)
{
    return bomContent;
}
            string base64Signature = Convert.ToBase64String(signatureInBytes);

            var signature = new Signature
            {
                Algorithm = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256",
                Value = base64Signature
            };
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            var signatureJson = JsonSerializer.Serialize(signature, options);

            var array = AddPropertyToJson(bomContent, DataConstant.Signature, signatureJson);
            Logger.Logger.Log(null, log4net.Core.Level.Info, "SBOM Signed Successfully", null);
            return array;

        }
        /// <summary>
        /// Reads the sbom content
        /// </summary>
        public void ReadSBOMFile(string sbomFilePath, out bool isValid)
        {
            if (string.IsNullOrEmpty(sbomFilePath))
            {
                Logger.Error("Please provide a valid input filepath");
                isValid = false;
                return;
            }

            string sbomContent = File.ReadAllText(sbomFilePath);

            Signature? signature = signatureHelper.ExtractSignature(sbomContent);
            if (signature == null || string.IsNullOrEmpty(signature.Value))
            {
                Logger.Warn("No signature was found in the SBOM file to validate!");
                isValid = false;
                return;
            }

            string originalSbom = signatureHelper.RemoveSignature(sbomContent);

            isValid = certificateHelper.VerifySignature(originalSbom, signature.Value);
            if (isValid)
            {
                Logger.Info($"SBOM Signature is Valid");
            }
            else
            {
                isValid = false;
            }
        }
        
        private static string AddPropertyToJson(string jsonString, string propertyName, string propertyValue)
        {
            using (JsonDocument document = JsonDocument.Parse(jsonString))
            {
                JsonElement root = document.RootElement;

                using (var stream = new MemoryStream())
                {
                    using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
                    {
                        writer.WriteStartObject();

                        // Write existing properties
                        foreach (JsonProperty property in root.EnumerateObject())
                        {
                            property.WriteTo(writer);
                        }

                        // Write new property
                        using (JsonDocument newPropertyDoc = JsonDocument.Parse(propertyValue))
                        {
                            writer.WritePropertyName(propertyName);
                            newPropertyDoc.RootElement.WriteTo(writer);
                        }

                        writer.WriteEndObject();
                    }

                    return Encoding.UTF8.GetString(stream.ToArray());
                }
            }
        }

        private static bool IsPropertyPresent(string jsonString, string propertyName)
        {
            using (JsonDocument document = JsonDocument.Parse(jsonString))
            {
                JsonElement root = document.RootElement;
                return root.TryGetProperty(propertyName, out _);
            }
        }
    }

}

