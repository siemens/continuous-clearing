// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using System.Diagnostics.CodeAnalysis;

namespace LCT.SW360PackageCreator.Model
{
    /// <summary>
    /// Debian Package Class
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class DebianPackage
    {
        public string Name { get; set; }

        public string Version { get; set; }

        public string PurlID { get; set; }

        public string SourceUrl { get; set; }

        public string[] PatchURLs { get; set; }

        public string DownloadUrl { get; set; }

        public string JsonText { get; set; }

        public bool IsRetryRequired { get; set; }
    }
}
