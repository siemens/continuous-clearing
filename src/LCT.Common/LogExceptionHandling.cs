// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 



using log4net;
using System;
using System.Net;
using System.Net.Http;
using System.Reflection;



namespace LCT.Common
{
    public class LogExceptionHandling:Exception
    {
        public LogExceptionHandling() { }
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static void HttpException(Exception ex,HttpResponseMessage responce, string exceptionSource) 
        {
            if (400 <=Convert.ToInt32(responce.StatusCode) && Convert.ToInt32(responce.StatusCode) <= 499)
            Logger.Error(ex.Message+ ": The exception may be caused by an incorrect or missing token for " + exceptionSource + ", Please ensure that a valid token is provided and try again");
            if (exceptionSource!="fossology")
            {
                throw new HttpRequestException(ex.ToString());
            }
            
        }
        public static void InternalException(Exception ex, HttpResponseMessage responce, string exceptionSource)
        {
            if (500 <= Convert.ToInt32(responce.StatusCode) && Convert.ToInt32(responce.StatusCode) <= 599)
            Logger.Error(ex.Message + ": The exception may arise because " + exceptionSource  + " is currently unresponsive. Please try again later");
            if (exceptionSource != "fossology")
            {
                throw new WebException(ex.ToString());
            }
            
        }
        public static void GenericExceptions(Exception ex, string exceptionSource)
        {
            Logger.Error(ex.Message+ " : An exception has occurred due to unknown reasons originating from " + exceptionSource);
            if (exceptionSource != "fossology")
            {
                throw new Exception(ex.ToString());
            }
           
        }
    }
}
