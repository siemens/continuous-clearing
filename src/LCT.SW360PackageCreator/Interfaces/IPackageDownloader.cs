// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.Common.Model;
using System.Threading.Tasks;

namespace LCT.SW360PackageCreator.Interfaces
{
    /// <summary>
    /// Interface for package download operations.
    /// </summary>
    public interface IPackageDownloader
    {
        /// <summary>
        /// Asynchronously downloads a package to the specified local path.
        /// </summary>
        /// <param name="component">The comparison BOM data component to download.</param>
        /// <param name="localPathforDownload">The local path where the package should be downloaded.</param>
        /// <returns>A task representing the asynchronous operation that returns the path to the downloaded package.</returns>
        public Task<string> DownloadPackage(ComparisonBomData component, string localPathforDownload);
    }
}
