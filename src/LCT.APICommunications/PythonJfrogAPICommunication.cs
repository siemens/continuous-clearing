// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT

// -------------------------------------------------------------------------------------------------------------------- 

using LCT.APICommunications.Model;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace LCT.APICommunications
{
    /// <summary>
    /// Provides JFrog Artifactory API communication functionality specific to Python packages.
    /// </summary>
    public class PythonJfrogApiCommunication : JfrogApicommunication
    {
        #region Properties

        /// <summary>
        /// Gets or sets the timeout value in seconds for HTTP requests.
        /// </summary>
        private static int TimeoutInSec { get; set; }

        #endregion Properties

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="PythonJfrogApiCommunication"/> class.
        /// </summary>
        /// <param name="repoDomainName">The domain name of the repository.</param>
        /// <param name="srcrepoName">The source repository name.</param>
        /// <param name="repoCredentials">The credentials for accessing the Artifactory repository.</param>
        /// <param name="timeout">The timeout value in seconds for HTTP requests.</param>
        public PythonJfrogApiCommunication(string repoDomainName, string srcrepoName, ArtifactoryCredentials repoCredentials, int timeout) : base(repoDomainName, srcrepoName, repoCredentials, timeout)
        {
            TimeoutInSec = timeout;
        }

        #endregion Constructors

        #region Methods

        /// <summary>
        /// Creates and configures an HttpClient instance with authentication and timeout settings.
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
            TimeSpan timeOutInSec = TimeSpan.FromSeconds(TimeoutInSec);
            httpClient.Timeout = timeOutInSec;
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", credentials.Token);
            return httpClient;
        }

        /// <summary>
        /// Asynchronously retrieves the API key from the Artifactory server.
        /// </summary>
        /// <returns>An HttpResponseMessage containing the API key response.</returns>
        public override async Task<HttpResponseMessage> GetApiKey()
        {
            HttpClient httpClient = GetHttpClient(ArtifactoryCredentials);
            string url = $"{DomainName}/api/security/apiKey";
            return await httpClient.GetAsync(url);
        }

        /// <summary>
        /// Asynchronously copies a package from a remote repository to the local repository.
        /// </summary>
        /// <param name="component">The component containing the copy package API URL.</param>
        /// <returns>An HttpResponseMessage indicating the result of the copy operation.</returns>
        public override async Task<HttpResponseMessage> CopyFromRemoteRepo(ComponentsToArtifactory component)
        {
            HttpClient httpClient = GetHttpClient(ArtifactoryCredentials);
            const HttpContent httpContent = null;
            return await httpClient.PostAsync(component.CopyPackageApiUrl, httpContent);
        }

        /// <summary>
        /// Asynchronously moves a package from one repository to another.
        /// </summary>
        /// <param name="component">The component containing the move package API URL.</param>
        /// <returns>An HttpResponseMessage indicating the result of the move operation.</returns>
        public override async Task<HttpResponseMessage> MoveFromRepo(ComponentsToArtifactory component)
        {
            HttpClient httpClient = GetHttpClient(ArtifactoryCredentials);
            const HttpContent httpContent = null;
            return await httpClient.PostAsync(component.MovePackageApiUrl, httpContent);
        }

        /// <summary>
        /// Asynchronously retrieves package information from the Artifactory repository.
        /// </summary>
        /// <param name="component">The component containing the package info API URL.</param>
        /// <returns>An HttpResponseMessage containing the package information.</returns>
        public override async Task<HttpResponseMessage> GetPackageInfo(ComponentsToArtifactory component)
        {
            HttpClient httpClient = GetHttpClient(ArtifactoryCredentials);
            return await httpClient.GetAsync(component.PackageInfoApiUrl);
        }

        /// <summary>
        /// Updates package properties in JFrog Artifactory with SW360 release URL information.
        /// </summary>
        /// <param name="sw360releaseUrl">The SW360 release URL to associate with the package.</param>
        /// <param name="destRepoName">The destination repository name.</param>
        /// <param name="uploadArgs">The upload arguments containing release name and version.</param>
        public override void UpdatePackagePropertiesInJfrog(string sw360releaseUrl, string destRepoName, UploadArgs uploadArgs)
        {
            HttpClient httpClient = GetHttpClient(ArtifactoryCredentials);
            const HttpContent httpContent = null;
            string url = $"{DomainName}/api/storage/{destRepoName}/{uploadArgs.ReleaseName}.{uploadArgs.Version}.pypi?" +
                 $"properties=sw360url={sw360releaseUrl}";
            httpClient.PutAsync(url, httpContent);
        }

        #endregion Methods
    }
}


