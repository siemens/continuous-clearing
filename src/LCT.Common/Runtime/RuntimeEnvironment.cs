// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

namespace LCT.Common.Runtime
{
    /// <summary>
    /// The RuntimeEnvironment class
    /// </summary>
    public static class RuntimeEnvironment
    {
        public static EnvironmentType GetEnvironment()
        {
            // Azure Release Pipeline contains both "Release_ReleaseId" and
            // "Build_BuildId". Therefore we need to check first for "Release_ReleaseId".
            // https://docs.microsoft.com/en-us/azure/devops/pipelines/release/variables
            if (IsEnvironmentVariableDefined("RELEASE_RELEASEID"))
            {
                return EnvironmentType.AzureRelease;
            }

            // https://docs.microsoft.com/en-us/azure/devops/pipelines/build/variables
            if (IsEnvironmentVariableDefined("BUILD_BUILDID"))
            {
                return EnvironmentType.AzurePipeline;
            }

            // https://docs.gitlab.com/ce/ci/variables/predefined_variables.html
            if (IsEnvironmentVariableDefined("CI_JOB_ID"))
            {
                return EnvironmentType.GitLab;
            }

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
