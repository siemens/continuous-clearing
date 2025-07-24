// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 
using System.Diagnostics.CodeAnalysis;

namespace LCT.PackageIdentifier.Model
{
    [ExcludeFromCodeCoverage]
    public class SpdxComponentInfo
    {
        public bool SpdxComponent { get; set; } = false;
        public string SpdxFilePath { get; set; }
        public bool DevComponent { get; set; } = false;

    }
}
