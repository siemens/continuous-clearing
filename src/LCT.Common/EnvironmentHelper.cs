using LCT.Common.Interface;
using System;

namespace LCT.Common
{
    public class EnvironmentHelper : IEnvironmentHelper
    {
        public void CallEnvironmentExit(int code)
        {
            if (code == -1 || code == 0)
            {
                PipelineArtifactUploader.UploadLogs();
                EnvironmentExit(code);
            }
            else if (code == 2)
            {
                Environment.ExitCode = 2;
            }
        }
        private static void EnvironmentExit(int exitCode)
        {
            Environment.Exit(exitCode);
        }

    }
}
