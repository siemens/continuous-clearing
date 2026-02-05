// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.APICommunications.Model.Foss;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace LCT.APICommunications.Model
{
    /// <summary>
    /// Represents external tool processing information.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class ExternalToolProcess
    {
        #region Properties

        /// <summary>
        /// Gets or sets the external tool name.
        /// </summary>
        [JsonProperty("externalTool")]
        public string ExternalTool { get; set; }

        /// <summary>
        /// Gets or sets the process status.
        /// </summary>
        [JsonProperty("processStatus")]
        public string ProcessStatus { get; set; }

        /// <summary>
        /// Gets or sets the attachment identifier.
        /// </summary>
        [JsonProperty("attachmentId")]
        public string AttachmentId { get; set; }

        /// <summary>
        /// Gets or sets the attachment hash.
        /// </summary>
        [JsonProperty("attachmentHash")]
        public string AttachmentHash { get; set; }

        /// <summary>
        /// Gets or sets the list of process steps.
        /// </summary>
        [JsonProperty("processSteps")]
        public List<ProcessSteps> ProcessSteps { get; set; }

        #endregion Properties
    }
}
