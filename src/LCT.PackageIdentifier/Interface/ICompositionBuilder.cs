// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models;
using NuGet.Versioning;
using System.Collections.Generic;

namespace LCT.PackageIdentifier.Interface
{
    public interface ICompositionBuilder
    {
        void AddCompositionsToBom(Bom bom, Dictionary<string, Dictionary<string, NuGetVersion>> frameworkPackages);
    }
}
