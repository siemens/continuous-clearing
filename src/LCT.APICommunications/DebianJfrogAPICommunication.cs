// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.APICommunications.Model;
using LCT.Common;
using log4net;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading.Tasks;

namespace LCT.APICommunications
{
    /// <summary>
    /// Provides JFrog Artifactory API communication functionality specific to Debian packages.
    /// </summary>
    public class DebianJfrogAPICommunication : JfrogApicommunication
    {
        #region Fields

        /// <summary>
        /// The logger instance for logging messages and errors.
        /// </summary>
        static readonly ILog Logger = LoggerFactory.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Helper instance for environment-related operations.
        /// </summary>
        private static readonly EnvironmentHelper environmentHelper = new EnvironmentHelper();

        #endregion Fields

        #region Properties

        /// <summary>
        /// Gets or sets the timeout value in seconds for HTTP requests.
        /// </summary>
        private static int TimeoutInSec { get; set; }

        /// <summary>
        /// Gets the configured HttpClient instance for API communication.
        /// </summary>
        private readonly HttpClient _httpClient;

        #endregion Properties

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DebianJfrogAPICommunication"/> class.
        /// </summary>
        /// <param name="repoDomainName">The domain name of the repository.</param>
        /// <param name="srcrepoName">The source repository name.</param>
        /// <param name="repoCredentials">The credentials for accessing the Artifactory repository.</param>
        /// <param name="timeout">The timeout value in seconds for HTTP requests.</param>
        public DebianJfrogAPICommunication(string repoDomainName, string srcrepoName, ArtifactoryCredentials repoCredentials, int timeout) : base(repoDomainName, srcrepoName, repoCredentials, timeout)
        {
            TimeoutInSec = timeout;
            _httpClient = GetHttpClient(repoCredentials);
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
            string url = $"{DomainName}/api/security/apiKey";
            return await _httpClient.GetAsync(url);
        }

        /// <summary>
        /// Asynchronously copies a package from a remote repository to the local repository.
        /// </summary>
        /// <param name="component">The component containing the copy package API URL.</param>
        /// <returns>An HttpResponseMessage indicating the result of the copy operation.</returns>
        public override async Task<HttpResponseMessage> CopyFromRemoteRepo(ComponentsToArtifactory component)
        {
            return await _httpClient.PostAsync(component.CopyPackageApiUrl, null);
        }

        /// <summary>
        /// Asynchronously moves a package from one repository to another.
        /// </summary>
        /// <param name="component">The component containing the move package API URL.</param>
        /// <returns>An HttpResponseMessage indicating the result of the move operation.</returns>
        public override async Task<HttpResponseMessage> MoveFromRepo(ComponentsToArtifactory component)
        {
            return await _httpClient.PostAsync(component.MovePackageApiUrl, null);
        }

        /// <summary>
        /// Asynchronously retrieves package information from the Artifactory repository.
        /// </summary>
        /// <param name="component">The component containing the package info API URL.</param>
        /// <returns>An HttpResponseMessage containing the package information.</returns>
        public override async Task<HttpResponseMessage> GetPackageInfo(ComponentsToArtifactory component)
        {
            HttpResponseMessage responseMessage = new HttpResponseMessage();
            var result = responseMessage;
            try
            {
                result = await _httpClient.GetAsync(component.PackageInfoApiUrl);
            }
            catch (TaskCanceledException ex)
            {
                Logger.Debug($"{ex.Message}");
                Logger.Error("A timeout error is thrown from Jfrog server,Please wait for sometime and re run the pipeline again");
                environmentHelper.CallEnvironmentExit(-1);

            }
            return result;
        }

        /// <summary>
        /// Updates package properties in JFrog Artifactory with SW360 release URL information.
        /// </summary>
        /// <param name="sw360releaseUrl">The SW360 release URL to associate with the package.</param>
        /// <param name="destRepoName">The destination repository name.</param>
        /// <param name="uploadArgs">The upload arguments containing package name, release name, and version.</param>
        public override void UpdatePackagePropertiesInJfrog(string sw360releaseUrl, string destRepoName, UploadArgs uploadArgs)
        {
            string url = $"{DomainName}/api/storage/{destRepoName}/{uploadArgs.PackageName}/-/{uploadArgs.ReleaseName}-{uploadArgs.Version}.debian?" +
              $"properties=sw360url={sw360releaseUrl}";
            _httpClient.PutAsync(url, null);
        }

        #endregion Methods
    }
}



