// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;
using System.Collections.Generic;

namespace LCT.APICommunications.Model
{
    /// <summary>
    /// The Sw360Component Type  Model class
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class ComponentsDetailData
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("createdOn")]
        public string CreatedOn { get; set; }

        [JsonProperty("componentType")]
        public string ComponentType { get; set; }

        [JsonProperty("componentOwner")]
        public string ComponentOwner { get; set; }

        [JsonProperty("ownerAccountingUnit")]
        public string OwnerAccountingUnit { get; set; }

        [JsonProperty("ownerCountry")]
        public string OwnerCountry { get; set; }

        [JsonProperty("ownerGroup")]
        public string OwnerGroup { get; set; }

        [JsonProperty("roles")]
        public Roles Roles { get; set; }

        [JsonProperty("categories")]
        public IList<string> Categories { get; set; }

        [JsonProperty("languages")]
        public IList<string> Languages { get; set; }

        [JsonProperty("operatingSystems")]
        public IList<string> OperatingSystems { get; set; }

        [JsonProperty("homepage")]
        public string Homepage { get; set; }

        [JsonProperty("mailinglist")]
        public string Mailinglist { get; set; }

        [JsonProperty("_embedded")]
        public ComponentsDetailEmbedded Embedded { get; set; }

        [JsonProperty("_links")]
        public Links Links { get; set; }
    }
}
