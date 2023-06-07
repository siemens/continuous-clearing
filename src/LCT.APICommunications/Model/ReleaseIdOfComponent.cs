// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;
using System.Collections.Generic;

namespace LCT.APICommunications.Model
{
    /// <summary>
    /// ReleaseIdOfComponent model
    /// </summary>
    /// 

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class ReleaseIdOfComponent
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

        [JsonProperty("ownerGroup")]
        public string OwnerGroup { get; set; }

        [JsonProperty("ownerCountry")]
        public string OwnerCountry { get; set; }

        [JsonProperty("roles")]
        public Roles Roles { get; set; }

        [JsonProperty("externalIds")]
        public ExternalIds ExternalIds { get; set; }

        [JsonProperty("categories")]
        public IList<string> Categories { get; set; }

        [JsonProperty("languages")]
        public IList<object> Languages { get; set; }

        [JsonProperty("operatingSystems")]
        public IList<object> OperatingSystems { get; set; }

        [JsonProperty("homepage")]
        public string Homepage { get; set; }

        [JsonProperty("_mailinglist")]
        public string Mailinglist { get; set; }

        [JsonProperty("_links")]
        public Links Links { get; set; }

        [JsonProperty("_embedded")]
        public ReleaseEmbedded Embedded { get; set; }
    }
}