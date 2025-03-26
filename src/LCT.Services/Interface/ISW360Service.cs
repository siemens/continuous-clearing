// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.APICommunications.Model;
using LCT.APICommunications.Model.Foss;
using LCT.Common.Model;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace LCT.Services.Interface
{
    /// <summary>
    /// ISW360Service interface
    /// </summary>
    public interface ISW360Service
    {
        Task<string> GetComponentReleaseID(string componentName, string version);

        Task<HttpResponseMessage> GetReleaseInfoByReleaseId(string releaseLink);

        Task<ReleasesInfo> GetReleaseDataOfComponent(string releaseLink);

        Task<List<Components>> GetAvailableReleasesInSw360(List<Components> listOfComponentsToBom);

        Task<Sw360AttachmentHash> GetAttachmentDownloadLink(string releaseAttachmentUrl);

        Task<string> GetUploadDescriptionfromSW360(string componentName, string componetVersion, string sw360url);
        Task<List<Components>> GetAvailablePackagesInSw360(List<Components> listOfComponentsToBom);   

    }
}
