// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.Services.Interface;
using System.IO;
using System.Threading.Tasks;
using LCT.Common;


namespace LCT.SW360PackageCreator
{
    /// <summary>
    /// Validates the creator param
    /// </summary>
    public static class CreatorValidator
    {
        public static async Task ValidateAppSettings(CommonAppSettings appSettings, ISw360ProjectService sw360ProjectService)
        {
            string sw360ProjectName = await sw360ProjectService.GetProjectNameByProjectIDFromSW360(appSettings.SW360ProjectID, appSettings.SW360ProjectName);

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
