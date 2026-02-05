// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
//---------------------------------------------------------------------------------------------------------------------

namespace LCT.APICommunications.Model
{
    /// <summary>
    /// Specifies the type of package.
    /// </summary>
    public enum PackageType
    {
        /// <summary>
        /// Represents a cleared third-party package.
        /// </summary>
        ClearedThirdParty,

        /// <summary>
        /// Represents an internal package.
        /// </summary>
        Internal,

        /// <summary>
        /// Represents a development package.
        /// </summary>
        Development,

        /// <summary>
        /// Represents an unknown package type.
        /// </summary>
        Unknown
    }
}
