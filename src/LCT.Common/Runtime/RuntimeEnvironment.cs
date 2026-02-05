// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

namespace LCT.Common.Runtime
{
    /// <summary>
    /// Provides utilities for detecting the runtime environment (Azure, GitLab, etc.).
    /// </summary>
    public static class RuntimeEnvironment
    {
        #region Methods

        /// <summary>
        /// Gets the current runtime environment type based on environment variables.
        /// </summary>
        /// <returns>The detected environment type (AzureRelease, AzurePipeline, GitLab, or Unknown).</returns>
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

        /// <summary>
        /// Checks if an environment variable is defined and has a non-null, non-whitespace value.
        /// </summary>
        /// <param name="name">The name of the environment variable to check.</param>
        /// <returns>True if the environment variable is defined and has a value; otherwise, false.</returns>
        public static bool IsEnvironmentVariableDefined(string name)
        {
            string value = System.Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }
            return true;
        }

        #endregion
    }
}
