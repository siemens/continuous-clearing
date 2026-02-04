// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;

namespace LCT.APICommunications.Model.Foss
{
    /// <summary>
    /// Represents the Fossology process information.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class FossologyProcessInfo
    {
        #region Properties

        /// <summary>
        /// Gets or sets the external tool name.
        /// </summary>
        [JsonProperty("externalTool")]
        public string ExternalTool { get; set; }

        /// <summary>
        /// Gets or sets the array of process steps.
        /// </summary>
        [JsonProperty("processSteps")]
        public ProcessSteps[] ProcessSteps { get; set; }

        #endregion Properties
    }
}
