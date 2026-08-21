// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2026 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;
using System.Collections.Generic;

namespace SIT.APICommunications.Model
{
    /// <summary>
    /// AttachmentEmbedded model
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class AttachmentEmbedded
    {
        #region Properties

        /// <summary>
        /// Gets or sets the list of SW360 attachments.
        /// </summary>
        [JsonProperty("sw360:attachments")]
        public IList<Sw360Attachments> Sw360attachments { get; set; }

        #endregion Properties
    }
}