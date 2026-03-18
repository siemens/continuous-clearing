// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.APICommunications.Interfaces;
using LCT.APICommunications.Model;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace LCT.APICommunications
{
    /// <summary>
    /// Abstract base class for JFrog Artifactory API communication operations.
    /// </summary>
    public abstract class JfrogApicommunication : IJFrogApiCommunication
    {
        #region Properties

        /// <summary>
        /// Gets or sets the domain name of the JFrog Artifactory server.
        /// </summary>
        protected string DomainName { get; set; }

        /// <summary>
        /// Gets or sets the credentials for accessing the Artifactory repository.
        /// </summary>
        protected ArtifactoryCredentials ArtifactoryCredentials { get; set; }

        /// <summary>
        /// Gets or sets the source repository name.
        /// </summary>
        protected string SourceRepoName { get; set; }

        /// <summary>
        /// Gets or sets the timeout value in seconds for HTTP requests.
        /// </summary>
        private static int TimeoutInSec { get; set; }

        #endregion Properties

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="JfrogApicommunication"/> class.
        /// </summary>
        /// <param name="repoDomainName">The domain name of the repository.</param>
        /// <param name="srcrepoName">The source repository name.</param>
        /// <param name="artifactoryCredentials">The credentials for accessing the Artifactory repository.</param>
        /// <param name="timeout">The timeout value in seconds for HTTP requests.</param>
        protected JfrogApicommunication(string repoDomainName, string srcrepoName, ArtifactoryCredentials artifactoryCredentials, int timeout)
        {
            DomainName = repoDomainName;
            ArtifactoryCredentials = artifactoryCredentials;
            SourceRepoName = srcrepoName;
            TimeoutInSec = timeout;
        }

        #endregion Constructors

        #region Methods

        /// <summary>
        /// Creates and configures an HttpClient instance with authentication and timeout settings.
        /// </summary>
        /// <param name="credentials">The Artifactory credentials for authentication.</param>
        /// <returns>A configured HttpClient instance.</returns>
        protected HttpClient GetHttpClient(ArtifactoryCredentials credentials)
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
        /// Asynchronously deletes a package from the JFrog repository.
        /// </summary>
        /// <param name="repoName">The name of the repository containing the package.</param>
        /// <param name="componentName">The name of the component to delete.</param>
        /// <returns>An HttpResponseMessage indicating the result of the delete operation.</returns>
        public async Task<HttpResponseMessage> DeletePackageFromJFrogRepo(string repoName, string componentName)
        {
            HttpClient httpClient = GetHttpClient(ArtifactoryCredentials);
            httpClient.SetLogWarnings(false, "unable to delete package from jfrog repository");
            string url = $"{DomainName}/{repoName}/{componentName}";
            return await httpClient.DeleteAsync(url);
        }

        /// <summary>
        /// Asynchronously retrieves the API key from the Artifactory server.
        /// </summary>
        /// <returns>An HttpResponseMessage containing the API key response.</returns>
        public abstract Task<HttpResponseMessage> GetApiKey();

        /// <summary>
        /// Asynchronously retrieves package information from the Artifactory repository.
        /// </summary>
        /// <param name="component">The component containing the package information request details.</param>
        /// <returns>An HttpResponseMessage containing the package information.</returns>
        public abstract Task<HttpResponseMessage> GetPackageInfo(ComponentsToArtifactory component);

        /// <summary>
        /// Asynchronously copies a package from a remote repository to the local repository.
        /// </summary>
        /// <param name="component">The component containing the copy operation details.</param>
        /// <returns>An HttpResponseMessage indicating the result of the copy operation.</returns>
        public abstract Task<HttpResponseMessage> CopyFromRemoteRepo(ComponentsToArtifactory component);

        /// <summary>
        /// Asynchronously moves a package from one repository to another.
        /// </summary>
        /// <param name="component">The component containing the move operation details.</param>
        /// <returns>An HttpResponseMessage indicating the result of the move operation.</returns>
        public abstract Task<HttpResponseMessage> MoveFromRepo(ComponentsToArtifactory component);

        /// <summary>
        /// Updates package properties in JFrog Artifactory with SW360 release URL information.
        /// </summary>
        /// <param name="sw360releaseUrl">The SW360 release URL to associate with the package.</param>
        /// <param name="destRepoName">The destination repository name.</param>
        /// <param name="uploadArgs">The upload arguments containing package details.</param>
        public abstract void UpdatePackagePropertiesInJfrog(string sw360releaseUrl, string destRepoName, UploadArgs uploadArgs);

        #endregion Methods
    }
}
