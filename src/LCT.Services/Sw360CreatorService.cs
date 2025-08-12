// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.APICommunications;
using LCT.APICommunications.Model;
using LCT.APICommunications.Model.Foss;
using LCT.Common;
using LCT.Common.Constants;
using LCT.Common.Interface;
using LCT.Common.Model;
using LCT.Facade.Interfaces;
using LCT.Services.Interface;
using LCT.Services.Model;
using log4net;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Level = log4net.Core.Level;

namespace LCT.Services
{
    /// <summary>
    /// sw360 component creator service
    /// </summary>
    public class Sw360CreatorService : ISw360CreatorService
    {
        static readonly ILog Logger = LoggerFactory.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        readonly ISW360ApicommunicationFacade m_SW360ApiCommunicationFacade;
        readonly ISW360CommonService m_SW360CommonService;
        private static EnvironmentHelper environmentHelper = new EnvironmentHelper();

        public Sw360CreatorService(ISW360ApicommunicationFacade sw360ApiCommunicationFacade)
        {
            m_SW360ApiCommunicationFacade = sw360ApiCommunicationFacade;
        }

        public Sw360CreatorService(ISW360ApicommunicationFacade sw360ApiCommunicationFacade, ISW360CommonService sW360CommonService)
        {
            m_SW360ApiCommunicationFacade = sw360ApiCommunicationFacade;
            m_SW360CommonService = sW360CommonService;
        }

        public async Task<ComponentCreateStatus> CreateComponentBasesOFswComaprisonBOM(
            ComparisonBomData componentInfo, Dictionary<string, string> attachmentUrlList)
        {
            Logger.Debug($"CreateComponent(): Name-{componentInfo.Name},version-{componentInfo.Version}");
            ComponentCreateStatus componentCreateStatus = new ComponentCreateStatus
            {
                IsCreated = true,
                ReleaseStatus = new ReleaseCreateStatus() { IsCreated = true }
            };

            try
            {
                CreateComponent crt = new CreateComponent
                {
                    ComponentType = ApiConstant.Oss,
                    Name = componentInfo.Name,
                    Categories = new string[] { "-" },
                    ExternalIds = new ExternalIds()

                };
                crt.ExternalIds.Package_Url = componentInfo.ComponentExternalId;
                crt.ExternalIds.Purl_Id = string.Empty;
                // create component in sw360
                HttpResponseMessage response = await m_SW360ApiCommunicationFacade.CreateComponent(crt);

                //Component creation Success 
                if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.Conflict)
                {
                    string componentId = await GetComponentId(componentInfo.Name);
                    Logger.Debug($"Name - {componentInfo.Name}, component Id - {componentId}");
                    componentCreateStatus.ReleaseStatus = await CreateReleaseForComponent(componentInfo, componentId, attachmentUrlList);
                }
                else
                {
                    componentCreateStatus.IsCreated = false;
                    componentCreateStatus.ReleaseStatus.IsCreated = false;
                    Environment.ExitCode = -1;
                    Logger.Debug($"CreateComponent():Component Name -{componentInfo.Name}- " +
                   $"response status code-{response.StatusCode} and reason pharase-{response.ReasonPhrase}");
                    Logger.Error($"CreateComponent():Component Name -{componentInfo.Name}- " +
                        $"response status code-{response.StatusCode} and reason pharase-{response.ReasonPhrase}");
                }
            }
            catch (HttpRequestException e)
            {
                Logger.Error($"CreateComponent():", e);
                Environment.ExitCode = -1;
                componentCreateStatus.IsCreated = false;
                componentCreateStatus.ReleaseStatus.IsCreated = false;
            }

            return componentCreateStatus;
        }


        public async Task<FossTriggerStatus> TriggerFossologyProcess(string releaseId, string sw360link)
        {
            FossTriggerStatus fossTriggerStatus = null;
            try
            {
                string triggerStatus = await m_SW360ApiCommunicationFacade.TriggerFossologyProcess(releaseId, sw360link);
                fossTriggerStatus = JsonConvert.DeserializeObject<FossTriggerStatus>(triggerStatus);
            }
            catch (HttpRequestException ex)
            {
                ExceptionHandling.FossologyException(ex);

            }

            return fossTriggerStatus;

        }

