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
            IJFrogApiCommunication jfrogApicommunication;
            HttpResponseMessage responsemessage = new HttpResponseMessage();
            try
            {
                //Package Information
                var packageInfo = await GetPackageInfoWithRetry(jFrogService, component);
                if (packageInfo == null)
                {
                    responsemessage.StatusCode = HttpStatusCode.NotFound;
                    responsemessage.ReasonPhrase = ApiConstant.PackageNotFound;
                    return responsemessage;
                }

                ArtifactoryCredentials repoCredentials = new ArtifactoryCredentials()
                { 
                    ApiKey = component.ApiKey,
                    Email = component.Email
                };
                if (component.ComponentType?.ToUpperInvariant() == "MAVEN")
                {
                    jfrogApicommunication = new MavenJfrogApiCommunication(component.JfrogApi, component.SrcRepoName, repoCredentials, timeout);
                }
                else if (component.ComponentType?.ToUpperInvariant() == "PYTHON")
                {
                    jfrogApicommunication = new PythonJfrogApiCommunication(component.JfrogApi, component.SrcRepoName, repoCredentials, timeout);
                }
                else
                {
                    jfrogApicommunication = new NpmJfrogApiCommunication(component.JfrogApi, component.SrcRepoName, repoCredentials, timeout);
                }

                responsemessage = await jfrogApicommunication.CopyFromRemoteRepo(component);
                if (responsemessage.StatusCode != HttpStatusCode.OK)
                {
                    responsemessage.ReasonPhrase = ApiConstant.ErrorInUpload;
                    return responsemessage;
                }

                if (component.DryRun)
                {
                    Logger.Info($"Successful dry-run for package {component.PackageName}-{component.Version} from {component.SrcRepoName} to {component.DestRepoName}");
                }
                else
                {
                    Logger.Info($"Successfully copied package {component.PackageName}-{component.Version} from {component.SrcRepoName} to {component.DestRepoName}");
                }
            }
            catch (HttpRequestException ex)
            {
                Logger.Error($"Error has occured in UploadPackageToArtifactory--{ex}");
                return responsemessage;
            }
            Logger.Debug("Ending UploadPackageToArtifactory method");
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
