// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.APICommunications;
using LCT.APICommunications.Interfaces;
using LCT.APICommunications.Model;
using LCT.APICommunications.Model.AQL;
using LCT.ArtifactoryUploader.Model;
using LCT.Common;
using LCT.Services.Interface;
using log4net;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Directory = System.IO.Directory;

namespace LCT.ArtifactoryUploader
{
    public static class ArtfactoryUploader
    {
        #region Fields

        static readonly ILog Logger = LoggerFactory.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #endregion

        #region Properties

        public static IJFrogService JFrogService { get; set; }
        public static IJFrogApiCommunication JFrogApiCommInstance { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// Asynchronously uploads a package to the repository.
        /// </summary>
        /// <param name="component">The component to upload.</param>
        /// <param name="timeout">The timeout value in seconds.</param>
        /// <param name="displayPackagesInfo">The display information for packages.</param>
        /// <returns>A task containing the HTTP response message.</returns>

        public static async Task<HttpResponseMessage> UploadPackageToRepo(ComponentsToArtifactory component, int timeout, DisplayPackagesInfo displayPackagesInfo)
        {

            Logger.Debug("UploadPackageToRepo(): Starting the upload package to Artifactory.");
            string operationType = GetOperationType(component);
            string dryRunSuffix = component.DryRun ? " dry-run" : "";
            HttpResponseMessage responsemessage = new HttpResponseMessage();
            try
            {
                // Package Information
                var packageInfo = await GetPackageInfoWithRetry(JFrogService, component);
                if (packageInfo == null)
                {
                    return CreateNotFoundResponse(ApiConstant.PackageNotFound);
                }

                // Perform Copy or Move operation

                responsemessage = await GetRepoOperationResponse(component);
                await LogHandlingHelper.HttpResponseHandling("Upload Package To Repo", $"MethodName:UploadPackageToRepo()", responsemessage, "");

                // Check status code and handle errors
                if (responsemessage.StatusCode != HttpStatusCode.OK)
                {
                    responsemessage.ReasonPhrase = ApiConstant.ErrorInUpload;
                    return responsemessage;
                }

                await PackageUploadHelper.JfrogFoundPackagesAsync(component, displayPackagesInfo, operationType, responsemessage, dryRunSuffix);
            }
            catch (HttpRequestException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling("UploadPackageToRepo", $"MethodName:UploadPackageToRepo(), ComponentName: {component.Name}", ex, "An HTTP request error occurred while uploading the package to Artifactory.");
                return HandleUploadException(ex, responsemessage);
            }
            catch (InvalidOperationException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling("UploadPackageToRepo", $"MethodName:UploadPackageToRepo(), ComponentName: {component.Name}", ex, "An invalid operation occurred while uploading the package to Artifactory.");
                return HandleUploadException(ex, responsemessage);
            }
            finally
            {
                Logger.DebugFormat("UploadPackageToRepo(): Ending the upload process for component: {0}.", component.Name);
            }

            return responsemessage;
        }

        /// <summary>
        /// Gets the operation type for the component based on package type.
        /// </summary>
        /// <param name="component">The component to determine operation type for.</param>
        /// <returns>The operation type as a string ("copy" or "move").</returns>
        private static string GetOperationType(ComponentsToArtifactory component)
        {
            if (component.ComponentType == "CHOCO")
            {
                return component.PackageType == PackageType.Internal ? "move" : "copy";
            }
            return (component.PackageType == PackageType.ClearedThirdParty || component.PackageType == PackageType.Development) ? "copy" : "move";
        }

        /// <summary>
        /// Asynchronously gets the repository operation response based on package type.
        /// </summary>
        /// <param name="component">The component to perform the operation on.</param>
        /// <returns>A task containing the HTTP response message.</returns>
        private static async Task<HttpResponseMessage> GetRepoOperationResponse(ComponentsToArtifactory component)
        {
            return component.PackageType switch
            {
                PackageType.ClearedThirdParty or PackageType.Development =>
                    await JFrogApiCommInstance.CopyFromRemoteRepo(component),
                PackageType.Internal =>
                    await JFrogApiCommInstance.MoveFromRepo(component),
                _ => new HttpResponseMessage(HttpStatusCode.NotFound)
            };
        }

        /// <summary>
        /// Creates a not found HTTP response message.
        /// </summary>
        /// <param name="reasonPhrase">The reason phrase for the response.</param>
        /// <returns>An HTTP response message with not found status.</returns>
        private static HttpResponseMessage CreateNotFoundResponse(string reasonPhrase)
        {
            return new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                ReasonPhrase = reasonPhrase
            };
        }

