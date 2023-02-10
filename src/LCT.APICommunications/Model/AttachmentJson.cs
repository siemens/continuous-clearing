// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT

// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;
using System;

namespace LCT.APICommunications.Model
{
    /// <summary>
    /// AttachmentJson model
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    [Serializable]
    public class AttachmentJson
    {
        [JsonProperty("filename")]
        public string Filename { get; set; }

        [JsonProperty("attachmentContentId")]
        public string AttachmentContentId { get; set; }

        [JsonProperty("attachmentType")]
        public string AttachmentType { get; set; }

        [JsonProperty("checkStatus")]
        public string CheckStatus { get; set; }

        [JsonProperty("createdComment")]
        public string CreatedComment { get; set; }
    }
}
