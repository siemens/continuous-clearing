// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.APICommunications.Model;
using LCT.APICommunications.Model.Foss;
using LCT.Common.Model;
using LCT.Services.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LCT.Services.Interface
{
    /// <summary>
    /// ISW360CreatorService interface
    /// </summary>
    public interface ISw360CreatorService
    {
        /// <summary>
        /// Asynchronously creates a component in SW360 based on comparison BOM data.
        /// </summary>
        /// <param name="componentInfo">The component information from comparison BOM.</param>
        /// <param name="attachmentUrlList">The dictionary of attachment URLs.</param>
        /// <returns>A task representing the asynchronous operation that returns the component creation status.</returns>
        Task<ComponentCreateStatus> CreateComponentBasesOFswComaprisonBOM(
            ComparisonBomData componentInfo, Dictionary<string, string> attachmentUrlList);

        /// <summary>
        /// Asynchronously creates a release for a component in SW360.
        /// </summary>
        /// <param name="componentInfo">The component information from comparison BOM.</param>
        /// <param name="componentId">The SW360 component identifier.</param>
        /// <param name="attachmentUrlList">The dictionary of attachment URLs.</param>
        /// <returns>A task representing the asynchronous operation that returns the release creation status.</returns>
        Task<ReleaseCreateStatus> CreateReleaseForComponent(
            ComparisonBomData componentInfo, string componentId, Dictionary<string, string> attachmentUrlList);

        /// <summary>
        /// Asynchronously links releases to a SW360 project.
        /// </summary>
        /// <param name="releasesTobeLinked">The list of releases to be linked.</param>
        /// <param name="manuallyLinkedReleases">The list of manually linked releases.</param>
        /// <param name="sw360ProjectId">The SW360 project identifier.</param>
        /// <returns>A task representing the asynchronous operation that returns true if successful; otherwise, false.</returns>
        Task<bool> LinkReleasesToProject(List<ReleaseLinked> releasesTobeLinked, List<ReleaseLinked> manuallyLinkedReleases, string sw360ProjectId);

        /// <summary>
        /// Asynchronously gets the release identifier of a component.
        /// </summary>
        /// <param name="componentName">The component name.</param>
        /// <param name="componentVersion">The component version.</param>
        /// <param name="componentid">The component identifier.</param>
        /// <returns>A task representing the asynchronous operation that returns the release identifier.</returns>
        Task<string> GetReleaseIDofComponent(string componentName, string componentVersion, string componentid);

        /// <summary>
        /// Asynchronously triggers the FOSSology process for a release.
        /// </summary>
        /// <param name="releaseId">The release identifier.</param>
        /// <param name="sw360link">The SW360 link.</param>
        /// <returns>A task representing the asynchronous operation that returns the FOSSology trigger status.</returns>
        Task<FossTriggerStatus> TriggerFossologyProcess(string releaseId, string sw360link);

        /// <summary>
        /// Asynchronously checks the FOSSology process status.
        /// </summary>
        /// <param name="link">The link to check status.</param>
        /// <returns>A task representing the asynchronous operation that returns the FOSSology process status.</returns>
        Task<CheckFossologyProcess> CheckFossologyProcessStatus(string link);

        /// <summary>
        /// Asynchronously gets the component identifier by name.
        /// </summary>
        /// <param name="componentName">The component name.</param>
        /// <returns>A task representing the asynchronous operation that returns the component identifier.</returns>
        Task<string> GetComponentId(string componentName);
        
        /// <summary>
        /// Asynchronously gets the component identifier using external ID.
        /// </summary>
        /// <param name="name">The component name.</param>
        /// <param name="componentExternalId">The component external identifier.</param>
        /// <returns>A task representing the asynchronous operation that returns the component identifier.</returns>
        Task<string> GetComponentIdUsingExternalId(string name, string componentExternalId);
        
        /// <summary>
        /// Asynchronously gets the release by external ID.
        /// </summary>
        /// <param name="name">The component name.</param>
        /// <param name="releaseVersion">The release version.</param>
        /// <param name="releaseExternalId">The release external identifier.</param>
        /// <returns>A task representing the asynchronous operation that returns the release identifier.</returns>
        Task<string> GetReleaseByExternalId(string name, string releaseVersion, string releaseExternalId);
        
        /// <summary>
        /// Asynchronously gets the release information.
        /// </summary>
        /// <param name="releaseId">The release identifier.</param>
        /// <returns>A task representing the asynchronous operation that returns the release information.</returns>
        Task<ReleasesInfo> GetReleaseInfo(string releaseId);

        /// <summary>
        /// Asynchronously gets the release identifier by component name and version.
        /// </summary>
        /// <param name="componentName">The component name.</param>
        /// <param name="componentVersion">The component version.</param>
        /// <returns>A task representing the asynchronous operation that returns the release identifier.</returns>
        Task<string> GetReleaseIdByName(string componentName, string componentVersion);

        /// <summary>
        /// Asynchronously updates the PURL identifier for an existing component.
        /// </summary>
        /// <param name="cbomData">The comparison BOM data.</param>
        /// <param name="componentId">The component identifier.</param>
        /// <returns>A task representing the asynchronous operation that returns true if successful; otherwise, false.</returns>
        Task<bool> UpdatePurlIdForExistingComponent(ComparisonBomData cbomData, string componentId);

        /// <summary>
        /// Asynchronously updates the PURL identifier for an existing release.
        /// </summary>
        /// <param name="cbomData">The comparison BOM data.</param>
        /// <param name="releaseId">The release identifier.</param>
        /// <param name="releasesInfo">The optional release information.</param>
        /// <returns>A task representing the asynchronous operation that returns true if successful; otherwise, false.</returns>
        Task<bool> UpdatePurlIdForExistingRelease(ComparisonBomData cbomData, string releaseId, ReleasesInfo releasesInfo = null);

        /// <summary>
        /// Asynchronously updates the SW360 release content.
        /// </summary>
        /// <param name="component">The component to update.</param>
        /// <param name="fossUrl">The FOSSology URL.</param>
        /// <returns>A task representing the asynchronous operation that returns true if successful; otherwise, false.</returns>
        Task<bool> UpdateSW360ReleaseContent(Components component, string fossUrl);
        
        /// <summary>
        /// Asynchronously triggers the FOSSology process for validation.
        /// </summary>
        /// <param name="releaseId">The release identifier.</param>
        /// <param name="sw360link">The SW360 link.</param>
        /// <returns>A task representing the asynchronous operation that returns the FOSSology trigger status.</returns>
        Task<FossTriggerStatus> TriggerFossologyProcessForValidation(string releaseId, string sw360link);
        
        /// <summary>
        /// Attaches sources to releases created in SW360.
        /// </summary>
        /// <param name="releaseId">The release identifier.</param>
        /// <param name="attachmentUrlList">The dictionary of attachment URLs.</param>
        /// <param name="comparisonBomData">The comparison BOM data.</param>
        /// <returns>The result of the attachment operation.</returns>
        public string AttachSourcesToReleasesCreated(string releaseId, Dictionary<string, string> attachmentUrlList, ComparisonBomData comparisonBomData);
        
        /// <summary>
        /// Asynchronously updates the source code download URL for an existing release.
        /// </summary>
        /// <param name="cbomData">The comparison BOM data.</param>
        /// <param name="attachmentUrlList">The dictionary of attachment URLs.</param>
        /// <param name="releaseId">The release identifier.</param>
        /// <returns>A task representing the asynchronous operation that returns true if successful; otherwise, false.</returns>
        Task<bool> UpdateSourceCodeDownloadURLForExistingRelease(ComparisonBomData cbomData, Dictionary<string, string> attachmentUrlList, string releaseId);
    }
}
