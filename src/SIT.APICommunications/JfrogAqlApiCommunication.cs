// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2026 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using Newtonsoft.Json;
using SIT.APICommunications.Interfaces;
using SIT.APICommunications.Model;
using SIT.APICommunications.Model.AQL;
using SIT.Common;
using SIT.Common.Constants;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace SIT.APICommunications
{
    /// <summary>
    /// The JfrogAqlApiCommunication class
    /// </summary>
    public class JfrogAqlApiCommunication : IJfrogAqlApiCommunication
    {
        #region Properties

        /// <summary>
        /// Gets or sets the domain name of the JFrog Artifactory server.
        /// </summary>
        protected string DomainName { get; set; }

        /// <summary>
        /// Gets or sets the timeout value in seconds for HTTP requests.
        /// </summary>
        private static int TimeoutInSec { get; set; }

        /// <summary>
        /// Gets or sets the credentials for accessing the Artifactory repository.
        /// </summary>
        protected ArtifactoryCredentials ArtifactoryCredentials { get; set; }

        #endregion Properties

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="JfrogAqlApiCommunication"/> class.
        /// </summary>
        /// <param name="repoDomainName">The domain name of the repository.</param>
        /// <param name="artifactoryCredentials">The credentials for accessing the Artifactory repository.</param>
        /// <param name="timeout">The timeout value in seconds for HTTP requests.</param>
        public JfrogAqlApiCommunication(string repoDomainName, ArtifactoryCredentials artifactoryCredentials, int timeout)
        {
            DomainName = repoDomainName;
            ArtifactoryCredentials = artifactoryCredentials;
            TimeoutInSec = timeout;
        }

        #endregion Constructors

        #region Methods

        /// <summary>
        /// Asynchronously checks the connection to the Artifactory server.
        /// </summary>
        /// <returns>An HttpResponseMessage indicating the connection status.</returns>
        public async Task<HttpResponseMessage> CheckConnection()
        {
            HttpClient httpClient = GetHttpClient(ArtifactoryCredentials);
            string url = $"{DomainName}/api/security/apiKey";
            await LogHandlingHelper.HttpRequestHandling("JFrog Connection validation", $"Methodname:CheckConnection()", httpClient, url);
            return await httpClient.GetAsync(url);
        }

        /// <summary>
        /// <summary>
        /// Asynchronously retrieves component data from a specified repository using AQL. The first page is buffered
        /// once, then remaining pages are fetched in bounded, concurrent batches - continuing another batch only when
        /// the last page of the previous batch came back "full" (exactly <see cref="ApiConstant.AqlPageSize"/> items),
        /// which is the only reliable end-of-results signal: AQL's "range.total" reflects just the current page's
        /// count, not the true total across the whole matching set, so it cannot be used to precompute page count.
        /// Each page is deserialized directly from its response stream into <see cref="AqlResult"/> lists and merged,
        /// so no more than one page's worth of raw JSON is ever held in memory at a time.
        /// </summary>
        /// <param name="repoName">The name of the repository to query.</param>
        /// <param name="includeFields">The fields to include in the AQL query result.</param>
        /// <returns>An HttpResponseMessage containing the full, combined component data.</returns>
        private async Task<HttpResponseMessage> GetComponentDataByRepo(string repoName, string includeFields)
        {
            using HttpResponseMessage firstPageResponse = await GetAqlPageAsync(repoName, includeFields, 0);
            if (!firstPageResponse.IsSuccessStatusCode)
            {
                string failureContent = await firstPageResponse.Content.ReadAsStringAsync();
                return new HttpResponseMessage(firstPageResponse.StatusCode)
                {
                    ReasonPhrase = firstPageResponse.ReasonPhrase,
                    Content = new StringContent(failureContent)
                };
            }

            string firstPageJson = await firstPageResponse.Content.ReadAsStringAsync();
            AqlResponse firstPage;
            try
            {
                firstPage = JsonConvert.DeserializeObject<AqlResponse>(firstPageJson);
            }
            catch (JsonReaderException)
            {
                firstPage = null;
            }

            if (firstPage?.Results == null)
            {
                return new HttpResponseMessage(firstPageResponse.StatusCode) { Content = new StringContent(firstPageJson) };
            }

            int nextPage = 1;
            bool lastPageWasFull = firstPage.Results.Count == ApiConstant.AqlPageSize;

            while (lastPageWasFull)
            {
                using SemaphoreSlim throttle = new(ApiConstant.AqlMaxConcurrency);
                var batchTasks = Enumerable.Range(nextPage, ApiConstant.AqlMaxConcurrency).Select(async page =>
                {
                    await throttle.WaitAsync();
                    try
                    {
                        using HttpResponseMessage pageResponse = await GetAqlPageAsync(repoName, includeFields, page);
                        pageResponse.EnsureSuccessStatusCode();
                        return await DeserializePageAsync(pageResponse);
                    }
                    finally
                    {
                        throttle.Release();
                    }
                }).ToList();

                AqlResponse[] batchPages = await Task.WhenAll(batchTasks);
                foreach (AqlResult item in batchPages.Where(p => p?.Results != null).SelectMany(p => p.Results))
                {
                    firstPage.Results.Add(item);
                }

                lastPageWasFull = batchPages[^1]?.Results?.Count == ApiConstant.AqlPageSize;
                nextPage += ApiConstant.AqlMaxConcurrency;
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                // Preserve range.notification so callers can still detect a single page hitting the server's hard limit.
                Content = new StringContent(JsonConvert.SerializeObject(new { results = firstPage.Results, range = firstPage.Range }))
            };
        }

        /// <summary>
        /// Deserializes an AQL page straight from the response stream via <see cref="JsonTextReader"/>, so the full
        /// page body never needs to be buffered as a single string.
        /// </summary>
        private static async Task<AqlResponse> DeserializePageAsync(HttpResponseMessage response)
        {
            using Stream contentStream = await response.Content.ReadAsStreamAsync();
            using StreamReader streamReader = new(contentStream);
            using JsonTextReader jsonReader = new(streamReader);
            try
            {
                return JsonSerializer.CreateDefault().Deserialize<AqlResponse>(jsonReader);
            }
            catch (JsonReaderException)
            {
                return null;
            }
        }

        /// <summary>
        /// Requests a single AQL page (limit/offset) for a repository.
        /// </summary>
        /// <param name="repoName">The name of the repository to query.</param>
        /// <param name="includeFields">The fields to include in the AQL query result.</param>
        /// <param name="page">The 0-based page number to request.</param>
        /// <returns>The HTTP response for the requested page.</returns>
        private async Task<HttpResponseMessage> GetAqlPageAsync(string repoName, string includeFields, int page)
        {
            string aqlQueryToBody = BuildSimpleAqlQuery(repoName, includeFields, ApiConstant.AqlPageSize, page * ApiConstant.AqlPageSize);
            string uri = $"{DomainName}{ApiConstant.JfrogArtifactoryApiSearchAql}";
            HttpClient httpClient = GetHttpClient(ArtifactoryCredentials);
            httpClient.Timeout = TimeSpan.FromSeconds(TimeoutInSec);
            HttpContent httpContent = new StringContent(aqlQueryToBody);
            await LogHandlingHelper.HttpRequestHandling("Get component data from jfrog repository", $"MethodName:GetComponentDataByRepo() page:{page}", httpClient, uri, httpContent);
            return await httpClient.PostAsync(uri, httpContent);
        }

        /// <summary>
        /// Builds a simple AQL query string for a repository with specified include fields, paginated via limit/offset.
        /// </summary>
        /// <param name="repoName">The name of the repository to query.</param>
        /// <param name="includeFields">The fields to include in the query result.</param>
        /// <param name="limit">The maximum number of results to return for this page.</param>
        /// <param name="offset">The number of results to skip before returning this page.</param>
        /// <returns>A formatted AQL query string.</returns>
        private static string BuildSimpleAqlQuery(string repoName, string includeFields, int limit, int offset)
        {
            // AQL requires offset() before limit(), otherwise the server rejects it with a syntax error.
            return $"items.find({{\"repo\":\"{repoName}\"}}).include({includeFields}).offset({offset}).limit({limit})";
        }

        /// <summary>
        /// Asynchronously retrieves internal component data from a specified repository.
        /// </summary>
        /// <param name="repoName">The name of the repository to query.</param>
        /// <returns>An HttpResponseMessage containing the internal component data.</returns>
        public async Task<HttpResponseMessage> GetInternalComponentDataByRepo(string repoName)
        {
            return await GetComponentDataByRepo(repoName, "\"repo\", \"path\", \"name\", \"actual_sha1\",\"actual_md5\",\"sha256\"");
        }

        /// <summary>
        /// Asynchronously retrieves NPM component data from a specified repository.
        /// </summary>
        /// <param name="repoName">The name of the repository to query.</param>
        /// <returns>An HttpResponseMessage containing the NPM component data.</returns>
        public async Task<HttpResponseMessage> GetNpmComponentDataByRepo(string repoName)
        {
            return await GetComponentDataByRepo(repoName, "\"repo\", \"path\", \"name\",\"@npm.name\",\"@npm.version\", \"actual_sha1\",\"actual_md5\",\"sha256\"");
        }

        /// <summary>
        /// Asynchronously retrieves PyPI component data from a specified repository.
        /// </summary>
        /// <param name="repoName">The name of the repository to query.</param>
        /// <returns>An HttpResponseMessage containing the PyPI component data.</returns>
        public async Task<HttpResponseMessage> GetPypiComponentDataByRepo(string repoName)
        {
            return await GetComponentDataByRepo(repoName, $"\"repo\", \"path\", \"name\",\"@{Dataconstant.PypiNormalizedNameKey}\",\"@{Dataconstant.PypiVersionKey}\", \"actual_sha1\",\"actual_md5\",\"sha256\"");
        }

        /// <summary>
        /// Asynchronously retrieves Cargo component data from a specified repository.
        /// </summary>
        /// <param name="repoName">The name of the repository to query.</param>
        /// <returns>An HttpResponseMessage containing the Cargo component data.</returns>
        public async Task<HttpResponseMessage> GetCargoComponentDataByRepo(string repoName)
        {
            return await GetComponentDataByRepo(repoName, "\"repo\", \"path\", \"name\",\"@crate.name\",\"@crate.version\", \"actual_sha1\",\"actual_md5\",\"sha256\"");
        }

        /// <summary>
        /// Asynchronously retrieves package information from the repository via name or path.
        /// </summary>
        /// <param name="component">The component containing package name and path information.</param>
        /// <returns>An HttpResponseMessage containing the package information.</returns>
        public async Task<HttpResponseMessage> GetPackageInfo(ComponentsToArtifactory component = null)
        {
            ValidateParameters(component.JfrogPackageName, component.Path);

            var aqlQueryToBody = BuildAqlQuery(component);

            string uri = $"{DomainName}{ApiConstant.JfrogArtifactoryApiSearchAql}";
            HttpContent httpContent = new StringContent(aqlQueryToBody);

            return await ExecuteSearchAqlAsync(uri, httpContent);
        }

        /// <summary>
        /// Creates and configures an HttpClient instance with authentication settings.
        /// </summary>
        /// <param name="credentials">The Artifactory credentials for authentication.</param>
        /// <returns>A configured HttpClient instance.</returns>
        private static HttpClient GetHttpClient(ArtifactoryCredentials credentials)
        {
            var handler = new RetryHttpClientHandler()
            {
                InnerHandler = new HttpClientHandler()
            };
            var httpClient = new HttpClient(handler);
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", credentials.Token);
            return httpClient;
        }

        /// <summary>
        /// Validates that at least one of the required parameters is provided.
        /// </summary>
        /// <param name="packageName">The package name to validate.</param>
        /// <param name="path">The path to validate.</param>
        /// <exception cref="ArgumentException">Thrown when both packageName and path are null or empty.</exception>
        private static void ValidateParameters(string packageName, string path)
        {
            if (string.IsNullOrEmpty(packageName) && string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("Either packageName or path, or both must be provided.");
            }
        }

        /// <summary>
        /// Builds an AQL query string based on the component type and properties.
        /// </summary>
        /// <param name="component">The component containing query parameters.</param>
        /// <returns>A formatted AQL query string appropriate for the component type.</returns>
        public static string BuildAqlQuery(ComponentsToArtifactory component)
        {
            // Use a single helper for repeated AQL query construction
            if (component.ComponentType.Equals("NPM", StringComparison.InvariantCultureIgnoreCase))
            {
                return BuildAqlQueryWithFields(component.SrcRepoName, new[] { ("@npm.name", component.Name), ("@npm.version", component.Version) });
            }
            else if (component.ComponentType.Equals("Python", StringComparison.InvariantCultureIgnoreCase))
            {
                return BuildAqlQueryWithFields(component.SrcRepoName, new[] { ($"@{Dataconstant.PypiNormalizedNameKey}", CommonHelper.NormalizePypiName(component.Name)), ($"@{Dataconstant.PypiVersionKey}", component.Version) });
            }
            else if (component.ComponentType.Equals("Nuget", StringComparison.InvariantCultureIgnoreCase) || component.ComponentType.Equals("Choco", StringComparison.InvariantCultureIgnoreCase))
            {
                // NuGet: $and for repo, $or for id (case), and version
                return $"items.find({{\"$and\": [{{ \"repo\":{{ \"$eq\": \"{component.SrcRepoName}\" }} }},{{ \"$or\":[{{ \"@nuget.id\":{{ \"$eq\": \"{component.Name}\" }} }},{{ \"@nuget.id\":{{ \"$eq\": \"{component.Name.ToLowerInvariant()}\" }} }}]}},{{ \"@nuget.version\":{{\"$eq\": \"{component.Version}\" }} }}]}}).include(\"repo\", \"path\", \"name\").limit(1)";
            }
            else if (component.ComponentType.Equals("Cargo", StringComparison.InvariantCultureIgnoreCase))
            {
                // Cargo: $and for repo, $or for name, and version
                return $"items.find({{\"$and\": [{{ \"repo\":{{ \"$eq\": \"{component.SrcRepoName}\" }} }},{{ \"$or\":[{{ \"@crate.name\":{{ \"$eq\": \"{component.Name}\" }} }},{{ \"@crate.name\":{{ \"$eq\": \"{component.Name.ToLowerInvariant()}\" }} }}]}},{{ \"@crate.version\":{{\"$eq\": \"{component.Version}\" }} }}]}}).include(\"repo\", \"path\", \"name\").limit(1)";
            }
            else
            {
                var queryList = new List<string>()
                {
                    $"\"repo\":{{\"$eq\":\"{component.SrcRepoName}\"}}"
                };
                if (!string.IsNullOrEmpty(component.Path))
                {
                    queryList.Add($"\"path\":{{\"$match\":\"{component.Path}\"}}"
                    );
                }
                if (!string.IsNullOrEmpty(component.JfrogPackageName))
                {
                    queryList.Add($"\"name\":{{\"$match\":\"{component.JfrogPackageName}\"}}"
                    );
                }
                return $"items.find({{{string.Join(", ", queryList)}}}).include(\"repo\", \"path\", \"name\").limit(1)";
            }
        }

        /// <summary>
        /// Builds an AQL query string with specified repository and field-value pairs.
        /// </summary>
        /// <param name="repoName">The name of the repository to query.</param>
        /// <param name="fields">An array of field-value tuples to include in the query.</param>
        /// <returns>A formatted AQL query string.</returns>
        private static string BuildAqlQueryWithFields(string repoName, (string field, string value)[] fields)
        {
            var queryList = new List<string> { $"\"repo\":{{\"$eq\":\"{repoName}\"}}" };
            foreach (var (field, value) in fields)
            {
                queryList.Add($"\"{field}\":{{\"$eq\":\"{value}\"}}"
                );
            }
            return $"items.find({{{string.Join(", ", queryList)}}}).include(\"repo\", \"path\", \"name\")";
        }

        /// <summary>
        /// Asynchronously executes an AQL search query against the Artifactory server.
        /// </summary>
        /// <param name="uri">The URI endpoint for the AQL search.</param>
        /// <param name="httpContent">The HTTP content containing the AQL query.</param>
        /// <returns>An HttpResponseMessage containing the search results.</returns>
        private async Task<HttpResponseMessage> ExecuteSearchAqlAsync(string uri, HttpContent httpContent)
        {
            HttpClient httpClient = GetHttpClient(ArtifactoryCredentials);
            TimeSpan timeOutInSec = TimeSpan.FromSeconds(TimeoutInSec);
            httpClient.Timeout = timeOutInSec;
            await LogHandlingHelper.HttpRequestHandling("Get package information from jfrog repository", $"MethodName:ExecuteSearchAqlAsync()", httpClient, uri, httpContent);
            return await httpClient.PostAsync(uri, httpContent);
        }

        #endregion Methods
    }
}
