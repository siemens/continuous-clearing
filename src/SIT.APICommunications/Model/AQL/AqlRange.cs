// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2026 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;

namespace SIT.APICommunications.Model.AQL
{
    /// <summary>
    /// The AqlRange model representing the "range" object of an AQL search response.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class AqlRange
    {
        #region Properties

        /// <summary>
        /// Gets or sets the start position of the returned result set.
        /// </summary>
        [JsonProperty("start_pos")]
        public int StartPos { get; set; }

        /// <summary>
        /// Gets or sets the end position of the returned result set.
        /// </summary>
        [JsonProperty("end_pos")]
        public int EndPos { get; set; }

        /// <summary>
        /// Gets or sets the total number of matching results.
        /// </summary>
        [JsonProperty("total")]
        public int Total { get; set; }

        /// <summary>
        /// Gets or sets the notification message returned when the result set hit the server's configured hard limit (aql.search.query.max.limit).
        /// </summary>
        [JsonProperty("notification")]
        public string Notification { get; set; }

        #endregion Properties
    }
}
