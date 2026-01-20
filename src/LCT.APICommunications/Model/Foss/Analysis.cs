// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;

namespace LCT.APICommunications.Model.Foss
{
    /// <summary>
    /// Fossology Analyzer model
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class Analysis
    {
        #region Properties

        /// <summary>
        /// Gets or sets a value indicating whether bucket analysis is enabled.
        /// </summary>
        [JsonProperty("bucket")]
        public bool Bucket { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether copyright email author analysis is enabled.
        /// </summary>
        [JsonProperty("copyright_email_author")]
        public bool Copyright_email_author { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether ECC (Export Control Classification) analysis is enabled.
        /// </summary>
        [JsonProperty("ecc")]
        public bool Ecc { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether keyword analysis is enabled.
        /// </summary>
        [JsonProperty("keyword")]
        public bool Keyword { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether MIME type analysis is enabled.
        /// </summary>
        [JsonProperty("mime")]
        public bool Mime { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether Monk license scanner is enabled.
        /// </summary>
        [JsonProperty("monk")]
        public bool Monk { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether Nomos license scanner is enabled.
        /// </summary>
        [JsonProperty("nomos")]
        public bool Nomos { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether Ojo license scanner is enabled.
        /// </summary>
        [JsonProperty("ojo")]
        public bool Ojo { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether package analysis is enabled.
        /// </summary>
        [JsonProperty("package")]
        public bool Package { get; set; }

        #endregion Properties

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Analysis"/> class.
        /// </summary>
        public Analysis()
        {
        }

        #endregion Constructors
    }
}
