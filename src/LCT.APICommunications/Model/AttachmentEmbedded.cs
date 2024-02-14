// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;
using System.Collections.Generic;

namespace LCT.APICommunications.Model
{
    /// <summary>
    /// AttachmentEmbedded model
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class AttachmentEmbedded
    {
        [JsonProperty("sw360:attachments")]
        public IList<Sw360Attachments> Sw360attachments { get; set; }
    }
}