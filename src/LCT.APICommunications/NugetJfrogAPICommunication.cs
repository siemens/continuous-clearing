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
using System.Reflection;
using System.Threading.Tasks;

namespace LCT.APICommunications
{
    /// <summary>
    /// Provides NuGet-specific JFrog Artifactory API communication operations.
    /// </summary>
    public class NugetJfrogApiCommunication : JfrogApicommunication
    {
        #region Fields

        /// <summary>
        /// Logger instance for this class.
        /// </summary>
        static readonly ILog Logger = LoggerFactory.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Environment helper instance for exit handling.
        /// </summary>
        private static readonly EnvironmentHelper environmentHelper = new EnvironmentHelper();

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="NugetJfrogApiCommunication"/> class.
        /// </summary>
        /// <param name="repoDomainName">The repository domain name.</param>
        /// <param name="srcrepoName">The source repository name.</param>
        /// <param name="repoCredentials">The repository credentials.</param>
        /// <param name="timeout">The timeout in seconds.</param>
        public NugetJfrogApiCommunication(string repoDomainName, string srcrepoName, ArtifactoryCredentials repoCredentials, int timeout) 
            : base(repoDomainName, srcrepoName, repoCredentials, timeout)
        {
            // Base class handles all initialization including timeout
        }

        #endregion

        #region Methods

        /// <summary>
        /// Asynchronously retrieves the API key from Artifactory.
        /// </summary>
        /// <returns>An HTTP response message containing the API key.</returns>
        public override async Task<HttpResponseMessage> GetApiKey()
        {
            var httpClient = GetHttpClient(ArtifactoryCredentials);
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
            var httpClient = GetHttpClient(ArtifactoryCredentials);
            const HttpContent httpContent = null;
            await LogHandlingHelper.HttpRequestHandling("Package Copy from remote repository", $"MethodName:CopyFromRemoteRepo()", httpClient, component.CopyPackageApiUrl, httpContent);
            return await httpClient.PostAsync(component.CopyPackageApiUrl, httpContent);
        }

        /// <summary>
        /// Asynchronously moves a component from one repository to another.
        /// </summary>
        /// <param name="component">The component to move.</param>
        /// <returns>An HTTP response message indicating the result of the move operation.</returns>
        public override async Task<HttpResponseMessage> MoveFromRepo(ComponentsToArtifactory component)
        {
            var httpClient = GetHttpClient(ArtifactoryCredentials);
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
            HttpResponseMessage responseMessage = new HttpResponseMessage();
            var result = responseMessage;
            
            try
            {
                var httpClient = GetHttpClient(ArtifactoryCredentials);
                result = await httpClient.GetAsync(component.PackageInfoApiUrl);
            }
            catch (TaskCanceledException ex)
            {
                Logger.Debug("Request timeout occurred", ex);
                Logger.Error("A timeout error is thrown from Jfrog server,Please wait for sometime and re run the pipeline again");
                environmentHelper.CallEnvironmentExit(-1);

            }            
            return result;
        }

        /// <summary>
        /// Updates the package properties in JFrog Artifactory with the SW360 release URL.
        /// </summary>
        /// <param name="sw360releaseUrl">The SW360 release URL.</param>
        /// <param name="destRepoName">The destination repository name.</param>
        /// <param name="uploadArgs">The upload arguments containing package information.</param>
        public override void UpdatePackagePropertiesInJfrog(string sw360releaseUrl, string destRepoName, UploadArgs uploadArgs)
        {
            var httpClient = GetHttpClient(ArtifactoryCredentials);
            const HttpContent httpContent = null;
            string url = $"{DomainName}/api/storage/{destRepoName}/{uploadArgs.ReleaseName}.{uploadArgs.Version}.nupkg?" +
                 $"properties=sw360url={sw360releaseUrl}";
            httpClient.PutAsync(url, httpContent);
        }

        #endregion
    }
}