// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using log4net;
using System.Reflection;

namespace LCT.Common.Runtime
{
    /// <summary>
    /// The RuntimeEnvironment class
    /// </summary>
    public static class RuntimeEnvironment
    {
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public static EnvironmentType GetEnvironment()
        {
            Logger.Debug("GetEnvironment(): Determining the runtime environment.");
            // Azure Release Pipeline contains both "Release_ReleaseId" and
            // "Build_BuildId". Therefore we need to check first for "Release_ReleaseId".
            // https://docs.microsoft.com/en-us/azure/devops/pipelines/release/variables
            if (IsEnvironmentVariableDefined("RELEASE_RELEASEID"))
            {
                Logger.Debug("GetEnvironment(): Detected Azure Release environment.");
                return EnvironmentType.AzureRelease;
            }

            // https://docs.microsoft.com/en-us/azure/devops/pipelines/build/variables
            if (IsEnvironmentVariableDefined("BUILD_BUILDID"))
            {
                Logger.Debug("GetEnvironment(): Detected Azure Pipeline environment.");
                return EnvironmentType.AzurePipeline;
            }

            // https://docs.gitlab.com/ce/ci/variables/predefined_variables.html
            if (IsEnvironmentVariableDefined("CI_JOB_ID"))
            {
                Logger.Debug("GetEnvironment(): Detected GitLab CI environment.");
                return EnvironmentType.GitLab;
            }
            Logger.Debug("GetEnvironment(): Environment type is unknown.");
            return EnvironmentType.Unknown;
        }

        public static bool IsEnvironmentVariableDefined(string name)
        {
            string value = System.Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }
            return true;
        }
    }
}
