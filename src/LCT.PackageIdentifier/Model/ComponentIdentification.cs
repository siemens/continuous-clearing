// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models;
using System.Collections.Generic;

namespace LCT.PackageIdentifier.Model
{

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class ComponentIdentification
    {
        #region Properties
        /// <summary>
        /// Components from the comparison BOM used to match against the project.
        /// </summary>
        public List<Component> comparisonBOMData { get; set; }

        /// <summary>
        /// Internal components discovered within the project.
        /// </summary>
        public List<Component> internalComponents { get; set; }
        #endregion

        #region Constructors
        #endregion

        #region Methods
        #endregion

        #region Events
        #endregion

    }
}
