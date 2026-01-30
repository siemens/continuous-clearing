// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.APICommunications;
using LCT.APICommunications.Interfaces;
using LCT.APICommunications.Model;
using LCT.Common.Model;
using LCT.Facade.Interfaces;
using System.Net.Http;
using System.Threading.Tasks;

namespace LCT.Facade
{

    /// <summary>
    /// SW360 Api Communication Facade class
    /// </summary>
    public class SW360ApicommunicationFacade : ISW360ApicommunicationFacade
    {
        #region Fields
        private readonly ISw360ApiCommunication m_sw360ApiCommunication;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="SW360ApicommunicationFacade"/> class.
        /// </summary>
        /// <param name="sw360ConnectionSettings">The SW360 connection settings.</param>
        public SW360ApicommunicationFacade(SW360ConnectionSettings sw360ConnectionSettings)
        {
            m_sw360ApiCommunication = new SW360Apicommunication(sw360ConnectionSettings);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SW360ApicommunicationFacade"/> class.
        /// </summary>
        /// <param name="sw360ApiCommunication">The SW360 API communication instance.</param>
        public SW360ApicommunicationFacade(ISw360ApiCommunication sw360ApiCommunication)
        {
            m_sw360ApiCommunication = sw360ApiCommunication;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Asynchronously gets all projects from SW360.
        /// </summary>
        /// <returns>A task representing the asynchronous operation that returns the projects as a JSON string.</returns>
        public Task<string> GetProjects()
        {
            return m_sw360ApiCommunication.GetProjects();
        }

        /// <summary>
        /// Asynchronously gets all SW360 users.
        /// </summary>
        /// <returns>A task representing the asynchronous operation that returns the users as a JSON string.</returns>
        public Task<string> GetSw360Users()
        {
            return m_sw360ApiCommunication.GetSw360Users();
        }

        /// <summary>
        /// Asynchronously gets projects by name from SW360.
        /// </summary>
        /// <param name="projectName">The project name to search for.</param>
        /// <returns>A task representing the asynchronous operation that returns the projects as a JSON string.</returns>
        public Task<string> GetProjectsByName(string projectName)
        {
            return m_sw360ApiCommunication.GetProjectsByName(projectName);
        }

        /// <summary>
        /// Asynchronously gets projects by tag from SW360.
        /// </summary>
        /// <param name="projectTag">The project tag to search for.</param>
        /// <returns>A task representing the asynchronous operation that returns an HTTP response message.</returns>
        public Task<HttpResponseMessage> GetProjectsByTag(string projectTag)
        {
            return m_sw360ApiCommunication.GetProjectsByTag(projectTag);
        }

        /// <summary>
        /// Asynchronously gets a project by ID from SW360.
        /// </summary>
        /// <param name="projectId">The project identifier.</param>
        /// <returns>A task representing the asynchronous operation that returns an HTTP response message.</returns>
        public Task<HttpResponseMessage> GetProjectById(string projectId)
        {
            return m_sw360ApiCommunication.GetProjectById(projectId);
        }

        /// <summary>
        /// Asynchronously gets all releases from SW360.
        /// </summary>
        /// <returns>A task representing the asynchronous operation that returns the releases as a JSON string.</returns>
        public Task<string> GetReleases()
        {
            return m_sw360ApiCommunication.GetReleases();
        }
        
        /// <summary>
        /// Asynchronously triggers the FOSSology process for a release.
        /// </summary>
        /// <param name="releaseId">The release identifier.</param>
        /// <param name="sw360link">The SW360 link.</param>
        /// <returns>A task representing the asynchronous operation that returns the process status as a JSON string.</returns>
        public Task<string> TriggerFossologyProcess(string releaseId, string sw360link)
        {
            return m_sw360ApiCommunication.TriggerFossologyProcess(releaseId, sw360link);
        }
        
        /// <summary>
        /// Asynchronously checks the FOSSology process status.
        /// </summary>
        /// <param name="link">The link to check the status.</param>
        /// <returns>A task representing the asynchronous operation that returns an HTTP response message.</returns>
        public Task<HttpResponseMessage> CheckFossologyProcessStatus(string link)
        {
            return m_sw360ApiCommunication.CheckFossologyProcessStatus(link);
        }
        
        /// <summary>
        /// Asynchronously gets all components from SW360.
        /// </summary>
        /// <returns>A task representing the asynchronous operation that returns the components as a JSON string.</returns>
        public Task<string> GetComponents()
        {
            return m_sw360ApiCommunication.GetComponents();
        }

        /// <summary>
        /// Asynchronously gets a release by ID from SW360.
        /// </summary>
        /// <param name="releaseId">The release identifier.</param>
        /// <returns>A task representing the asynchronous operation that returns an HTTP response message.</returns>
        public Task<HttpResponseMessage> GetReleaseById(string releaseId)
        {
            return m_sw360ApiCommunication.GetReleaseById(releaseId);
        }

        /// <summary>
        /// Asynchronously gets a release by link from SW360.
        /// </summary>
        /// <param name="releaseLink">The release link.</param>
        /// <returns>A task representing the asynchronous operation that returns an HTTP response message.</returns>
        public Task<HttpResponseMessage> GetReleaseByLink(string releaseLink)
        {
            return m_sw360ApiCommunication.GetReleaseByLink(releaseLink);
        }

        /// <summary>
        /// Asynchronously links releases to a project in SW360.
        /// </summary>
        /// <param name="httpContent">The HTTP content containing release information.</param>
        /// <param name="sw360ProjectId">The SW360 project identifier.</param>
        /// <returns>A task representing the asynchronous operation that returns an HTTP response message.</returns>
        public async Task<HttpResponseMessage> LinkReleasesToProject(HttpContent httpContent, string sw360ProjectId)
        {
            return await m_sw360ApiCommunication.LinkReleasesToProject(httpContent, sw360ProjectId);
        }

        /// <summary>
        /// Asynchronously creates a component in SW360.
        /// </summary>
        /// <param name="createComponentContent">The component creation data.</param>
        /// <returns>A task representing the asynchronous operation that returns an HTTP response message.</returns>
        public async Task<HttpResponseMessage> CreateComponent(CreateComponent createComponentContent)
        {
            return await m_sw360ApiCommunication.CreateComponent(createComponentContent);
        }

        /// <summary>
        /// Asynchronously creates a release in SW360.
        /// </summary>
        /// <param name="createReleaseContent">The release creation data.</param>
        /// <returns>A task representing the asynchronous operation that returns an HTTP response message.</returns>
        public async Task<HttpResponseMessage> CreateRelease(Releases createReleaseContent)
        {
            return await m_sw360ApiCommunication.CreateRelease(createReleaseContent);
        }

        /// <summary>
        /// Asynchronously gets the release of a component by component ID.
        /// </summary>
        /// <param name="componentId">The component identifier.</param>
        /// <returns>A task representing the asynchronous operation that returns the release data as a JSON string.</returns>
        public Task<string> GetReleaseOfComponentById(string componentId)
        {
            return m_sw360ApiCommunication.GetReleaseOfComponentById(componentId);
        }

        /// <summary>
        /// Asynchronously gets release attachments from SW360.
        /// </summary>
        /// <param name="releaseAttachmentsUrl">The release attachments URL.</param>
        /// <returns>A task representing the asynchronous operation that returns the attachments as a JSON string.</returns>
        public Task<string> GetReleaseAttachments(string releaseAttachmentsUrl)
        {
            return m_sw360ApiCommunication.GetReleaseAttachments(releaseAttachmentsUrl);
        }

        /// <summary>
        /// Asynchronously gets attachment information from SW360.
        /// </summary>
        /// <param name="attachmentUrl">The attachment URL.</param>
        /// <returns>A task representing the asynchronous operation that returns the attachment information as a JSON string.</returns>
        public Task<string> GetAttachmentInfo(string attachmentUrl)
        {
            return m_sw360ApiCommunication.GetAttachmentInfo(attachmentUrl);
        }

        /// <summary>
        /// Downloads an attachment using WebClient.
        /// </summary>
        /// <param name="attachmentDownloadLink">The attachment download link.</param>
        /// <param name="fileName">The file name to save the attachment.</param>
        public void DownloadAttachmentUsingWebClient(string attachmentDownloadLink, string fileName)
        {
            m_sw360ApiCommunication.DownloadAttachmentUsingWebClient(attachmentDownloadLink, fileName);
        }

        /// <summary>
        /// Asynchronously updates a release in SW360.
        /// </summary>
        /// <param name="releaseId">The release identifier.</param>
        /// <param name="httpContent">The HTTP content containing the update data.</param>
        /// <returns>A task representing the asynchronous operation that returns an HTTP response message.</returns>
        public async Task<HttpResponseMessage> UpdateRelease(string releaseId, HttpContent httpContent)
        {
            return await m_sw360ApiCommunication.UpdateRelease(releaseId, httpContent);
        }

        /// <summary>
        /// Asynchronously updates a component in SW360.
        /// </summary>
        /// <param name="componentId">The component identifier.</param>
        /// <param name="httpContent">The HTTP content containing the update data.</param>
        /// <returns>A task representing the asynchronous operation that returns an HTTP response message.</returns>
        public async Task<HttpResponseMessage> UpdateComponent(string componentId, HttpContent httpContent)
        {
            return await m_sw360ApiCommunication.UpdateComponent(componentId, httpContent);
        }

        /// <summary>
        /// Attaches component source to SW360.
        /// </summary>
        /// <param name="attachReport">The attachment report data.</param>
        /// <param name="comparisonBomData">The comparison BOM data.</param>
        /// <returns>The attachment result.</returns>
        public string AttachComponentSourceToSW360(AttachReport attachReport, ComparisonBomData comparisonBomData)
        {
            return m_sw360ApiCommunication.AttachComponentSourceToSW360(attachReport, comparisonBomData);
        }

        /// <summary>
        /// Asynchronously gets a release by component name from SW360.
        /// </summary>
        /// <param name="componentName">The component name.</param>
        /// <returns>A task representing the asynchronous operation that returns the release data as a JSON string.</returns>
        public Task<string> GetReleaseByCompoenentName(string componentName)
        {
            return m_sw360ApiCommunication.GetReleaseByCompoenentName(componentName);
        }

        /// <summary>
        /// Asynchronously gets component details by URL from SW360.
        /// </summary>
        /// <param name="componentLink">The component link.</param>
        /// <returns>A task representing the asynchronous operation that returns an HTTP response message.</returns>
        public Task<HttpResponseMessage> GetComponentDetailsByUrl(string componentLink)
        {

            return m_sw360ApiCommunication.GetComponentDetailsByUrl(componentLink);
        }

        /// <summary>
        /// Asynchronously gets a component by name from SW360.
        /// </summary>
        /// <param name="componentName">The component name.</param>
        /// <returns>A task representing the asynchronous operation that returns the component data as a JSON string.</returns>
        public Task<string> GetComponentByName(string componentName)
        {
            return m_sw360ApiCommunication.GetComponentByName(componentName);
        }
        
        /// <summary>
        /// Asynchronously gets a component using name from SW360.
        /// </summary>
        /// <param name="componentName">The component name.</param>
        /// <returns>A task representing the asynchronous operation that returns an HTTP response message.</returns>
        public Task<HttpResponseMessage> GetComponentUsingName(string componentName)
        {
            return m_sw360ApiCommunication.GetComponentUsingName(componentName);
        }

        /// <summary>
        /// Asynchronously updates a linked release in SW360.
        /// </summary>
        /// <param name="projectId">The project identifier.</param>
        /// <param name="releaseId">The release identifier.</param>
        /// <param name="updateLinkedRelease">The linked release update data.</param>
        /// <returns>A task representing the asynchronous operation that returns an HTTP response message.</returns>
        public Task<HttpResponseMessage> UpdateLinkedRelease(string projectId, string releaseId, UpdateLinkedRelease updateLinkedRelease)
        {
            return m_sw360ApiCommunication.UpdateLinkedRelease(projectId, releaseId, updateLinkedRelease);
        }

        /// <summary>
        /// Asynchronously gets a release by external ID from SW360.
        /// </summary>
        /// <param name="purlId">The PURL identifier.</param>
        /// <param name="externalIdKey">The external ID key (optional).</param>
        /// <returns>A task representing the asynchronous operation that returns an HTTP response message.</returns>
        public Task<HttpResponseMessage> GetReleaseByExternalId(string purlId, string externalIdKey = "")
        {
            return m_sw360ApiCommunication.GetReleaseByExternalId(purlId, externalIdKey);
        }

        /// <summary>
        /// Asynchronously gets a component by external ID from SW360.
        /// </summary>
        /// <param name="purlId">The PURL identifier.</param>
        /// <param name="externalIdKey">The external ID key (optional).</param>
        /// <returns>A task representing the asynchronous operation that returns an HTTP response message.</returns>
        public Task<HttpResponseMessage> GetComponentByExternalId(string purlId, string externalIdKey = "")
        {
            return m_sw360ApiCommunication.GetComponentByExternalId(purlId, externalIdKey);
        }
        
        /// <summary>
        /// Asynchronously gets all releases with all data from SW360.
        /// </summary>
        /// <param name="page">The page number.</param>
        /// <param name="pageEntries">The number of entries per page.</param>
        /// <returns>A task representing the asynchronous operation that returns an HTTP response message.</returns>
        public Task<HttpResponseMessage> GetAllReleasesWithAllData(int page, int pageEntries)
        {
            return m_sw360ApiCommunication.GetAllReleasesWithAllData(page, pageEntries);
        }
        #endregion
     }
 }
