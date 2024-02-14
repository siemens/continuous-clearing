// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;

namespace LCT.PackageIdentifier.Model
{
    /// <summary>
    /// CycloneDX Component Information
    /// </summary>
    [ExcludeFromCodeCoverage]
    internal class ComponentsInfo
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("publisher")]
        public string Publisher { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("scope")]
        public string Scope { get; set; }

        [JsonProperty("purl")]
        public string ReleaseExternalId { get; set; }
        public string ComponentExternalId { get; set; }
        public string PackageUrl { get; set; }
        public string SourceUrl { get; set; }
        public string DownloadUrl { get; set; }
    }
}
