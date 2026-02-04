// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

namespace LCT.APICommunications.Model
{
    /// <summary>
    /// Represents the status of a component.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class ComponentStatus
    {
        #region Properties

        /// <summary>
        /// Gets or sets the SW360 components information.
        /// </summary>
        public Sw360Components Sw360components { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the component exists.
        /// </summary>
        public bool isComponentExist { get; set; }

        #endregion Properties
    }
}
