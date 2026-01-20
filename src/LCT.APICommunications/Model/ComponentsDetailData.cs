// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
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
        #region Properties

        /// <summary>
        /// Gets or sets the component name.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the component description.
        /// </summary>
        [JsonProperty("description")]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the creation date of the component.
        /// </summary>
        [JsonProperty("createdOn")]
        public string CreatedOn { get; set; }

        /// <summary>
        /// Gets or sets the component type.
        /// </summary>
        [JsonProperty("componentType")]
        public string ComponentType { get; set; }

        /// <summary>
        /// Gets or sets the component owner.
        /// </summary>
        [JsonProperty("componentOwner")]
        public string ComponentOwner { get; set; }

        /// <summary>
        /// Gets or sets the owner's accounting unit.
        /// </summary>
        [JsonProperty("ownerAccountingUnit")]
        public string OwnerAccountingUnit { get; set; }

        /// <summary>
        /// Gets or sets the owner's country.
        /// </summary>
        [JsonProperty("ownerCountry")]
        public string OwnerCountry { get; set; }

        /// <summary>
        /// Gets or sets the owner's group.
        /// </summary>
        [JsonProperty("ownerGroup")]
        public string OwnerGroup { get; set; }

        /// <summary>
        /// Gets or sets the roles associated with the component.
        /// </summary>
        [JsonProperty("roles")]
        public Roles Roles { get; set; }

        /// <summary>
        /// Gets or sets the list of categories.
        /// </summary>
        [JsonProperty("categories")]
        public IList<string> Categories { get; set; }

        /// <summary>
        /// Gets or sets the list of programming languages.
        /// </summary>
        [JsonProperty("languages")]
        public IList<string> Languages { get; set; }

        /// <summary>
        /// Gets or sets the list of operating systems.
        /// </summary>
        [JsonProperty("operatingSystems")]
        public IList<string> OperatingSystems { get; set; }

        /// <summary>
        /// Gets or sets the homepage URL.
        /// </summary>
        [JsonProperty("homepage")]
        public string Homepage { get; set; }

        /// <summary>
        /// Gets or sets the mailing list address.
        /// </summary>
        [JsonProperty("mailinglist")]
        public string Mailinglist { get; set; }

        /// <summary>
        /// Gets or sets the embedded component details data.
        /// </summary>
        [JsonProperty("_embedded")]
        public ComponentsDetailEmbedded Embedded { get; set; }

        /// <summary>
        /// Gets or sets the links associated with the component.
        /// </summary>
        [JsonProperty("_links")]
        public Links Links { get; set; }

        #endregion Properties
    }
}
