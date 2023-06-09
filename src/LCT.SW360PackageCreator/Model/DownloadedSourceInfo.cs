// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using System.Diagnostics.CodeAnalysis;

namespace LCT.SW360PackageCreator.Model
{
    /// <summary>
    /// Downloaded Source Info
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class DownloadedSourceInfo
    {
        public string Name { get; set; }

        public string Version { get; set; }

        public string SourceRepoUrl { get; set; }

        public string DownloadedPath { get; set; }

        public string TaggedVersion { get; set; }
    }
}
