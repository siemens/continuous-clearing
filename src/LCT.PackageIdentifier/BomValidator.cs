// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.Common;
using LCT.Services.Interface;
using System.IO;
using System.Threading.Tasks;

namespace LCT.PackageIdentifier
{
    /// <summary>
    /// Validates the input params
    /// </summary>
    public static class BomValidator
    {
        public static async Task ValidateAppSettings(CommonAppSettings appSettings, ISw360ProjectService bomService)
        {
            string sw360ProjectName = await bomService.GetProjectNameByProjectIDFromSW360(appSettings.SW360ProjectID, appSettings.SW360ProjectName);


            if (string.IsNullOrEmpty(sw360ProjectName))
            {
                throw new InvalidDataException($"Invalid Project Id - {appSettings.SW360ProjectID}");
            }
            else
            {
                appSettings.SW360ProjectName = sw360ProjectName;
            }
        }
    }
}