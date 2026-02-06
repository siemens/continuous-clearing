// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.Common.Logging;
using log4net;
using log4net.Core;
using System;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace LCT.Common
{
    /// <summary>
    /// Provides exception handling utilities for logging different types of exceptions.
    /// </summary>
    public class ExceptionHandling
    {
        #region Fields

        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #endregion

        #region Constructors

        /// <summary>
        /// Prevents external instantiation of the ExceptionHandling class.
        /// </summary>
        protected ExceptionHandling() { }

        #endregion

        #region Methods

        /// <summary>
        /// Handles HTTP exceptions and logs appropriate error messages based on status code.
        /// </summary>
        /// <param name="ex">The HTTP request exception.</param>
        /// <param name="response">The HTTP response message.</param>
        /// <param name="exceptionSource">The source of the exception.</param>
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

        /// <summary>
        /// Handles Fossology-specific exceptions and logs an error message.
        /// </summary>
        /// <param name="ex">The HTTP request exception from Fossology.</param>
        public static void FossologyException(HttpRequestException ex)
        {
            string message = $" The Fossology process could not be completed. Exception: {ex.Message}";
            LoggerHelper.WriteFossologyExceptionMessage(message);
        }

        /// <summary>
        /// Handles argument exceptions and logs missing argument error messages.
        /// </summary>
        /// <param name="message">The message describing the missing arguments.</param>
        public static void ArgumentException(string message)
        {
            Logger.Logger.Log(null, Level.Error, $"Missing Arguments: Please provide the below arguments via inline or in the appSettings.json file to proceed.", null);
            Logger.Logger.Log(null, Level.Warn, $"{message}", null);
        }

        /// <summary>
        /// Handles task cancellation exceptions and logs a timeout error message.
        /// </summary>
        /// <param name="ex">The task canceled exception.</param>
        /// <param name="exceptionSource">The source of the exception.</param>
        public static void TaskCancelledException(TaskCanceledException ex, string exceptionSource)
        {
            Logger.Logger.Log(null, Level.Error, $"A timeout error is thrown from {exceptionSource} server,Please wait for sometime and re run the pipeline again:{ex.Message}", null);
        }

        #endregion
    }
}