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
        //ConfigurationAttribute
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public static IJFrogService JFrogService { get; set; }
        public static IJFrogApiCommunication JFrogApiCommInstance { get; set; }

        public static async Task<HttpResponseMessage> UploadPackageToRepo(ComponentsToArtifactory component, int timeout, DisplayPackagesInfo displayPackagesInfo)
        {
            Logger.Debug("UploadPackageToRepo(): Starting the upload package to Artifactory.");
            string operationType = component.PackageType == PackageType.ClearedThirdParty
                || component.PackageType == PackageType.Development ? "copy" : "move";
            string dryRunSuffix = component.DryRun ? " dry-run" : "";
            HttpResponseMessage responsemessage = new HttpResponseMessage();
            try
            {

                // Package Information
                var packageInfo = await GetPackageInfoWithRetry(JFrogService, component);
                if (packageInfo == null)
                {
                    return new HttpResponseMessage(HttpStatusCode.NotFound)
                    {
                        ReasonPhrase = ApiConstant.PackageNotFound
                    };
                }

                // Perform Copy or Move operation
                responsemessage = component.PackageType switch
                {
                    PackageType.ClearedThirdParty or PackageType.Development => await JFrogApiCommInstance.CopyFromRemoteRepo(component),
                    PackageType.Internal => await JFrogApiCommInstance.MoveFromRepo(component),
                    _ => new HttpResponseMessage(HttpStatusCode.NotFound)
                };
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
                Logger.Error($"Error has occurred in UploadPackageToArtifactory--{ex}");
                responsemessage.ReasonPhrase = ApiConstant.ErrorInUpload;
                return responsemessage;
            }
            catch (InvalidOperationException ex)
            {
                LogHandlingHelper.ExceptionErrorHandling("UploadPackageToRepo", $"MethodName:UploadPackageToRepo(), ComponentName: {component.Name}", ex, "An invalid operation occurred while uploading the package to Artifactory.");
                Logger.Error($"Error has occurred in UploadPackageToArtifactory--{ex}");
                responsemessage.ReasonPhrase = ApiConstant.ErrorInUpload;
                return responsemessage;
            }
            finally
            {
                Logger.Debug($"UploadPackageToRepo(): Ending the upload process for component: {component.Name}.");
            }

            return responsemessage;
        }

        private static async Task<AqlResult> GetPackageInfoWithRetry(IJFrogService jFrogService, ComponentsToArtifactory component)
        {
            async Task<AqlResult> TryGetPackageInfo(ComponentsToArtifactory component)
                => await jFrogService.GetPackageInfo(component);

            var packageInfo = await TryGetPackageInfo(component);


            // Handle DEBIAN package name mismatch
            if (component.ComponentType == "DEBIAN" && packageInfo != null && packageInfo.Name != component.JfrogPackageName)
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

    }
}
