// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using System.Diagnostics.CodeAnalysis;

namespace SIT.Common.Model
{
    /// <summary>
    /// Describes a Debian style APT archive that is used to resolve the source packages
    /// (.dsc, .orig.tar.*, .debian.tar.*) of the binary packages found in an image.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class AptRepository
    {
        #region Properties

        /// <summary>
        /// Gets or sets the display name of the repository, used for logging only.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the archive root of the repository, i.e. the URL that contains the "dists" and "pool" folders.
        /// </summary>
        public string Uri { get; set; }

        /// <summary>
        /// Gets or sets the suites (codenames) to search. When empty, the distribution of the scanned component is used.
        /// </summary>
        public string[] Suites { get; set; }

        /// <summary>
        /// Gets or sets the archive components to search. When empty, the components announced by the Release file are used.
        /// </summary>
        public string[] Components { get; set; }

        /// <summary>
        /// Gets or sets the binary architectures to search. When empty, the architectures announced by the Release file are used.
        /// </summary>
        public string[] Architectures { get; set; }

        #endregion
    }
}
