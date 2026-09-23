// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2026 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace SIT.APICommunications.Model
{
    /// <summary>
    /// Pagination metadata ("page") returned by SW360 list endpoints.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class PageInfo
    {
        #region Properties

        /// <summary>
        /// Gets or sets the total number of pages available for the request.
        /// </summary>
        [JsonProperty("totalPages")]
        public int TotalPages { get; set; }

        /// <summary>
        /// Captures any additional page fields (e.g. size, totalElements, number) so they round-trip unchanged.
        /// </summary>
        [JsonExtensionData]
        public IDictionary<string, JToken> ExtensionData { get; set; }

        #endregion Properties
    }
}
