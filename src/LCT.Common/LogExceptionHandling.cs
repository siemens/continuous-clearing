// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 



using log4net;
using log4net.Core;
using System;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;



namespace LCT.Common
{
    public class LogExceptionHandling
    {
        protected LogExceptionHandling() { }
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static void HttpException(HttpRequestException ex,HttpResponseMessage responce, string exceptionSource) 
        {
            if (400 <= Convert.ToInt32(responce.StatusCode) && Convert.ToInt32(responce.StatusCode) <= 499)
            {
                Logger.Logger.Log(null, Level.Error, $"\tThe exception may be caused by an incorrect or missing token for  {exceptionSource} :{ex.Message} Please ensure that a valid token is provided and try again", null);
               
            }
            else if (500 <= Convert.ToInt32(responce.StatusCode) && Convert.ToInt32(responce.StatusCode) <= 599)
            {
                Logger.Logger.Log(null, Level.Error, $"\tThe exception may arise because  {exceptionSource} is currently unresponsive:{ex.Message} Please try again later", null);

            }

            throw new HttpRequestException(ex.ToString());
        }
       
        public static void AggregateException(AggregateException ex, string exceptionSource)
        {
            Logger.Logger.Log(null, Level.Error, $"\t An exception has occurred due to unknown reasons originating from {exceptionSource}:{ex.Message} ", null);

            throw new AggregateException(ex.ToString());

        }
        public static void TaskCanceledException(TaskCanceledException ex, string exceptionSource)
        {
            Logger.Logger.Log(null, Level.Error, $"\t An exception has occurred from {exceptionSource}:{ex.Message} ", null);

            throw new TaskCanceledException(ex.ToString());

        }
        public static void InvalidOperationException(InvalidOperationException ex, string exceptionSource)
        {
            Logger.Logger.Log(null, Level.Error, $"\t An exception has occurred from {exceptionSource}:{ex.Message} ", null);

            throw new InvalidOperationException(ex.ToString());

        }
    }
}
