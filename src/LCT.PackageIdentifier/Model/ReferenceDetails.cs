// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using System.Diagnostics.CodeAnalysis;

namespace LCT.PackageIdentifier.Model
{
    /// <summary>
    /// Reference Details
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class ReferenceDetails
    {
        public string Library { get; set; }
        public string Version { get; set; }
        public bool Private { get; set; }

    }
}
