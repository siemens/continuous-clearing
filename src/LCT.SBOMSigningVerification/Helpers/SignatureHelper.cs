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
        public Signature ExtractSignature(string sbomContent)
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
    }
}
