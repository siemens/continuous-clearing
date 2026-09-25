// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2026 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using SIT.APICommunications.Interfaces;
using SIT.APICommunications.Model;
using SIT.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;
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
        /// Asynchronously retrieves component data from a specified repository using AQL.
        /// Paginates through all results (using a "created" timestamp cursor, since JFrog's
        /// AQL ".offset()" operator requires an Enterprise license) and wraps the aggregated
        /// results back into a single HttpResponseMessage so callers can keep deserializing
        /// via the existing "results" JSON shape.
        /// </summary>
        /// <param name="repoName">The name of the repository to query.</param>
        /// <param name="includeFields">The fields to include in the AQL query result.</param>
        /// <returns>An HttpResponseMessage containing the aggregated component data.</returns>
        private async Task<HttpResponseMessage> GetComponentDataByRepo(string repoName, string includeFields)
        {
            // Fetch all pages so large repos aren't silently truncated.
            List<JsonNode> allResults = await GetAllComponentDataByRepoAsync(repoName, includeFields);

            // Re-wrap into JFrog's normal "{ results: [...] }" shape so existing callers are unaffected.
            var responseJson = new JsonObject
            {
                ["results"] = new JsonArray(allResults.Select(n => n?.DeepClone()).ToArray())
            };

            // Return a synthetic 200 OK response to keep the method signature unchanged.
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson.ToJsonString(), System.Text.Encoding.UTF8, "application/json")
            };
        }


        /// <summary>
        /// Retrieves all artifacts for a repo by paginating through JFrog AQL results
        /// using a "created" timestamp cursor.
        /// </summary>
        public async Task<List<JsonNode>> GetAllComponentDataByRepoAsync(string repoName, string includeFields)
        {
            int limit = ApiConstant.JfrogAqlPageLimit; // Max artifacts per page
            string lastCreatedTimestamp = null; // Cursor for the next page
            bool hasMoreResults = true;
            List<JsonNode> combinedResults = new List<JsonNode>();

            string uri = $"{DomainName}{ApiConstant.JfrogArtifactoryApiSearchAql}";
            HttpClient httpClient = GetHttpClient(ArtifactoryCredentials);
            httpClient.Timeout = TimeSpan.FromSeconds(TimeoutInSec);

            while (hasMoreResults)
            {
                // First iteration fetches from the start; later ones filter by cursor.
                string aqlQueryToBody = BuildProAqlQuery(repoName, includeFields, limit, lastCreatedTimestamp);
                HttpContent httpContent = new StringContent(aqlQueryToBody, System.Text.Encoding.UTF8, "text/plain");

                await LogHandlingHelper.HttpRequestHandling(
                    "Get component data from jfrog repository",
                    $"MethodName:GetComponentDataByRepo() | LastTimestamp: {lastCreatedTimestamp ?? "Initial Run"}",
                    httpClient, uri, httpContent
                );

                HttpResponseMessage response = await httpClient.PostAsync(uri, httpContent);
                if (!response.IsSuccessStatusCode)
                {
                    // Log the failure details before throwing, for diagnosability.
                    await LogHandlingHelper.HttpResponseHandling(
                        "AQL query failed",
                        $"MethodName:GetAllComponentDataByRepoAsync() | LastTimestamp: {lastCreatedTimestamp ?? "Initial Run"}",
                        response
                    );
                }
                response.EnsureSuccessStatusCode();

                string jsonString = await response.Content.ReadAsStringAsync();
                var jsonDocument = JsonNode.Parse(jsonString);
                var resultsArray = jsonDocument?["results"]?.AsArray();

                if (resultsArray != null && resultsArray.Count > 0)
                {
                    foreach (var node in resultsArray)
                    {
                        combinedResults.Add(node.Deserialize<JsonNode>());
                    }

                    // Advance cursor to the last item's "created" value (results sorted ascending).
                    var lastNode = resultsArray.Last();
                    lastCreatedTimestamp = lastNode?["created"]?.GetValue<string>();

                    // Full page => more data may remain; partial page => end of repo contents.
                    if (resultsArray.Count == limit && !string.IsNullOrEmpty(lastCreatedTimestamp))
                    {
                        // Continue to next batch
                    }
                    else
                    {
                        hasMoreResults = false;
                    }
                }
                else
                {
                    hasMoreResults = false; // Empty page => no more data
                }
            }

            return combinedResults;
        }

        /// <summary>
        /// Builds a cursor-paginated AQL query for a repo, filtering by "created" and sorting ascending.
        /// </summary>
        private static string BuildProAqlQuery(string repoName, string includeFields, int limit, string lastCreatedTimestamp)
        {
            // "created" is required as the pagination cursor, so always include it.
            if (!includeFields.Contains("created"))
            {
                includeFields += ",created";
            }

            // Normalize/quote each include field for the AQL ".include(...)" clause.
            string formattedFields = string.Join(",", includeFields
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(f => f.Trim().StartsWith("\"") ? f.Trim() : $"\"{f.Trim()}\""));

            // Filter by repo; add a "created > cursor" filter on subsequent pages.
            string criteria = $"\"repo\":\"{repoName}\"";
            if (!string.IsNullOrEmpty(lastCreatedTimestamp))
            {
                criteria += $",\"created\":{{\"$gt\":\"{lastCreatedTimestamp}\"}}";
            }

            // Ascending sort ensures a stable cursor order across pages.
            return $"items.find({{{criteria}}}).include({formattedFields}).sort({{\"$asc\":[\"created\"]}}).limit({limit})";
        }

        /// <summary>
        /// Builds a simple AQL query string for a repository with specified include fields.
        /// </summary>
        /// <param name="repoName">The name of the repository to query.</param>
        /// <param name="includeFields">The fields to include in the query result.</param>
        /// <returns>A formatted AQL query string.</returns>
        private static string BuildSimpleAqlQuery(string repoName, string includeFields)
        {
            // Helper to build simple AQL queries for repo and include fields
            return $"items.find({{\"repo\":\"{repoName}\"}}).include({includeFields})";
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
            return await GetComponentDataByRepo(repoName, "\"repo\", \"path\", \"name\",\"@pypi.normalized.name\",\"@pypi.version\", \"actual_sha1\",\"actual_md5\",\"sha256\"");
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
                return BuildAqlQueryWithFields(component.SrcRepoName, new[] { ("@pypi.normalized.name", component.Name), ("@pypi.version", component.Version) });
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
