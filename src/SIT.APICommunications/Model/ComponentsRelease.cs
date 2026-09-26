// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2026 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Management.Automation;

namespace SIT.APICommunications.Model
{
    /// <summary>
    /// ComponentsRelease model
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class ComponentsRelease : IPagedFetchResult<Sw360Releases>
    {
        #region Properties

        /// <summary>
        /// Gets or sets the embedded release data.
        /// </summary>
        [JsonProperty("_embedded")]
        public ReleaseEmbedded Embedded { get; set; }

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
        public IEnumerable<Sw360Releases> Items => Embedded?.Sw360Releases ?? (IEnumerable<Sw360Releases>)System.Array.Empty<Sw360Releases>();

        /// <inheritdoc/>
        public void AddItems(IEnumerable<Sw360Releases> items)
        {
            Embedded ??= new ReleaseEmbedded();
            Embedded.Sw360Releases ??= new List<Sw360Releases>();
            foreach (Sw360Releases item in items)
            {
                Embedded.Sw360Releases.Add(item);
            }
        }

        #endregion Properties
    }
}