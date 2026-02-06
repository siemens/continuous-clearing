// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
// SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 


namespace LCT.Common.Model
{
    /// <summary>
    /// Represents information about the CA tool.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class CatoolInfo
    {
        #region Properties

        /// <summary>
        /// Gets or sets the CA tool version.
        /// </summary>
        public string CatoolVersion { get; set; }

        /// <summary>
        /// Gets or sets the CA tool running location.
        /// </summary>
        public string CatoolRunningLocation { get; set; }

        #endregion
    }
}