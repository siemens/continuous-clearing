// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT

// -------------------------------------------------------------------------------------------------------------------- 

using LCT.APICommunications.Model;
using System.Threading.Tasks;

namespace LCT.Services.Interface
{
    /// <summary>
    /// the interface ISW360CommonService, provides common services 
    /// </summary>
    public interface ISW360CommonService
    {
        /// <summary>
        /// Gets the componet data by component external id
        /// </summary>
        /// <param name="componentName">componentName</param>
        /// <param name="componentExternalId">componentExternalId</param>
        /// <param name="isComponentExist">isComponentExist</param>
        /// <returns>Sw360Components</returns>
        Task<ComponentStatus> GetComponentDataByExternalId(string componentName, string componentExternalId);

        /// <summary>
        /// Gets the release data by release external id
        /// </summary>
        /// <param name="releaseName">releaseName</param>
        /// <param name="releaseVersion">releaseVersion</param>
        /// <param name="releaseExternalId">releaseExternalId</param>
        /// <param name="isReleaseExist">isReleaseExist</param>
        /// <returns>Sw360Releases</returns>
        Task<Releasestatus> GetReleaseDataByExternalId(string releaseName, string releaseVersion, string releaseExternalId);

        /// <summary>
        /// Gets the ReleaseId By using the ComponentId & version
        /// </summary>
        /// <param name="componentId">componentId</param>
        /// <param name="componentVersion">componentVersion</param>
        /// <returns>string</returns>
        Task<string> GetReleaseIdByComponentId(string componentId, string componentVersion);
    }
}