        public async Task<CheckFossologyProcess> CheckFossologyProcessStatus(string link)
        {
            CheckFossologyProcess fossTriggerStatus = null;
            try
            {

                var triggerStatus = await m_SW360ApiCommunicationFacade.CheckFossologyProcessStatus(link);
                string fossStatus = await triggerStatus.Content.ReadAsStringAsync();
                fossTriggerStatus = JsonConvert.DeserializeObject<CheckFossologyProcess>(fossStatus);
            }
            catch (JsonReaderException ex)
            {
                Logger.Error($"TriggerFossologyProcess {ex.Message}");
            }
            catch (HttpRequestException ex)
            {
                Logger.Error($"TriggerFossologyProcess {ex.Message}");
            }
            return fossTriggerStatus;

        }
        public async Task<ReleaseCreateStatus> CreateReleaseForComponent(ComparisonBomData componentInfo, string componentId,
                                                             Dictionary<string, string> attachmentUrlList)
        {
            string releaseId = "";
            ReleaseCreateStatus createStatus = new ReleaseCreateStatus { IsCreated = true };

            try
            {
                Releases release = new Releases
                {
                    ComponentId = componentId,
                    Name = componentInfo.Name,
                    Version = componentInfo.Version,
                    SourceDownloadurl = GetSourceDownloadUrl(componentInfo, attachmentUrlList),
                    BinaryDownloadUrl = GetPackageDownloadUrl(componentInfo, attachmentUrlList),
                    ClearingState = Dataconstant.NewClearing,
                    ExternalIds = new ExternalIds()
                };
                release.ExternalIds.Package_Url = componentInfo.ReleaseExternalId;
                release.ExternalIds.Purl_Id = string.Empty;
                var response = await m_SW360ApiCommunicationFacade.CreateRelease(release);
                //come here - if success to attch src code
                if (response.IsSuccessStatusCode)
                {
                    releaseId = AttachSourceAndBinary(attachmentUrlList, createStatus, response, componentInfo);
                }
                else if (response.StatusCode == HttpStatusCode.Conflict)
                {
                    releaseId = await GetReleaseIdToLinkToProject(componentInfo.Name, componentInfo.Version, componentInfo.ReleaseExternalId, componentId);
                    Logger.Debug($"CreateReleaseForComponent():Release already exists for component -->" +
                        $"{componentInfo.Name} - {componentInfo.Version}. No changes made by tool");
                    createStatus.ReleaseAlreadyExist = true;
                }
                else
                {
                    createStatus.IsCreated = false;

                    Environment.ExitCode = -1;
                    Logger.Debug($"CreateReleaseForComponent():Component Name -{componentInfo.Name}{componentInfo.Version}- " +
                   $"response status code-{response.StatusCode} and reason pharase-{response.ReasonPhrase}");
                    Logger.Error($"CreateReleaseForComponent():Component Name -{componentInfo.Name}{componentInfo.Version}- " +
                        $"response status code-{response.StatusCode} and reason pharase-{response.ReasonPhrase}");
                }

                Logger.Debug($"Component Name -{componentInfo.Name},Version :{componentInfo.Version} , Release Id :{releaseId}");
                createStatus.ReleaseIdToLink = releaseId;
            }
            catch (HttpRequestException e)
            {
                Logger.Error($"CreateReleaseForComponent():", e);
                Environment.ExitCode = -1;
                createStatus.IsCreated = false;
            }
            return createStatus;
        }
        private async Task<string> GetReleaseIdToLinkToProject(string name, string version, string releaseExternalId, string componentId)
        {
            string releaseId = await GetReleaseIdByName(name, version);
            if (string.IsNullOrEmpty(releaseId))
            {
                releaseId = await GetReleaseByExternalId(name, version, releaseExternalId);

                if (string.IsNullOrEmpty(releaseId))
                {
                    releaseId = await m_SW360CommonService.GetReleaseIdByComponentId(componentId, version);
                }
            }

            if (!string.IsNullOrEmpty(releaseId))
            {
                Logger.Debug($"Updating the Release with ID : {releaseId}");
                //update the package URL in the already existing release of the componentid used here
                ReleasesInfo releasesInfo = await GetReleaseInfo(releaseId);
                ComparisonBomData bomData = new ComparisonBomData { ReleaseExternalId = releaseExternalId };
                _ = UpdatePurlIdForExistingRelease(bomData, releaseId, releasesInfo);
            }
            if (string.IsNullOrEmpty(releaseId))
            {
                Logger.Warn($"Release id not found for the Component - {name}-{version}");
            }
            return releaseId ?? string.Empty;
        }

