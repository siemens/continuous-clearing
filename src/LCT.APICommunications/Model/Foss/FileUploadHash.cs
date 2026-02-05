// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;

namespace LCT.APICommunications.Model.Foss
{
    /// <summary>
    /// Then File upload hash code model
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class FileUploadHash
    {
        #region Properties

        /// <summary>
        /// Gets or sets the SHA1 hash of the uploaded file.
        /// </summary>
        [JsonProperty("sha1")]
        public string Sha1 { get; set; }

        /// <summary>
        /// Gets or sets the MD5 hash of the uploaded file.
        /// </summary>
        [JsonProperty("md5")]
        public string Md5 { get; set; }

        /// <summary>
        /// Gets or sets the SHA256 hash of the uploaded file.
        /// </summary>
        [JsonProperty("sha256")]
        public string Sha256 { get; set; }

        /// <summary>
        /// Gets or sets the size of the uploaded file.
        /// </summary>
        [JsonProperty("size")]
        public string Size { get; set; }

        #endregion Properties
    }
}
