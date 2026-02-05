// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
// SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

namespace LCT.Common.Model
{
    /// <summary>
    /// Represents connection settings for SW360 integration.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class SW360ConnectionSettings
    {
        #region Properties

        /// <summary>
        /// Gets or sets the SW360 server URL.
        /// </summary>
        public string SW360URL { get; set; }

        /// <summary>
        /// Gets or sets the SW360 authentication token type.
        /// </summary>
        public string SW360AuthTokenType { get; set; }

        /// <summary>
        /// Gets or sets the SW360 authentication token.
        /// </summary>
        public string Sw360Token { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether test mode is enabled.
        /// </summary>
        public bool IsTestMode { get; set; }

        /// <summary>
        /// Gets or sets the connection timeout in milliseconds.
        /// </summary>
        public int Timeout { get; set; }

        #endregion
    }
}
