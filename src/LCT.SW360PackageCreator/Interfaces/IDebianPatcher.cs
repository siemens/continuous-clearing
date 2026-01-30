// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.Common;
using LCT.Common.Model;

namespace LCT.SW360PackageCreator.Interfaces
{
    /// <summary>
    /// Interface for Debian package patching operations.
    /// </summary>
    public interface IDebianPatcher
    {
        /// <summary>
        /// Applies patches to a Debian component.
        /// </summary>
        /// <param name="component">The comparison BOM data component.</param>
        /// <param name="localDownloadPath">The local download path for the component.</param>
        /// <param name="fileName">The file name of the component.</param>
        /// <returns>The result of the patch operation.</returns>
        public Result ApplyPatch(ComparisonBomData component, string localDownloadPath, string fileName);
    }
}
