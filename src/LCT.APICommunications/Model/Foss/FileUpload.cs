// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
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
        #region Properties

        /// <summary>
        /// Gets or sets the folder identifier.
        /// </summary>
        [JsonProperty("folderid")]
        public string FolderId { get; set; }

        /// <summary>
        /// Gets or sets the folder name.
        /// </summary>
        [JsonProperty("foldername")]
        public string Foldername { get; set; }

        /// <summary>
        /// Gets or sets the upload identifier.
        /// </summary>
        [JsonProperty("id")]
        public string UploadId { get; set; }

        /// <summary>
        /// Gets or sets the description of the upload.
        /// </summary>
        [JsonProperty("description")]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the upload name.
        /// </summary>
        [JsonProperty("uploadname")]
        public string UploadName { get; set; }

        /// <summary>
        /// Gets or sets the upload date.
        /// </summary>
        [JsonProperty("uploaddate")]
        public string UploadDate { get; set; }

        /// <summary>
        /// Gets or sets the file upload hash information.
        /// </summary>
        [JsonProperty("hash")]
        public FileUploadHash FileUploadHash { get; set; }

        #endregion Properties
    }
}