        private string AttachSourceAndBinary(Dictionary<string, string> attachmentUrlList, ReleaseCreateStatus createStatus, HttpResponseMessage response, ComparisonBomData comparisonBomData)
        {
            string releaseId = string.Empty;

            if (response.Content != null)
            {
                string responseString = response.Content?.ReadAsStringAsync()?.Result ?? string.Empty;
                var responseData = JsonConvert.DeserializeObject<Releases>(responseString);
                string href = responseData?.Links?.Self?.Href ?? string.Empty;
                releaseId = CommonHelper.GetSubstringOfLastOccurance(href, "/");
                createStatus.AttachmentApiUrl = AttachSourcesToReleasesCreated(releaseId, attachmentUrlList,comparisonBomData);
            }

            return releaseId;
        }

        private static string GetSourceDownloadUrl(ComparisonBomData componentInfo, Dictionary<string, string> attachmentUrlList)
        {
            if (componentInfo.DownloadUrl == Dataconstant.DownloadUrlNotFound || !(attachmentUrlList.ContainsKey("SOURCE")))
            {
                return string.Empty;
            }
            return componentInfo.DownloadUrl ?? string.Empty;
        }

        private static string GetPackageDownloadUrl(ComparisonBomData componentInfo, Dictionary<string, string> attachmentUrlList)
        {
            if (!(attachmentUrlList.ContainsKey("BINARY")))
            {
                return string.Empty;
            }
            return componentInfo.PackageUrl ?? string.Empty;
        }

        public async Task<bool> LinkReleasesToProject(List<ReleaseLinked> releasesTobeLinked, List<ReleaseLinked> manuallyLinkedReleases, string sw360ProjectId)
        {
            if (manuallyLinkedReleases.Count <= 0 && releasesTobeLinked.Count <= 0)
            {
                Logger.Debug($"No of release Id's to link - 0");
                return true;
            }
            try
            {
                var finalReleasesToBeLinked = manuallyLinkedReleases.Concat(releasesTobeLinked).Distinct().ToList();
                Logger.Debug($"No of release Id's to link - {finalReleasesToBeLinked.Count}");

                Dictionary<string, ReleaseLinked> linkedReleasesUniqueDict = new Dictionary<string, ReleaseLinked>();
                foreach (var release in finalReleasesToBeLinked)
                {
                    if (!linkedReleasesUniqueDict.TryGetValue(release.ReleaseId, out ReleaseLinked value))
                    {
                        linkedReleasesUniqueDict.Add(release.ReleaseId, release);
                    }
                    else
                    {
                        Logger.Debug("Duplicate entries found in finalReleasesToBeLinked: " + release.Name + ":" + release.ReleaseId +
                            " , with :" + value.Name + ":" + value.ReleaseId);
                    }
                }

                // Assigning unique entries from the Dict
                finalReleasesToBeLinked = linkedReleasesUniqueDict.Values.ToList();

                Dictionary<string, AddLinkedRelease> linkedReleasesDict;
                linkedReleasesDict = finalReleasesToBeLinked
                                        .ToDictionary(
                                            releaseLinked => releaseLinked.ReleaseId,
                                            releaseLinked => new AddLinkedRelease()
                                            {
                                                ReleaseRelation = string.IsNullOrEmpty(releaseLinked.Relation) ? Dataconstant.LinkedByCAToolReleaseRelationContained
                                                                    : releaseLinked.Relation,
                                                Comment = manuallyLinkedReleases.Exists(r => r.ReleaseId == releaseLinked.ReleaseId) ? releaseLinked.Comment : Dataconstant.LinkedByCATool
                                            });

                StringContent content = new StringContent(JsonConvert.SerializeObject(linkedReleasesDict), Encoding.UTF8, "application/json");

                var response = await m_SW360ApiCommunicationFacade.LinkReleasesToProject(content, sw360ProjectId);
                if (!response.IsSuccessStatusCode)
                {

                    Environment.ExitCode = -1;
                    Logger.Error($"LinkReleasesToProject() : Linking releases to project Id {sw360ProjectId} is failed.");
                    return false;
                }
                return true;
            }
            catch (HttpRequestException ex)
            {
                Logger.Error($"LinkReleasesToProject():", ex);
                Environment.ExitCode = -1;
                return false;
            }
            catch (AggregateException ex)
            {
                Logger.Error($"LinkReleasesToProject():", ex);
                Environment.ExitCode = -1;
                return false;
            }
        }

