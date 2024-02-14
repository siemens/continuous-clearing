// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;

namespace LCT.APICommunications.Model.Foss
{
    /// <summary>
    /// File upload Model
    /// </summary>

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class FileUpload
    {
        [JsonProperty("folderid")]
        public string FolderId { get; set; }

        [JsonProperty("foldername")]
        public string Foldername { get; set; }

        [JsonProperty("id")]
        public string UploadId { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("uploadname")]
        public string UploadName { get; set; }

        [JsonProperty("uploaddate")]
        public string UploadDate { get; set; }

        [JsonProperty("hash")]
        public FileUploadHash FileUploadHash { get; set; }
    }
}
