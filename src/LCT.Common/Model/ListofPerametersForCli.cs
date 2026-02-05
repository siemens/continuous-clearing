// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using System.Diagnostics.CodeAnalysis;

namespace LCT.Common.Model
{
    /// <summary>
    /// Represents a list of parameters for the CLI.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class ListofPerametersForCli
    {
        #region Properties

        /// <summary>
        /// Gets or sets the internal repository list.
        /// </summary>
        public string InternalRepoList { get; set; }

        /// <summary>
        /// Gets or sets the include pattern.
        /// </summary>
        public string Include { get; set; }

        /// <summary>
        /// Gets or sets the exclude pattern.
        /// </summary>
        public string Exclude { get; set; }

        /// <summary>
        /// Gets or sets the components to exclude.
        /// </summary>
        public string ExcludeComponents { get; set; }

        #endregion
    }
}