        public async Task<string> GetReleaseIDofComponent(string componentName, string componentVersion, string componentid)
        {
            string releaseIdOfComponent = null;
            try
            {
                string releaseResponseBody = await m_SW360ApiCommunicationFacade.GetReleaseOfComponentById(componentid);
                releaseIdOfComponent = GetReleaseIdFromResponse(componentName, componentVersion, releaseIdOfComponent, releaseResponseBody);
            }
            catch (HttpRequestException e)
            {
                Logger.Error("GetReleaseIDofComponent():", e);
                Environment.ExitCode = -1;
            }

            if (string.IsNullOrEmpty(releaseIdOfComponent))
            {
                Logger.Warn($"GetReleaseIDofComponent():Release id is null for Component - {componentName}-{componentVersion}");
            }

            return releaseIdOfComponent ?? string.Empty;
        }

        public async Task<string> GetReleaseIdByName(string componentName, string componentVersion)
        {
            string releaseid = string.Empty;
            try
            {
                string responseBody = await m_SW360ApiCommunicationFacade.GetReleaseByCompoenentName(componentName);
                releaseid = GetReleaseIdFromResponse(componentName, componentVersion, releaseid, responseBody);
            }
            catch (HttpRequestException e)
            {
                Logger.Error("GetReleaseIdByName():", e);
                Environment.ExitCode = -1;
            }

            return releaseid ?? string.Empty;
        }

        private static string GetReleaseIdFromResponse(string componentName, string componentVersion, string releaseid, string responseBody)
        {
            var responseData = JsonConvert.DeserializeObject<ReleaseIdOfComponent>(responseBody);
            var listofSw360Releases = responseData?.Embedded?.Sw360Releases ?? new List<Sw360Releases>();
            for (int i = 0; i < listofSw360Releases.Count; i++)
            {
                if (string.Equals(listofSw360Releases[i].Name, componentName, StringComparison.InvariantCultureIgnoreCase)
                    && string.Equals(listofSw360Releases[i].Version, componentVersion, StringComparison.InvariantCultureIgnoreCase))
                {
                    string urlofreleaseid = listofSw360Releases[i]?.Links?.Self?.Href ?? string.Empty;
                    releaseid = CommonHelper.GetSubstringOfLastOccurance(urlofreleaseid, "/");
                }
            }

            return releaseid;
        }

        public async Task<string> GetComponentId(string componentName)
        {
            string ComponentId = "";
            try
            {
                componentName = componentName?.ToLowerInvariant() ?? string.Empty;
                string responseBody = await m_SW360ApiCommunicationFacade.GetComponentByName(componentName);
                var responseData = JsonConvert.DeserializeObject<ComponentsModel>(responseBody);
                string href = string.Empty;
                var sw360ComponentsList = responseData?.Embedded?.Sw360components ?? new List<Sw360Components>();
                if (sw360ComponentsList.Count > 0)
                {
                    Sw360Components component = sw360ComponentsList.FirstOrDefault(x => x.Name.ToLowerInvariant().Equals(componentName));
                    href = component?.Links?.Self?.Href ?? string.Empty;
                }
                ComponentId = CommonHelper.GetSubstringOfLastOccurance(href, "/");
            }
            catch (HttpRequestException e)
            {
                Logger.Error($"GetComponentId():", e);
                Environment.ExitCode = -1;
            }
            catch (AggregateException e)
            {
                Logger.Error($"GetComponentId():", e);
                Environment.ExitCode = -1;
            }
            return ComponentId;
        }


