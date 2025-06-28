// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.Common.Interface;
using System;

namespace LCT.Common
{
    public class EnvironmentHelper : IEnvironmentHelper
    {
        public void CallEnvironmentExit(int exitCode)
        {
            if (exitCode == -1 || exitCode == 0)
            {
                PipelineArtifactUploader.UploadLogs();
                EnvironmentExit(exitCode);
            }
            else if (exitCode == 2)
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
