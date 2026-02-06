// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;

namespace LCT.APICommunications.Model
{
    /// <summary>
    /// DocUpdate model
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class DocUpdate
    {
        #region Properties

        /// <summary>
        /// Gets or sets the embedded attachment information.
        /// </summary>
        [JsonProperty("_embedded")]
        public AttachmentEmbedded Embedded { get; set; }

        #endregion Properties
    }
}