// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.APICommunications.Interfaces;
using LCT.APICommunications.Model;
using System;
using System.Collections.Generic;
using System.Net.Http;
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
            query.Append("\"}).include(\"repo\", \"path\", \"name\")");

            string aqlQueryToBody = query.ToString();
            string uri = $"{DomainName}{ApiConstant.JfrogArtifactoryApiSearchAql}";
            HttpContent httpContent = new StringContent(aqlQueryToBody);
            return await httpClient.PostAsync(uri, httpContent);
        }

        /// <summary>
        /// Gets the package information in the repo, via the name or path
        /// </summary>
        /// <param name="repoName">repoName</param>
        /// <param name="packageName">repoName</param>
        /// <param name="path">repoName</param>
        /// <returns>AqlResult</returns>
        public async Task<HttpResponseMessage> GetPackageInfo(string repoName, string packageName)
        {
            HttpClient httpClient = GetHttpClient(ArtifactoryCredentials);
            TimeSpan timeOutInSec = TimeSpan.FromSeconds(TimeoutInSec);
            httpClient.Timeout = timeOutInSec;

            StringBuilder query = new();
            query.Append("items.find({\"repo\":\"");
            query.Append($"{repoName}\", ");
            query.Append($"\"name\": \"{packageName}\"");
            query.Append("}).include(\"repo\", \"path\", \"name\").limit(1)");

            string aqlQueryToBody = query.ToString();
            string uri = $"{DomainName}{ApiConstant.JfrogArtifactoryApiSearchAql}";
            HttpContent httpContent = new StringContent(aqlQueryToBody);
            return await httpClient.PostAsync(uri, httpContent);
        }

        public async Task<HttpResponseMessage> GetPackageInfo(string repoName, string packageName = null, string path = null)
        {
            ValidateParameters(packageName, path);

            var aqlQueryToBody = BuildAqlQuery(repoName, packageName, path);

            string uri = $"{DomainName}{ApiConstant.JfrogArtifactoryApiSearchAql}";
            HttpContent httpContent = new StringContent(aqlQueryToBody);

            return await ExecuteSearchAqlAsync(uri, httpContent);
        }

        private static HttpClient GetHttpClient(ArtifactoryCredentials credentials)
        {
            HttpClient httpClient = new HttpClient();

            httpClient.DefaultRequestHeaders.Add(ApiConstant.JFrog_API_Header, credentials.ApiKey);
            httpClient.DefaultRequestHeaders.Add(ApiConstant.Email, credentials.Email);
            return httpClient;
        }

        private void ValidateParameters(string packageName, string path)
        {
            if (string.IsNullOrEmpty(packageName) && string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("Either packageName or path, or both must be provided.");
            }
        }

        private string BuildAqlQuery(string repoName, string packageName, string path)
        {
            var queryList = new List<string>()
            {
                $"\"repo\":{{\"$eq\":\"{repoName}\"}}"
            };

            if (!string.IsNullOrEmpty(path))
            {
                queryList.Add($"\"path\":{{\"$match\":\"{path}\"}}");
            }

            if (!string.IsNullOrEmpty(packageName))
            {
                queryList.Add($"\"name\":{{\"$match\":\"{packageName}\"}}");
            }

            StringBuilder query = new();
            query.Append($"items.find({{{string.Join(", ", queryList)}}}).include(\"repo\", \"path\", \"name\").limit(1)");

            return query.ToString();
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
