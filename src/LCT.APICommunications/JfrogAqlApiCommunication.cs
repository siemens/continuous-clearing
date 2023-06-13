// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.APICommunications.Interfaces;
using LCT.APICommunications.Model;
using System;
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

        private static HttpClient GetHttpClient(ArtifactoryCredentials credentials)
        {
            HttpClient httpClient = new HttpClient();

            httpClient.DefaultRequestHeaders.Add(ApiConstant.JFrog_API_Header, credentials.ApiKey);
            httpClient.DefaultRequestHeaders.Add(ApiConstant.Email, credentials.Email);
            return httpClient;
        }
    }
}
