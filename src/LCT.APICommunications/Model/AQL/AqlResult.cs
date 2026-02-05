// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;
using System.Collections.Generic;

namespace LCT.APICommunications.Model.AQL
{
    /// <summary>
    /// The AqlResult model
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class AqlResult
    {
        #region Properties

        /// <summary>
        /// Gets or sets the repository name.
        /// </summary>
        [JsonProperty("repo")]
        public string Repo { get; set; }

        /// <summary>
        /// Gets or sets the artifact path.
        /// </summary>
        [JsonProperty("path")]
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets the artifact name.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the MD5 checksum of the artifact.
        /// </summary>
        [JsonProperty("actual_md5")]
        public string MD5 { get; set; }

        /// <summary>
        /// Gets or sets the SHA1 checksum of the artifact.
        /// </summary>
        [JsonProperty("actual_sha1")]
        public string SHA1 { get; set; }

        /// <summary>
        /// Gets or sets the SHA256 checksum of the artifact.
        /// </summary>
        [JsonProperty("sha256")]
        public string SHA256 { get; set; }

        /// <summary>
        /// Gets or sets the list of AQL properties.
        /// </summary>
        public List<AqlProperty> Properties { get; set; }

        #endregion Properties
    }

    /// <summary>
    /// The AqlProperty model representing a key-value property.
    /// </summary>
    public class AqlProperty
    {
        #region Properties

        /// <summary>
        /// Gets or sets the property key.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets the property value.
        /// </summary>
        public string Value { get; set; }

        #endregion Properties
    }

}
