// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using System.Diagnostics.CodeAnalysis;

namespace LCT.Services.Model
{
    /// <summary>
    /// ComponentCreateStatus 
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class ComponentCreateStatus
    {
        #region Properties
        /// <summary>
        /// Gets or sets a value indicating whether the component was created successfully.
        /// </summary>
        public bool IsCreated { get; set; }
        
        /// <summary>
        /// Gets or sets the release creation status.
        /// </summary>
        public ReleaseCreateStatus ReleaseStatus { get; set; }
        #endregion
    }
}
