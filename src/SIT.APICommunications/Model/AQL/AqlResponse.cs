// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2026 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;
using System.Collections.Generic;

namespace SIT.APICommunications.Model.AQL
{
    /// <summary>
    /// The AqlResponse model class
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class AqlResponse
    {
        #region Properties

        /// <summary>
        /// Gets or sets the list of AQL query results.
        /// </summary>
        [JsonProperty("results")]
        public IList<AqlResult> Results { get; set; }

        /// <summary>
        /// Gets or sets the range metadata, including a notification when the server's hard result limit is reached.
        /// </summary>
        [JsonProperty("range")]
        public AqlRange Range { get; set; }

        #endregion Properties
    }
}
