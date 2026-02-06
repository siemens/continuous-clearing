// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;

namespace LCT.APICommunications.Model.Foss
{
    /// <summary>
    /// Represents a single step in a Fossology process.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class ProcessSteps
    {
        #region Properties

        /// <summary>
        /// Gets or sets the name of the process step.
        /// </summary>
        [JsonProperty("stepName")]
        public string StepName { get; set; }

        /// <summary>
        /// Gets or sets the status of the process step.
        /// </summary>
        [JsonProperty("stepStatus")]
        public string StepStatus { get; set; }

        /// <summary>
        /// Gets or sets the process step identifier in the external tool.
        /// </summary>
        [JsonProperty("processStepIdInTool")]
        public string ProcessStepIdInTool { get; set; }

        /// <summary>
        /// Gets or sets the result of the process step.
        /// </summary>
        [JsonProperty("result")]
        public string Result { get; set; }

        #endregion Properties
    }
}
