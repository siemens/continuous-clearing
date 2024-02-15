using LCT.Common.Interface;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LCT.Common
{
    public class ExceptionHandling
    {
        public ExceptionHandling() { }
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static void HandleHttpException(Exception ex, string exceptionSource) 
        {
            Logger.Error(ex.Message);
        }
    }
}
