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
            Logger.Error(ex.Message+": Exception might cause of wrong/missing token for " + exceptionSource + ", please ensure to give valid token and try again");
            throw new Exception(ex.ToString());
        }
        public static void InternalException(Exception ex, HttpResponseMessage responce, string exceptionSource)
        {
            if (500 <= Convert.ToInt32(responce.StatusCode) && Convert.ToInt32(responce.StatusCode) <= 599)
            Logger.Error(ex.Message + ": Exception might cause of "+ exceptionSource  + " is not responding at this moment, please try again after sometime");
            throw new Exception(ex.ToString());
        }
        public static void GenericExceptions(Exception ex, string exceptionSource)
        {
            Logger.Error(ex.Message+" : exception occured due to some unknown reason from "+ exceptionSource);
            throw new Exception(ex.ToString());
        }
    }
}
