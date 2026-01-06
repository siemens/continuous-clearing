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
        static readonly ILog Logger = LoggerFactory.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public static IJFrogService JFrogService { get; set; }
        public static IJFrogApiCommunication JFrogApiCommInstance { get; set; }

        public static async Task<HttpResponseMessage> UploadPackageToRepo(ComponentsToArtifactory component, int timeout, DisplayPackagesInfo displayPackagesInfo)
        {
            Logger.Debug("Starting UploadPackageToArtifactory method");
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
                return HandleUploadException(ex, responsemessage);
            }
            catch (InvalidOperationException ex)
            {
                return HandleUploadException(ex, responsemessage);
            }
            finally
            {
                Logger.Debug("Ending UploadPackageToArtifactory method");
            }

            return responsemessage;
        }

        private static string GetOperationType(ComponentsToArtifactory component)
        {
            if (component.ComponentType == "CHOCO")
            {
                return component.PackageType == PackageType.Internal ? "move" : "copy";
            }
            return (component.PackageType == PackageType.ClearedThirdParty || component.PackageType == PackageType.Development) ? "copy" : "move";
        }

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

        private static HttpResponseMessage CreateNotFoundResponse(string reasonPhrase)
        {
            return new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                ReasonPhrase = reasonPhrase
            };
        }

        private static HttpResponseMessage HandleUploadException(Exception ex, HttpResponseMessage responsemessage)
        {
            Logger.Error("Error has occurred in UploadPackageToArtifactory--{Exception}", ex);
            responsemessage.ReasonPhrase = ApiConstant.ErrorInUpload;
            return responsemessage;
        }

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
