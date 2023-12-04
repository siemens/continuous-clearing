// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.APICommunications;
using LCT.APICommunications.Interfaces;
using LCT.APICommunications.Model;
using LCT.APICommunications.Model.AQL;
using LCT.Services;
using LCT.Services.Interface;
using log4net;
using System;
using System.Configuration;
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
        private static string destRepoName = Environment.GetEnvironmentVariable("JfrogDestRepoName");
        private static string JfrogApi = Environment.GetEnvironmentVariable("JfrogApi");
        private static string srcRepoName = Environment.GetEnvironmentVariable("JfrogSrcRepo");
        public static IJFrogService jFrogService { get; set; }

        public static async Task<HttpResponseMessage> UploadPackageToRepo(ComponentsToArtifactory component, int timeout)
        {
            Logger.Debug("Starting UploadPackageToArtifactory method");
            string operationType = component.PackageType == PackageType.ClearedThirdParty ? "copy" : "move";
            string dryRunSuffix = component.DryRun ? " dry-run" : "";
            HttpResponseMessage responsemessage = new HttpResponseMessage();
            try
            {
                IJFrogApiCommunication jfrogApicommunication;

                // Package Information
                var packageInfo = await GetPackageInfoWithRetry(jFrogService, component);
                if (packageInfo == null)
                {
                    return new HttpResponseMessage(HttpStatusCode.NotFound)
                    {
                        ReasonPhrase = ApiConstant.PackageNotFound
                    };
                }

                ArtifactoryCredentials repoCredentials = new ArtifactoryCredentials()
                {
                    ApiKey = component.ApiKey,
                    Email = component.Email
                };

                // Initialize JFrog API communication based on Component Type
                jfrogApicommunication = component.ComponentType?.ToUpperInvariant() switch
                {
                    "MAVEN" => new MavenJfrogApiCommunication(component.JfrogApi, component.SrcRepoName, repoCredentials, timeout),
                    "PYTHON" => new PythonJfrogApiCommunication(component.JfrogApi, component.SrcRepoName, repoCredentials, timeout),
                    _ => new NpmJfrogApiCommunication(component.JfrogApi, component.SrcRepoName, repoCredentials, timeout)
                };

                // Perform Copy or Move operation
                responsemessage = component.PackageType switch
                {
                    PackageType.ClearedThirdParty => await jfrogApicommunication.CopyFromRemoteRepo(component),
                    PackageType.Internal or PackageType.Development => await jfrogApicommunication.MoveFromRepo(component),
                    _ => new HttpResponseMessage(HttpStatusCode.NotFound)
                };

                // Check status code and handle errors
                if (responsemessage.StatusCode != HttpStatusCode.OK)
                {
                    responsemessage.ReasonPhrase = ApiConstant.ErrorInUpload;
                    return responsemessage;
                }

                Logger.Info($"Successful{dryRunSuffix} {operationType} package {component.PackageName}-{component.Version}" +
                                    $" from {component.SrcRepoName} to {component.DestRepoName}");

            }
            catch (HttpRequestException ex)
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

        /// </summary>
        public static void SetConfigurationValues()
        {
            if (string.IsNullOrEmpty(destRepoName))
            {
                destRepoName = ConfigurationManager.AppSettings["JfrogDestRepoName"];
            }
            if (string.IsNullOrEmpty(JfrogApi))
            {
                JfrogApi = ConfigurationManager.AppSettings["JfrogApi"];
            }
            if (string.IsNullOrEmpty(srcRepoName))
            {
                srcRepoName = ConfigurationManager.AppSettings["JfrogSrcRepo"];
            }
        }

        private static async Task<AqlResult> GetPackageInfoWithRetry(IJFrogService jFrogService, ComponentsToArtifactory component)
        {
            string srcRepoNameLower = component.SrcRepoName.ToLower();
            string packageNameLower = component.JfrogPackageName.ToLower();
            string pathLower = component.Path.ToLower();

            var packageInfo = await jFrogService.GetPackageInfo(component.SrcRepoName, component.JfrogPackageName, component.Path);

            if (packageInfo == null)
            {
                // Retry with lowercase parameters
                var lowercasePackageInfo = await jFrogService.GetPackageInfo(srcRepoNameLower, packageNameLower, pathLower);

                if (lowercasePackageInfo != null)
                {
                    // Update the package API URL
                    component.CopyPackageApiUrl = component.CopyPackageApiUrl.ToLower();
                    packageInfo = lowercasePackageInfo;
                }
            }

            return packageInfo;
        }

    }
}
