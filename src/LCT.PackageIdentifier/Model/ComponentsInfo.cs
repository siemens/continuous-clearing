// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
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
        #region Properties
        /// <summary>
        /// Component type as defined in CycloneDX.
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary>
        /// Publisher or vendor of the component.
        /// </summary>
        [JsonProperty("publisher")]
        public string Publisher { get; set; }

        /// <summary>
        /// Component name.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Component version.
        /// </summary>
        [JsonProperty("version")]
        public string Version { get; set; }

        /// <summary>
        /// Component scope (for example required or optional).
        /// </summary>
        [JsonProperty("scope")]
        public string Scope { get; set; }

        /// <summary>
        /// External identifier (purl) from the release metadata.
        /// </summary>
        [JsonProperty("purl")]
        public string ReleaseExternalId { get; set; }

        /// <summary>
        /// Component external identifier.
        /// </summary>
        public string ComponentExternalId { get; set; }

        /// <summary>
        /// Package URL for the component.
        /// </summary>
        public string PackageUrl { get; set; }

        /// <summary>
        /// Source URL for the component.
        /// </summary>
        public string SourceUrl { get; set; }

        /// <summary>
        /// Download URL for the component binary or artifact.
        /// </summary>
        public string DownloadUrl { get; set; }
        #endregion

        #region Constructors
        #endregion

        #region Methods
        #endregion

        #region Events
        #endregion
    }
}
