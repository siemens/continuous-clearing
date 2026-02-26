// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
// SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

namespace SBOMSigning.Runtime
{
    /// <summary>
    /// The EnvironmentType enum
    /// </summary>
    public enum EnvironmentType
    {
        Unknown = 0,
        GitLab = 1,
        AzurePipeline = 2,
        AzureRelease = 3
    }
}
