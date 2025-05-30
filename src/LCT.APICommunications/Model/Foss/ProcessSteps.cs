﻿// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;

namespace LCT.APICommunications.Model.Foss
{

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class ProcessSteps
    {
        [JsonProperty("stepName")]
        public string StepName { get; set; }

        [JsonProperty("stepStatus")]
        public string StepStatus { get; set; }

        [JsonProperty("processStepIdInTool")]
        public string ProcessStepIdInTool { get; set; }

        [JsonProperty("result")]
        public string Result { get; set; }
    }
}
