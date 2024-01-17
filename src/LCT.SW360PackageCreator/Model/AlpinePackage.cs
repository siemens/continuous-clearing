// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using System.Diagnostics.CodeAnalysis;

namespace LCT.SW360PackageCreator.Model
{
    /// <summary>
    /// Alpine Package Class
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class AlpinePackage
    {
        public string Name { get; set; }

        public string Version { get; set; }

        public string PurlID { get; set; }

        public string SourceUrl { get; set; }

        public string DownloadUrl { get; set; }


    }
}
