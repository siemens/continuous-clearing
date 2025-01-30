using LCT.Common.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LCT.Common
{
    public class EnvironmentHelper : IEnvironmentHelper
    {
        public void CallEnvironmentExit(int code)
        {
            if (code == -1)
            {
                PipelineArtifactUploader.UploadLogs();
                EnvironmentExit(code);
            }
        }
        public static void EnvironmentExit(int exitCode)
        {
            Environment.Exit(exitCode);
        }

    }
}
