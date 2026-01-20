// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;

namespace LCT.Common.Model
{
    /// <summary>
    /// Represents a software component with its metadata and attributes.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class Components
    {
        #region Properties

        /// <summary>
        /// Gets or sets the component name.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the component group.
        /// </summary>
        [JsonProperty("group")]
        public string Group { get; set; }

        /// <summary>
        /// Gets or sets the component version.
        /// </summary>
        [JsonProperty("version")]
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets the component external identifier.
        /// </summary>
        [JsonProperty("componentExternalId")]
        public string ComponentExternalId { get; set; }

        /// <summary>
        /// Gets or sets the release external identifier.
        /// </summary>
        [JsonProperty("releaseExternalId")]
        public string ReleaseExternalId { get; set; }

        /// <summary>
        /// Gets or sets the package URL.
        /// </summary>
        [JsonProperty("packageUrl")]
        public string PackageUrl { get; set; }

        /// <summary>
        /// Gets or sets the source URL.
        /// </summary>
        [JsonProperty("sourceUrl")]
        public string SourceUrl { get; set; }

        /// <summary>
        /// Gets or sets the download URL.
        /// </summary>
        [JsonProperty("downloadUrl")]
        public string DownloadUrl { get; set; }

        /// <summary>
        /// Gets or sets the array of patch URLs.
        /// </summary>
        [JsonProperty("patchURLs")]
        public string[] PatchURLs { get; set; }

        /// <summary>
        /// Gets or sets the Alpine source data.
        /// </summary>
        [JsonProperty("alpineSourceData")]
        public string AlpineSourceData { get; set; }

        /// <summary>
        /// Gets or sets the upload identifier.
        /// </summary>
        [JsonIgnore]
        public string UploadId { get; set; }

        /// <summary>
        /// Gets or sets the release link.
        /// </summary>
        [JsonIgnore]
        public string ReleaseLink { get; set; }

        /// <summary>
        /// Gets or sets the release identifier.
        /// </summary>
        [JsonIgnore]
        public string ReleaseId { get; set; }

        /// <summary>
        /// Gets or sets the folder path.
        /// </summary>
        [JsonIgnore]
        public string FolderPath { get; set; }

        /// <summary>
        /// Gets or sets the project type.
        /// </summary>
        [JsonIgnore]
        public string ProjectType { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this is a development component.
        /// </summary>
        [JsonIgnore]
        public string IsDev { get; set; }

        /// <summary>
        /// Gets or sets the exclude component flag.
        /// </summary>
        [JsonIgnore]
        public string ExcludeComponent { get; set; }

        /// <summary>
        /// Gets or sets the user who created the release.
        /// </summary>
        [JsonIgnore]
        public string ReleaseCreatedBy { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the component is invalid by PURL ID.
        /// </summary>
        [JsonIgnore]
        public bool InvalidComponentByPurlId { get; set; } = false;

        /// <summary>
        /// Gets or sets the component link.
        /// </summary>
        [JsonIgnore]
        public string ComponentLink { get; set; }

        /// <summary>
        /// Gets or sets the component identifier.
        /// </summary>
        [JsonIgnore]
        public string ComponentId { get; set; }

        #endregion
    }
}
