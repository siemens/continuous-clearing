// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2026 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using System.Text.Json.Serialization;


namespace SIT.SBOMSigningVerification.Model
{
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class Signature
    {
        [JsonPropertyName("algorithm")]
        public string? Algorithm { get; set; }

        [JsonPropertyName("value")]
        public string? Value { get; set; }
    }
}
