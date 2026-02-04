// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;

namespace LCT.APICommunications.Model
{
    /// <summary>
    /// Represents checksum information for a file or package.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class Checksum
    {
        #region Properties

        /// <summary>
        /// Gets or sets the SHA1 hash value.
        /// </summary>
        [JsonProperty("sha1")]
        public string Sha1 { get; set; }

        #endregion Properties
    }
}
