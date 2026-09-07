// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SIT.Create.Model
{
    /// <summary>
    /// The source package of a binary package, as announced by the index files of an APT archive.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class AptSourceResolution
    {
        #region Properties

        /// <summary>
        /// Gets or sets the name of the source package.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the version of the source package.
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets the download URLs of all files that belong to the source package
        /// (.dsc, .orig.tar.*, .debian.tar.* / .diff.*).
        /// </summary>
        public List<string> FileUrls { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the archive the source package was found in, used for logging only.
        /// </summary>
        public string Origin { get; set; }

        #endregion
    }
}
