﻿// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using log4net;
using log4net.Core;
using System;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace LCT.Common
{
    public class ExceptionHandling
    {
        public ExceptionHandling() { }
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static void HttpException(HttpRequestException ex, HttpResponseMessage response, string exceptionSource)
        {
            if (400 <= Convert.ToInt32(response.StatusCode) && Convert.ToInt32(response.StatusCode) <= 499)
            {
                Logger.Logger.Log(null, Level.Error, $"The exception may be caused by an incorrect projectid or missing token for {exceptionSource} , Please ensure that a valid token is provided and try again:{ex.Message}", null);

            }
            else if ((500 <= Convert.ToInt32(ex.StatusCode) && Convert.ToInt32(ex.StatusCode) <= 599) || ex.StatusCode == null)
            {
                Logger.Logger.Log(null, Level.Error, $"The exception may arise because  {exceptionSource} is currently unresponsive:{ex.Message} Please try again later", null);
            }

        }
        public static void FossologyException(HttpRequestException ex)
        {
            Logger.Logger.Log(null, Level.Error, $"\tThe Fossology process could not be completed. Exception: {ex.Message}", null);
        }

        public static void ArgumentException(string message)
        {
            Logger.Logger.Log(null, Level.Error, $"Missing Arguments: Please provide the below arguments via inline or in the appSettings.json file to proceed.", null);
            Logger.Logger.Log(null, Level.Warn, $"{message}", null);
        }

        public static void TaskCancelledException(TaskCanceledException ex, string exceptionSource)
        {
            Logger.Logger.Log(null, Level.Error, $"A timeout error is thrown from {exceptionSource} server,Please wait for sometime and re run the pipeline again:{ex.Message}", null);
        }
    }
}