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
        #region Properties

        /// <summary>
        /// Gets or sets the release name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the version.
        /// </summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the release identifier.
        /// </summary>
        public string ReleaseId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the comment.
        /// </summary>
        public string Comment { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the relation type.
        /// </summary>
        public string Relation { get; set; } = string.Empty;

        #endregion Properties
    }
}