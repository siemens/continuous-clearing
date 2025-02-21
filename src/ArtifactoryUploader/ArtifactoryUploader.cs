// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.APICommunications;
using LCT.APICommunications.Interfaces;
using LCT.APICommunications.Model;
using LCT.APICommunications.Model.AQL;
using LCT.ArtifactoryUploader.Model;
using LCT.Services.Interface;
using log4net;
using System;
using System.Configuration;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace LCT.ArtifactoryUploader
{
    public static class ArtfactoryUploader
    {
        //ConfigurationAttribute
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public static IJFrogService jFrogService { get; set; }
        public static IJFrogApiCommunication JFrogApiCommInstance { get; set; }

        public static async Task<HttpResponseMessage> UploadPackageToRepo(ComponentsToArtifactory component, int timeout, DisplayPackagesInfo displayPackagesInfo)
        {
            Logger.Debug("Starting UploadPackageToArtifactory method");
            string operationType = component.PackageType == PackageType.ClearedThirdParty
                || component.PackageType == PackageType.Development ? "copy" : "move";
            string dryRunSuffix = component.DryRun ? " dry-run" : "";
            HttpResponseMessage responsemessage = new HttpResponseMessage();
            try
            {

                // Package Information
                var packageInfo = await GetPackageInfoWithRetry(jFrogService, component);
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
                Logger.Error($"Error has occurred in UploadPackageToArtifactory--{ex}");
                responsemessage.ReasonPhrase = ApiConstant.ErrorInUpload;
                return responsemessage;
            }
            catch (InvalidOperationException ex)
            {
                Logger.Error($"Error has occurred in UploadPackageToArtifactory--{ex}");
                responsemessage.ReasonPhrase = ApiConstant.ErrorInUpload;
                return responsemessage;
            }
            finally
            {
                Logger.Debug("Ending UploadPackageToArtifactory method");
            }

            return responsemessage;
        }

        private static async Task<AqlResult> GetPackageInfoWithRetry(IJFrogService jFrogService, ComponentsToArtifactory component)
        {
            async Task<AqlResult> TryGetPackageInfo(ComponentsToArtifactory component)
                => await jFrogService.GetPackageInfo(component);

            var packageInfo = await TryGetPackageInfo(component);


            // Handle DEBIAN package name mismatch
            if (component.ComponentType == "DEBIAN" && packageInfo?.Name != component.JfrogPackageName)
            {
                if (packageInfo != null) // Add this null check
                {
                    component.CopyPackageApiUrl = component.CopyPackageApiUrl.Replace(component.JfrogPackageName, packageInfo.Name);
                }
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
                    component.CopyPackageApiUrl = component.CopyPackageApiUrl.Replace(component.JfrogPackageName, packageInfo.Name);
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
                Logger.Error($"GettPathForArtifactoryUpload() ", ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                Logger.Error($"GettPathForArtifactoryUpload() ", ex);
            }

            return localPathforartifactory;
        }

    }
}
