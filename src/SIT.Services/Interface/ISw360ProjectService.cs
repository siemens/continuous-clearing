// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2026 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using SIT.APICommunications.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SIT.Services.Interface
{
    /// <summary>
    /// The ISw360ProjectService interface
    /// </summary>
    public interface ISw360ProjectService
    {
        /// <summary>
        /// Gets the ProjectName By ProjectID From SW360
        /// </summary>
        /// <param name="projectId">projectId</param>
        /// <param name="projectName">projectName</param>
        /// <returns>string</returns>
        Task<string> GetProjectNameByProjectIDFromSW360(string projectId, string projectName, ProjectReleases projectReleases);


        /// <summary>
        /// gets already linked project id
        /// </summary>
        /// <param name="projectId"></param>
        /// <returns></returns>
        Task<List<ReleaseLinked>> GetAlreadyLinkedReleasesByProjectId(string projectId);

        /// <summary>
        /// Adds or updates a key in the project's additionalData in SW360, preserving existing entries.
        /// </summary>
        /// <param name="projectId">The SW360 project identifier.</param>
        /// <param name="key">The additionalData key to add or update.</param>
        /// <param name="value">The value to set for the given key.</param>
        /// <returns>True if the project was updated successfully; otherwise, false.</returns>
        Task<bool> UpdateProjectAdditionalData(string projectId, string key, string value);
    }
}
