// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.APICommunications.Model;
using LCT.Common;
using LCT.Services.Interface;
using System;
using System.IO;
using System.Threading.Tasks;
using log4net;
using System.Reflection;

namespace LCT.PackageIdentifier
{
    /// <summary>
    /// Validates the input params
    /// </summary>
    public static class BomValidator
    {
        static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public static async Task ValidateAppSettings(CommonAppSettings appSettings, ISw360ProjectService bomService)
        {
            ProjectReleases projectReleases = await bomService.GetProjectNameByProjectIDFromSW360(appSettings.SW360ProjectID, appSettings.SW360ProjectName);

            if (projectReleases == null || string.IsNullOrEmpty(projectReleases.Name))
            {
                throw new InvalidDataException($"Invalid Project Id - {appSettings.SW360ProjectID}");
            }
            else if (projectReleases.clearingState == "CLOSED")
            {
                Logger.Error($"Provided Sw360 project is not in active state ,Please make sure you added the correct project details that is in active state..");
                Logger.Debug($"ValidateAppSettings() : Sw360 project "+ projectReleases.Name +" is in " + projectReleases.clearingState +" state.");
                Environment.Exit(-1);
            }
            else  
            {
                appSettings.SW360ProjectName = projectReleases.Name;
            }
        }
    }
}