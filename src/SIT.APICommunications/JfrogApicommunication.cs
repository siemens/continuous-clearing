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
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace SIT.APICommunications
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
        /// Asynchronously sets (or updates) item properties on the destination artifact in JFrog
        /// Artifactory after a successful copy/move operation. The destination path is derived from
        /// the component's copy/move API URL, so this works uniformly across all package types.
        /// </summary>
        /// <param name="component">The component whose destination artifact should be updated.</param>
        /// <param name="properties">The metadata properties to set, keyed by property name.</param>
        /// <returns>An HttpResponseMessage indicating the result of the property update.</returns>
        public virtual async Task<HttpResponseMessage> SetPackagePropertiesInJfrog(ComponentsToArtifactory component, IReadOnlyDictionary<string, string> properties)
        {
            if (component == null || properties == null || properties.Count == 0)
            {
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            }

            string destinationPath = GetDestinationStoragePath(component);
            if (string.IsNullOrEmpty(destinationPath))
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    ReasonPhrase = "Unable to resolve destination path for property update"
                };
            }

            string encodedProperties = string.Join(";", properties
                .Where(p => !string.IsNullOrEmpty(p.Key) && !string.IsNullOrEmpty(p.Value))
                .Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));

            if (string.IsNullOrEmpty(encodedProperties))
            {
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            }

            string url = $"{DomainName}/api/storage/{destinationPath}?properties={encodedProperties}&recursive=1";

            HttpClient httpClient = GetHttpClient(ArtifactoryCredentials);
            httpClient.SetLogWarnings(false, "unable to update package properties in jfrog repository");
            const HttpContent httpContent = null;

            await LogHandlingHelper.HttpRequestHandling("Set Package Properties In Jfrog", "MethodName:SetPackagePropertiesInJfrog()", httpClient, url, httpContent);
            HttpResponseMessage response = await httpClient.PutAsync(url, httpContent);
            await LogHandlingHelper.HttpResponseHandling("Set Package Properties In Jfrog", "MethodName:SetPackagePropertiesInJfrog()", response, "");

            return response;
        }

        /// <summary>
        /// Extracts the destination storage path (repo/path) from the component's copy/move API URL.
        /// The copy/move URLs end with a <c>?to=/{repo}/{path}</c> segment that identifies the final
        /// artifact location. Trailing wildcards and dry-run suffixes are removed.
        /// </summary>
        /// <param name="component">The component to resolve the destination path for.</param>
        /// <returns>The destination storage path, or an empty string when it cannot be resolved.</returns>
        private static string GetDestinationStoragePath(ComponentsToArtifactory component)
        {
            string sourceUrl = component.PackageType == PackageType.Internal
                ? component.MovePackageApiUrl
                : component.CopyPackageApiUrl;

            if (string.IsNullOrEmpty(sourceUrl))
            {
                return string.Empty;
            }

            const string marker = "?to=/";
            int markerIndex = sourceUrl.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (markerIndex < 0)
            {
                return string.Empty;
            }

            string path = sourceUrl.Substring(markerIndex + marker.Length);

            // Remove any trailing query parameters (e.g. &dry=1) that follow the destination.
            int ampersandIndex = path.IndexOf('&');
            if (ampersandIndex >= 0)
            {
                path = path.Substring(0, ampersandIndex);
            }

            // Remove trailing wildcard used for AQL searches (Debian/Conan).
            path = path.TrimEnd('*');

            return path.Trim('/');
        }

        #endregion Methods
    }
}
