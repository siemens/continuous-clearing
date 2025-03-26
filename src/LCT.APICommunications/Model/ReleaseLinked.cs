// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

namespace LCT.APICommunications.Model
{
    /// <summary>
    /// The ReleasesLinked info class
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class ReleaseLinked
    {
        public string Name { get; set; } = string.Empty;

        public string Version { get; set; } = string.Empty;

        public string ReleaseId { get; set; } = string.Empty;

        public string Comment { get; set; } = string.Empty;
        public string Relation { get; set; } = string.Empty;
    }
}