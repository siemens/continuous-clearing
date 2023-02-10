// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT

// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;

namespace LCT.APICommunications.Model.Foss
{


    public class FossologyProcessInfo
    {

        [JsonProperty("externalTool")]
        public string externalTool { get; set; }

        [JsonProperty("processSteps")]
        public ProcessSteps[] processSteps { get; set; }
    }
}
