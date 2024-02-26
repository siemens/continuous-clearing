// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;

namespace LCT.APICommunications.Model.Foss
{

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class FossologyProcessInfo
    {

        [JsonProperty("externalTool")]
        public string ExternalTool { get; set; }

        [JsonProperty("processSteps")]
        public ProcessSteps[] ProcessSteps { get; set; }
    }
}
