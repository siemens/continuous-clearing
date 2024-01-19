// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;

namespace LCT.Common.Model
{
    /// <summary>
    /// Component model
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class Components
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("group")]
        public string Group { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("componentExternalId")]
        public string ComponentExternalId { get; set; }

        [JsonProperty("releaseExternalId")]
        public string ReleaseExternalId { get; set; }

        [JsonProperty("packageUrl")]
        public string PackageUrl { get; set; }

        [JsonProperty("sourceUrl")]
        public string SourceUrl { get; set; }

        [JsonProperty("downloadUrl")]
        public string DownloadUrl { get; set; }

        [JsonProperty("PatchURLs")]
        public string[] PatchURLs { get; set; }

        [JsonProperty("AlpineSourceData")]
        public string AlpineSourceData { get; set; }

        [JsonIgnore]
        public string UploadId { get; set; }

        [JsonIgnore]
        public string ReleaseLink { get; set; }

        [JsonIgnore]
        public string ReleaseId { get; set; }

        [JsonIgnore]
        public string FolderPath { get; set; }

        [JsonIgnore]
        public string ProjectType { get; set; }

        [JsonIgnore]
        public string IsDev { get; set; }
        
    }
}
