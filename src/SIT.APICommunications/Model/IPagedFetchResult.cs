// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2026 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using System.Collections.Generic;

namespace SIT.APICommunications.Model
{
    /// <summary>
    /// Implemented by typed SW360 paged list responses (e.g. <see cref="ComponentsModel"/>, <see cref="ComponentsRelease"/>)
    /// so <see cref="Sw360PagedApiResponseFetcher"/> can page through and merge them with one shared, type-agnostic routine.
    /// </summary>
    /// <typeparam name="TItem">The embedded item type for this page (e.g. <see cref="Sw360Components"/>).</typeparam>
    public interface IPagedFetchResult<TItem>
    {
        /// <summary>
        /// Gets the pagination metadata for this page.
        /// </summary>
        PageInfo Page { get; }

        /// <summary>
        /// Gets the items embedded in this page.
        /// </summary>
        IEnumerable<TItem> Items { get; }

        /// <summary>
        /// Appends items (typically from a subsequent page) onto this page's item list.
        /// </summary>
        void AddItems(IEnumerable<TItem> items);
    }
}
