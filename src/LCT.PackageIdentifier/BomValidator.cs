// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.APICommunications.Model;
using LCT.Common;
using LCT.Services.Interface;
using System.Threading.Tasks;

namespace LCT.PackageIdentifier
{
    /// <summary>
    /// Validates the input params
    /// </summary>
    public static class BomValidator
    {
        public static async Task<int> ValidateAppSettings(CommonAppSettings appSettings, ISw360ProjectService bomService, ProjectReleases projectReleases)
        {
            string sw360ProjectName = await bomService.GetProjectNameByProjectIDFromSW360(appSettings.SW360.ProjectID, appSettings.SW360.ProjectName, projectReleases);

            return CommonHelper.ValidateSw360Project(sw360ProjectName, projectReleases?.ClearingState, projectReleases?.Name, appSettings);
        }

    }
}