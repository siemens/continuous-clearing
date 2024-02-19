using CycloneDX.Models.Vulnerabilities;
using LCT.Common.Interface;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LCT.Common
{
    public class LogExceptionHandling
    {
        public LogExceptionHandling() { }
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static void HttpException(Exception ex,HttpResponseMessage responce, string exceptionSource) 
        {
            if (400 <=Convert.ToInt32(responce.StatusCode) && Convert.ToInt32(responce.StatusCode) <= 499)
            Logger.Error(ex.Message+ ": The exception may be caused by an incorrect or missing token for " + exceptionSource + ", Please ensure that a valid token is provided and try again");
            throw new Exception(ex.ToString());
        }
        public static void InternalException(Exception ex, HttpResponseMessage responce, string exceptionSource)
        {
            if (500 <= Convert.ToInt32(responce.StatusCode) && Convert.ToInt32(responce.StatusCode) <= 599)
            Logger.Error(ex.Message + ": The exception may arise because " + exceptionSource  + " is currently unresponsive. Please try again later");
            throw new Exception(ex.ToString());
        }
        public static void GenericExceptions(Exception ex, string exceptionSource)
        {
            Logger.Error(ex.Message+ " : An exception has occurred due to unknown reasons originating from " + exceptionSource);
            throw new Exception(ex.ToString());
        }
    }
}