        public string AttachSourcesToReleasesCreated(string releaseId, Dictionary<string, string> attachmentUrlList, ComparisonBomData comparisonBomData)
        {
            Logger.Debug($"AttachSourcesToReleasesCreated(): start");

            string attachmentApiUrl = string.Empty;
            foreach (var attachmenturl in attachmentUrlList)
            {
                AttachReport attachReport = new AttachReport()
                {
                    AttachmentCheckStatus = string.Empty,
                    AttachmentType = attachmenturl.Key,
                    AttachmentFile = attachmenturl.Value,
                    ReleaseId = releaseId,
                    AttachmentReleaseComment = Dataconstant.ReleaseAttachmentComment
                };
                attachmentApiUrl = m_SW360ApiCommunicationFacade.AttachComponentSourceToSW360(attachReport,comparisonBomData);
            }

            Logger.Debug($"AttachSourcesToReleasesCreated(): end");
            return attachmentApiUrl;
        }

        public async Task<bool> UpdatePurlIdForExistingComponent(ComparisonBomData cbomData, string componentId)
        {
            try
            {
                string responseBody = await m_SW360ApiCommunicationFacade.GetReleaseOfComponentById(componentId);
                var componentPurlId = JsonConvert.DeserializeObject<ComponentPurlId>(responseBody);
                Dictionary<string, string> externalIds = new Dictionary<string, string>();
                Dictionary<string, string> existingExternalIds = componentPurlId.ExternalIds;

                bool result = ExternalIdAdditionForComponent(cbomData, componentPurlId, ref externalIds, existingExternalIds);
                if (result)
                {
                    return result;
                }

                ComponentPurlId componentPurlUpdated = new ComponentPurlId
                {
                    ExternalIds = externalIds
                };

                StringContent content = new StringContent(JsonConvert.SerializeObject(componentPurlUpdated), Encoding.UTF8, "application/json");

                var updateResponse = await m_SW360ApiCommunicationFacade.UpdateComponent(componentId, content);
                return updateResponse.IsSuccessStatusCode;
            }
            catch (HttpRequestException ex)
            {
                Logger.Error($"UpdateExternalIdForRelease(): {ex}");
                Environment.ExitCode = -1;
                return false;
            }
            catch (AggregateException ex)
            {
                Logger.Error($"UpdateExternalIdForRelease(): {ex}");
                Environment.ExitCode = -1;
                return false;
            }
        }

        private static bool ExternalIdAdditionForComponent(ComparisonBomData cbomData, ComponentPurlId componentPurlId, ref Dictionary<string, string> externalIds, Dictionary<string, string> existingExternalIds)
        {


            if (componentPurlId.ExternalIds == null || componentPurlId.ExternalIds?.Count == 0)
            {
                externalIds.Add(ApiConstant.PurlId, cbomData.ComponentExternalId);
            }
            else if (!componentPurlId.ExternalIds.ContainsKey(ApiConstant.PurlId))
            {
                externalIds = componentPurlId.ExternalIds;
                externalIds.Add(ApiConstant.PurlId, cbomData.ComponentExternalId);
            }
            else
            {

                return AddingToExistingComponentPurlIdList(existingExternalIds, cbomData, componentPurlId, ref externalIds);

            }
            return false;
        }

        private static bool AddingToExistingComponentPurlIdList(Dictionary<string, string> existingExternalIds, ComparisonBomData cbomData, ComponentPurlId componentPurlId, ref Dictionary<string, string> externalIds)
        {
            bool isUpdated = false;
            foreach (var externalid in existingExternalIds)
            {
                try
                {

                    if (externalid.Key == ApiConstant.PurlId && !externalid.Value.Contains(cbomData.ComponentExternalId))
                    {
                        var externalId = externalid.Value;
                        if (!externalId.Contains('['))
                        {
                            string[] formatedExternalId = new string[] { externalId };
                            externalId = JsonConvert.SerializeObject(formatedExternalId);
                        }

                        var existingIds = JsonConvert.DeserializeObject<string[]>(externalId)?.ToList() ?? new List<string>();
                        existingIds.Add(cbomData.ComponentExternalId);
                        existingIds.ToArray();
                        componentPurlId.ExternalIds.Remove(externalid.Key);
                        externalIds = componentPurlId.ExternalIds;
                        externalIds.Add(ApiConstant.PurlId, JsonConvert.SerializeObject(existingIds));
                        break;
                    }
                    else if (externalid.Key == ApiConstant.PurlId && externalid.Value.Contains(cbomData.ComponentExternalId))
                    {
                        isUpdated = true;
                    }
                    else
                    {
                        // do nothing
                    }
                }

                catch (JsonReaderException ex)
                {
                    Logger.Debug($"ExternalIdAdditionForComponent(): {ex}");
                    isUpdated = true;

                }
            }
            return isUpdated;
        }

