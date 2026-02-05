// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// --------------------------------------------------------------------------------------------------------------------

using LCT.Common;
using LCT.PackageIdentifier.Model;

namespace LCT.PackageIdentifier.Interface
{
    /// <summary>
    /// RuntimeIdentifier interface
    /// </summary>
    public interface IRuntimeIdentifier
    {
        /// <summary>
        /// Registers the runtime identifier.
        /// </summary>
        void Register();

        /// <summary>
        /// Identifies the runtime information based on application settings.
        /// </summary>
        /// <param name="appSettings">The common application settings.</param>
        /// <returns>The runtime information.</returns>
        RuntimeInfo IdentifyRuntime(CommonAppSettings appSettings);
    }
}
