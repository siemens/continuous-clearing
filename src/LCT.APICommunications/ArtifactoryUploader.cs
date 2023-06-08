// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.APICommunications.Interfaces;
using LCT.APICommunications.Model;
using log4net;
using Newtonsoft.Json;
using System;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace LCT.APICommunications
{
    public class ArtfactoryUploader 
    {
        //ConfigurationAttribute
        private readonly ISw360ApiCommunication m_Sw360ApiCommunication;
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private string destRepoName = Environment.GetEnvironmentVariable("JfrogDestRepoName");
        private string JfrogApi = Environment.GetEnvironmentVariable("JfrogApi");
        private string srcRepoName = Environment.GetEnvironmentVariable("JfrogSrcRepo");

        public ArtfactoryUploader(ISw360ApiCommunication sw360ApiCommunication)
        {
            m_Sw360ApiCommunication = sw360ApiCommunication;
        }
        public ArtfactoryUploader()
        {
        }

        public async Task<HttpResponseMessage> UploadPackageToRepo(ComponentsToArtifactory component,int timeout)
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
        public void SetConfigurationValues()
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
