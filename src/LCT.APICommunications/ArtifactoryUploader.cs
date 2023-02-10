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
    public class ArtfactoryUploader : IArtfactoryUploader
    {
        //ConfigurationAttribute
        private readonly ISw360ApiCommunication m_Sw360ApiCommunication;
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private string destRepoName = Environment.GetEnvironmentVariable("JfrogDestRepoName");
        private string JfrogApi = Environment.GetEnvironmentVariable("JfrogApi");
        private string srcRepoName = Environment.GetEnvironmentVariable("JfrogSrcRepo");
        ReleasesDetails releaseInfo = new ReleasesDetails();
        string releaseName = string.Empty;
        public ArtfactoryUploader(ISw360ApiCommunication sw360ApiCommunication)
        {
            m_Sw360ApiCommunication = sw360ApiCommunication;
        }
        public ArtfactoryUploader()
        {
        }
        private async Task<HttpResponseMessage> GetReleaseInfoById(string sw360ReleaseId)
        {
            HttpResponseMessage responsemessage = new HttpResponseMessage(HttpStatusCode.OK);

            HttpResponseMessage responseBody = await m_Sw360ApiCommunication.GetReleaseById(sw360ReleaseId);
            if (!responseBody.IsSuccessStatusCode)
            {
                return responseBody;
            }

            string response = await responseBody?.Content?.ReadAsStringAsync();
            releaseInfo = JsonConvert.DeserializeObject<ReleasesDetails>(response);
            if (releaseInfo.Embedded != null)
            {

                releaseName = releaseInfo.Name;
                if (releaseInfo.Name.Contains('/'))
                {
                    releaseInfo.Name = releaseInfo.Name[(releaseInfo.Name.IndexOf("/") + 1)..];
                }
            }
            else
            {
                releaseInfo = null;
            }

            return responsemessage;
        }
        public async Task<HttpResponseMessage> UploadNPMPackageToArtifactory(string sw360ReleaseId, string sw360releaseUrl, ArtifactoryCredentials credentials)
        {
            Logger.Debug("Starting UploadNPMPackageToArtifactory method");
            HttpResponseMessage responsemessage = new HttpResponseMessage(HttpStatusCode.OK);
            try
            {
                IJFrogApiCommunication jfrogApicommunication = new NpmJfrogApiCommunication(JfrogApi, srcRepoName, credentials);
                HttpResponseMessage responseBody = await m_Sw360ApiCommunication.GetReleaseById(sw360ReleaseId);
                if (!responseBody.IsSuccessStatusCode)
                {
                    return responseBody;
                }
                responsemessage = await GetReleaseInfoById(sw360ReleaseId);

                if (!responsemessage.IsSuccessStatusCode)
                    return responsemessage;

                if (releaseInfo != null)
                {
                    UploadArgs uploadArgs = new UploadArgs()
                    {
                        PackageName = releaseName,
                        ReleaseName = releaseInfo.Name,
                        Version = releaseInfo.Version
                    };

                    return await UploadToArtifactory(jfrogApicommunication, uploadArgs, sw360releaseUrl);
                }
            }
            catch (HttpRequestException ex)
            {
                Logger.Error($"Error has occured in UploadNPMPackageToArtifactory--{ex}");
                responsemessage.StatusCode = HttpStatusCode.BadRequest;
                return responsemessage;
            }
            return responsemessage;
        }

        public async Task<HttpResponseMessage> UploadNUGETPackageToArtifactory(string sw360ReleaseId, string sw360releaseUrl, ArtifactoryCredentials credentials)
        {
            Logger.Debug("Starting UploadNPMPackageToArtifactory method");

            HttpResponseMessage responsemessage = new HttpResponseMessage(HttpStatusCode.OK);
            destRepoName = ConfigurationManager.AppSettings["JfrogNugetDestRepoName"];
            srcRepoName = ConfigurationManager.AppSettings["JfrogNugetSrcRepo"];
            JfrogApi = ConfigurationManager.AppSettings["JfrogApi"];

            IJFrogApiCommunication jfrogApicommunication = new NugetJfrogApiCommunication(JfrogApi, srcRepoName, credentials);
            try
            {

                responsemessage = await GetReleaseInfoById(sw360ReleaseId);
                if (!responsemessage.IsSuccessStatusCode)
                {
                    return responsemessage;
                }
                if (releaseInfo != null)
                {
                    UploadArgs uploadArgs = new UploadArgs()
                    {
                        PackageName = releaseName,
                        ReleaseName = releaseInfo.Name,
                        Version = releaseInfo.Version
                    };
                    return await UploadToArtifactory(jfrogApicommunication, uploadArgs, sw360releaseUrl);
                }

            }
            catch (HttpRequestException ex)
            {
                Logger.Error($"Error has occured in UploadNPMPackageToArtifactory--{ex}");
                return responsemessage;
            }
            return responsemessage;
        }

        private async Task<HttpResponseMessage> UploadToArtifactory(IJFrogApiCommunication jfrogApicommunication, UploadArgs uploadArgs, string sw360releaseUrl)
        {
            HttpResponseMessage responsemessage = new HttpResponseMessage(HttpStatusCode.OK);
            HttpResponseMessage responseBodyJfrog = await jfrogApicommunication.GetPackageByPackageName(uploadArgs);
            if (responseBodyJfrog.StatusCode != HttpStatusCode.OK)
            {
                Logger.Error("Package not found in artifactory");
                responsemessage.StatusCode = responseBodyJfrog.StatusCode;
                responsemessage.ReasonPhrase = ApiConstant.PackageNotFound;
                responsemessage.Content = new StringContent($"{"Package not found in artifactory"}");
                return responsemessage;
            }


            responsemessage = await jfrogApicommunication.CopyPackageFromRemoteRepo(uploadArgs, destRepoName);
            jfrogApicommunication.UpdatePackagePropertiesInJfrog(sw360releaseUrl, destRepoName, uploadArgs);

            if (responsemessage.IsSuccessStatusCode)
            {
                Logger.Info("Successfully copied package from nuget-remote-cache to nuget-test");
            }

            else
            {
                Logger.Error("Invalid artifactory file sha code not matching with nuget-remote-cache");
                responsemessage.StatusCode = HttpStatusCode.BadRequest;
                responsemessage.ReasonPhrase = ApiConstant.InvalidArtifactory;
                responsemessage.Content = new StringContent($"{"Invalid artifactory file sha code not matching with nuget-remote-cache"}");
                return responsemessage;
            }
            return responsemessage;
        }
        public async Task<HttpResponseMessage> UploadPackageToRepo(ComponentsToArtifactory component)
        {
            Logger.Debug("Starting UploadPackageToArtifactory method");

            HttpResponseMessage responsemessage = new HttpResponseMessage();
            try
            {
                ArtifactoryCredentials repoCredentials = new ArtifactoryCredentials()
                {
                    ApiKey = component.ApiKey,
                    Email = component.Email
                };
                IJFrogApiCommunication jfrogApicommunication = new NpmJfrogApiCommunication(component.JfrogApi, component.SrcRepoName, repoCredentials);
                HttpResponseMessage responseBodyJfrog = await jfrogApicommunication.GetPackageInfo(component);

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
