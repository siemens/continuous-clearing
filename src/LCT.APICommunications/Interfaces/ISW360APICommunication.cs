// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.APICommunications.Model;
using LCT.Common.Model;
using System.Net.Http;
using System.Threading.Tasks;

namespace LCT.APICommunications.Interfaces
{
    /// <summary>
    /// Defines the contract for SW360 API communication operations.
    /// </summary>
    public interface ISw360ApiCommunication
    {
        /// <summary>
        /// Asynchronously gets all projects from SW360.
        /// </summary>
        /// <returns>A task containing the projects as a string.</returns>
        Task<string> GetProjects();

        /// <summary>
        /// Asynchronously gets all releases from SW360.
        /// </summary>
        /// <returns>A task containing the releases as a string.</returns>
        Task<string> GetReleases();

        /// <summary>
        /// Asynchronously gets all SW360 users.
        /// </summary>
        /// <returns>A task containing the users as a string.</returns>
        Task<string> GetSw360Users();

        /// <summary>
        /// Asynchronously gets all components from SW360.
        /// </summary>
        /// <returns>A task containing the components as a string.</returns>
        Task<string> GetComponents();

        /// <summary>
        /// Asynchronously gets projects by name.
        /// </summary>
        /// <param name="projectName">The project name to search for.</param>
        /// <returns>A task containing the matching projects as a string.</returns>
        Task<string> GetProjectsByName(string projectName);

        /// <summary>
        /// Asynchronously triggers the Fossology process for a release.
        /// </summary>
        /// <param name="releaseId">The release identifier.</param>
        /// <param name="sw360link">The SW360 link.</param>
        /// <returns>A task containing the result as a string.</returns>
        Task<string> TriggerFossologyProcess(string releaseId, string sw360link);

        /// <summary>
        /// Asynchronously checks the Fossology process status.
        /// </summary>
        /// <param name="link">The status link to check.</param>
        /// <returns>A task containing the HTTP response message.</returns>
        Task<HttpResponseMessage> CheckFossologyProcessStatus(string link);

        /// <summary>
        /// Asynchronously gets a component by its external identifier.
        /// </summary>
        /// <param name="purlId">The package URL identifier.</param>
        /// <param name="externalIdKey">The external identifier key.</param>
        /// <returns>A task containing the HTTP response message.</returns>
        Task<HttpResponseMessage> GetComponentByExternalId(string purlId, string externalIdKey = "");

        /// <summary>
        /// Asynchronously gets a release by its external identifier.
        /// </summary>
        /// <param name="purlId">The package URL identifier.</param>
        /// <param name="externalIdKey">The external identifier key.</param>
        /// <returns>A task containing the HTTP response message.</returns>
        Task<HttpResponseMessage> GetReleaseByExternalId(string purlId, string externalIdKey = "");

        /// <summary>
        /// Asynchronously gets a project by its identifier.
        /// </summary>
        /// <param name="projectId">The project identifier.</param>
        /// <returns>A task containing the HTTP response message.</returns>
        Task<HttpResponseMessage> GetProjectById(string projectId);

        /// <summary>
        /// Asynchronously gets projects by tag.
        /// </summary>
        /// <param name="projectTag">The project tag to filter by.</param>
        /// <returns>A task containing the HTTP response message.</returns>
        Task<HttpResponseMessage> GetProjectsByTag(string projectTag);

        /// <summary>
        /// Asynchronously gets a component using its name.
        /// </summary>
        /// <param name="componentName">The component name.</param>
        /// <returns>A task containing the HTTP response message.</returns>
        Task<HttpResponseMessage> GetComponentUsingName(string componentName);

        /// <summary>
        /// Asynchronously gets a component by name.
        /// </summary>
        /// <param name="componentName">The component name.</param>
        /// <returns>A task containing the component as a string.</returns>
        Task<string> GetComponentByName(string componentName);

        /// <summary>
        /// Asynchronously gets the release of a component by its identifier.
        /// </summary>
        /// <param name="componentId">The component identifier.</param>
        /// <returns>A task containing the release as a string.</returns>
        Task<string> GetReleaseOfComponentById(string componentId);

        /// <summary>
        /// Asynchronously gets release attachments.
        /// </summary>
        /// <param name="releaseAttachmentsUrl">The URL to the release attachments.</param>
        /// <returns>A task containing the attachments as a string.</returns>
        Task<string> GetReleaseAttachments(string releaseAttachmentsUrl);

