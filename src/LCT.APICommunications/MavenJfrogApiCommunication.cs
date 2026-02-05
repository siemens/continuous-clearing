// --------------------------------------------------------------------------------------------------------------------
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
    /// <summary>
    /// Provides Maven-specific JFrog Artifactory API communication operations.
    /// </summary>
    public class MavenJfrogApiCommunication : JfrogApicommunication
    {
        #region Properties

        /// <summary>
        /// Gets or sets the timeout in seconds for HTTP requests.
        /// </summary>
        private static int TimeoutInSec { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="MavenJfrogApiCommunication"/> class.
        /// </summary>
        /// <param name="repoDomainName">The repository domain name.</param>
        /// <param name="srcrepoName">The source repository name.</param>
        /// <param name="repoCredentials">The repository credentials.</param>
        /// <param name="timeout">The timeout in seconds.</param>
        public MavenJfrogApiCommunication(string repoDomainName, string srcrepoName, ArtifactoryCredentials repoCredentials, int timeout) : base(repoDomainName, srcrepoName, repoCredentials, timeout)
        {
            TimeoutInSec = timeout;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Creates and configures an HTTP client with authentication.
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
        /// Asynchronously retrieves the API key from Artifactory.
        /// </summary>
        /// <returns>An HTTP response message containing the API key.</returns>
        public override async Task<HttpResponseMessage> GetApiKey()
        {
            HttpClient httpClient = GetHttpClient(ArtifactoryCredentials);
            string url = $"{DomainName}/api/security/apiKey";
            return await httpClient.GetAsync(url);
        }

        /// <summary>
        /// Asynchronously copies a component from a remote repository.
        /// </summary>
        /// <param name="component">The component to copy.</param>
        /// <returns>An HTTP response message indicating the result of the copy operation.</returns>
        public override async Task<HttpResponseMessage> CopyFromRemoteRepo(ComponentsToArtifactory component)
        {
            HttpClient httpClient = GetHttpClient(ArtifactoryCredentials);
            const HttpContent httpContent = null;
            await LogHandlingHelper.HttpRequestHandling("Package copy from remote repository", $"MethodName:CopyFromRemoteRepo()", httpClient, component.CopyPackageApiUrl, httpContent);
            return await httpClient.PostAsync(component.CopyPackageApiUrl, httpContent);
        }

        /// <summary>
        /// Asynchronously moves a component from one repository to another.
        /// </summary>
        /// <param name="component">The component to move.</param>
        /// <returns>An HTTP response message indicating the result of the move operation.</returns>
        public override async Task<HttpResponseMessage> MoveFromRepo(ComponentsToArtifactory component)
        {
            HttpClient httpClient = GetHttpClient(ArtifactoryCredentials);
            const HttpContent httpContent = null;
            await LogHandlingHelper.HttpRequestHandling("Package Move from remote repository", $"MethodName:MoveFromRepo()", httpClient, component.MovePackageApiUrl, httpContent);
            return await httpClient.PostAsync(component.MovePackageApiUrl, httpContent);
        }

        /// <summary>
        /// Asynchronously retrieves package information from Artifactory.
        /// </summary>
        /// <param name="component">The component to retrieve information for.</param>
        /// <returns>An HTTP response message containing the package information.</returns>
        public override async Task<HttpResponseMessage> GetPackageInfo(ComponentsToArtifactory component)
        {
            HttpClient httpClient = GetHttpClient(ArtifactoryCredentials);
            return await httpClient.GetAsync(component.PackageInfoApiUrl);
        }

        /// <summary>
        /// Updates the package properties in JFrog Artifactory with the SW360 release URL.
        /// </summary>
        /// <param name="sw360releaseUrl">The SW360 release URL.</param>
        /// <param name="destRepoName">The destination repository name.</param>
        /// <param name="uploadArgs">The upload arguments containing package information.</param>
        public override void UpdatePackagePropertiesInJfrog(string sw360releaseUrl, string destRepoName, UploadArgs uploadArgs)
        {
            HttpClient httpClient = GetHttpClient(ArtifactoryCredentials);
            const HttpContent httpContent = null;
            string url = $"{DomainName}/api/storage/{destRepoName}/{uploadArgs.PackageName}/{uploadArgs.ReleaseName}/{uploadArgs.Version}/{uploadArgs.ReleaseName}.{uploadArgs.Version}-sources.jar?" +
                 $"properties=sw360url={sw360releaseUrl}";
            httpClient.PutAsync(url, httpContent);
        }

        #endregion
    }
}