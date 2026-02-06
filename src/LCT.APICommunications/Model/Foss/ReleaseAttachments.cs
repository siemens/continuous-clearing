// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;

namespace LCT.APICommunications.Model.Foss
{
    /// <summary>
    /// Release Attachment model
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class ReleaseAttachments
    {
        #region Properties

        /// <summary>
        /// Gets or sets the embedded attachment data.
        /// </summary>
        [JsonProperty("_embedded")]
        public AttachmentEmbedded Embedded { get; set; }

        #endregion Properties
    }
}