        /// <summary>
        /// Asynchronously gets attachment information.
        /// </summary>
        /// <param name="attachmentUrl">The attachment URL.</param>
        /// <returns>A task containing the attachment info as a string.</returns>
        Task<string> GetAttachmentInfo(string attachmentUrl);

        /// <summary>
        /// Asynchronously gets a release by its identifier.
        /// </summary>
        /// <param name="releaseId">The release identifier.</param>
        /// <returns>A task containing the HTTP response message.</returns>
        Task<HttpResponseMessage> GetReleaseById(string releaseId);

        /// <summary>
        /// Asynchronously gets a release by its link.
        /// </summary>
        /// <param name="releaseLink">The release link.</param>
        /// <returns>A task containing the HTTP response message.</returns>
        Task<HttpResponseMessage> GetReleaseByLink(string releaseLink);

        /// <summary>
        /// Asynchronously gets a release by component name.
        /// </summary>
        /// <param name="componentName">The component name.</param>
        /// <returns>A task containing the release as a string.</returns>
        Task<string> GetReleaseByCompoenentName(string componentName);

        /// <summary>
        /// Asynchronously creates a new release in SW360.
        /// </summary>
        /// <param name="createReleaseContent">The release content to create.</param>
        /// <returns>A task containing the HTTP response message.</returns>
        Task<HttpResponseMessage> CreateRelease(Releases createReleaseContent);

        /// <summary>
        /// Asynchronously creates a new component in SW360.
        /// </summary>
        /// <param name="createComponentContent">The component content to create.</param>
        /// <returns>A task containing the HTTP response message.</returns>
        Task<HttpResponseMessage> CreateComponent(CreateComponent createComponentContent);

        /// <summary>
        /// Asynchronously links releases to a project.
        /// </summary>
        /// <param name="httpContent">The HTTP content containing link information.</param>
        /// <param name="sw360ProjectId">The SW360 project identifier.</param>
        /// <returns>A task containing the HTTP response message.</returns>
        Task<HttpResponseMessage> LinkReleasesToProject(HttpContent httpContent, string sw360ProjectId);

        /// <summary>
        /// Asynchronously updates a linked release.
        /// </summary>
        /// <param name="projectId">The project identifier.</param>
        /// <param name="releaseId">The release identifier.</param>
        /// <param name="updateLinkedRelease">The update linked release content.</param>
        /// <returns>A task containing the HTTP response message.</returns>
        Task<HttpResponseMessage> UpdateLinkedRelease(string projectId, string releaseId, UpdateLinkedRelease updateLinkedRelease);

        /// <summary>
        /// Asynchronously updates a release.
        /// </summary>
        /// <param name="releaseId">The release identifier.</param>
        /// <param name="httpContent">The HTTP content containing update data.</param>
        /// <returns>A task containing the HTTP response message.</returns>
        Task<HttpResponseMessage> UpdateRelease(string releaseId, HttpContent httpContent);

        /// <summary>
        /// Asynchronously updates a component.
        /// </summary>
        /// <param name="componentId">The component identifier.</param>
        /// <param name="httpContent">The HTTP content containing update data.</param>
        /// <returns>A task containing the HTTP response message.</returns>
        Task<HttpResponseMessage> UpdateComponent(string componentId, HttpContent httpContent);

        /// <summary>
        /// Attaches component source to SW360.
        /// </summary>
        /// <param name="attachReport">The attach report information.</param>
        /// <param name="comparisonBomData">The comparison BOM data.</param>
        /// <returns>The attachment result as a string.</returns>
        string AttachComponentSourceToSW360(AttachReport attachReport, ComparisonBomData comparisonBomData);

        /// <summary>
        /// Downloads an attachment using the web client.
        /// </summary>
        /// <param name="attachmentDownloadLink">The download link for the attachment.</param>
        /// <param name="fileName">The file name to save the attachment as.</param>
        void DownloadAttachmentUsingWebClient(string attachmentDownloadLink, string fileName);

        /// <summary>
        /// Asynchronously gets component details by URL.
        /// </summary>
        /// <param name="componentLink">The component link URL.</param>
        /// <returns>A task containing the HTTP response message.</returns>
        Task<HttpResponseMessage> GetComponentDetailsByUrl(string componentLink);

        /// <summary>
        /// Asynchronously gets all releases with all data using pagination.
        /// </summary>
        /// <param name="page">The page number.</param>
        /// <param name="pageEntries">The number of entries per page.</param>
        /// <returns>A task containing the HTTP response message.</returns>
        Task<HttpResponseMessage> GetAllReleasesWithAllData(int page, int pageEntries);
    }
}
