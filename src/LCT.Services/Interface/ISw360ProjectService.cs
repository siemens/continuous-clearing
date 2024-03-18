// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.APICommunications.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LCT.Services.Interface
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
        Task<string> GetProjectNameByProjectIDFromSW360(string projectId, string projectName);

        Task<List<ReleaseLinked>> GetAlreadyLinkedReleasesByProjectId(string projectId);
    }
}
