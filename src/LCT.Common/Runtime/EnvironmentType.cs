// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

namespace LCT.Common.Runtime
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
