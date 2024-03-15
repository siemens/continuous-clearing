// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.Services.Interface;
using System.IO;
using System.Threading.Tasks;
using LCT.Common;
using System.Net.Http;
using System;


namespace LCT.SW360PackageCreator
{
    /// <summary>
    /// Validates the creator param
    /// </summary>
    public static class CreatorValidator
    {
        public static async Task ValidateAppSettings(CommonAppSettings appSettings, ISw360ProjectService sw360ProjectService)
        {

            var response = new HttpResponseMessage();
            try
            {
                response = await sw360ProjectService.GetProjectNameByProjectIDFromSW360(appSettings.SW360ProjectID, appSettings.SW360ProjectName);
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
            {
                LogExceptionHandling.HttpException(ex, response, "SW360");
            }
            catch (AggregateException ex)
            {
                LogExceptionHandling.AggregateException(ex, "SW360");
            }

        }
    }
}