        /// <summary>
        /// Handles upload exceptions and returns an error response.
        /// </summary>
        /// <param name="ex">The exception that occurred.</param>
        /// <param name="responsemessage">The response message to update.</param>
        /// <returns>An HTTP response message with error information.</returns>
        private static HttpResponseMessage HandleUploadException(Exception ex, HttpResponseMessage responsemessage)
        {
            Logger.Error("Error has occurred in UploadPackageToArtifactory--{Exception}", ex);
            responsemessage.ReasonPhrase = ApiConstant.ErrorInUpload;
            return responsemessage;
        }

        /// <summary>
        /// Asynchronously gets package information with retry logic for lowercase names.
        /// </summary>
        /// <param name="jFrogService">The JFrog service instance.</param>
        /// <param name="component">The component to get information for.</param>
        /// <returns>A task containing the AQL result with package information, or null if not found.</returns>
        private static async Task<AqlResult> GetPackageInfoWithRetry(IJFrogService jFrogService, ComponentsToArtifactory component)
        {
            async Task<AqlResult> TryGetPackageInfo(ComponentsToArtifactory component)
                => await jFrogService.GetPackageInfo(component);

            var packageInfo = await TryGetPackageInfo(component);


            // Handle DEBIAN or NUGET package name mismatch
            if ((component.ComponentType == "DEBIAN" || component.ComponentType == "NUGET") && packageInfo != null && packageInfo.Name != component.JfrogPackageName)
            {
                component.CopyPackageApiUrl = component.CopyPackageApiUrl.Replace(component.JfrogPackageName, packageInfo.Name);
            }

            // Retry with lowercase values if packageInfo is still null
            if (packageInfo == null)
            {
                _ = component.SrcRepoName.ToLower();
                _ = component.JfrogPackageName.ToLower();
                _ = component.Path.ToLower();


                packageInfo = await TryGetPackageInfo(component);

                if (packageInfo != null)
                {
                    component.CopyPackageApiUrl = component.CopyPackageApiUrl.ToLower();
                }
            }

            return packageInfo;
        }

        /// <summary>
        /// Gets the path for Artifactory upload directory.
        /// </summary>
        /// <returns>The local path for Artifactory upload.</returns>
        public static string GettPathForArtifactoryUpload()
        {
            string localPathforartifactory = string.Empty;
            try
            {
                String Todaysdate = DateTime.Now.ToString("dd-MM-yyyy_ss");
                localPathforartifactory = $"{Directory.GetParent(Directory.GetCurrentDirectory())}\\ClearingTool\\ArtifactoryFiles\\{Todaysdate}\\";
                if (!Directory.Exists(localPathforartifactory))
                {
                    localPathforartifactory = Directory.CreateDirectory(localPathforartifactory).ToString();
                }
            }
            catch (IOException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling("GettPathForArtifactoryUpload", $"Failed to create directory ", ex, "IOException occurred while creating the directory.");
            }
            catch (UnauthorizedAccessException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling("GettPathForArtifactoryUpload", $"Unauthorized access while creating directory", ex, "UnauthorizedAccessException occurred while creating the directory.");
            }

            return localPathforartifactory;
        }

        #endregion
    }
}
