﻿// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT

// -------------------------------------------------------------------------------------------------------------------- 

using LCT.APICommunications.Model;
using LCT.Common;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace LCT.APICommunications
{
    public class PythonJfrogApiCommunication : JfrogApicommunication
    {
        #region Properties
        /// <summary>
        /// Gets or sets the timeout duration in seconds for HTTP requests.
        /// </summary>
        private static int TimeoutInSec { get; set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="PythonJfrogApiCommunication"/> class.
        /// </summary>
        /// <param name="repoDomainName">The repository domain name.</param>
        /// <param name="srcrepoName">The source repository name.</param>
        /// <param name="repoCredentials">The artifactory credentials.</param>
        /// <param name="timeout">The timeout duration in seconds.</param>
        public PythonJfrogApiCommunication(string repoDomainName, string srcrepoName, ArtifactoryCredentials repoCredentials, int timeout) : base(repoDomainName, srcrepoName, repoCredentials, timeout)
        {
            TimeoutInSec = timeout;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Gets a configured HTTP client with authentication headers.
        /// </summary>
        /// <param name="credentials">The artifactory credentials.</param>
        /// <returns>A configured HTTP client instance.</returns>
        private static HttpClient GetHttpClient(ArtifactoryCredentials credentials)
        {
            var handler = new RetryHttpClientHandler()
            {
                InnerHandler = new HttpClientHandler()
            };
            var httpClient = new HttpClient(handler);
            TimeSpan timeOutInSec = TimeSpan.FromSeconds(TimeoutInSec);
            httpClient.Timeout = timeOutInSec;
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", credentials.Token);
            return httpClient;
        }

        /// <summary>
        /// Asynchronously retrieves the API key from the Artifactory server.
        /// </summary>
        /// <returns>An HTTP response message containing the API key information.</returns>
        public override async Task<HttpResponseMessage> GetApiKey()
        {
            string url = $"{DomainName}/api/security/apiKey";
            return await GetHttpClient(ArtifactoryCredentials).GetAsync(url);
        }

        /// <summary>
        /// Asynchronously copies a package from the remote repository.
        /// </summary>
        /// <param name="component">The component to copy to Artifactory.</param>
        /// <returns>An HTTP response message indicating the result of the copy operation.</returns>
        public override async Task<HttpResponseMessage> CopyFromRemoteRepo(ComponentsToArtifactory component)
        {
            HttpClient httpClient = GetHttpClient(ArtifactoryCredentials);
            await LogHandlingHelper.HttpRequestHandling("Package copy from remote repository", $"MethodName:CopyFromRemoteRepo()", httpClient, component.CopyPackageApiUrl, null);
            return await httpClient.PostAsync(component.CopyPackageApiUrl, null);
        }

        /// <summary>
        /// Asynchronously moves a package from the remote repository.
        /// </summary>
        /// <param name="component">The component to move in Artifactory.</param>
        /// <returns>An HTTP response message indicating the result of the move operation.</returns>
        public override async Task<HttpResponseMessage> MoveFromRepo(ComponentsToArtifactory component)
        {
            HttpClient httpClient = GetHttpClient(ArtifactoryCredentials);
            await LogHandlingHelper.HttpRequestHandling("Package Move from remote repository", $"MethodName:MoveFromRepo()", httpClient, component.MovePackageApiUrl, null);
            return await httpClient.PostAsync(component.MovePackageApiUrl, null);
        }

        /// <summary>
        /// Asynchronously retrieves package information from Artifactory.
        /// </summary>
        /// <param name="component">The component to retrieve information for.</param>
        /// <returns>An HTTP response message containing the package information.</returns>
        public override async Task<HttpResponseMessage> GetPackageInfo(ComponentsToArtifactory component)
        {
            return await GetHttpClient(ArtifactoryCredentials).GetAsync(component.PackageInfoApiUrl);
        }

        /// <summary>
        /// Updates package properties in JFrog Artifactory with SW360 release URL.
        /// </summary>
        /// <param name="sw360releaseUrl">The SW360 release URL.</param>
        /// <param name="destRepoName">The destination repository name.</param>
        /// <param name="uploadArgs">The upload arguments containing release information.</param>
        public override void UpdatePackagePropertiesInJfrog(string sw360releaseUrl, string destRepoName, UploadArgs uploadArgs)
        {
            string url = $"{DomainName}/api/storage/{destRepoName}/{uploadArgs.ReleaseName}.{uploadArgs.Version}.pypi?" +
                 $"properties=sw360url={sw360releaseUrl}";
            GetHttpClient(ArtifactoryCredentials).PutAsync(url, null);
        }
        #endregion
    }
}