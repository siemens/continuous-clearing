// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.APICommunications.Model;
using LCT.Common;
using LCT.Services.Interface;
using log4net;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace LCT.PackageIdentifier
{
    /// <summary>
    /// Validates the input params
    /// </summary>
    public static class BomValidator
    {
        static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public static async Task<int> ValidateAppSettings(CommonAppSettings appSettings, ISw360ProjectService bomService, ProjectReleases projectReleases)
        {
            string sw360ProjectName = await bomService.GetProjectNameByProjectIDFromSW360(appSettings.SW360.ProjectID, appSettings.SW360.ProjectName, projectReleases);

            if (string.IsNullOrEmpty(sw360ProjectName))
            {
                throw new InvalidDataException($"Invalid Project Id - {appSettings.SW360.ProjectID}");
            }
            else if (projectReleases?.clearingState == "CLOSED")
            {
                Logger.Error($"Provided Sw360 project is not in active state ,Please make sure you added the correct project details that is in active state..");
                Logger.Debug($"ValidateAppSettings() : Sw360 project " + projectReleases.Name + " is in " + projectReleases.clearingState + " state.");
                return -1;
            }
            else
            {
                appSettings.SW360.ProjectName = sw360ProjectName;
            }
            return 0;
        }
    }
}