        public async Task<bool> UpdatePurlIdForExistingRelease(ComparisonBomData cbomData, string releaseId, ReleasesInfo releasesInfo = null)
        {
            try
            {
                Dictionary<string, string> externalIds = new Dictionary<string, string>();
                Dictionary<string, string> existingExternalIds = releasesInfo?.ExternalIds;

                bool result = ExternalIdAdditionForRelease(cbomData, releasesInfo, ref externalIds, existingExternalIds);
                if (result)
                {
                    return result;
                }
                UpdateReleaseExternalId updateRelease = new UpdateReleaseExternalId
                {
                    ExternalIds = externalIds
                };

                StringContent content = new StringContent(JsonConvert.SerializeObject(updateRelease), Encoding.UTF8, "application/json");

                var updateResponse = await m_SW360ApiCommunicationFacade.UpdateRelease(releaseId, content);

                if (updateResponse != null)
                {
                    return updateResponse.IsSuccessStatusCode;
                }
                else
                {
                    return false;
                }
            }
            catch (HttpRequestException ex)
            {
                Logger.Error($"UpdateExternalIdForRelease(): {ex}");
                Environment.ExitCode = -1;
                return false;
            }
            catch (AggregateException ex)
            {
                Logger.Error($"UpdateExternalIdForRelease(): {ex}");
                Environment.ExitCode = -1;
                return false;
            }
        }
        public async Task<bool> UpdateSourceCodeDownloadURLForExistingRelease(ComparisonBomData cbomData, Dictionary<string, string> attachmentUrlList, string releaseId)
        {
            try
            {                
                Releases release = new Releases
                {                   
                    SourceDownloadurl = GetSourceDownloadUrl(cbomData, attachmentUrlList)                    
                };

                StringContent content = new StringContent(JsonConvert.SerializeObject(release), Encoding.UTF8, "application/json");

                HttpResponseMessage updateResponse = await m_SW360ApiCommunicationFacade.UpdateRelease(releaseId, content);
                string responseContent = await updateResponse.Content.ReadAsStringAsync();
                if (responseContent.Contains(Dataconstant.ModerationRequestMessage, StringComparison.OrdinalIgnoreCase))
                {
                    Logger.Logger.Log(null, Level.Warn, $"Moderation request is created while updating the SourceDownloadURL in SW360. Please request {cbomData.ReleaseCreatedBy} or the license clearing team to approve the moderation request.", null);
                    return false;
                }
                else
                {
                    return true;
                }
            }
            catch (HttpRequestException ex)
            {
                Logger.Error($"UpdateExternalIdForRelease(): {ex}");
                return false;
            }
            catch (AggregateException ex)
            {
                Logger.Error($"UpdateExternalIdForRelease(): {ex}");
                return false;
            }
        }
        private static string GetDecodedExternalId(string ReleaseExternalID)
        {
            string releaseID;
            if (!string.IsNullOrEmpty(ReleaseExternalID) && ReleaseExternalID.Contains(Dataconstant.PurlCheck()["DEBIAN"]))
            {
                releaseID = WebUtility.UrlDecode(ReleaseExternalID);
            }
            else
            {
                return ReleaseExternalID;
            }

            return releaseID;
        }

        private static bool ExternalIdAdditionForRelease(ComparisonBomData cbomData, ReleasesInfo releasesInfo, ref Dictionary<string, string> externalIds, Dictionary<string, string> existingExternalIds)
        {
            if (releasesInfo == null || releasesInfo.ExternalIds == null || releasesInfo.ExternalIds?.Count == 0)
            {
                externalIds.Add(ApiConstant.PurlId, cbomData.ReleaseExternalId);
            }
            else if (!releasesInfo.ExternalIds.ContainsKey(ApiConstant.PurlId))
            {
                externalIds = releasesInfo.ExternalIds;
                externalIds.Add(ApiConstant.PurlId, cbomData.ReleaseExternalId);
            }
            else
            {

                return AddingToExistingReleasePurlIdList(existingExternalIds, cbomData, releasesInfo, ref externalIds);


            }

            return false;
        }

