// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;

namespace LCT.APICommunications.Model.Foss
{
    /// <summary>
    /// Multiple upload component model
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class MultipleUploadComponentModel
    {
        #region Properties

        /// <summary>
        /// Gets or sets the folder identifier.
        /// </summary>
        [JsonProperty("folderid")]
        public string Folderid { get; set; }

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
        public string Uploadname { get; set; }

        /// <summary>
        /// Gets or sets the upload date.
        /// </summary>
        [JsonProperty("uploaddate")]
        public string Uploaddate { get; set; }

        /// <summary>
        /// Gets or sets the file size.
        /// </summary>
        [JsonProperty("filesize")]
        public string Filesize { get; set; }

        #endregion Properties
    }
}
