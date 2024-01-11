// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.APICommunications.Model;
using System.Net.Http;
using System.Threading.Tasks;

namespace LCT.Facade.Interfaces
{
    /// <summary>
    ///SW360 Api Communication Facade interface
    /// </summary>
    public interface ISW360ApicommunicationFacade
    {
        Task<string> GetProjects();
        Task<string> GetReleases();
        Task<string> GetSw360Users();
        Task<string> GetComponents();
        Task<string> GetProjectsByName(string projectName);
        Task<string> TriggerFossologyProcess(string releaseId, string sw360link);
        Task<HttpResponseMessage> CheckFossologyProcessStatus(string link);
        Task<HttpResponseMessage> GetProjectById(string projectId);
        Task<HttpResponseMessage> GetProjectsByTag(string projectTag);
        Task<HttpResponseMessage> GetComponentUsingName(string componentName);
        Task<string> GetComponentByName(string componentName);
        Task<string> GetReleaseOfComponentById(string componentId);
        Task<string> GetReleaseAttachments(string releaseAttachmentsUrl);
        Task<string> GetAttachmentInfo(string attachmentUrl);
        Task<HttpResponseMessage> GetReleaseById(string releaseId);
        Task<HttpResponseMessage> GetReleaseByLink(string releaseLink);
        Task<string> GetReleaseByCompoenentName(string componentName);
        Task<HttpResponseMessage> CreateRelease(Releases createReleaseContent);
        Task<HttpResponseMessage> CreateComponent(CreateComponent createComponentContent);
        Task<HttpResponseMessage> LinkReleasesToProject(string[] releaseidArray, string sw360ProjectId);
        Task<HttpResponseMessage> UpdateRelease(string releaseId, HttpContent httpContent);
        Task<HttpResponseMessage> UpdateComponent(string componentId, HttpContent httpContent);
        string AttachComponentSourceToSW360(AttachReport attachReport);
        void DownloadAttachmentUsingWebClient(string attachmentDownloadLink, string fileName);
        Task<HttpResponseMessage> GetComponentDetailsByUrl(string componentLink);
        Task<HttpResponseMessage> UpdateLinkedRelease(string projectId, string releaseId, UpdateLinkedRelease updateLinkedRelease);
        Task<HttpResponseMessage> GetReleaseByExternalId(string purlId,string externalIdKey = "");
        Task<HttpResponseMessage> GetComponentByExternalId(string purlId, string externalIdKey = "");
    }
}