        private static bool AddingToExistingReleasePurlIdList(Dictionary<string, string> existingExternalIds, ComparisonBomData cbomData, ReleasesInfo releasesInfo, ref Dictionary<string, string> externalIds)
        {
            bool isUpdated = false;
            string decodedExternalId = GetDecodedExternalId(cbomData.ReleaseExternalId);
            foreach (var externalid in existingExternalIds)
            {
                try
                {

                    if (externalid.Key == ApiConstant.PurlId && !externalid.Value.Contains(cbomData.ReleaseExternalId) && !externalid.Value.Contains(decodedExternalId))
                    {
                        var externalId = externalid.Value;
                        if (!externalId.Contains('['))
                        {
                            string[] formatedExternalId = new string[] { externalId };
                            externalId = JsonConvert.SerializeObject(formatedExternalId);
                        }
                        var existingIds = JsonConvert.DeserializeObject<string[]>(externalId)?.ToList() ?? new List<string>();
                        existingIds.Add(cbomData.ReleaseExternalId);
                        existingIds.ToArray();
                        releasesInfo.ExternalIds.Remove(externalid.Key);
                        externalIds = releasesInfo.ExternalIds;
                        externalIds.Add(ApiConstant.PurlId, JsonConvert.SerializeObject(existingIds));
                        break;
                    }

                    else if (externalid.Key == ApiConstant.PurlId && (externalid.Value.Contains(cbomData.ReleaseExternalId) || externalid.Value.Contains(decodedExternalId)))
                    {
                        isUpdated = true;
                    }
                    else
                    {
                        // do nothing
                    }
                }
                catch (JsonReaderException ex)
                {
                    Logger.Debug($"ExternalIdAdditionForComponent(): {ex}");
                    isUpdated = true;
                }

            }
            return isUpdated;
        }

        public async Task<ReleasesInfo> GetReleaseInfo(string releaseId)
        {
            ReleasesInfo responsBody = null;
            try
            {
                var responseData = await m_SW360ApiCommunicationFacade.GetReleaseById(releaseId);
                string response = responseData?.Content?.ReadAsStringAsync()?.Result ?? string.Empty;
                responsBody = JsonConvert.DeserializeObject<ReleasesInfo>(response);
            }
            catch (HttpRequestException ex)
            {
                Logger.Debug($"GetReleaseInfo(): {ex}");
            }
            catch (AggregateException ex)
            {
                Logger.Debug($"GetReleaseInfo(): {ex}");
            }

            return responsBody;
        }


        public async Task<bool> UpdateSW360ReleaseContent(Components component, string fossUrl)
        {
            bool isUpdated = false;
            try
            {
                string releaseId = component.ReleaseId;
                Logger.Debug($"UpdateSW360ReleaseContent():Name-{component.Name},Version-{component.Version}");

                UpdateReleaseAdditinoalData updateRelease = await GetUpdateReleaseContent(releaseId, fossUrl, component.UploadId);

                var content = new StringContent(
                    JsonConvert.SerializeObject(updateRelease),
                    Encoding.UTF8,
                    ApiConstant.ApplicationJson);
                HttpResponseMessage response = await m_SW360ApiCommunicationFacade.UpdateRelease(releaseId, content);
                string responseContent = await response.Content.ReadAsStringAsync();
                Logger.Debug($"UpdateSW360ReleaseContent():Response of fossology Url updation in SW360:{responseContent}");
                if (responseContent.Contains(Dataconstant.ModerationRequestMessage, StringComparison.OrdinalIgnoreCase))
                {
                    Logger.Logger.Log(null, Level.Warn, $"\t⏳ Moderation request is created while updating the Fossology URL in SW360. Please request {component.ReleaseCreatedBy} or the license clearing team to approve the moderation request.", null);
                }
                else
                {
                    isUpdated = true;
                }

            }
            catch (HttpRequestException ex)
            {
                Logger.Error($"UpdateSW360ReleaseContent():", ex);
            }
            catch (AggregateException ex)
            {
                Logger.Error($"UpdateSW360ReleaseContent():", ex);
            }

            return isUpdated;
        }

