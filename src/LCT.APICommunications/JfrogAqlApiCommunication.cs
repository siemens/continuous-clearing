// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.APICommunications.Interfaces;
using LCT.APICommunications.Model;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
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
            return await httpClient.GetAsync(url);
        }

        /// <summary>
        /// Gets the Internal Component Data By Repo name
        /// </summary>
        /// <param name="repoName"></param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> GetInternalComponentDataByRepo(string repoName)
        {
            HttpClient httpClient = GetHttpClient(ArtifactoryCredentials);
            TimeSpan timeOutInSec = TimeSpan.FromSeconds(TimeoutInSec);
            httpClient.Timeout = timeOutInSec;

            StringBuilder query = new();
            query.Append("items.find({\"repo\":\"");
            query.Append($"{repoName}");
            query.Append("\"}).include(\"repo\", \"path\", \"name\", \"actual_sha1\",\"actual_md5\",\"sha256\")");

            string aqlQueryToBody = query.ToString();
            string uri = $"{DomainName}{ApiConstant.JfrogArtifactoryApiSearchAql}";
            HttpContent httpContent = new StringContent(aqlQueryToBody);
            return await httpClient.PostAsync(uri, httpContent);
        }
        public async Task<HttpResponseMessage> GetNpmComponentDataByRepo(string repoName)
        {
            HttpClient httpClient = GetHttpClient(ArtifactoryCredentials);
            TimeSpan timeOutInSec = TimeSpan.FromSeconds(TimeoutInSec);
            httpClient.Timeout = timeOutInSec;

            StringBuilder query = new();
            query.Append("items.find({\"repo\":\"");
            query.Append($"{repoName}");
            query.Append("\"}).include(\"repo\", \"path\", \"name\",\"@npm.name\",\"@npm.version\", \"actual_sha1\",\"actual_md5\",\"sha256\")");

            string aqlQueryToBody = query.ToString();
            string uri = $"{DomainName}{ApiConstant.JfrogArtifactoryApiSearchAql}";
            HttpContent httpContent = new StringContent(aqlQueryToBody);
            return await httpClient.PostAsync(uri, httpContent);
        }
        public async Task<HttpResponseMessage> GetPypiComponentDataByRepo(string repoName)
        {
            HttpClient httpClient = GetHttpClient(ArtifactoryCredentials);
            TimeSpan timeOutInSec = TimeSpan.FromSeconds(TimeoutInSec);
            httpClient.Timeout = timeOutInSec;

            StringBuilder query = new();
            query.Append("items.find({\"repo\":\"");
            query.Append($"{repoName}");
            query.Append("\"}).include(\"repo\", \"path\", \"name\",\"@pypi.normalized.name\",\"@pypi.version\", \"actual_sha1\",\"actual_md5\",\"sha256\")");

            string aqlQueryToBody = query.ToString();
            string uri = $"{DomainName}{ApiConstant.JfrogArtifactoryApiSearchAql}";
            HttpContent httpContent = new StringContent(aqlQueryToBody);
            return await httpClient.PostAsync(uri, httpContent);
        }
        public async Task<HttpResponseMessage> GetCargoComponentDataByRepo(string repoName)
        {
            HttpClient httpClient = GetHttpClient(ArtifactoryCredentials);
            TimeSpan timeOutInSec = TimeSpan.FromSeconds(TimeoutInSec);
            httpClient.Timeout = timeOutInSec;

            StringBuilder query = new();
            query.Append("items.find({\"repo\":\"");
            query.Append($"{repoName}");
            query.Append("\"}).include(\"repo\", \"path\", \"name\",\"@crate.name\",\"@crate.version\", \"actual_sha1\",\"actual_md5\",\"sha256\")");

            string aqlQueryToBody = query.ToString();
            string uri = $"{DomainName}{ApiConstant.JfrogArtifactoryApiSearchAql}";
            HttpContent httpContent = new StringContent(aqlQueryToBody);
            return await httpClient.PostAsync(uri, httpContent);
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

            if (component.ComponentType.Equals("NPM", StringComparison.InvariantCultureIgnoreCase))
            {
                var queryList = new List<string>
        {
            $"\"repo\":{{\"$eq\":\"{component.SrcRepoName}\"}}",
            $"\"@npm.name\":{{\"$eq\":\"{component.Name}\"}}",
            $"\"@npm.version\":{{\"$eq\":\"{component.Version}\"}}"
        };

                // Build the AQL query string
                StringBuilder query = new();
                query.Append($"items.find({{{string.Join(", ", queryList)}}}).include(\"repo\", \"path\", \"name\")");
                return query.ToString();
            }
            else if (component.ComponentType.Equals("Python", StringComparison.InvariantCultureIgnoreCase))
            {
                var queryList = new List<string>
        {
            $"\"repo\":{{\"$eq\":\"{component.SrcRepoName}\"}}",
            $"\"@pypi.normalized.name\":{{\"$eq\":\"{component.Name}\"}}",
            $"\"@pypi.version\":{{\"$eq\":\"{component.Version}\"}}"
        };

                // Build the AQL query string
                StringBuilder query = new();
                query.Append($"items.find({{{string.Join(", ", queryList)}}}).include(\"repo\", \"path\", \"name\")");

                return query.ToString();
            }
            else if (component.ComponentType.Equals("Nuget", StringComparison.InvariantCultureIgnoreCase))
            {
                // Build the AQL query for NuGet components
                StringBuilder query = new();
                query.Append("items.find({");
                query.Append("\"$and\": [");
                query.Append($"{{ \"repo\":{{ \"$eq\": \"{component.SrcRepoName}\" }} }},");
                query.Append("{ \"$or\":[");
                query.Append($"{{ \"@nuget.id\":{{ \"$eq\": \"{component.Name}\" }} }} ,");
                query.Append($"{{ \"@nuget.id\":{{ \"$eq\": \"{component.Name.ToLowerInvariant()}\" }} }}");
                query.Append("] },");
                query.Append($"{{ \"@nuget.version\":{{\"$eq\": \"{component.Version}\" }} }}");
                query.Append(']');
                query.Append("}).include(\"repo\", \"path\", \"name\").limit(1)");

                return query.ToString();
            }
            else if (component.ComponentType.Equals("Cargo", StringComparison.InvariantCultureIgnoreCase))
            {
                // Build the AQL query for Cargo components
                StringBuilder query = new();
                query.Append("items.find({");
                query.Append("\"$and\": [");
                query.Append($"{{ \"repo\":{{ \"$eq\": \"{component.SrcRepoName}\" }} }},");
                query.Append("{ \"$or\":[");
                query.Append($"{{ \"@carte.name\":{{ \"$eq\": \"{component.Name}\" }} }} ,");
                query.Append($"{{ \"@crate.name\":{{ \"$eq\": \"{component.Name.ToLowerInvariant()}\" }} }}");
                query.Append("] },");
                query.Append($"{{ \"@crate.version\":{{\"$eq\": \"{component.Version}\" }} }}");
                query.Append(']');
                query.Append("}).include(\"repo\", \"path\", \"name\").limit(1)");

                return query.ToString();
            }
            else
            {
                var queryList = new List<string>()
            {
                $"\"repo\":{{\"$eq\":\"{component.SrcRepoName}\"}}"
            };

                if (!string.IsNullOrEmpty(component.Path))
                {
                    queryList.Add($"\"path\":{{\"$match\":\"{component.Path}\"}}");
                }

                if (!string.IsNullOrEmpty(component.JfrogPackageName))
                {
                    queryList.Add($"\"name\":{{\"$match\":\"{component.JfrogPackageName}\"}}");
                }

                StringBuilder query = new();
                query.Append($"items.find({{{string.Join(", ", queryList)}}}).include(\"repo\", \"path\", \"name\").limit(1)");
                return query.ToString();
            }



        }

        private async Task<HttpResponseMessage> ExecuteSearchAqlAsync(string uri, HttpContent httpContent)
        {
            HttpClient httpClient = GetHttpClient(ArtifactoryCredentials);
            TimeSpan timeOutInSec = TimeSpan.FromSeconds(TimeoutInSec);
            httpClient.Timeout = timeOutInSec;

            return await httpClient.PostAsync(uri, httpContent);
        }


    }
}
