// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
// SPDX-License-Identifier: MIT
// --------------------------------------------------------------------------------------------------------------------

using LCT.SBOMSigningVerification.Interface;
using LCT.SBOMSigningVerification.Model;
using System.Text;
using System.Text.Json;

namespace LCT.SBOMSigningVerification.Helpers
{
    public class SignatureHelper : ISignatureHelper
    {
        public SignatureHelper()
        {

        }
        /// <summary>
        /// Extract the signature
        /// </summary>
        /// <param name="sbomContent">sbomContent</param>
        /// <returns>signature</returns>
        public Signature? ExtractSignature(string sbomContent)
        {
            var sbomJson = JsonDocument.Parse(sbomContent);
            if (sbomJson.RootElement.TryGetProperty(DataConstant.Signature, out JsonElement signatureElement))
            {
                var signatureJson = signatureElement.GetRawText();
                return JsonSerializer.Deserialize<Signature>(signatureJson)!;
            }
            return default;
        }
        /// <summary>
        /// Extract the signature
        /// </summary>
        /// <param name="sbomContent">sbomContent</param>
        /// <returns>removes the signature</returns>
        public string RemoveSignature(string sbomContent)
        {
            var sbomJson = JsonDocument.Parse(sbomContent);
            using (var stream = new MemoryStream())
            {
                using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
                {
                    writer.WriteStartObject();

                    foreach (JsonProperty property in sbomJson.RootElement.EnumerateObject())
                    {
                        if (property.Name != DataConstant.Signature)
                        {
                            property.WriteTo(writer);
                        }
                    }

                    writer.WriteEndObject();
                }

                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        public string RemoveSignature1(string sbomContent)
        {
            if (string.IsNullOrEmpty(sbomContent))
            {
                throw new ArgumentException("SBOM content cannot be null or empty", nameof(sbomContent));
            }

            var sbomJson = JsonDocument.Parse(sbomContent);

            // Check if signature exists
            bool hasSignature = sbomJson.RootElement.TryGetProperty(DataConstant.Signature, out _);

            if (!hasSignature)
            {
                // No signature to remove, return original content
                return sbomContent;
            }

            using (var stream = new MemoryStream())
            {
                using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
                {
                    writer.WriteStartObject();

                    // Copy all properties EXCEPT "signature"
                    foreach (JsonProperty property in sbomJson.RootElement.EnumerateObject())
                    {
                        // ✅ Only skip if property name EXACTLY matches "signature"
                        if (!string.Equals(property.Name, DataConstant.Signature, StringComparison.OrdinalIgnoreCase))
                        {
                            property.WriteTo(writer);
                        }
                    }

                    writer.WriteEndObject();
                }

                string resultContent = Encoding.UTF8.GetString(stream.ToArray());

                // ✅ Verify signature was actually removed
                if (resultContent.Contains($"\"{DataConstant.Signature}\"", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Failed to remove signature from SBOM content");
                }

                return resultContent;
            }
        }
    }
}
