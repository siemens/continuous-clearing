// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;

namespace LCT.APICommunications.Model.Foss
{
    /// <summary>
    /// Fossology Jobs model
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class FossologyJobs
    {
        #region Properties

        /// <summary>
        /// Gets or sets the analysis configuration for the Fossology job.
        /// </summary>
        [JsonProperty("analysis")]
        public Analysis Analysis { get; set; }

        /// <summary>
        /// Gets or sets the decider configuration for the Fossology job.
        /// </summary>
        [JsonProperty("decider")]
        public Decider Decider { get; set; }

        /// <summary>
        /// Gets or sets the reuse configuration for the Fossology job.
        /// </summary>
        [JsonProperty("reuse")]
        public Reuse Reuse { get; set; }

        #endregion Properties
    }
}
