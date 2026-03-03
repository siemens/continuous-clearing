// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
// SPDX-License-Identifier: MIT
// --------------------------------------------------------------------------------------------------------------------

using LCT.SBOMSigning;
using log4net;
using SBOMSigning.Interface;
using SBOMSigning.Model;
using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace SBOMSigning.Helpers
{
    public class JsonFileHelper : IJsonFileHelper
    {
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        AppSettings appSettings;
        private ICertificateHelper certificateHelper;
        private ISignatureHelper signatureHelper;
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

            var bomContent = File.ReadAllText(appSettings.BomFilePath);
            Logger.Info($"Loading the BOM file : {appSettings.BomFilePath}");

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
                    return appSettings.BomFilePath;
                }
            }        

            var signatureInBytes = certificateHelper.SignCertificate(bomContent);

            if (signatureInBytes == null)
            {
                return appSettings.BomFilePath;
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
            string signedSbomFilePath = WriteSignedBOMIntoFile(array, appSettings);
            return signedSbomFilePath;

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

            Logger.Info("Reading the SBOM file...");
            string sbomContent = File.ReadAllText(sbomFilePath);

            Signature signature = signatureHelper.ExtractSignature(sbomContent);
            if (signature == null)
            {
                Logger.Error("No signature was found in the SBOM file to validate!");
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
                //Logger.Error("SBOM Veriication failed");
                isValid = false;
            }

        }
        private static string WriteSignedBOMIntoFile(string array, AppSettings appSettings)
        {
            string dirPath = string.Empty;
            string filename = string.Empty;
            dirPath = Path.GetDirectoryName(appSettings.BomFilePath);
            filename = Path.GetFileName(appSettings.BomFilePath);

            string signedSbomFilePath = Path.Combine(dirPath, $"{filename.Split(".")[0]}{DataConstant.SBOMExtension}");
            File.WriteAllText(signedSbomFilePath, array);

            Logger.Info($"Signing of SBOM successful. File located at: {signedSbomFilePath}\n");
            return signedSbomFilePath;
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

