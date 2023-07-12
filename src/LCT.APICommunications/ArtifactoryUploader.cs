// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.APICommunications.Interfaces;
using LCT.APICommunications.Model;
using log4net;
using System;
using System.Configuration;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace LCT.APICommunications
{
    public static class ArtfactoryUploader 
    {
        //ConfigurationAttribute
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static string destRepoName = Environment.GetEnvironmentVariable("JfrogDestRepoName");
        private static string JfrogApi = Environment.GetEnvironmentVariable("JfrogApi");
        private static string srcRepoName = Environment.GetEnvironmentVariable("JfrogSrcRepo");


        public static async Task<HttpResponseMessage> UploadPackageToRepo(ComponentsToArtifactory component,int timeout)
        {
            Logger.Debug("Starting UploadPackageToArtifactory method");
            IJFrogApiCommunication jfrogApicommunication;
            HttpResponseMessage responsemessage = new HttpResponseMessage();
            HttpResponseMessage responseBodyJfrog = new HttpResponseMessage();
            try
            {
                ArtifactoryCredentials repoCredentials = new ArtifactoryCredentials()
                {
                    ApiKey = component.ApiKey,
                    Email = component.Email
                };
                if (component?.ComponentType?.ToUpperInvariant() == "MAVEN")
                {
                    jfrogApicommunication = new MavenJfrogApiCommunication(component.JfrogApi, component.SrcRepoName, repoCredentials,timeout);
                    responseBodyJfrog = await jfrogApicommunication.GetPackageInfo(component);
                }
                else
                {
                    jfrogApicommunication = new NpmJfrogApiCommunication(component.JfrogApi, component.SrcRepoName, repoCredentials,timeout);
                    responseBodyJfrog = await jfrogApicommunication.GetPackageInfo(component);
                }
                if (responseBodyJfrog.StatusCode == HttpStatusCode.NotFound)
                {
                    component.PackageInfoApiUrl = component.PackageInfoApiUrl.ToLower();
                    responseBodyJfrog = await jfrogApicommunication.GetPackageInfo(component);
                    component.CopyPackageApiUrl = component.CopyPackageApiUrl.ToLower();
                }
          
             
                if (responseBodyJfrog.StatusCode != HttpStatusCode.OK)
                {
                    responsemessage.StatusCode = responseBodyJfrog.StatusCode;
                    responsemessage.ReasonPhrase = ApiConstant.PackageNotFound;
                    return responsemessage;
                }

                responsemessage = await jfrogApicommunication.CopyFromRemoteRepo(component);
                if (responsemessage.StatusCode != HttpStatusCode.OK)
                {
                    responsemessage.StatusCode = responseBodyJfrog.StatusCode;
                    responsemessage.ReasonPhrase = ApiConstant.ErrorInUpload;
                    return responsemessage;
                }
                Logger.Info($"Successfully copied package {component.PackageName}-{component.Version} from {component.SrcRepoName} to {component.DestRepoName}");
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

    }
}
