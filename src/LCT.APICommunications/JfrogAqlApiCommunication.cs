// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.APICommunications.Interfaces;
using LCT.APICommunications.Model;
using LCT.Common;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace LCT.APICommunications
{
    /// <summary>
    /// The JfrogAqlApiCommunication class
    /// </summary>
    public class JfrogAqlApiCommunication : IJfrogAqlApiCommunication
    {
        protected string DomainName { get; set; }
        private static int TimeoutInSec { get; set; }
        protected ArtifactoryCredentials ArtifactoryCredentials { get; set; }

        /// <summary>
        /// The JfrogAqlApiCommunication constructor
        /// </summary>
        /// <param name="repoDomainName">repoDomainName</param>
        /// <param name="artifactoryCredentials">artifactoryCredentials</param>
        /// <param name="timeout">timeout</param>
        public JfrogAqlApiCommunication(string repoDomainName, ArtifactoryCredentials artifactoryCredentials, int timeout)
        {
            DomainName = repoDomainName;
            ArtifactoryCredentials = artifactoryCredentials;
            TimeoutInSec = timeout;
        }

        public async Task<HttpResponseMessage> CheckConnection()
        {
            HttpClient httpClient = GetHttpClient(ArtifactoryCredentials);
            string url = $"{DomainName}/api/security/apiKey";
            await LogHandlingHelper.HttpRequestHandling("JFrog Connection validation", $"Methodname:CheckConnection()", httpClient, url);
            return await httpClient.GetAsync(url);
        }

        private async Task<HttpResponseMessage> GetComponentDataByRepo(string repoName, string includeFields)
        {
            string aqlQueryToBody = BuildSimpleAqlQuery(repoName, includeFields);
            string uri = $"{DomainName}{ApiConstant.JfrogArtifactoryApiSearchAql}";
            HttpClient httpClient = GetHttpClient(ArtifactoryCredentials);
            TimeSpan timeOutInSec = TimeSpan.FromSeconds(TimeoutInSec);
            httpClient.Timeout = timeOutInSec;
            HttpContent httpContent = new StringContent(aqlQueryToBody);
            await LogHandlingHelper.HttpRequestHandling("Get component data from jfrog repository", $"MethodName:GetComponentDataByRepo()", httpClient, uri, httpContent);
            return await httpClient.PostAsync(uri, httpContent);
        }

        private static string BuildSimpleAqlQuery(string repoName, string includeFields)
        {
            // Helper to build simple AQL queries for repo and include fields
            return $"items.find({{\"repo\":\"{repoName}\"}}).include({includeFields})";
        }

        /// <summary>
        /// Gets the Internal Component Data By Repo name
        /// </summary>
        /// <param name="repoName"></param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> GetInternalComponentDataByRepo(string repoName)
        {
            return await GetComponentDataByRepo(repoName, "\"repo\", \"path\", \"name\", \"actual_sha1\",\"actual_md5\",\"sha256\"");
        }
        public async Task<HttpResponseMessage> GetNpmComponentDataByRepo(string repoName)
        {
            return await GetComponentDataByRepo(repoName, "\"repo\", \"path\", \"name\",\"@npm.name\",\"@npm.version\", \"actual_sha1\",\"actual_md5\",\"sha256\"");
        }
        public async Task<HttpResponseMessage> GetPypiComponentDataByRepo(string repoName)
        {
            return await GetComponentDataByRepo(repoName, "\"repo\", \"path\", \"name\",\"@pypi.normalized.name\",\"@pypi.version\", \"actual_sha1\",\"actual_md5\",\"sha256\"");
        }
        public async Task<HttpResponseMessage> GetCargoComponentDataByRepo(string repoName)
        {
            return await GetComponentDataByRepo(repoName, "\"repo\", \"path\", \"name\",\"@crate.name\",\"@crate.version\", \"actual_sha1\",\"actual_md5\",\"sha256\"");
        }

        /// <summary>
        /// Gets the package information in the repo, via the name or path
        /// </summary>

        public async Task<HttpResponseMessage> GetPackageInfo(ComponentsToArtifactory component = null)
        {
            ValidateParameters(component.JfrogPackageName, component.Path);

            var aqlQueryToBody = BuildAqlQuery(component);

            string uri = $"{DomainName}{ApiConstant.JfrogArtifactoryApiSearchAql}";
            HttpContent httpContent = new StringContent(aqlQueryToBody);

            return await ExecuteSearchAqlAsync(uri, httpContent);
        }

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

        private static void ValidateParameters(string packageName, string path)
        {
            if (string.IsNullOrEmpty(packageName) && string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("Either packageName or path, or both must be provided.");
            }
        }

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
            else if (component.ComponentType.Equals("Nuget", StringComparison.InvariantCultureIgnoreCase))
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

        // Helper for NPM/Python AQL query
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

        private async Task<HttpResponseMessage> ExecuteSearchAqlAsync(string uri, HttpContent httpContent)
        {
            HttpClient httpClient = GetHttpClient(ArtifactoryCredentials);
            TimeSpan timeOutInSec = TimeSpan.FromSeconds(TimeoutInSec);
            httpClient.Timeout = timeOutInSec;
            await LogHandlingHelper.HttpRequestHandling("Get package information from jfrog repository", $"MethodName:ExecuteSearchAqlAsync()", httpClient, uri, httpContent);
            return await httpClient.PostAsync(uri, httpContent);
        }


    }
}
