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
        Task<ComponentCreateStatus> CreateComponentBasesOFswComaprisonBOM(
            ComparisonBomData componentInfo, Dictionary<string, string> attachmentUrlList);

        Task<ReleaseCreateStatus> CreateReleaseForComponent(
            ComparisonBomData componentInfo, string componentId, Dictionary<string, string> attachmentUrlList);

        Task<bool> LinkReleasesToProject(List<ReleaseLinked> releasesTobeLinked, List<ReleaseLinked> manuallyLinkedReleases, string sw360ProjectId);

        Task<string> GetReleaseIDofComponent(string componentName, string componentVersion, string componentid);

        Task<FossTriggerStatus> TriggerFossologyProcess(string releaseId, string sw360link);

        Task<CheckFossologyProcess> CheckFossologyProcessStatus(string link);

        Task<string> GetComponentId(string componentName);
        Task<string> GetComponentIdUsingExternalId(string name, string componentExternalId);
        Task<string> GetReleaseByExternalId(string name, string releaseVersion, string releaseExternalId);
        Task<ReleasesInfo> GetReleaseInfo(string releaseId);

        Task<string> GetReleaseIdByName(string componentName, string componentVersion);

        Task<bool> UpdatePurlIdForExistingComponent(ComparisonBomData cbomData, string componentId);

        Task<bool> UpdatePurlIdForExistingRelease(ComparisonBomData cbomData, string releaseId, ReleasesInfo releasesInfo = null);

        Task<bool> UpdateSW360ReleaseContent(Components component, string fossUrl);
        Task<FossTriggerStatus> TriggerFossologyProcessForValidation(string releaseId, string sw360link);
        public string AttachSourcesToReleasesCreated(string releaseId, Dictionary<string, string> attachmentUrlList, ComparisonBomData comparisonBomData);
        Task<bool> UpdateSourceCodeDownloadURLForExistingRelease(ComparisonBomData cbomData, Dictionary<string, string> attachmentUrlList, string releaseId);
    }
}