        public async Task<string> GetReleaseByExternalId(string name, string releaseVersion, string releaseExternalId)
        {
            Releasestatus releasestatus = await m_SW360CommonService.GetReleaseDataByExternalId(name, releaseVersion, releaseExternalId);
            string href = releasestatus.sw360Releases?.Links?.Self?.Href ?? string.Empty;
            string releaseId = CommonHelper.GetSubstringOfLastOccurance(href, "/");
            return releaseId;
        }

        public async Task<string> GetComponentIdUsingExternalId(string name, string componentExternalId)
        {
            ComponentStatus componentstatus = await m_SW360CommonService.GetComponentDataByExternalId(name, componentExternalId);
            string href = componentstatus.Sw360components?.Links?.Self?.Href ?? string.Empty;
            string ComponentId = CommonHelper.GetSubstringOfLastOccurance(href, "/");
            return ComponentId;
        }

        private async Task<UpdateReleaseAdditinoalData> GetUpdateReleaseContent(string releaseId, string fossUrl, string uploadId)
        {
            ReleasesInfo releasesInfo = await GetReleaseInfo(releaseId);
            Logger.Debug($"GetUpdateReleaseContent():uploadId-{uploadId} for releaseD-{releaseId}");
            string fossologyUrl = string.Empty;
            if (!string.IsNullOrEmpty(uploadId))
            {
                fossologyUrl = $"{fossUrl}{ApiConstant.FossUploadJobUrlSuffix}{uploadId}";
            }
            Logger.Debug($"GetUpdateReleaseContent():releaseId-{releaseId},fossologyUrl-{fossologyUrl}");

            Dictionary<string, string> additonalData = new Dictionary<string, string>();

            if (releasesInfo?.AdditionalData == null || releasesInfo.AdditionalData?.Count == 0)
            {
                additonalData.Add(ApiConstant.AdditionalDataFossologyURL, fossologyUrl);
            }
            else if (!releasesInfo.AdditionalData.ContainsKey(ApiConstant.AdditionalDataFossologyURL))
            {
                additonalData = releasesInfo.AdditionalData;
                additonalData.Add(ApiConstant.AdditionalDataFossologyURL, fossologyUrl);
            }
            else if (releasesInfo.AdditionalData.ContainsKey(ApiConstant.AdditionalDataFossologyURL))
            {
                additonalData = releasesInfo.AdditionalData;
                if (!additonalData[ApiConstant.AdditionalDataFossologyURL].Equals(fossologyUrl))
                {
                    additonalData[ApiConstant.AdditionalDataFossologyURL] = fossologyUrl;
                }
            }
            else
            {
                // do nothing
            }

            UpdateReleaseAdditinoalData updateRelease = new UpdateReleaseAdditinoalData
            {
                AdditionalData = additonalData,
            };

            return updateRelease;
        }
        public async Task<FossTriggerStatus> TriggerFossologyProcessForValidation(string releaseId, string sw360link)
        {            
            FossTriggerStatus fossTriggerStatus = null;
            try
            {
                string triggerStatus = await m_SW360ApiCommunicationFacade.TriggerFossologyProcess(releaseId, sw360link);
                fossTriggerStatus = JsonConvert.DeserializeObject<FossTriggerStatus>(triggerStatus);
            }
            catch (HttpRequestException ex)
            {
                if (ex.Message == "500:Connection to Fossology server Failed.")
                {
                    Logger.Debug($"TriggerFossologyProcessForValidation():", ex);
                    Logger.Error($"Fossology process failed.Please check fossology configuration or Token in sw360");
                    environmentHelper.CallEnvironmentExit(-1);
                }
            }
            catch (InvalidOperationException ex)
            {
                Logger.Debug($"TriggerFossologyProcessForValidation():", ex);
            }
            catch (UriFormatException ex)
            {
                Logger.Debug($"TriggerFossologyProcessForValidation():", ex);
            }
            catch (TaskCanceledException ex)
            {
                Logger.Debug($"TriggerFossologyProcessForValidation():", ex);
            }
            return fossTriggerStatus;
        }
    }
}
