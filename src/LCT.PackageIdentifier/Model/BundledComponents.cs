// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using System.Diagnostics.CodeAnalysis;

namespace LCT.PackageIdentifier.Model
{
    [ExcludeFromCodeCoverage]
    public class BundledComponents
    {
        public string Name { get; set; }
        public string Version { get; set; }
    }
}
