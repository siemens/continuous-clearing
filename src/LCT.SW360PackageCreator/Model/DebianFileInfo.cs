// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// --------------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;


namespace LCT.SW360PackageCreator.Model
{
    [ExcludeFromCodeCoverage]
    public class DebianFileInfo
    {
        public string name { get; set; }

        public string archive_name { get; set; }

        public string path { get; set; }

        public string first_seen { get; set; }

    }
}
