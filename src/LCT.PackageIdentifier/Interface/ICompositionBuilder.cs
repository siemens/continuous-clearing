// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models;
using LCT.PackageIdentifier.Model;
using NuGet.Versioning;
using System.Collections.Generic;

namespace LCT.PackageIdentifier.Interface
{
    /// <summary>
    /// CompositionBuilder interface
    /// </summary>
    public interface ICompositionBuilder
    {
        /// <summary>
        /// Adds compositions to the BOM based on framework packages and runtime information.
        /// </summary>
        /// <param name="bom">The BOM to add compositions to.</param>
        /// <param name="frameworkPackages">The dictionary of framework packages organized by framework and package name with versions.</param>
        /// <param name="runtimeInfo">The runtime information.</param>
        void AddCompositionsToBom(Bom bom, Dictionary<string, Dictionary<string, NuGetVersion>> frameworkPackages, RuntimeInfo runtimeInfo);
    }
}