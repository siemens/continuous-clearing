// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2026 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace SIT.APICommunications.Model
{
    /// <summary>
    /// ComponentsModel 
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class ComponentsModel : IPagedFetchResult<Sw360Components>
    {
        #region Properties

        /// <summary>
        /// Gets or sets the embedded component data.
        /// </summary>
        [JsonProperty("_embedded")]
        public ComponentEmbedded Embedded { get; set; }

        /// <summary>
        /// Gets or sets the pagination metadata, used to drive page-by-page fetching without a JObject DOM.
        /// </summary>
        [JsonProperty("page")]
        public PageInfo Page { get; set; }

        /// <summary>
        /// Captures any other top-level fields so a page can be re-serialized without losing data.
        /// </summary>
        [JsonExtensionData]
        public IDictionary<string, JToken> ExtensionData { get; set; }

        /// <inheritdoc/>
        [JsonIgnore]
        public IEnumerable<Sw360Components> Items => Embedded?.Sw360components ?? (IEnumerable<Sw360Components>)System.Array.Empty<Sw360Components>();

        /// <inheritdoc/>
        public void AddItems(IEnumerable<Sw360Components> items)
        {
            Embedded ??= new ComponentEmbedded();
            Embedded.Sw360components ??= new List<Sw360Components>();
            foreach (Sw360Components item in items)
            {
                Embedded.Sw360components.Add(item);
            }
        }

        #endregion Properties
    }
}