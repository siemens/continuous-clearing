// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.APICommunications.Interfaces;
using LCT.APICommunications.Model;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace LCT.APICommunications
{
    public class JfrogAqlApiCommunication : IJfrogAqlApiCommunication
    {
        protected string DomainName { get; set; }
        protected ArtifactoryCredentials ArtifactoryCredentials { get; set; }
        public JfrogAqlApiCommunication(string repoDomainName, ArtifactoryCredentials artifactoryCredentials)
        {
            DomainName = repoDomainName;
            ArtifactoryCredentials = artifactoryCredentials;
        }

        public async Task<HttpResponseMessage> GetInternalComponentDataByRepo(string repoName)
        {
            HttpClient httpClient = GetHttpClient(ArtifactoryCredentials);

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
