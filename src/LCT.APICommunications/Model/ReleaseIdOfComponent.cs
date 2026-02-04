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
    /// ReleaseIdOfComponent model
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class ReleaseIdOfComponent
    {
        #region Properties

        /// <summary>
        /// Gets or sets the component name.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        [JsonProperty("description")]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the creation date.
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
        /// Gets or sets the owner accounting unit.
        /// </summary>
        [JsonProperty("ownerAccountingUnit")]
        public string OwnerAccountingUnit { get; set; }

        /// <summary>
        /// Gets or sets the owner group.
        /// </summary>
        [JsonProperty("ownerGroup")]
        public string OwnerGroup { get; set; }

        /// <summary>
        /// Gets or sets the owner country.
        /// </summary>
        [JsonProperty("ownerCountry")]
        public string OwnerCountry { get; set; }

        /// <summary>
        /// Gets or sets the roles.
        /// </summary>
        [JsonProperty("roles")]
        public Roles Roles { get; set; }

        /// <summary>
        /// Gets or sets the external identifiers.
        /// </summary>
        [JsonProperty("externalIds")]
        public ExternalIds ExternalIds { get; set; }

        /// <summary>
        /// Gets or sets the list of categories.
        /// </summary>
        [JsonProperty("categories")]
        public IList<string> Categories { get; set; }

        /// <summary>
        /// Gets or sets the list of programming languages.
        /// </summary>
        [JsonProperty("languages")]
        public IList<object> Languages { get; set; }

        /// <summary>
        /// Gets or sets the list of operating systems.
        /// </summary>
        [JsonProperty("operatingSystems")]
        public IList<object> OperatingSystems { get; set; }

        /// <summary>
        /// Gets or sets the homepage URL.
        /// </summary>
        [JsonProperty("homepage")]
        public string Homepage { get; set; }

        /// <summary>
        /// Gets or sets the mailing list.
        /// </summary>
        [JsonProperty("_mailinglist")]
        public string Mailinglist { get; set; }

        /// <summary>
        /// Gets or sets the links.
        /// </summary>
        [JsonProperty("_links")]
        public Links Links { get; set; }

        /// <summary>
        /// Gets or sets the embedded release information.
        /// </summary>
        [JsonProperty("_embedded")]
        public ReleaseEmbedded Embedded { get; set; }

        #endregion Properties
    }
}