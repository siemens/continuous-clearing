// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using System.Diagnostics.CodeAnalysis;

namespace LCT.SW360PackageCreator.Model
{
    /// <summary>
    /// MavenPackage constants
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class MavenPackage
    {
        public string ID { get; set; }

        public string Version { get; set; }
        public string GroupID { get; }
    }
}
