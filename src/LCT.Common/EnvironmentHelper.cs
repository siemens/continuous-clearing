// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.Common.Interface;
using System;

namespace LCT.Common
{
    /// <summary>
    /// Provides helper methods for managing environment exit operations.
    /// </summary>
    public class EnvironmentHelper : IEnvironmentHelper
    {
        #region Methods

        /// <summary>
        /// Calls environment exit with appropriate handling based on exit code.
        /// </summary>
        /// <param name="exitCode">The exit code to use (-1, 0, or 2).</param>
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

        /// <summary>
        /// Exits the environment with the specified exit code.
        /// </summary>
        /// <param name="exitCode">The exit code to use.</param>
        private static void EnvironmentExit(int exitCode)
        {
            Environment.Exit(exitCode);
        }

        #endregion
    }
}
