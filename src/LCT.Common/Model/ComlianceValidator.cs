// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// --------------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;

namespace LCT.Common.ComplianceValidator
{
    using System.Text.Json.Serialization;

    public class ComplianceSettingsModel
    {
        [JsonPropertyName("complianceExceptionComponents")]
        public List<ComplianceExceptionComponent> ComplianceExceptionComponents { get; set; }
    }

    public class ComplianceExceptionComponent
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("purl")]
        public List<string> Purl { get; set; }

        [JsonPropertyName("complianceInstructions")]
        public ComplianceInstructions ComplianceInstructions { get; set; }
    }

    public class ComplianceInstructions
    {
        [JsonPropertyName("warningMessage")]
        public string WarningMessage { get; set; }

        [JsonPropertyName("recommendation")]
        public string Recommendation { get; set; }
    }
}