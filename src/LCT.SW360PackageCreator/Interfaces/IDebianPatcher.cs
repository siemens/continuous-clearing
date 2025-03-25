// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.Common;
using LCT.Common.Model;

namespace LCT.SW360PackageCreator.Interfaces
{
    public interface IDebianPatcher
    {
        public Result ApplyPatch(ComparisonBomData component, string localDownloadPath, string fileName);
    }
}
