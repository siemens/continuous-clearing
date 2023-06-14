// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;

namespace LCT.PackageIdentifier.Model
{
    /// <summary>
    /// CycloneDx Data
    /// </summary>
    [ExcludeFromCodeCoverage]
    internal class CycloneDxBomData
    {
        [JsonProperty("bomFormat")]
        public string BomFormat { get; set; }

        [JsonProperty("specVersion")]
        public string SpecVersion { get; set; }

        [JsonProperty("components")]
        public ComponentsInfo[] ComponentsInfo { get; set; }
    }
}
