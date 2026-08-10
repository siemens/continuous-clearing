// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2026 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

namespace SIT.Common.Runtime
{
    /// <summary>
    /// Represents the type of runtime environment where the application is executing.
    /// </summary>
    public enum EnvironmentType
    {
        /// <summary>
        /// Unknown or unrecognized environment.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// GitLab CI/CD environment.
        /// </summary>
        GitLab = 1,

        /// <summary>
        /// Azure DevOps build pipeline environment.
        /// </summary>
        AzurePipeline = 2,

        /// <summary>
        /// Azure DevOps release pipeline environment.
        /// </summary>
        AzureRelease = 3
    }
}
