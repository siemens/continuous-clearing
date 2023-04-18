// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT

// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;

namespace LCT.APICommunications.Model.Foss
{

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class ProcessSteps
    {
        [JsonProperty("processStstepNameeps")]
        public string stepName { get; set; }

        [JsonProperty("stepStatus")]
        public string stepStatus { get; set; }

        [JsonProperty("processStepIdInTool")]
        public string processStepIdInTool { get; set; }

        [JsonProperty("result")]
        public string result { get; set; }
    }
}
