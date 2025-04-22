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
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class ExternalToolProcess
    {
        [JsonProperty("externalTool")]
        public string ExternalTool { get; set; }

        [JsonProperty("processStatus")]
        public string ProcessStatus { get; set; }

        [JsonProperty("attachmentId")]
        public string AttachmentId { get; set; }

        [JsonProperty("attachmentHash")]
        public string AttachmentHash { get; set; }

        [JsonProperty("processSteps")]
        public List<ProcessSteps> ProcessSteps { get; set; }
    }
}
