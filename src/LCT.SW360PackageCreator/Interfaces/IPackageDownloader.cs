// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.Common.Model;
using System.Threading.Tasks;

namespace LCT.SW360PackageCreator.Interfaces
{
    public interface IPackageDownloader
    {
        public Task<string> DownloadPackage(ComparisonBomData component, string localPathforDownload);
    }
}
