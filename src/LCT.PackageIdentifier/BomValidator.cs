// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.Common;
using LCT.Services.Interface;
using System;
using System.IO;
using System.Net.Http;
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
            var response=new HttpResponseMessage();
            try
            {
                response = await bomService.GetProjectNameByProjectIDFromSW360(appSettings.SW360ProjectID, appSettings.SW360ProjectName);
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
            {
                LogExceptionHandling.HttpException(ex, response, "SW360");
            }
            catch (AggregateException ex)
            {
                LogExceptionHandling.GenericExceptions(ex, "SW360");
            }

        }
    }
}