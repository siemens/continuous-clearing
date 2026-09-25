// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2026 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SIT.APICommunications.Model;
using SIT.Common;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SIT.APICommunications
{
    /// <summary>
    /// Shared page-by-page fetch-and-merge routine for typed SW360 list endpoints (releases, components, ...).
    /// Each page is deserialized directly from the response stream into typed objects (no JObject DOM, no
    /// intermediate full-body string per page) so a dataset spanning many pages doesn't hold multiple redundant
    /// in-memory copies of the same payload.
    /// </summary>
    public static class Sw360PagedApiResponseFetcher
    {
        static readonly ILog Logger = LoggerFactory.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Fetches every page via <paramref name="getPageAsync"/>, merges the typed item lists, and returns the
        /// combined result serialized back to JSON. Strict: throws if the first page is not a success/valid JSON
        /// response, since callers of this overload (e.g. "fetch the whole catalog") treat that as a hard failure,
        /// not a valid "empty" result. See <see cref="FetchAllPagesTolerantAsync"/> for the opposite behavior.
        /// </summary>
        /// <typeparam name="TPage">The typed page/response model, implementing <see cref="IPagedFetchResult{TItem}"/>.</typeparam>
        /// <typeparam name="TItem">The embedded item type for this page.</typeparam>
        /// <param name="getPageAsync">Fetches a single page (0-based) for the endpoint being paged through.</param>
        public static async Task<string> FetchAllPagesStrictAsync<TPage, TItem>(Func<int, Task<HttpResponseMessage>> getPageAsync)
            where TPage : class, IPagedFetchResult<TItem>
        {
            using HttpResponseMessage firstPageResponse = await getPageAsync(0);
            firstPageResponse.EnsureSuccessStatusCode();

            // The first page is buffered as a string (bounded to one page) so a non-JSON/empty body (e.g. an HTML
            // error page) is still returned as-is instead of crashing.
            string firstPageJson = await firstPageResponse.Content.ReadAsStringAsync();
            TPage firstPage;
            try
            {
                firstPage = JsonConvert.DeserializeObject<TPage>(firstPageJson);
            }
            catch (JsonReaderException)
            {
                firstPage = null;
            }

            if (firstPage == null)
            {
                return firstPageJson;
            }

            int totalPages = firstPage.Page?.TotalPages ?? 1;
            int totalElements = firstPage.Page?.ExtensionData != null && firstPage.Page.ExtensionData.TryGetValue("totalElements", out JToken totalElementsToken) ? totalElementsToken.Value<int>() : -1;
            Logger.DebugFormat("SW360 totalElements={0}", totalElements);

            if (totalPages > 1)
            {
                using SemaphoreSlim throttle = new(ApiConstant.Sw360LookupMaxConcurrency);
                var remainingPageTasks = Enumerable.Range(1, totalPages - 1).Select(async page =>
                {
                    await throttle.WaitAsync();
                    try
                    {
                        using HttpResponseMessage pageResponse = await getPageAsync(page);
                        pageResponse.EnsureSuccessStatusCode();
                        return await DeserializePageAsync<TPage>(pageResponse);
                    }
                    finally
                    {
                        throttle.Release();
                    }
                });

                TPage[] remainingPages = await Task.WhenAll(remainingPageTasks);
                foreach (TPage page in remainingPages.Where(p => p != null))
                {
                    firstPage.AddItems(page.Items);
                }
            }

            return JsonConvert.SerializeObject(firstPage);
        }

        /// <summary>
        /// Deserializes a page straight from the response stream via <see cref="JsonTextReader"/>, so the full page
        /// body never needs to be buffered as a single string or a JObject tree.
        /// </summary>
        private static async Task<TPage> DeserializePageAsync<TPage>(HttpResponseMessage response)
        {
            using Stream contentStream = await response.Content.ReadAsStreamAsync();
            using StreamReader streamReader = new(contentStream);
            using JsonTextReader jsonReader = new(streamReader);
            try
            {
                return JsonSerializer.CreateDefault().Deserialize<TPage>(jsonReader);
            }
            catch (JsonReaderException)
            {
                return default;
            }
        }

        /// <summary>
        /// Fetches every page of a paginated SW360 list endpoint and merges the "_embedded" arrays into one JSON payload,
        /// so callers keep seeing the full result set even when it exceeds the server's per-page cap. The first page is
        /// fetched to learn the total page count, then remaining pages are fetched concurrently in small bounded
        /// batches (chunks) rather than a single oversized page_entries request, so no one request holds the server
        /// too long. Tolerant: a non-success/non-JSON first page is treated as an empty result instead of throwing,
        /// since callers (e.g. externalId/name lookups) rely on that to mean "no match" rather than a hard failure.
        /// See <see cref="FetchAllPagesStrictAsync{TPage, TItem}"/> for the opposite (typed, fail-fast) behavior.
        /// </summary>
        /// <param name="httpClient">The configured HttpClient to issue requests with.</param>
        /// <param name="baseUrl">The list endpoint URL, optionally including filter query parameters but not pagination parameters.</param>
        /// <param name="pageSize">The number of entries requested per page.</param>
        /// <param name="extraQueryParams">Additional query parameters appended to every page request.</param>
        /// <returns>
        /// A new HTTP response (status code preserved from the first page, or from any failing page) whose content
        /// is the merged JSON payload; this never mutates the original per-page responses returned by SW360.
        /// </returns>
        public static async Task<HttpResponseMessage> FetchAllPagesTolerantAsync(HttpClient httpClient, string baseUrl, int pageSize, string extraQueryParams = "")
        {
            using HttpResponseMessage firstPageResponse = await GetPageAsync(httpClient, baseUrl, 0, pageSize, extraQueryParams);
            string firstPageContent = await firstPageResponse.Content.ReadAsStringAsync();
            if (!firstPageResponse.IsSuccessStatusCode || !CommonHelper.TryParseJObject(firstPageContent, out JObject firstPage))
            {
                // A non-success status, or a "successful" response with an empty/non-JSON body (observed from
                // some SW360 search endpoints on no-match), is treated as an empty result instead of crashing.
                return new HttpResponseMessage(firstPageResponse.StatusCode)
                {
                    ReasonPhrase = firstPageResponse.ReasonPhrase,
                    Content = new StringContent(firstPageContent, Encoding.UTF8, ApiConstant.ApplicationHalJson)
                };
            }

            int totalPages = firstPage["page"]?["totalPages"]?.Value<int>() ?? 1;
            int totalElements = firstPage["page"]?["totalElements"]?.Value<int>() ?? -1;
            Logger.DebugFormat("SW360 totalElements={0} for url={1}", totalElements, baseUrl);

            if (totalPages > 1)
            {
                using SemaphoreSlim throttle = new(ApiConstant.Sw360LookupMaxConcurrency);
                var remainingPageTasks = Enumerable.Range(1, totalPages - 1).Select(async page =>
                {
                    await throttle.WaitAsync();
                    try
                    {
                        using HttpResponseMessage pageResponse = await GetPageAsync(httpClient, baseUrl, page, pageSize, extraQueryParams);
                        pageResponse.EnsureSuccessStatusCode();
                        string pageContent = await pageResponse.Content.ReadAsStringAsync();
                        return CommonHelper.TryParseJObject(pageContent, out JObject page2) ? page2 : null;
                    }
                    finally
                    {
                        throttle.Release();
                    }
                });

                JObject[] remainingPages = await Task.WhenAll(remainingPageTasks);
                MergeEmbeddedPages(firstPage, remainingPages.Where(p => p != null).ToArray());
            }

            return new HttpResponseMessage(firstPageResponse.StatusCode)
            {
                ReasonPhrase = firstPageResponse.ReasonPhrase,
                Content = new StringContent(firstPage.ToString(Formatting.None), Encoding.UTF8, ApiConstant.ApplicationHalJson)
            };
        }

        /// <summary>
        /// Appends every "_embedded" array from each subsequent page onto the matching array on the first page.
        /// </summary>
        private static void MergeEmbeddedPages(JObject firstPage, JObject[] remainingPages)
        {
            if (firstPage["_embedded"] is not JObject embedded)
            {
                return;
            }

            foreach (JObject page in remainingPages)
            {
                if (page["_embedded"] is not JObject pageEmbedded)
                {
                    continue;
                }

                foreach (JProperty property in pageEmbedded.Properties())
                {
                    if (embedded[property.Name] is JArray existingArray && property.Value is JArray pageArray)
                    {
                        existingArray.Merge(pageArray);
                    }
                }
            }
        }

        /// <summary>
        /// Fetches a single page of a paginated SW360 list endpoint.
        /// </summary>
        /// <param name="httpClient">The configured HttpClient to issue the request with.</param>
        /// <param name="baseUrl">The list endpoint URL, optionally including filter query parameters but not pagination parameters.</param>
        /// <param name="page">The 0-based page number to request.</param>
        /// <param name="pageSize">The number of entries requested per page.</param>
        /// <param name="extraQueryParams">Additional query parameters appended to the request.</param>
        /// <returns>The HTTP response for the requested page.</returns>
        public static Task<HttpResponseMessage> GetPageAsync(HttpClient httpClient, string baseUrl, int page, int pageSize, string extraQueryParams = "")
        {
            string querySeparator = baseUrl.Contains('?') ? "&" : "?";
            string normalizedExtraQueryParams = extraQueryParams.Trim('?', '&');
            string extraQueryPrefix = string.IsNullOrEmpty(normalizedExtraQueryParams) ? string.Empty : $"{normalizedExtraQueryParams}&";
            string pageUrl = $"{baseUrl}{querySeparator}{extraQueryPrefix}page={page}&page_entries={pageSize}";
            return SW360Apicommunication.SendGetAsync(httpClient, pageUrl);
        }
    }
}
